"""
Enlisted CrewAI Crew Orchestration

Main crew class that coordinates agents for Enlisted mod development workflows.

Model Strategy (OpenAI GPT-5 family):
- GPT-5.2: Best for complex architecture, code generation, agentic tasks
- GPT-5 mini: Fast and cost-efficient for well-defined tasks, memory ops
- GPT-5 nano: Ultra-fast for validation, schema checks, style review

All models support configurable reasoning effort and have no thinking/tool_choice conflicts.
Context: 200K tokens across the board.
"""

import os
from pathlib import Path
from crewai import Agent, Crew, Process, Task, LLM
from crewai.project import CrewBase, agent, crew, task
from crewai.knowledge.source.text_file_knowledge_source import TextFileKnowledgeSource

from .tools import (
    # Validation
    validate_content,
    sync_strings,
    build,
    analyze_issues,
    # Style Review
    review_prose,
    review_tooltip,
    suggest_edits,
    get_style_guide,
    # Event Schema
    check_event_format,
    draft_event,
    read_event,
    list_events,
    # Code Review
    review_code,
    check_game_patterns,
    check_compatibility,
    review_source_file,
    # Documentation
    read_doc_tool,
    list_docs_tool,
    find_in_docs,
    # Source Code
    read_source,
    find_in_code,
    read_source_section,
    list_feature_files_tool,
    # Debug & Native
    read_debug_logs_tool,
    search_debug_logs_tool,
    read_native_crash_logs_tool,
    find_in_native_api,
    # Context Loaders
    get_writing_guide,
    get_architecture,
    get_dev_reference,
    get_game_systems,
    # Planning
    save_plan,
    load_plan,
    # Verification
    verify_file_exists_tool,
    list_event_ids,
    # File Writing
    write_source,
    write_event,
    write_doc,
    update_localization,
    append_to_csproj,
)

# === LLM Configurations ===
# Philosophy: Spend tokens on ANALYSIS and VALIDATION, save on EXECUTION.
# Bugs from cheap generation cost more to fix than reasoning tokens.
#
# Model Strategy: OpenAI GPT-5 family
# - GPT-5.2: Flagship model, best for complex architecture and agentic tasks
# - GPT-5 mini: Cost-efficient, fast, optimized for well-defined tasks
# - GPT-5 nano: Ultra-fast for high-volume validation and generation
#
# All models: 200K context window, configurable reasoning effort
# Pricing (per 1M tokens):
# - GPT-5.2: ~$2.50 in / ~$10 out (estimated)
# - GPT-5 mini: $0.25 in / $2 out
# - GPT-5 nano: $0.15 in / $0.60 out (estimated)
#
# Tier 1: Deep Reasoning (architecture, design, system integration)
# Tier 2: Standard Reasoning (code analysis, bug investigation)
# Tier 3: Execution (implementation from specs)
# Tier 4: Fast Validation (schema checks, style checks)

import os as _os
def _get_env(name: str, default: str) -> str:
    return _os.environ.get(name, default)

# TIER 1: GPT-5.2 with high reasoning - architectural decisions
# Use for: Feature design, system integration, multi-file analysis
GPT5_ARCHITECT = LLM(
    model=_get_env("ENLISTED_LLM_ARCHITECT", "gpt-5.2"),
    max_tokens=16000,
    temperature=0.7,
)

# TIER 2: GPT-5.2 for code analysis and review
# Use for: Code review, bug investigation, understanding existing systems
GPT5_ANALYST = LLM(
    model=_get_env("ENLISTED_LLM_ANALYST", "gpt-5.2"),
    max_tokens=12000,
    temperature=0.5,
)

# TIER 3: GPT-5 mini for implementation - execution from specs
# Use for: Writing code from specs, implementing defined changes
GPT5_IMPLEMENTER = LLM(
    model=_get_env("ENLISTED_LLM_IMPLEMENTER", "gpt-5-mini"),
    max_tokens=8000,
    temperature=0.3,
)

# TIER 4: GPT-5 nano - high-volume validation and content generation
# Use for: Schema validation, style checks, content from templates
GPT5_FAST = LLM(
    model=_get_env("ENLISTED_LLM_FAST", "gpt-5-nano"),
    max_tokens=4000,
    temperature=0.2,
)

# TIER 2.5: GPT-5 mini for QA - final safety net
# Use for: Final validation before commit, catching what others missed
GPT5_QA = LLM(
    model=_get_env("ENLISTED_LLM_QA", "gpt-5-mini"),
    max_tokens=6000,
    temperature=0.3,
)

# === Embedder Configuration ===
# Use text-embedding-3-small for memory/knowledge embeddings.
# Default ada-002 has strict 8192 token limit; 3-small handles longer inputs better.
EMBEDDER_CONFIG = {
    "provider": "openai",
    "config": {
        "model_name": "text-embedding-3-small",
    }
}


@CrewBase
class EnlistedCrew:
    """
    Enlisted CrewAI Crew
    
    Orchestrates agents for content validation, creation, and review workflows.
    """
    
    agents_config = "config/agents.yaml"
    tasks_config = "config/tasks.yaml"
    
    def __init__(self):
        """Initialize the crew with project root detection and knowledge sources."""
        self.project_root = self._find_project_root()
        os.environ["ENLISTED_PROJECT_ROOT"] = str(self.project_root)
        self._init_knowledge_sources()
    
    def _init_knowledge_sources(self):
        """
        Initialize knowledge sources from the knowledge/ folder.
        
        These provide dynamic context that changes with the codebase,
        unlike static backstories which should remain architectural.
        
        Note: CrewAI expects relative paths from the project's knowledge/ directory.
        
        Chunk settings (for embedding model compatibility):
        - chunk_size=2000 chars (~500 tokens) - fits within 8192 token embedding limit
        - chunk_overlap=200 - maintains context between chunks
        """
        # Shared chunk settings for embedding model compatibility
        chunk_params = {"chunk_size": 2000, "chunk_overlap": 200}
        
        # Systems knowledge - for systems_analyst, feature_architect
        self.systems_knowledge = TextFileKnowledgeSource(
            file_paths=["core-systems.md"],
            **chunk_params,
        )
        
        # Code knowledge - for code_analyst, csharp_implementer
        self.code_knowledge = TextFileKnowledgeSource(
            file_paths=[
                "core-systems.md",
                "error-codes.md",
            ],
            **chunk_params,
        )
        
        # Content knowledge - for content_author, content_analyst
        self.content_knowledge = TextFileKnowledgeSource(
            file_paths=[
                "event-format.md",
                "balance-values.md",
                "game-design-principles.md",  # Tier-aware content guidance
            ],
            **chunk_params,
        )
        
        # Balance knowledge - for balance_analyst
        self.balance_knowledge = TextFileKnowledgeSource(
            file_paths=[
                "balance-values.md",
                "core-systems.md",
                "game-design-principles.md",  # Player engagement checks
            ],
            **chunk_params,
        )
        
        # Planning knowledge - for documentation_maintainer (planning tasks)
        self.planning_knowledge = TextFileKnowledgeSource(
            file_paths=[
                "core-systems.md",
                "content-files.md",  # JSON content inventory for verification
            ],
            **chunk_params,
        )
        
        # Design knowledge - for feature_architect (player experience)
        self.design_knowledge = TextFileKnowledgeSource(
            file_paths=[
                "core-systems.md",
                "game-design-principles.md",  # Tier-aware design, Story Test
            ],
            **chunk_params,
        )
        
        # UI knowledge - for feature_architect, csharp_implementer (UI work)
        self.ui_knowledge = TextFileKnowledgeSource(
            file_paths=[
                "ui-systems.md",
                "core-systems.md",
            ],
            **chunk_params,
        )
    
    def _find_project_root(self) -> Path:
        """Find the Enlisted project root directory."""
        env_root = os.environ.get("ENLISTED_PROJECT_ROOT")
        if env_root:
            return Path(env_root)
        
        current = Path(__file__).resolve()
        for parent in current.parents:
            if (parent / "Enlisted.csproj").exists():
                return parent
        
        return Path(r"C:\Dev\Enlisted\Enlisted")
    
    # === Agents ===
    
    @agent
    def systems_analyst(self) -> Agent:
        """Systems integration analyst - uses GPT-5.2 for complex system analysis."""
        return Agent(
            config=self.agents_config["systems_analyst"],
            llm=GPT5_ARCHITECT,  # TIER 1: System integration requires deep reasoning
            tools=[
                get_game_systems,    # Context first
                find_in_docs,        # Find docs
                read_doc_tool,       # Read docs
                find_in_code,        # Find code patterns
            ],
            knowledge_sources=[self.systems_knowledge],
            respect_context_window=True,
            max_retry_limit=2,
            allow_delegation=True,  # Coordination role
        )
    
    @agent
    def code_analyst(self) -> Agent:
        """C# code analyst - uses GPT-5.2 for code analysis and bug investigation."""
        return Agent(
            config=self.agents_config["code_analyst"],
            llm=GPT5_ANALYST,  # TIER 2: Analysis needs reasoning to catch bugs
            tools=[
                get_dev_reference,       # CALL FIRST - loads dev guide, APIs, common issues
                verify_file_exists_tool, # Verify file paths in planning docs
                list_event_ids,          # Verify event IDs in planning docs
                build,
                validate_content,
                review_code,
                check_game_patterns,
                check_compatibility,
                review_source_file,
                read_debug_logs_tool,
                search_debug_logs_tool,
                read_native_crash_logs_tool,  # For hard game crashes
                find_in_native_api,
                find_in_code,        # Search first, then read snippets
                read_source_section, # Prefer snippets over full files
                read_source,         # Full file only when needed
            ],
            knowledge_sources=[self.code_knowledge],
            respect_context_window=True,
            max_retry_limit=1,
        )
    
    @agent
    def content_analyst(self) -> Agent:
        """Content schema analyst - uses GPT-5 nano for fast validation."""
        return Agent(
            config=self.agents_config["content_analyst"],
            llm=GPT5_FAST,
            tools=[
                check_event_format,
                read_event,
                list_events,
                validate_content,
            ],
            knowledge_sources=[self.content_knowledge],
            respect_context_window=True,
        )
    
    @agent
    def feature_architect(self) -> Agent:
        """Feature architect - uses GPT-5.2 for complex multi-file design."""
        return Agent(
            config=self.agents_config["feature_architect"],
            llm=GPT5_ARCHITECT,  # TIER 1: Architecture mistakes are expensive
            tools=[
                get_architecture,        # Context first
                verify_file_exists_tool, # Verify file paths in specs
                list_event_ids,          # Verify event IDs in specs
                find_in_docs,            # Find docs
                read_doc_tool,           # Read docs
                find_in_code,            # Find code
                read_source_section,     # Read sections
                validate_content,        # Check schemas
            ],
            knowledge_sources=[self.design_knowledge],  # Includes game-design-principles.md
            respect_context_window=True,
            max_retry_limit=2,
            allow_delegation=True,  # Can delegate
        )
    
    @agent
    def csharp_implementer(self) -> Agent:
        """C# implementer - uses GPT-5 mini for code generation from specs."""
        return Agent(
            config=self.agents_config["csharp_implementer"],
            llm=GPT5_IMPLEMENTER,  # TIER 3: Executing from clear specs, QA catches issues
            tools=[
                verify_file_exists_tool, # Verify file paths before proposing changes
                build,
                validate_content,
                review_code,
                check_game_patterns,
                check_compatibility,
                review_source_file,
                find_in_native_api,
                find_in_code,        # Search to find relevant code
                read_source_section, # Read targeted snippets
                read_source,         # Full file when implementing
                # File writing tools
                write_source,        # Write C# files
                append_to_csproj,    # Add new files to project
                update_localization, # Add TextObject strings to XML
            ],
            knowledge_sources=[self.ui_knowledge],
            respect_context_window=True,
        )
    
    @agent
    def content_author(self) -> Agent:
        """Content author - uses GPT-5 nano for fast content generation."""
        return Agent(
            config=self.agents_config["content_author"],
            llm=GPT5_FAST,
            tools=[
                get_writing_guide,   # CALL FIRST - loads style guide, schemas, reminders
                list_event_ids,      # Check existing IDs to avoid conflicts
                draft_event,
                review_prose,
                review_tooltip,
                suggest_edits,
                read_event,
                check_event_format,
                get_style_guide,
                read_doc_tool,
                # File writing tools
                write_event,         # Write JSON event files
                update_localization, # Add strings to XML
                # Validation tools (complete the workflow)
                sync_strings,        # Batch-sync JSON string IDs to XML
                validate_content,    # Full content validation
            ],
            knowledge_sources=[self.content_knowledge],
            respect_context_window=True,
        )
    
    @agent
    def qa_agent(self) -> Agent:
        """QA agent - uses GPT-5 mini as final safety net."""
        return Agent(
            config=self.agents_config["qa_agent"],
            llm=GPT5_QA,  # TIER 2.5: QA is last defense - NEVER skimp here
            tools=[
                validate_content,
                sync_strings,
                build,
                analyze_issues,
                review_code,
            ],
            respect_context_window=True,
        )
    
    @agent
    def balance_analyst(self) -> Agent:
        """Balance analyst - uses GPT-5 nano for content review."""
        return Agent(
            config=self.agents_config["balance_analyst"],
            llm=GPT5_FAST,
            tools=[
                get_game_systems,   # CALL FIRST - needs game balance knowledge
                check_event_format,
                read_event,
                list_events,
                read_doc_tool,
            ],
            knowledge_sources=[self.balance_knowledge],
            respect_context_window=True,
        )
    
    @agent
    def documentation_maintainer(self) -> Agent:
        """Documentation maintainer - uses GPT-5.2 to ensure docs stay in sync."""
        return Agent(
            config=self.agents_config["documentation_maintainer"],
            llm=GPT5_ANALYST,  # TIER 2: Doc sync requires reasoning about what changed
            tools=[
                save_plan,               # Write planning docs
                load_plan,               # Read planning docs (for fixes)
                verify_file_exists_tool, # Verify file paths before including
                list_event_ids,          # Verify event IDs before including
                read_doc_tool,
                list_docs_tool,
                find_in_docs,
                find_in_code,        # Search for code changes
                read_source_section, # Read relevant snippets
                read_source,         # Full file when needed
                list_feature_files_tool,
                get_game_systems,
                # File writing for doc updates
                write_doc,           # Write/update markdown docs
                update_localization, # Update XML localization strings
            ],
            knowledge_sources=[self.planning_knowledge],
            respect_context_window=True,
        )
    
    @agent
    def architecture_advisor(self) -> Agent:
        """Architecture advisor - uses GPT-5.2 for deep analysis and improvement suggestions."""
        return Agent(
            config=self.agents_config["architecture_advisor"],
            llm=GPT5_ARCHITECT,  # TIER 1: Architecture analysis needs deep reasoning
            tools=[
                get_game_systems,        # Understand game systems first
                get_architecture,        # Understand architecture patterns
                verify_file_exists_tool, # Verify paths when suggesting changes
                find_in_docs,            # Find documentation
                read_doc_tool,           # Read docs
                find_in_code,            # Find code patterns
                read_source_section,     # Read targeted snippets
                read_source,             # Full files when needed
                list_feature_files_tool, # Understand feature structure
                find_in_native_api,      # Check Bannerlord API constraints
            ],
            knowledge_sources=[self.systems_knowledge],
            respect_context_window=True,
            max_retry_limit=2,
            allow_delegation=True,
        )
    
    # === Tasks ===
    
    # Research tasks
    @task
    def analyze_systems_task(self) -> Task:
        """Analyze system integration for a feature."""
        return Task(
            config=self.tasks_config["analyze_systems"],
        )
    
    @task
    def analyze_codebase_task(self) -> Task:
        """Analyze codebase for implementation patterns."""
        return Task(
            config=self.tasks_config["analyze_codebase"],
        )
    
    @task
    def analyze_content_structure_task(self) -> Task:
        """Analyze content structure and schemas."""
        return Task(
            config=self.tasks_config["analyze_content_structure"],
        )
    
    @task
    def investigate_bug_task(self) -> Task:
        """Investigate a bug or crash."""
        return Task(
            config=self.tasks_config["investigate_bug"],
        )
    
    @task
    def analyze_bug_systems_task(self) -> Task:
        """Analyze systems related to a bug."""
        return Task(
            config=self.tasks_config["analyze_bug_systems"],
        )
    
    @task
    def propose_bug_fix_task(self) -> Task:
        """Propose a minimal fix for a bug."""
        return Task(
            config=self.tasks_config["propose_bug_fix"],
        )
    
    @task
    def validate_bug_fix_task(self) -> Task:
        """Validate a proposed bug fix."""
        return Task(
            config=self.tasks_config["validate_bug_fix"],
        )
    
    # Advisory tasks
    @task
    def suggest_improvements_task(self) -> Task:
        """Suggest improvements based on industry best practices."""
        return Task(
            config=self.tasks_config["suggest_improvements"],
        )
    
    @task
    def review_architecture_task(self) -> Task:
        """Review system architecture and suggest refactoring."""
        return Task(
            config=self.tasks_config["review_architecture"],
        )
    
    # Design tasks
    @task
    def design_feature_task(self) -> Task:
        """Design a new feature architecture."""
        return Task(
            config=self.tasks_config["design_feature"],
        )
    
    @task
    def review_feature_design_task(self) -> Task:
        """Review a feature design for balance and patterns."""
        return Task(
            config=self.tasks_config["review_feature_design"],
        )
    
    # Implementation tasks
    @task
    def implement_csharp_task(self) -> Task:
        """Implement C# code changes."""
        return Task(
            config=self.tasks_config["implement_csharp"],
        )
    
    @task
    def implement_content_task(self) -> Task:
        """Implement JSON content changes."""
        return Task(
            config=self.tasks_config["implement_content"],
        )
    
    # Validation tasks
    @task
    def validate_all_task(self) -> Task:
        """Run comprehensive validation."""
        return Task(
            config=self.tasks_config["validate_all"],
        )
    
    @task
    def validate_implementation_task(self) -> Task:
        """Validate C# implementation."""
        return Task(
            config=self.tasks_config["validate_implementation"],
        )
    
    @task
    def validate_content_file_task(self) -> Task:
        """Validate a specific content file."""
        return Task(
            config=self.tasks_config["validate_content_file"],
        )
    
    # Review tasks
    @task
    def review_code_task(self) -> Task:
        """Review C# code for patterns and style."""
        return Task(
            config=self.tasks_config["review_code"],
        )
    
    @task
    def review_balance_task(self) -> Task:
        """Review content for game balance."""
        return Task(
            config=self.tasks_config["review_balance"],
        )
    
    # Workflow tasks
    @task
    def full_feature_workflow_task(self) -> Task:
        """Complete feature development workflow."""
        return Task(
            config=self.tasks_config["full_feature_workflow"],
        )
    
    @task
    def full_content_workflow_task(self) -> Task:
        """Complete content creation workflow."""
        return Task(
            config=self.tasks_config["full_content_workflow"],
        )
    
    # Documentation tasks
    @task
    def sync_documentation_task(self) -> Task:
        """Synchronize documentation with code changes."""
        return Task(
            config=self.tasks_config["sync_documentation"],
        )
    
    @task
    def audit_documentation_task(self) -> Task:
        """Audit documentation for accuracy against codebase."""
        return Task(
            config=self.tasks_config["audit_documentation"],
        )
    
    @task
    def create_planning_doc_task(self) -> Task:
        """Create a planning document (NOT implementation - planning only)."""
        return Task(
            config=self.tasks_config["create_planning_doc"],
        )
    
    @task
    def validate_planning_doc_task(self) -> Task:
        """Validate a planning document for accuracy (file paths, event IDs, etc.)."""
        return Task(
            config=self.tasks_config["validate_planning_doc"],
        )
    
    @task
    def fix_planning_doc_task(self) -> Task:
        """Fix hallucinations and errors found during validation."""
        return Task(
            config=self.tasks_config["fix_planning_doc"],
        )
    
    # ==========================================================================
    # THREE CORE WORKFLOWS
    # ==========================================================================
    # 
    # 1. plan_workflow    - Design a feature (research → advise → design → validate)
    # 2. bug_workflow     - Find & fix bugs (investigate → analyze → fix → validate)  
    # 3. implement_workflow - Build from plan (read → code → content → validate → docs)
    #
    # Each workflow is SELF-CONTAINED - it handles everything including validation.
    # ==========================================================================
    
    @crew
    def plan_workflow(self) -> Crew:
        """
        PLANNING WORKFLOW - Design a feature completely.
        
        CLI: enlisted-crew plan -f "feature-name" -d "description"
        
        Complete chain:
        1. systems_analyst → Research existing systems
        2. architecture_advisor → Suggest best practices & improvements
        3. feature_architect → Design the feature spec
        4. documentation_maintainer → Write planning doc to docs/CrewAI_Plans/
        5. code_analyst → VALIDATE plan accuracy (no hallucinated files/IDs)
        
        Output: Validated planning doc ready for implementation.
        """
        # Task 1: Research existing systems
        analyze = self.analyze_systems_task()
        
        # Task 2: Suggest improvements based on analysis
        suggest = self.suggest_improvements_task()
        suggest.context = [analyze]
        
        # Task 3: Design the feature (receives research + suggestions)
        design = self.design_feature_task()
        design.context = [analyze, suggest]
        
        # Task 4: Write the planning doc (receives all prior work)
        create_doc = self.create_planning_doc_task()
        create_doc.context = [analyze, suggest, design]
        
        # Task 5: Validate the plan is accurate
        validate_doc = self.validate_planning_doc_task()
        validate_doc.context = [create_doc]
        
        # Task 6: Fix any hallucinations found (auto-correction loop)
        fix_doc = self.fix_planning_doc_task()
        fix_doc.context = [create_doc, validate_doc]
        
        return Crew(
            agents=[
                self.systems_analyst(),
                self.architecture_advisor(),
                self.feature_architect(),
                self.documentation_maintainer(),
                self.code_analyst(),
            ],
            tasks=[
                analyze,
                suggest,
                design,
                create_doc,
                validate_doc,
                fix_doc,  # Auto-correct hallucinations
            ],
            process=Process.sequential,
            verbose=True,
            memory=True,  # Enable crew memory for context across tasks
            cache=True,   # Cache tool results to avoid redundant calls
            embedder=EMBEDDER_CONFIG,  # Use text-embedding-3-small (larger context than ada-002)
        )
    
    @crew
    def bug_workflow(self) -> Crew:
        """
        BUG HUNTING WORKFLOW - Find and fix bugs completely.
        
        CLI: enlisted-crew hunt-bug -d "description" -e "error codes"
        
        Complete chain:
        1. code_analyst → Investigate logs, find the bug
        2. systems_analyst → Analyze related systems for impact
        3. csharp_implementer → Propose minimal fix
        4. qa_agent → VALIDATE fix is correct, builds, doesn't break anything
        
        Output: Bug report + validated fix ready to apply.
        """
        # Task 1: Investigate the bug
        investigate = self.investigate_bug_task()
        
        # Task 2: Analyze related systems
        analyze_systems = self.analyze_bug_systems_task()
        analyze_systems.context = [investigate]
        
        # Task 3: Propose fix (receives investigation + analysis)
        propose_fix = self.propose_bug_fix_task()
        propose_fix.context = [investigate, analyze_systems]
        
        # Task 4: Validate the fix
        validate_fix = self.validate_bug_fix_task()
        validate_fix.context = [investigate, analyze_systems, propose_fix]
        
        return Crew(
            agents=[
                self.code_analyst(),
                self.systems_analyst(),
                self.csharp_implementer(),
                self.qa_agent(),
            ],
            tasks=[
                investigate,
                analyze_systems,
                propose_fix,
                validate_fix,
            ],
            process=Process.sequential,
            verbose=True,
            memory=True,  # Enable crew memory for context across tasks
            cache=True,   # Cache tool results to avoid redundant calls
            embedder=EMBEDDER_CONFIG,  # Use text-embedding-3-small (larger context than ada-002)
        )
    
    @crew
    def implement_workflow(self) -> Crew:
        """
        IMPLEMENTATION WORKFLOW - Build from an approved plan.
        
        CLI: enlisted-crew implement -p "docs/CrewAI_Plans/feature.md"
        
        Complete chain:
        1. systems_analyst → Read plan, understand what to build
        2. csharp_implementer → Write C# code
        3. content_author → Write JSON content
        4. qa_agent → VALIDATE everything builds and works
        5. documentation_maintainer → Update docs to reflect implementation
        
        Output: Complete implementation + updated documentation.
        """
        # Task 1: Analyze the plan
        analyze = self.analyze_systems_task()
        
        # Task 2: Implement C# code
        impl_csharp = self.implement_csharp_task()
        impl_csharp.context = [analyze]
        
        # Task 3: Implement JSON content
        impl_content = self.implement_content_task()
        impl_content.context = [analyze, impl_csharp]
        
        # Task 4: Validate everything
        validate = self.validate_all_task()
        validate.context = [impl_csharp, impl_content]
        
        # Task 5: Update documentation
        sync_docs = self.sync_documentation_task()
        sync_docs.context = [impl_csharp, impl_content, validate]
        
        return Crew(
            agents=[
                self.systems_analyst(),
                self.csharp_implementer(),
                self.content_author(),
                self.qa_agent(),
                self.documentation_maintainer(),
            ],
            tasks=[
                analyze,
                impl_csharp,
                impl_content,
                validate,
                sync_docs,
            ],
            process=Process.sequential,
            verbose=True,
            memory=True,  # Enable crew memory for context across tasks
            cache=True,   # Cache tool results to avoid redundant calls
            embedder=EMBEDDER_CONFIG,  # Use text-embedding-3-small (larger context than ada-002)
        )
    
    # ==========================================================================
    # UTILITY CREWS (kept for specific use cases)
    # ==========================================================================
    
    @crew
    def validation_crew(self) -> Crew:
        """
        Quick validation check - pre-commit or CI.
        
        CLI: enlisted-crew validate
        
        Runs build + content validation without the full workflow overhead.
        """
        return Crew(
            agents=[self.content_analyst(), self.qa_agent()],
            tasks=[self.validate_all_task()],
            process=Process.sequential,
            verbose=True,
            cache=True,   # Cache tool results to avoid redundant calls
        )
