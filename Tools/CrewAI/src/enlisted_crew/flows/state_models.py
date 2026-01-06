"""
State Models for All CrewAI Flows

Pydantic models that define structured state for all workflows.
These ensure type safety and validation as data flows between agents.

Flows:
- PlanningFlow: Feature design and specification
- ImplementationFlow: Code and content implementation
- BugHuntingFlow: Bug investigation and fixing
"""

from enum import Enum
from typing import List, Optional
from pydantic import BaseModel, Field


class BugSeverity(str, Enum):
    """Classification of bug severity for routing decisions."""
    SIMPLE = "simple"       # Typos, config issues, missing content
    MODERATE = "moderate"   # Logic errors, state issues
    COMPLEX = "complex"     # Multi-system, architectural
    CRITICAL = "critical"   # Crashes, save corruption, data loss


class ConfidenceLevel(str, Enum):
    """How confident we are in the analysis."""
    CERTAIN = "certain"       # Clear evidence, reproducible
    LIKELY = "likely"         # Strong indicators
    SPECULATIVE = "speculative"  # Educated guess, needs verification


# === Planning Flow Enums and Models ===

class ValidationStatus(str, Enum):
    """Status of plan validation."""
    NOT_VALIDATED = "not_validated"
    PASSED = "passed"
    FAILED = "failed"
    FIXED = "fixed"


class ValidationOutput(BaseModel):
    """
    Structured output model for validation tasks.
    
    Use with TaskOutput.pydantic for type-safe validation result parsing.
    """
    status: ValidationStatus = ValidationStatus.NOT_VALIDATED
    issues_found: List[str] = Field(default_factory=list)
    verified_items: List[str] = Field(default_factory=list)
    file_checks: int = Field(default=0, description="Number of files verified")
    method_checks: int = Field(default=0, description="Number of methods verified")
    content_id_checks: int = Field(default=0, description="Number of content IDs verified")


class PlanningState(BaseModel):
    """
    State for the Planning Flow.
    
    Tracks research, design, and validation progress.
    """
    # Flow ID (required by CrewAI Flows)
    id: str = Field(
        default="",
        description="Unique flow execution ID (auto-generated)"
    )
    
    # Input
    feature_name: str = ""
    description: str = ""
    related_systems: str = ""
    related_docs: str = ""
    
    # Research phase
    research_output: str = ""
    suggestions_output: str = ""
    
    # Design phase
    design_output: str = ""
    plan_path: str = ""
    
    # Validation phase
    validation_status: ValidationStatus = ValidationStatus.NOT_VALIDATED
    validation_issues: List[str] = Field(default_factory=list)
    fix_attempts: int = 0
    max_fix_attempts: int = 2
    
    # Execution tracking
    current_step: str = "start"
    
    # Final
    success: bool = False
    final_report: str = ""


# === Implementation Flow Enums and Models ===

class ImplementationStatus(str, Enum):
    """Status of a component's implementation."""
    NOT_STARTED = "not_started"
    PARTIAL = "partial"
    COMPLETE = "complete"
    SKIP = "skip"  # Explicitly marked to skip


class ComponentCheck(BaseModel):
    """Result of checking if a component exists."""
    name: str
    exists: bool
    path: Optional[str] = None
    details: str = ""


class ImplementationState(BaseModel):
    """
    State for the Implementation Flow.
    
    Tracks what's been done, what needs doing, and routing decisions.
    """
    # Flow ID (required by CrewAI Flows)
    id: str = Field(
        default="",
        description="Unique flow execution ID (auto-generated)"
    )
    
    # Input
    plan_path: str = ""
    plan_content: str = ""
    
    # Plan version tracking (for detecting mid-implementation changes)
    plan_hash: str = Field(
        default="",
        description="MD5 hash of plan content at start of implementation"
    )
    plan_changed: bool = Field(
        default=False,
        description="True if plan was modified during implementation"
    )
    
    # Verification results (populated by verify_existing step)
    csharp_status: ImplementationStatus = ImplementationStatus.NOT_STARTED
    content_status: ImplementationStatus = ImplementationStatus.NOT_STARTED
    existing_files: List[str] = Field(default_factory=list)
    existing_event_ids: List[str] = Field(default_factory=list)
    missing_csharp: List[str] = Field(default_factory=list)  # Files/methods still needed
    missing_content: List[str] = Field(default_factory=list)  # Event IDs still needed
    
    # Execution tracking
    current_step: str = "start"
    skipped_steps: List[str] = Field(default_factory=list)
    
    # Outputs
    csharp_output: str = ""
    content_output: str = ""
    validation_output: str = ""
    docs_output: str = ""
    
    # Final
    success: bool = False
    final_report: str = ""


# === Bug Hunting Flow Models ===

class BugReport(BaseModel):
    """Input from user describing the bug."""
    description: str = Field(
        ...,
        description="User's description of what went wrong"
    )
    error_codes: str = Field(
        default="none",
        description="Any E-*/W-* error codes from logs"
    )
    repro_steps: str = Field(
        default="unknown",
        description="Steps to reproduce the bug"
    )
    context: Optional[str] = Field(
        default=None,
        description="Additional context (game state, mods, etc.)"
    )
    user_log_content: Optional[str] = Field(
        default=None,
        description="Log content provided by the end user (not local dev logs)"
    )
    has_user_logs: bool = Field(
        default=False,
        description="Whether the user provided log content to analyze"
    )


class AffectedFile(BaseModel):
    """A file potentially affected by the bug."""
    path: str = Field(..., description="File path relative to project root")
    line_numbers: Optional[str] = Field(
        default=None,
        description="Relevant line numbers or ranges"
    )
    reason: str = Field(..., description="Why this file is relevant")


class Investigation(BaseModel):
    """Output from code_analyst's bug investigation."""
    summary: str = Field(
        ...,
        description="Brief summary of findings"
    )
    root_cause: Optional[str] = Field(
        default=None,
        description="Identified root cause of the bug"
    )
    error_codes_found: List[str] = Field(
        default_factory=list,
        description="Error codes discovered in logs"
    )
    stack_trace: Optional[str] = Field(
        default=None,
        description="Relevant stack trace if found"
    )
    affected_files: List[AffectedFile] = Field(
        default_factory=list,
        description="Files related to the bug"
    )
    severity: BugSeverity = Field(
        default=BugSeverity.MODERATE,
        description="Assessed severity for routing"
    )
    confidence: ConfidenceLevel = Field(
        default=ConfidenceLevel.LIKELY,
        description="Confidence in the analysis"
    )
    needs_systems_analysis: bool = Field(
        default=False,
        description="Whether systems analyst should review"
    )
    raw_output: str = Field(
        default="",
        description="Full agent output for reference"
    )


class SystemsAnalysis(BaseModel):
    """Output from systems_analyst's deeper analysis."""
    related_systems: List[str] = Field(
        default_factory=list,
        description="Systems involved (Orchestrator, Enlistment, etc.)"
    )
    scope: str = Field(
        default="localized",
        description="Scope of issue: localized, module-wide, systemic"
    )
    similar_patterns: List[str] = Field(
        default_factory=list,
        description="Similar code that might have same issue"
    )
    risk_assessment: str = Field(
        default="",
        description="Risk level and potential side effects"
    )
    additional_files: List[AffectedFile] = Field(
        default_factory=list,
        description="Additional files discovered during analysis"
    )
    raw_output: str = Field(
        default="",
        description="Full agent output for reference"
    )


class CodeChange(BaseModel):
    """A proposed code change."""
    file_path: str = Field(..., description="File to modify")
    change_type: str = Field(
        default="modify",
        description="Type: create, modify, delete"
    )
    description: str = Field(..., description="What the change does")
    diff: Optional[str] = Field(
        default=None,
        description="Code diff or pseudocode"
    )


class FixProposal(BaseModel):
    """Output from csharp_implementer's fix proposal."""
    summary: str = Field(
        ...,
        description="Brief summary of the proposed fix"
    )
    changes: List[CodeChange] = Field(
        default_factory=list,
        description="List of code changes"
    )
    explanation: str = Field(
        default="",
        description="Why this fix addresses the root cause"
    )
    risks: List[str] = Field(
        default_factory=list,
        description="Potential risks or side effects"
    )
    testing_notes: str = Field(
        default="",
        description="How to verify the fix works"
    )
    raw_output: str = Field(
        default="",
        description="Full agent output for reference"
    )


class ValidationResult(BaseModel):
    """Output from qa_agent's validation."""
    approved: bool = Field(
        default=False,
        description="Whether the fix is approved"
    )
    build_status: str = Field(
        default="not_run",
        description="Build result: pass, fail, not_run"
    )
    style_issues: List[str] = Field(
        default_factory=list,
        description="Code style violations found"
    )
    concerns: List[str] = Field(
        default_factory=list,
        description="QA concerns about the fix"
    )
    recommendations: List[str] = Field(
        default_factory=list,
        description="Recommendations for improvement"
    )
    raw_output: str = Field(
        default="",
        description="Full agent output for reference"
    )


class BugHuntingState(BaseModel):
    """
    Combined state for the bug hunting flow.
    
    This is the main state object that flows through the entire workflow,
    accumulating results from each step.
    
    Inputs are passed via kickoff(inputs={...}) and populate fields directly.
    Use bug_description (not description) to avoid conflicts.
    """
    # Flow ID (required by CrewAI Flows)
    id: str = Field(
        default="",
        description="Unique flow execution ID (auto-generated)"
    )
    
    # Input fields (flat inputs for kickoff)
    bug_description: str = Field(
        default="",
        description="User's description of what went wrong"
    )
    error_codes: str = Field(
        default="none",
        description="Any E-*/W-* error codes from logs"
    )
    repro_steps: str = Field(
        default="unknown",
        description="Steps to reproduce the bug"
    )
    context: Optional[str] = Field(
        default=None,
        description="Additional context (game state, mods, etc.)"
    )
    user_log_content: Optional[str] = Field(
        default=None,
        description="Log content provided by the end user"
    )
    
    # Constructed bug report (built from flat inputs)
    bug_report: Optional[BugReport] = None
    
    # Step outputs
    investigation: Optional[Investigation] = None
    systems_analysis: Optional[SystemsAnalysis] = None
    fix_proposal: Optional[FixProposal] = None
    validation: Optional[ValidationResult] = None
    
    # Flow control
    current_step: str = Field(
        default="not_started",
        description="Current step in the flow"
    )
    skipped_steps: List[str] = Field(
        default_factory=list,
        description="Steps that were skipped (e.g., systems_analysis for simple bugs)"
    )
    
    # Final output
    final_report: Optional[str] = None
    success: bool = False


# Type alias for cleaner function signatures
FlowState = BugHuntingState
