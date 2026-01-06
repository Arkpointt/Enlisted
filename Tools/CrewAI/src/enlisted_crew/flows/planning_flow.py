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

from crewai import Agent, Crew, Process, Task, LLM
from crewai.flow.flow import Flow, listen, router, start, or_
from crewai.tasks.conditional_task import ConditionalTask
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

# LOW reasoning - planning from structured prompts
GPT5_PLANNING = LLM(
    model=_get_env("ENLISTED_LLM_PLANNING", "gpt-5.2"),
    max_completion_tokens=4000,
    reasoning_effort="low",
)


# === Agent Factory ===

_agent_cache = {}


def get_systems_analyst() -> Agent:
    """Research existing systems and code."""
    if "systems_analyst" not in _agent_cache:
        _agent_cache["systems_analyst"] = Agent(
            role="Systems Analyst",
            goal="Research existing Enlisted systems to understand integration points",
            backstory="""You research the Enlisted codebase to understand how systems work.
You find relevant code, documentation, and dependencies. Your research forms
the foundation for feature design.""",
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
            allow_delegation=True,  # Coordinator can delegate to specialists
        )
    return _agent_cache["systems_analyst"]


def get_architecture_advisor() -> Agent:
    """Suggest best practices and improvements."""
    if "architecture_advisor" not in _agent_cache:
        _agent_cache["architecture_advisor"] = Agent(
            role="Architecture Advisor",
            goal="Suggest best practices and architectural improvements",
            backstory="""You advise on architecture and best practices for Enlisted features.
You identify potential issues, suggest improvements, and ensure designs
follow established patterns.""",
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
            allow_delegation=True,  # Advisor can delegate research tasks
        )
    return _agent_cache["architecture_advisor"]


def get_feature_architect() -> Agent:
    """Design the technical specification."""
    if "feature_architect" not in _agent_cache:
        _agent_cache["feature_architect"] = Agent(
            role="Feature Architect",
            goal="Design complete technical specifications for new features",
            backstory="""You design features with detailed technical specs. You specify
exact file paths, method signatures, event IDs, and integration points.
Your specs are detailed enough for direct implementation.""",
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
            max_iter=15,
            max_retry_limit=3,
            reasoning=True,  # Think before designing
            max_reasoning_attempts=3,  # Limit reasoning iterations
            allow_delegation=True,  # Orchestrator can delegate implementation details
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
    
    State Persistence: Enabled via persist=True. If a run fails, you can resume
    from the last successful step by re-running with the same inputs.
    
    Steps:
    1. research_systems - Analyze existing code and docs
    2. suggest_improvements - Get architectural advice
    3. design_feature - Create technical specification
    4. write_plan - Save planning document
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
        
        state.current_step = "research"
        return state
    
    @listen(receive_request)
    def research_systems(self, state: PlanningState) -> PlanningState:
        """Step 1: Research existing systems."""
        print("\n" + "-"*60)
        print("[RESEARCH] Analyzing existing systems...")
        print("-"*60)
        
        task = Task(
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
        
        crew = Crew(
            agents=[get_systems_analyst()],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
            memory=True,
            cache=True,  # Cache tool results
            planning=True,
            planning_llm=GPT5_PLANNING,  # Use gpt-5 for planning
        )
        
        result = crew.kickoff()
        state.research_output = str(result)
        state.current_step = "suggest"
        return state
    
    @listen(research_systems)
    def suggest_improvements(self, state: PlanningState) -> PlanningState:
        """Step 2: Get architectural advice."""
        print("\n" + "-"*60)
        print("[ADVISE] Getting architectural recommendations...")
        print("-"*60)
        
        task = Task(
            description=f"""
            Suggest architectural improvements for this feature:
            
            FEATURE: {state.feature_name}
            DESCRIPTION: {state.description}
            
            RESEARCH FINDINGS:
            {state.research_output[:4000]}
            
            ADVISOR TASKS:
            1. Review the research findings
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
        )
        
        crew = Crew(
            agents=[get_architecture_advisor()],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
            memory=True,
            cache=True,
            planning=True,
            planning_llm=GPT5_PLANNING,  # Use gpt-5 for planning
        )
        
        result = crew.kickoff()
        state.suggestions_output = str(result)
        state.current_step = "design"
        return state
    
    @listen(suggest_improvements)
    def design_feature(self, state: PlanningState) -> PlanningState:
        """Step 3: Create technical specification."""
        print("\n" + "-"*60)
        print("[DESIGN] Creating technical specification...")
        print("-"*60)
        
        task = Task(
            description=f"""
            Design the technical specification for this feature:
            
            FEATURE: {state.feature_name}
            DESCRIPTION: {state.description}
            
            RESEARCH:
            {state.research_output[:3000]}
            
            RECOMMENDATIONS:
            {state.suggestions_output[:2000]}
            
            DESIGN TASKS:
            1. Define exact file paths for new C# code
            2. Specify method signatures and class structure
            3. Define JSON content IDs (events, decisions, orders)
            4. Specify integration with existing systems
            5. Include tier-variant content if applicable
            
            OUTPUT: Complete technical spec with:
            - File paths (exact, verifiable)
            - Method signatures
            - Content IDs (unique, following naming conventions)
            - Integration points
            - Implementation order
            """,
            expected_output="Technical specification document",
            agent=get_feature_architect(),
        )
        
        crew = Crew(
            agents=[get_feature_architect()],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
            memory=True,
            cache=True,
            planning=True,
            planning_llm=GPT5_PLANNING,  # Use gpt-5 for planning
        )
        
        result = crew.kickoff()
        state.design_output = str(result)
        state.current_step = "write"
        return state
    
    @listen(design_feature)
    def write_plan(self, state: PlanningState) -> PlanningState:
        """Step 4: Write planning document."""
        print("\n" + "-"*60)
        print("[WRITE] Saving planning document...")
        print("-"*60)
        
        plan_path = f"docs/CrewAI_Plans/{state.feature_name}.md"
        
        task = Task(
            description=f"""
            Write the planning document for this feature:
            
            FEATURE: {state.feature_name}
            OUTPUT PATH: {plan_path}
            
            DESIGN SPEC:
            {state.design_output[:5000]}
            
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
        state.plan_path = plan_path
        state.current_step = "validate"
        return state
    
    @listen(write_plan)
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
