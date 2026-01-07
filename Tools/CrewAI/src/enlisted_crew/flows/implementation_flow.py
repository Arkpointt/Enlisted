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
from typing import List, Optional

from crewai import Agent, Crew, Process, Task, LLM
from crewai.flow.flow import Flow, listen, router, start, or_
from pydantic import BaseModel, Field

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
    max_completion_tokens=8000,
    reasoning_effort="low",
)

# MEDIUM reasoning - QA needs to catch issues
GPT5_QA = LLM(
    model=_get_env("ENLISTED_LLM_QA", "gpt-5.2"),
    max_completion_tokens=6000,
    reasoning_effort="medium",
)

# NONE reasoning - fast content, no thinking needed
GPT5_FAST = LLM(
    model=_get_env("ENLISTED_LLM_FAST", "gpt-5.2"),
    max_completion_tokens=4000,
    reasoning_effort="none",
)

# LOW reasoning - planning from structured prompts
GPT5_PLANNING = LLM(
    model=_get_env("ENLISTED_LLM_PLANNING", "gpt-5.2"),
    max_completion_tokens=4000,
    reasoning_effort="low",
)


# === Agent Factory ===
# Module-level cache to avoid recreation

_agent_cache = {}


def get_implementation_manager() -> Agent:
    """Manager agent that coordinates the implementation workflow."""
    if "implementation_manager" not in _agent_cache:
        _agent_cache["implementation_manager"] = Agent(
            role="Implementation Lead",
            goal="Coordinate efficient implementation of approved plans with quality validation",
            backstory="""You lead an implementation team that builds features from approved plans.
            Your responsibilities:
            1. Verify what's already implemented (delegate to Systems Analyst)
            2. Coordinate C# implementation ONLY if needed (conditional)
            3. Coordinate content creation ONLY if needed (conditional)
            4. Ensure QA validates all changes
            5. Update documentation to reflect completed work
            
            You skip unnecessary work and ensure quality at each step.""",
            llm=GPT5_ARCHITECT,
            allow_delegation=True,  # REQUIRED for manager
            verbose=True,
            max_iter=35,
            max_retry_limit=3,
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
2. Call lookup_content_id for EACH content ID (one call per ID)
3. Call verify_file_exists_tool for EACH C# file (one call per file)
4. Use find_in_code only if you need to verify methods exist

TOOL CALL FORMAT: Each tool takes ONE argument, not arrays.
- CORRECT: lookup_content_id("event_1") then lookup_content_id("event_2")
- WRONG: lookup_content_id(["event_1", "event_2"])

DATABASE TOOLS ARE FAST - use them before reading files.
Report exactly: DONE (exists) vs NEEDED (not found).""",
            llm=GPT5_ARCHITECT,
            tools=[
                # Plan parsing tools FIRST - structured extraction
                parse_plan,
                get_plan_hash,
                # Database tools - fast lookups
                lookup_content_id,
                search_content,
                list_event_ids,
                # File verification tools
                verify_file_exists_tool,
                find_in_code,
                # Only use these if needed for context
                load_plan,
                read_source,
                read_doc_tool,
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
                read_source,
                find_in_code,
                write_source,
                append_to_csproj,
                update_localization,
                verify_file_exists_tool,
                review_code,
                check_game_patterns,
                get_balance_value,
                get_tier_info,
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
            backstory="""You create JSON content for the Enlisted mod.

Critical workflow:
1. Check existing IDs with list_event_ids first
2. Create JSON with UNIQUE IDs (not duplicating existing)
3. Write JSON to ModuleData/Enlisted/Events/ (or appropriate folder)
4. Add localization strings to XML
5. Run sync_strings and validate_content to verify

Valid categories: camp_life, combat, company_events, crisis, dialogue, 
discipline, downtime, equipment, escalation, faction, fatigue, 
formation, identity, leave, logistics, maintenance, morale, officer, 
order, progression, quartermaster, reaction, reputation, rest, 
retinue, supply, training

Valid severities: normal, attention, critical, positive, info

You only create what's MISSING. Skip existing content.""",
            llm=GPT5_FAST,
            tools=[
                list_event_ids,
                write_event,
                update_localization,
                sync_strings,
                validate_content,
                get_valid_categories,
                get_valid_severities,
                lookup_content_id,
                add_content_item,
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
                read_doc_tool,
                write_doc,
                find_in_docs,
                load_plan,
                save_plan,
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
        
        Manager coordinates: verify → C# (conditional) → content (conditional) → QA → docs
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
            
            CRITICAL - TOOL-FIRST WORKFLOW:
            1. Call parse_plan with the plan path to get structured data
            2. For each content_id: call lookup_content_id to check existence
            3. For each csharp_file: call verify_file_exists_tool
            
            REPORT FORMAT:
            ## Already Implemented
            - List all files/IDs that ALREADY EXIST
            
            ## Still Needed  
            - List all files/IDs that DON'T EXIST
            
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
            Implement the C# code specified in this plan.
            
            CRITICAL: Only implement what's MISSING. Skip anything that already exists.
            
            PLAN:
            {state.plan_content[:6000]}
            
            STEPS:
            1. Review the plan for C# requirements
            2. Use verify_file_exists_tool before creating any file
            3. Only write code for what's MISSING
            4. Follow Enlisted patterns: Allman braces, _camelCase, XML docs
            5. Use write_source to create/modify files
            6. Use append_to_csproj for new .cs files
            
            If EVERYTHING already exists, say "All C# already implemented".
            """,
            expected_output="C# implementation report",
            condition=needs_csharp_implementation,
            agent=get_csharp_implementer(),
            context=[verify_task],
        )
        
        # Task 3: Implement JSON content (CONDITIONAL - only if needed)
        content_task = ConditionalTask(
            description=f"""
            Create the JSON content specified in this plan.
            
            CRITICAL: Only create what's MISSING. Skip anything that already exists.
            
            PLAN:
            {state.plan_content[:6000]}
            
            WORKFLOW:
            1. Call list_event_ids FIRST to see all existing IDs
            2. Only create events/decisions with NEW IDs
            3. Use write_event for each new piece of content
            4. Use update_localization for display strings
            5. Call sync_strings and validate_content to verify
            
            If EVERYTHING already exists, say "All content already implemented".
            """,
            expected_output="Content implementation report",
            condition=needs_content_implementation,
            agent=get_content_author(),
            context=[verify_task],
        )
        
        # Task 4: Validate everything builds (always runs)
        validate_task = Task(
            description="""
            Validate that the implementation is correct:
            
            VALIDATION CHECKLIST:
            1. Run build tool to verify C# compilation
            2. Run validate_content to check JSON/XML
            3. Check for any obvious issues
            
            Report: PASS or FAIL with details.
            """,
            expected_output="Validation report with pass/fail status",
            agent=get_qa_agent(),
            context=[verify_task, csharp_task, content_task],
        )
        
        # Task 5: Update documentation (always runs)
        docs_task = Task(
            description=f"""
            Update documentation to reflect the implementation:
            
            PLAN FILE: {state.plan_path}
            
            TASKS:
            1. Update plan status to "Implemented" or "Partial"
            2. Add Implementation Summary section
            3. Call sync_content_from_files to update content database
            4. Call record_implementation with summary
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
            memory=True,
            cache=True,
            planning=True,
            planning_llm=GPT5_PLANNING,
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
    def generate_report(self, state: ImplementationState) -> ImplementationState:
        """Final step: Generate implementation report."""
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
