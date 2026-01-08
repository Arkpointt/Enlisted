"""
Bug Hunting Flow - CrewAI Flow-based workflow for bug investigation.

This Flow orchestrates multiple agents to investigate bugs, analyze systems,
propose fixes, and validate solutions. Uses state management for clean data
passing and conditional routing based on bug severity.

Usage:
    from enlisted_crew.flows import BugHuntingFlow, BugReport
    
    flow = BugHuntingFlow()
    result = flow.kickoff(inputs={
        "description": "Player can't move after leaving lord",
        "error_codes": "none",
        "repro_steps": "left my lord"
    })
"""

import os
import re
from pathlib import Path
from typing import Optional, Tuple, Any

from crewai import Agent, Crew, Process, Task, LLM
from crewai.flow.flow import Flow, listen, router, start, or_
from crewai.tasks.task_output import TaskOutput
from crewai.tasks.conditional_task import ConditionalTask
from pydantic import BaseModel

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
    AffectedFile,
    CodeChange,
)

# Import condition functions
from .conditions import (
    needs_systems_analysis,
    is_simple_bug,
    is_critical_bug,
    has_high_confidence,
    has_user_logs,
    format_routing_decision,
)

from ..memory_config import get_memory_config
from ..monitoring import EnlistedExecutionMonitor

# Import tools from our tools module
from ..tools import (
    SearchCache,
    # Debug & Logs
    read_debug_logs_tool,
    search_debug_logs_tool,
    read_native_crash_logs_tool,
    # Source Code
    find_in_code,  # Was: search_csharp_tool
    read_source,  # Was: read_csharp_tool
    read_source_section,  # Was: read_csharp_snippet_tool
    # Documentation
    read_doc_tool,
    find_in_docs,  # Was: search_docs_tool
    list_feature_files_tool,
    # Context Loaders (use get_dev_reference instead of load_code_context_tool)
    get_dev_reference,
    get_game_systems,  # Was: load_domain_context_tool
    # Native API (deprecated, agents should use MCP server instead)
    find_in_native_api,  # Was: search_native_api_tool
    # Validation & Build
    build,  # Was: run_build_tool
    validate_content,  # Was: validate_content_tool
    # Code Review
    review_code,  # Was: check_code_style_tool
    check_game_patterns,  # Was: check_bannerlord_patterns_tool
    # Verification
    verify_file_exists_tool,
    # Database Tools (for bug investigation)
    lookup_error_code,  # Find known error patterns
    lookup_api_pattern,  # Find correct Bannerlord API usage
    get_system_dependencies,  # Understand system interactions
    lookup_core_system,  # Identify which core systems are affected
    lookup_content_id,  # Check if content IDs exist when debugging content bugs
    search_content,  # Search for related content when investigating
    get_balance_value,  # Check balance values when debugging gameplay issues
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
    """Ensure bug fix is minimal and targeted.
    
    Bug fixes should be surgical, not rewrites. This prevents scope creep.
    """
    content = str(output.raw).lower()
    
    # Reject if proposing a complete rewrite
    rewrite_indicators = [
        "complete rewrite",
        "rewrite the entire",
        "redesign the",
        "refactor everything",
        "replace the whole",
    ]
    for indicator in rewrite_indicators:
        if indicator in content:
            return (False, f"Bug fix proposes a '{indicator}'. Bug fixes should be minimal and targeted. Focus on the specific issue.")
    
    return (True, output)


def validate_fix_has_code(output: TaskOutput) -> Tuple[bool, Any]:
    """Ensure fix proposal includes actual code.
    
    A fix proposal without code is just analysis, not a solution.
    """
    content = str(output.raw)
    
    # Check for code blocks
    has_csharp = "```csharp" in content.lower() or "```c#" in content.lower()
    has_inline_code = re.search(r'`[^`]+`', content) is not None
    
    if not has_csharp and not has_inline_code:
        return (False, "Fix proposal must include actual code changes. Add the fix in ```csharp code blocks.")
    
    return (True, output)


def validate_fix_explains_root_cause(output: TaskOutput) -> Tuple[bool, Any]:
    """Ensure fix explains the root cause.
    
    Understanding why helps validate the fix is correct.
    """
    content = str(output.raw).lower()
    
    # Check for root cause explanation
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
        return (False, "Fix proposal must explain the root cause. Add a 'Root Cause:' section explaining why this bug occurs.")
    
    return (True, output)


def validate_investigation_has_evidence(output: TaskOutput) -> Tuple[bool, Any]:
    """Ensure investigation cites actual evidence from tools.
    
    Investigations should be grounded in actual log/code findings, not speculation.
    """
    content = str(output.raw).lower()
    
    # Check for evidence indicators
    evidence_indicators = [
        "log shows",
        "found in",
        "line ",
        "file:",
        ".cs",
        "error code",
        "stack trace",
        "method",
    ]
    has_evidence = sum(1 for ind in evidence_indicators if ind in content) >= 2
    
    if not has_evidence:
        return (False, "Investigation must cite evidence from logs or code. Reference specific files, line numbers, or error codes.")
    
    return (True, output)


# === LLM Configurations (OpenAI GPT-5 family) ===
# Unified with main crew.py - all workflows use GPT-5 models.
# Override via ENLISTED_LLM_* env vars if needed.

import os as _os

def _get_env(name: str, default: str) -> str:
    return _os.environ.get(name, default)

# =============================================================================
# LLM TIERS - reasoning_effort optimizes cost/performance
# high=deep thinking | medium=balanced | low=quick | none=instant
# =============================================================================

# MEDIUM reasoning - bug analysis needs careful thought
GPT5_ANALYSIS = LLM(
    model=_get_env("ENLISTED_LLM_ANALYST", "gpt-5.2"),
    max_completion_tokens=12000,  # Increased for detailed analysis
    reasoning_effort="medium",
)

# HIGH reasoning - deep system analysis for complex bugs
GPT5_DEEP = LLM(
    model=_get_env("ENLISTED_LLM_ARCHITECT", "gpt-5.2"),
    max_completion_tokens=16000,
    reasoning_effort="high",
)

# LOW reasoning - fix implementation from clear specs
GPT5_EXECUTE = LLM(
    model=_get_env("ENLISTED_LLM_IMPLEMENTER", "gpt-5.2"),
    max_completion_tokens=12000,  # Increased for comprehensive fixes
    reasoning_effort="low",
)

# MEDIUM reasoning - QA validation needs to catch issues
GPT5_QA = LLM(
    model=_get_env("ENLISTED_LLM_QA", "gpt-5.2"),
    max_completion_tokens=8000,  # Increased for validation reports
    reasoning_effort="medium",
)

# Planning LLM - use simple string (LLM objects with reasoning_effort cause issues with AgentPlanner)
# See: https://docs.crewai.com/en/concepts/planning - examples all use simple strings
GPT5_PLANNING = _get_env("ENLISTED_LLM_PLANNING", "gpt-5.2")

# Function calling LLM - lightweight, no reasoning overhead for tool parameter extraction
# Used at Crew level for all agents' tool calls (per CrewAI docs recommendation)
GPT5_FUNCTION_CALLING = _get_env("ENLISTED_LLM_FUNCTION_CALLING", "gpt-5.2")


# Agent cache (module-level to avoid Flow initialization issues)
_agent_cache = {}


def get_bug_hunting_manager() -> Agent:
    """Manager agent that coordinates the bug hunting workflow."""
    if "bug_hunting_manager" not in _agent_cache:
        _agent_cache["bug_hunting_manager"] = Agent(
            role="Bug Triage Manager",
            goal="Efficiently investigate, fix, and validate bug reports with appropriate depth",
            backstory="""Experienced bug triage manager who coordinates investigation teams. 
            Delegates investigation to analysts, coordinates fixes with implementers, and ensures QA validation. 
            Adapts investigation depth based on bug severity.""",
            llm=GPT5_DEEP,
            allow_delegation=True,  # REQUIRED for hierarchical managers
            reasoning=True,  # Enable strategic coordination
            max_reasoning_attempts=2,  # Limit overthinking (prevent delays)
            max_iter=15,  # Reduced from default to prevent delegation delays
            max_retry_limit=3,
            verbose=True,
            respect_context_window=True,
        )
    return _agent_cache["bug_hunting_manager"]


def get_code_analyst() -> Agent:
    """Get or create code analyst agent."""
    if "code_analyst" not in _agent_cache:
        _agent_cache["code_analyst"] = Agent(
                role="C# Code Analyst",
                goal="Investigate bugs by searching logs, reading code, and tracing to root cause",
                backstory="""You investigate bugs using DATABASE TOOLS FIRST, then logs/code.

CRITICAL - DATABASE BEFORE LOGS:
1. FIRST call lookup_error_code for ANY error code mentioned (E-*, W-*)
2. Call get_dev_reference for coding patterns
3. THEN search_debug_logs_tool if error not in database
4. Call read_source_section for relevant code
5. Only THEN assess severity based on actual findings

TOOL CALL FORMAT: Each tool takes ONE argument, not arrays.
- CORRECT: lookup_error_code("E-MUSTER-042") then lookup_content_id("event_1")
- WRONG: lookup_error_code(["E-MUSTER-042", "E-UI-001"])

DO NOT write analysis without tool calls. "I think" is worthless - "The database shows" is valuable.

SEARCH EFFICIENCY: Limit to ~5-8 calls total. NEVER re-search same query.
ERROR CODES: E-ENCOUNTER-*=battle, E-SAVELOAD-*=corruption, E-CAMPUI-*=UI""",
            llm=GPT5_ANALYSIS,
            tools=[
                # Primary investigation tools
                search_debug_logs_tool,
                read_source_section,
                find_in_code,
                # Database tools (fast lookups)
                lookup_error_code,
                lookup_content_id,
                # Reference (only if needed)
                get_dev_reference,
            ],
            verbose=True,
            respect_context_window=True,
            max_iter=15,
            max_retry_limit=3,
            reasoning=True,  # Enable reflection for thorough investigation
            max_reasoning_attempts=3,  # Limit reasoning iterations
            allow_delegation=False,  # Specialists don't delegate (only managers do)
        )
    return _agent_cache["code_analyst"]


def get_systems_analyst() -> Agent:
    """Get or create systems analyst agent."""
    if "systems_analyst" not in _agent_cache:
        _agent_cache["systems_analyst"] = Agent(
                role="Systems Integration Analyst",
                goal="Analyze how systems interconnect and find related code that may have similar issues",
                backstory="""You trace system interactions using TOOLS, not memory.

CRITICAL - TOOL-FIRST WORKFLOW (execute in order):
1. IMMEDIATELY call get_game_systems - don't think, just call it
2. Call get_system_dependencies for EACH system (one call per system)
3. Call lookup_core_system to understand integration points
4. Use find_in_code only for NEW queries (previous results are cached)

TOOL CALL FORMAT: Each tool takes ONE argument, not arrays.
- CORRECT: get_system_dependencies("System1") then get_system_dependencies("System2")
- WRONG: get_system_dependencies(["System1", "System2"])

DO NOT re-investigate what Code Analyst already found. Build on their work.
Limit to ~3-5 NEW searches. Key systems: ContentOrchestrator, EnlistmentBehavior,
EscalationManager, CompanyNeedsManager.""",
            llm=GPT5_DEEP,
            tools=[
                # Database tools (fast, use first)
                get_game_systems,
                get_system_dependencies,
                lookup_core_system,
                # Code search (use after database)
                find_in_code,
                read_source_section,
            ],
            verbose=True,
            respect_context_window=True,
            max_iter=15,
            max_retry_limit=3,
            reasoning=True,  # Enable reflection for system integration analysis
            max_reasoning_attempts=3,  # Limit reasoning iterations
            allow_delegation=False,  # Specialists don't delegate (only managers do)
        )
    return _agent_cache["systems_analyst"]


def get_implementer() -> Agent:
    """Get or create C# implementer agent."""
    if "implementer" not in _agent_cache:
        _agent_cache["implementer"] = Agent(
                role="C# Implementer",
                goal="Propose minimal, correct fixes following Enlisted patterns",
                backstory="""You write production C# code for Bannerlord mods. You:
                
                1. Propose MINIMAL fixes that address root cause
                2. Follow Enlisted patterns: Allman braces, _camelCase fields, XML docs
                3. Use proper APIs: GiveGoldAction, TextObject, Campaign.Current?.GetCampaignBehavior<T>()
                4. Consider save/load compatibility
                5. Include code diffs or clear pseudocode in your OUTPUT (not as tool calls)
                
                CRITICAL PATTERNS:
                - Always check Hero.MainHero != null
                - Use TextObject for strings (not concatenation)
                - Add new files to Enlisted.csproj
                - VERIFY file paths exist before referencing them
                
                OUTPUT FORMAT:
                - Write your fix proposal as plain text with code blocks
                - DO NOT output XML or function_calls tags - just describe the fix
                - Use ```csharp code blocks for code changes
                """,
            llm=GPT5_EXECUTE,
            tools=[
                # Core fix proposal tools
                read_source,
                find_in_code,
                review_code,
                # Database lookups
                lookup_api_pattern,
            ],
            verbose=True,
            respect_context_window=True,
            max_iter=20,
            max_retry_limit=3,
            reasoning=True,  # Plan fix strategy before proposing
            max_reasoning_attempts=3,  # Limit reasoning iterations
            allow_delegation=False,  # Specialist focuses on fix proposals
        )
    return _agent_cache["implementer"]


def get_qa_agent() -> Agent:
    """Get or create QA agent."""
    if "qa_agent" not in _agent_cache:
        _agent_cache["qa_agent"] = Agent(
                role="Quality Assurance Specialist",
                goal="Validate proposed fixes compile and follow standards",
                backstory="""You are the final quality gate. You:
                
                1. Run dotnet build to verify compilation
                2. Check code style compliance (Allman braces, naming)
                3. Verify fix addresses the root cause
                4. Assess regression risk
                5. Only approve when all checks pass
                """,
            llm=GPT5_QA,
            tools=[
                build,
                validate_content,
                review_code,
            ],
            verbose=True,
            respect_context_window=True,
            max_iter=10,
            max_retry_limit=3,
            allow_delegation=False,  # Specialist focuses on QA/validation
        )
    return _agent_cache["qa_agent"]


# === Condition Functions for ConditionalTasks ===

def needs_systems_analysis_task(output: TaskOutput) -> bool:
    """Check if systems analysis is needed based on investigation output."""
    if not output or not output.raw:
        return False
    output_lower = str(output.raw).lower()
    
    # Skip indicators
    skip_indicators = [
        "no systems analysis needed", "simple fix", "straightforward fix",
        "localized issue", "single file"
    ]
    if any(indicator in output_lower for indicator in skip_indicators):
        return False
    
    # Triggers
    triggers = [
        "critical", "high", "multi-system", "complex", "integration issue",
        "state machine", "needs systems analysis"
    ]
    return any(trigger in output_lower for trigger in triggers)


class BugHuntingFlow(Flow[BugHuntingState]):
    """
    Flow-based bug hunting workflow with hierarchical process.
    
    Uses a single hierarchical crew with ConditionalTask for systems analysis.
    
    State Persistence: Enabled via persist=True. If a run fails, you can resume
    from the last successful step by re-running with the same inputs.
    
    Steps:
    1. receive_bug_report - Parse and validate input
    2. run_bug_hunting_crew - Single hierarchical crew (investigate -> analyze (conditional) -> fix -> validate)
    3. generate_report - Create final report
    """
    
    initial_state = BugHuntingState
    persist = True  # Auto-save state to SQLite for recovery on failure
    
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        # Initialize execution monitoring
        self._monitor = EnlistedExecutionMonitor()
        print("[MONITORING] Execution monitoring enabled for BugHuntingFlow")
    
    # === Flow Steps ===
    
    @start()
    def receive_bug_report(self) -> BugHuntingState:
        """
        Entry point: Read kickoff inputs mapped into state and normalize BugReport.
        
        Inputs are passed via kickoff(inputs={...}) and populate self.state.
        """
        # Clear search cache from any previous runs
        SearchCache.clear()
        
        print("\n" + "="*60)
        print("BUG HUNTING FLOW STARTED")
        print("="*60)
        
        # Access state - inputs populate flat fields (bug_description, error_codes, etc.)
        state = self.state
        
        # Build BugReport from flat state inputs
        br = BugReport(
            description=state.bug_description or "No description provided",
            error_codes=state.error_codes or "none",
            repro_steps=state.repro_steps or "unknown",
            context=state.context,
            user_log_content=state.user_log_content,
        )
        # Normalize has_user_logs
        br.has_user_logs = bool(br.user_log_content and br.user_log_content.strip())
        
        print(f"\nBug Report Received:")
        print(f"   Description: {br.description[:100]}...")
        print(f"   Error Codes: {br.error_codes}")
        print(f"   Repro Steps: {br.repro_steps}")
        print(f"   User Logs: {'[OK] Provided' if br.has_user_logs else '[X] Not provided (will analyze code paths)'}")
        
        # Update state with constructed BugReport and return
        state.bug_report = br
        state.current_step = "investigate"
        return state
    
    @listen(receive_bug_report)
    def run_bug_hunting_crew(self, state: BugHuntingState) -> BugHuntingState:
        """Single hierarchical crew for bug hunting.
        
        Manager coordinates: investigate -> analyze (conditional) -> fix -> validate
        """
        print("\n" + "-"*60)
        print("[BUG HUNTING CREW] Running hierarchical investigation...")
        print("-"*60)
        
        br = state.bug_report
        
        # Build investigation description based on log availability
        if br.has_user_logs:
            log_content = br.user_log_content or ""
            max_log_chars = 50000
            if len(log_content) > max_log_chars:
                log_content = log_content[:max_log_chars] + "\n... [TRUNCATED]"
            investigation_desc = f"""
Investigate bug using PROVIDED LOG CONTENT.

BUG DESCRIPTION: {br.description}
ERROR CODES: {br.error_codes}
REPRO STEPS: {br.repro_steps}

LOG CONTENT:
{log_content[:20000]}

WORKFLOW (execute in order):
1. FIRST: Search LOG CONTENT above for error patterns
2. If error codes present: call lookup_error_code for EACH code
3. Call read_source_section to see relevant code near errors
4. Assess severity and identify root cause

TOOL EFFICIENCY RULES:
- Limit to ~5-8 tool calls total
- DO NOT re-search the same query - results are cached
- Use lookup_error_code BEFORE searching logs for error patterns
- Extract info from LOG CONTENT above FIRST

OUTPUT: Severity assessment + root cause + affected files
"""
        else:
            investigation_desc = f"""
Investigate bug by analyzing CODE PATHS (no logs provided).

BUG DESCRIPTION: {br.description}
REPRO STEPS: {br.repro_steps}
CONTEXT: {br.context or "None"}

WORKFLOW (execute in order):
1. FIRST: call get_dev_reference to get coding patterns
2. Call lookup_error_code if any error codes mentioned
3. Use find_in_code to locate relevant code (max 3 searches)
4. Call read_source_section for each relevant file
5. Assess severity and identify root cause

TOOL EFFICIENCY RULES:
- Limit to ~6-10 tool calls total
- DO NOT re-search the same query - results are cached
- Use database tools (lookup_*) BEFORE code search tools
- Each search should have a DIFFERENT query string

OUTPUT: Severity assessment + root cause + affected files
"""
        
        # Task 1: Investigation
        investigate_task = Task(
            description=investigation_desc,
            expected_output="Investigation report with severity, root cause, affected files",
            agent=get_code_analyst(),
        )
        
        # Task 2: Systems Analysis (CONDITIONAL - only for complex bugs)
        systems_task = ConditionalTask(
            description="""
Analyze systems related to this bug. Build on investigation findings - DO NOT re-search.

WORKFLOW (execute in order):
1. FIRST: call get_game_systems to load system context
2. Call get_system_dependencies for EACH affected system (one per call)
3. Call lookup_core_system for integration points
4. Only use find_in_code for NEW queries not in investigation

TOOL EFFICIENCY RULES:
- Limit to ~4-6 tool calls total
- DO NOT re-search what Code Analyst already found
- Previous results are cached - no benefit to re-searching

OUTPUT: Scope assessment + related systems + risk level
""",
            expected_output="Systems analysis with scope, related systems, risk assessment",
            condition=needs_systems_analysis_task,
            agent=get_systems_analyst(),
            context=[investigate_task],
        )
        
        # Task 3: Propose Fix
        fix_task = Task(
            description="""
Propose a MINIMAL fix for this bug based on investigation findings.

WORKFLOW (execute in order):
1. FIRST: Review investigation context - root cause is already identified
2. Call verify_file_exists_tool for files you'll modify
3. Call read_source_section to see current code (if not in context)
4. Propose fix in ```csharp blocks
5. Call review_code on your proposed fix

TOOL EFFICIENCY RULES:
- Limit to ~4-6 tool calls total
- DO NOT re-search code - use investigation context above
- Only call read_source if file content not in context

FIX REQUIREMENTS:
- MINIMAL fix addressing root cause only
- Follow Enlisted style: Allman braces, _camelCase, XML docs
- Consider save/load compatibility
- Include code in ```csharp blocks

OUTPUT: Root cause explanation + fix code + testing notes
""",
            expected_output="Fix proposal with code changes and explanation",
            agent=get_implementer(),
            context=[investigate_task, systems_task],
            guardrails=[validate_fix_is_minimal, validate_fix_has_code, validate_fix_explains_root_cause],
            guardrail_max_retries=2,
        )
        
        # Task 4: Validate Fix
        validate_task = Task(
            description="""
Validate the proposed bug fix from context above.

WORKFLOW (execute in order):
1. Call build to verify compilation
2. Call review_code on the proposed fix
3. Verify fix addresses root cause from investigation
4. Assess regression risk

TOOL EFFICIENCY RULES:
- Limit to ~3-4 tool calls total
- DO NOT re-search code - fix is in context above

OUTPUT: Build status + style compliance + approval decision
""",
            expected_output="Validation report with build status and approval",
            agent=get_qa_agent(),
            context=[fix_task],
        )
        
        # Single hierarchical crew
        crew = Crew(
            agents=[
                get_code_analyst(),
                get_systems_analyst(),
                get_implementer(),
                get_qa_agent(),
            ],
            tasks=[investigate_task, systems_task, fix_task, validate_task],
            manager_agent=get_bug_hunting_manager(),
            process=Process.hierarchical,
            verbose=True,
            **get_memory_config(),  # memory=True + contextual retrieval
            cache=True,
            planning=True,
            planning_llm=GPT5_PLANNING,
            function_calling_llm=GPT5_FUNCTION_CALLING,  # Lightweight LLM for tool calls
        )
        
        result = crew.kickoff()
        raw_output = str(result)
        
        # Populate state from results
        state.investigation = Investigation(
            summary=raw_output[:500],
            root_cause=None,
            error_codes_found=[],
            severity=self._assess_severity_from_output(raw_output),
            confidence=ConfidenceLevel.LIKELY if br.has_user_logs else ConfidenceLevel.SPECULATIVE,
            needs_systems_analysis=False,
            raw_output=raw_output,
        )
        
        state.fix_proposal = FixProposal(
            summary=raw_output[:500],
            changes=[],
            explanation="",
            risks=[],
            testing_notes="",
            raw_output=raw_output,
        )
        
        state.validation = ValidationResult(
            approved="approved" in raw_output.lower() or "pass" in raw_output.lower(),
            build_status="pass" if "build: pass" in raw_output.lower() else "not_run",
            style_issues=[],
            concerns=[],
            recommendations=[],
            raw_output=raw_output,
        )
        
        state.current_step = "report"
        return state
    
    @listen(run_bug_hunting_crew)
    def check_for_issues(self, state: BugHuntingState) -> BugHuntingState:
        """Manager analyzes outputs and decides if human review needed.
        
        Detects:
        - Scope creep on fixes
        - Breaking changes
        - Security concerns
        """
        print("\n" + "-"*60)
        print("[MANAGER] Analyzing bug hunting outputs for issues...")
        print("-"*60)
        
        issues = []
        
        # Get all outputs
        investigation_output = state.investigation.raw_output.lower() if state.investigation else ""
        fix_output = state.fix_proposal.raw_output.lower() if state.fix_proposal else ""
        combined_output = investigation_output + fix_output
        
        # Check for scope creep on fixes
        scope_creep_patterns = [
            "refactor entire",
            "complete rewrite",
            "also fixed",
            "bonus fix",
            "while we're at it",
            "additional changes",
        ]
        for pattern in scope_creep_patterns:
            if pattern in combined_output:
                issues.append(DetectedIssue(
                    issue_type=IssueType.SCOPE_CREEP,
                    severity=IssueSeverity.HIGH,
                    confidence=IssueConfidence.MEDIUM,
                    description="Bug fix may include scope creep beyond original issue",
                    affected_component="Fix Proposal",
                    evidence=f"Pattern '{pattern}' found in fix proposal",
                    manager_recommendation="Review fix to ensure it only addresses the bug, not additional changes",
                    auto_fixable=False,
                ))
                break
        
        # Check for breaking changes
        breaking_patterns = [
            "breaking change",
            "backwards incompatible",
            "save migration",
            "schema change",
            "api change",
        ]
        for pattern in breaking_patterns:
            if pattern in combined_output:
                issues.append(DetectedIssue(
                    issue_type=IssueType.BREAKING_CHANGE,
                    severity=IssueSeverity.CRITICAL,
                    confidence=IssueConfidence.MEDIUM,
                    description="Bug fix may introduce breaking changes",
                    affected_component="Fix Proposal",
                    evidence=f"Pattern '{pattern}' found in fix proposal",
                    manager_recommendation="Review save/load compatibility and migration strategy",
                    auto_fixable=False,
                ))
                break
        
        # Check for security concerns
        security_patterns = [
            "security risk",
            "vulnerability",
            "exploit",
            "injection",
            "unsafe",
            "unvalidated input",
        ]
        for pattern in security_patterns:
            if pattern in combined_output:
                issues.append(DetectedIssue(
                    issue_type=IssueType.SECURITY_CONCERN,
                    severity=IssueSeverity.CRITICAL,
                    confidence=IssueConfidence.HIGH,
                    description="Bug or fix involves security concerns",
                    affected_component="Bug Investigation",
                    evidence=f"Pattern '{pattern}' found in analysis",
                    manager_recommendation="Security review required before proceeding",
                    auto_fixable=False,
                ))
                break
        
        # Check for data loss risk
        data_loss_patterns = [
            "data loss",
            "corruption",
            "save file",
            "delete",
            "truncate",
        ]
        for pattern in data_loss_patterns:
            if pattern in combined_output:
                issues.append(DetectedIssue(
                    issue_type=IssueType.DATA_LOSS_RISK,
                    severity=IssueSeverity.CRITICAL,
                    confidence=IssueConfidence.MEDIUM,
                    description="Bug or fix may involve data loss risk",
                    affected_component="Bug Investigation",
                    evidence=f"Pattern '{pattern}' found in analysis",
                    manager_recommendation="Verify fix preserves data integrity",
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
    def route_after_check(self, state: BugHuntingState) -> str:
        """Route to report (human feedback disabled)."""
        if state.needs_human_review:
            print("[ROUTER] Critical issues detected but auto-proceeding (HITL disabled)")
            print(f"[ROUTER] Issues logged: {len(state.critical_issues)}")
            # Auto-resolve: log and continue to report
            state.needs_human_review = False
        print("[ROUTER] -> report")
        return "report"
    
    @listen("escalate_to_human")
    def human_review_critical(self, state: BugHuntingState) -> BugHuntingState:
        """Human-in-the-loop for critical issues - DISABLED (never reached)."""
        print("\n[MANAGER] Critical issues detected - auto-proceeding with investigation")
        if state.manager_analysis:
            print(f"[MANAGER] Analysis: {state.manager_analysis[:200]}...")
        state.human_guidance = "auto-investigate - HITL disabled"
        state.needs_human_review = False
        return state
    
    @listen("report")
    def generate_report(self, state: BugHuntingState) -> BugHuntingState:
        """
        Final step: Generate comprehensive bug report.
        """
        # Skip if aborted
        if state.current_step == "complete" and not state.success:
            return state
        
        print("\n" + "-"*60)
        print("[DOC] STEP: Generating Final Report")
        print("-"*60)
        
        report_parts = [
            "=" * 60,
            "BUG HUNTING REPORT",
            "=" * 60,
            "",
            "## Bug Report",
            f"Description: {state.bug_report.description}",
            f"Error Codes: {state.bug_report.error_codes}",
            f"Repro Steps: {state.bug_report.repro_steps}",
            "",
            "## Investigation",
            f"Severity: {state.investigation.severity.value}",
            f"Confidence: {state.investigation.confidence.value}",
            f"Summary: {state.investigation.summary}",
            "",
        ]
        
        if state.systems_analysis:
            report_parts.extend([
                "## Systems Analysis",
                f"Scope: {state.systems_analysis.scope}",
                f"Related Systems: {', '.join(state.systems_analysis.related_systems) or 'See full output'}",
                "",
            ])
        else:
            report_parts.extend([
                "## Systems Analysis",
                "Skipped (simple bug)",
                "",
            ])
        
        report_parts.extend([
            "## Proposed Fix",
            f"Summary: {state.fix_proposal.summary}",
            "",
            "## Validation",
            f"Approved: {state.validation.approved}",
            f"Build Status: {state.validation.build_status}",
            "",
            "=" * 60,
            "FULL OUTPUTS",
            "=" * 60,
            "",
            "### Investigation Output",
            state.investigation.raw_output,
            "",
        ])
        
        if state.systems_analysis:
            report_parts.extend([
                "### Systems Analysis Output",
                state.systems_analysis.raw_output,
                "",
            ])
        
        report_parts.extend([
            "### Fix Proposal Output",
            state.fix_proposal.raw_output,
            "",
            "### Validation Output",
            state.validation.raw_output,
        ])
        
        state.final_report = "\n".join(report_parts)
        state.success = True
        state.current_step = "complete"
        
        print("\n" + "="*60)
        print("BUG HUNTING FLOW COMPLETE")
        print("="*60)
        
        return state
    
    # === Helper Methods ===
    
    def _assess_severity_from_output(self, output: str) -> BugSeverity:
        """Parse severity from agent output."""
        output_lower = output.lower()
        
        if "critical" in output_lower or "crash" in output_lower or "corruption" in output_lower:
            return BugSeverity.CRITICAL
        elif "complex" in output_lower or "multi-system" in output_lower or "architectural" in output_lower:
            return BugSeverity.COMPLEX
        elif "simple" in output_lower or "typo" in output_lower or "config" in output_lower:
            return BugSeverity.SIMPLE
        else:
            return BugSeverity.MODERATE
    
    def _needs_systems_analysis(self, output: str) -> bool:
        """Determine if systems analysis is needed based on investigation output."""
        output_lower = output.lower()
        
        # Explicit negative indicators (agent says "not needed")
        skip_indicators = [
            "no systems analysis needed",
            "systems analysis not needed",
            "systems analysis: not needed",
            "systems analysis: no",
            "skip systems analysis",
            "simple fix",
            "straightforward fix",
            "localized issue",
            "single file",
        ]
        
        if any(indicator in output_lower for indicator in skip_indicators):
            return False
        
        # Triggers for systems analysis
        triggers = [
            "multi-system",
            "multiple systems",
            "integration issue",
            "cross-system",
            "state machine",
            "complex interaction",
            "architectural",
            "needs systems analysis",
            "systems analysis: yes",
            "systems analysis needed",
        ]
        
        return any(trigger in output_lower for trigger in triggers)
