"""
Enlisted CrewAI Crew Orchestration

Main crew class that coordinates agents for Enlisted mod development workflows.

Model Strategy (Claude 4.5 family):
- Opus 4.5 + thinking: Complex architecture, multi-file design (feature_architect, systems_analyst)
- Sonnet 4.5 + thinking: Code analysis, implementation (code_analyst, csharp_implementer)
- Haiku 4.5: Fast validation, content generation (qa_agent, content_author, content_analyst, balance_analyst)
"""

import os
from pathlib import Path
from crewai import Agent, Crew, Process, Task, LLM
from crewai.project import CrewBase, agent, crew, task
from crewai.knowledge.source.text_file_knowledge_source import TextFileKnowledgeSource

from .tools import (
    validate_content_tool,
    sync_localization_tool,
    run_build_tool,
    analyze_validation_report_tool,
    check_writing_style_tool,
    check_tooltip_style_tool,
    suggest_style_improvements_tool,
    validate_event_schema_tool,
    create_event_json_tool,
    read_event_file_tool,
    list_event_files_tool,
    check_code_style_tool,
    check_bannerlord_patterns_tool,
    check_framework_compatibility_tool,
    check_csharp_file_tool,
    read_doc_tool,
    list_docs_tool,
    search_docs_tool,
    read_csharp_tool,
    search_csharp_tool,
    read_csharp_snippet_tool,
    list_feature_files_tool,
    read_debug_logs_tool,
    search_debug_logs_tool,
    read_native_crash_logs_tool,
    search_native_api_tool,
    read_writing_style_guide_tool,
    load_content_context_tool,
    load_feature_context_tool,
    load_code_context_tool,
    load_domain_context_tool,
    write_planning_doc_tool,
    verify_file_exists_tool,
    list_json_event_ids_tool,
)

# === LLM Configurations ===
# Philosophy: Spend tokens on ANALYSIS and VALIDATION, save on EXECUTION.
# Bugs from cheap generation cost more to fix than thinking tokens.
#
# Model versions: Claude 4.5 family (Opus, Sonnet, Haiku)
# - Opus 4.5: 200K context, best for complex multi-system reasoning
# - Sonnet 4.5: 200K context, excellent coding and analysis
# - Haiku 4.5: 200K context, fast and cost-effective
#
# Tier 1: Deep Thinking (complex reasoning, high-stakes decisions)
# Tier 2: Standard Thinking (code analysis, pattern detection)  
# Tier 3: Execution (following specs, generating from templates)
# Tier 4: Fast Validation (schema checks, style checks)

import os as _os
def _get_env(name: str, default: str) -> str:
    return _os.environ.get(name, default)

# TIER 1: Opus 4.5 with deep thinking - architectural decisions
# Use for: Feature design, system integration, multi-file changes
OPUS_DEEP = LLM(
    model=_get_env("ENLISTED_LLM_OPUS_DEEP", "anthropic/claude-opus-4-5-20251101"),
    thinking={"type": "enabled", "budget_tokens": 10000},
    max_tokens=16000,
)

# TIER 2: Sonnet 4.5 with thinking - analysis that requires reasoning
# Use for: Code review, bug analysis, understanding existing systems
SONNET_ANALYSIS = LLM(
    model=_get_env("ENLISTED_LLM_SONNET_ANALYSIS", "anthropic/claude-sonnet-4-5-20250929"),
    thinking={"type": "enabled", "budget_tokens": 5000},
    max_tokens=8000,
)

# TIER 3: Sonnet 4.5 without thinking - execution from clear specs
# Use for: Writing code from specs, implementing defined changes
SONNET_EXECUTE = LLM(
    model=_get_env("ENLISTED_LLM_EXECUTE", "anthropic/claude-sonnet-4-5-20250929"),
    max_tokens=8000,
)

# TIER 4: Haiku 4.5 - high-volume validation and generation
# Use for: Schema validation, style checks, content from templates
HAIKU_FAST = LLM(
    model=_get_env("ENLISTED_LLM_HAIKU", "anthropic/claude-haiku-4-5-20251001"),
    max_tokens=4000,
)

# TIER 2.5: Haiku 4.5 for QA with thinking - safety net
# Use for: Final validation before commit, catching what others missed
# Note: min budget_tokens is 1024 per Anthropic API
SONNET_QA = LLM(
    model=_get_env("ENLISTED_LLM_QA", "anthropic/claude-haiku-4-5-20251001"),
    thinking={"type": "enabled", "budget_tokens": 2048},
    max_tokens=6000,
)


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
        """
        # Systems knowledge - for systems_analyst, feature_architect
        self.systems_knowledge = TextFileKnowledgeSource(
            file_paths=["enlisted-systems.md"]
        )
        
        # Code knowledge - for code_analyst, csharp_implementer
        self.code_knowledge = TextFileKnowledgeSource(
            file_paths=[
                "enlisted-systems.md",
                "error-codes.md",
            ]
        )
        
        # Content knowledge - for content_author, content_analyst
        self.content_knowledge = TextFileKnowledgeSource(
            file_paths=[
                "json-schemas.md",
                "balance-values.md",
            ]
        )
        
        # Balance knowledge - for balance_analyst
        self.balance_knowledge = TextFileKnowledgeSource(
            file_paths=[
                "balance-values.md",
                "enlisted-systems.md",
            ]
        )
        
        # Planning knowledge - for documentation_maintainer (planning tasks)
        self.planning_knowledge = TextFileKnowledgeSource(
            file_paths=[
                "enlisted-systems.md",
                "content-files.md",  # JSON content inventory for verification
            ]
        )
        
        # UI knowledge - for feature_architect, csharp_implementer (UI work)
        self.ui_knowledge = TextFileKnowledgeSource(
            file_paths=[
                "ui-systems.md",
                "enlisted-systems.md",
            ]
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
        """Systems integration analyst - uses Opus for complex system analysis."""
        return Agent(
            config=self.agents_config["systems_analyst"],
            llm=OPUS_DEEP,  # TIER 1: System integration requires deep reasoning
            tools=[
                load_domain_context_tool,  # Context first
                search_docs_tool,          # Find docs
                read_doc_tool,             # Read docs
                search_csharp_tool,        # Find code patterns
            ],
            knowledge_sources=[self.systems_knowledge],
            respect_context_window=True,
            max_retry_limit=2,
            allow_delegation=True,  # Coordination role
        )
    
    @agent
    def code_analyst(self) -> Agent:
        """C# code analyst - uses Sonnet with thinking for code analysis."""
        return Agent(
            config=self.agents_config["code_analyst"],
            llm=SONNET_ANALYSIS,  # TIER 2: Analysis needs reasoning to catch bugs
            tools=[
                load_code_context_tool,  # CALL FIRST - loads dev guide, APIs, common issues
                verify_file_exists_tool,     # Verify file paths in planning docs
                list_json_event_ids_tool,    # Verify event IDs in planning docs
                run_build_tool,
                validate_content_tool,
                check_code_style_tool,
                check_bannerlord_patterns_tool,
                check_framework_compatibility_tool,
                check_csharp_file_tool,
                read_debug_logs_tool,
                search_debug_logs_tool,
                read_native_crash_logs_tool,  # For hard game crashes
                search_native_api_tool,
                search_csharp_tool,  # Search first, then read snippets
                read_csharp_snippet_tool,  # Prefer snippets over full files
                read_csharp_tool,  # Full file only when needed
            ],
            knowledge_sources=[self.code_knowledge],
            respect_context_window=True,
            max_retry_limit=1,
        )
    
    @agent
    def content_analyst(self) -> Agent:
        """Content schema analyst - uses Haiku for fast validation."""
        return Agent(
            config=self.agents_config["content_analyst"],
            llm=HAIKU_FAST,
            tools=[
                validate_event_schema_tool,
                read_event_file_tool,
                list_event_files_tool,
                validate_content_tool,
            ],
            knowledge_sources=[self.content_knowledge],
            respect_context_window=True,
        )
    
    @agent
    def feature_architect(self) -> Agent:
        """Feature architect - uses Opus for complex multi-file design."""
        return Agent(
            config=self.agents_config["feature_architect"],
            llm=OPUS_DEEP,  # TIER 1: Architecture mistakes are expensive
            tools=[
                load_feature_context_tool,    # Context first
                verify_file_exists_tool,      # Verify file paths in specs
                list_json_event_ids_tool,     # Verify event IDs in specs
                search_docs_tool,             # Find docs  
                read_doc_tool,                # Read docs
                search_csharp_tool,           # Find code
                read_csharp_snippet_tool,     # Read sections
                validate_content_tool,        # Check schemas
            ],
            knowledge_sources=[self.systems_knowledge],
            respect_context_window=True,
            max_retry_limit=2,
            allow_delegation=True,  # Can delegate
        )
    
    @agent
    def csharp_implementer(self) -> Agent:
        """C# implementer - uses Sonnet for code generation from specs."""
        return Agent(
            config=self.agents_config["csharp_implementer"],
            llm=SONNET_EXECUTE,  # TIER 3: Executing from clear specs, QA catches issues
            tools=[
                verify_file_exists_tool,      # Verify file paths before proposing changes
                run_build_tool,
                validate_content_tool,
                check_code_style_tool,
                check_bannerlord_patterns_tool,
                check_framework_compatibility_tool,
                check_csharp_file_tool,
                search_native_api_tool,
                search_csharp_tool,  # Search to find relevant code
                read_csharp_snippet_tool,  # Read targeted snippets
                read_csharp_tool,  # Full file when implementing
            ],
            knowledge_sources=[self.ui_knowledge],
            respect_context_window=True,
        )
    
    @agent
    def content_author(self) -> Agent:
        """Content author - uses Haiku for fast content generation."""
        return Agent(
            config=self.agents_config["content_author"],
            llm=HAIKU_FAST,
            tools=[
                load_content_context_tool,  # CALL FIRST - loads style guide, schemas, reminders
                list_json_event_ids_tool,   # Check existing IDs to avoid conflicts
                create_event_json_tool,
                check_writing_style_tool,
                check_tooltip_style_tool,
                suggest_style_improvements_tool,
                read_event_file_tool,
                validate_event_schema_tool,
                read_writing_style_guide_tool,
                read_doc_tool,
            ],
            knowledge_sources=[self.content_knowledge],
            respect_context_window=True,
        )
    
    @agent
    def qa_agent(self) -> Agent:
        """QA agent - uses Haiku 4.5 with thinking as final safety net."""
        return Agent(
            config=self.agents_config["qa_agent"],
            llm=SONNET_QA,  # TIER 2.5: QA is last defense - NEVER skimp here
            tools=[
                validate_content_tool,
                sync_localization_tool,
                run_build_tool,
                analyze_validation_report_tool,
                check_code_style_tool,
            ],
            respect_context_window=True,
        )
    
    @agent
    def balance_analyst(self) -> Agent:
        """Balance analyst - uses Haiku for content review."""
        return Agent(
            config=self.agents_config["balance_analyst"],
            llm=HAIKU_FAST,
            tools=[
                load_domain_context_tool,  # CALL FIRST - needs game balance knowledge
                validate_event_schema_tool,
                read_event_file_tool,
                list_event_files_tool,
                read_doc_tool,
            ],
            knowledge_sources=[self.balance_knowledge],
            respect_context_window=True,
        )
    
    @agent
    def documentation_maintainer(self) -> Agent:
        """Documentation maintainer - uses Sonnet to ensure docs stay in sync."""
        return Agent(
            config=self.agents_config["documentation_maintainer"],
            llm=SONNET_ANALYSIS,  # TIER 2: Doc sync requires reasoning about what changed
            tools=[
                write_planning_doc_tool,     # Write planning docs
                verify_file_exists_tool,     # Verify file paths before including
                list_json_event_ids_tool,    # Verify event IDs before including
                read_doc_tool,
                list_docs_tool,
                search_docs_tool,
                search_csharp_tool,  # Search for code changes
                read_csharp_snippet_tool,  # Read relevant snippets
                read_csharp_tool,  # Full file when needed
                list_feature_files_tool,
                load_domain_context_tool,
            ],
            knowledge_sources=[self.planning_knowledge],
            respect_context_window=True,
        )
    
    @agent
    def architecture_advisor(self) -> Agent:
        """Architecture advisor - uses Opus for deep analysis and improvement suggestions."""
        return Agent(
            config=self.agents_config["architecture_advisor"],
            llm=OPUS_DEEP,  # TIER 1: Architecture analysis needs deep reasoning
            tools=[
                load_domain_context_tool,    # Understand game systems first
                load_feature_context_tool,   # Understand architecture patterns
                verify_file_exists_tool,     # Verify paths when suggesting changes
                search_docs_tool,            # Find documentation
                read_doc_tool,               # Read docs
                search_csharp_tool,          # Find code patterns
                read_csharp_snippet_tool,    # Read targeted snippets
                read_csharp_tool,            # Full files when needed
                list_feature_files_tool,     # Understand feature structure
                search_native_api_tool,      # Check Bannerlord API constraints
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
    
    # === Crews ===
    
    @crew
    def feature_design_crew(self) -> Crew:
        """
        Feature design crew for complex architectural work.
        
        Use for: Features like Orchestrator-Opportunity-Unification, Systems Integration.
        Uses Opus 4.5 + thinking for deep analysis and design.
        
        Workflow:
        1. systems_analyst researches system interconnections
        2. code_analyst analyzes existing code patterns
        3. feature_architect produces detailed spec (receives 1+2)
        4. balance_analyst reviews for game balance
        5. code_analyst validates file paths and event IDs in the spec
        
        The final validation step catches hallucinated references before the spec
        is considered complete.
        """
        # Create tasks with explicit context for non-adjacent dependencies
        analyze_sys = self.analyze_systems_task()
        analyze_code = self.analyze_codebase_task()
        
        # Design needs BOTH systems and code analysis (non-adjacent: needs task 1)
        design = self.design_feature_task()
        design.context = [analyze_sys, analyze_code]
        
        review = self.review_feature_design_task()
        # Review auto-receives design output (adjacent)
        
        # Validation needs the design to check file paths
        validate_spec = self.validate_planning_doc_task()
        validate_spec.context = [design]
        
        return Crew(
            agents=[
                self.systems_analyst(),
                self.code_analyst(),
                self.feature_architect(),
                self.balance_analyst(),
            ],
            tasks=[
                analyze_sys,
                analyze_code,
                design,
                review,
                validate_spec,
            ],
            process=Process.sequential,
            verbose=True,
        )
    
    @crew
    def feature_implementation_crew(self) -> Crew:
        """
        Feature implementation crew for executing designs.
        
        Use for: Implementing approved feature specs.
        Uses Sonnet 4.5 + thinking for code generation.
        
        Workflow:
        1. csharp_implementer writes C# code
        2. content_author creates JSON content (receives C# context)
        3. qa_agent validates everything (receives both)
        """
        # Create tasks - sequential auto-passes adjacent outputs
        impl_csharp = self.implement_csharp_task()
        impl_content = self.implement_content_task()
        # Content auto-receives C# output (adjacent)
        
        # Validation needs both implementation outputs
        validate = self.validate_all_task()
        validate.context = [impl_csharp, impl_content]
        
        return Crew(
            agents=[
                self.csharp_implementer(),
                self.content_author(),
                self.qa_agent(),
            ],
            tasks=[
                impl_csharp,
                impl_content,
                validate,
            ],
            process=Process.sequential,
            verbose=True,
        )
    
    @crew
    def full_feature_crew(self) -> Crew:
        """
        Complete feature development crew (design + implementation + docs).
        
        Use for: End-to-end feature development like:
        - Systems Integration Analysis improvements
        - Content Skill Integration
        - New game mechanics
        
        This is the crew to use for docs/ANEWFEATURE/ style work.
        Managed by feature_architect using Opus 4.5.
        
        INCLUDES documentation_maintainer to ensure docs stay in sync!
        """
        return Crew(
            agents=[
                self.systems_analyst(),
                self.code_analyst(),
                self.content_analyst(),
                self.feature_architect(),
                self.csharp_implementer(),
                self.content_author(),
                self.qa_agent(),
                self.balance_analyst(),
                self.documentation_maintainer(),  # Keeps docs in sync
            ],
            tasks=[self.full_feature_workflow_task(), self.sync_documentation_task()],
            process=Process.hierarchical,
            manager_agent=self.feature_architect(),
            verbose=True,
        )
    
    @crew
    def validation_crew(self) -> Crew:
        """
        Validation-focused crew for checking content integrity.
        
        Use for: Pre-commit validation, CI checks, content audits.
        Uses Haiku 4.5 for fast validation.
        """
        return Crew(
            agents=[self.content_analyst(), self.qa_agent()],
            tasks=[self.validate_all_task()],
            process=Process.sequential,
            verbose=True,
        )
    
    @crew
    def content_creation_crew(self) -> Crew:
        """
        Content creation crew for writing new events.
        
        Use for: Creating new events, expanding content.
        Uses Haiku 4.5 for fast content generation.
        """
        return Crew(
            agents=[self.content_author(), self.content_analyst(), self.balance_analyst()],
            tasks=[self.full_content_workflow_task()],
            process=Process.sequential,
            verbose=True,
        )
    
    @crew
    def code_review_crew(self) -> Crew:
        """
        Code review crew for C# changes.
        
        Use for: PR reviews, code audits, pre-commit checks.
        Uses Sonnet 4.5 + thinking for code analysis.
        
        Workflow:
        1. code_analyst reviews code patterns and style
        2. qa_agent validates implementation (receives review)
        """
        # Sequential auto-passes review output to validation (adjacent)
        return Crew(
            agents=[self.code_analyst(), self.qa_agent()],
            tasks=[self.review_code_task(), self.validate_implementation_task()],
            process=Process.sequential,
            verbose=True,
        )
    
    @crew
    def documentation_crew(self) -> Crew:
        """
        Documentation maintenance crew.
        
        Use for:
        - Post-implementation doc sync (sync_documentation_task)
        - Periodic doc audits (audit_documentation_task)
        - Checking doc accuracy after major changes
        
        Uses Sonnet 4.5 + thinking because doc sync requires reasoning.
        """
        return Crew(
            agents=[self.documentation_maintainer()],
            tasks=[self.sync_documentation_task()],
            process=Process.sequential,
            verbose=True,
        )
    
    @crew
    def documentation_audit_crew(self) -> Crew:
        """
        Documentation audit crew for checking doc accuracy.
        
        Use for: Periodic audits, pre-release checks, finding stale docs.
        """
        return Crew(
            agents=[self.documentation_maintainer()],
            tasks=[self.audit_documentation_task()],
            process=Process.sequential,
            verbose=True,
        )
    
    @crew
    def bug_hunting_crew(self) -> Crew:
        """
        Bug investigation crew for tracking down crashes and issues.
        
        ⚠️ DEPRECATED: Use BugHuntingFlow instead for better state management.
        
        The Flow-based approach (flows/bug_hunting_flow.py) provides:
        - Structured state management with Pydantic models
        - Conditional routing based on bug severity
        - Better debugging and observability
        - Cleaner data passing between agents
        
        Usage:
            from enlisted_crew.flows import BugHuntingFlow
            flow = BugHuntingFlow()
            result = flow.kickoff(inputs={"description": "...", ...})
        
        Or via CLI:
            enlisted-crew hunt-bug -d "bug description" -e "error codes"
        
        This Crew is kept for backwards compatibility but may be removed in future.
        """
        # Create tasks with context chaining
        investigate = self.investigate_bug_task()
        analyze_systems = self.analyze_bug_systems_task()
        analyze_systems.context = [investigate]
        propose_fix = self.propose_bug_fix_task()
        propose_fix.context = [investigate, analyze_systems]
        validate_fix = self.validate_bug_fix_task()
        validate_fix.context = [propose_fix]
        
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
        )
    
    @crew
    def advisory_crew(self) -> Crew:
        """
        Architecture advisory crew for suggesting improvements.
        
        Use for:
        - "What should I improve in the Escalation system?"
        - "Review the Content system architecture"
        - "What are quick wins for the Camp UI?"
        - Proactive improvement suggestions based on industry best practices
        
        Unlike planning_crew (which designs what you ask for), this crew
        PROACTIVELY suggests what SHOULD be built based on:
        - Industry best practices (state machines, event-driven, etc.)
        - Game design patterns (progression, economy, feedback loops)
        - Technical debt identification
        - Bannerlord modding constraints
        
        Workflow:
        1. systems_analyst researches current implementation
        2. architecture_advisor suggests improvements (receives analysis)
        3. documentation_maintainer writes recommendations (receives both)
        4. code_analyst validates file paths (receives doc)
        """
        # Create tasks with explicit context for multi-task dependencies
        analyze = self.analyze_systems_task()
        
        # Suggest needs the analysis (non-adjacent if we add more tasks later)
        suggest = self.suggest_improvements_task()
        suggest.context = [analyze]
        
        # Doc needs both analysis context AND suggestions
        create_doc = self.create_planning_doc_task()
        create_doc.context = [analyze, suggest]
        
        # Validation needs the doc output
        validate_doc = self.validate_planning_doc_task()
        validate_doc.context = [create_doc]
        
        return Crew(
            agents=[
                self.systems_analyst(),
                self.architecture_advisor(),
                self.documentation_maintainer(),
                self.code_analyst(),  # Validates the recommendations
            ],
            tasks=[
                analyze,
                suggest,
                create_doc,
                validate_doc,
            ],
            process=Process.sequential,
            verbose=True,
        )
    
    @crew
    def planning_crew(self) -> Crew:
        """
        Planning-only crew for creating design docs WITHOUT implementation.
        
        Use for:
        - Creating docs/ANEWFEATURE/ planning documents
        - Designing features before committing to implementation
        - Exploring architectural options
        
        IMPORTANT: This crew does NOT implement code or update AI context docs.
        It only produces planning documents with Status: 📋 Planning.
        
        Workflow:
        1. systems_analyst researches existing systems
        2. feature_architect creates design spec (receives analysis)
        3. documentation_maintainer writes the planning doc (receives both)
        4. code_analyst validates the planning doc (checks file paths, event IDs)
        
        The validation step catches hallucinated file names, fabricated event IDs,
        and incorrect folder paths BEFORE the document is finalized.
        """
        # Create tasks with explicit context for multi-task dependencies
        analyze = self.analyze_systems_task()
        
        # Design needs the systems analysis
        design = self.design_feature_task()
        design.context = [analyze]
        
        # Doc creation needs both analysis AND design
        create_doc = self.create_planning_doc_task()
        create_doc.context = [analyze, design]
        
        # Validation needs the doc output
        validate_doc = self.validate_planning_doc_task()
        validate_doc.context = [create_doc]
        
        return Crew(
            agents=[
                self.systems_analyst(),
                self.feature_architect(),
                self.documentation_maintainer(),
                self.code_analyst(),  # Validates the planning doc
            ],
            tasks=[
                analyze,
                design,
                create_doc,
                validate_doc,
            ],
            process=Process.sequential,
            verbose=True,
        )
