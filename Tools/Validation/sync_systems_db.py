#!/usr/bin/env python3
"""
System Discovery & Database Sync Tool

Scans src/Features/ for C# system files (*Behavior.cs, *Manager.cs, *Generator.cs, *Handler.cs),
extracts metadata from XML doc comments, and updates the core_systems table in schema.sql.

This ensures the database always reflects the actual codebase - code is truth, database is derived.

Usage:
    python Tools/Validation/sync_systems_db.py [--dry-run] [--verbose]
    
Options:
    --dry-run   Show what would change without modifying schema.sql
    --verbose   Show detailed parsing information

Exit codes:
    0   Success, database in sync (or updated)
    1   Error during execution
    2   Drift detected (dry-run mode only)
"""

import argparse
import os
import re
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import List, Dict, Optional, Set, Tuple

# ============================================================================
# Configuration
# ============================================================================

PROJECT_ROOT = Path(__file__).parent.parent.parent
FEATURES_DIR = PROJECT_ROOT / "src" / "Features"
SCHEMA_FILE = PROJECT_ROOT / "Tools" / "CrewAI" / "database" / "schema.sql"

# System file patterns - files matching these are considered "systems"
SYSTEM_PATTERNS = ["*Behavior.cs", "*Manager.cs", "*Generator.cs", "*Handler.cs", "*Orchestrator.cs"]

# Files to exclude (internal, UI-only, or not gameplay systems)
EXCLUDE_PATTERNS = [
    "*VM.cs",           # ViewModels are UI, not systems
    "*Tests.cs",        # Test files
    "*State.cs",        # State classes are data, not systems
    "*Models.cs",       # Model classes
    "*Helper.cs",       # Helper utilities
    "*Validator.cs",    # Validation utilities
]

# Marker comments in schema.sql for the core_systems section
MARKER_START = "-- Core systems (from src/Features)"
MARKER_END = "-- System dependencies"


# ============================================================================
# Data Models
# ============================================================================

@dataclass
class DiscoveredSystem:
    """A system discovered from the codebase."""
    name: str                    # Class name (e.g., "EnlistmentBehavior")
    full_name: str               # Namespace.ClassName (e.g., "Features.Enlistment.Behaviors.EnlistmentBehavior")
    system_type: str             # "Behavior", "Manager", "Generator", "Handler", "Static"
    file_path: str               # Relative path from project root
    description: str             # From XML summary comment
    key_responsibilities: str    # From summary or inferred
    
    def to_sql_insert(self) -> str:
        """Generate SQL INSERT statement for this system."""
        # Escape single quotes in strings
        desc = self.description.replace("'", "''")
        resp = self.key_responsibilities.replace("'", "''")
        name = self.name.replace("'", "''")
        full = self.full_name.replace("'", "''")
        path = self.file_path.replace("'", "''")
        
        return (
            f"('{name}', '{full}', '{self.system_type}', "
            f"'{path}', '{desc}', '{resp}')"
        )


# ============================================================================
# Parsing Logic
# ============================================================================

def extract_namespace(content: str) -> str:
    """Extract namespace from C# file content."""
    # Handle file-scoped namespace (C# 10+)
    match = re.search(r'namespace\s+([\w.]+)\s*;', content)
    if match:
        return match.group(1)
    
    # Handle traditional namespace block
    match = re.search(r'namespace\s+([\w.]+)\s*\{', content)
    if match:
        return match.group(1)
    
    return ""


def extract_class_name(content: str, filename: str) -> Optional[str]:
    """Extract the main class name from file content."""
    # Look for public class/sealed class/static class matching the filename pattern
    base_name = Path(filename).stem
    
    # Try exact match first (handles: public class, public sealed class, public static class)
    pattern = rf'(?:public|internal)\s+(?:sealed\s+|static\s+)?class\s+({re.escape(base_name)})\s*[:<\{{]'
    match = re.search(pattern, content)
    if match:
        return match.group(1)
    
    # Fallback: any public class in the file
    pattern = r'(?:public|internal)\s+(?:sealed\s+|static\s+)?class\s+(\w+)\s*[:<\{]'
    match = re.search(pattern, content)
    if match:
        return match.group(1)
    
    return None


def extract_xml_summary(content: str, class_name: str) -> str:
    """Extract XML summary comment for the class."""
    # Look for /// <summary> block preceding the class declaration
    # Pattern: summary block followed eventually by class declaration
    pattern = rf'/// <summary>\s*(.*?)\s*/// </summary>.*?(?:public|internal)\s+(?:sealed\s+)?class\s+{re.escape(class_name)}'
    
    match = re.search(pattern, content, re.DOTALL)
    if match:
        summary_block = match.group(1)
        # Clean up: remove /// prefixes and normalize whitespace
        lines = summary_block.split('\n')
        cleaned_lines = []
        for line in lines:
            line = re.sub(r'^\s*///', '', line).strip()
            if line:
                cleaned_lines.append(line)
        
        summary = ' '.join(cleaned_lines)
        # Truncate if too long (database column limit)
        if len(summary) > 200:
            summary = summary[:197] + "..."
        return summary
    
    return "No description available"


def infer_system_type(class_name: str, content: str) -> str:
    """Infer system type from class name and content."""
    if class_name.endswith("Behavior"):
        # Check if it extends CampaignBehaviorBase
        if "CampaignBehaviorBase" in content:
            return "Behavior"
        return "Behavior"
    elif class_name.endswith("Manager"):
        # Check if static or CampaignBehavior
        if "CampaignBehaviorBase" in content:
            return "Behavior"
        if "static class" in content or re.search(r'public\s+static\s+\w+\s+Instance', content):
            return "Static"
        return "Manager"
    elif class_name.endswith("Generator"):
        if "CampaignBehaviorBase" in content:
            return "Behavior"
        return "Generator"
    elif class_name.endswith("Handler"):
        if "CampaignBehaviorBase" in content:
            return "Behavior"
        return "Handler"
    elif class_name.endswith("Orchestrator"):
        if "CampaignBehaviorBase" in content:
            return "Behavior"
        return "Orchestrator"
    
    return "Unknown"


def infer_responsibilities(class_name: str, summary: str, content: str) -> str:
    """Infer key responsibilities from class name, summary, and content."""
    responsibilities = []
    
    # Extract from summary keywords
    keywords_map = {
        "save": "save/load",
        "load": "save/load", 
        "persist": "save/load",
        "event": "event handling",
        "tick": "daily/hourly ticks",
        "daily": "daily updates",
        "menu": "menu system",
        "ui": "UI integration",
        "dialog": "dialogue system",
        "conversation": "conversations",
        "order": "order management",
        "camp": "camp system",
        "battle": "combat handling",
        "promotion": "tier advancement",
        "xp": "XP tracking",
        "reputation": "reputation tracking",
        "escalation": "escalation tracking",
        "supply": "supply management",
        "schedule": "scheduling",
        "forecast": "forecasting",
        "pacing": "pacing control",
    }
    
    summary_lower = summary.lower()
    found = set()
    for keyword, responsibility in keywords_map.items():
        if keyword in summary_lower and responsibility not in found:
            responsibilities.append(responsibility)
            found.add(responsibility)
    
    # Check RegisterEvents for additional clues
    if "DailyTickEvent" in content:
        if "daily ticks" not in found and "daily updates" not in found:
            responsibilities.append("daily ticks")
    if "HourlyTickEvent" in content:
        responsibilities.append("hourly ticks")
    if "SyncData" in content and "save/load" not in found:
        responsibilities.append("save/load")
    
    # Limit to 3-4 responsibilities for readability
    if len(responsibilities) > 4:
        responsibilities = responsibilities[:4]
    
    if not responsibilities:
        return "Core system functionality"
    
    return ", ".join(responsibilities)


def should_exclude_file(filepath: Path) -> bool:
    """Check if file should be excluded from system discovery."""
    filename = filepath.name
    for pattern in EXCLUDE_PATTERNS:
        if Path(filename).match(pattern):
            return True
    return False


def discover_systems(features_dir: Path, verbose: bool = False) -> List[DiscoveredSystem]:
    """Scan features directory and discover all systems."""
    systems = []
    
    for pattern in SYSTEM_PATTERNS:
        for filepath in features_dir.rglob(pattern):
            if should_exclude_file(filepath):
                if verbose:
                    print(f"  [SKIP] {filepath.name} (excluded pattern)")
                continue
            
            try:
                content = filepath.read_text(encoding='utf-8-sig')
            except Exception as e:
                print(f"  [WARN] Could not read {filepath}: {e}")
                continue
            
            class_name = extract_class_name(content, filepath.name)
            if not class_name:
                if verbose:
                    print(f"  [SKIP] {filepath.name} (no class found)")
                continue
            
            namespace = extract_namespace(content)
            full_name = f"{namespace}.{class_name}" if namespace else class_name
            
            # Strip "Enlisted." prefix if present (for consistency with existing data)
            if full_name.startswith("Enlisted."):
                full_name = full_name[9:]
            
            summary = extract_xml_summary(content, class_name)
            system_type = infer_system_type(class_name, content)
            responsibilities = infer_responsibilities(class_name, summary, content)
            
            # Relative path from project root
            rel_path = str(filepath.relative_to(PROJECT_ROOT)).replace("\\", "/")
            
            system = DiscoveredSystem(
                name=class_name,
                full_name=full_name,
                system_type=system_type,
                file_path=rel_path,
                description=summary,
                key_responsibilities=responsibilities
            )
            systems.append(system)
            
            if verbose:
                print(f"  [FOUND] {class_name} ({system_type}) - {rel_path}")
    
    return systems


# ============================================================================
# Database Schema Update Logic
# ============================================================================

def parse_existing_systems(schema_content: str) -> Set[str]:
    """Parse existing system names from schema.sql."""
    systems = set()
    
    # Find the INSERT block for core_systems
    pattern = r"INSERT OR IGNORE INTO core_systems.*?VALUES\s*(.*?);\s*\n\s*--"
    match = re.search(pattern, schema_content, re.DOTALL)
    
    if match:
        values_block = match.group(1)
        # Extract system names from each tuple
        for row_match in re.finditer(r"\('(\w+)',", values_block):
            systems.add(row_match.group(1))
    
    return systems


def generate_systems_sql(systems: List[DiscoveredSystem]) -> str:
    """Generate the SQL INSERT block for core_systems."""
    lines = [MARKER_START]
    lines.append("INSERT OR IGNORE INTO core_systems (name, full_name, type, file_path, description, key_responsibilities) VALUES")
    
    # Sort systems alphabetically by name for consistent output
    sorted_systems = sorted(systems, key=lambda s: s.name)
    
    for i, system in enumerate(sorted_systems):
        suffix = "," if i < len(sorted_systems) - 1 else ";"
        lines.append(system.to_sql_insert() + suffix)
    
    lines.append("")
    lines.append(MARKER_END)
    
    return "\n".join(lines)


def update_schema_file(schema_path: Path, new_sql: str) -> bool:
    """Update schema.sql with new core_systems INSERT block."""
    content = schema_path.read_text(encoding='utf-8')
    
    # Find and replace the core_systems section
    # Pattern: from MARKER_START to MARKER_END (inclusive of the line before dependencies)
    pattern = rf'{re.escape(MARKER_START)}.*?(?={re.escape(MARKER_END)})'
    
    if not re.search(pattern, content, re.DOTALL):
        print(f"[ERROR] Could not find core_systems section in {schema_path}")
        return False
    
    # Replace just the INSERT block, keeping MARKER_END
    new_content = re.sub(
        pattern,
        new_sql.replace(MARKER_END, ""),  # Don't include end marker in replacement
        content,
        flags=re.DOTALL
    )
    
    schema_path.write_text(new_content, encoding='utf-8')
    return True


# ============================================================================
# Main Logic
# ============================================================================

def compare_systems(discovered: List[DiscoveredSystem], existing: Set[str]) -> Tuple[List[str], List[str], List[str]]:
    """Compare discovered systems to existing database entries.
    
    Returns:
        (added, removed, unchanged) - lists of system names
    """
    discovered_names = {s.name for s in discovered}
    
    added = list(discovered_names - existing)
    removed = list(existing - discovered_names)
    unchanged = list(discovered_names & existing)
    
    return sorted(added), sorted(removed), sorted(unchanged)


def main():
    parser = argparse.ArgumentParser(description="Sync C# systems to database schema.sql")
    parser.add_argument("--dry-run", action="store_true", help="Show changes without modifying files")
    parser.add_argument("--verbose", "-v", action="store_true", help="Show detailed parsing info")
    args = parser.parse_args()
    
    print("=" * 60)
    print("System Discovery & Database Sync")
    print("=" * 60)
    
    if not FEATURES_DIR.exists():
        print(f"[ERROR] Features directory not found: {FEATURES_DIR}")
        return 1
    
    if not SCHEMA_FILE.exists():
        print(f"[ERROR] Schema file not found: {SCHEMA_FILE}")
        return 1
    
    # Phase 1: Discover systems from code
    print(f"\n[Phase 1] Scanning {FEATURES_DIR}...")
    discovered = discover_systems(FEATURES_DIR, args.verbose)
    print(f"  Found {len(discovered)} systems")
    
    # Phase 2: Parse existing database entries
    print(f"\n[Phase 2] Reading existing schema from {SCHEMA_FILE.name}...")
    schema_content = SCHEMA_FILE.read_text(encoding='utf-8')
    existing = parse_existing_systems(schema_content)
    print(f"  Found {len(existing)} existing entries")
    
    # Phase 3: Compare
    print("\n[Phase 3] Comparing...")
    added, removed, unchanged = compare_systems(discovered, existing)
    
    if added:
        print(f"\n  NEW SYSTEMS (will be added):")
        for name in added:
            system = next(s for s in discovered if s.name == name)
            print(f"    + {name} ({system.system_type}) - {system.file_path}")
    
    if removed:
        print(f"\n  REMOVED SYSTEMS (will be deleted):")
        for name in removed:
            print(f"    - {name}")
    
    if not added and not removed:
        print("\n  ✓ Database is in sync with code - no changes needed")
        return 0
    
    # Phase 4: Generate and apply changes
    print(f"\n[Phase 4] {'Generating changes (dry-run)' if args.dry_run else 'Applying changes'}...")
    
    new_sql = generate_systems_sql(discovered)
    
    if args.dry_run:
        print("\n[DRY-RUN] Would update schema.sql with:")
        print("-" * 40)
        # Show just first few and last few lines
        lines = new_sql.split('\n')
        if len(lines) > 10:
            for line in lines[:5]:
                print(f"  {line}")
            print(f"  ... ({len(lines) - 10} more lines) ...")
            for line in lines[-5:]:
                print(f"  {line}")
        else:
            for line in lines:
                print(f"  {line}")
        print("-" * 40)
        print("\n[DRY-RUN] No files modified. Run without --dry-run to apply.")
        return 2 if (added or removed) else 0
    
    # Apply changes
    if update_schema_file(SCHEMA_FILE, new_sql):
        print(f"\n  ✓ Updated {SCHEMA_FILE.name}")
        print(f"\n[Summary]")
        print(f"  Added:     {len(added)} systems")
        print(f"  Removed:   {len(removed)} systems")
        print(f"  Unchanged: {len(unchanged)} systems")
        print(f"  Total:     {len(discovered)} systems")
        print("\n[IMPORTANT] Review changes and run: git diff Tools/CrewAI/database/schema.sql")
        return 0
    else:
        print("[ERROR] Failed to update schema file")
        return 1


if __name__ == "__main__":
    sys.exit(main())
