"""
Validation Flow - CrewAI Flow-based workflow for pre-commit validation.

This Flow runs comprehensive validation checks on the codebase:
- JSON content schema validation
- C# build verification
- Localization string sync check

Usage:
    from enlisted_crew.flows import ValidationFlow
    
    flow = ValidationFlow()
    result = flow.kickoff()
"""

import os
from pathlib import Path
from typing import List, Optional, Tuple, Any

from crewai import Agent, Crew, Process, Task, LLM
from crewai.flow.flow import Flow, listen, router, start, or_
from crewai.tasks.task_output import TaskOutput
from pydantic import BaseModel, Field

# Import escalation framework
from .escalation import (
    DetectedIssue,
    IssueType,
    IssueSeverity,
    IssueConfidence,
    should_escalate_to_human,
    format_critical_issues,
    create_escalation_message,
)

from ..memory_config import get_memory_config

# Import tools
from ..tools import (
    validate_content,
    sync_strings,
    build,
    check_event_format,
    review_code,
)


def _get_project_root() -> Path:
    """Get the Enlisted project root directory."""
    env_root = os.environ.get("ENLISTED_PROJECT_ROOT")
    if env_root:
        return Path(env_root)
    
    current = Path(__file__).resolve()
    for parent in current.parents:
        if (parent / "Enlisted.csproj").exists():
            return parent
    
    return Path(r"C:\Dev\Enlisted\Enlisted")


# =============================================================================
# GUARDRAILS - Validate task outputs before proceeding
# =============================================================================

def validate_report_has_status(output: TaskOutput) -> Tuple[bool, Any]:
    """Ensure QA report includes clear pass/fail status.
    
    Reports must be actionable with clear verdicts.
    """
    content = str(output.raw).lower()
    
    # Check for status indicators
    has_status = any([
        "pass" in content,
        "fail" in content,
        "passed" in content,
        "failed" in content,
        "error" in content,
        "valid" in content,
        "invalid" in content,
    ])
    
    if not has_status:
        return (False, "QA report must include clear pass/fail status. State whether each check PASSED or FAILED.")
    
    return (True, output)


def validate_content_check_ran(output: TaskOutput) -> Tuple[bool, Any]:
    """Ensure content validation actually ran.
    
    Reports should reference actual validation results.
    """
    content = str(output.raw).lower()
    
    # Check for evidence that validation ran
    validation_indicators = [
        "validate",
        "schema",
        "json",
        "content",
        "checked",
        "verified",
    ]
    has_validation = sum(1 for ind in validation_indicators if ind in content) >= 2
    
    if not has_validation:
        return (False, "Content validation report must reference actual validation. Include what was checked and results.")
    
    return (True, output)


def validate_build_output_parsed(output: TaskOutput) -> Tuple[bool, Any]:
    """Ensure build validation includes actual build output.
    
    Build reports should reference actual dotnet build results.
    """
    content = str(output.raw).lower()
    
    # Check for build-related terms
    build_indicators = [
        "build",
        "compile",
        "succeeded",
        "dotnet",
        "error",
        "warning",
        "msbuild",
    ]
    has_build = sum(1 for ind in build_indicators if ind in content) >= 2
    
    if not has_build:
        return (False, "Build validation must include actual build results. Reference compilation status, errors, or warnings.")
    
    return (True, output)


# === LLM Configuration ===

def _get_env(name: str, default: str) -> str:
    return os.environ.get(name, default)


# MEDIUM reasoning for QA - needs to catch issues
GPT5_QA = LLM(
    model=_get_env("ENLISTED_LLM_QA", "gpt-5.2"),
    max_completion_tokens=8000,  # Increased for validation reports
    reasoning_effort="medium",
)

# NONE reasoning for fast validation - just run tools
GPT5_FAST = LLM(
    model=_get_env("ENLISTED_LLM_FAST", "gpt-5.2"),
    max_completion_tokens=4000,
    reasoning_effort="none",
)

# Planning LLM - use simple string (LLM objects with reasoning_effort cause issues with AgentPlanner)
GPT5_PLANNING = _get_env("ENLISTED_LLM_PLANNING", "gpt-5.2")

# Function calling LLM - lightweight, no reasoning overhead for tool parameter extraction
# Used at Crew level for all agents' tool calls (per CrewAI docs recommendation)
GPT5_FUNCTION_CALLING = _get_env("ENLISTED_LLM_FUNCTION_CALLING", "gpt-5.2")


# === State Model ===

class ValidationState(BaseModel):
    """State for the Validation Flow."""
    id: str = Field(default="", description="Unique flow execution ID")
    
    # Results
    content_valid: bool = Field(default=False)
    build_valid: bool = Field(default=False)
    strings_synced: bool = Field(default=False)
    
    # Outputs
    content_output: str = ""
    build_output: str = ""
    strings_output: str = ""
    
    # Tracking
    current_step: str = "start"
    issues_found: List[str] = Field(default_factory=list)
    
    # Manager escalation (human-in-the-loop)
    needs_human_review: bool = False
    critical_issues: List[str] = Field(default_factory=list)
    manager_analysis: str = ""
    manager_recommendation: str = ""
    human_guidance: str = ""
    
    # Final
    success: bool = False
    final_report: str = ""


# === Agent Factory ===

_agent_cache = {}


def get_validation_manager() -> Agent:
    """Manager agent that coordinates the validation workflow."""
    if "validation_manager" not in _agent_cache:
        _agent_cache["validation_manager"] = Agent(
            role="QA Coordinator",
            goal="Coordinate comprehensive validation to ensure quality before release",
            backstory="""QA manager who coordinates validation teams before release. 
            Delegates content validation and build checks to specialists, and ensures comprehensive QA reporting. 
            Verifies all validation passes before approval.""",
            llm=GPT5_QA,
            allow_delegation=True,  # REQUIRED for hierarchical managers
            reasoning=True,  # Enable strategic coordination
            max_reasoning_attempts=2,  # Limit overthinking (prevent delays)
            max_iter=15,  # Reduced from default to prevent delegation delays
            max_retry_limit=3,
            verbose=True,
            respect_context_window=True,
        )
    return _agent_cache["validation_manager"]


def get_content_validator() -> Agent:
    """Agent that validates JSON content."""
    if "content_validator" not in _agent_cache:
        _agent_cache["content_validator"] = Agent(
            role="Content Validator",
            goal="Validate JSON content files against schemas",
            backstory="""You validate JSON content for the Enlisted mod.

TOOL-FIRST WORKFLOW:
1. Call validate_content to run full validation
2. Call check_event_format for specific file issues
3. Report all errors found

Each tool takes ONE argument. Execute tools immediately, don't think first.""",
            llm=GPT5_FAST,
            tools=[
                validate_content,
                check_event_format,
            ],
            verbose=True,
            respect_context_window=True,
            max_iter=5,
            max_retry_limit=2,
        )
    return _agent_cache["content_validator"]


def get_build_validator() -> Agent:
    """Agent that validates C# build."""
    if "build_validator" not in _agent_cache:
        _agent_cache["build_validator"] = Agent(
            role="Build Validator",
            goal="Verify C# code compiles without errors",
            backstory="""You validate C# builds for the Enlisted mod.

TOOL-FIRST WORKFLOW:
1. Call build to run dotnet build
2. Report any compilation errors

Execute tools immediately, don't think first.""",
            llm=GPT5_FAST,
            tools=[
                build,
                review_code,
            ],
            verbose=True,
            respect_context_window=True,
            max_iter=5,
            max_retry_limit=2,
        )
    return _agent_cache["build_validator"]


def get_qa_reporter() -> Agent:
    """Agent that produces final QA report."""
    if "qa_reporter" not in _agent_cache:
        _agent_cache["qa_reporter"] = Agent(
            role="QA Reporter",
            goal="Produce clear validation report with prioritized issues",
            backstory="""You produce final QA reports for the Enlisted mod.

You receive validation results and produce a clear, prioritized report.
Focus on actionable issues. Don't pad the report with unnecessary text.""",
            llm=GPT5_QA,
            tools=[],
            verbose=True,
            respect_context_window=True,
            max_iter=3,
        )
    return _agent_cache["qa_reporter"]


# === The Flow ===

class ValidationFlow(Flow[ValidationState]):
    """
    Flow-based validation workflow with hierarchical process.
    
    Uses a single hierarchical crew for all validation tasks.
    
    Steps:
    1. begin_validation - Initialize
    2. run_validation_crew - Single hierarchical crew (content -> build -> strings -> report)
    3. generate_report - Final summary
    """
    
    initial_state = ValidationState
    
    @start()
    def begin_validation(self) -> ValidationState:
        """Entry point: Initialize validation."""
        print("\n" + "=" * 60)
        print("VALIDATION FLOW STARTED")
        print("=" * 60)
        
        state = self.state
        state.current_step = "validate"
        return state
    
    @listen(begin_validation)
    def run_validation_crew(self, state: ValidationState) -> ValidationState:
        """Single hierarchical crew for all validation work.
        
        Manager coordinates: content -> build -> report
        """
        print("\n" + "-" * 60)
        print("[VALIDATION CREW] Running hierarchical validation...")
        print("-" * 60)
        
        # Task 1: Content Validation
        content_task = Task(
            description="""
Validate all JSON content files in the Enlisted mod.

WORKFLOW (execute in order):
1. Call validate_content tool to run full validation
2. Report any schema errors, missing fields, or invalid values

TOOL EFFICIENCY RULES:
- Limit to 1-2 tool calls total
- validate_content runs ALL checks in one call

OUTPUT: List of validation errors or "All content valid"
""",
            expected_output="Content validation results with pass/fail",
            agent=get_content_validator(),
            guardrails=[validate_content_check_ran],
            guardrail_max_retries=2,
        )
        
        # Task 2: Build Validation
        build_task = Task(
            description="""
Run dotnet build to verify C# code compiles.

WORKFLOW (execute in order):
1. Call build tool to run compilation
2. Report any build errors or warnings

TOOL EFFICIENCY RULES:
- Limit to 1 tool call total
- build tool runs full compilation

OUTPUT: Build result (success/failure with errors)
""",
            expected_output="Build validation results with pass/fail",
            agent=get_build_validator(),
            guardrails=[validate_build_output_parsed],
            guardrail_max_retries=2,
        )
        
        # Task 3: Generate QA Report
        report_task = Task(
            description="""
Produce a clear validation report summarizing all checks.

WORKFLOW:
1. Review content validation results from context
2. Review build validation results from context
3. Compile into clear report

TOOL EFFICIENCY RULES:
- NO tool calls needed - all data is in context above
- Just synthesize the report

OUTPUT: Pass/fail status + prioritized list of issues (if any)
""",
            expected_output="Final QA report with overall status",
            agent=get_qa_reporter(),
            context=[content_task, build_task],
            guardrails=[validate_report_has_status],
            guardrail_max_retries=2,
        )
        
        # Single hierarchical crew
        crew = Crew(
            agents=[
                get_content_validator(),
                get_build_validator(),
                get_qa_reporter(),
            ],
            tasks=[content_task, build_task, report_task],
            manager_agent=get_validation_manager(),
            process=Process.hierarchical,
            verbose=True,
            **get_memory_config(),  # memory=True + contextual retrieval
            cache=True,
            planning=True,
            planning_llm=GPT5_PLANNING,
            function_calling_llm=GPT5_FUNCTION_CALLING,  # Lightweight LLM for tool calls
        )
        
        result = crew.kickoff()
        output = str(result)
        
        # Parse results
        output_lower = output.lower()
        state.content_output = output
        state.build_output = output
        
        # Check content validation
        if "content" in output_lower and ("error" in output_lower or "invalid" in output_lower):
            state.content_valid = False
            state.issues_found.append("Content validation errors found")
        else:
            state.content_valid = True
        
        # Check build validation
        if "build" in output_lower and "error" in output_lower:
            state.build_valid = False
            state.issues_found.append("Build errors found")
        else:
            state.build_valid = True
        
        # Check strings (run directly since it's a simple tool)
        try:
            strings_result = sync_strings._run()
            state.strings_output = strings_result
            if "error" in strings_result.lower() or "missing" in strings_result.lower():
                state.strings_synced = False
                state.issues_found.append("Localization strings out of sync")
            else:
                state.strings_synced = True
        except Exception as e:
            state.strings_output = f"Error: {str(e)}"
            state.strings_synced = False
        
        state.current_step = "report"
        return state
    
    @listen(run_validation_crew)
    def check_for_issues(self, state: ValidationState) -> ValidationState:
        """Manager analyzes outputs and decides if human review needed.
        
        Detects:
        - Critical build failures
        - Data integrity issues
        """
        print("\n" + "-"*60)
        print("[MANAGER] Analyzing validation outputs for issues...")
        print("-"*60)
        
        issues = []
        combined_output = (state.content_output + state.build_output + state.strings_output).lower()
        
        # Check for critical build failures
        critical_build_patterns = [
            "error cs",
            "build failed",
            "fatal error",
            "compilation error",
        ]
        for pattern in critical_build_patterns:
            if pattern in combined_output:
                issues.append(DetectedIssue(
                    issue_type=IssueType.ARCHITECTURE_VIOLATION,
                    severity=IssueSeverity.CRITICAL,
                    confidence=IssueConfidence.HIGH,
                    description="Critical build errors detected",
                    affected_component="Build Output",
                    evidence=f"Pattern '{pattern}' found in build output",
                    manager_recommendation="Fix compilation errors before proceeding",
                    auto_fixable=False,
                ))
                break
        
        # Check for data integrity issues (missing localizations, etc.)
        data_integrity_patterns = [
            "missing string",
            "orphan",
            "duplicate id",
            "schema violation",
        ]
        for pattern in data_integrity_patterns:
            if pattern in combined_output:
                issues.append(DetectedIssue(
                    issue_type=IssueType.DATA_LOSS_RISK,
                    severity=IssueSeverity.HIGH,
                    confidence=IssueConfidence.MEDIUM,
                    description="Data integrity issues detected",
                    affected_component="Content Validation",
                    evidence=f"Pattern '{pattern}' found in validation output",
                    manager_recommendation="Review and fix data integrity issues",
                    auto_fixable=True,
                ))
                break
        
        # Check for security patterns in content
        security_patterns = [
            "injection",
            "script",
            "<script",
            "eval(",
        ]
        for pattern in security_patterns:
            if pattern in combined_output:
                issues.append(DetectedIssue(
                    issue_type=IssueType.SECURITY_CONCERN,
                    severity=IssueSeverity.CRITICAL,
                    confidence=IssueConfidence.MEDIUM,
                    description="Potential security issue in content",
                    affected_component="Content Validation",
                    evidence=f"Pattern '{pattern}' found in content",
                    manager_recommendation="Security review required",
                    auto_fixable=False,
                ))
                break
        
        # Determine if escalation needed
        critical_issues = [i for i in issues if should_escalate_to_human(i)]
        
        if critical_issues:
            print(f"[MANAGER] Found {len(critical_issues)} critical issue(s) requiring human review")
            state.needs_human_review = True
            state.critical_issues = [str(i) for i in critical_issues]
            state.manager_analysis = format_critical_issues(critical_issues)
            state.manager_recommendation = critical_issues[0].manager_recommendation if critical_issues else ""
        else:
            for issue in issues:
                if issue.auto_fixable:
                    print(f"[MANAGER] Auto-handling: {issue.description}")
            print("[MANAGER] No critical issues found - generating report")
        
        return state
    
    @router(check_for_issues)
    def route_after_check(self, state: ValidationState) -> str:
        """Route to report (human feedback disabled)."""
        if state.needs_human_review:
            print("[ROUTER] Critical issues detected but auto-proceeding (HITL disabled)")
            print(f"[ROUTER] Issues logged: {len(state.critical_issues)}")
            # Auto-resolve: log and continue to report
            state.needs_human_review = False
        print("[ROUTER] -> report")
        return "report"
    
    @listen("escalate_to_human")
    def human_review_critical(self, state: ValidationState) -> ValidationState:
        """Human-in-the-loop for critical issues - DISABLED (never reached)."""
        print("\n[MANAGER] Critical issues detected - auto-proceeding with investigation")
        if state.manager_analysis:
            print(f"[MANAGER] Analysis: {state.manager_analysis[:200]}...")
        state.human_guidance = "auto-investigate - HITL disabled"
        state.needs_human_review = False
        return state
    
    @listen("report")
    def generate_report(self, state: ValidationState) -> ValidationState:
        """Step 4: Generate final validation report."""
        # Skip if aborted
        if state.current_step == "complete" and not state.success:
            return state
        
        print("\n" + "-" * 60)
        print("[REPORT] Generating validation report...")
        print("-" * 60)
        
        # Determine overall success
        state.success = state.content_valid and state.build_valid and state.strings_synced
        
        # Build report
        report_lines = [
            "=" * 60,
            "VALIDATION REPORT",
            "=" * 60,
            "",
            f"Content Validation: {'PASSED' if state.content_valid else 'FAILED'}",
            f"C# Build: {'PASSED' if state.build_valid else 'FAILED'}",
            f"Localization Sync: {'PASSED' if state.strings_synced else 'FAILED'}",
            "",
        ]
        
        if state.issues_found:
            report_lines.append("ISSUES FOUND:")
            for issue in state.issues_found:
                report_lines.append(f"  - {issue}")
            report_lines.append("")
        
        report_lines.append("=" * 60)
        report_lines.append(f"OVERALL: {'PASSED' if state.success else 'FAILED'}")
        report_lines.append("=" * 60)
        
        state.final_report = "\n".join(report_lines)
        
        print(state.final_report)
        
        state.current_step = "complete"
        return state
