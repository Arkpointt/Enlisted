"""
Planning Flow - CrewAI Flow-based workflow for feature design.

This Flow orchestrates agents to research, design, and document new features
with automatic validation and hallucination correction.

State Persistence: Enabled. If a run fails, re-running resumes from last step.

Usage:
    from enlisted_crew.flows import PlanningFlow
    
    flow = PlanningFlow()
    result = flow.kickoff(inputs={
        "feature_name": "reputation-integration",
        "description": "Connect reputation to morale and supply systems",
    })
"""

import os
from pathlib import Path
from typing import List, Optional

from typing import Tuple, Any

from crewai import Agent, Crew, Process, Task, LLM
from crewai.flow.flow import Flow, listen, router, start, or_
try:
    from crewai.flow.human_feedback import human_feedback, HumanFeedbackResult
    HUMAN_FEEDBACK_AVAILABLE = True
except ImportError:
    HUMAN_FEEDBACK_AVAILABLE = False
    HumanFeedbackResult = None  # Type hint fallback
    print("[WARNING] human_feedback not available in this CrewAI version")
from crewai.tasks.conditional_task import ConditionalTask

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
from crewai.tasks.task_output import TaskOutput
from pydantic import BaseModel, Field

# Import state models
from .state_models import (
    PlanningState,
    ValidationStatus,
)

# Import condition functions
from .conditions import (
    validation_passed,
    validation_fixed,
    needs_plan_fix,
    validation_complete,
    format_routing_decision,
)

from ..tools import (
    # Context Loaders
    get_writing_guide,
    get_architecture,
    get_dev_reference,
    get_game_systems,
    # Documentation
    read_doc_tool,
    list_docs_tool,
    find_in_docs,
    # Source Code
    read_source,
    find_in_code,
    read_source_section,
    list_feature_files_tool,
    # Planning
    save_plan,
    load_plan,
    # Verification
    verify_file_exists_tool,
    list_event_ids,
    # Database
    lookup_content_id,
    search_content,
    get_balance_value,
    get_tier_info,
    get_system_dependencies,
    lookup_api_pattern,
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

def validate_plan_structure(output: TaskOutput) -> Tuple[bool, Any]:
    """Ensure plan document has required sections.
    
    Returns (True, output) if valid, (False, error_message) if invalid.
    Invalid outputs trigger retry up to guardrail_max_retries.
    """
    content = str(output.raw).lower()
    required_sections = [
        "overview",
        "technical specification",
        "files to create",
    ]
    missing = [s for s in required_sections if s not in content]
    if missing:
        return (False, f"Plan missing required sections: {missing}. Please include all sections.")
    return (True, output)


def validate_no_placeholder_paths(output: TaskOutput) -> Tuple[bool, Any]:
    """Check for hallucinated or placeholder file paths.
    
    Catches common placeholder patterns that indicate incomplete planning.
    """
    content = str(output.raw)
    suspicious_patterns = [
        "PLACEHOLDER",
        "TODO_PATH",
        "path/to/",
        "<file_path>",
        "YOUR_PATH",
        "CHANGE_ME",
    ]
    found = [p for p in suspicious_patterns if p in content]
    if found:
        return (False, f"Found placeholder paths: {found}. Replace with actual file paths.")
    return (True, output)


def validate_design_has_content_ids(output: TaskOutput) -> Tuple[bool, Any]:
    """Ensure design task includes content IDs if applicable.
    
    Design output should reference specific content IDs for events/decisions.
    """
    content = str(output.raw).lower()
    # If design mentions events/decisions, it should have actual IDs
    if "event" in content or "decision" in content:
        # Check for ID patterns like evt_, decision_, etc.
        if "evt_" not in content and "decision_" not in content and "_id" not in content:
            return (False, "Design mentions events/decisions but no content IDs found. Please specify actual content IDs (e.g., evt_reputation_check).")
    return (True, output)


# === LLM Configurations (OpenAI GPT-5 family) ===

def _get_env(name: str, default: str) -> str:
    return os.environ.get(name, default)


# =============================================================================
# LLM TIERS - reasoning_effort optimizes cost/performance
# high=deep thinking | medium=balanced | low=quick | none=instant
# =============================================================================

# HIGH reasoning - architecture and feature design
GPT5_ARCHITECT = LLM(
    model=_get_env("ENLISTED_LLM_ARCHITECT", "gpt-5.2"),
    max_completion_tokens=16000,
    reasoning_effort="high",
)

# MEDIUM reasoning - analysis and verification
GPT5_ANALYST = LLM(
    model=_get_env("ENLISTED_LLM_ANALYST", "gpt-5.2"),
    max_completion_tokens=8000,
    reasoning_effort="medium",
)

# LOW reasoning - documentation from clear structure
GPT5_DOCS = LLM(
    model=_get_env("ENLISTED_LLM_IMPLEMENTER", "gpt-5.2"),
    max_completion_tokens=8000,
    reasoning_effort="low",
)

# Planning LLM - use simple string (LLM objects with reasoning_effort cause issues with AgentPlanner)
# See: https://docs.crewai.com/en/concepts/planning - examples all use simple strings
GPT5_PLANNING = _get_env("ENLISTED_LLM_PLANNING", "gpt-5.2")


# === Agent Factory ===

_agent_cache = {}


def get_planning_manager() -> Agent:
    """Manager agent that coordinates the planning workflow."""
    if "planning_manager" not in _agent_cache:
        _agent_cache["planning_manager"] = Agent(
            role="Planning Coordinator",
            goal="Efficiently coordinate research, design, and documentation to produce high-quality feature plans",
            backstory="""You are an experienced project manager coordinating a feature planning team.
            Your role is to:
            1. Delegate research tasks to Systems Analyst
            2. Request architectural advice from Architecture Advisor
            3. Coordinate design work with Feature Architect
            4. Ensure Documentation Maintainer captures all decisions
            5. Validate outputs meet quality standards before proceeding
            
            You ensure each specialist contributes their expertise while maintaining
            overall coherence and quality of the planning document.""",
            llm=GPT5_ARCHITECT,
            allow_delegation=True,  # REQUIRED for manager
            verbose=True,
            max_iter=30,
            max_retry_limit=3,
            respect_context_window=True,
        )
    return _agent_cache["planning_manager"]


def get_systems_analyst() -> Agent:
    """Research existing systems and code."""
    if "systems_analyst" not in _agent_cache:
        _agent_cache["systems_analyst"] = Agent(
            role="Systems Analyst",
            goal="Research existing Enlisted systems to understand integration points",
            backstory="""You research the Enlisted codebase using TOOLS, not memory.

CRITICAL - TOOL-FIRST WORKFLOW:
1. IMMEDIATELY call get_game_systems - don't think, just call it
2. Call get_system_dependencies for EACH system (one call per system)
3. Use find_in_code to locate relevant files
4. Use read_source to examine code

TOOL CALL FORMAT: Each tool takes ONE argument, not arrays.
- CORRECT: get_system_dependencies("CompanyNeedsManager") then get_system_dependencies("EscalationManager")
- WRONG: get_system_dependencies(["CompanyNeedsManager", "EscalationManager"])

DO NOT spend iterations "planning" or "thinking". EXECUTE TOOLS.
If you find yourself writing paragraphs without tool calls, STOP and call a tool.""",
            llm=GPT5_ARCHITECT,
            tools=[
                get_game_systems,
                read_doc_tool,
                find_in_docs,
                read_source,
                find_in_code,
                list_feature_files_tool,
                get_system_dependencies,
                lookup_api_pattern,
            ],
            verbose=True,
            respect_context_window=True,
            max_iter=15,  # Limit iterations to prevent runaway loops
            max_retry_limit=3,  # Retry on transient errors
            reasoning=True,  # Plan research strategy before executing
            max_reasoning_attempts=3,  # Limit reasoning iterations
            allow_delegation=False,  # Specialists don't delegate (only managers do)
        )
    return _agent_cache["systems_analyst"]


def get_architecture_advisor() -> Agent:
    """Suggest best practices and improvements."""
    if "architecture_advisor" not in _agent_cache:
        _agent_cache["architecture_advisor"] = Agent(
            role="Architecture Advisor",
            goal="Suggest best practices and architectural improvements",
            backstory="""You advise on architecture using VERIFIED facts from tools.

CRITICAL - TOOL-FIRST WORKFLOW:
1. IMMEDIATELY call get_architecture to load patterns
2. Call get_tier_info for EACH tier needed (one call per tier: 1, 4, 5, 6, 7)
3. Use find_in_code to verify patterns exist in codebase
4. Use get_balance_value for EACH value needed (one call per value)

TOOL CALL FORMAT: Each tool takes ONE argument, not arrays.
- CORRECT: get_tier_info(5) then get_tier_info(6) then get_tier_info(7)
- WRONG: get_tier_info([5, 6, 7])

DO NOT give advice based on assumptions. Every recommendation must be
grounded in actual tool output. Your advice is only valuable if verified.""",
            llm=GPT5_ARCHITECT,
            tools=[
                get_architecture,
                read_doc_tool,
                find_in_docs,
                read_source,
                find_in_code,
                get_tier_info,
                get_balance_value,
            ],
            verbose=True,
            respect_context_window=True,
            max_iter=15,
            max_retry_limit=3,
            reasoning=True,  # Think before advising
            max_reasoning_attempts=3,  # Limit reasoning iterations
            allow_delegation=False,  # Specialists don't delegate (only managers do)
        )
    return _agent_cache["architecture_advisor"]


def get_feature_architect() -> Agent:
    """Design the technical specification."""
    if "feature_architect" not in _agent_cache:
        _agent_cache["feature_architect"] = Agent(
            role="Feature Architect",
            goal="Design complete technical specifications AND identify gaps in existing code",
            backstory="""You design features with VERIFIED technical specs AND investigate gaps.

CRITICAL - TOOL-FIRST WORKFLOW:
1. Call get_dev_reference FIRST to understand coding patterns
2. Call lookup_content_id to check what content IDs already exist
3. Call list_event_ids to see existing event naming patterns
4. Use find_in_code to search for broken references (e.g., methods calling non-existent IDs)
5. Use read_source to examine code that references missing content

GAP ANALYSIS - When you find broken references:
1. INVESTIGATE WHY: Check docs, search for related code, look for patterns
2. CLASSIFY the gap:
   - DEPRECATED: Old code calling removed features → plan to delete it
   - MISSING_IMPL: Code calling features that should exist → plan to implement
   - TEST_CODE: Test file calling unbuilt APIs → note as expected gap
   - OUTDATED_DOCS: Docs reference removed code → plan doc update
   - UNCLEAR: Can't determine intent → FLAG FOR HUMAN REVIEW with question
3. INCLUDE REMEDIATION in your design spec

EVERY file path, method name, and content ID you specify MUST be verified.
DO NOT invent file paths or method names. Check they exist or follow patterns.

Your spec must address BOTH new features AND discovered gaps.""",
            llm=GPT5_ARCHITECT,
            tools=[
                get_dev_reference,
                read_doc_tool,
                read_source,
                find_in_code,
                list_event_ids,
                lookup_content_id,
                get_tier_info,
            ],
            verbose=True,
            respect_context_window=True,
            max_iter=25,  # Increased for gap analysis workflow
            max_retry_limit=3,
            reasoning=True,  # Think before designing
            max_reasoning_attempts=3,  # Limit reasoning iterations
            allow_delegation=False,  # Specialists don't delegate (only managers do)
        )
    return _agent_cache["feature_architect"]


def get_documentation_maintainer() -> Agent:
    """Write planning documents."""
    if "documentation_maintainer" not in _agent_cache:
        _agent_cache["documentation_maintainer"] = Agent(
            role="Documentation Maintainer",
            goal="Write clear, complete planning documents",
            backstory="""You write planning documents that capture all design decisions.
Your documents are saved to docs/CrewAI_Plans/ and serve as the
source of truth for implementation.""",
            llm=GPT5_DOCS,
            tools=[
                save_plan,
                load_plan,
                read_doc_tool,
                get_writing_guide,
            ],
            verbose=True,
            respect_context_window=True,
            max_iter=10,
            max_retry_limit=3,
            allow_delegation=False,  # Specialist focuses on documentation
        )
    return _agent_cache["documentation_maintainer"]


def get_code_analyst() -> Agent:
    """Validate plan accuracy."""
    if "code_analyst" not in _agent_cache:
        _agent_cache["code_analyst"] = Agent(
            role="Code Analyst",
            goal="Validate that plans reference real files, methods, and IDs",
            backstory="""You validate plans against the actual codebase. You check that
every file path exists, every method signature is correct, and every
content ID is available. You catch hallucinations before implementation.""",
            llm=GPT5_ANALYST,
            tools=[
                verify_file_exists_tool,
                list_event_ids,
                find_in_code,
                read_source,
                lookup_content_id,
                search_content,
            ],
            verbose=True,
            respect_context_window=True,
            max_iter=20,  # More iterations for thorough validation
            max_retry_limit=3,
        )
    return _agent_cache["code_analyst"]


# === The Flow ===

class PlanningFlow(Flow[PlanningState]):
    """
    Flow-based planning workflow for feature design.
    
    Uses hierarchical process with a Planning Manager coordinating specialists.
    
    State Persistence: Enabled via persist=True. If a run fails, you can resume
    from the last successful step by re-running with the same inputs.
    
    Steps:
    1. receive_request - Parse and validate inputs
    2. run_planning_crew - Single hierarchical crew (research → advise → design → write)
    3. route_after_planning - Check for UNCLEAR gaps
    4. review_unclear_gaps - (conditional) Human review of gaps
    5. validate_plan - Check for hallucinations
    6. route_validation - Fix issues or complete
    7. fix_plan - (conditional) Correct any issues
    8. generate_report - Final summary
    """
    
    initial_state = PlanningState
    persist = True  # Auto-save state to SQLite for recovery on failure
    
    # === Flow Steps ===
    
    @start()
    def receive_request(self) -> PlanningState:
        """Entry point: Parse feature request.
        
        Inputs are passed via kickoff(inputs={...}) and populate self.state.
        """
        print("\n" + "="*60)
        print("PLANNING FLOW STARTED")
        print("="*60)
        
        # Access state - inputs populate state fields with matching names
        state = self.state
        
        feature_name = state.feature_name if hasattr(state, 'feature_name') else ""
        description = state.description if hasattr(state, 'description') else ""
        
        if not feature_name or not description:
            print("[ERROR] Missing feature_name or description!")
            print(f"[DEBUG] state={state}")
            state.current_step = "error"
            state.success = False
            state.final_report = "Error: Missing feature_name or description"
            return state
        
        print(f"\nFeature: {feature_name}")
        print(f"Description: {description[:100]}...")
        
        state.current_step = "planning"
        return state
    
    @listen(receive_request)
    def run_planning_crew(self, state: PlanningState) -> PlanningState:
        """Single hierarchical crew for all planning work.
        
        Manager coordinates: research → advise → design → write
        """
        print("\n" + "-"*60)
        print("[PLANNING CREW] Running hierarchical planning...")
        print("-"*60)
        
        plan_path = f"docs/CrewAI_Plans/{state.feature_name}.md"
        
        # Task 1: Research existing systems
        research_task = Task(
            description=f"""
            Research existing Enlisted systems for this feature:
            
            FEATURE: {state.feature_name}
            DESCRIPTION: {state.description}
            RELATED SYSTEMS: {state.related_systems or "To be determined"}
            
            RESEARCH TASKS:
            1. Call get_game_systems first to understand core systems
            2. Search documentation for related features
            3. Find relevant C# code and understand the patterns
            4. Identify integration points and dependencies
            5. Note any existing similar functionality
            
            OUTPUT: Comprehensive research summary with:
            - Related systems and their roles
            - Key files and methods
            - Integration points
            - Existing patterns to follow
            """,
            expected_output="Research summary of existing systems",
            agent=get_systems_analyst(),
        )
        
        # Task 2: Suggest architectural improvements (depends on research)
        advise_task = Task(
            description=f"""
            Suggest architectural improvements for this feature:
            
            FEATURE: {state.feature_name}
            DESCRIPTION: {state.description}
            
            ADVISOR TASKS:
            1. Review the research findings from Systems Analyst
            2. Identify potential issues or risks
            3. Suggest best practices to follow
            4. Recommend architectural patterns
            5. Note tier-aware design considerations (T1-T4 Grunt, T5-T6 NCO, T7+ Commander)
            
            OUTPUT: Architectural recommendations with:
            - Suggested patterns
            - Risks to avoid
            - Best practices
            - Tier considerations
            """,
            expected_output="Architectural recommendations",
            agent=get_architecture_advisor(),
            context=[research_task],  # Depends on research
        )
        
        # Task 3: Design technical specification (depends on research + advice)
        design_task = Task(
            description=f"""
            Design the technical specification for this feature:
            
            FEATURE: {state.feature_name}
            DESCRIPTION: {state.description}
            
            DESIGN TASKS:
            1. Define exact file paths for new C# code
            2. Specify method signatures and class structure
            3. Define JSON content IDs (events, decisions, orders)
            4. Specify integration with existing systems
            5. Include tier-variant content if applicable
            6. SEARCH for broken references in related code (use find_in_code)
            7. For EACH broken reference found:
               a. Investigate WHY it's broken (check docs, patterns, history)
               b. Classify: DEPRECATED | MISSING_IMPL | TEST_CODE | OUTDATED_DOCS | UNCLEAR
               c. Propose remediation OR flag for human review
            
            If you encounter major uncertainties during design, you can ask the human
            for clarification. Use human input sparingly - only for critical decisions
            that significantly impact the design.
            
            OUTPUT: Complete technical spec with:
            - File paths (exact, verifiable)
            - Method signatures
            - Content IDs (unique, following naming conventions)
            - Integration points
            - Implementation order
            - **GAPS DISCOVERED** section:
              * List each broken reference found
              * Classification and reasoning
              * Proposed fix OR question for human
            """,
            expected_output="Technical specification document",
            agent=get_feature_architect(),
            context=[research_task, advise_task],  # Depends on both
            human_input=True,  # PRESERVE task-level HITL
        )
        
        # Task 4: Write planning document (depends on design)
        write_task = Task(
            description=f"""
            Write the planning document for this feature:
            
            FEATURE: {state.feature_name}
            OUTPUT PATH: {plan_path}
            
            DOCUMENT TASKS:
            1. Use save_plan tool to write the document
            2. Include all technical details from the design
            3. Add implementation checklist
            4. Include validation criteria
            5. Mark status as "Planning"
            
            DOCUMENT STRUCTURE:
            - Title and Status
            - Overview
            - Technical Specification
            - Files to Create/Modify
            - Content IDs
            - Implementation Checklist
            - Validation Criteria
            """,
            expected_output="Planning document saved to disk",
            agent=get_documentation_maintainer(),
            context=[design_task],  # Depends on design
            guardrails=[validate_plan_structure, validate_no_placeholder_paths],
            guardrail_max_retries=2,
        )
        
        # Single hierarchical crew with manager coordination
        crew = Crew(
            agents=[
                get_systems_analyst(),
                get_architecture_advisor(),
                get_feature_architect(),
                get_documentation_maintainer(),
            ],
            tasks=[research_task, advise_task, design_task, write_task],
            manager_agent=get_planning_manager(),
            process=Process.hierarchical,
            verbose=True,
            memory=True,
            cache=True,
            planning=True,
            planning_llm=GPT5_PLANNING,
        )
        
        result = crew.kickoff()
        
        # Extract outputs from crew result
        result_str = str(result)
        state.design_output = result_str
        state.plan_path = plan_path
        
        # Check if design contains UNCLEAR gaps that need human review
        if "unclear" in result_str.lower() and "gap" in result_str.lower():
            state.current_step = "human_review"
        else:
            state.current_step = "validate"
        
        return state
    
    @listen(run_planning_crew)
    def check_for_issues(self, state: PlanningState) -> PlanningState:
        """Manager analyzes outputs and decides if human review needed.
        
        Detects:
        - Hallucinated files/methods
        - Conflicting documentation
        - Dead code references
        """
        print("\n" + "-"*60)
        print("[MANAGER] Analyzing planning outputs for issues...")
        print("-"*60)
        
        issues = []
        design_output = state.design_output.lower() if state.design_output else ""
        
        # Check for hallucinated file references
        hallucinated_file_patterns = [
            "file not found",
            "does not exist",
            "could not locate",
            "missing file",
        ]
        for pattern in hallucinated_file_patterns:
            if pattern in design_output:
                issues.append(DetectedIssue(
                    issue_type=IssueType.HALLUCINATED_FILE,
                    severity=IssueSeverity.HIGH,
                    confidence=IssueConfidence.MEDIUM,
                    description=f"Plan references files that may not exist",
                    affected_component="Planning Output",
                    evidence=f"Pattern '{pattern}' found in design",
                    manager_recommendation="Verify file paths with find_in_code tool",
                    auto_fixable=True,
                ))
                break
        
        # Check for conflicting requirements
        if "conflict" in design_output or "contradicts" in design_output:
            issues.append(DetectedIssue(
                issue_type=IssueType.CONFLICTING_REQUIREMENTS,
                severity=IssueSeverity.CRITICAL,
                confidence=IssueConfidence.MEDIUM,
                description="Design contains conflicting requirements",
                affected_component="Planning Output",
                evidence="'conflict' or 'contradicts' found in design",
                manager_recommendation="Human review needed to resolve conflicts",
                auto_fixable=False,
            ))
        
        # Check for UNCLEAR gaps (already handled by existing logic but add to escalation)
        if "unclear" in design_output and "gap" in design_output:
            issues.append(DetectedIssue(
                issue_type=IssueType.HALLUCINATED_API,
                severity=IssueSeverity.HIGH,
                confidence=IssueConfidence.LOW,
                description="Design contains unclear gaps that need classification",
                affected_component="Planning Output",
                evidence="UNCLEAR gaps found in design output",
                manager_recommendation="Classify gaps as DEPRECATED, MISSING_IMPL, or TEST_CODE",
                auto_fixable=False,
            ))
        
        # Check for dead code detection
        dead_code_patterns = ["deprecated", "unused", "dead code", "obsolete"]
        for pattern in dead_code_patterns:
            if pattern in design_output:
                issues.append(DetectedIssue(
                    issue_type=IssueType.DEPRECATED_SYSTEM,
                    severity=IssueSeverity.MEDIUM,
                    confidence=IssueConfidence.HIGH,
                    description=f"Design references potentially deprecated code",
                    affected_component="Planning Output",
                    evidence=f"Pattern '{pattern}' found in design",
                    manager_recommendation="Verify if code should be removed or updated",
                    auto_fixable=True,
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
            # Auto-fix minor issues
            for issue in issues:
                if issue.auto_fixable:
                    print(f"[MANAGER] Auto-handling: {issue.description}")
            print("[MANAGER] No critical issues found - proceeding with validation")
        
        return state
    
    @router(check_for_issues)
    def route_after_check(self, state: PlanningState) -> str:
        """Route to human review if critical issues found, otherwise validate."""
        if state.needs_human_review:
            print("[ROUTER] -> escalate_to_human")
            return "escalate_to_human"
        if state.current_step == "human_review":
            print("[ROUTER] -> human_review")
            return "human_review"
        print("[ROUTER] -> validate")
        return "validate"
    
    @listen("escalate_to_human")
    def human_review_critical(self, state: PlanningState):
        """Human-in-the-loop for critical issues.
        
        When @human_feedback is available, this returns content for review.
        Otherwise, uses terminal input as fallback.
        """
        print("\n" + "!"*60)
        print("CRITICAL ISSUE DETECTED - Manager needs your review")
        print("!"*60)
        print(state.manager_analysis)
        print(f"\nRecommendation: {state.manager_recommendation}")
        print("\nOptions: abort | investigate | fix_and_retry | override")
        
        choice = input("Your decision: ").strip().lower()
        
        if choice == "abort":
            state.success = False
            state.current_step = "done"
            state.final_report = f"ABORTED: {state.manager_analysis}"
        elif choice == "fix_and_retry":
            state.human_guidance = "User requested retry"
            state.current_step = "planning"
        elif choice == "override":
            state.needs_human_review = False
            state.current_step = "validate"
        else:  # investigate or default
            state.human_guidance = "investigate - proceed with caution"
            state.current_step = "validate"
            state.needs_human_review = False
        
        return state
    
    # Apply @human_feedback decorator if available (after class definition)
    # This will be done at module level after class is defined
    
    # Note: These handlers are for @human_feedback emit routing.
    # When @human_feedback is not available, human_review_critical handles
    # routing directly via terminal input and state modification.
    
    @listen("abort")
    def abort_workflow(self, result) -> PlanningState:
        """Abort the workflow due to critical issues (emit route)."""
        state = self.state
        feedback = getattr(result, 'feedback', '') if hasattr(result, 'feedback') else ''
        print(f"\nABORTED: Workflow stopped. Reason: {feedback}")
        state.success = False
        state.current_step = "done"
        state.final_report = f"ABORTED: {state.manager_analysis}\nUser feedback: {feedback}"
        return state
    
    @listen("investigate")
    def deep_investigation(self, result) -> PlanningState:
        """Continue with deeper investigation (emit route)."""
        state = self.state
        feedback = getattr(result, 'feedback', '') if hasattr(result, 'feedback') else ''
        print(f"\nINVESTIGATING: Proceeding with additional analysis")
        if feedback:
            print(f"User guidance: {feedback}")
            state.human_guidance = feedback
        state.current_step = "validate"
        state.needs_human_review = False
        return state
    
    @listen("fix_and_retry")
    def retry_with_guidance(self, result) -> PlanningState:
        """Retry planning with human guidance (emit route)."""
        state = self.state
        feedback = getattr(result, 'feedback', '') if hasattr(result, 'feedback') else ''
        print(f"\nFIXING: Retrying with guidance")
        if feedback:
            print(f"User guidance: {feedback}")
            state.human_guidance = feedback
        state.current_step = "planning"  # Will re-run planning crew
        return state
    
    @listen("override")
    def continue_despite_issues(self, result) -> PlanningState:
        """Continue despite issues - human override (emit route)."""
        state = self.state
        feedback = getattr(result, 'feedback', '') if hasattr(result, 'feedback') else ''
        print(f"\nOVERRIDE: Continuing despite issues")
        if feedback:
            print(f"Override reason: {feedback}")
        state.needs_human_review = False
        state.current_step = "validate"
        return state
    
    @listen("human_review")
    def review_unclear_gaps(self, state: PlanningState) -> PlanningState:
        """Human-in-the-loop step for reviewing unclear gaps.
        
        NOTE: @human_feedback decorator disabled - requires CrewAI version with HITL support.
        For now, just continue with validation.
        """
        print("\n[INFO] HITL feature disabled - continuing with validation")
        print("Unclear gaps found in design - proceeding without human review")
        state.current_step = "validate"
        return state
    
    @listen("deprecated")
    def handle_deprecated_gap(self, result) -> PlanningState:
        """Human classified gap as deprecated code - add removal to plan."""
        state = self.state
        print("\n[HUMAN FEEDBACK] Gap classified as DEPRECATED")
        print(f"Instructions: {result.feedback}")
        
        # Append removal instructions to design
        state.design_output += f"\n\n## HUMAN GUIDANCE: DEPRECATED CODE\n{result.feedback}\n"
        state.design_output += "Action: Remove deprecated code references in implementation.\n"
        state.current_step = "validate"
        return state
    
    @listen("implement")
    def handle_missing_implementation(self, result) -> PlanningState:
        """Human classified gap as missing feature - add implementation to plan."""
        state = self.state
        print("\n[HUMAN FEEDBACK] Gap classified as MISSING IMPLEMENTATION")
        print(f"Instructions: {result.feedback}")
        
        # Append implementation instructions to design
        state.design_output += f"\n\n## HUMAN GUIDANCE: IMPLEMENT FEATURE\n{result.feedback}\n"
        state.design_output += "Action: Include missing feature implementation in plan.\n"
        state.current_step = "validate"
        return state
    
    @listen("test_code")
    def handle_test_code_gap(self, result) -> PlanningState:
        """Human classified gap as test code - note and continue."""
        state = self.state
        print("\n[HUMAN FEEDBACK] Gap classified as TEST CODE (expected)")
        print(f"Note: {result.feedback}")
        
        # Note that gap is expected
        state.design_output += f"\n\n## HUMAN GUIDANCE: TEST CODE GAP (EXPECTED)\n{result.feedback}\n"
        state.current_step = "validate"
        return state
    
    @listen("continue")
    def handle_continue(self, result) -> PlanningState:
        """Human chose to continue with current plan."""
        state = self.state
        print("\n[HUMAN FEEDBACK] Continuing with current plan")
        if result.feedback:
            print(f"Additional notes: {result.feedback}")
            state.design_output += f"\n\n## HUMAN NOTES\n{result.feedback}\n"
        state.current_step = "validate"
        return state
    
    @listen(or_("validate", "deprecated", "implement", "test_code", "continue", "investigate", "override"))
    def validate_plan(self, state: PlanningState) -> PlanningState:
        """
        Step 5: Validate plan accuracy.
        
        Uses ConditionalTask for deep validation when needed.
        """
        print("\n" + "-"*60)
        print("[VALIDATE] Checking for hallucinations...")
        print("-"*60)
        
        # Define condition function for deep validation
        def needs_deep_validation(output: TaskOutput) -> bool:
            """Check if plan mentions complex systems requiring deep validation."""
            if not output or not output.raw:
                return False
            
            content = str(output.raw).lower()
            
            # Trigger deep validation for complex systems
            complex_keywords = [
                "contentorchestrator",
                "enlistmentbehavior",
                "save system",
                "state machine",
                "multi-system",
            ]
            
            return any(keyword in content for keyword in complex_keywords)
        
        # Basic validation task (always runs)
        basic_validation = Task(
            description=f"""
            Validate the planning document for accuracy:
            
            PLAN PATH: {state.plan_path}
            
            VALIDATION TASKS:
            1. Load the plan with load_plan tool
            2. For each file path: use verify_file_exists_tool
            3. For each method reference: use find_in_code to verify
            4. For each content ID: use lookup_content_id to check availability
            5. Report any hallucinations or errors
            
            OUTPUT FORMAT:
            ## Validation Result
            Status: PASSED or FAILED
            
            ## Issues Found (if any)
            - List each issue with details
            
            ## Verified Items
            - List items that checked out correctly
            """,
            expected_output="Validation report with PASSED or FAILED status",
            agent=get_code_analyst(),
        )
        
        # Deep validation task (conditional - only for complex plans)
        deep_validation = ConditionalTask(
            description=f"""
            Perform deep validation for complex system integration:
            
            PLAN PATH: {state.plan_path}
            
            DEEP CHECKS:
            1. Verify system integration points are correct
            2. Check for state management conflicts
            3. Validate save/load compatibility
            4. Ensure proper event lifecycle
            5. Verify behavior registration
            
            OUTPUT FORMAT:
            ## Deep Validation Result
            Status: PASSED or FAILED
            
            ## Integration Issues (if any)
            - List system integration concerns
            """,
            expected_output="Deep validation report for complex systems",
            condition=needs_deep_validation,
            agent=get_architecture_advisor(),
        )
        
        crew = Crew(
            agents=[get_code_analyst(), get_architecture_advisor()],
            tasks=[basic_validation, deep_validation],
            process=Process.sequential,
            verbose=True,
            memory=True,
            cache=True,
            planning=True,
            planning_llm=GPT5_PLANNING,
        )
        
        result = crew.kickoff()
        output = str(result)
        
        # Parse validation result
        output_lower = output.lower()
        if "status: passed" in output_lower or "validation: passed" in output_lower:
            state.validation_status = ValidationStatus.PASSED
            print("[OK] Validation PASSED")
        else:
            state.validation_status = ValidationStatus.FAILED
            # Extract issues
            if "issues found" in output_lower:
                state.validation_issues.append(output[:2000])
            print("[!] Validation FAILED - issues found")
        
        state.current_step = "route"
        return state
    
    @router(validate_plan)
    def route_validation(self, state: PlanningState) -> str:
        """
        Router: Decide next step based on validation.
        
        Uses condition functions for clean, testable routing logic.
        
        Returns:
        - "complete" if validation passed
        - "fix_plan" if validation failed and attempts remain
        - "complete" if max attempts reached (proceed anyway)
        """
        # Check if validation passed or fixed successfully
        if validation_passed(state) or validation_fixed(state):
            print(format_routing_decision(
                condition_name="validation_passed or validation_fixed",
                condition_result=True,
                chosen_path="complete",
                reason=f"Validation status: {state.validation_status.value}"
            ))
            return "complete"
        
        # Check if we should attempt a fix
        if needs_plan_fix(state):
            print(format_routing_decision(
                condition_name="needs_plan_fix",
                condition_result=True,
                chosen_path="fix_plan",
                reason=f"Validation failed, attempt {state.fix_attempts + 1} of {state.max_fix_attempts}"
            ))
            return "fix_plan"
        
        # Max attempts reached - proceed anyway
        print(format_routing_decision(
            condition_name="max_fix_attempts_reached",
            condition_result=True,
            chosen_path="complete",
            reason=f"Max fix attempts ({state.max_fix_attempts}) reached, proceeding with warnings"
        ))
        return "complete"
    
    @listen("fix_plan")
    def fix_plan(self, state: PlanningState) -> PlanningState:
        """Step 6 (conditional): Fix hallucinations in the plan."""
        print("\n" + "-"*60)
        print(f"[FIX] Correcting issues (attempt {state.fix_attempts + 1})...")
        print("-"*60)
        
        state.fix_attempts += 1
        
        task = Task(
            description=f"""
            Fix the issues found in the planning document:
            
            PLAN PATH: {state.plan_path}
            
            ISSUES FOUND:
            {state.validation_issues[-1] if state.validation_issues else "Unknown issues"}
            
            FIX TASKS:
            1. Load the plan with load_plan tool
            2. For each hallucinated file path: find the correct path
            3. For each wrong method: find the correct signature
            4. For each conflicting content ID: generate a new unique ID
            5. Save the corrected plan with save_plan tool
            
            Be precise - only fix what's actually wrong.
            """,
            expected_output="Corrected planning document",
            agent=get_documentation_maintainer(),
        )
        
        crew = Crew(
            agents=[get_documentation_maintainer()],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
            memory=True,
            cache=True,
            planning=True,
            planning_llm=GPT5_PLANNING,  # Use gpt-5 for planning
        )
        
        result = crew.kickoff()
        state.validation_status = ValidationStatus.FIXED
        state.current_step = "validate"
        return state
    
    @listen(fix_plan)
    def revalidate(self, state: PlanningState) -> PlanningState:
        """Re-run validation after fix."""
        return self.validate_plan(state)
    
    @listen(or_("complete", validate_plan))
    def generate_report(self, state: PlanningState) -> PlanningState:
        """Final step: Generate planning report."""
        # Only run if we're actually complete
        if state.current_step != "route" or state.validation_status not in [
            ValidationStatus.PASSED, ValidationStatus.FIXED
        ]:
            if state.fix_attempts < state.max_fix_attempts and state.validation_status == ValidationStatus.FAILED:
                return state  # Don't generate report yet
        
        print("\n" + "-"*60)
        print("[REPORT] Generating final report...")
        print("-"*60)
        
        report_parts = [
            "=" * 60,
            "PLANNING REPORT",
            "=" * 60,
            "",
            f"Feature: {state.feature_name}",
            f"Description: {state.description}",
            f"Plan Path: {state.plan_path}",
            "",
            "## Validation Status",
            f"Status: {state.validation_status.value}",
            f"Fix Attempts: {state.fix_attempts}",
            "",
        ]
        
        if state.validation_issues:
            report_parts.extend([
                "## Issues Addressed",
                *[f"- {issue[:200]}..." for issue in state.validation_issues],
                "",
            ])
        
        report_parts.extend([
            "## Next Steps",
            f"1. Review the plan at: {state.plan_path}",
            "2. Make any manual adjustments if needed",
            f"3. Run: enlisted-crew implement -p \"{state.plan_path}\"",
            "",
            "=" * 60,
        ])
        
        state.final_report = "\n".join(report_parts)
        state.success = True
        state.current_step = "done"
        
        print("\n" + "="*60)
        print("PLANNING FLOW COMPLETE")
        print("="*60)
        
        return state
