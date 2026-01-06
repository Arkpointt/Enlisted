"""
Schema Tools for Enlisted CrewAI

JSON schema validation and event creation based on event-system-schemas.md
"""

import json
import os
from pathlib import Path
from typing import Dict, Any, List, Optional
from crewai.tools import tool


def get_project_root() -> Path:
    """Get the Enlisted project root directory."""
    env_root = os.environ.get("ENLISTED_PROJECT_ROOT")
    if env_root:
        return Path(env_root)
    
    current = Path(__file__).resolve()
    for parent in current.parents:
        if (parent / "Enlisted.csproj").exists():
            return parent
    
    return Path(r"C:\Dev\Enlisted\Enlisted")


PROJECT_ROOT = get_project_root()

# Valid values from event-system-schemas.md
VALID_SKILLS = {
    "OneHanded", "TwoHanded", "Polearm", "Bow", "Crossbow", "Throwing",
    "Riding", "Athletics", "Crafting", "Scouting", "Tactics", "Roguery",
    "Charm", "Leadership", "Trade", "Stewardship", "Medicine", "Engineering",
    "Perception"
}

VALID_ROLES = {"Any", "Scout", "Medic", "Engineer", "Officer", "Operative", "NCO", "Soldier"}

VALID_CONTEXTS = {"Any", "War", "Peace", "Siege", "Battle", "Town", "Village", "Camp", "March"}

VALID_CATEGORIES = {
    "decision", "escalation", "role", "universal", "muster", "crisis", "general",
    "onboarding", "pay", "promotion", "retinue", "training", "threshold",
    "medical", "map_incident"
}

VALID_SEVERITIES = {"normal", "attention", "critical", "urgent", "positive"}


def validate_event_structure(event: Dict[str, Any]) -> List[str]:
    """Validate an event's structure against the schema."""
    issues = []
    
    # Required fields
    if "id" not in event:
        issues.append("Missing required field: id")
    
    if "category" not in event:
        issues.append("Missing required field: category")
    elif event["category"] not in VALID_CATEGORIES:
        issues.append(f"Invalid category: {event['category']}. Valid: {VALID_CATEGORIES}")
    
    # Severity validation (if present)
    if "severity" in event and event["severity"] not in VALID_SEVERITIES:
        issues.append(f"Invalid severity: {event['severity']}. Valid: {VALID_SEVERITIES}")
    
    # Title/setup - need either ID or fallback (both preferred)
    if "titleId" not in event and "title" not in event:
        issues.append("Missing title: need titleId or title (both preferred)")
    
    if "setupId" not in event and "setup" not in event:
        issues.append("Missing setup: need setupId or setup (both preferred)")
    
    # Field ordering: fallback must immediately follow ID
    fields = list(event.keys())
    if "titleId" in event and "title" in event:
        title_id_idx = fields.index("titleId")
        title_idx = fields.index("title")
        if title_idx != title_id_idx + 1:
            issues.append("JSON field order: 'title' must immediately follow 'titleId'")
    
    if "setupId" in event and "setup" in event:
        setup_id_idx = fields.index("setupId")
        setup_idx = fields.index("setup")
        if setup_idx != setup_id_idx + 1:
            issues.append("JSON field order: 'setup' must immediately follow 'setupId'")
    
    # Options validation
    if "options" in event:
        options = event["options"]
        if len(options) == 1:
            issues.append("CRITICAL: Cannot have exactly 1 option. Use 0 (dynamic) or 2-6.")
        elif len(options) > 6:
            issues.append(f"Too many options ({len(options)}). Max is 6 (rarely 5-6).")
        
        for i, opt in enumerate(options):
            # Tooltip is required
            if "tooltip" not in opt or not opt.get("tooltip"):
                issues.append(f"Option {i+1} missing tooltip (required, <80 chars)")
            elif len(opt.get("tooltip", "")) > 80:
                issues.append(f"Option {i+1} tooltip too long ({len(opt['tooltip'])} chars, max 80)")
            
            # Order events must have skillXp
            if event.get("category") == "order" or event.get("id", "").startswith("ord_"):
                effects = opt.get("effects", {})
                if "skillXp" not in effects:
                    issues.append(f"Option {i+1}: Order events MUST include effects.skillXp")
    
    # Requirements validation
    if "requirements" in event:
        reqs = event["requirements"]
        
        # Tier validation
        if "tier" in reqs:
            tier = reqs["tier"]
            if isinstance(tier, dict):
                min_tier = tier.get("min", 1)
                max_tier = tier.get("max", 9)
                if min_tier < 1 or max_tier > 9:
                    issues.append(f"Invalid tier range: {min_tier}-{max_tier}. Valid: 1-9")
        
        # Role validation
        if "role" in reqs:
            role = reqs["role"]
            if isinstance(role, str) and role not in VALID_ROLES:
                issues.append(f"Invalid role: {role}. Valid: {VALID_ROLES}")
        
        # Context validation
        if "context" in reqs:
            context = reqs["context"]
            if isinstance(context, str) and context not in VALID_CONTEXTS:
                issues.append(f"Invalid context: {context}. Valid: {VALID_CONTEXTS}")
    
    # Effects validation
    for opt in event.get("options", []):
        effects = opt.get("effects", {})
        
        # Skill XP validation
        if "skillXp" in effects:
            for skill, xp in effects["skillXp"].items():
                if skill not in VALID_SKILLS:
                    issues.append(f"Unknown skill in effects.skillXp: {skill}")
        
        # Skill check validation
        if "skill_check" in opt:
            check = opt["skill_check"]
            if "skill" in check and check["skill"] not in VALID_SKILLS:
                issues.append(f"Unknown skill in skill_check: {check['skill']}")
    
    return issues


@tool("Check Event Format")
def check_event_format(event_json: str) -> str:
    """
    Validate a JSON event against the Enlisted schema.
    
    Checks:
    - Required fields (id, category, title, setup)
    - Field ordering (fallback after ID)
    - Option count (0 or 2-6, never 1)
    - Tooltip presence and length (<80 chars)
    - Order events have skillXp
    - Valid skills, roles, contexts
    
    Args:
        event_json: JSON string of the event to validate
    
    Returns:
        Validation report with any issues found.
    """
    try:
        event = json.loads(event_json)
    except json.JSONDecodeError as e:
        return f"INVALID JSON: {e}"
    
    issues = validate_event_structure(event)
    
    if not issues:
        return "SCHEMA VALID: Event follows all schema rules."
    
    report = f"SCHEMA ISSUES ({len(issues)}):\n\n"
    for issue in issues:
        severity = "❌" if "CRITICAL" in issue or "Missing required" in issue else "⚠️"
        report += f"  {severity} {issue}\n"
    
    return report


@tool("Draft Event")
def draft_event(
    event_id: str,
    category: str,
    title: str,
    setup: str,
    options: List[Dict[str, Any]],
    requirements: Optional[Dict[str, Any]] = None,
    severity: str = "normal"
) -> str:
    """
    Create a new JSON event following the Enlisted schema.
    
    Args:
        event_id: Unique ID (e.g., "dec_gambling", "evt_camp_rumor")
        category: Event category (decision, escalation, role, etc.)
        title: Event title text
        setup: Event setup/description text
        options: List of option dicts with text, tooltip, and effects
        requirements: Optional requirements dict (tier, role, context)
        severity: Event severity (normal, attention, critical, urgent, positive)
    
    Returns:
        Valid JSON string for the event, or error message.
    """
    # Generate string IDs from event_id
    title_id = f"{event_id}_title"
    setup_id = f"{event_id}_setup"
    
    # Build event structure with correct field ordering
    event = {
        "id": event_id,
        "category": category,
        "severity": severity,
        "titleId": title_id,
        "title": title,  # Immediately after titleId
        "setupId": setup_id,
        "setup": setup,  # Immediately after setupId
    }
    
    # Add requirements if provided
    if requirements:
        event["requirements"] = requirements
    else:
        event["requirements"] = {"tier": {"min": 1, "max": 9}}
    
    # Process options with correct field ordering
    processed_options = []
    for i, opt in enumerate(options):
        text_id = f"{event_id}_opt{i+1}_text"
        result_id = f"{event_id}_opt{i+1}_result"
        
        processed_opt = {
            "id": opt.get("id", f"opt{i+1}"),
            "textId": text_id,
            "text": opt.get("text", f"Option {i+1}"),  # Immediately after textId
            "tooltip": opt.get("tooltip", ""),
        }
        
        # Add result if provided
        if "result" in opt:
            processed_opt["resultId"] = result_id
            processed_opt["result"] = opt["result"]
        
        # Add effects
        if "effects" in opt:
            processed_opt["effects"] = opt["effects"]
        
        processed_options.append(processed_opt)
    
    event["options"] = processed_options
    
    # Validate before returning
    issues = validate_event_structure(event)
    
    if issues:
        warning = "# WARNING: Event has validation issues:\n"
        for issue in issues:
            warning += f"#   - {issue}\n"
        warning += "\n"
        return warning + json.dumps(event, indent=2)
    
    return json.dumps(event, indent=2)


@tool("Read Event")
def read_event(file_path: str) -> str:
    """
    Read a JSON event file from ModuleData/Enlisted/.
    
    Args:
        file_path: Path relative to project root, or filename in Events/Decisions folder
    
    Returns:
        File contents or error message.
    """
    # Try various path resolutions
    possible_paths = [
        PROJECT_ROOT / file_path,
        PROJECT_ROOT / "ModuleData" / "Enlisted" / file_path,
        PROJECT_ROOT / "ModuleData" / "Enlisted" / "Events" / file_path,
        PROJECT_ROOT / "ModuleData" / "Enlisted" / "Decisions" / file_path,
    ]
    
    for path in possible_paths:
        if path.exists():
            try:
                with open(path, encoding="utf-8-sig") as f:
                    return f.read()
            except Exception as e:
                return f"ERROR reading {path}: {e}"
    
    return f"ERROR: File not found. Tried:\n" + "\n".join(str(p) for p in possible_paths)


@tool("List Events")
def list_events() -> str:
    """
    List all JSON event and decision files in ModuleData/Enlisted/.
    
    Returns:
        List of event files with their categories.
    """
    events_dir = PROJECT_ROOT / "ModuleData" / "Enlisted" / "Events"
    decisions_dir = PROJECT_ROOT / "ModuleData" / "Enlisted" / "Decisions"
    
    files = []
    
    if events_dir.exists():
        for f in events_dir.glob("*.json"):
            files.append(f"Events/{f.name}")
    
    if decisions_dir.exists():
        for f in decisions_dir.glob("*.json"):
            files.append(f"Decisions/{f.name}")
    
    if not files:
        return "No event files found in ModuleData/Enlisted/"
    
    return "Event files:\n" + "\n".join(f"  • {f}" for f in sorted(files))
