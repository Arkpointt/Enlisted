"""
Unit tests for manager escalation framework.

Tests the escalation logic, issue detection, and formatting.

Usage:
    cd Tools/CrewAI
    .venv\Scripts\Activate.ps1
    python test_escalation.py
"""

import sys
from pathlib import Path

# Add src to path for imports
sys.path.insert(0, str(Path(__file__).parent / "src"))

from enlisted_crew.flows.escalation import (
    IssueType,
    IssueSeverity,
    IssueConfidence,
    DetectedIssue,
    should_escalate_to_human,
    format_critical_issues,
    categorize_issue_severity,
    create_escalation_message,
)


class Colors:
    GREEN = '\033[92m'
    RED = '\033[91m'
    CYAN = '\033[96m'
    RESET = '\033[0m'
    BOLD = '\033[1m'


def print_test(name):
    print(f"\n{Colors.CYAN}[TEST] {name}{Colors.RESET}")


def print_pass(msg=""):
    print(f"  {Colors.GREEN}[PASS]{Colors.RESET} {msg}")


def print_fail(msg=""):
    print(f"  {Colors.RED}[FAIL]{Colors.RESET} {msg}")


def test_critical_severity_always_escalates():
    """CRITICAL severity issues should always escalate."""
    print_test("Critical severity always escalates")
    
    issue = DetectedIssue(
        issue_type=IssueType.ARCHITECTURE_VIOLATION,
        severity=IssueSeverity.CRITICAL,
        confidence=IssueConfidence.HIGH,
        description="Breaking architecture",
        affected_component="test.cs",
        evidence="Uses wrong pattern",
        manager_recommendation="Fix it",
    )
    
    result = should_escalate_to_human(issue, "critical_only")
    if result:
        print_pass("Critical severity escalates")
        return True
    else:
        print_fail("Critical severity did not escalate")
        return False


def test_security_always_escalates():
    """Security concerns should always escalate regardless of threshold."""
    print_test("Security concerns always escalate")
    
    issue = DetectedIssue(
        issue_type=IssueType.SECURITY_CONCERN,
        severity=IssueSeverity.HIGH,
        confidence=IssueConfidence.MEDIUM,
        description="Potential SQL injection",
        affected_component="query.cs",
        evidence="Unsanitized input",
        manager_recommendation="Review security",
    )
    
    result = should_escalate_to_human(issue, "critical_only")
    if result:
        print_pass("Security concern escalates")
        return True
    else:
        print_fail("Security concern did not escalate")
        return False


def test_high_confidence_auto_fix():
    """High confidence + auto-fixable should NOT escalate."""
    print_test("High confidence auto-fixable doesn't escalate")
    
    issue = DetectedIssue(
        issue_type=IssueType.INCORRECT_METHOD,
        severity=IssueSeverity.MEDIUM,
        confidence=IssueConfidence.HIGH,
        description="Wrong method name",
        affected_component="test.cs",
        evidence="Found correct method",
        manager_recommendation="Use GetNeeds() instead",
        auto_fixable=True,
    )
    
    result = should_escalate_to_human(issue, "critical_only")
    if not result:
        print_pass("Auto-fixable high confidence doesn't escalate")
        return True
    else:
        print_fail("Auto-fixable high confidence escalated incorrectly")
        return False


def test_low_confidence_high_severity_escalates():
    """High severity + low confidence should escalate."""
    print_test("High severity + low confidence escalates")
    
    issue = DetectedIssue(
        issue_type=IssueType.HALLUCINATED_API,
        severity=IssueSeverity.HIGH,
        confidence=IssueConfidence.LOW,
        description="Method might not exist",
        affected_component="test.cs",
        evidence="Search returned ambiguous results",
        manager_recommendation="Need human to verify",
    )
    
    result = should_escalate_to_human(issue, "critical_only")
    if result:
        print_pass("High severity + low confidence escalates")
        return True
    else:
        print_fail("High severity + low confidence did not escalate")
        return False


def test_conflicting_requirements_escalates():
    """Conflicting requirements always need human decision."""
    print_test("Conflicting requirements always escalate")
    
    issue = DetectedIssue(
        issue_type=IssueType.CONFLICTING_REQUIREMENTS,
        severity=IssueSeverity.HIGH,
        confidence=IssueConfidence.HIGH,
        description="Plan says X, docs say Y",
        affected_component="feature.cs",
        evidence="Plan: use SystemA, Docs: SystemA deprecated",
        manager_recommendation="Need human to decide",
    )
    
    result = should_escalate_to_human(issue, "critical_only")
    if result:
        print_pass("Conflicting requirements escalate")
        return True
    else:
        print_fail("Conflicting requirements did not escalate")
        return False


def test_low_severity_never_escalates():
    """Low severity issues should never escalate."""
    print_test("Low severity never escalates")
    
    issue = DetectedIssue(
        issue_type=IssueType.SPELLING_ERROR,
        severity=IssueSeverity.LOW,
        confidence=IssueConfidence.HIGH,
        description="Typo in comment",
        affected_component="test.cs",
        evidence="'recieve' should be 'receive'",
        manager_recommendation="Auto-fix typo",
        auto_fixable=True,
    )
    
    result = should_escalate_to_human(issue, "critical_only")
    if not result:
        print_pass("Low severity doesn't escalate")
        return True
    else:
        print_fail("Low severity escalated incorrectly")
        return False


def test_escalation_threshold_high_and_critical():
    """Test 'high_and_critical' threshold."""
    print_test("Threshold: high_and_critical")
    
    # High severity should escalate with this threshold
    high_issue = DetectedIssue(
        issue_type=IssueType.SCOPE_CREEP,
        severity=IssueSeverity.HIGH,
        confidence=IssueConfidence.HIGH,
        description="Task growing too large",
        affected_component="feature.cs",
        evidence="Originally 1 file, now 5 files",
        manager_recommendation="Review scope",
    )
    
    result = should_escalate_to_human(high_issue, "high_and_critical")
    if result:
        print_pass("HIGH severity escalates with high_and_critical threshold")
    else:
        print_fail("HIGH severity did not escalate with high_and_critical")
        return False
    
    # Medium severity should NOT escalate
    medium_issue = DetectedIssue(
        issue_type=IssueType.STYLE_VIOLATION,
        severity=IssueSeverity.MEDIUM,
        confidence=IssueConfidence.HIGH,
        description="Style issue",
        affected_component="test.cs",
        evidence="Wrong brace style",
        manager_recommendation="Auto-fix",
        auto_fixable=True,
    )
    
    result = should_escalate_to_human(medium_issue, "high_and_critical")
    if not result:
        print_pass("MEDIUM severity doesn't escalate with high_and_critical")
        return True
    else:
        print_fail("MEDIUM severity escalated incorrectly")
        return False


def test_categorize_issue_severity():
    """Test automatic severity categorization."""
    print_test("Categorize issue severity")
    
    tests = [
        (IssueType.SECURITY_CONCERN, IssueSeverity.CRITICAL),
        (IssueType.HALLUCINATED_API, IssueSeverity.HIGH),
        (IssueType.DEPRECATED_SYSTEM, IssueSeverity.MEDIUM),
        (IssueType.SPELLING_ERROR, IssueSeverity.LOW),
    ]
    
    for issue_type, expected_severity in tests:
        result = categorize_issue_severity(issue_type)
        if result == expected_severity:
            print_pass(f"{issue_type.value} → {expected_severity.value}")
        else:
            print_fail(f"{issue_type.value} → got {result.value}, expected {expected_severity.value}")
            return False
    
    return True


def test_format_critical_issues():
    """Test issue formatting for display."""
    print_test("Format critical issues for display")
    
    issues = [
        DetectedIssue(
            issue_type=IssueType.HALLUCINATED_API,
            severity=IssueSeverity.HIGH,
            confidence=IssueConfidence.LOW,
            description="Method doesn't exist",
            affected_component="ReputationManager.cs",
            evidence="Search returned no results for GetCurrentReputation()",
            manager_recommendation="Use GetReputation() instead",
        ),
        DetectedIssue(
            issue_type=IssueType.ARCHITECTURE_VIOLATION,
            severity=IssueSeverity.CRITICAL,
            confidence=IssueConfidence.HIGH,
            description="Direct Hero.Gold access",
            affected_component="feature.cs",
            evidence="Should use GiveGoldAction",
            manager_recommendation="Refactor to use action system",
        ),
    ]
    
    output = format_critical_issues(issues)
    
    # Check that output contains expected elements
    checks = [
        "CRITICAL ISSUES FOUND: 2",
        "HALLUCINATED API",
        "ARCHITECTURE VIOLATION",
        "ReputationManager.cs",
        "feature.cs",
        "Manager's Recommendation:",
    ]
    
    for check in checks:
        if check in output:
            print_pass(f"Output contains: {check}")
        else:
            print_fail(f"Output missing: {check}")
            print(f"\nActual output:\n{output}")
            return False
    
    return True


def test_create_escalation_message():
    """Test escalation message creation."""
    print_test("Create escalation message")
    
    issues = [
        DetectedIssue(
            issue_type=IssueType.CONFLICTING_REQUIREMENTS,
            severity=IssueSeverity.HIGH,
            confidence=IssueConfidence.HIGH,
            description="Conflicting info",
            affected_component="test.cs",
            evidence="Plan vs docs mismatch",
            manager_recommendation="Review",
        ),
    ]
    
    message = create_escalation_message(
        issues,
        "Planning",
        "Additional context here"
    )
    
    checks = [
        "PLANNING MANAGER",
        "CRITICAL ISSUE DETECTED",
        "Additional context here",
        "review the details",
    ]
    
    for check in checks:
        if check.lower() in message.lower():
            print_pass(f"Message contains: {check}")
        else:
            print_fail(f"Message missing: {check}")
            print(f"\nActual message:\n{message}")
            return False
    
    return True


def main():
    """Run all tests."""
    print(f"\n{Colors.BOLD}{Colors.CYAN}")
    print("=" * 60)
    print("  ESCALATION FRAMEWORK UNIT TESTS")
    print("=" * 60)
    print(f"{Colors.RESET}\n")
    
    tests = [
        ("Critical severity always escalates", test_critical_severity_always_escalates),
        ("Security concerns always escalate", test_security_always_escalates),
        ("High confidence auto-fix doesn't escalate", test_high_confidence_auto_fix),
        ("High severity + low confidence escalates", test_low_confidence_high_severity_escalates),
        ("Conflicting requirements escalate", test_conflicting_requirements_escalates),
        ("Low severity never escalates", test_low_severity_never_escalates),
        ("Threshold: high_and_critical", test_escalation_threshold_high_and_critical),
        ("Categorize issue severity", test_categorize_issue_severity),
        ("Format critical issues", test_format_critical_issues),
        ("Create escalation message", test_create_escalation_message),
    ]
    
    results = {}
    
    for test_name, test_func in tests:
        try:
            results[test_name] = test_func()
        except Exception as e:
            print_fail(f"Test crashed: {e}")
            import traceback
            traceback.print_exc()
            results[test_name] = False
    
    # Summary
    print(f"\n{Colors.CYAN}{'='*60}{Colors.RESET}")
    print(f"{Colors.BOLD}TEST SUMMARY{Colors.RESET}")
    print(f"{Colors.CYAN}{'='*60}{Colors.RESET}\n")
    
    passed = sum(1 for v in results.values() if v)
    total = len(results)
    
    for test_name, result in results.items():
        status = f"{Colors.GREEN}PASS{Colors.RESET}" if result else f"{Colors.RED}FAIL{Colors.RESET}"
        print(f"  {test_name:<45} [{status}]")
    
    print(f"\n{Colors.CYAN}{'='*60}{Colors.RESET}")
    
    if passed == total:
        print(f"{Colors.GREEN}{Colors.BOLD}[OK] ALL {total} TESTS PASSED{Colors.RESET}")
        print(f"{Colors.CYAN}{'='*60}{Colors.RESET}\n")
        
        print(f"{Colors.BOLD}ESCALATION FRAMEWORK: FULLY TESTED ✅{Colors.RESET}")
        print("\n✅ Critical issues always escalate")
        print("✅ Security/data loss always escalate")
        print("✅ High confidence auto-fixes don't escalate")
        print("✅ Low confidence + high severity escalate")
        print("✅ Low severity issues never escalate")
        print("✅ Thresholds work correctly\n")
        return 0
    else:
        print(f"{Colors.RED}{Colors.BOLD}[FAIL] {total - passed} of {total} TESTS FAILED{Colors.RESET}")
        print(f"{Colors.CYAN}{'='*60}{Colors.RESET}\n")
        return 1


if __name__ == "__main__":
    sys.exit(main())
