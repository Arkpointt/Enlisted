"""
Reusable condition functions for CrewAI Flow routing.

These pure functions make routing decisions based on state and task outputs.
They enable clean, testable conditional logic across all flows.

Pattern:
    def condition_name(state: StateType) -> bool:
        \"\"\"Check if condition is met.\"\"\"
        return boolean_result

Usage in Flows:
    @router(some_step)
    def route_next(self, state: MyState) -> str:
        if my_condition(state):
            return "path_a"
        else:
            return "path_b"
"""

from typing import Union, List
from crewai.tasks.task_output import TaskOutput

from .state_models import (
    # Bug Hunting
    BugHuntingState,
    BugSeverity,
    ConfidenceLevel,
    # Planning
    PlanningState,
    ValidationStatus,
    # Implementation
    ImplementationState,
    ImplementationStatus,
)


# === Planning Flow Conditions ===

def validation_passed(state: PlanningState) -> bool:
    """Check if plan validation passed."""
    return state.validation_status == ValidationStatus.PASSED


def validation_fixed(state: PlanningState) -> bool:
    """Check if plan was successfully fixed after validation failure."""
    return state.validation_status == ValidationStatus.FIXED


def can_retry_fix(state: PlanningState) -> bool:
    """Check if we can attempt another fix iteration."""
    return state.fix_attempts < state.max_fix_attempts


def needs_plan_fix(state: PlanningState) -> bool:
    """Check if plan validation failed and we should attempt a fix."""
    return (
        state.validation_status == ValidationStatus.FAILED
        and can_retry_fix(state)
    )


def validation_complete(state: PlanningState) -> bool:
    """Check if validation process is complete (passed, fixed, or max attempts reached)."""
    return (
        state.validation_status in [ValidationStatus.PASSED, ValidationStatus.FIXED]
        or state.fix_attempts >= state.max_fix_attempts
    )


# === Implementation Flow Conditions ===

def needs_csharp_work(state: ImplementationState) -> bool:
    """Check if C# implementation is needed."""
    return state.csharp_status != ImplementationStatus.COMPLETE


def needs_content_work(state: ImplementationState) -> bool:
    """Check if JSON content implementation is needed."""
    return state.content_status != ImplementationStatus.COMPLETE


def csharp_complete(state: ImplementationState) -> bool:
    """Check if C# implementation is complete."""
    return state.csharp_status == ImplementationStatus.COMPLETE


def content_complete(state: ImplementationState) -> bool:
    """Check if content implementation is complete."""
    return state.content_status == ImplementationStatus.COMPLETE


def all_work_complete(state: ImplementationState) -> bool:
    """Check if both C# and content work are complete."""
    return csharp_complete(state) and content_complete(state)


def has_partial_implementation(state: ImplementationState) -> bool:
    """Check if there's any partial implementation."""
    return (
        state.csharp_status == ImplementationStatus.PARTIAL
        or state.content_status == ImplementationStatus.PARTIAL
    )


# === Bug Hunting Flow Conditions ===

def needs_systems_analysis(state: BugHuntingState) -> bool:
    """Check if bug requires deeper systems analysis."""
    if not state.investigation:
        return False
    
    investigation = state.investigation
    
    # Complex or critical bugs always need systems analysis
    if investigation.severity in [BugSeverity.COMPLEX, BugSeverity.CRITICAL]:
        return True
    
    # Explicit flag from investigation
    if investigation.needs_systems_analysis:
        return True
    
    return False


def is_simple_bug(state: BugHuntingState) -> bool:
    """Check if bug is simple enough to skip systems analysis."""
    if not state.investigation:
        return False
    
    return (
        state.investigation.severity == BugSeverity.SIMPLE
        and not state.investigation.needs_systems_analysis
    )


def is_critical_bug(state: BugHuntingState) -> bool:
    """Check if bug is critical severity."""
    if not state.investigation:
        return False
    
    return state.investigation.severity == BugSeverity.CRITICAL


def has_high_confidence(state: BugHuntingState) -> bool:
    """Check if investigation has high confidence in findings."""
    if not state.investigation:
        return False
    
    return state.investigation.confidence in [
        ConfidenceLevel.CERTAIN,
        ConfidenceLevel.LIKELY,
    ]


def has_user_logs(state: BugHuntingState) -> bool:
    """Check if user provided log content."""
    return state.bug_report.has_user_logs if state.bug_report else False


# === TaskOutput-Based Conditions ===

def validation_failed_in_output(output: TaskOutput) -> bool:
    """
    Check if validation failed by examining task output.
    
    Prefers structured Pydantic output if available, falls back to text parsing.
    
    Example usage with TaskOutput.pydantic:
        if hasattr(output, 'pydantic') and output.pydantic:
            # Structured access to ValidationOutput model
            return output.pydantic.status == ValidationStatus.FAILED
    """
    if not output:
        return False
    
    # Prefer structured Pydantic output if available
    if hasattr(output, 'pydantic') and output.pydantic:
        try:
            # Access ValidationOutput model fields directly
            from .state_models import ValidationStatus, ValidationOutput
            if isinstance(output.pydantic, ValidationOutput):
                return output.pydantic.status == ValidationStatus.FAILED
        except (AttributeError, ImportError):
            pass  # Fall back to text parsing
    
    # Fallback: Parse text output
    if not output.raw:
        return False
    
    output_lower = str(output.raw).lower()
    
    # Failure indicators
    failure_patterns = [
        "status: failed",
        "validation: failed",
        "validation failed",
        "issues found",
        "errors found",
    ]
    
    return any(pattern in output_lower for pattern in failure_patterns)


def validation_passed_in_output(output: TaskOutput) -> bool:
    """
    Check if validation passed by examining task output.
    
    Prefers structured Pydantic output if available, falls back to text parsing.
    """
    if not output:
        return False
    
    # Prefer structured Pydantic output if available
    if hasattr(output, 'pydantic') and output.pydantic:
        try:
            from .state_models import ValidationStatus, ValidationOutput
            if isinstance(output.pydantic, ValidationOutput):
                return output.pydantic.status == ValidationStatus.PASSED
        except (AttributeError, ImportError):
            pass  # Fall back to text parsing
    
    # Fallback: Parse text output
    if not output.raw:
        return False
    
    output_lower = str(output.raw).lower()
    
    # Success indicators
    success_patterns = [
        "status: passed",
        "validation: passed",
        "validation passed",
        "no issues",
        "all checks passed",
    ]
    
    return any(pattern in output_lower for pattern in success_patterns)


def get_validation_issues_from_output(output: TaskOutput) -> List[str]:
    """
    Extract validation issues from task output.
    
    Example of structured data extraction using TaskOutput.pydantic.
    
    Returns:
        List of validation issues found
    """
    if not output:
        return []
    
    # Prefer structured Pydantic output
    if hasattr(output, 'pydantic') and output.pydantic:
        try:
            from .state_models import ValidationOutput
            if isinstance(output.pydantic, ValidationOutput):
                return output.pydantic.issues_found
        except (AttributeError, ImportError):
            pass
    
    # Fallback: Return empty list
    return []


def build_succeeded_in_output(output: TaskOutput) -> bool:
    """
    Check if build succeeded by examining task output.
    """
    if not output or not output.raw:
        return False
    
    output_lower = str(output.raw).lower()
    
    return (
        "build: success" in output_lower
        or "build succeeded" in output_lower
        or "build status: pass" in output_lower
    )


def has_implementation_status_in_output(output: TaskOutput, status_keyword: str) -> bool:
    """
    Generic check for implementation status in output.
    
    Args:
        output: Task output to check
        status_keyword: Keyword to look for (e.g., "complete", "partial", "not_started")
    """
    if not output or not output.raw:
        return False
    
    output_lower = str(output.raw).lower()
    keyword_lower = status_keyword.lower()
    
    return keyword_lower in output_lower


# === Logging Helpers ===

def format_routing_decision(
    condition_name: str,
    condition_result: bool,
    chosen_path: str,
    reason: str,
) -> str:
    """
    Format a consistent routing decision log message.
    
    Args:
        condition_name: Name of the condition being evaluated
        condition_result: Boolean result of the condition
        chosen_path: The path being taken
        reason: Human-readable explanation
    
    Returns:
        Formatted log message
    """
    symbol = "[OK]" if condition_result else "[X]"
    
    return f"""
*** CONDITIONAL ROUTING ***
   Condition: {condition_name}
   Result: {symbol} {condition_result}
   Decision: {chosen_path}
   Reason: {reason}
"""
