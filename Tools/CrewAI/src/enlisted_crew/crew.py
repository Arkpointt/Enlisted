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
    # Database Query
    lookup_content_id,
    search_content,
    get_balance_value,
    search_balance,  # MISSING! Used by systems_analyst, balance_analyst, architecture_advisor
    lookup_error_code,
    get_tier_info,
    get_system_dependencies,
    lookup_api_pattern,
    get_valid_categories,
    get_valid_severities,
    lookup_core_system,
    get_all_tiers,
    get_balance_by_category,  # MISSING! Used by feature_architect, balance_analyst
    check_database_health,  # MISSING! Used by qa_agent
    # Database Maintenance
    add_content_item,
    update_content_item,
    delete_content_item,
    update_balance_value,
    add_balance_value,
    add_error_code,
    add_system_dependency,
    record_implementation,
    sync_content_from_files,
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
# Prompt Caching: ENABLED on all models
# - Reduces input costs by ~90% for repeated knowledge sources
# - Reduces latency by ~85% (time-to-first-token)
# - Perfect for stable context (docs, error codes, balance values)
#
# Tier 1: Deep Reasoning (architecture, design, system integration)
# Tier 2: Standard Reasoning (code analysis, bug investigation)
# Tier 3: Execution (implementation from specs)
# Tier 4: Fast Validation (schema checks, style checks)

import os as _os
def _get_env(name: str, default: str) -> str:
    return _os.environ.get(name, default)

# =============================================================================
# LLM TIERS - Optimized reasoning_effort for cost/performance balance
# =============================================================================
# high   = Deep thinking, more tokens, slower (architecture, complex decisions)
# medium = Balanced reasoning (analysis, QA, catching bugs)
# low    = Quick thinking (implementation from clear specs)
# none   = Instant mode only (validation, formatting, simple tasks)
# =============================================================================

# TIER 1: HIGH reasoning - architectural decisions requiring deep thought
# Use for: Feature design, system integration, multi-file analysis
GPT5_ARCHITECT = LLM(
    model=_get_env("ENLISTED_LLM_ARCHITECT", "gpt-5.2"),
    max_completion_tokens=16000,
    temperature=0.7,
    reasoning_effort="high",  # Deep reasoning for complex architecture
)

# TIER 2: MEDIUM reasoning - analysis requiring careful thought
# Use for: Code review, bug investigation, understanding systems
GPT5_ANALYST = LLM(
    model=_get_env("ENLISTED_LLM_ANALYST", "gpt-5.2"),
    max_completion_tokens=12000,
    temperature=0.5,
    reasoning_effort="medium",  # Balanced reasoning for analysis
)

# TIER 3: LOW reasoning - execution from clear specifications
# Use for: Writing code from specs, implementing defined changes
GPT5_IMPLEMENTER = LLM(
    model=_get_env("ENLISTED_LLM_IMPLEMENTER", "gpt-5.2"),
    max_completion_tokens=8000,
    temperature=0.3,
    reasoning_effort="low",  # Quick thinking, specs are clear
)

# TIER 4: NONE reasoning - pure execution, no thinking needed
# Use for: Schema validation, style checks, content from templates
GPT5_FAST = LLM(
    model=_get_env("ENLISTED_LLM_FAST", "gpt-5.2"),
    max_completion_tokens=4000,
    temperature=0.2,
    reasoning_effort="none",  # Instant mode, no reasoning overhead
)

# TIER 2.5: MEDIUM reasoning - QA needs to catch what others missed
# Use for: Final validation before commit, finding edge cases
GPT5_QA = LLM(
    model=_get_env("ENLISTED_LLM_QA", "gpt-5.2"),
    max_completion_tokens=6000,
    temperature=0.3,
    reasoning_effort="medium",  # Needs reasoning to catch bugs
)

# === Embedder Configuration ===
# Use text-embedding-3-large for memory/knowledge embeddings.
# 3,072 dimensions (vs 1,536 in 3-small) for superior semantic understanding.
# Same 8,191 token limit, higher accuracy for technical content retrieval.
EMBEDDER_CONFIG = {
    "provider": "openai",
    "config": {
        "model_name": "text-embedding-3-large",
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
        self._check_index_staleness()
        self._init_knowledge_sources()
    
    def _check_index_staleness(self):
        """Check if MCP index is stale and warn user."""
        from pathlib import Path
        
        mcp_index = self.project_root / "Tools" / "CrewAI" / "mcp_servers" / "bannerlord_api_index.db"
        decompile_path = Path(r"C:\Dev\Enlisted\Decompile")
        
        if not mcp_index.exists():
            print("\n[WARN] MCP index not found. Run: cd Tools/CrewAI/mcp_servers && python build_index.py\n")
            return
        
        if not decompile_path.exists():
            return  # Can't check staleness without decompile folder
        
        # Compare modification times (sample check)
        index_mtime = mcp_index.stat().st_mtime
        sample_dirs = ["TaleWorlds.Core", "TaleWorlds.CampaignSystem"]
        
        for subdir in sample_dirs:
            check_dir = decompile_path / subdir
            if check_dir.exists():
                for cs_file in list(check_dir.rglob("*.cs"))[:5]:
                    if cs_file.stat().st_mtime > index_mtime:
                        print("\n" + "="*60)
                        print("[WARN] MCP INDEX IS STALE - Decompile folder has newer files!")
                        print("       Run: cd Tools/CrewAI && .\\update_after_patch.ps1")
                        print("="*60 + "\n")
                        return
    
    def _init_knowledge_sources(self):
        """
        Initialize knowledge sources from the knowledge/ folder.
        
        These provide dynamic context that changes with the codebase,
        unlike static backstories which should remain architectural.
        
        Note: CrewAI expects relative paths from the project's knowledge/ directory.
        
        Chunk settings (for embedding model compatibility):
        - chunk_size=24000 chars (~6000 tokens) - fits within 8191 token embedding limit
        - chunk_overlap=2400 chars (~600 tokens) - 10% overlap maintains context
        - text-embedding-3-small model has 8191 token limit
        - Using ~6000 tokens leaves headroom for dense technical content
        """
        # Shared chunk settings for embedding model compatibility
        # Much larger chunks than before - GPT-5 + text-embedding-3-small handle this well
        chunk_params = {"chunk_size": 24000, "chunk_overlap": 2400}
        
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
                read_source_section, # Read code snippets
                read_source,         # Read full files when needed
                # Database tools for systems analysis - CRITICAL!
                get_system_dependencies,  # Understand how systems interact
                lookup_core_system,       # Get core system definitions
                lookup_api_pattern,       # Verify Bannerlord API patterns
                get_all_tiers,            # Understand tier progression impact
                search_balance,           # Search balance values by category
            ],
            knowledge_sources=[self.systems_knowledge],
            respect_context_window=True,
            max_iter=10,  # Prevent infinite loops
            max_retry_limit=2,
            reasoning=True,  # Enable reflection before acting
            max_reasoning_attempts=3,  # Allow 3 reasoning cycles
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
                # Database tools for analysis
                lookup_error_code,      # Identify known error patterns
                lookup_api_pattern,     # Verify correct API usage
                get_system_dependencies,  # Understand system interactions
                lookup_core_system,     # Identify affected core systems
            ],
            knowledge_sources=[self.code_knowledge],
            respect_context_window=True,
            max_iter=10,  # Prevent infinite loops
            max_retry_limit=1,
            reasoning=True,  # Enable reflection for bug analysis
            max_reasoning_attempts=3,  # Allow 3 reasoning cycles
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
                # Database tools for content analysis
                lookup_content_id,  # Verify content IDs exist
                search_content,     # Find related content
                get_valid_categories,  # Verify categories are valid
                get_valid_severities,  # Verify severities are valid
            ],
            knowledge_sources=[self.content_knowledge],
            respect_context_window=True,
            max_iter=10,  # Prevent infinite loops
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
                # Database tools for architectural design
                get_system_dependencies,  # Understand how systems interact
                lookup_api_pattern,       # Find correct Bannerlord API patterns
                lookup_core_system,       # Identify which core systems will be affected
                get_all_tiers,            # Understand tier progression for features
                get_balance_by_category,  # Get balance values by category for design
            ],
            knowledge_sources=[self.design_knowledge],  # Includes game-design-principles.md
            respect_context_window=True,
            max_iter=10,  # Prevent infinite loops
            max_retry_limit=2,
            reasoning=True,  # Enable reflection for architecture design
            max_reasoning_attempts=3,  # Allow 3 reasoning cycles
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
                # Database tools
                lookup_api_pattern,  # Find correct Bannerlord API usage
                get_balance_value,   # Get correct thresholds/costs
                add_error_code,      # Add new error codes when implementing logging
                add_system_dependency,  # Record new system relationships
            ],
            knowledge_sources=[self.ui_knowledge],
            respect_context_window=True,
            max_iter=10,  # Prevent infinite loops
            reasoning=True,  # Enable reflection for code generation
            max_reasoning_attempts=3,  # Allow 3 reasoning cycles
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
                # Database maintenance
                lookup_content_id,   # Check if ID exists before creating
                add_content_item,    # Register new content in database
                get_valid_categories,  # Get valid categories
                get_valid_severities,  # Get valid severities
                get_tier_info,       # Get tier XP ranges for tier-aware content
            ],
            knowledge_sources=[self.content_knowledge],
            respect_context_window=True,
            max_iter=10,  # Prevent infinite loops
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
                check_game_patterns,
                # Database tools for QA - CRITICAL for verification!
                lookup_error_code,      # Check if errors match known patterns
                lookup_content_id,      # Verify content IDs exist
                search_content,         # Find related content for validation
                get_valid_categories,   # Verify categories are valid
                get_valid_severities,   # Verify severities are valid
                lookup_api_pattern,     # Verify API usage is correct
                check_database_health,  # Verify database integrity
            ],
            knowledge_sources=[self.code_knowledge],  # MISSING! QA needs code patterns
            respect_context_window=True,
            max_iter=10,  # Prevent infinite loops
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
                # Database tools for balance analysis - THIS WAS MISSING!
                get_balance_value,        # Get specific balance values
                search_balance,           # Search all balance values
                get_balance_by_category,  # Get balance values by category
                get_tier_info,            # Get tier XP ranges for balance
                get_all_tiers,            # Get all tier data
                lookup_content_id,        # Verify content IDs
                search_content,           # Find related content for balance review
            ],
            knowledge_sources=[self.balance_knowledge],
            respect_context_window=True,
            max_iter=10,  # Prevent infinite loops
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
                # Database maintenance
                record_implementation,    # Record what was implemented
                sync_content_from_files,  # Sync database with actual JSON files
                update_balance_value,     # Update balance values when changed
            ],
            knowledge_sources=[self.planning_knowledge],
            respect_context_window=True,
            max_iter=10,  # Prevent infinite loops
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
                # Database tools for architecture analysis - CRITICAL!
                get_system_dependencies,  # Understand system interactions
                lookup_core_system,       # Get core system definitions
                lookup_api_pattern,       # Verify Bannerlord API patterns
                get_all_tiers,            # Understand tier progression for architecture
                search_balance,           # Check balance implications
            ],
            knowledge_sources=[self.systems_knowledge],
            respect_context_window=True,
            max_iter=10,  # Prevent infinite loops
            max_retry_limit=2,
            reasoning=True,  # Enable reflection for architecture analysis
            max_reasoning_attempts=3,  # Allow 3 reasoning cycles
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
    # All workflows are now Flow-based (see flows/ directory):
    # 1. PlanningFlow       - Design a feature (flows/planning_flow.py)
    # 2. ImplementationFlow - Build from plan (flows/implementation_flow.py)
    # 3. BugHuntingFlow     - Find & fix bugs (flows/bug_hunting_flow.py)
    #
    # Each Flow uses state persistence and conditional routing.
    # The only Crew remaining here is bug_workflow (legacy, kept for compatibility).
    # ==========================================================================
    
    # NOTE: plan_workflow has been replaced by PlanningFlow
    # (see flows/planning_flow.py) which uses Flow-based state management
    # with automatic hallucination detection and correction.
    
    # NOTE: bug_workflow has been replaced by BugHuntingFlow
    # (see flows/bug_hunting_flow.py) which uses Flow-based state management
    # with severity routing and automatic fix validation.
    
    # ==========================================================================
    # UTILITY CREWS (kept for specific use cases)
    # ==========================================================================
    # 
    # NOTE: implement_workflow has been replaced by ImplementationFlow
    # (see flows/implementation_flow.py) which uses Flow-based state management
    # to detect partial implementations and route around completed work.
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
