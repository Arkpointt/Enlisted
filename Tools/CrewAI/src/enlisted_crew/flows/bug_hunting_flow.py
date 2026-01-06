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
from pathlib import Path
from typing import Optional

from crewai import Agent, Crew, Process, Task, LLM
from crewai.flow.flow import Flow, listen, router, start, or_
from pydantic import BaseModel

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
    max_completion_tokens=8000,
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
    max_completion_tokens=8000,
    reasoning_effort="low",
)

# MEDIUM reasoning - QA validation needs to catch issues
GPT5_QA = LLM(
    model=_get_env("ENLISTED_LLM_QA", "gpt-5.2"),
    max_completion_tokens=6000,
    reasoning_effort="medium",
)

# LOW reasoning - planning from structured prompts
GPT5_PLANNING = LLM(
    model=_get_env("ENLISTED_LLM_PLANNING", "gpt-5.2"),
    max_completion_tokens=4000,
    reasoning_effort="low",
)


# Agent cache (module-level to avoid Flow initialization issues)
_agent_cache = {}


def get_code_analyst() -> Agent:
    """Get or create code analyst agent."""
    if "code_analyst" not in _agent_cache:
        _agent_cache["code_analyst"] = Agent(
                role="C# Code Analyst",
                goal="Investigate bugs by searching logs, reading code, and tracing to root cause",
                backstory="""You investigate bugs using TOOLS, not assumptions.

CRITICAL - TOOL-FIRST WORKFLOW (execute in order):
1. IMMEDIATELY call get_dev_reference - don't think, just call it
2. Call search_debug_logs_tool with error codes or keywords
3. Call read_source_section for relevant code snippets
4. Only THEN assess severity based on actual findings

TOOL CALL FORMAT: Each tool takes ONE argument, not arrays.
- CORRECT: lookup_content_id("event_1") then lookup_content_id("event_2")
- WRONG: lookup_content_id(["event_1", "event_2"])

DO NOT write analysis without tool calls. Each response needs a tool call
until you have concrete evidence. "I think" is worthless - "The log shows" is valuable.

SEARCH EFFICIENCY: Limit to ~5-8 searches total. NEVER re-search same query.
ERROR CODES: E-ENCOUNTER-*=battle, E-SAVELOAD-*=corruption, E-CAMPUI-*=UI""",
            llm=GPT5_ANALYSIS,
            tools=[
                get_dev_reference,
                read_debug_logs_tool,
                search_debug_logs_tool,
                read_native_crash_logs_tool,
                find_in_code,
                read_source_section,
                read_doc_tool,
                find_in_docs,
                list_feature_files_tool,
                # Database tools for bug investigation
                lookup_error_code,  # Check if this is a known error pattern
                lookup_content_id,  # Verify content IDs when debugging content bugs
                search_content,  # Find related content
            ],
            verbose=True,
            respect_context_window=True,
            max_iter=15,
            max_retry_limit=3,
            reasoning=True,  # Enable reflection for thorough investigation
            max_reasoning_attempts=3,  # Limit reasoning iterations
            allow_delegation=True,  # Lead investigator can delegate specialized tasks
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
                get_game_systems,
                get_dev_reference,
                find_in_code,  # Search first
                read_source_section,  # Read targeted snippets
                read_source,  # Full file when needed
                read_doc_tool,
                find_in_docs,
                list_feature_files_tool,
                find_in_native_api,  # DEPRECATED: Use MCP server
                # Database tools for systems analysis
                get_system_dependencies,  # Understand how systems interact
                lookup_core_system,  # Identify which core systems are affected
                lookup_api_pattern,  # Find correct Bannerlord API usage patterns
            ],
            verbose=True,
            respect_context_window=True,
            max_iter=15,
            max_retry_limit=3,
            reasoning=True,  # Enable reflection for system integration analysis
            max_reasoning_attempts=3,  # Limit reasoning iterations
            allow_delegation=True,  # Coordinator can delegate system-specific analysis
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
                verify_file_exists_tool,  # Verify file paths before proposing changes
                find_in_code,  # Search to find relevant code
                read_source_section,  # Read targeted snippets
                read_source,  # Full file when implementing
                find_in_native_api,  # DEPRECATED: Agents should use MCP server
                review_code,
                check_game_patterns,
                # Database tools for fix proposals
                lookup_api_pattern,  # Ensure fix uses correct Bannerlord API patterns
                get_balance_value,  # Check balance values when proposing gameplay fixes
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


class BugHuntingFlow(Flow[BugHuntingState]):
    """
    Flow-based bug hunting workflow.
    
    State Persistence: Enabled via persist=True. If a run fails, you can resume
    from the last successful step by re-running with the same inputs.
    
    Steps:
    1. receive_bug_report - Parse and validate input
    2. investigate_bug - Code analyst searches logs and code
    3. assess_severity - Router decides next steps based on findings
    4. analyze_systems - (optional) Systems analyst for complex bugs
    5. propose_fix - C# implementer suggests minimal fix
    6. validate_fix - QA agent validates the proposal
    7. generate_report - Create final report
    
    The flow uses conditional routing to skip systems analysis for simple bugs.
    """
    
    initial_state = BugHuntingState
    persist = True  # Auto-save state to SQLite for recovery on failure
    
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
    def investigate_bug(self, state: BugHuntingState) -> BugHuntingState:
        """
        Step 2: Investigate the bug using either user logs or code-path analysis.
        
        If user provided logs: analyze those logs (do not read local logs).
        Else: analyze relevant code paths based on symptoms.
        """
        br = state.bug_report
        
        if br.has_user_logs:
            print("\n[ROUTE] User provided logs -> Analyzing provided logs")
            print("\n" + "-"*60)
            print("[SEARCH] STEP: Investigating Bug (with user-provided logs)")
            print("-"*60)
            log_content = br.user_log_content or ""
            # Truncate log content to avoid token spikes
            max_log_chars = 50000
            if len(log_content) > max_log_chars:
                log_content = log_content[:max_log_chars] + "\n... [TRUNCATED - log too long]"
                print(f"[!]  Log content truncated to {max_log_chars} chars")
            task = Task(
                description=f"""
                Investigate the following bug report using the PROVIDED LOG CONTENT.
                IMPORTANT: Analyze ONLY the provided log content. Do NOT search local logs.
                
                HARD CONSTRAINTS (token safety):
                - Read at most 10 short excerpts/snippets (method bodies or <200 lines total)
                - Do not paste full files; quote only minimal, relevant lines
                - After gathering notes, summarize and STOP; do not retry if context warnings occur
                
                BUG DESCRIPTION: {br.description}
                ERROR CODES MENTIONED: {br.error_codes}
                REPRO STEPS: {br.repro_steps}
                CONTEXT: {br.context or "None provided"}
                
                ===== USER-PROVIDED LOG CONTENT =====
                {log_content}
                ===== END LOG CONTENT =====
                
                INVESTIGATION STEPS:
                1. Call get_dev_reference first to understand error codes
                2. Search the provided content for E-*/W-* codes
                3. Identify stack traces and exception messages
                4. Prefer find_in_code + read_source_section (small excerpts); avoid read_source
                5. Trace to root cause
                
                Assess severity and whether systems analysis is needed.
                """,
                expected_output="Investigation report (from provided logs): summary, root cause, files, severity, confidence, needs systems analysis",
                agent=get_code_analyst(),
            )
        else:
            print("\n[ROUTE] No user logs -> Analyzing code paths based on symptoms")
            print("\n" + "-"*60)
            print("[SEARCH] STEP: Investigating Bug (analyzing code paths - no logs)")
            print("-"*60)
            task = Task(
                description=f"""
                Investigate this END USER bug report by analyzing CODE PATHS.
                IMPORTANT: No logs were provided. Do NOT search local dev logs.
                
                HARD CONSTRAINTS (token safety):
                - Search first; then open only targeted, small code excerpts
                - Quote at most 10 short snippets total (<200 lines combined)
                - Summarize between reads; if context warnings occur, STOP and produce findings
                
                BUG DESCRIPTION: {br.description}
                REPRO STEPS: {br.repro_steps}
                CONTEXT: {br.context or "None provided"}
                
                APPROACH:
                1. Call get_dev_reference first
                2. Break down the symptoms and related game state transitions
                3. Use find_in_docs for relevant docs
                4. Use find_in_code + read_source_section on implicated code (small excerpts only)
                5. Hypothesize root causes and identify likely affected files
                
                Assess severity and whether systems analysis is needed.
                """,
                expected_output="Investigation report (no logs): summary, hypothesized root cause, files, severity, confidence, needs systems analysis",
                agent=get_code_analyst(),
            )
        
        crew = Crew(
            agents=[get_code_analyst()],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
            memory=True,    # Enable memory for context retention
            cache=True,     # Cache tool results
            planning=True,  # AgentPlanner creates step plan before execution
            planning_llm=GPT5_PLANNING,  # Use gpt-5 for planning
        )
        result = crew.kickoff()
        raw_output = str(result)
        
        investigation = Investigation(
            summary=raw_output[:500] if len(raw_output) > 500 else raw_output,
            root_cause=None,
            error_codes_found=[],
            severity=self._assess_severity_from_output(raw_output),
            confidence=ConfidenceLevel.LIKELY if br.has_user_logs else ConfidenceLevel.SPECULATIVE,
            needs_systems_analysis=self._needs_systems_analysis(raw_output),
            raw_output=raw_output,
        )
        print(f"\n[RESULT] Investigation Complete:")
        print(f"   Severity: {investigation.severity.value}")
        print(f"   Needs Systems Analysis: {investigation.needs_systems_analysis}")
        state.investigation = investigation
        state.current_step = "route"
        return state
    
    @router(investigate_bug)
    def assess_severity(self, state: BugHuntingState) -> str:
        """
        Router: Decide next step based on investigation results.
        
        Uses condition functions for clean, testable routing logic.
        Listens to both investigation methods (one will skip, one will run).
        Only processes after the actual investigation completes.
        
        Returns:
        - "analyze_systems" for COMPLEX/CRITICAL bugs
        - "propose_fix" for SIMPLE/MODERATE bugs
        """
        investigation = state.investigation
        severity = investigation.severity.value if investigation else "unknown"
        
        if needs_systems_analysis(state):
            print(format_routing_decision(
                condition_name="needs_systems_analysis",
                condition_result=True,
                chosen_path="analyze_systems",
                reason=f"Severity: {severity}, requires deeper analysis"
            ))
            return "analyze_systems"
        
        print(format_routing_decision(
            condition_name="is_simple_bug",
            condition_result=True,
            chosen_path="propose_fix",
            reason=f"Severity: {severity}, can skip systems analysis"
        ))
        state.skipped_steps.append("analyze_systems")
        return "propose_fix"
    
    @listen("analyze_systems")
    def analyze_systems(self, state: BugHuntingState) -> BugHuntingState:
        """
        Step 3 (optional): Systems analyst does deeper analysis.
        
        Only runs for COMPLEX/CRITICAL bugs.
        """
        print("\n" + "-"*60)
        print("[SYSTEMS] STEP: Systems Analysis (systems_analyst)")
        print("-"*60)
        
        investigation = state.investigation
        
        task = Task(
            description=f"""
            Analyze systems related to this bug:
            
            HARD CONSTRAINTS (token safety):
            - Quote at most 8 small excerpts in total; no full files
            - Prefer diagrams/lists over code dumps; summarize aggressively
            - If context warnings occur, finalize with current summary and proceed
            
            INVESTIGATION SUMMARY: {investigation.summary}
            AFFECTED FILES: {[f.path for f in investigation.affected_files] if investigation.affected_files else "Unknown"}
            
            ANALYSIS TASKS:
            1. Call get_game_systems first
            2. Review the systems identified in the investigation
            3. Check for related code patterns that might have similar issues
            4. Identify integration points that could be affected
            5. Assess scope: localized, module-wide, or systemic
            6. Determine what other files might need similar fixes
            """,
            expected_output="""
            Systems analysis with:
            - Related systems and their roles
            - Scope of the issue
            - Similar code patterns found
            - Risk assessment for any fix
            - Additional files that may need changes
            """,
            agent=get_systems_analyst(),
        )
        
        crew = Crew(
            agents=[get_systems_analyst()],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
            memory=True,    # Enable memory for context retention
            cache=True,     # Cache tool results
            planning=True,  # AgentPlanner creates step plan before execution
            planning_llm=GPT5_PLANNING,  # Use gpt-5 for planning
        )
        
        result = crew.kickoff()
        raw_output = str(result)
        
        systems_analysis = SystemsAnalysis(
            related_systems=[],  # Would parse from output
            scope="module-wide",  # Would parse from output
            similar_patterns=[],
            risk_assessment="",
            raw_output=raw_output,
        )
        
        state.systems_analysis = systems_analysis
        state.current_step = "propose_fix"
        return state
    
    @listen(or_("propose_fix", analyze_systems))
    def propose_fix(self, state: BugHuntingState) -> BugHuntingState:
        """
        Step 4: C# implementer proposes a fix.
        
        Receives investigation (and optionally systems analysis) as context.
        """
        print("\n" + "-"*60)
        print("[FIX] STEP: Proposing Fix (csharp_implementer)")
        print("-"*60)
        
        investigation = state.investigation
        systems_analysis = state.systems_analysis
        
        context = f"""
        INVESTIGATION:
        {investigation.summary}
        
        Affected Files: {[f.path for f in investigation.affected_files] if investigation.affected_files else "See investigation"}
        Severity: {investigation.severity.value}
        """
        
        if systems_analysis:
            context += f"""
            
        SYSTEMS ANALYSIS:
        {systems_analysis.raw_output[:2000] if systems_analysis.raw_output else "No additional analysis"}
        """
        
        task = Task(
            description=f"""
            Propose a minimal fix for this bug:
            
            {context}
            
            REQUIREMENTS:
            1. Propose the MINIMAL fix that addresses root cause
            2. Follow Enlisted code style (Allman braces, _camelCase, XML docs)
            3. Use proper Bannerlord patterns
            4. Consider save/load compatibility
            5. Include code changes in ```csharp code blocks
            
            CRITICAL PATTERNS TO FOLLOW:
            - Always check Hero.MainHero != null before access
            - Use TextObject for all user-visible strings
            - Use GiveGoldAction for gold changes
            - Use EscalationManager for reputation changes
            
            OUTPUT FORMAT (IMPORTANT):
            - Write your response as plain text with markdown code blocks
            - Use ```csharp for C# code changes
            - DO NOT output XML tags or <function_calls> - just describe the fix
            - Be concise but complete
            """,
            expected_output="""
            Fix proposal with:
            - Summary of the proposed fix (1-2 sentences)
            - Files to modify (list paths)
            - Code changes in ```csharp blocks
            - Brief explanation of why this fixes the bug
            - Any risks or side effects
            """,
            agent=get_implementer(),
        )
        
        crew = Crew(
            agents=[get_implementer()],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
            memory=True,    # Enable memory for context retention
            cache=True,     # Cache tool results
            planning=True,  # AgentPlanner creates step plan before execution
            planning_llm=GPT5_PLANNING,  # Use gpt-5 for planning
        )
        
        result = crew.kickoff()
        raw_output = str(result)
        
        fix_proposal = FixProposal(
            summary=raw_output[:500] if len(raw_output) > 500 else raw_output,
            changes=[],  # Would parse from output
            explanation="",
            risks=[],
            testing_notes="",
            raw_output=raw_output,
        )
        
        state.fix_proposal = fix_proposal
        state.current_step = "validate"
        return state
    
    @listen(propose_fix)
    def validate_fix(self, state: BugHuntingState) -> BugHuntingState:
        """
        Step 5: QA agent validates the proposed fix.
        """
        print("\n" + "-"*60)
        print("[OK] STEP: Validating Fix (qa_agent)")
        print("-"*60)
        
        fix_proposal = state.fix_proposal
        
        task = Task(
            description=f"""
            Validate the proposed bug fix:
            
            FIX PROPOSAL:
            {fix_proposal.summary}
            
            FULL PROPOSAL:
            {fix_proposal.raw_output[:3000]}
            
            VALIDATION CHECKS:
            1. Run dotnet build to verify compilation
            2. Check code style compliance
            3. Verify the fix matches the investigation findings
            4. Assess if the fix could introduce regressions
            5. Confirm fix is minimal and targeted
            """,
            expected_output="""
            Validation report with:
            - Build status (pass/fail/not_run)
            - Style compliance issues
            - Concerns about the fix
            - Recommendations for improvement
            - Final approval (approved/needs_changes)
            """,
            agent=get_qa_agent(),
        )
        
        crew = Crew(
            agents=[get_qa_agent()],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
            memory=True,    # Enable memory for context retention
            cache=True,     # Cache tool results
            planning=True,  # AgentPlanner creates step plan before execution
            planning_llm=GPT5_PLANNING,  # Use gpt-5 for planning
        )
        
        result = crew.kickoff()
        raw_output = str(result)
        
        validation = ValidationResult(
            approved=True,  # Would parse from output
            build_status="not_run",  # Would parse from output
            style_issues=[],
            concerns=[],
            recommendations=[],
            raw_output=raw_output,
        )
        
        state.validation = validation
        state.current_step = "report"
        return state
    
    @listen(validate_fix)
    def generate_report(self, state: BugHuntingState) -> BugHuntingState:
        """
        Final step: Generate comprehensive bug report.
        """
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
