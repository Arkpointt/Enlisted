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
from typing import List, Optional

from crewai import Agent, Crew, Process, Task, LLM
from crewai.flow.flow import Flow, listen, router, start
from pydantic import BaseModel, Field

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


# === LLM Configuration ===

def _get_env(name: str, default: str) -> str:
    return os.environ.get(name, default)


# MEDIUM reasoning for QA - needs to catch issues
GPT5_QA = LLM(
    model=_get_env("ENLISTED_LLM_QA", "gpt-5.2"),
    max_completion_tokens=6000,
    reasoning_effort="medium",
)

# NONE reasoning for fast validation - just run tools
GPT5_FAST = LLM(
    model=_get_env("ENLISTED_LLM_FAST", "gpt-5.2"),
    max_completion_tokens=4000,
    reasoning_effort="none",
)


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
            backstory="""You lead a QA team that validates all changes before release.
            Your responsibilities:
            1. Delegate content validation (JSON schemas, strings)
            2. Delegate build validation (compilation, no warnings)
            3. Coordinate final QA report
            4. Ensure no issues are missed
            
            You ensure all validation passes before approving.""",
            llm=GPT5_QA,
            allow_delegation=True,  # REQUIRED for manager
            verbose=True,
            max_iter=20,
            max_retry_limit=3,
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
    2. run_validation_crew - Single hierarchical crew (content → build → strings → report)
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
        
        Manager coordinates: content → build → report
        """
        print("\n" + "-" * 60)
        print("[VALIDATION CREW] Running hierarchical validation...")
        print("-" * 60)
        
        # Task 1: Content Validation
        content_task = Task(
            description="""
            Validate all JSON content files in the Enlisted mod.
            
            WORKFLOW:
            1. Call validate_content tool to run full validation
            2. Report any schema errors, missing fields, or invalid values
            
            OUTPUT: List of validation errors or "All content valid"
            """,
            expected_output="Content validation results with pass/fail",
            agent=get_content_validator(),
        )
        
        # Task 2: Build Validation
        build_task = Task(
            description="""
            Run dotnet build to verify C# code compiles.
            
            WORKFLOW:
            1. Call build tool to run compilation
            2. Report any build errors or warnings
            
            OUTPUT: Build result (success/failure with errors)
            """,
            expected_output="Build validation results with pass/fail",
            agent=get_build_validator(),
        )
        
        # Task 3: Generate QA Report
        report_task = Task(
            description="""
            Produce a clear validation report summarizing all checks.
            
            Compile results from content validation and build validation.
            List any issues found and provide overall pass/fail status.
            """,
            expected_output="Final QA report with overall status",
            agent=get_qa_reporter(),
            context=[content_task, build_task],
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
    def generate_report(self, state: ValidationState) -> ValidationState:
        """Step 4: Generate final validation report."""
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
