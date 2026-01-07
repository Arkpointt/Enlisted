"""
Planning document tools for CrewAI.

Handles creating and versioning planning documents in docs/CrewAI_Plans/
"""
import os
import re
import hashlib
import json
from pathlib import Path
from datetime import datetime
from typing import Dict, List, Any, Optional
from crewai.tools import tool


@tool("Save Plan")
def save_plan(feature_name: str, content: str) -> str:
    """
    Write a planning document to docs/CrewAI_Plans/ with intelligent versioning.
    
    Versioning logic:
    - First time: feature-name.md
    - Updates: Checks if content is substantially different
      - If minor changes (< 30% different): Updates existing file with timestamp note
      - If major changes (> 30% different): Creates feature-name-v2.md, v3.md, etc.
    
    Args:
        feature_name: Kebab-case name like "reputation-morale-integration"
        content: Full markdown content of the planning document
    
    Returns:
        Success message with file path
    """
    try:
        # Find project root
        project_root = Path(os.environ.get("ENLISTED_PROJECT_ROOT", r"C:\Dev\Enlisted\Enlisted"))
        plans_dir = project_root / "docs" / "CrewAI_Plans"
        plans_dir.mkdir(parents=True, exist_ok=True)
        
        # Clean feature name
        clean_name = re.sub(r'[^a-z0-9-]', '-', feature_name.lower())
        base_path = plans_dir / f"{clean_name}.md"
        
        # Check if file exists
        if not base_path.exists():
            # First time - create new file
            _write_file(base_path, content, is_new=True)
            rel_path = base_path.relative_to(project_root)
            return f"OK: Created new planning document:\n   Path: {base_path}\n   Relative: {rel_path}"
        
        # File exists - check similarity
        existing_content = base_path.read_text(encoding='utf-8')
        similarity = _calculate_similarity(existing_content, content)
        
        if similarity > 0.70:  # > 70% similar = minor update
            # Update existing file with revision note
            timestamp = datetime.now().strftime("%Y-%m-%d %H:%M")
            updated_content = _add_revision_note(content, timestamp)
            _write_file(base_path, updated_content, is_new=False)
            rel_path = base_path.relative_to(project_root)
            return f"OK: Updated planning document (minor changes):\n   Path: {base_path}\n   Relative: {rel_path}"
        
        else:  # < 70% similar = major rewrite, create new version
            version = _find_next_version(plans_dir, clean_name)
            versioned_path = plans_dir / f"{clean_name}-v{version}.md"
            _write_file(versioned_path, content, is_new=True)
            rel_path = versioned_path.relative_to(project_root)
            return f"OK: Created new version (major changes):\n   Path: {versioned_path}\n   Relative: {rel_path}\n   Previous version preserved as {base_path.name}"
    
    except Exception as e:
        return f"ERROR: Error writing planning document: {str(e)}"


def _write_file(path: Path, content: str, is_new: bool):
    """Write content to file with UTF-8 encoding."""
    # Ensure content ends with newline
    if not content.endswith('\n'):
        content += '\n'
    
    path.write_text(content, encoding='utf-8')


def _add_revision_note(content: str, timestamp: str) -> str:
    """Add revision timestamp to existing content."""
    # Look for "Last Updated" or similar header
    if "**Last Updated:**" in content or "Last Updated:" in content:
        # Update existing timestamp
        content = re.sub(
            r'\*\*Last Updated:\*\*[^\n]*',
            f'**Last Updated:** {timestamp} (revised)',
            content
        )
    else:
        # Add after first heading or at top
        lines = content.split('\n')
        for i, line in enumerate(lines):
            if line.startswith('# '):
                lines.insert(i + 1, f'\n**Last Updated:** {timestamp} (revised)\n')
                break
        content = '\n'.join(lines)
    
    return content


def _calculate_similarity(text1: str, text2: str) -> float:
    """
    Calculate simple text similarity (0.0 to 1.0).
    Uses word overlap as a basic heuristic.
    """
    # Normalize: lowercase, remove special chars, split into words
    words1 = set(re.findall(r'\w+', text1.lower()))
    words2 = set(re.findall(r'\w+', text2.lower()))
    
    if not words1 or not words2:
        return 0.0
    
    # Jaccard similarity
    intersection = len(words1 & words2)
    union = len(words1 | words2)
    
    return intersection / union if union > 0 else 0.0


@tool("Load Plan")
def load_plan(feature_name: str) -> str:
    """
    Read a planning document from docs/CrewAI_Plans/.
    
    Args:
        feature_name: Kebab-case name like "reputation-morale-integration"
    
    Returns:
        The full content of the planning document, or error if not found.
    """
    try:
        project_root = Path(os.environ.get("ENLISTED_PROJECT_ROOT", r"C:\Dev\Enlisted\Enlisted"))
        plans_dir = project_root / "docs" / "CrewAI_Plans"
        
        # Clean feature name
        clean_name = re.sub(r'[^a-z0-9-]', '-', feature_name.lower())
        doc_path = plans_dir / f"{clean_name}.md"
        
        if not doc_path.exists():
            # Check for versioned files
            versioned = sorted(plans_dir.glob(f"{clean_name}-v*.md"), reverse=True)
            if versioned:
                doc_path = versioned[0]  # Use latest version
            else:
                return f"ERROR: Planning document not found: {clean_name}.md"
        
        content = doc_path.read_text(encoding='utf-8')
        return f"FILE: {doc_path.name}:\n\n{content}"
    
    except Exception as e:
        return f"ERROR: Error reading planning document: {str(e)}"


def _find_next_version(directory: Path, base_name: str) -> int:
    """Find the next available version number (v2, v3, etc.)."""
    existing = list(directory.glob(f"{base_name}-v*.md"))
    
    if not existing:
        return 2  # First versioned file is v2 (v1 is the base file)
    
    # Extract version numbers
    versions = []
    for path in existing:
        match = re.search(r'-v(\d+)\.md$', path.name)
        if match:
            versions.append(int(match.group(1)))
    
    return max(versions) + 1 if versions else 2


@tool("Parse Plan")
def parse_plan(plan_path: str) -> str:
    """
    Extract structured implementation data from a planning document.
    
    Parses the plan to identify:
    - C# files that need to be created/modified
    - Content IDs (events, decisions, orders) that need to be created
    - Methods/classes mentioned
    - Current implementation status
    - Plan version hash (for change detection)
    
    Args:
        plan_path: Path to plan file (relative or absolute)
    
    Returns:
        JSON with structured plan data for implementation tracking
    """
    try:
        project_root = Path(os.environ.get("ENLISTED_PROJECT_ROOT", r"C:\Dev\Enlisted\Enlisted"))
        
        # Resolve path
        if not Path(plan_path).is_absolute():
            full_path = project_root / plan_path
        else:
            full_path = Path(plan_path)
        
        if not full_path.exists():
            return f'ERROR: Plan not found: {plan_path}'
        
        content = full_path.read_text(encoding='utf-8')
        
        # Calculate content hash for version tracking
        content_hash = hashlib.md5(content.encode('utf-8')).hexdigest()[:12]
        
        # Extract structured data
        result = {
            "plan_path": str(full_path),
            "plan_hash": content_hash,
            "csharp_files": _extract_csharp_files(content),
            "content_ids": _extract_content_ids(content),
            "methods": _extract_methods(content),
            "status": _extract_status(content),
            "phases": _extract_phases(content),
        }
        
        return json.dumps(result, indent=2)
    
    except Exception as e:
        return f'ERROR: Failed to parse plan: {str(e)}'


def _extract_csharp_files(content: str) -> List[Dict[str, Any]]:
    """Extract C# file paths from plan content."""
    files = []
    
    # Pattern: src/.../*.cs or full Windows paths
    cs_pattern = r'(?:src/[\w/]+\.cs|[A-Z]:\\[\w\\]+\.cs)'
    
    for match in re.finditer(cs_pattern, content):
        path = match.group(0)
        # Determine status from surrounding context
        context_start = max(0, match.start() - 100)
        context = content[context_start:match.end() + 50].lower()
        
        status = "unknown"
        if any(marker in context for marker in ["completed", "implemented", "done"]):
            status = "complete"
        elif any(marker in context for marker in ["pending", "todo", "needed", "remaining"]):
            status = "pending"
        elif any(marker in context for marker in ["blocked", "failed"]):
            status = "blocked"
        
        files.append({"path": path, "status": status})
    
    # Deduplicate
    seen = set()
    unique_files = []
    for f in files:
        if f["path"] not in seen:
            seen.add(f["path"])
            unique_files.append(f)
    
    return unique_files


def _extract_content_ids(content: str) -> List[Dict[str, Any]]:
    """Extract content IDs (events, decisions, orders) from plan."""
    ids = []
    
    # Patterns for different content types
    patterns = [
        (r'\b(supply_pressure_[\w_]+)', 'event'),
        (r'\b(morale_pressure_[\w_]+)', 'event'),
        (r'\b(rest_pressure_[\w_]+)', 'event'),
        (r'\b(morale_high_[\w_]+)', 'event'),
        (r'\b(supply_high_[\w_]+)', 'event'),
        (r'\b(supply_crisis[\w_]*)', 'event'),
        (r'\b(morale_crisis[\w_]*)', 'event'),
        (r'\b(evt_[\w_]+)', 'event'),
        (r'\b(opp_[\w_]+)', 'decision'),
        (r'\b(order_[\w_]+)', 'order'),
    ]
    
    for pattern, content_type in patterns:
        for match in re.finditer(pattern, content, re.IGNORECASE):
            content_id = match.group(1).lower()
            
            # Check surrounding context for status
            context_start = max(0, match.start() - 100)
            context = content[context_start:match.end() + 50].lower()
            
            status = "unknown"
            if any(marker in context for marker in ["completed", "implemented", "done"]):
                status = "complete"
            elif any(marker in context for marker in ["pending", "todo", "needed", "remaining"]):
                status = "pending"
            
            ids.append({
                "id": content_id,
                "type": content_type,
                "status": status
            })
    
    # Deduplicate
    seen = set()
    unique_ids = []
    for item in ids:
        if item["id"] not in seen:
            seen.add(item["id"])
            unique_ids.append(item)
    
    return unique_ids


def _extract_methods(content: str) -> List[str]:
    """Extract method names mentioned in plan."""
    methods = []
    
    # Pattern: MethodName() or ClassName.MethodName()
    method_pattern = r'\b([A-Z][a-zA-Z0-9]+(?:\.[A-Z][a-zA-Z0-9]+)?)\(\)'
    
    for match in re.finditer(method_pattern, content):
        method = match.group(1)
        if method not in methods:
            methods.append(method)
    
    return methods


def _extract_status(content: str) -> Dict[str, Any]:
    """Extract overall plan status."""
    status = {
        "overall": "unknown",
        "phases_complete": 0,
        "phases_total": 0,
    }
    
    # Check status line
    status_match = re.search(r'\*\*Status:\*\*\s*([^\n]+)', content)
    if status_match:
        status_text = status_match.group(1).lower()
        if "complete" in status_text or "implemented" in status_text:
            status["overall"] = "complete"
        elif "partial" in status_text or "in progress" in status_text:
            status["overall"] = "partial"
        elif "planning" in status_text:
            status["overall"] = "planning"
    
    # Count completed checkboxes
    completed = len(re.findall(r'\[x\]', content, re.IGNORECASE))
    total = len(re.findall(r'\[[ x]\]', content, re.IGNORECASE))
    status["phases_complete"] = completed
    status["phases_total"] = total
    
    return status


def _extract_phases(content: str) -> List[Dict[str, Any]]:
    """Extract implementation phases from plan."""
    phases = []
    
    # Look for phase headers: ## Phase N: or ### Phase N:
    phase_pattern = r'(?:##?#?)\s*(?:Phase\s*(\d+)|Goal\s*(\d+))[:.]?\s*([^\n]+)'
    
    for match in re.finditer(phase_pattern, content, re.IGNORECASE):
        phase_num = match.group(1) or match.group(2)
        phase_title = match.group(3).strip()
        
        # Check if completed based on surrounding content
        context_end = min(len(content), match.end() + 500)
        context = content[match.start():context_end].lower()
        
        status = "pending"
        if "completed" in context[:200]:
            status = "complete"
        
        phases.append({
            "number": int(phase_num) if phase_num else len(phases) + 1,
            "title": phase_title,
            "status": status
        })
    
    return phases


@tool("Get Plan Hash")
def get_plan_hash(plan_path: str) -> str:
    """
    Get the content hash of a plan for change detection.
    
    Use this to check if a plan has changed since you started implementing.
    Compare the hash before and after implementation to detect mid-work changes.
    
    Args:
        plan_path: Path to plan file
    
    Returns:
        12-character MD5 hash of plan content, or error
    """
    try:
        project_root = Path(os.environ.get("ENLISTED_PROJECT_ROOT", r"C:\Dev\Enlisted\Enlisted"))
        
        if not Path(plan_path).is_absolute():
            full_path = project_root / plan_path
        else:
            full_path = Path(plan_path)
        
        if not full_path.exists():
            return f'ERROR: Plan not found: {plan_path}'
        
        content = full_path.read_text(encoding='utf-8')
        content_hash = hashlib.md5(content.encode('utf-8')).hexdigest()[:12]
        
        return f'HASH: {content_hash} for {full_path.name}'
    
    except Exception as e:
        return f'ERROR: {str(e)}'
