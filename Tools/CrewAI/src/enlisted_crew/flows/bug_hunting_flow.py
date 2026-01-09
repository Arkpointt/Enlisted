"""
Bug Hunting Flow - Single-agent Crews per Flow step.

Refactored from multi-agent Crew to separate Flow steps following CrewAI best practices.
Each step either creates a single-agent Crew or uses pure Python.

Key Pattern: Access state via self.state, not as parameter.
Based on: https://docs.crewai.com/en/guides/flows/mastering-flow-state
"""

import os
from pathlib import Path
from typing import Tuple, Any

from crewai import Agent, Crew, Process, Task, LLM
from crewai.flow.flow import Flow, listen, router, start
from crewai.tasks.task_output import TaskOutput

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

from .state_models import (
    BugHuntingState,
    BugReport,
    BugSeverity,
    ConfidenceLevel,
    Investigation,
    SystemsAnalysis,
    FixProposal,
    ValidationResult,
)

# Import condition functions
from .conditions import (
    needs_systems_analysis,
    is_simple_bug,
    is_critical_bug,
    has_high_confidence,
    format_routing_decision,
)

from ..memory_config import get_memory_config
from ..monitoring import EnlistedExecutionMonitor

# Import tools
from ..tools import (
    SearchCache,
    # Debug & Logs
    read_debug_logs_tool,
    search_debug_logs_tool,
    # Source Code
    read_source,
    read_source_section,
    # Database Tools
    lookup_error_code,
    lookup_api_pattern,
    get_system_dependencies,
    lookup_core_system,
    get_game_systems,
    # Validation & Build
    build,
)

# Import RAG tools
from ..rag.codebase_rag_tool import search_codebase
from ..tools.docs_tools import search_docs_semantic

# Import prompt templates
from ..prompts import (
    ARCHITECTURE_PATTERNS,
    BUG_INVESTIGATION_WORKFLOW,
    CODE_STYLE_RULES,
    TOOL_EFFICIENCY_RULES,
)


def get_project_root() -> Path:
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

def validate_fix_is_minimal(output: TaskOutput) -> Tuple[bool, Any]:
    """Ensure bug fix is minimal and targeted."""
    content = str(output.raw).lower()
    
    rewrite_indicators = [
        "complete rewrite",
        "rewrite the entire",
        "redesign the",
        "refactor everything",
        "replace the whole",
    ]
    for indicator in rewrite_indicators:
        if indicator in content:
            return (False, f"Bug fix proposes a '{indicator}'. Bug fixes should be minimal and targeted.")
    
    return (True, output)


def validate_fix_has_code(output: TaskOutput) -> Tuple[bool, Any]:
    """Ensure fix proposal includes actual code."""
    content = str(output.raw)
    
    has_csharp = "```csharp" in content.lower() or "```c#" in content.lower()
    
    if not has_csharp:
        return (False, "Fix proposal must include actual code changes in ```csharp code blocks.")
    
    return (True, output)


def validate_fix_explains_root_cause(output: TaskOutput) -> Tuple[bool, Any]:
    """Ensure fix explains the root cause."""
    content = str(output.raw).lower()
    
    root_cause_indicators = [
        "root cause",
        "because",
        "the issue is",
        "the bug occurs",
        "the problem is",
        "caused by",
    ]
    has_explanation = any(ind in content for ind in root_cause_indicators)
    
    if not has_explanation:
        return (False, "Fix proposal must explain the root cause. Add a 'Root Cause:' section.")
    
    return (True, output)


# =============================================================================
# LLM CONFIGURATIONS
# =============================================================================

GPT5_ARCHITECT = LLM(model="gpt-5.2", max_completion_tokens=16000, reasoning_effort="high")
GPT5_ANALYST = LLM(model="gpt-5.2", max_completion_tokens=12000, reasoning_effort="medium")
GPT5_IMPLEMENTER = LLM(model="gpt-5.2", max_completion_tokens=12000, reasoning_effort="low")


# =============================================================================
# BUG HUNTING FLOW - Single-agent pattern
# =============================================================================

class BugHuntingFlow(Flow[BugHuntingState]):
    """
    Single-agent Flow implementation following CrewAI best practices.
    
    Pattern: Each step accesses state via self.state (not parameter).
    Routers return string route names, listeners use self.state.
    
    Refactored from multi-agent Crew (4 agents → 8 Flow steps).
    """
    
    initial_state = BugHuntingState
    
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self._monitor = EnlistedExecutionMonitor()
        print("[MONITORING] Execution monitoring enabled")
    
    @start()
    def load_bug_report(self):
        """Step 1: Load bug report (pure Python)."""
        SearchCache.clear()
        
        print("\n" + "="*60)
        print("BUG HUNTING FLOW STARTED")
        print("="*60)
        
        # Build BugReport from flat state inputs
        br = BugReport(
            description=self.state.bug_description or "No description provided",
            error_codes=self.state.error_codes or "none",
            repro_steps=self.state.repro_steps or "unknown",
            context=self.state.context,
            user_log_content=self.state.user_log_content,
        )
        br.has_user_logs = bool(br.user_log_content and br.user_log_content.strip())
        
        print(f"\nBug Report Received:")
        print(f"   Description: {br.description[:100]}...")
        print(f"   Error Codes: {br.error_codes}")
        print(f"   User Logs: {'[OK] Provided' if br.has_user_logs else '[X] Not provided'}")
        
        self.state.bug_report = br
        self.state.current_step = "investigate"
    
    @listen(load_bug_report)
    def investigate_bug(self):
        """Step 2: Investigate bug (single-agent Crew)."""
        print("\n" + "-"*60)
        print("[STEP 2/8] Investigating bug...")
        print("-"*60)
        
        br = self.state.bug_report
        
        # Build investigation description
        if br.has_user_logs:
            log_content = br.user_log_content or ""
            if len(log_content) > 50000:
                log_content = log_content[:50000] + "\n... [TRUNCATED]"
            
            investigation_desc = f"""{ARCHITECTURE_PATTERNS}
{BUG_INVESTIGATION_WORKFLOW}
{TOOL_EFFICIENCY_RULES}

=== YOUR TASK ===
Investigate bug using PROVIDED LOG CONTENT.

**Bug Information:**
- BUG DESCRIPTION: {br.description}
- ERROR CODES: {br.error_codes}
- REPRO STEPS: {br.repro_steps}

**LOG CONTENT:**
{log_content[:20000]}

**Expected Output:**
- Severity assessment (critical/high/medium/low)
- Root cause explanation
- Affected files with line numbers
- Confidence level (high/medium/low)
"""
        else:
            investigation_desc = f"""{ARCHITECTURE_PATTERNS}
{BUG_INVESTIGATION_WORKFLOW}
{TOOL_EFFICIENCY_RULES}

=== YOUR TASK ===
Investigate bug by analyzing CODE PATHS (no logs provided).

**Bug Information:**
- BUG DESCRIPTION: {br.description}
- REPRO STEPS: {br.repro_steps}
- CONTEXT: {br.context or "None"}

**Expected Output:**
- Severity assessment (critical/high/medium/low)
- Root cause explanation
- Affected files with line numbers
- Confidence level (high/medium/low)
"""
        
        agent = Agent(
            role="Bug Investigator",
            goal="Find root cause of bug using logs or code analysis",
            backstory="You investigate bugs efficiently using tools.",
            llm=GPT5_ANALYST,
            tools=[search_debug_logs_tool, search_codebase, search_docs_semantic, lookup_error_code, read_source_section],
            max_iter=8,
            allow_delegation=False,
            verbose=True,
        )
        
        task = Task(
            description=investigation_desc,
            expected_output="Investigation report with severity, root cause, affected files",
            agent=agent,
        )
        
        crew = Crew(
            agents=[agent],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
            **get_memory_config(),
        )
        
        result = crew.kickoff()
        raw_output = str(result)
        
        # Parse investigation results
        severity = BugSeverity.MEDIUM
        output_lower = raw_output.lower()
        if "critical" in output_lower:
            severity = BugSeverity.CRITICAL
        elif "high" in output_lower:
            severity = BugSeverity.HIGH
        elif "low" in output_lower:
            severity = BugSeverity.LOW
        
        self.state.investigation = Investigation(
            summary=raw_output[:500],
            root_cause=None,
            error_codes_found=[],
            severity=severity,
            confidence=ConfidenceLevel.LIKELY if br.has_user_logs else ConfidenceLevel.SPECULATIVE,
            needs_systems_analysis="multi-system" in output_lower or "complex" in output_lower,
            raw_output=raw_output,
        )
        
        print(f"[INVESTIGATION] Severity: {severity.value}, Confidence: {self.state.investigation.confidence.value}")
    
    @router(investigate_bug)
    def route_complexity(self):
        """Step 3: Router - decide if systems analysis needed."""
        if needs_systems_analysis(self.state):
            print(format_routing_decision(
                condition_name="needs_systems_analysis",
                condition_result=True,
                chosen_path="analyze_systems",
                reason=f"Severity: {self.state.investigation.severity.value}, needs multi-system analysis"
            ))
            return "analyze_systems"
        
        print(format_routing_decision(
            condition_name="needs_systems_analysis",
            condition_result=False,
            chosen_path="propose_fix",
            reason="Simple bug, skip systems analysis"
        ))
        self.state.skipped_steps.append("analyze_systems")
        return "propose_fix"
    
    @listen("analyze_systems")
    def analyze_systems(self):
        """Step 4: Analyze systems (single-agent Crew, conditional)."""
        print("\n" + "-"*60)
        print("[STEP 4/8] Analyzing related systems...")
        print("-"*60)
        
        agent = Agent(
            role="Systems Analyst",
            goal="Analyze system dependencies and integration risks",
            backstory="You analyze system relationships efficiently.",
            llm=GPT5_ARCHITECT,
            tools=[get_system_dependencies, lookup_core_system, get_game_systems],
            max_iter=5,
            allow_delegation=False,
            verbose=True,
        )
        
        task = Task(
            description=f"""{ARCHITECTURE_PATTERNS}
{TOOL_EFFICIENCY_RULES}

=== YOUR TASK ===
Analyze systems related to this bug.

**Investigation Summary:**
{self.state.investigation.raw_output[:2000]}

**Expected Output:**
- Scope assessment (localized/multi-system/architecture)
- Related systems and dependencies
- Risk level (low/medium/high/critical)
""",
            expected_output="Systems analysis with scope and risk assessment",
            agent=agent,
        )
        
        crew = Crew(
            agents=[agent],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
            **get_memory_config(),
        )
        
        result = crew.kickoff()
        
        self.state.systems_analysis = SystemsAnalysis(
            scope="multi-system",
            affected_systems=[],
            dependencies=[],
            integration_concerns=[],
            risk_level="medium",
            raw_output=str(result),
        )
        
        print("[SYSTEMS] Analysis complete")
    
    @listen(route_complexity)
    def propose_fix(self):
        """Step 5: Propose fix (single-agent Crew)."""
        print("\n" + "-"*60)
        print("[STEP 5/8] Proposing fix...")
        print("-"*60)
        
        agent = Agent(
            role="Fix Implementer",
            goal="Propose minimal fix following Enlisted patterns",
            backstory="You create targeted bug fixes efficiently.",
            llm=GPT5_IMPLEMENTER,
            tools=[read_source, search_codebase, search_docs_semantic, lookup_api_pattern],
            max_iter=8,
            allow_delegation=False,
            verbose=True,
        )
        
        context_summary = self.state.investigation.raw_output[:3000]
        if self.state.systems_analysis:
            context_summary += "\n\n" + self.state.systems_analysis.raw_output[:1000]
        
        task = Task(
            description=f"""{ARCHITECTURE_PATTERNS}
{CODE_STYLE_RULES}
{TOOL_EFFICIENCY_RULES}

=== YOUR TASK ===
Propose a MINIMAL fix based on investigation.

**Context:**
{context_summary}

**Fix Requirements:**
- MINIMAL fix addressing root cause only
- Follow Enlisted style: Allman braces, _camelCase, XML docs
- Include code in ```csharp blocks
- Explain root cause

**Expected Output:**
- Root Cause: [why bug occurs]
- Fix Code: [in ```csharp blocks]
- Files Modified: [specific paths]
- Risks: [potential side effects]
""",
            expected_output="Fix proposal with code changes and explanation",
            agent=agent,
        )
        
        # Add guardrails
        task.guardrails = [validate_fix_is_minimal, validate_fix_has_code, validate_fix_explains_root_cause]
        task.guardrail_max_retries = 2
        
        crew = Crew(
            agents=[agent],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
            **get_memory_config(),
        )
        
        result = crew.kickoff()
        
        self.state.fix_proposal = FixProposal(
            summary=str(result)[:500],
            changes=[],
            explanation="",
            risks=[],
            testing_notes="",
            raw_output=str(result),
        )
        
        print("[FIX] Proposal complete")
    
    @listen(propose_fix)
    def validate_fix(self):
        """Step 6: Validate fix (pure Python)."""
        print("\n" + "-"*60)
        print("[STEP 6/8] Validating fix...")
        print("-"*60)
        
        build_result = build.run()
        
        build_passed = "succeeded" in build_result.lower() or "0 error" in build_result.lower()
        
        self.state.validation = ValidationResult(
            approved=build_passed,
            build_status="pass" if build_passed else "fail",
            style_issues=[],
            concerns=[],
            recommendations=[],
            raw_output=build_result,
        )
        
        print(f"[BUILD] {'✓ Succeeded' if build_passed else '✗ Failed'}")
    
    @listen(validate_fix)
    def check_for_issues(self):
        """Step 7: Check for issues (pure Python)."""
        print("\n" + "-"*60)
        print("[STEP 7/8] Checking for issues...")
        print("-"*60)
        
        issues = []
        
        # Get all outputs
        investigation_output = self.state.investigation.raw_output.lower() if self.state.investigation else ""
        fix_output = self.state.fix_proposal.raw_output.lower() if self.state.fix_proposal else ""
        combined_output = investigation_output + fix_output
        
        # Check for scope creep
        scope_creep_patterns = [
            "refactor entire",
            "complete rewrite",
            "also fixed",
            "while we're at it",
        ]
        for pattern in scope_creep_patterns:
            if pattern in combined_output:
                issues.append(DetectedIssue(
                    issue_type=IssueType.SCOPE_CREEP,
                    severity=IssueSeverity.HIGH,
                    confidence=IssueConfidence.MEDIUM,
                    description="Bug fix may include scope creep",
                    affected_component="Fix Proposal",
                    evidence=f"Pattern '{pattern}' found",
                    manager_recommendation="Review fix scope",
                    auto_fixable=False,
                ))
                break
        
        # Check for breaking changes
        breaking_patterns = [
            "breaking change",
            "backwards incompatible",
            "save migration",
        ]
        for pattern in breaking_patterns:
            if pattern in combined_output:
                issues.append(DetectedIssue(
                    issue_type=IssueType.BREAKING_CHANGE,
                    severity=IssueSeverity.CRITICAL,
                    confidence=IssueConfidence.MEDIUM,
                    description="Bug fix may introduce breaking changes",
                    affected_component="Fix Proposal",
                    evidence=f"Pattern '{pattern}' found",
                    manager_recommendation="Review compatibility",
                    auto_fixable=False,
                ))
                break
        
        self.state.detected_issues = issues
        
        if issues:
            print(f"[ISSUES] Detected {len(issues)} potential issues")
            if should_escalate_to_human(issues):
                print("[ESCALATION] Human review recommended")
                self.state.escalation_message = create_escalation_message(issues, self.state)
        else:
            print("[ISSUES] No issues detected")
    
    @listen(check_for_issues)
    def generate_report(self):
        """Step 8: Generate final report (pure Python)."""
        print("\n" + "-"*60)
        print("[REPORT] Generating final report...")
        print("-"*60)
        
        report_parts = [
            "=" * 60,
            "BUG HUNTING REPORT",
            "=" * 60,
            "",
            f"Bug: {self.state.bug_report.description[:100]}...",
            "",
            "## Investigation",
            self.state.investigation.raw_output[:1000] if self.state.investigation else "N/A",
            "",
        ]
        
        if self.state.systems_analysis:
            report_parts.extend([
                "## Systems Analysis",
                self.state.systems_analysis.raw_output[:1000],
                "",
            ])
        
        if self.state.fix_proposal:
            report_parts.extend([
                "## Fix Proposal",
                self.state.fix_proposal.raw_output[:2000],
                "",
            ])
        
        if self.state.validation:
            report_parts.extend([
                "## Validation",
                f"Build Status: {self.state.validation.build_status}",
                f"Approved: {self.state.validation.approved}",
                "",
            ])
        
        if self.state.detected_issues:
            report_parts.extend([
                "## Issues Detected",
                format_critical_issues(self.state.detected_issues),
                "",
            ])
        
        report_parts.extend([
            f"Skipped Steps: {', '.join(self.state.skipped_steps) or 'None'}",
            "",
            "=" * 60,
        ])
        
        self.state.final_report = "\n".join(report_parts)
        self.state.success = self.state.validation.approved if self.state.validation else False
        
        print("\n" + "="*60)
        print("BUG HUNTING FLOW COMPLETE")
        print("="*60)
