"""
Writing Style Tools for Enlisted CrewAI

Implements style checking based on docs/Features/Content/writing-style-guide.md
"""

import os
import re
from pathlib import Path
from typing import List, Tuple
from crewai.tools import tool


def get_project_root() -> Path:
    """Get the Enlisted project root directory."""
    env_root = os.environ.get("ENLISTED_PROJECT_ROOT")
    if env_root:
        return Path(env_root)
    return Path(r"C:\Dev\Enlisted\Enlisted")


# Style rules from writing-style-guide.md
STYLE_RULES = {
    "no_exclamation": {
        "pattern": r"!",
        "message": "Exclamation marks are not allowed in narration. Soldiers don't exclaim.",
        "severity": "error"
    },
    "no_question_in_narration": {
        "pattern": r"\?(?!\")",  # Questions not in dialogue
        "message": "Question marks in narration are discouraged. Save for dialogue.",
        "severity": "warning"
    },
    "modern_terms": {
        "words": ["stressed", "trauma", "PTSD", "facility", "unit", "equipment", 
                  "armament", "leadership", "proceed", "relocate", "consumables",
                  "adverse conditions", "discomfort", "injured"],
        "message": "Modern term detected. Use medieval military register.",
        "severity": "warning"
    },
    "abstract_emotions": {
        "patterns": [
            r"[Yy]ou feel \w+",
            r"[Yy]ou are \w+ing",
            r"[Hh]e('s| is) (angry|sad|happy|scared|worried)",
            r"[Ss]he('s| is) (angry|sad|happy|scared|worried)",
        ],
        "message": "Named emotion detected. Show physical reactions instead.",
        "severity": "warning"
    },
    "third_person": {
        "patterns": [
            r"[Tt]he player ",
            r"[Pp]layer's ",
        ],
        "message": "Third person detected. Use second person (you/your).",
        "severity": "error"
    },
}

# Medieval military vocabulary replacements
VOCABULARY_SUGGESTIONS = {
    "stressed": "weary",
    "facility": "garrison",
    "unit": "company",
    "equipment": "gear/weapons/arms",
    "injured": "wounded",
    "proceed": "march/advance",
    "leadership": "officers/captain",
    "money": "gold/coin/denars",
    "travel": "march/ride",
}


def check_sentence_length(text: str) -> List[Tuple[str, str]]:
    """Check if sentences are too long (max 20 words recommended)."""
    issues = []
    sentences = re.split(r'[.!?]+', text)
    
    for sentence in sentences:
        words = sentence.strip().split()
        if len(words) > 20:
            issues.append((
                f"Long sentence ({len(words)} words): \"{sentence[:50]}...\"",
                "warning"
            ))
    
    return issues


def check_modern_terms(text: str) -> List[Tuple[str, str]]:
    """Check for modern terms that should use medieval equivalents."""
    issues = []
    text_lower = text.lower()
    
    for word in STYLE_RULES["modern_terms"]["words"]:
        if word.lower() in text_lower:
            suggestion = VOCABULARY_SUGGESTIONS.get(word.lower(), "a medieval equivalent")
            issues.append((
                f"Modern term '{word}' found. Consider: {suggestion}",
                "warning"
            ))
    
    return issues


def check_abstract_emotions(text: str) -> List[Tuple[str, str]]:
    """Check for named emotions instead of physical descriptions."""
    issues = []
    
    for pattern in STYLE_RULES["abstract_emotions"]["patterns"]:
        matches = re.findall(pattern, text)
        if matches:
            issues.append((
                f"Abstract emotion detected. Show physical reaction instead.",
                "warning"
            ))
            break  # One warning is enough
    
    return issues


def check_perspective(text: str) -> List[Tuple[str, str]]:
    """Check for third-person perspective (should be second person)."""
    issues = []
    
    for pattern in STYLE_RULES["third_person"]["patterns"]:
        if re.search(pattern, text):
            issues.append((
                "Third person ('the player') detected. Use 'you/your' instead.",
                "error"
            ))
            break
    
    return issues


def check_punctuation(text: str) -> List[Tuple[str, str]]:
    """Check for prohibited punctuation."""
    issues = []
    
    if "!" in text and not re.search(r'"[^"]*![^"]*"', text):  # Allow in dialogue
        issues.append((
            "Exclamation mark in narration. Remove or move to dialogue.",
            "error"
        ))
    
    if ";" in text:
        issues.append((
            "Semicolon detected. Too literary for military prose. Use periods.",
            "warning"
        ))
    
    return issues


@tool("Review Prose")
def review_prose(text: str) -> str:
    """
    Check if text follows the Enlisted writing style guide.
    
    Rules checked:
    - No exclamation marks in narration
    - Second person perspective (you/your)
    - Medieval military vocabulary (no modern terms)
    - Physical descriptions over abstract emotions
    - Sentence length (average 8-12 words, max 20)
    
    Args:
        text: The narrative text to check (setup, option, or result text)
    
    Returns:
        A report listing style issues found, or "STYLE OK" if no issues.
    """
    all_issues = []
    
    # Run all checks
    all_issues.extend(check_sentence_length(text))
    all_issues.extend(check_modern_terms(text))
    all_issues.extend(check_abstract_emotions(text))
    all_issues.extend(check_perspective(text))
    all_issues.extend(check_punctuation(text))
    
    if not all_issues:
        return "STYLE OK: Text follows writing style guide."
    
    # Format report
    errors = [msg for msg, sev in all_issues if sev == "error"]
    warnings = [msg for msg, sev in all_issues if sev == "warning"]
    
    report = "STYLE ISSUES FOUND:\n\n"
    
    if errors:
        report += "ERRORS (must fix):\n"
        for e in errors:
            report += f"  ERROR: {e}\n"
        report += "\n"
    
    if warnings:
        report += "WARNINGS (should fix):\n"
        for w in warnings:
            report += f"  WARNING: {w}\n"
    
    return report


@tool("Review Tooltip")
def review_tooltip(tooltip: str) -> str:
    """
    Check if a tooltip follows Enlisted tooltip guidelines.
    
    Rules:
    - Maximum 80 characters
    - Factual, mechanical description only
    - No narrative text or emotions
    - Format: action + side effects + cooldown
    
    Args:
        tooltip: The tooltip text to check
    
    Returns:
        A report listing issues, or "TOOLTIP OK" if valid.
    """
    issues = []
    
    # Length check
    if len(tooltip) > 80:
        issues.append(f"Too long ({len(tooltip)} chars). Max 80 chars.")
    
    # No narrative markers
    if "!" in tooltip:
        issues.append("No exclamation marks in tooltips.")
    
    # Check for emotion words (tooltips should be mechanical)
    emotion_words = ["feel", "emotion", "happy", "sad", "worried", "excited"]
    for word in emotion_words:
        if word.lower() in tooltip.lower():
            issues.append(f"Emotional word '{word}' in tooltip. Keep mechanical.")
            break
    
    # Check for good patterns
    good_patterns = [
        r"\.", # Ends with period
        r"(Grants|Causes|Costs|Requires|Adds|Removes)", # Action verbs
    ]
    
    has_good_pattern = any(re.search(p, tooltip) for p in good_patterns)
    if not has_good_pattern:
        issues.append("Consider using action verbs: Grants/Causes/Costs/etc.")
    
    if not issues:
        return "TOOLTIP OK: Follows tooltip guidelines."
    
    report = "TOOLTIP ISSUES:\n"
    for issue in issues:
        report += f"  WARNING: {issue}\n"
    
    return report


@tool("Get Style Guide")
def get_style_guide() -> str:
    """
    Read the complete Enlisted writing style guide.
    
    Use this tool to understand:
    - Core voice principles (terse, physical, laconic)
    - Tense and perspective rules (second person, present tense)
    - Vocabulary guidelines (medieval military register)
    - Setup/Option/Result text structure
    - Tooltip formatting rules
    - Common mistakes to avoid
    
    Returns:
        The full writing-style-guide.md content.
    """
    guide_path = get_project_root() / "docs" / "Features" / "Content" / "writing-style-guide.md"
    
    if not guide_path.exists():
        return "ERROR: Writing style guide not found at expected path."
    
    try:
        with open(guide_path, 'r', encoding='utf-8') as f:
            return f.read()
    except Exception as e:
        return f"ERROR reading style guide: {e}"


@tool("Suggest Edits")
def suggest_edits(text: str) -> str:
    """
    Suggest improvements to make text more Enlisted-style.
    
    Provides specific suggestions for:
    - Vocabulary replacements (modern -> medieval)
    - Emotion rewrites (abstract -> physical)
    - Sentence shortening
    
    Args:
        text: The text to improve
    
    Returns:
        Specific suggestions for improvement.
    """
    suggestions = []
    
    # Vocabulary suggestions
    text_lower = text.lower()
    for modern, medieval in VOCABULARY_SUGGESTIONS.items():
        if modern in text_lower:
            suggestions.append(f"Replace '{modern}' -> '{medieval}'")
    
    # Emotion rewrite suggestions
    emotion_patterns = {
        r"[Yy]ou feel tired": "Your legs ache / Your eyes burn",
        r"[Yy]ou feel guilty": "Your stomach turns / The weight feels heavier",
        r"[Yy]ou feel scared": "Your hands won't stop shaking",
        r"[Hh]e('s| is) angry": "His jaw tightens / His fist clenches",
        r"[Ss]he('s| is) grateful": "She nods once, says nothing",
    }
    
    for pattern, suggestion in emotion_patterns.items():
        if re.search(pattern, text):
            suggestions.append(f"Rewrite emotion: '{pattern}' -> '{suggestion}'")
    
    # Sentence length suggestions
    sentences = re.split(r'[.!?]+', text)
    for sentence in sentences:
        words = sentence.strip().split()
        if len(words) > 15:
            suggestions.append(
                f"Shorten: \"{sentence[:40]}...\" ({len(words)} words -> aim for 8-12)"
            )
    
    if not suggestions:
        return "No specific suggestions. Text follows style guide well."
    
    report = "STYLE SUGGESTIONS:\n\n"
    for s in suggestions:
        report += f"- {s}\n"
    
    return report
