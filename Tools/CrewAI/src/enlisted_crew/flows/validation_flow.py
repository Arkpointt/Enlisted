"""
Validation Flow - CrewAI Flow-based workflow for pre-commit validation.

This Flow runs comprehensive validation checks on the codebase:
- JSON content schema validation
- C# build verification
- Localization string sync check

Phase 4 Refactor (2026-01-09):
- Removed: Multi-agent Crew with 3 agents
- Pattern: Pure Python + single-agent Crew for complex checks
- Steps: 6 Flow methods (2 single-agent + 4 pure Python)

Usage:
    from enlisted_crew.flows import ValidationFlow
    
    flow = ValidationFlow()
    result = flow.kickoff()
"""

import os
from pathlib import Path
from typing import List, Tuple, Any

from crewai import Agent, Crew, Process, Task, LLM
from crewai.flow.flow import Flow, listen, router, start
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
)

from ..memory_config import get_memory_config
from ..monitoring import EnlistedExecutionMonitor

# Import tools
from ..tools import (
    validate_content,
    sync_strings,
    build,
    check_event_format,
    review_code,
)

# Import prompt templates for caching optimization
from ..prompts import (
    VALIDATION_WORKFLOW,
    TOOL_EFFICIENCY_RULES,
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
    model=_get_env("ENLISTED_LLM_QA", "gpt-5.2-codex"),
    max_completion_tokens=8000,
    reasoning_effort="medium",
)

# NONE reasoning for fast validation - just run tools
GPT5_FAST = LLM(
    model=_get_env("ENLISTED_LLM_FAST", "gpt-5.2-codex"),
    max_completion_tokens=4000,
    reasoning_effort="none",
)

# Planning LLM - use simple string
GPT5_PLANNING = _get_env("ENLISTED_LLM_PLANNING", "gpt-5.2-codex")

# Function calling LLM - lightweight
GPT5_FUNCTION_CALLING = _get_env("ENLISTED_LLM_FUNCTION_CALLING", "gpt-5.2-codex")


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
    
    # Manager escalation
    needs_human_review: bool = False
    critical_issues: List[str] = Field(default_factory=list)
    manager_analysis: str = ""
    manager_recommendation: str = ""
    
    # Final
    success: bool = False
    final_report: str = ""


# === The Flow ===

class ValidationFlow(Flow[ValidationState]):
    """
    Flow-based validation workflow with single-agent pattern.
    
    Phase 4 Architecture (2026-01-09):
    - Step 1: validate_content_check - Single-agent Crew (2 tools: validate_content, check_event_format) + memory
    - Step 2: validate_build_check - Single-agent Crew (2 tools: build, review_code) + memory
    - Step 3: sync_localization - Pure Python (direct tool call)
    - Step 4: check_for_issues - Pure Python (pattern-based issue detection)
    - Step 5: route_after_check - @router (escalation disabled, always proceeds)
    - Step 6: generate_report - Pure Python (format final report)
    
    Removed:
    - Multi-agent Crew with 3 agents
    - Agent factory functions (get_content_validator, get_build_validator, get_qa_reporter, get_validation_manager)
    - Hierarchical process overhead
    
    Expected impact:
    - Tool calls: 15-20 → 4-6 (70% reduction)
    - Execution time: 1-2 min → 30-45s (60% faster)
    - Cost: 70% reduction
    """
    
    initial_state = ValidationState
    
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self._monitor = EnlistedExecutionMonitor()
        print("[MONITORING] Execution monitoring enabled")
    
    @start()
    def validate_content_check(self) -> ValidationState:
        """Step 1: Validate JSON content with single-agent Crew."""
        print("\n" + "=" * 60)
        print("VALIDATION FLOW - Step 1: Content Validation")
        print("=" * 60)
        
        state = self.state
        state.current_step = "content_check"
        
        # Single agent with only content validation tools
        agent = Agent(
            role="Content Validator",
            goal="Validate JSON content files against schemas",
            backstory="""You validate JSON content for the Enlisted mod.

TOOL-FIRST WORKFLOW:
1. Call validate_content to run full validation
2. Call check_event_format for specific file issues
3. Report all errors found

Each tool takes ONE argument. Execute tools immediately.""",
            llm=GPT5_FAST,
            tools=[
                validate_content,
                check_event_format,
            ],
            max_iter=5,
            max_retry_limit=2,
            allow_delegation=False,
        )
        
        task = Task(
            description=f"""{VALIDATION_WORKFLOW}
{TOOL_EFFICIENCY_RULES}

=== YOUR TASK ===
Validate all JSON content files in the Enlisted mod.

**Expected Output:**
- Validation Status: PASS or FAIL
- If FAIL: List of validation errors with details
- Missing tooltips: [list if any]
- Invalid categories: [list if any]
- Orphaned strings: [list if any]
            """,
            expected_output="Content validation results with pass/fail",
            agent=agent,
            guardrails=[validate_content_check_ran],
            guardrail_max_retries=2,
        )
        
        # Single-agent crew with memory
        crew = Crew(
            agents=[agent],
            tasks=[task],
            **get_memory_config(),
            process=Process.sequential,
            cache=True,
            planning=True,
            planning_llm=GPT5_PLANNING,
            function_calling_llm=GPT5_FUNCTION_CALLING,
        )
        
        result = crew.kickoff()
        state.content_output = str(result)
        
        # Parse results
        output_lower = state.content_output.lower()
        if "error" in output_lower or "invalid" in output_lower or "fail" in output_lower:
            state.content_valid = False
            state.issues_found.append("Content validation errors found")
        else:
            state.content_valid = True
        
        print(f"[CONTENT] Validation: {'PASSED' if state.content_valid else 'FAILED'}")
        return state
    
    @listen(validate_content_check)
    def validate_build_check(self, state: ValidationState) -> ValidationState:
        """Step 2: Validate C# build with single-agent Crew."""
        print("\n" + "-" * 60)
        print("VALIDATION FLOW - Step 2: Build Validation")
        print("-" * 60)
        
        state.current_step = "build_check"
        
        # Single agent with only build validation tools
        agent = Agent(
            role="Build Validator",
            goal="Verify C# code compiles without errors",
            backstory="""You validate C# builds for the Enlisted mod.

TOOL-FIRST WORKFLOW:
1. Call build to run dotnet build
2. Report any compilation errors

Execute tools immediately.""",
            llm=GPT5_FAST,
            tools=[
                build,
                review_code,
            ],
            max_iter=5,
            max_retry_limit=2,
            allow_delegation=False,
        )
        
        task = Task(
            description=f"""{VALIDATION_WORKFLOW}
{TOOL_EFFICIENCY_RULES}

=== YOUR TASK ===
Run dotnet build to verify C# code compiles.

**Expected Output:**
- Build Status: SUCCESS or FAIL
- If FAIL: Compilation errors with file paths and line numbers
- Warnings: [list if any]
            """,
            expected_output="Build validation results with pass/fail",
            agent=agent,
            guardrails=[validate_build_output_parsed],
            guardrail_max_retries=2,
        )
        
        # Single-agent crew with memory
        crew = Crew(
            agents=[agent],
            tasks=[task],
            **get_memory_config(),
            process=Process.sequential,
            cache=True,
            planning=True,
            planning_llm=GPT5_PLANNING,
            function_calling_llm=GPT5_FUNCTION_CALLING,
        )
        
        result = crew.kickoff()
        state.build_output = str(result)
        
        # Parse results
        output_lower = state.build_output.lower()
        if "error" in output_lower or "fail" in output_lower:
            state.build_valid = False
            state.issues_found.append("Build errors found")
        else:
            state.build_valid = True
        
        print(f"[BUILD] Validation: {'PASSED' if state.build_valid else 'FAILED'}")
        return state
    
    @listen(validate_build_check)
    def sync_localization(self, state: ValidationState) -> ValidationState:
        """Step 3: Sync localization strings - Pure Python."""
        print("\n" + "-" * 60)
        print("VALIDATION FLOW - Step 3: Localization Sync")
        print("-" * 60)
        
        state.current_step = "localization_sync"
        
        # Call tool directly (no agent needed for simple tool execution)
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
            state.issues_found.append(f"Localization sync failed: {str(e)}")
        
        print(f"[STRINGS] Sync: {'PASSED' if state.strings_synced else 'FAILED'}")
        return state
    
    @listen(sync_localization)
    def check_for_issues(self, state: ValidationState) -> ValidationState:
        """Step 4: Pattern-based issue detection - Pure Python.
        
        Detects:
        - Critical build failures
        - Data integrity issues
        - Security concerns
        """
        print("\n" + "-" * 60)
        print("[MANAGER] Analyzing validation outputs for issues...")
        print("-" * 60)
        
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
        
        # Check for data integrity issues
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
        
        # Check for security patterns
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
        """Step 5: Route to report (escalation disabled, always proceeds)."""
        if state.needs_human_review:
            print("[ROUTER] Critical issues detected but auto-proceeding (HITL disabled)")
            print(f"[ROUTER] Issues logged: {len(state.critical_issues)}")
            # Auto-resolve: log and continue to report
            state.needs_human_review = False
        print("[ROUTER] -> report")
        return "report"
    
    @listen("report")
    def generate_report(self, state: ValidationState) -> ValidationState:
        """Step 6: Generate final validation report - Pure Python."""
        print("\n" + "-" * 60)
        print("[REPORT] Generating validation report...")
        print("-" * 60)
        
        state.current_step = "complete"
        
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
        
        return state
