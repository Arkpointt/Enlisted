"""
Enlisted CrewAI Flows

Flow-based workflows for complex, state-dependent tasks.

Available Flows:
- PlanningFlow: Design features with research, architecture advice, and validation
- ImplementationFlow: Implement features from plans with smart partial-implementation handling
- BugHuntingFlow: Investigate bugs, analyze systems, propose and validate fixes
- ValidationFlow: Pre-commit validation (content, build, localization)

All flows use:
- State persistence (persist=True) for recovery on failure
- GPT-5.2 with automatic instant/thinking mode switching
- Conditional routing with reusable condition functions

Conditional Routing:
- Flow-level routing with @router decorators
- Task-level conditions with ConditionalTask
- Structured output parsing with TaskOutput.pydantic
- Reusable condition functions in conditions.py
"""

from .implementation_flow import ImplementationFlow
from .bug_hunting_flow import BugHuntingFlow
from .validation_flow import ValidationFlow, ValidationState

# Import all state models from centralized location
from .state_models import (
    # Planning
    PlanningState,
    ValidationStatus,
    ValidationOutput,
    # Implementation
    ImplementationState,
    ImplementationStatus,
    ComponentCheck,
    # Bug Hunting
    BugHuntingState,
    BugReport,
    BugSeverity,
    ConfidenceLevel,
    Investigation,
    SystemsAnalysis,
    FixProposal,
    ValidationResult,
    AffectedFile,
    CodeChange,
)

# Condition functions for conditional routing
from .conditions import (
    # Planning conditions
    validation_passed,
    validation_fixed,
    needs_plan_fix,
    validation_complete,
    # Implementation conditions
    needs_csharp_work,
    needs_content_work,
    csharp_complete,
    content_complete,
    all_work_complete,
    has_partial_implementation,
    # Bug hunting conditions
    needs_systems_analysis,
    is_simple_bug,
    is_critical_bug,
    has_high_confidence,
    has_user_logs,
    # TaskOutput conditions
    validation_passed_in_output,
    validation_failed_in_output,
    build_succeeded_in_output,
    get_validation_issues_from_output,
    # Logging
    format_routing_decision,
)

__all__ = [
    # Flows
    "ImplementationFlow",
    "BugHuntingFlow",
    "ValidationFlow",
    "ValidationState",
    # Planning state models
    "PlanningState",
    "ValidationStatus",
    "ValidationOutput",
    # Implementation state models
    "ImplementationState",
    "ImplementationStatus",
    "ComponentCheck",
    # Bug hunting state models
    "BugHuntingState",
    "BugReport",
    "BugSeverity",
    "ConfidenceLevel",
    "Investigation",
    "SystemsAnalysis",
    "FixProposal",
    "ValidationResult",
    "AffectedFile",
    "CodeChange",
    # Condition functions
    "validation_passed",
    "validation_fixed",
    "needs_plan_fix",
    "validation_complete",
    "needs_csharp_work",
    "needs_content_work",
    "csharp_complete",
    "content_complete",
    "all_work_complete",
    "has_partial_implementation",
    "needs_systems_analysis",
    "is_simple_bug",
    "is_critical_bug",
    "has_high_confidence",
    "has_user_logs",
    "validation_passed_in_output",
    "validation_failed_in_output",
    "build_succeeded_in_output",
    "get_validation_issues_from_output",
    "format_routing_decision",
]
