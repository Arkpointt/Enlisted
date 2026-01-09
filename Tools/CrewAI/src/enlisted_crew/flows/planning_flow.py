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
import time
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

from ..memory_config import get_memory_config
from ..monitoring import EnlistedExecutionMonitor

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

# Import RAG tool
from ..rag.codebase_rag_tool import search_codebase

# Import prompt templates for caching optimization (Phase 3)
from ..prompts import (
    ARCHITECTURE_PATTERNS,
    RESEARCH_WORKFLOW,
    DESIGN_WORKFLOW,
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


# =============================================================================
# TOOL CALL MONITOR - Tracks agent tool usage and warns at thresholds
# =============================================================================

_tool_call_counts = {}  # Track per-agent tool calls


def _tool_call_monitor(step_output) -> None:
    """Step callback to monitor tool calls per agent.
    
    CrewAI best practice: Use step_callback to track and limit tool usage.
    Warns when agent exceeds threshold, helping identify runaway loops.
    
    Args:
        step_output: The output from the current agent step
    """
    try:
        # Get agent name from step output
        agent_name = getattr(step_output, 'agent', 'unknown')
        if hasattr(agent_name, 'role'):
            agent_name = agent_name.role
        
        # Initialize or increment count
        if agent_name not in _tool_call_counts:
            _tool_call_counts[agent_name] = 0
        _tool_call_counts[agent_name] += 1
        
        count = _tool_call_counts[agent_name]
        
        # Log at thresholds
        if count == 5:
            print(f"[MONITOR] {agent_name}: 5 tool calls - on track")
        elif count == 8:
            print(f"[MONITOR] {agent_name}: 8 tool calls - approaching limit")
        elif count == 10:
            print(f"[WARNING] {agent_name}: 10 tool calls - consider wrapping up")
        elif count > 10 and count % 5 == 0:
            print(f"[WARNING] {agent_name}: {count} tool calls - excessive usage detected")
            
    except Exception as e:
        # Don't let monitoring errors break the flow
        pass


def reset_tool_call_monitor() -> None:
    """Reset tool call counts between workflow runs."""
    global _tool_call_counts
    _tool_call_counts = {}


# === LLM Configurations (OpenAI GPT-5 family) ===

def _get_env(name: str, default: str) -> str:
    return os.environ.get(name, default)


# =============================================================================
# LLM TIERS - reasoning_effort optimizes cost/performance
# high=deep thinking | medium=balanced | low=quick | none=instant
# 2026 Best Practice: Medium reasoning matches high quality for most tasks
# =============================================================================

# HIGH reasoning - only for deepest design work (feature_architect)
GPT5_ARCHITECT_HIGH = LLM(
    model=_get_env("ENLISTED_LLM_ARCHITECT", "gpt-5.2"),
    max_completion_tokens=16000,
    reasoning_effort="high",
)

# MEDIUM reasoning - strategic coordination, research, analysis (manager, analysts, advisor)
GPT5_ARCHITECT = LLM(
    model=_get_env("ENLISTED_LLM_ARCHITECT", "gpt-5.2"),
    max_completion_tokens=16000,
    reasoning_effort="medium",
)

# MEDIUM reasoning - analysis and verification
GPT5_ANALYST = LLM(
    model=_get_env("ENLISTED_LLM_ANALYST", "gpt-5.2"),
    max_completion_tokens=12000,  # Increased for validation reports
    reasoning_effort="medium",
)

# LOW reasoning - documentation from clear structure
GPT5_DOCS = LLM(
    model=_get_env("ENLISTED_LLM_IMPLEMENTER", "gpt-5.2"),
    max_completion_tokens=12000,  # Increased for comprehensive planning docs
    reasoning_effort="low",
)

# Planning LLM - use simple string (LLM objects with reasoning_effort cause issues with AgentPlanner)
# See: https://docs.crewai.com/en/concepts/planning - examples all use simple strings
GPT5_PLANNING = _get_env("ENLISTED_LLM_PLANNING", "gpt-5.2")

# Function calling LLM - lightweight, no reasoning overhead for tool parameter extraction
# Used at Crew level for all agents' tool calls (per CrewAI docs recommendation)
GPT5_FUNCTION_CALLING = _get_env("ENLISTED_LLM_FUNCTION_CALLING", "gpt-5.2")


# === Agent Factory ===

_agent_cache = {}


def get_planning_manager() -> Agent:
    """Manager agent that coordinates the planning workflow."""
    if "planning_manager" not in _agent_cache:
        _agent_cache["planning_manager"] = Agent(
            role="Planning Coordinator",
            goal="Efficiently coordinate research, design, and documentation to produce high-quality feature plans",
            backstory="""Experienced technical project manager who coordinates planning teams. 
            Delegates research to analysts, design to architects, and documentation to writers. 
            Validates quality and ensures coherent output before proceeding.""",
            llm=GPT5_ARCHITECT,
            allow_delegation=True,  # REQUIRED for hierarchical managers
            reasoning=False,  # DISABLED: Prevents reasoning loops (2026 best practice - managers coordinate, don't plan)
            max_iter=20,  # Minimum for manager coordination (test requirement)
            max_retry_limit=3,
            verbose=True,
            respect_context_window=True,
        )
    return _agent_cache["planning_manager"]


def get_systems_analyst() -> Agent:
    """Research existing systems and code."""
    if "systems_analyst" not in _agent_cache:
        _agent_cache["systems_analyst"] = Agent(
            role="Systems Analyst",
            goal="Research existing Enlisted systems to understand integration points",
            backstory="""You research systems using PRE-LOADED CONTEXT first, then database tools.

CRITICAL - USE PRE-LOADED CONTEXT:
1. Game systems overview is PRE-LOADED in your task description - read it there
2. Call get_system_dependencies for specific systems you need details on
3. Call lookup_api_pattern for Bannerlord API usage patterns
4. ONLY use search_codebase for NEW queries not in pre-loaded context

Database tools are FAST and cached. Code search is SLOW.
Limit to 5-8 tool calls total. Never re-search same query.

STOP CONDITION: Once you have dependencies for 2-3 relevant systems, STOP researching.""",
            llm=GPT5_ARCHITECT,
            tools=[
                search_codebase,  # Fast semantic search - PRIMARY tool
                read_source,  # Read specific files
                get_system_dependencies,  # Database lookup
                lookup_api_pattern,  # Database lookup
                # REMOVED: find_in_docs, find_in_code, list_feature_files_tool (redundant with search_codebase)
            ],
            verbose=True,
            respect_context_window=True,
            max_iter=8,  # Reduced from 15 - prevents over-research
            max_execution_time=120,  # 2 minute hard limit
            max_retry_limit=3,
            reasoning=True,  # Plan research strategy before executing
            max_reasoning_attempts=2,  # Reduced from 3
            allow_delegation=False,
            step_callback=_tool_call_monitor,  # Monitor tool usage
        )
    return _agent_cache["systems_analyst"]


def get_architecture_advisor() -> Agent:
    """Suggest best practices and improvements."""
    if "architecture_advisor" not in _agent_cache:
        _agent_cache["architecture_advisor"] = Agent(
            role="Architecture Advisor",
            goal="Suggest best practices and architectural improvements",
            backstory="""Architecture expert who suggests best practices and design patterns.

You RECEIVE research from Systems Analyst - use that context, don't re-research.
Only use tools to verify specific patterns or check balance values.

STOP CONDITION: Provide 3-5 concrete recommendations based on existing research. Don't explore tangents.""",
            llm=GPT5_ARCHITECT,
            tools=[
                search_codebase,  # For verification only
                read_source,  # Read specific files
                get_tier_info,  # Database lookup
                get_balance_value,  # Database lookup
                # REMOVED: find_in_docs, find_in_code (redundant with search_codebase)
            ],
            verbose=True,
            respect_context_window=True,
            max_iter=8,  # Reduced from 15
            max_execution_time=90,  # 90 second limit
            max_retry_limit=3,
            reasoning=True,
            max_reasoning_attempts=2,  # Reduced from 3
            allow_delegation=False,
            step_callback=_tool_call_monitor,
        )
    return _agent_cache["architecture_advisor"]


def get_feature_architect() -> Agent:
    """Design the technical specification."""
    if "feature_architect" not in _agent_cache:
        _agent_cache["feature_architect"] = Agent(
            role="Feature Architect",
            goal="Design complete technical specifications AND identify gaps in existing code",
            backstory="""You design specs using DATABASE TOOLS FIRST to verify IDs exist.

CRITICAL - DATABASE BEFORE CODE:
1. FIRST call lookup_content_id for EACH content ID you plan to reference
2. Call list_event_ids to see existing IDs in a category
3. Call get_tier_info for tier-related features
4. Use search_codebase only if database doesn't have the info

Database tools prevent hallucinated IDs. Always verify before referencing.
Limit to 8-12 tool calls total.

STOP CONDITION: Once you have verified IDs and designed the spec, STOP. Don't explore alternatives.""",
            llm=GPT5_ARCHITECT_HIGH,  # Only agent using high reasoning - complex design work
            tools=[
                search_codebase,  # Fast semantic search
                read_source,
                list_event_ids,  # Database lookup
                lookup_content_id,  # Database lookup
                get_tier_info,  # Database lookup
                # REMOVED: find_in_code (redundant with search_codebase)
            ],
            verbose=True,
            respect_context_window=True,
            max_iter=12,  # Reduced from 25
            max_execution_time=180,  # 3 minute limit for complex design
            max_retry_limit=3,
            reasoning=True,
            max_reasoning_attempts=2,  # Reduced from 3
            allow_delegation=False,
            step_callback=_tool_call_monitor,
        )
    return _agent_cache["feature_architect"]


def get_documentation_maintainer() -> Agent:
    """Write planning documents."""
    if "documentation_maintainer" not in _agent_cache:
        _agent_cache["documentation_maintainer"] = Agent(
            role="Documentation Maintainer",
            goal="Write clear, complete planning documents",
            backstory="""Technical writer who creates clear, comprehensive planning documents 
            that serve as the source of truth for implementation.""",
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
            backstory="""You validate plans using DATABASE TOOLS FIRST for fast lookups.

CRITICAL - DATABASE BEFORE CODE:
1. FIRST call lookup_content_id for EACH content ID in the plan
2. Call verify_file_exists_tool for EACH file path
3. Call list_event_ids to check categories
4. ONLY use search_codebase if database doesn't answer

Database lookups are instant. Code search is slower.
Report: VALID (exists) vs HALLUCINATED (not found).

STOP CONDITION: Once each ID and file is checked ONCE, produce report. Don't re-verify.""",
            llm=GPT5_ANALYST,
            tools=[
                lookup_content_id,  # Database - check IDs
                verify_file_exists_tool,  # File system - check paths
                list_event_ids,  # Database - list categories
                search_codebase,  # Semantic search fallback
                read_source,  # Read files for verification
                # REMOVED: find_in_code, search_content (redundant)
            ],
            verbose=True,
            respect_context_window=True,
            max_iter=10,  # Reduced from 20
            max_execution_time=90,  # 90 second limit
            max_retry_limit=3,
            step_callback=_tool_call_monitor,
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
    2. run_planning_crew - Single hierarchical crew (research -> advise -> design -> write)
    3. check_for_issues - Manager analyzes outputs
    4. route_after_check - Route to validation
    5. validate_plan - Check for hallucinations
    6. route_validation - Fix issues or complete
    7. fix_plan - (conditional) Correct any issues
    8. generate_report - Final summary
    """
    
    initial_state = PlanningState
    persist = True  # Auto-save state to SQLite for recovery on failure
    
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        # Clear agent cache to ensure latest agent configurations are used
        global _agent_cache
        _agent_cache.clear()
        # Initialize execution monitoring
        self._monitor = EnlistedExecutionMonitor()
        print("[MONITORING] Execution monitoring enabled for PlanningFlow")
    
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
        
        Manager coordinates: research -> advise -> design -> write
        """
        print("\n" + "-"*60)
        print("[PLANNING CREW] Running hierarchical planning...")
        print("-"*60)
        
        # Pre-populate tool cache to avoid duplicate calls
        if not state.cached_game_systems:
            print("[CACHE] Pre-loading game systems...")
            try:
                state.cached_game_systems = get_game_systems.run()
                print(f"[CACHE] Cached game systems ({len(state.cached_game_systems)} chars)")
            except Exception as e:
                print(f"[CACHE] Warning: Failed to cache game systems: {e}")
        
        if not state.cached_architecture:
            print("[CACHE] Pre-loading architecture docs...")
            try:
                state.cached_architecture = get_architecture.run()
                print(f"[CACHE] Cached architecture ({len(state.cached_architecture)} chars)")
            except Exception as e:
                print(f"[CACHE] Warning: Failed to cache architecture: {e}")
        
        if not state.cached_dev_reference:
            print("[CACHE] Pre-loading dev reference...")
            try:
                state.cached_dev_reference = get_dev_reference.run()
                print(f"[CACHE] Cached dev reference ({len(state.cached_dev_reference)} chars)")
            except Exception as e:
                print(f"[CACHE] Warning: Failed to cache dev reference: {e}")
        
        plan_path = f"docs/CrewAI_Plans/{state.feature_name}.md"
        
        # Task 1: Research existing systems
        research_task = Task(
            description=f"""{ARCHITECTURE_PATTERNS}
{RESEARCH_WORKFLOW}
{TOOL_EFFICIENCY_RULES}

=== YOUR TASK ===
Research existing Enlisted systems for this feature.

**Feature Information:**
- FEATURE: {state.feature_name}
- DESCRIPTION: {state.description}
- RELATED SYSTEMS: {state.related_systems or "To be determined"}

=== PRE-LOADED CONTEXT (DO NOT RE-FETCH) ===
{state.cached_game_systems[:3000]}
=== END PRE-LOADED CONTEXT ===

**Expected Output:**
- Related systems and their roles
- Key files and methods with paths
- Integration points
- Risks or dependencies
            """,
            expected_output="Research summary of existing systems",
            agent=get_systems_analyst(),
        )
        
        # Task 2: Suggest architectural improvements (depends on research)
        advise_task = Task(
            description=f"""{ARCHITECTURE_PATTERNS}
{DESIGN_WORKFLOW}
{TOOL_EFFICIENCY_RULES}

=== YOUR TASK ===
Suggest architectural improvements and best practices for this feature.

**Feature Information:**
- FEATURE: {state.feature_name}
- DESCRIPTION: {state.description}

=== PRE-LOADED CONTEXT (DO NOT RE-FETCH) ===
{state.cached_architecture[:3000]}
=== END PRE-LOADED CONTEXT ===

**Expected Output:**
- Suggested architectural patterns
- Risks to avoid
- Tier considerations
- Balance considerations
            """,
            expected_output="Architectural recommendations",
            agent=get_architecture_advisor(),
            context=[research_task],  # Depends on research
        )
        
        # Task 3: Design technical specification (depends on research + advice)
        design_task = Task(
            description=f"""{ARCHITECTURE_PATTERNS}
{DESIGN_WORKFLOW}
{TOOL_EFFICIENCY_RULES}

=== YOUR TASK ===
Design the technical specification for this feature based on research and architectural advice.

**Feature Information:**
- FEATURE: {state.feature_name}
- DESCRIPTION: {state.description}

=== PRE-LOADED CONTEXT (DO NOT RE-FETCH) ===
{state.cached_dev_reference[:3000]}
=== END PRE-LOADED CONTEXT ===

**Expected Output - Technical Specification:**
- Files to Create: [specific paths]
- Files to Modify: [specific paths with changes]
- Content IDs: [specific IDs in snake_case, verified unique]
- Localization Keys: [XML string IDs needed]
- Integration Points: [where to hook into existing systems]
- Testing Approach: [how to verify it works]
- Gap Analysis: [what's missing that needs investigation]
            """,
            expected_output="Technical specification document",
            agent=get_feature_architect(),
            context=[research_task, advise_task],  # Depends on both
        )
        
        # Task 4: Write planning document (depends on design)
        write_task = Task(
            description=f"""{ARCHITECTURE_PATTERNS}

=== YOUR TASK ===
Write the comprehensive planning document for this feature.

**Output Requirements:**
- FEATURE: {state.feature_name}
- OUTPUT PATH: {plan_path}

**Document Structure Required:**
1. Title and Status (mark as "Planning")
2. Overview - what the feature does
3. Technical Specification - from design task
4. Files to Create/Modify - specific paths
5. Content IDs - all event/decision/order IDs
6. Implementation Checklist - discrete steps
7. Validation Criteria - how to verify it works

**Tool to Use:**
- Call save_plan tool with complete document content
- Include ALL technical details from the design
- Use markdown formatting
- NO placeholder paths (PLACEHOLDER, TODO_PATH, path/to/)
            """,
            expected_output="Planning document saved to disk",
            agent=get_documentation_maintainer(),
            context=[design_task],  # Depends on design
            guardrails=[validate_plan_structure, validate_no_placeholder_paths],
            guardrail_max_retries=2,
        )
        
        # Sequential crew - Flow handles coordination, tasks execute in order
        # Per CrewAI best practices (Dec 2025): "Flow enforces business logic, agents provide intelligence within steps"
        # Uses optimized memory config with truncating storage to prevent token limit errors
        crew = Crew(
            name="Planning Crew - Feature Design",
            agents=[
                get_systems_analyst(),
                get_architecture_advisor(),
                get_feature_architect(),
                get_documentation_maintainer(),
            ],
            tasks=[research_task, advise_task, design_task, write_task],
            # manager_agent REMOVED - Flow handles coordination (Phase 2.5 optimization)
            process=Process.sequential,  # Tasks execute in defined order - deterministic and cacheable
            verbose=True,
            **get_memory_config(),  # memory=True + truncating storage
            cache=True,
            planning=True,
            planning_llm=GPT5_PLANNING,
            function_calling_llm=GPT5_FUNCTION_CALLING,  # Lightweight LLM for tool calls
        )
        
        start_time = time.time()
        result = crew.kickoff()
        elapsed = time.time() - start_time
        
        # Progress tracking
        try:
            token_usage = result.token_usage if hasattr(result, 'token_usage') else None
            if token_usage:
                print(f"\n[PROGRESS] Phase 1 (Planning Crew) Complete:")
                print(f"  Time: {elapsed:.1f}s")
                print(f"  Tokens: {token_usage}")
        except Exception as e:
            print(f"\n[PROGRESS] Phase 1 (Planning Crew) Complete: {elapsed:.1f}s")
        
        # Extract outputs from crew result
        result_str = str(result)
        state.design_output = result_str
        state.plan_path = plan_path
        state.current_step = "check_issues"
        
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
        
        # Note: UNCLEAR gaps detection removed - manager proceeds automatically
        
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
        
        # Log issues but proceed automatically (no human review)
        if issues:
            print(f"[MANAGER] Found {len(issues)} issue(s) - logged for review")
            state.critical_issues = [str(i) for i in issues]
            for issue in issues:
                print(f"[MANAGER] Issue: {issue.description}")
        else:
            print("[MANAGER] No issues found - proceeding with validation")
        
        return state
    
    @router(check_for_issues)
    def route_after_check(self, state: PlanningState) -> str:
        """Route to validation (fully automated)."""
        print("[ROUTER] -> validate")
        return "validate"
    
    @listen("validate")
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
            name="Validation Crew - Plan Verification",
            agents=[get_code_analyst(), get_architecture_advisor()],
            tasks=[basic_validation, deep_validation],
            process=Process.sequential,
            verbose=True,
            **get_memory_config(),  # memory=True + truncating storage
            cache=True,
            planning=True,
            planning_llm=GPT5_PLANNING,
            function_calling_llm=GPT5_FUNCTION_CALLING,  # Lightweight LLM for tool calls
        )
        
        start_time = time.time()
        result = crew.kickoff()
        elapsed = time.time() - start_time
        
        # Progress tracking
        try:
            token_usage = result.token_usage if hasattr(result, 'token_usage') else None
            if token_usage:
                print(f"\n[PROGRESS] Phase 2 (Validation Crew) Complete:")
                print(f"  Time: {elapsed:.1f}s")
                print(f"  Tokens: {token_usage}")
        except Exception as e:
            print(f"\n[PROGRESS] Phase 2 (Validation Crew) Complete: {elapsed:.1f}s")
        
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
            name="Fix Crew - Plan Corrections",
            agents=[get_documentation_maintainer()],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
            **get_memory_config(),  # memory=True + truncating storage
            cache=True,
            planning=True,
            planning_llm=GPT5_PLANNING,
            function_calling_llm=GPT5_FUNCTION_CALLING,  # Lightweight LLM for tool calls
        )
        
        start_time = time.time()
        result = crew.kickoff()
        elapsed = time.time() - start_time
        
        # Progress tracking
        try:
            token_usage = result.token_usage if hasattr(result, 'token_usage') else None
            if token_usage:
                print(f"\n[PROGRESS] Phase 3 (Fix Plan) Complete:")
                print(f"  Time: {elapsed:.1f}s")
                print(f"  Tokens: {token_usage}")
        except Exception as e:
            print(f"\n[PROGRESS] Phase 3 (Fix Plan) Complete: {elapsed:.1f}s")
        
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
