"""
Enlisted CrewAI Flows

Flow-based workflows for complex, state-dependent tasks.

Available Flows:
- BugHuntingFlow: Investigate bugs, analyze systems, propose and validate fixes
"""

from .bug_hunting_flow import BugHuntingFlow
from .state_models import (
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

__all__ = [
    # Flows
    "BugHuntingFlow",
    # State models
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
]
