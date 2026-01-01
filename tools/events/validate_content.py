#!/usr/bin/env python3
"""
Enhanced Content Validation Tool for Enlisted Mod

Validates events, decisions, and orders against structural rules, logical constraints,
and integration requirements documented in docs/Features/Technical/conflict-detection-system.md

Usage:
    python tools/events/validate_content.py [--strict] [--fix-refs]

Validation Phases:
    Phase 1: Structure validation (JSON schema, required fields, enum values)
    Phase 2: Reference validation (localization strings, skills, traits)
    Phase 3: Logical validation (impossible combinations, reasonable values)
    Phase 4: Consistency checks (flags, multi-stage events, priorities)
"""

import argparse
import glob
import json
import re
import sys
import xml.etree.ElementTree as ET
from collections import Counter, defaultdict
from pathlib import Path
from typing import Dict, List, Set, Tuple, Any, Optional

# ============================================================================
# Constants and Reference Data
# ============================================================================

# Valid Bannerlord skills
VALID_SKILLS = {
    "OneHanded", "TwoHanded", "Polearm", "Bow", "Crossbow", "Throwing",
    "Riding", "Athletics", "Crafting", "Scouting", "Tactics", "Roguery",
    "Charm", "Leadership", "Trade", "Stewardship", "Medicine", "Engineering"
}

# Valid roles as defined by the Identity System
VALID_ROLES = {
    "Any", "Scout", "Medic", "Engineer", "Officer", "Operative", "NCO", "Soldier"
}

# Valid contexts
VALID_CONTEXTS = {
    "Any", "War", "Peace", "Siege", "Battle", "Town", "Village", "Camp", "March"
}

# Valid categories
VALID_CATEGORIES = {
    "decision", "escalation", "role", "universal", "muster", "crisis", "general",
    "onboarding", "pay", "promotion", "retinue", "training"
}

# Valid time of day
VALID_TIME_OF_DAY = {"dawn", "morning", "midday", "afternoon", "evening", "night"}

# Role tier requirements (minimum tier for each role)
ROLE_MIN_TIERS = {
    "Officer": 5,
    "NCO": 4,
    "Operative": 3,
    "Scout": 1,
    "Medic": 1,
    "Engineer": 1,
    "Soldier": 1,
    "Any": 1
}

# Cooldown guidelines (min, max, suggested) in days
COOLDOWN_RANGES = {
    "core_decisions": (1, 2, "Core decisions like rest, training"),
    "social_economic": (3, 7, "Social/economic decisions"),
    "rare_special": (14, 30, "Rare or special decisions"),
    "major_events": (30, 60, "Major narrative events"),
}

# Priority ranges
PRIORITY_VALUES = {"critical": (80, 100), "high": (60, 79), "normal": (40, 59), "low": (20, 39), "rare": (1, 19)}

# Escalation tracks and their ranges
ESCALATION_TRACKS = {
    "scrutiny": (0, 10),
    "discipline": (0, 10),
    "medical_risk": (0, 5),
    "pay_tension": (0, 100),
    "pay_tension_min": (0, 100),  # Alias used in triggers
}

# ============================================================================
# Data Structures
# ============================================================================

class ValidationIssue:
    """Represents a validation issue with severity and context."""
    
    def __init__(self, severity: str, category: str, message: str, file_path: str, event_id: str = None):
        self.severity = severity  # "error", "warning", "info"
        self.category = category  # e.g., "structure", "reference", "logic", "consistency"
        self.message = message
        self.file_path = file_path
        self.event_id = event_id
    
    def __str__(self):
        prefix = f"[{self.severity.upper()}]"
        location = f"{Path(self.file_path).name}"
        if self.event_id:
            location += f":{self.event_id}"
        return f"{prefix} {location} [{self.category}] {self.message}"


class ValidationContext:
    """Accumulates validation issues and provides reporting."""
    
    def __init__(self, strict: bool = False):
        self.strict = strict
        self.issues: List[ValidationIssue] = []
        self.stats = Counter()
        self.event_ids: Set[str] = set()
        self.flag_references: Dict[str, List[str]] = defaultdict(list)  # flag -> [event_ids using it]
        self.flag_setters: Dict[str, List[str]] = defaultdict(list)  # flag -> [event_ids setting it]
        
    def add_issue(self, severity: str, category: str, message: str, file_path: str, event_id: str = None):
        """Add a validation issue."""
        self.issues.append(ValidationIssue(severity, category, message, file_path, event_id))
        self.stats[f"{severity}_{category}"] += 1
    
    def has_errors(self) -> bool:
        """Check if any errors were recorded."""
        return any(issue.severity == "error" for issue in self.issues)
    
    def has_critical_issues(self) -> bool:
        """Check if there are issues that should block merge."""
        return self.has_errors() or (self.strict and self.has_warnings())
    
    def has_warnings(self) -> bool:
        """Check if any warnings were recorded."""
        return any(issue.severity == "warning" for issue in self.issues)
    
    def print_report(self):
        """Print validation report."""
        # Group issues by severity
        errors = [i for i in self.issues if i.severity == "error"]
        warnings = [i for i in self.issues if i.severity == "warning"]
        infos = [i for i in self.issues if i.severity == "info"]
        
        print("\n" + "=" * 80)
        print("VALIDATION REPORT")
        print("=" * 80)
        
        if errors:
            print(f"\n❌ ERRORS ({len(errors)}):")
            for issue in errors[:50]:  # Limit to first 50
                print(f"  {issue}")
            if len(errors) > 50:
                print(f"  ... and {len(errors) - 50} more errors")
        
        if warnings:
            print(f"\n⚠️  WARNINGS ({len(warnings)}):")
            for issue in warnings[:50]:
                print(f"  {issue}")
            if len(warnings) > 50:
                print(f"  ... and {len(warnings) - 50} more warnings")
        
        if infos:
            print(f"\nℹ️  INFO ({len(infos)}):")
            for issue in infos[:20]:
                print(f"  {issue}")
            if len(infos) > 20:
                print(f"  ... and {len(infos) - 20} more info messages")
        
        # Summary
        print("\n" + "-" * 80)
        print("SUMMARY:")
        print(f"  Total Events: {len(self.event_ids)}")
        print(f"  Errors: {len(errors)}")
        print(f"  Warnings: {len(warnings)}")
        print(f"  Info: {len(infos)}")
        
        # Category breakdown
        if self.stats:
            print("\nBy Category:")
            for category, count in sorted(self.stats.items()):
                print(f"  {category}: {count}")
        
        print("=" * 80 + "\n")


# ============================================================================
# Localization String Loader
# ============================================================================

def load_localization_strings() -> Set[str]:
    """Load all string IDs from enlisted_strings.xml."""
    xml_path = Path("ModuleData/Languages/enlisted_strings.xml")
    if not xml_path.exists():
        print(f"Warning: Localization file not found at {xml_path}")
        return set()
    
    try:
        tree = ET.parse(xml_path)
        root = tree.getroot()
        string_ids = set()
        
        for string_elem in root.findall(".//string"):
            string_id = string_elem.get("id")
            if string_id:
                string_ids.add(string_id)
        
        print(f"[INFO] Loaded {len(string_ids)} localization strings from enlisted_strings.xml")
        return string_ids
    except Exception as e:
        print(f"Error loading localization strings: {e}")
        return set()


# ============================================================================
# Phase 1: Structure Validation
# ============================================================================

def validate_structure(event: Dict, file_path: str, ctx: ValidationContext) -> bool:
    """Validate event structure and required fields."""
    event_id = event.get("id", "UNKNOWN")
    
    # Required fields
    if not event_id or event_id == "UNKNOWN":
        ctx.add_issue("error", "structure", "Missing or empty 'id' field", file_path)
        return False
    
    # Track event ID
    if event_id in ctx.event_ids:
        ctx.add_issue("error", "structure", f"Duplicate event ID: {event_id}", file_path, event_id)
    else:
        ctx.event_ids.add(event_id)
    
    # Category
    category = event.get("category", "")
    if not category:
        ctx.add_issue("error", "structure", "Missing 'category' field", file_path, event_id)
    elif category not in VALID_CATEGORIES:
        ctx.add_issue("warning", "structure", f"Unknown category: '{category}'", file_path, event_id)
    
    # Title and setup (check both new and legacy locations)
    title_id = event.get("titleId") or (event.get("content", {}).get("titleId"))
    setup_id = event.get("setupId") or (event.get("content", {}).get("setupId"))
    
    if not title_id:
        ctx.add_issue("error", "structure", "Missing 'titleId' field", file_path, event_id)
    if not setup_id:
        ctx.add_issue("error", "structure", "Missing 'setupId' field", file_path, event_id)
    
    # Options (check both locations)
    options = event.get("options", []) or event.get("content", {}).get("options", [])
    if not options:
        ctx.add_issue("error", "structure", "Missing or empty 'options' array", file_path, event_id)
    elif not (2 <= len(options) <= 4):
        ctx.add_issue("error", "structure", f"Invalid option count: {len(options)} (must be 2-4)", file_path, event_id)
    
    # Validate option structure
    for i, option in enumerate(options):
        opt_id = option.get("id", f"option_{i}")
        if not option.get("textId") and not option.get("text"):
            ctx.add_issue("error", "structure", f"Option '{opt_id}' missing textId and fallback text", file_path, event_id)
        
        # Tooltip check (tooltips cannot be null)
        if not option.get("tooltip"):
            ctx.add_issue("error", "structure", f"Option '{opt_id}' missing tooltip (tooltips cannot be null)", file_path, event_id)
        elif len(option.get("tooltip", "")) > 80:
            ctx.add_issue("warning", "structure", f"Option '{opt_id}' tooltip exceeds 80 chars ({len(option['tooltip'])})", file_path, event_id)
    
    return True


# ============================================================================
# Phase 2: Reference Validation
# ============================================================================

def validate_references(event: Dict, file_path: str, ctx: ValidationContext, localization_ids: Set[str]):
    """Validate references to localization strings, skills, traits."""
    event_id = event.get("id", "UNKNOWN")
    
    # Check localization references
    title_id = event.get("titleId") or (event.get("content", {}).get("titleId"))
    setup_id = event.get("setupId") or (event.get("content", {}).get("setupId"))
    
    if title_id and title_id not in localization_ids:
        ctx.add_issue("warning", "reference", f"titleId '{title_id}' not found in enlisted_strings.xml", file_path, event_id)
    if setup_id and setup_id not in localization_ids:
        ctx.add_issue("warning", "reference", f"setupId '{setup_id}' not found in enlisted_strings.xml", file_path, event_id)
    
    # Check option references
    options = event.get("options", []) or event.get("content", {}).get("options", [])
    for option in options:
        text_id = option.get("textId")
        result_id = option.get("resultTextId")
        
        if text_id and text_id not in localization_ids:
            ctx.add_issue("warning", "reference", f"textId '{text_id}' not found in enlisted_strings.xml", file_path, event_id)
        if result_id and result_id not in localization_ids:
            ctx.add_issue("warning", "reference", f"resultTextId '{result_id}' not found in enlisted_strings.xml", file_path, event_id)
    
    # Check skill references
    requirements = event.get("requirements", {})
    min_skills = requirements.get("minSkills", {})
    for skill_name in min_skills.keys():
        if skill_name not in VALID_SKILLS:
            ctx.add_issue("error", "reference", f"Invalid skill name: '{skill_name}'", file_path, event_id)
    
    # Check skill XP rewards and effects
    for option in options:
        # Check rewards.skillXp (for sub-choices)
        rewards = option.get("rewards", {})
        xp_rewards = rewards.get("xp", {}) or rewards.get("skillXp", {})
        for skill_name in xp_rewards.keys():
            if skill_name not in VALID_SKILLS:
                ctx.add_issue("error", "reference", f"Invalid skill name in rewards: '{skill_name}'", file_path, event_id)
        
        # Check effects.skillXp (for main choices)
        effects = option.get("effects", {})
        effects_xp = effects.get("skillXp", {})
        for skill_name in effects_xp.keys():
            if skill_name not in VALID_SKILLS:
                ctx.add_issue("error", "reference", f"Invalid skill name in effects: '{skill_name}'", file_path, event_id)
        
        # Check failEffects.skillXp (for failed skill checks)
        fail_effects = option.get("failEffects", {})
        fail_xp = fail_effects.get("skillXp", {})
        for skill_name in fail_xp.keys():
            if skill_name not in VALID_SKILLS:
                ctx.add_issue("error", "reference", f"Invalid skill name in failEffects: '{skill_name}'", file_path, event_id)


# ============================================================================
# Phase 3: Logical Validation
# ============================================================================

def validate_logic(event: Dict, file_path: str, ctx: ValidationContext):
    """Validate logical consistency and impossible combinations."""
    event_id = event.get("id", "UNKNOWN")
    requirements = event.get("requirements", {})
    
    # Get tier requirements
    tier_req = requirements.get("tier", {})
    min_tier = tier_req.get("min") or requirements.get("minTier")
    max_tier = tier_req.get("max") or requirements.get("maxTier")
    
    # Get role requirement
    role = requirements.get("role", "Any")
    
    # Rule 1: Check tier × role combinations
    if role in ROLE_MIN_TIERS:
        role_min = ROLE_MIN_TIERS[role]
        if min_tier and min_tier < role_min:
            ctx.add_issue("error", "logic", 
                f"Impossible tier×role: role '{role}' requires tier {role_min}+, but minTier={min_tier}",
                file_path, event_id)
        if max_tier and max_tier < role_min:
            ctx.add_issue("error", "logic",
                f"Impossible tier×role: role '{role}' requires tier {role_min}+, but maxTier={max_tier}",
                file_path, event_id)
    
    # Rule 2: Check context for camp decisions
    category = event.get("category", "")
    context = requirements.get("context", "Any")
    
    if event_id.startswith("dec_") and context == "Battle":
        ctx.add_issue("error", "logic",
            "Camp Hub decisions (dec_*) cannot require 'Battle' context (Camp Hub unavailable during battles)",
            file_path, event_id)
    
    # Rule 3: Skill requirements should match role
    min_skills = requirements.get("minSkills", {})
    if role == "Medic" and min_skills:
        if "Medicine" not in min_skills and any(s != "Medicine" for s in min_skills.keys()):
            ctx.add_issue("warning", "logic",
                f"Role 'Medic' usually requires Medicine skill, but minSkills={list(min_skills.keys())}",
                file_path, event_id)
    elif role == "Engineer" and min_skills:
        if "Engineering" not in min_skills and any(s != "Engineering" for s in min_skills.keys()):
            ctx.add_issue("warning", "logic",
                f"Role 'Engineer' usually requires Engineering skill, but minSkills={list(min_skills.keys())}",
                file_path, event_id)
    
    # Rule 4: Check escalation requirements
    triggers = event.get("triggers", {})
    escalation_reqs = triggers.get("escalation_requirements", {}) or requirements.get("minEscalation", {})
    
    for track, value in escalation_reqs.items():
        if track in ESCALATION_TRACKS:
            min_val, max_val = ESCALATION_TRACKS[track]
            if not (min_val <= value <= max_val):
                ctx.add_issue("error", "logic",
                    f"Escalation track '{track}' value {value} out of range ({min_val}-{max_val})",
                    file_path, event_id)
    
    # Rule 5: Check cooldown reasonableness
    timing = event.get("timing", {})
    cooldown = timing.get("cooldown_days", timing.get("cooldownDays", 0))
    
    if cooldown < 0:
        ctx.add_issue("error", "logic", f"Negative cooldown: {cooldown}", file_path, event_id)
    elif event_id.startswith("dec_rest") and cooldown > 7:
        ctx.add_issue("warning", "logic",
            f"Rest decisions should have short cooldowns (1-2 days), but cooldown={cooldown}",
            file_path, event_id)
    elif event_id.startswith("dec_") and cooldown > 60:
        ctx.add_issue("warning", "logic",
            f"Unusually long cooldown for decision: {cooldown} days",
            file_path, event_id)
    
    # Rule 7: HP requirements only for medical events
    hp_below = requirements.get("hp_below") or requirements.get("hpBelow")
    if hp_below is not None:
        if "treatment" not in event_id.lower() and "medical" not in event_id.lower() and "wound" not in event_id.lower():
            ctx.add_issue("warning", "logic",
                f"HP requirement (hp_below={hp_below}) used in non-medical event",
                file_path, event_id)
    
    # Rule 9: Priority validation
    priority = timing.get("priority", "normal")
    one_time = timing.get("one_time", False)
    
    if one_time and priority in ["low", "rare"]:
        ctx.add_issue("warning", "logic",
            f"One-time event with low priority ({priority}) - should use 'high' or 'critical'",
            file_path, event_id)
    
    # Rule 10: Order events should grant XP
    order_type = event.get("order_type")
    if order_type:  # This is an order event
        options = event.get("options", [])
        for option in options:
            opt_id = option.get("id", "unknown")
            effects = option.get("effects", {})
            fail_effects = option.get("failEffects", {})
            
            # Check if this option grants any XP
            has_xp = "skillXp" in effects or "skillXp" in fail_effects
            
            # Warn if no XP is granted (order events should reward player progression)
            if not has_xp:
                ctx.add_issue("warning", "logic",
                    f"Order event option '{opt_id}' grants no skillXp - players expect XP for completing orders",
                    file_path, event_id)


# ============================================================================
# Phase 4: Consistency Checks
# ============================================================================

def validate_consistency(event: Dict, file_path: str, ctx: ValidationContext):
    """Validate flag usage and multi-stage event consistency."""
    event_id = event.get("id", "UNKNOWN")
    
    # Track flag references and setters
    triggers = event.get("triggers", {})
    all_triggers = triggers.get("all", [])
    any_triggers = triggers.get("any", [])
    none_triggers = triggers.get("none", [])
    
    for trigger in all_triggers + any_triggers + none_triggers:
        if trigger.startswith("has_flag:"):
            flag_name = trigger.replace("has_flag:", "")
            ctx.flag_references[flag_name].append(event_id)
    
    # Track flag setters
    options = event.get("options", []) or event.get("content", {}).get("options", [])
    for option in options:
        effects = option.get("effects", {})
        flags_set = effects.get("setFlags", []) or option.get("flags_set", [])
        for flag in flags_set:
            ctx.flag_setters[flag].append(event_id)


def validate_flag_consistency(ctx: ValidationContext):
    """Check for flags that are referenced but never set, or set but never referenced."""
    # Flags referenced but never set
    for flag, references in ctx.flag_references.items():
        if flag not in ctx.flag_setters:
            ctx.add_issue("warning", "consistency",
                f"Flag '{flag}' referenced by {len(references)} event(s) but never set: {references[:3]}",
                "flag_analysis", None)
    
    # Flags set but never referenced (might be okay for terminal flags)
    for flag, setters in ctx.flag_setters.items():
        if flag not in ctx.flag_references:
            ctx.add_issue("info", "consistency",
                f"Flag '{flag}' set by event(s) but never referenced (terminal flag?): {setters[:3]}",
                "flag_analysis", None)


# ============================================================================
# Main Validation Pipeline
# ============================================================================

def validate_event_file(file_path: str, ctx: ValidationContext, localization_ids: Set[str]):
    """Validate a single event JSON file."""
    try:
        with open(file_path, encoding="utf-8") as f:
            data = json.load(f)
    except json.JSONDecodeError as e:
        ctx.add_issue("error", "structure", f"Invalid JSON: {e}", file_path)
        return
    except Exception as e:
        ctx.add_issue("error", "structure", f"Failed to read file: {e}", file_path)
        return
    
    # Get events array
    events = data.get("events", [])
    if not events:
        ctx.add_issue("warning", "structure", "No events found in 'events' array", file_path)
        return
    
    # Validate each event
    for event in events:
        # Phase 1: Structure
        if not validate_structure(event, file_path, ctx):
            continue  # Skip further validation if structure is broken
        
        # Phase 2: References
        validate_references(event, file_path, ctx, localization_ids)
        
        # Phase 3: Logic
        validate_logic(event, file_path, ctx)
        
        # Phase 4: Consistency (accumulates data)
        validate_consistency(event, file_path, ctx)


def main():
    """Main validation entry point."""
    parser = argparse.ArgumentParser(description="Validate Enlisted mod content files")
    parser.add_argument("--strict", action="store_true",
                      help="Treat warnings as errors (blocks merge)")
    parser.add_argument("--fix-refs", action="store_true",
                      help="Attempt to fix missing localization references")
    args = parser.parse_args()
    
    print("=" * 80)
    print("ENLISTED MOD - CONTENT VALIDATION TOOL")
    print("=" * 80)
    print()
    
    # Initialize context
    ctx = ValidationContext(strict=args.strict)
    
    # Load localization strings
    print("[Phase 0] Loading localization strings...")
    localization_ids = load_localization_strings()
    
    # Collect files
    event_files = sorted(glob.glob("ModuleData/Enlisted/Events/**/*.json", recursive=True))
    decision_files = sorted(glob.glob("ModuleData/Enlisted/Decisions/**/*.json", recursive=True))
    all_files = event_files + decision_files
    
    if not all_files:
        print("[ERROR] No content files found!")
        return 2
    
    print(f"[Phase 0] Found {len(all_files)} content files ({len(event_files)} events, {len(decision_files)} decisions)")
    print()
    
    # Phase 1-4: Validate each file
    print(f"[Phase 1-4] Validating structure, references, logic, and consistency...")
    for file_path in all_files:
        validate_event_file(file_path, ctx, localization_ids)
    
    # Phase 4: Cross-file consistency checks
    print("[Phase 4] Running cross-file consistency checks...")
    validate_flag_consistency(ctx)
    
    # Print report
    ctx.print_report()
    
    # Determine exit code
    if ctx.has_critical_issues():
        print("❌ VALIDATION FAILED - Critical issues found")
        return 1
    elif ctx.has_warnings():
        print("⚠️  VALIDATION PASSED WITH WARNINGS")
        return 0
    else:
        print("✅ VALIDATION PASSED")
        return 0


if __name__ == "__main__":
    sys.exit(main())

