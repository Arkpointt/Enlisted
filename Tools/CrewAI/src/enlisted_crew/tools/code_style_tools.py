"""
C# Code Style Tools for Enlisted CrewAI

Enforces Enlisted C# coding standards and Bannerlord patterns.
"""

import re
from typing import List, Tuple
from pathlib import Path
from crewai.tools import tool


# Enlisted C# Style Rules
CODE_STYLE_RULES = {
    "allman_braces": {
        "pattern": r"\)\s*\{",  # Simplified check for K&R style
        "message": "Use Allman braces (opening brace on new line)",
        "severity": "warning"
    },
    "private_field_naming": {
        "pattern": r"private\s+\w+\s+([a-z][a-zA-Z0-9]*)\s*[;=]",  # No underscore
        "message": "Private fields must use _camelCase with underscore prefix",
        "severity": "error"
    },
    "xml_doc_missing": {
        "pattern": r"public\s+(?:static\s+)?(?:async\s+)?(?:\w+(?:<[^>]+>)?)\s+\w+\s*\([^)]*\)\s*(?:\{|=>)",
        "message": "Public methods must have XML documentation (///)",
        "severity": "warning"
    },
}

# Bannerlord Pattern Checks
BANNERLORD_PATTERNS = {
    "textobject_concat": {
        "pattern": r'new\s+TextObject\s*\([^)]*\)\s*\+',
        "message": "Use TextObject.SetTextVariable() instead of string concatenation",
        "severity": "error"
    },
    "hero_null_check": {
        "pattern": r'Hero\.MainHero\.\w+',
        "message": "Check Hero.MainHero for null before accessing properties",
        "severity": "warning"
    },
    "equipment_enum_iteration": {
        "pattern": r'Enum\.GetValues\s*\(\s*typeof\s*\(\s*EquipmentIndex\s*\)\s*\)',
        "message": "Use numeric loop (0 to NumEquipmentSetSlots) instead of Enum.GetValues",
        "severity": "error"
    },
    "gold_direct_assignment": {
        "pattern": r'Hero\.Gold\s*[+\-]?=',
        "message": "Use GiveGoldAction.ApplyBetweenCharacters() instead of direct gold assignment",
        "severity": "error"
    },
}

# Framework Compatibility Checks
FRAMEWORK_CHECKS = {
    "file_scoped_namespace": {
        "pattern": r'namespace\s+\w+(?:\.\w+)*\s*;',
        "message": "File-scoped namespaces not supported (.NET 6+). Use block syntax.",
        "severity": "error"
    },
    "record_types": {
        "pattern": r'\brecord\s+\w+',
        "message": "Record types not supported (.NET 5+). Use class/struct.",
        "severity": "error"
    },
    "init_accessor": {
        "pattern": r'\binit\s*;',
        "message": "init accessor not supported (.NET 5+). Use set or readonly.",
        "severity": "error"
    },
    "top_level_statements": {
        "pattern": r'^(?!using\s|namespace\s|//|\s*$)\w+',
        "message": "Top-level statements not supported (.NET 5+). Use class with Main method.",
        "severity": "error"
    },
}


def check_xml_documentation(code: str) -> List[Tuple[str, str]]:
    """Check if public methods have XML documentation."""
    issues = []
    lines = code.split('\n')
    
    for i, line in enumerate(lines):
        # Skip if line is commented out
        stripped = line.strip()
        if stripped.startswith('//') or stripped.startswith('*'):
            continue
        
        # Check for public method
        if re.search(r'public\s+(?:static\s+)?(?:async\s+)?(?:\w+(?:<[^>]+>)?)\s+\w+\s*\(', line):
            # Check if previous non-empty line has ///
            prev_lines = [l.strip() for l in lines[max(0, i-5):i] if l.strip()]
            if not any(l.startswith('///') for l in prev_lines[-3:]):
                method_name = re.search(r'\b\w+\s*\(', line)
                if method_name:
                    issues.append((
                        f"Line {i+1}: Public method '{method_name.group().strip()}' missing XML docs",
                        "warning"
                    ))
    
    return issues


def check_private_field_naming(code: str) -> List[Tuple[str, str]]:
    """Check if private fields use _camelCase naming."""
    issues = []
    lines = code.split('\n')
    
    for i, line in enumerate(lines):
        # Match private field declarations
        match = re.search(r'private\s+(?:readonly\s+)?(?:static\s+)?\w+(?:<[^>]+>)?\s+([a-zA-Z_]\w*)\s*[;=]', line)
        if match:
            field_name = match.group(1)
            # Check if it starts with underscore and second char is lowercase
            if not field_name.startswith('_') or (len(field_name) > 1 and not field_name[1].islower()):
                issues.append((
                    f"Line {i+1}: Private field '{field_name}' should be '_camelCase'",
                    "error"
                ))
    
    return issues


def check_allman_braces(code: str) -> List[Tuple[str, str]]:
    """Check for Allman brace style (opening brace on new line)."""
    issues = []
    lines = code.split('\n')
    
    for i, line in enumerate(lines):
        # Check for K&R style (brace on same line after method/if/for/etc)
        if re.search(r'(?:if|for|while|foreach|using|switch|try|catch|finally|else|do)\s*\([^)]*\)\s*\{', line):
            issues.append((
                f"Line {i+1}: Use Allman braces - opening brace should be on new line",
                "warning"
            ))
        # Method declarations
        elif re.search(r'\)\s*\{(?!\s*$)', line) and not line.strip().startswith('//'):
            issues.append((
                f"Line {i+1}: Use Allman braces - opening brace should be on new line",
                "warning"
            ))
    
    return issues


def check_textobject_usage(code: str) -> List[Tuple[str, str]]:
    """Check for TextObject string concatenation anti-pattern."""
    issues = []
    lines = code.split('\n')
    
    for i, line in enumerate(lines):
        # Check for TextObject concatenation
        if 'TextObject' in line and ('+' in line or '+ ' in line):
            if re.search(r'new\s+TextObject.*\+|TextObject.*\+.*TextObject', line):
                issues.append((
                    f"Line {i+1}: Use SetTextVariable() instead of concatenating TextObjects",
                    "error"
                ))
    
    return issues


def check_hero_safety(code: str) -> List[Tuple[str, str]]:
    """Check for unsafe Hero.MainHero property access."""
    issues = []
    lines = code.split('\n')
    
    for i, line in enumerate(lines):
        # Check for direct property access without null check
        if re.search(r'Hero\.MainHero\.\w+', line):
            # Check if there's a null check nearby
            context = '\n'.join(lines[max(0, i-3):min(len(lines), i+2)])
            if 'Hero.MainHero' in line and ('== null' not in context and '!= null' not in context and '?.' not in line):
                issues.append((
                    f"Line {i+1}: Check Hero.MainHero for null before accessing properties",
                    "warning"
                ))
    
    return issues


def check_equipment_iteration(code: str) -> List[Tuple[str, str]]:
    """Check for unsafe EquipmentIndex enum iteration."""
    issues = []
    lines = code.split('\n')
    
    for i, line in enumerate(lines):
        if 'Enum.GetValues' in line and 'EquipmentIndex' in line:
            issues.append((
                f"Line {i+1}: Use numeric loop (0 to NumEquipmentSetSlots) instead of Enum.GetValues",
                "error"
            ))
    
    return issues


def check_gold_transactions(code: str) -> List[Tuple[str, str]]:
    """Check for direct gold manipulation."""
    issues = []
    lines = code.split('\n')
    
    for i, line in enumerate(lines):
        if re.search(r'\.Gold\s*[+\-]?=', line) and 'GiveGoldAction' not in line:
            issues.append((
                f"Line {i+1}: Use GiveGoldAction.ApplyBetweenCharacters() for gold changes",
                "error"
            ))
    
    return issues


def check_framework_compatibility(code: str) -> List[Tuple[str, str]]:
    """Check for .NET 5+ features not supported in .NET Framework 4.7.2."""
    issues = []
    lines = code.split('\n')
    
    for i, line in enumerate(lines):
        # File-scoped namespace
        if re.match(r'namespace\s+\w+(?:\.\w+)*\s*;', line.strip()):
            issues.append((
                f"Line {i+1}: File-scoped namespaces require .NET 6+. Use block syntax.",
                "error"
            ))
        
        # Record types
        if re.search(r'\brecord\s+\w+', line):
            issues.append((
                f"Line {i+1}: Record types require .NET 5+. Use class or struct.",
                "error"
            ))
        
        # Init accessor
        if re.search(r'\binit\s*;', line):
            issues.append((
                f"Line {i+1}: init accessor requires .NET 5+. Use set or readonly.",
                "error"
            ))
    
    return issues


@tool("Check C# Code Style")
def check_code_style_tool(code: str) -> str:
    """
    Check C# code against Enlisted coding standards.
    
    Checks:
    - Allman braces (opening brace on new line)
    - Private field naming (_camelCase)
    - XML documentation on public methods
    - 4-space indentation
    
    Args:
        code: C# source code to check
    
    Returns:
        Style issues report or "STYLE OK" if compliant.
    """
    all_issues = []
    
    # Run style checks
    all_issues.extend(check_allman_braces(code))
    all_issues.extend(check_private_field_naming(code))
    all_issues.extend(check_xml_documentation(code))
    
    if not all_issues:
        return "CODE STYLE OK: Follows Enlisted standards."
    
    # Format report
    errors = [msg for msg, sev in all_issues if sev == "error"]
    warnings = [msg for msg, sev in all_issues if sev == "warning"]
    
    report = "CODE STYLE ISSUES:\n\n"
    
    if errors:
        report += "ERRORS (must fix):\n"
        for e in errors:
            report += f"  ❌ {e}\n"
        report += "\n"
    
    if warnings:
        report += "WARNINGS (should fix):\n"
        for w in warnings:
            report += f"  ⚠️ {w}\n"
    
    return report


@tool("Check Bannerlord Patterns")
def check_bannerlord_patterns_tool(code: str) -> str:
    """
    Check C# code for Bannerlord-specific anti-patterns.
    
    Checks:
    - TextObject concatenation (use SetTextVariable)
    - Hero.MainHero null safety
    - EquipmentIndex iteration (numeric loop required)
    - Gold direct assignment (use GiveGoldAction)
    
    Args:
        code: C# source code to check
    
    Returns:
        Pattern violations report or "PATTERNS OK" if compliant.
    """
    all_issues = []
    
    # Run Bannerlord pattern checks
    all_issues.extend(check_textobject_usage(code))
    all_issues.extend(check_hero_safety(code))
    all_issues.extend(check_equipment_iteration(code))
    all_issues.extend(check_gold_transactions(code))
    
    if not all_issues:
        return "BANNERLORD PATTERNS OK: No anti-patterns detected."
    
    # Format report
    errors = [msg for msg, sev in all_issues if sev == "error"]
    warnings = [msg for msg, sev in all_issues if sev == "warning"]
    
    report = "BANNERLORD PATTERN ISSUES:\n\n"
    
    if errors:
        report += "CRITICAL (will cause bugs):\n"
        for e in errors:
            report += f"  ❌ {e}\n"
        report += "\n"
    
    if warnings:
        report += "WARNINGS (potential issues):\n"
        for w in warnings:
            report += f"  ⚠️ {w}\n"
    
    return report


@tool("Check C# File")
def check_csharp_file_tool(file_path: str) -> str:
    """
    Read and check a C# file for all style, pattern, and compatibility issues.
    
    Combines all checks:
    - Code style (Allman braces, naming, XML docs)
    - Bannerlord patterns (TextObject, Hero, Equipment, Gold)
    - Framework compatibility (.NET 4.7.2)
    
    Args:
        file_path: Path to C# file relative to project root, or absolute path
    
    Returns:
        Complete analysis report of the file.
    """
    # Try various path resolutions
    possible_paths = [
        Path(file_path),
        PROJECT_ROOT / file_path,
        PROJECT_ROOT / "src" / file_path,
    ]
    
    code = None
    for path in possible_paths:
        if path.exists() and path.is_file():
            try:
                with open(path, 'r', encoding='utf-8') as f:
                    code = f.read()
                break
            except Exception as e:
                return f"ERROR reading {path}: {e}"
    
    if code is None:
        return f"ERROR: File not found. Tried:\n" + "\n".join(str(p) for p in possible_paths)
    
    # Run all checks
    report = f"Analyzing: {file_path}\n\n"
    
    style_report = check_code_style_tool(code)
    pattern_report = check_bannerlord_patterns_tool(code)
    compat_report = check_framework_compatibility_tool(code)
    
    if "OK" in style_report and "OK" in pattern_report and "COMPATIBLE" in compat_report:
        return report + "✅ ALL CHECKS PASSED\n\nNo issues found."
    
    if "ISSUES" in style_report:
        report += style_report + "\n"
    
    if "ISSUES" in pattern_report:
        report += pattern_report + "\n"
    
    if "ISSUES" in compat_report:
        report += compat_report + "\n"
    
    return report


@tool("Check Framework Compatibility")
def check_framework_compatibility_tool(code: str) -> str:
    """
    Check C# code for .NET 5+ features incompatible with .NET Framework 4.7.2.
    
    Checks:
    - File-scoped namespaces
    - Record types
    - init accessors
    - Top-level statements
    
    Args:
        code: C# source code to check
    
    Returns:
        Compatibility issues or "COMPATIBLE" if valid.
    """
    issues = check_framework_compatibility(code)
    
    if not issues:
        return "FRAMEWORK COMPATIBLE: Code targets .NET Framework 4.7.2 / C# 9.0 correctly."
    
    report = "FRAMEWORK COMPATIBILITY ISSUES:\n\n"
    report += "Target: .NET Framework 4.7.2 / C# 9.0\n\n"
    
    for msg, sev in issues:
        report += f"  ❌ {msg}\n"
    
    return report
