"""
Implementation Flow - CrewAI Flow-based workflow for feature implementation.

This Flow orchestrates agents to implement features from approved plans,
with smart detection of already-implemented components to avoid duplicating work.

The key insight: Before implementing anything, verify what already exists.
Use routers to skip steps that are already done.

Usage:
    from enlisted_crew.flows import ImplementationFlow
    
    flow = ImplementationFlow()
    result = flow.kickoff(inputs={
        "plan_path": "docs/CrewAI_Plans/my-feature.md",
    })
"""

import os
import json
import re
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

# Import state models
from .state_models import (
    ImplementationState,
    ImplementationStatus,
    ComponentCheck,
)

# Import condition functions
from .conditions import (
    needs_csharp_work,
    needs_content_work,
    csharp_complete,
    content_complete,
    all_work_complete,
    has_partial_implementation,
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
    # Planning
    load_plan,
    save_plan,
    parse_plan,
    get_plan_hash,
    # File Operations
    write_source,
    write_event,
    write_doc,
    update_localization,
    append_to_csproj,
    # Verification
    verify_file_exists_tool,
    list_event_ids,
    # Validation
    validate_content,
    sync_strings,
    build,
    # Code Review
    review_code,
    check_game_patterns,
    # Documentation
    read_doc_tool,
    list_docs_tool,
    find_in_docs,
    # Source Code
    read_source,
    find_in_code,
    read_source_section,
    # Database
    lookup_content_id,
    lookup_content_ids_batch,
    search_content,
    get_balance_value,
    get_tier_info,
    get_valid_categories,
    get_valid_severities,
    add_content_item,
    record_implementation,
    sync_content_from_files,
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

def validate_csharp_braces(output: TaskOutput) -> Tuple[bool, Any]:
    """Ensure C# code has balanced braces.
    
    Catches common syntax errors before they reach the build step.
    """
    content = str(output.raw)
    # Extract code blocks
    csharp_blocks = re.findall(r'```csharp\s*([\s\S]*?)```', content, re.IGNORECASE)
    csharp_blocks += re.findall(r'```c#\s*([\s\S]*?)```', content, re.IGNORECASE)
    
    for block in csharp_blocks:
        open_braces = block.count('{')
        close_braces = block.count('}')
        if open_braces != close_braces:
            return (False, f"Unbalanced braces in C# code: {open_braces} open vs {close_braces} close. Fix the code structure.")
    
    return (True, output)


def validate_csharp_has_code(output: TaskOutput) -> Tuple[bool, Any]:
    """Ensure C# implementation actually contains code if not skipped.
    
    Prevents empty implementations from passing through.
    """
    content = str(output.raw).lower()
    
    # Skip check if everything was already implemented
    if "all c# already implemented" in content or "already exists" in content:
        return (True, output)
    
    # Otherwise, we should see code blocks
    if "```csharp" not in content.lower() and "```c#" not in content.lower():
        if "class " not in content and "void " not in content and "public " not in content:
            return (False, "C# implementation task completed but no code was provided. Include the actual C# code in ```csharp blocks.")
    
    return (True, output)


def validate_json_syntax(output: TaskOutput) -> Tuple[bool, Any]:
    """Validate JSON blocks in content output.
    
    Catches malformed JSON before it's written to files.
    """
    content = str(output.raw)
    json_blocks = re.findall(r'```json\s*([\s\S]*?)```', content, re.IGNORECASE)
    
    for i, block in enumerate(json_blocks):
        try:
            json.loads(block.strip())
        except json.JSONDecodeError as e:
            return (False, f"Invalid JSON in block {i+1}: {e}. Fix the JSON syntax.")
    
    return (True, output)


def validate_content_ids_format(output: TaskOutput) -> Tuple[bool, Any]:
    """Ensure content IDs follow naming conventions.
    
    Content IDs should use snake_case and meaningful prefixes.
    """
    content = str(output.raw)
    
    # Skip if already implemented
    if "all content already implemented" in content.lower():
        return (True, output)
    
    # Look for ID definitions and check format
    id_patterns = re.findall(r'["\']([a-zA-Z_][a-zA-Z0-9_]*)["\']', content)
    
    # Check for camelCase IDs (should be snake_case)
    camel_case = [id for id in id_patterns if re.match(r'^[a-z]+[A-Z]', id) and len(id) > 3]
    if camel_case:
        return (False, f"Content IDs should use snake_case, not camelCase. Found: {camel_case[:3]}. Rename to snake_case format.")
    
    return (True, output)


# === LLM Configurations (OpenAI GPT-5 family) ===

def _get_env(name: str, default: str) -> str:
    return os.environ.get(name, default)


# =============================================================================
# LLM TIERS - reasoning_effort optimizes cost/performance
# high=deep thinking | medium=balanced | low=quick | none=instant
# =============================================================================

# HIGH reasoning - architecture/design decisions
GPT5_ARCHITECT = LLM(
    model=_get_env("ENLISTED_LLM_ARCHITECT", "gpt-5.2"),
    max_completion_tokens=16000,
    reasoning_effort="high",
)

# LOW reasoning - implementation from clear specs
GPT5_IMPLEMENTER = LLM(
    model=_get_env("ENLISTED_LLM_IMPLEMENTER", "gpt-5.2"),
    max_completion_tokens=12000,  # Increased for comprehensive implementations
    reasoning_effort="low",
)

# MEDIUM reasoning - QA needs to catch issues
GPT5_QA = LLM(
    model=_get_env("ENLISTED_LLM_QA", "gpt-5.2"),
    max_completion_tokens=8000,  # Increased for validation reports
    reasoning_effort="medium",
)

# NONE reasoning - fast content, no thinking needed
GPT5_FAST = LLM(
    model=_get_env("ENLISTED_LLM_FAST", "gpt-5.2"),
    max_completion_tokens=4000,
    reasoning_effort="none",
)

# Planning LLM - use simple string (LLM objects with reasoning_effort cause issues with AgentPlanner)
# See: https://docs.crewai.com/en/concepts/planning - examples all use simple strings
GPT5_PLANNING = _get_env("ENLISTED_LLM_PLANNING", "gpt-5.2")

# Function calling LLM - lightweight, no reasoning overhead for tool parameter extraction
# Used at Crew level for all agents' tool calls (per CrewAI docs recommendation)
GPT5_FUNCTION_CALLING = _get_env("ENLISTED_LLM_FUNCTION_CALLING", "gpt-5.2")


# === Agent Factory ===
# Module-level cache to avoid recreation

_agent_cache = {}


def get_implementation_manager() -> Agent:
    """Manager agent that coordinates the implementation workflow."""
    if "implementation_manager" not in _agent_cache:
        _agent_cache["implementation_manager"] = Agent(
            role="Implementation Lead",
            goal="Coordinate efficient implementation of approved plans with quality validation",
            backstory="""Technical lead who coordinates implementation teams from approved plans. 
            Delegates verification to analysts, coordinates C# and content work conditionally, and ensures QA validation. 
            Optimizes by skipping already-implemented work.""",
            llm=GPT5_ARCHITECT,
            allow_delegation=True,  # REQUIRED for hierarchical managers
            reasoning=True,  # Enable strategic coordination
            max_reasoning_attempts=2,  # Limit overthinking (prevent delays)
            max_iter=15,  # Reduced from default to prevent delegation delays
            max_retry_limit=3,
            verbose=True,
            respect_context_window=True,
        )
    return _agent_cache["implementation_manager"]


def get_systems_analyst() -> Agent:
    """Analyzer that reads plans and verifies existing implementation."""
    if "systems_analyst" not in _agent_cache:
        _agent_cache["systems_analyst"] = Agent(
            role="Systems Analyst",
            goal="Read implementation plans and verify what already exists in the codebase",
            backstory="""You verify implementation status using DATABASE TOOLS FIRST.

CRITICAL - TOOL-FIRST WORKFLOW (execute in order):
1. Call parse_plan to get structured list of content IDs and files
2. For content IDs:
   - If 3+ IDs: Use lookup_content_ids_batch("id1,id2,id3") (ONE call for all)
   - If 1-2 IDs: Use lookup_content_id("id") (one call per ID)
3. Call verify_file_exists_tool for EACH C# file (one call per file)
4. Use find_in_code only if you need to verify methods exist

BATCH TOOL FORMAT:
- CORRECT: lookup_content_ids_batch("event_1,event_2,event_3")  # Comma-separated string
- WRONG: lookup_content_ids_batch(["event_1", "event_2"])  # Not an array

DATABASE TOOLS ARE FAST - use them before reading files.
Report exactly: DONE (exists) vs NEEDED (not found).""",
            llm=GPT5_ARCHITECT,
            tools=[
                # Plan parsing tools FIRST - structured extraction
                parse_plan,
                get_plan_hash,
                # Database tools - fast lookups
                lookup_content_id,
                lookup_content_ids_batch,  # Use for 3+ IDs
                list_event_ids,
                # File verification tools
                verify_file_exists_tool,
                find_in_code,
                # Context (only if needed)
                load_plan,
            ],
            verbose=True,
            respect_context_window=True,
            max_iter=20,  # Thorough verification needs more iterations
            max_retry_limit=3,
            reasoning=True,  # Enable reflection for thorough analysis
            max_reasoning_attempts=3,  # Limit reasoning iterations
            allow_delegation=False,  # Specialists don't delegate (only managers do)
        )
    return _agent_cache["systems_analyst"]


def get_csharp_implementer() -> Agent:
    """C# code implementer."""
    if "csharp_implementer" not in _agent_cache:
        _agent_cache["csharp_implementer"] = Agent(
            role="C# Implementer",
            goal="Write C# code for Bannerlord mods following Enlisted patterns",
            backstory="""You write production C# code for the Enlisted mod.

You follow these patterns:
- Allman braces, _camelCase for fields, PascalCase for methods
- XML documentation on public members
- Proper null checks (Hero.MainHero != null)
- TextObject for user-visible strings
- Add new files to Enlisted.csproj

You only implement what's MISSING. If code already exists, skip it.""",
            llm=GPT5_IMPLEMENTER,
            tools=[
                # Core implementation tools
                read_source,
                write_source,
                append_to_csproj,
                update_localization,
                # Validation (run at end)
                review_code,
            ],
            verbose=True,
            respect_context_window=True,
            max_iter=25,  # Code writing may need multiple iterations
            max_retry_limit=3,
            reasoning=True,  # Plan code structure before implementing
            max_reasoning_attempts=3,  # Limit reasoning iterations
            allow_delegation=False,  # Specialist focuses on C# implementation
        )
    return _agent_cache["csharp_implementer"]


def get_content_author() -> Agent:
    """JSON content author."""
    if "content_author" not in _agent_cache:
        _agent_cache["content_author"] = Agent(
            role="Content Author",
            goal="Write JSON events, decisions, and orders following Enlisted schemas",
            backstory="""You create JSON content using DATABASE TOOLS FIRST.

CRITICAL - DATABASE BEFORE CREATING:
1. FIRST call lookup_content_id for EACH ID you plan to create
2. Call get_valid_categories to verify category names
3. Call get_valid_severities to verify severity values
4. ONLY create content with IDs that DON'T exist in database

CREATION WORKFLOW:
1. Create JSON with UNIQUE IDs (verified non-existent above)
2. Write JSON to ModuleData/Enlisted/Events/ (or appropriate folder)
3. Add localization strings to XML with update_localization
4. Run sync_strings and validate_content ONCE at end

Database lookups prevent duplicate IDs. Always verify first.
You only create what's MISSING. Skip existing content.""",
            llm=GPT5_FAST,
            tools=[
                # Core content tools
                write_event,
                update_localization,
                # Validation (run at end)
                validate_content,
                # Database tools
                get_valid_categories,
                lookup_content_id,
                add_content_item,  # Register new content in database immediately
            ],
            verbose=True,
            respect_context_window=True,
            max_iter=15,
            max_retry_limit=3,
            allow_delegation=False,  # Specialist focuses on JSON content creation
        )
    return _agent_cache["content_author"]


def get_qa_agent() -> Agent:
    """Quality assurance agent."""
    if "qa_agent" not in _agent_cache:
        _agent_cache["qa_agent"] = Agent(
            role="QA Specialist",
            goal="Validate that implementations build and follow standards",
            backstory="""You are the quality gate. You:

1. Run dotnet build to verify compilation
2. Run validate_content to check JSON/XML
3. Check code style compliance
4. Verify nothing was broken

Only approve when everything passes.""",
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


def get_documentation_maintainer() -> Agent:
    """Documentation maintainer."""
    if "documentation_maintainer" not in _agent_cache:
        _agent_cache["documentation_maintainer"] = Agent(
            role="Documentation Maintainer",
            goal="Update documentation to reflect implementations and maintain knowledge base",
            backstory="""You maintain project documentation. After implementation:

1. Update the plan file status to IMPLEMENTED
2. Add implementation summary to the plan
3. Update relevant feature docs in docs/Features/
4. Sync content database with sync_content_from_files
5. Record implementation in database

Disperse implementation details to permanent docs, don't leave them only in plan files.""",
            llm=GPT5_FAST,
            tools=[
                # Core doc tools
                write_doc,
                save_plan,
                # Database sync
                sync_content_from_files,
                record_implementation,
            ],
            verbose=True,
            respect_context_window=True,
            max_iter=10,
            max_retry_limit=3,
            allow_delegation=False,  # Specialist focuses on documentation
        )
    return _agent_cache["documentation_maintainer"]


# === The Flow ===

# === Condition Functions for ConditionalTasks ===

def needs_csharp_implementation(output: TaskOutput) -> bool:
    """Check if C# implementation is needed based on verification output."""
    if not output or not output.raw:
        return True  # Default to running if no output
    content = str(output.raw).lower()
    # Skip if verification says complete
    if "c# status: complete" in content or "csharp status: complete" in content:
        return False
    # Run if needed
    return "not_started" in content or "partial" in content or "still needed" in content


def needs_content_implementation(output: TaskOutput) -> bool:
    """Check if JSON content creation is needed based on verification output."""
    if not output or not output.raw:
        return True  # Default to running if no output
    content = str(output.raw).lower()
    # Skip if verification says complete
    if "content status: complete" in content:
        return False
    # Run if needed
    return "not_started" in content or "partial" in content or "still needed" in content


class ImplementationFlow(Flow[ImplementationState]):
    """
    Flow-based implementation workflow with hierarchical process.
    
    Uses a single hierarchical crew with ConditionalTasks to skip
    already-implemented components.
    
    State Persistence: Enabled via persist=True. If a run fails, you can resume
    from the last successful step by re-running with the same inputs.
    
    Steps:
    1. load_plan - Read the plan file
    2. run_implementation_crew - Single hierarchical crew with ConditionalTasks
    3. generate_report - Final summary
    """
    
    initial_state = ImplementationState
    persist = True  # Auto-save state to SQLite for recovery on failure
    
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        # Initialize execution monitoring
        self._monitor = EnlistedExecutionMonitor()
        print("[MONITORING] Execution monitoring enabled for ImplementationFlow")
    
    # === Flow Steps ===
    
    @start()
    def load_plan(self) -> ImplementationState:
        """Entry point: Load the implementation plan.
        
        Inputs are passed via kickoff(inputs={...}) and populate self.state.
        """
        print("\n" + "="*60)
        print("IMPLEMENTATION FLOW STARTED")
        print("="*60)
        
        # Access state - inputs populate state fields with matching names
        state = self.state
        plan_path = state.plan_path if hasattr(state, 'plan_path') else ""
        
        if not plan_path:
            print("[ERROR] No plan_path provided!")
            state.current_step = "error"
            state.success = False
            state.final_report = "Error: No plan_path provided in inputs"
            return state
        
        print(f"\nLoading plan: {plan_path}")
        
        # Read plan content
        project_root = get_project_root()
        full_path = project_root / plan_path
        
        if not full_path.exists():
            print(f"[ERROR] Plan not found: {full_path}")
            state.current_step = "error"
            state.success = False
            state.final_report = f"Error: Plan not found: {plan_path}"
            return state
        
        plan_content = full_path.read_text(encoding="utf-8")
        print(f"[OK] Plan loaded ({len(plan_content)} chars)")
        
        # Calculate and store plan hash for version tracking
        import hashlib
        plan_hash = hashlib.md5(plan_content.encode('utf-8')).hexdigest()[:12]
        print(f"[OK] Plan hash: {plan_hash}")
        
        state.plan_content = plan_content
        state.plan_hash = plan_hash
        state.current_step = "implement"
        return state
    
    @listen(load_plan)
    def run_implementation_crew(self, state: ImplementationState) -> ImplementationState:
        """Single hierarchical crew for all implementation work.
        
        Manager coordinates: verify -> C# (conditional) -> content (conditional) -> QA -> docs
        """
        print("\n" + "-"*60)
        print("[IMPLEMENTATION CREW] Running hierarchical implementation...")
        print("-"*60)
        
        # Task 1: Verify what already exists
        verify_task = Task(
            description=f"""
Verify what's already implemented for this plan.

PLAN PATH: {state.plan_path}
PLAN HASH: {state.plan_hash}

WORKFLOW (execute in order):
1. FIRST: call parse_plan with the plan path to get structured data
2. For each content_id: call lookup_content_id to check existence
3. For each csharp_file: call verify_file_exists_tool
4. Compile results into status report

TOOL EFFICIENCY RULES:
- Limit to ~8-12 tool calls total
- Use database tools (lookup_*) - they are FAST
- DO NOT read full files yet - just verify existence
- Each lookup_content_id takes ONE id (not arrays)

OUTPUT FORMAT:
## Already Implemented
- List files/IDs that EXIST

## Still Needed  
- List files/IDs that DON'T EXIST

## Status
- C# Status: COMPLETE / PARTIAL / NOT_STARTED
- Content Status: COMPLETE / PARTIAL / NOT_STARTED
""",
            expected_output="Verification report with implementation status",
            agent=get_systems_analyst(),
        )
        
        # Task 2: Implement C# code (CONDITIONAL - only if needed)
        csharp_task = ConditionalTask(
            description=f"""
Implement the C# code specified in this plan. Only implement what's MISSING.

PLAN (first 6000 chars):
{state.plan_content[:6000]}

WORKFLOW (execute in order):
1. FIRST: Check verification context - "Still Needed" section has the list
2. For each needed file: call read_source to see related code patterns
3. Write code following Enlisted patterns
4. Use write_source to create/modify files
5. Use append_to_csproj for NEW .cs files

TOOL EFFICIENCY RULES:
- Limit to ~10-15 tool calls total
- DO NOT re-verify existence - verification context above has this
- Only read files you need for context (not all files)
- Batch related writes together

CODE REQUIREMENTS:
- Follow Enlisted patterns: Allman braces, _camelCase, XML docs
- Proper null checks (Hero.MainHero != null)
- TextObject for user-visible strings

OUTPUT: If ALL already exists: "All C# already implemented"
Otherwise: List of files created/modified + summary
""",
            expected_output="C# implementation report",
            condition=needs_csharp_implementation,
            agent=get_csharp_implementer(),
            context=[verify_task],
            guardrails=[validate_csharp_braces, validate_csharp_has_code],
            guardrail_max_retries=2,
        )
        
        # Task 3: Implement JSON content (CONDITIONAL - only if needed)
        content_task = ConditionalTask(
            description=f"""
Create the JSON content specified in this plan. Only create what's MISSING.

PLAN (first 6000 chars):
{state.plan_content[:6000]}

WORKFLOW (execute in order):
1. FIRST: Check verification context - "Still Needed" section has the list
2. For each needed content: call write_event to create
3. Call update_localization for display strings
4. Call sync_strings once at end
5. Call validate_content to verify

TOOL EFFICIENCY RULES:
- Limit to ~8-12 tool calls total
- DO NOT call list_event_ids - verification context above has existing IDs
- Create content in batches where possible
- Call sync_strings and validate_content ONCE at end (not per file)

CONTENT REQUIREMENTS:
- Use snake_case for IDs
- Valid categories: camp_life, combat, company_events, crisis, dialogue, etc.
- Valid severities: normal, attention, critical, positive, info

OUTPUT: If ALL already exists: "All content already implemented"
Otherwise: List of content created + validation status
""",
            expected_output="Content implementation report",
            condition=needs_content_implementation,
            agent=get_content_author(),
            context=[verify_task],
            guardrails=[validate_json_syntax, validate_content_ids_format],
            guardrail_max_retries=2,
        )
        
        # Task 4: Validate everything builds (always runs)
        validate_task = Task(
            description="""
Validate that the implementation is correct.

WORKFLOW (execute in order):
1. Call build to verify C# compilation
2. Call validate_content to check JSON/XML
3. Review context above for any issues

TOOL EFFICIENCY RULES:
- Limit to ~2-3 tool calls total
- DO NOT re-read files - implementation context above has details
- Build and validate_content are the key checks

OUTPUT: PASS or FAIL with details
""",
            expected_output="Validation report with pass/fail status",
            agent=get_qa_agent(),
            context=[verify_task, csharp_task, content_task],
        )
        
        # Task 5: Update documentation (always runs)
        docs_task = Task(
            description=f"""
Update documentation to reflect the implementation.

PLAN FILE: {state.plan_path}

WORKFLOW (execute in order):
1. Call load_plan to get current plan content
2. Call save_plan with updated status ("Implemented" or "Partial")
3. Call sync_content_from_files to update content database
4. Call record_implementation with summary

TOOL EFFICIENCY RULES:
- Limit to ~4-5 tool calls total
- DO NOT re-read implementation details - context above has them
- sync_content_from_files updates database from actual files

OUTPUT: Documentation update confirmation
""",
            expected_output="Documentation update report",
            agent=get_documentation_maintainer(),
            context=[validate_task],
        )
        
        # Single hierarchical crew with manager coordination
        crew = Crew(
            agents=[
                get_systems_analyst(),
                get_csharp_implementer(),
                get_content_author(),
                get_qa_agent(),
                get_documentation_maintainer(),
            ],
            tasks=[verify_task, csharp_task, content_task, validate_task, docs_task],
            manager_agent=get_implementation_manager(),
            process=Process.hierarchical,
            verbose=True,
            **get_memory_config(),  # memory=True + contextual retrieval
            cache=True,
            planning=True,
            planning_llm=GPT5_PLANNING,
            function_calling_llm=GPT5_FUNCTION_CALLING,  # Lightweight LLM for tool calls
        )
        
        result = crew.kickoff()
        
        # Store outputs from hierarchical crew
        result_str = str(result)
        state.csharp_output = result_str
        state.content_output = result_str
        state.validation_output = result_str
        state.docs_output = result_str
        
        # Parse status from output
        output_lower = result_str.lower()
        if "c# status: complete" in output_lower or "all c# already implemented" in output_lower:
            state.csharp_status = ImplementationStatus.COMPLETE
        elif "c# status: partial" in output_lower:
            state.csharp_status = ImplementationStatus.PARTIAL
        
        if "content status: complete" in output_lower or "all content already implemented" in output_lower:
            state.content_status = ImplementationStatus.COMPLETE
        elif "content status: partial" in output_lower:
            state.content_status = ImplementationStatus.PARTIAL
        
        state.current_step = "report"
        return state
    
    @listen(run_implementation_crew)
    def check_for_issues(self, state: ImplementationState) -> ImplementationState:
        """Manager analyzes outputs and decides if human review needed.
        
        Detects:
        - Hallucinated APIs
        - Architecture violations
        - Scope creep
        """
        print("\n" + "-"*60)
        print("[MANAGER] Analyzing implementation outputs for issues...")
        print("-"*60)
        
        issues = []
        output_combined = (state.csharp_output + state.content_output + state.validation_output).lower()
        
        # Check for hallucinated API calls
        hallucinated_api_patterns = [
            "method not found",
            "does not exist",
            "cannot find",
            "no such method",
            "undefined method",
            "getneeds()",  # Common hallucination from context
        ]
        for pattern in hallucinated_api_patterns:
            if pattern in output_combined:
                issues.append(DetectedIssue(
                    issue_type=IssueType.HALLUCINATED_API,
                    severity=IssueSeverity.HIGH,
                    confidence=IssueConfidence.MEDIUM,
                    description=f"Implementation may use non-existent API",
                    affected_component="Implementation Output",
                    evidence=f"Pattern '{pattern}' found in output",
                    manager_recommendation="Verify API calls with find_in_code tool",
                    auto_fixable=True,
                ))
                break
        
        # Check for architecture violations
        architecture_violations = [
            "violates architecture",
            "breaks pattern",
            "anti-pattern",
            "not following convention",
            "bypassing manager",
        ]
        for pattern in architecture_violations:
            if pattern in output_combined:
                issues.append(DetectedIssue(
                    issue_type=IssueType.ARCHITECTURE_VIOLATION,
                    severity=IssueSeverity.CRITICAL,
                    confidence=IssueConfidence.MEDIUM,
                    description="Implementation violates architecture patterns",
                    affected_component="Implementation Output",
                    evidence=f"Pattern '{pattern}' found in output",
                    manager_recommendation="Review architecture docs and refactor",
                    auto_fixable=False,
                ))
                break
        
        # Check for scope creep
        scope_creep_patterns = [
            "also added",
            "additionally implemented",
            "bonus feature",
            "extra functionality",
            "while we're here",
        ]
        for pattern in scope_creep_patterns:
            if pattern in output_combined:
                issues.append(DetectedIssue(
                    issue_type=IssueType.SCOPE_CREEP,
                    severity=IssueSeverity.HIGH,
                    confidence=IssueConfidence.LOW,
                    description="Implementation may include scope creep",
                    affected_component="Implementation Output",
                    evidence=f"Pattern '{pattern}' found in output",
                    manager_recommendation="Review to ensure only planned features are implemented",
                    auto_fixable=False,
                ))
                break
        
        # Check for breaking changes
        if "breaking change" in output_combined or "backward compatibility" in output_combined:
            issues.append(DetectedIssue(
                issue_type=IssueType.BREAKING_CHANGE,
                severity=IssueSeverity.CRITICAL,
                confidence=IssueConfidence.MEDIUM,
                description="Implementation may include breaking changes",
                affected_component="Implementation Output",
                evidence="'breaking change' or 'backward compatibility' mentioned",
                manager_recommendation="Review save/load compatibility and migration paths",
                auto_fixable=False,
            ))
        
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
    def route_after_check(self, state: ImplementationState) -> str:
        """Route to report (human feedback disabled)."""
        if state.needs_human_review:
            print("[ROUTER] Critical issues detected but auto-proceeding (HITL disabled)")
            print(f"[ROUTER] Issues logged: {len(state.critical_issues)}")
            # Auto-resolve: log and continue to report
            state.needs_human_review = False
        print("[ROUTER] -> report")
        return "report"
    
    @listen("escalate_to_human")
    def human_review_critical(self, state: ImplementationState) -> ImplementationState:
        """Human-in-the-loop for critical issues - DISABLED (never reached)."""
        print("\n[MANAGER] Critical issues detected - auto-proceeding with investigation")
        if state.manager_analysis:
            print(f"[MANAGER] Analysis: {state.manager_analysis[:200]}...")
        state.human_guidance = "auto-investigate - HITL disabled"
        state.needs_human_review = False
        return state
    
    @listen("report")
    def generate_report(self, state: ImplementationState) -> ImplementationState:
        """Final step: Generate implementation report."""
        # Skip if aborted
        if state.current_step == "complete" and not state.success:
            return state
        
        print("\n" + "-"*60)
        print("[REPORT] Generating final report...")
        print("-"*60)
        
        report_parts = [
            "=" * 60,
            "IMPLEMENTATION REPORT",
            "=" * 60,
            "",
            f"Plan: {state.plan_path}",
            "",
            "## Status Summary",
            f"- C# Status: {state.csharp_status.value}",
            f"- Content Status: {state.content_status.value}",
            f"- Skipped Steps: {', '.join(state.skipped_steps) or 'None'}",
            "",
        ]
        
        if state.csharp_output:
            report_parts.extend([
                "## C# Implementation",
                state.csharp_output[:2000],
                "",
            ])
        
        if state.content_output:
            report_parts.extend([
                "## Content Implementation",
                state.content_output[:2000],
                "",
            ])
        
        report_parts.extend([
            "## Validation",
            state.validation_output[:1000],
            "",
            "## Documentation",
            state.docs_output[:1000],
            "",
            "=" * 60,
        ])
        
        state.final_report = "\n".join(report_parts)
        state.success = True
        state.current_step = "complete"
        
        print("\n" + "="*60)
        print("IMPLEMENTATION FLOW COMPLETE")
        print("="*60)
        
        return state
