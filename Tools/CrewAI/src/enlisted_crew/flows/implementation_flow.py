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


def get_systems_analyst() -> Agent:
    """Analyzer that reads plans and verifies existing implementation."""
    if "systems_analyst" not in _agent_cache:
        _agent_cache["systems_analyst"] = Agent(
            role="Systems Analyst",
            goal="Read implementation plans and verify what already exists in the codebase",
            backstory="""You analyze feature plans and check what's already implemented.

Your job is to:
1. Read the plan file thoroughly
2. Extract all C# files/methods that need to be created
3. Extract all JSON content IDs (events, decisions, orders) that need to be created
4. Check if each one ALREADY EXISTS in the codebase
5. Report exactly what's DONE vs what's STILL NEEDED

This prevents duplicate work and wasted effort.""",
            llm=GPT5_ARCHITECT,
            tools=[
                load_plan,
                read_doc_tool,
                verify_file_exists_tool,
                list_event_ids,
                find_in_code,
                read_source,
                lookup_content_id,
                search_content,
            ],
            verbose=True,
            respect_context_window=True,
            max_iter=20,  # Thorough verification needs more iterations
            max_retry_limit=3,
            reasoning=True,  # Enable reflection for thorough analysis
            max_reasoning_attempts=3,  # Limit reasoning iterations
            allow_delegation=True,  # Coordinator can delegate verification tasks
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

class ImplementationFlow(Flow[ImplementationState]):
    """
    Flow-based implementation workflow with smart partial-implementation handling.
    
    Key feature: Verifies what already exists BEFORE implementing.
    Routes around completed work to avoid duplicates.
    
    State Persistence: Enabled via persist=True. If a run fails, you can resume
    from the last successful step by re-running with the same inputs.
    
    Steps:
    1. load_plan - Read the plan file
    2. verify_existing - Check what's already implemented
    3. route_csharp - Decide if C# work is needed
    4. implement_csharp - (conditional) Write C# code
    5. route_content - Decide if content work is needed
    6. implement_content - (conditional) Write JSON content
    7. validate_all - Validate everything builds
    8. update_docs - Update documentation and database
    9. generate_report - Final summary
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
        
        state.plan_content = plan_content
        state.current_step = "verify"
        return state
    
    @listen(load_plan)
    def verify_existing(self, state: ImplementationState) -> ImplementationState:
        """
        Step 2: Analyze plan and verify what already exists.
        
        This is the KEY step that prevents duplicate work.
        """
        print("\n" + "-"*60)
        print("[VERIFY] Checking what's already implemented...")
        print("-"*60)
        
        task = Task(
            description=f"""
            Analyze this implementation plan and verify what already exists:
            
            PLAN CONTENT:
            {state.plan_content[:8000]}
            
            YOUR TASKS:
            1. Extract ALL C# files/methods mentioned in the plan
            2. Extract ALL JSON content IDs (events, decisions, orders) mentioned
            3. For each C# file: use verify_file_exists_tool to check if it exists
            4. For each content ID: use lookup_content_id to check if it exists
            5. If files exist, use find_in_code to check if specific methods exist
            
            REPORT FORMAT:
            ## Already Implemented
            - List all files/IDs that ALREADY EXIST
            
            ## Still Needed
            - List all files/IDs that DON'T EXIST and need to be created
            
            ## Status
            - C# Status: COMPLETE / PARTIAL / NOT_STARTED
            - Content Status: COMPLETE / PARTIAL / NOT_STARTED
            
            Be thorough - check EVERY file and ID mentioned in the plan.
            """,
            expected_output="Verification report with lists of existing vs missing implementations",
            agent=get_systems_analyst(),
        )
        
        crew = Crew(
            agents=[get_systems_analyst()],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
            memory=True,
            cache=True,
            planning=True,
            planning_llm=GPT5_PLANNING,  # Use gpt-5 for planning
        )
        
        result = crew.kickoff()
        output = str(result)
        
        # Parse the output to determine statuses
        output_lower = output.lower()
        
        # Determine C# status
        if "c# status: complete" in output_lower or "csharp status: complete" in output_lower:
            state.csharp_status = ImplementationStatus.COMPLETE
        elif "c# status: partial" in output_lower or "csharp status: partial" in output_lower:
            state.csharp_status = ImplementationStatus.PARTIAL
        elif "still needed" in output_lower and (".cs" in output_lower or "method" in output_lower):
            state.csharp_status = ImplementationStatus.PARTIAL
        elif "c# status: not" in output_lower or "no c# work" in output_lower:
            state.csharp_status = ImplementationStatus.NOT_STARTED
        
        # Determine content status
        if "content status: complete" in output_lower:
            state.content_status = ImplementationStatus.COMPLETE
        elif "content status: partial" in output_lower:
            state.content_status = ImplementationStatus.PARTIAL
        elif "still needed" in output_lower and ("event" in output_lower or "json" in output_lower):
            state.content_status = ImplementationStatus.PARTIAL
        elif "content status: not" in output_lower or "no content work" in output_lower:
            state.content_status = ImplementationStatus.NOT_STARTED
        
        print(f"\n[VERIFY] Results:")
        print(f"   C# Status: {state.csharp_status.value}")
        print(f"   Content Status: {state.content_status.value}")
        
        state.current_step = "route_csharp"
        return state
    
    @router(verify_existing)
    def route_csharp(self, state: ImplementationState) -> str:
        """
        Router: Decide if C# implementation is needed.
        
        Uses condition functions for clean, testable routing logic.
        
        Returns:
        - "implement_csharp" if work is needed
        - "route_content" if C# is already complete
        """
        if csharp_complete(state):
            print(format_routing_decision(
                condition_name="csharp_complete",
                condition_result=True,
                chosen_path="route_content",
                reason=f"C# status: {state.csharp_status.value} - skipping implementation"
            ))
            state.skipped_steps.append("implement_csharp")
            return "route_content"
        
        print(format_routing_decision(
            condition_name="needs_csharp_work",
            condition_result=True,
            chosen_path="implement_csharp",
            reason=f"C# status: {state.csharp_status.value} - work needed"
        ))
        return "implement_csharp"
    
    @listen("implement_csharp")
    def implement_csharp(self, state: ImplementationState) -> ImplementationState:
        """
        Step 3 (conditional): Implement C# code.
        
        Only implements what's MISSING, not what already exists.
        """
        print("\n" + "-"*60)
        print("[CODE] Implementing C# (missing parts only)...")
        print("-"*60)
        
        task = Task(
            description=f"""
            Implement the C# code specified in this plan.
            
            CRITICAL: Only implement what's MISSING. Skip anything that already exists.
            
            PLAN:
            {state.plan_content[:6000]}
            
            ALREADY EXISTS (from verification):
            {state.existing_files}
            
            STEPS:
            1. Review the plan for C# requirements
            2. Use verify_file_exists_tool before creating any file
            3. If file exists, use find_in_code to check if method exists
            4. Only write code for what's MISSING
            5. Follow Enlisted patterns: Allman braces, _camelCase, XML docs
            6. Use write_source to create/modify files
            7. Use append_to_csproj for new .cs files
            8. Use update_localization for any new strings
            
            If EVERYTHING already exists, say "All C# already implemented" and stop.
            """,
            expected_output="C# implementation report: what was created, what was skipped",
            agent=get_csharp_implementer(),
        )
        
        crew = Crew(
            agents=[get_csharp_implementer()],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
            memory=True,
            cache=True,
            planning=True,
            planning_llm=GPT5_PLANNING,  # Use gpt-5 for planning
        )
        
        result = crew.kickoff()
        state.csharp_output = str(result)
        state.current_step = "route_content"
        return state
    
    @listen(or_("route_content", implement_csharp))
    def route_content_check(self, state: ImplementationState) -> ImplementationState:
        """Intermediate step to set up content routing."""
        state.current_step = "content_decision"
        return state
    
    @router(route_content_check)
    def route_content(self, state: ImplementationState) -> str:
        """
        Router: Decide if content implementation is needed.
        
        Uses condition functions for clean, testable routing logic.
        
        Returns:
        - "implement_content" if work is needed
        - "validate_all" if content is already complete
        """
        if content_complete(state):
            print(format_routing_decision(
                condition_name="content_complete",
                condition_result=True,
                chosen_path="validate_all",
                reason=f"Content status: {state.content_status.value} - skipping implementation"
            ))
            state.skipped_steps.append("implement_content")
            return "validate_all"
        
        print(format_routing_decision(
            condition_name="needs_content_work",
            condition_result=True,
            chosen_path="implement_content",
            reason=f"Content status: {state.content_status.value} - work needed"
        ))
        return "implement_content"
    
    @listen("implement_content")
    def implement_content(self, state: ImplementationState) -> ImplementationState:
        """
        Step 4 (conditional): Implement JSON content.
        
        Only creates what's MISSING, not what already exists.
        """
        print("\n" + "-"*60)
        print("[CONTENT] Implementing JSON content (missing parts only)...")
        print("-"*60)
        
        task = Task(
            description=f"""
            Create the JSON content specified in this plan.
            
            CRITICAL: Only create what's MISSING. Skip anything that already exists.
            
            PLAN:
            {state.plan_content[:6000]}
            
            ALREADY EXISTS (from verification):
            {state.existing_event_ids}
            
            WORKFLOW:
            1. Call list_event_ids FIRST to see all existing IDs
            2. Compare with what the plan requires
            3. Only create events/decisions with NEW IDs
            4. Use write_event for each new piece of content
            5. Use update_localization for display strings
            6. Call sync_strings after writing
            7. Call validate_content to verify
            
            VALID CATEGORIES: camp_life, combat, company_events, crisis, dialogue,
            discipline, downtime, equipment, escalation, faction, fatigue, formation,
            identity, leave, logistics, maintenance, morale, officer, order, progression,
            quartermaster, reaction, reputation, rest, retinue, supply, training
            
            VALID SEVERITIES: normal, attention, critical, positive, info
            
            If EVERYTHING already exists, say "All content already implemented" and stop.
            """,
            expected_output="Content implementation report: what was created, what was skipped",
            agent=get_content_author(),
        )
        
        crew = Crew(
            agents=[get_content_author()],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
            memory=True,
            cache=True,
            planning=True,
            planning_llm=GPT5_PLANNING,  # Use gpt-5 for planning
        )
        
        result = crew.kickoff()
        state.content_output = str(result)
        state.current_step = "validate"
        return state
    
    @listen(or_("validate_all", implement_content))
    def validate_all(self, state: ImplementationState) -> ImplementationState:
        """
        Step 5: Validate everything builds and passes checks.
        
        Always runs, even if implementation was skipped.
        """
        print("\n" + "-"*60)
        print("[QA] Validating implementation...")
        print("-"*60)
        
        task = Task(
            description=f"""
            Validate that the implementation is correct:
            
            SKIPPED STEPS: {state.skipped_steps}
            
            VALIDATION CHECKLIST:
            1. Run build tool to verify C# compilation
            2. Run validate_content to check JSON/XML
            3. Check for any obvious issues
            
            If steps were skipped (already implemented), just verify
            the existing implementation still works.
            
            Report: PASS or FAIL with details.
            """,
            expected_output="Validation report: build status, content validation, overall pass/fail",
            agent=get_qa_agent(),
        )
        
        crew = Crew(
            agents=[get_qa_agent()],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
            memory=True,
            cache=True,
            planning=True,
            planning_llm=GPT5_PLANNING,  # Use gpt-5 for planning
        )
        
        result = crew.kickoff()
        state.validation_output = str(result)
        state.current_step = "docs"
        return state
    
    @listen(validate_all)
    def update_docs(self, state: ImplementationState) -> ImplementationState:
        """
        Step 6: Update documentation and database.
        
        Always runs to ensure docs are up to date.
        """
        print("\n" + "-"*60)
        print("[DOCS] Updating documentation...")
        print("-"*60)
        
        task = Task(
            description=f"""
            Update documentation to reflect the implementation:
            
            PLAN FILE: {state.plan_path}
            
            TASKS:
            1. Read the plan file
            2. Update status to "Implemented" or "Partial"
            3. Add Implementation Summary section listing:
               - What was implemented
               - What was already done (skipped)
               - Files created/modified
            4. Disperse key details to permanent feature docs in docs/Features/
            5. Call sync_content_from_files to update content database
            6. Call record_implementation with summary
            
            SKIPPED (already done): {state.skipped_steps}
            
            The plan file should clearly show what's complete vs remaining.
            """,
            expected_output="Documentation update report",
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
        state.docs_output = str(result)
        state.current_step = "report"
        return state
    
    @listen(update_docs)
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
