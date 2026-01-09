"""
Implementation Flow - Single-agent Crews per Flow step.

Refactored from multi-agent Crew to separate Flow steps following CrewAI best practices.
Each step either creates a single-agent Crew or uses pure Python.

Key Pattern: Access state via self.state, not as parameter.
Based on: https://docs.crewai.com/en/guides/flows/mastering-flow-state
"""

import os
from pathlib import Path

from crewai import Agent, Crew, Process, Task, LLM
from crewai.flow.flow import Flow, listen, router, start
from crewai.tasks.task_output import TaskOutput

from .state_models import ImplementationState, ImplementationStatus
from ..memory_config import get_memory_config
from ..monitoring import EnlistedExecutionMonitor
from ..tools import (
    parse_plan, lookup_content_ids_batch, verify_file_exists_tool,
    read_source, write_source, append_to_csproj,
    write_event, update_localization, lookup_content_id, add_content_item,
    get_valid_categories, get_style_guide, build, validate_content,
    sync_content_from_files, record_implementation
)
from ..rag.codebase_rag_tool import search_codebase
from ..tools.docs_tools import search_docs_semantic
from ..prompts import ARCHITECTURE_PATTERNS, IMPLEMENTATION_WORKFLOW, CODE_STYLE_RULES


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


# LLM Configurations
GPT5_ARCHITECT = LLM(model="gpt-5.2", max_completion_tokens=16000, reasoning_effort="high")
GPT5_IMPLEMENTER = LLM(model="gpt-5.2", max_completion_tokens=12000, reasoning_effort="low")
GPT5_FAST = LLM(model="gpt-5.2", max_completion_tokens=4000, reasoning_effort="none")


class ImplementationFlow(Flow[ImplementationState]):
    """
    Single-agent Flow implementation following CrewAI best practices.
    
    Pattern: Each step accesses state via self.state (not parameter).
    Routers return string route names, listeners use self.state.
    
    Refactored from multi-agent Crew (5 agents → 8 Flow steps).
    """
    
    initial_state = ImplementationState
    
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self._monitor = EnlistedExecutionMonitor()
        print("[MONITORING] Execution monitoring enabled")
    
    @start()
    def load_plan(self):
        """Step 1: Load plan file (pure Python)."""
        print("\n" + "="*60)
        print("IMPLEMENTATION FLOW STARTED")
        print("="*60)
        
        plan_path = self.state.plan_path if hasattr(self.state, 'plan_path') else ""
        
        if not plan_path:
            print("[ERROR] No plan_path provided!")
            self.state.success = False
            self.state.final_report = "Error: No plan_path provided"
            return
        
        print(f"\nLoading plan: {plan_path}")
        
        project_root = get_project_root()
        full_path = project_root / plan_path
        
        if not full_path.exists():
            print(f"[ERROR] Plan not found: {full_path}")
            self.state.success = False
            self.state.final_report = f"Error: Plan not found: {plan_path}"
            return
        
        plan_content = full_path.read_text(encoding="utf-8")
        print(f"[OK] Plan loaded ({len(plan_content)} chars)")
        
        import hashlib
        plan_hash = hashlib.md5(plan_content.encode('utf-8')).hexdigest()[:12]
        print(f"[OK] Plan hash: {plan_hash}")
        
        self.state.plan_content = plan_content
        self.state.plan_hash = plan_hash
    
    @listen(load_plan)
    def verify_existing(self):
        """Step 2: Verify existing implementation (single-agent Crew)."""
        print("\n" + "-"*60)
        print("[STEP 2/8] Verifying existing implementation...")
        print("-"*60)
        
        agent = Agent(
            role="Systems Verifier",
            goal="Check what's already implemented using database and file tools",
            backstory="You verify implementation status efficiently.",
            llm=GPT5_ARCHITECT,
            tools=[parse_plan, lookup_content_ids_batch, verify_file_exists_tool, search_codebase, search_docs_semantic],
            max_iter=5,
            allow_delegation=False,
            verbose=True,
        )
        
        task = Task(
            description=f"""{IMPLEMENTATION_WORKFLOW}

=== YOUR TASK ===
Verify what's already implemented for this plan.

**Plan Content (first 4000 chars):**
{self.state.plan_content[:4000]}

**Expected Output:**
## Already Implemented
- List files/IDs that EXIST

## Still Needed  
- List files/IDs that DON'T EXIST

## Status
- C# Status: COMPLETE / PARTIAL / NOT_STARTED
- Content Status: COMPLETE / PARTIAL / NOT_STARTED
            """,
            expected_output="Verification report",
            agent=agent,
        )
        
        crew = Crew(
            agents=[agent],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
            **get_memory_config(),  # Uses simplified memory (no custom storage)
        )
        
        result = crew.kickoff()
        result_str = str(result)
        
        # Parse status
        output_lower = result_str.lower()
        if "c# status: complete" in output_lower:
            self.state.csharp_status = ImplementationStatus.COMPLETE
        elif "c# status: partial" in output_lower:
            self.state.csharp_status = ImplementationStatus.PARTIAL
        else:
            self.state.csharp_status = ImplementationStatus.NOT_STARTED
        
        if "content status: complete" in output_lower:
            self.state.content_status = ImplementationStatus.COMPLETE
        elif "content status: partial" in output_lower:
            self.state.content_status = ImplementationStatus.PARTIAL
        else:
            self.state.content_status = ImplementationStatus.NOT_STARTED
        
        print(f"[VERIFICATION] C# Status: {self.state.csharp_status.value}")
        print(f"[VERIFICATION] Content Status: {self.state.content_status.value}")
    
    @listen(verify_existing)
    def implement_csharp(self):
        """Step 3: Implement C# code (single-agent Crew, conditional)."""
        # Check if already complete
        if self.state.csharp_status == ImplementationStatus.COMPLETE:
            print("[SKIP] C# already complete")
            self.state.skipped_steps.append("implement_csharp")
            return
        
        print("\n" + "-"*60)
        print("[STEP 3/8] Implementing C# code...")
        print("-"*60)
        
        agent = Agent(
            role="C# Implementer",
            goal="Write C# code following Enlisted patterns",
            backstory="You implement production C# code efficiently.",
            llm=GPT5_IMPLEMENTER,
            tools=[search_codebase, search_docs_semantic, read_source, write_source, append_to_csproj],
            max_iter=8,
            allow_delegation=False,
            verbose=True,
        )
        
        task = Task(
            description=f"""{ARCHITECTURE_PATTERNS}
{CODE_STYLE_RULES}

=== YOUR TASK ===
Implement C# code from plan. Only implement what's MISSING.

**Plan Content (first 6000 chars):**
{self.state.plan_content[:6000]}
            """,
            expected_output="C# implementation report",
            agent=agent,
        )
        
        crew = Crew(
            agents=[agent],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
            **get_memory_config(),  # Uses simplified memory (no custom storage)
        )
        
        result = crew.kickoff()
        self.state.csharp_output = str(result)
    
    @listen(implement_csharp)
    def implement_content(self):
        """Step 4: Implement content (single-agent Crew, conditional)."""
        # Check if already complete
        if self.state.content_status == ImplementationStatus.COMPLETE:
            print("[SKIP] Content already complete")
            self.state.skipped_steps.append("implement_content")
            return
        
        print("\n" + "-"*60)
        print("[STEP 4/8] Implementing content...")
        print("-"*60)
        
        agent = Agent(
            role="Content Author",
            goal="Create JSON content following Enlisted schemas",
            backstory="You create JSON events, decisions, orders efficiently.",
            llm=GPT5_FAST,
            tools=[write_event, update_localization, lookup_content_id, add_content_item, get_valid_categories, get_style_guide, search_docs_semantic],
            max_iter=8,
            allow_delegation=False,
            verbose=True,
        )
        
        task = Task(
            description=f"""{ARCHITECTURE_PATTERNS}

=== YOUR TASK ===
Create JSON content from plan. Only create what's MISSING.

**Plan Content (first 6000 chars):**
{self.state.plan_content[:6000]}
            """,
            expected_output="Content implementation report",
            agent=agent,
        )
        
        crew = Crew(
            agents=[agent],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
            **get_memory_config(),  # Uses simplified memory (no custom storage)
        )
        
        result = crew.kickoff()
        self.state.content_output = str(result)
    
    @listen(implement_content)
    def validate_build(self):
        """Step 5: Validate build (pure Python)."""
        print("\n" + "-"*60)
        print("[STEP 5/8] Running build validation...")
        print("-"*60)
        
        build_result = build.run()
        self.state.validation_output = build_result
        
        if "succeeded" in build_result.lower() or "0 error" in build_result.lower():
            print("[BUILD] ✓ Build succeeded")
        else:
            print("[BUILD] ✗ Build failed")
    
    @listen(validate_build)
    def validate_content_check(self):
        """Step 6: Validate content (pure Python)."""
        print("\n" + "-"*60)
        print("[STEP 6/8] Running content validation...")
        print("-"*60)
        
        content_result = validate_content.run()
        self.state.validation_output += "\n" + content_result
        print("[CONTENT] Validation complete")
    
    @listen(validate_content_check)
    def sync_docs(self):
        """Step 7: Sync documentation (pure Python)."""
        print("\n" + "-"*60)
        print("[STEP 7/8] Syncing documentation...")
        print("-"*60)
        
        sync_result = sync_content_from_files.run()
        record_result = record_implementation.run(
            plan_path=self.state.plan_path,
            status="complete"
        )
        
        self.state.docs_output = f"{sync_result}\n{record_result}"
        print("[DOCS] Synchronization complete")
    
    @listen(sync_docs)
    def generate_report(self):
        """Step 8: Generate final report (pure Python)."""
        print("\n" + "-"*60)
        print("[REPORT] Generating final report...")
        print("-"*60)
        
        report_parts = [
            "=" * 60,
            "IMPLEMENTATION REPORT",
            "=" * 60,
            "",
            f"Plan: {self.state.plan_path}",
            "",
            "## Status Summary",
            f"- C# Status: {self.state.csharp_status.value}",
            f"- Content Status: {self.state.content_status.value}",
            f"- Skipped Steps: {', '.join(self.state.skipped_steps) or 'None'}",
            "",
        ]
        
        if self.state.csharp_output:
            report_parts.extend([
                "## C# Implementation",
                self.state.csharp_output[:2000],
                "",
            ])
        
        if self.state.content_output:
            report_parts.extend([
                "## Content Implementation",
                self.state.content_output[:2000],
                "",
            ])
        
        report_parts.extend([
            "## Validation",
            self.state.validation_output[:1000],
            "",
            "## Documentation",
            self.state.docs_output[:1000],
            "",
            "=" * 60,
        ])
        
        self.state.final_report = "\n".join(report_parts)
        self.state.success = True
        
        print("\n" + "="*60)
        print("IMPLEMENTATION FLOW COMPLETE")
        print("="*60)
