"""
Shared escalation logic for manager agents across all flows.

Managers use this to determine when to escalate to human vs auto-fix.
"""

from enum import Enum
from typing import List, Dict, Any
from dataclasses import dataclass


class IssueType(Enum):
    """Types of issues managers can detect."""
    # CRITICAL - Always escalate
    ARCHITECTURE_VIOLATION = "architecture_violation"
    SECURITY_CONCERN = "security_concern"
    DATA_LOSS_RISK = "data_loss_risk"
    BREAKING_CHANGE = "breaking_change"
    CONFLICTING_REQUIREMENTS = "conflicting_requirements"
    
    # HIGH - Escalate if low confidence
    HALLUCINATED_API = "hallucinated_api"
    HALLUCINATED_FILE = "hallucinated_file"
    DEPRECATED_SYSTEM = "deprecated_system"
    SCOPE_CREEP = "scope_creep"
    MISSING_DEPENDENCY = "missing_dependency"
    
    # MEDIUM - Usually auto-fix
    INCORRECT_METHOD = "incorrect_method"
    WRONG_NAMESPACE = "wrong_namespace"
    STYLE_VIOLATION = "style_violation"
    
    # LOW - Always auto-fix
    MISSING_DOCUMENTATION = "missing_documentation"
    SPELLING_ERROR = "spelling_error"
    FORMATTING_ISSUE = "formatting_issue"


class IssueSeverity(Enum):
    """Severity levels for detected issues."""
    CRITICAL = "critical"
    HIGH = "high"
    MEDIUM = "medium"
    LOW = "low"


class IssueConfidence(Enum):
    """Manager's confidence in issue detection."""
    HIGH = "high"      # Manager is certain
    MEDIUM = "medium"  # Manager is fairly sure
    LOW = "low"        # Manager is unsure


@dataclass
class DetectedIssue:
    """An issue detected by the manager."""
    issue_type: IssueType
    severity: IssueSeverity
    confidence: IssueConfidence
    description: str
    affected_component: str
    evidence: str
    manager_recommendation: str
    auto_fixable: bool = False


def should_escalate_to_human(
    issue: DetectedIssue,
    escalation_threshold: str = "critical_only"
) -> bool:
    """
    Determine if an issue requires human review.
    
    Args:
        issue: The detected issue
        escalation_threshold: "critical_only" | "high_and_critical" | "all"
    
    Returns:
        True if human review required, False if manager can handle
    """
    # CRITICAL severity issues always escalate
    if issue.severity == IssueSeverity.CRITICAL:
        return True
    
    # Security and data loss always escalate regardless of threshold
    if issue.issue_type in [
        IssueType.SECURITY_CONCERN,
        IssueType.DATA_LOSS_RISK,
        IssueType.BREAKING_CHANGE,
    ]:
        return True
    
    # High severity with low confidence escalates
    if issue.severity == IssueSeverity.HIGH and issue.confidence == IssueConfidence.LOW:
        return True
    
    # Conflicting requirements always need human decision
    if issue.issue_type == IssueType.CONFLICTING_REQUIREMENTS:
        return True
    
    # Architecture violations need human approval
    if issue.issue_type == IssueType.ARCHITECTURE_VIOLATION:
        return True
    
    # Check threshold setting
    if escalation_threshold == "high_and_critical":
        if issue.severity in [IssueSeverity.CRITICAL, IssueSeverity.HIGH]:
            return True
    elif escalation_threshold == "all":
        return True
    
    # If auto-fixable and confidence is high, don't escalate
    if issue.auto_fixable and issue.confidence == IssueConfidence.HIGH:
        return False
    
    # Default: don't escalate
    return False


def format_critical_issues(issues: List[DetectedIssue]) -> str:
    """
    Format critical issues for human review display.
    
    Returns formatted string suitable for terminal display.
    """
    if not issues:
        return "No critical issues detected."
    
    output = []
    output.append(f"CRITICAL ISSUES FOUND: {len(issues)}\n")
    
    for i, issue in enumerate(issues, 1):
        output.append(f"\n{'='*60}")
        output.append(f"ISSUE #{i}: {issue.issue_type.value.upper().replace('_', ' ')}")
        output.append(f"{'='*60}")
        output.append(f"Severity: {issue.severity.value.upper()}")
        output.append(f"Confidence: {issue.confidence.value.upper()}")
        output.append(f"Component: {issue.affected_component}")
        output.append(f"\nDescription:")
        output.append(f"  {issue.description}")
        output.append(f"\nEvidence:")
        output.append(f"  {issue.evidence}")
        output.append(f"\nManager's Recommendation:")
        output.append(f"  {issue.manager_recommendation}")
        
        if issue.auto_fixable:
            output.append(f"\nAuto-fix Available: Yes")
        else:
            output.append(f"\nAuto-fix Available: No - requires human decision")
    
    return "\n".join(output)


def categorize_issue_severity(issue_type: IssueType) -> IssueSeverity:
    """
    Determine default severity for an issue type.
    
    Manager can override this based on context.
    """
    critical_types = [
        IssueType.ARCHITECTURE_VIOLATION,
        IssueType.SECURITY_CONCERN,
        IssueType.DATA_LOSS_RISK,
        IssueType.BREAKING_CHANGE,
    ]
    
    high_types = [
        IssueType.CONFLICTING_REQUIREMENTS,
        IssueType.HALLUCINATED_API,
        IssueType.HALLUCINATED_FILE,
        IssueType.SCOPE_CREEP,
    ]
    
    medium_types = [
        IssueType.DEPRECATED_SYSTEM,
        IssueType.MISSING_DEPENDENCY,
        IssueType.INCORRECT_METHOD,
        IssueType.WRONG_NAMESPACE,
    ]
    
    if issue_type in critical_types:
        return IssueSeverity.CRITICAL
    elif issue_type in high_types:
        return IssueSeverity.HIGH
    elif issue_type in medium_types:
        return IssueSeverity.MEDIUM
    else:
        return IssueSeverity.LOW


def create_escalation_message(
    issues: List[DetectedIssue],
    flow_name: str,
    additional_context: str = ""
) -> str:
    """
    Create formatted escalation message for human_feedback decorator.
    
    Args:
        issues: List of critical issues
        flow_name: Name of the flow (Planning, Implementation, etc.)
        additional_context: Optional additional context
    
    Returns:
        Formatted message string
    """
    msg = [
        f"⚠️  {flow_name.upper()} MANAGER - CRITICAL ISSUE DETECTED",
        "",
        "The manager has detected issues that require your review.",
        "These issues cannot be auto-fixed with confidence.",
        "",
    ]
    
    if additional_context:
        msg.append(additional_context)
        msg.append("")
    
    msg.append("Please review the details below and provide guidance:")
    msg.append("")
    
    return "\n".join(msg)
