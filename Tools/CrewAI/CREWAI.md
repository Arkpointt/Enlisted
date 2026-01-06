# Enlisted CrewAI - Master Documentation

**Summary:** Three AI workflows for Enlisted Bannerlord mod development with advanced conditional routing, Bannerlord API MCP server, SQLite knowledge base (23 database tools), and prompt caching across all LLM tiers.  
**Status:** ✅ Implemented  
**Last Updated:** 2026-01-06 (Added conditional tasks, Bannerlord API MCP server with 8 tools, 23 database tools across all agents, prompt caching enabled for ~90% cost savings on knowledge sources, comprehensive tool audits)

---

## 📑 Quick Start

```bash
# Design a feature
enlisted-crew plan -f "feature-name" -d "what it does"

# Find and fix bugs  
enlisted-crew hunt-bug -d "bug description" -e "E-XXX-*"

# Build from approved plan
enlisted-crew implement -p "docs/CrewAI_Plans/feature.md"

# Pre-commit validation
enlisted-crew validate
```

---

## Three Workflows (All Flow-based)

All workflows use CrewAI Flows with:
- **State persistence** (`persist=True`) - Resume on failure
- **Conditional routing** (`@router`) - Skip completed work
- **GPT-5 models** - No thinking/tool_choice conflicts

---

### 1. Plan - Design a Feature (PlanningFlow)
```bash
enlisted-crew plan -f "reputation-integration" -d "Connect reputation to morale/supply"
```

**What it does:**
1. **Research** - systems_analyst investigates existing code
2. **Advise** - architecture_advisor suggests best practices
3. **Design** - feature_architect creates technical spec
4. **Document** - documentation_maintainer writes to `docs/CrewAI_Plans/`
5. **Validate** - code_analyst verifies no hallucinated files/IDs
6. **Auto-fix** - If hallucinations found, automatically correct them (up to 2 attempts)

**State Persistence:** If the flow fails mid-run, re-running resumes from last successful step.

**Output:** Validated planning doc ready for implementation.

---

### 2. Hunt Bug - Find & Fix Issues (BugHuntingFlow)
```bash
enlisted-crew hunt-bug -d "Crash when opening camp menu" -e "E-CAMPUI-042"
```

**What it does:**
1. **Investigate** - code_analyst searches logs, finds bug location
2. **Route Severity** - Different handling for critical vs minor bugs
3. **Analyze** - systems_analyst checks related systems for impact
4. **Fix** - csharp_implementer proposes minimal code fix
5. **Validate** - qa_agent verifies fix builds and doesn't break anything

**State Persistence:** If the flow fails mid-run, re-running resumes from last successful step.

**Output:** Bug report + validated fix ready to apply.

---

### 3. Implement - Build from Plan (ImplementationFlow)
```bash
enlisted-crew implement -p "docs/CrewAI_Plans/reputation-integration.md"
```

**What it does (smart partial-implementation handling):**
1. **Load Plan** - Read the approved planning document
2. **Verify Existing** - Check what's ALREADY implemented in codebase
   - Searches for C# files/methods mentioned in plan
   - Checks for existing JSON content IDs
   - Determines C# status: COMPLETE / PARTIAL / NOT_STARTED
   - Determines Content status: COMPLETE / PARTIAL / NOT_STARTED
3. **Route C#** - Skip if already complete, implement only missing parts
4. **Route Content** - Skip if already complete, create only missing IDs
5. **Validate** - qa_agent runs build + content checks
6. **Document** - Update plan status, sync database, disperse to feature docs

**Key Feature:** The Flow automatically detects partial implementations and routes around completed work. You can re-run the same plan multiple times safely - it will only do what's still needed.

**State Persistence:** If the flow fails mid-run, re-running resumes from last successful step.

**Output:** Complete implementation + updated documentation.

---

## Agents (10 total)

Each workflow uses the agents it needs:

| Agent | Role | Model |
|-------|------|-------|
| systems_analyst | Research existing systems | GPT-5.2 (high reasoning) |
| architecture_advisor | Suggest best practices | GPT-5.2 (high reasoning) |
| feature_architect | Design technical specs | GPT-5.2 (high reasoning) |
| code_analyst | Find bugs, validate plans | GPT-5.2 (standard reasoning) |
| csharp_implementer | Write C# code | GPT-5 mini (execution) |
| content_author | Write JSON events | GPT-5 nano (fast) |
| content_analyst | Validate JSON schemas | GPT-5 nano (fast) |
| qa_agent | Final validation | GPT-5 mini (standard) |
| documentation_maintainer | Write/update docs | GPT-5.2 (standard reasoning) |
| balance_analyst | Review game balance | GPT-5 nano (fast) |

---

## Setup

### 1. Create Virtual Environment

Requires **Python 3.10-3.13**.

```powershell
cd Tools/CrewAI
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -e .
```

Or with uv:
```powershell
uv venv .venv
.\.venv\Scripts\Activate.ps1
uv pip install -e .
```

### 2. Configure API Keys

Copy the example environment file:

```bash
cp .env.example .env
```

Edit `.env`:
```
OPENAI_API_KEY=sk-your-openai-key-here
```

> **Security:** The `.env` file is gitignored. Never commit API keys.

### 3. Set Project Root (Optional)

If running from outside the project:
```bash
export ENLISTED_PROJECT_ROOT=C:\Dev\Enlisted\Enlisted
```

---

## Custom Tools

Tools use natural naming for readability. The `@tool("Name")` decorator defines what agents see.

### Validation

| Tool | Purpose |
|------|---------|
| `validate_content` | Runs `validate_content.py` |
| `sync_strings` | Runs `sync_event_strings.py` |
| `build` | Runs `dotnet build` |
| `analyze_issues` | Generates prioritized report |

### Style Review

| Tool | Purpose |
|------|---------|
| `review_prose` | Validates against `writing-style-guide.md` |
| `review_tooltip` | Checks tooltips (<80 chars) |
| `suggest_edits` | Provides rewrites |
| `get_style_guide` | Loads style guide |

### Event Schema

| Tool | Purpose |
|------|---------|
| `check_event_format` | JSON structure validation |
| `draft_event` | Generates valid events |
| `read_event` | Reads event files |
| `list_events` | Lists all events |

### Code Review

| Tool | Purpose |
|------|---------|
| `review_code` | Allman braces, _camelCase, XML docs |
| `check_game_patterns` | TextObject, Hero, Equipment patterns |
| `check_compatibility` | .NET Framework 4.7.2 compatibility |
| `review_source_file` | Combined C# analysis |

### Documentation

| Tool | Purpose |
|------|---------|
| `read_doc_tool` | Read project docs |
| `list_docs_tool` | List doc files |
| `find_in_docs` | Search across docs |
| `read_source` | Read C# source |
| `find_in_code` | Search C# codebase |
| `read_source_section` | Read specific sections |
| `list_feature_files_tool` | List `src/Features/` files |

### Debug & Native API

| Tool | Purpose |
|------|---------|
| `read_debug_logs_tool` | Read mod debug logs |
| `search_debug_logs_tool` | Search for error codes |
| `read_native_crash_logs_tool` | Read native game crash logs |
| `find_in_native_api` | Search Bannerlord API docs |

### Context Loaders

| Tool | Purpose |
|------|---------|
| `get_game_systems` | Loads game systems knowledge |
| `get_architecture` | Loads BLUEPRINT, patterns |
| `get_dev_reference` | Loads dev guide, APIs |
| `get_writing_guide` | Loads style guide, schemas |

### Planning

| Tool | Purpose |
|------|---------|
| `save_plan` | Writes to `docs/CrewAI_Plans/` with versioning |
| `load_plan` | Reads planning document |

### Verification

| Tool | Purpose |
|------|---------|
| `verify_file_exists_tool` | Validates C# file paths exist before including in plans |
| `list_event_ids` | Lists all event/opportunity IDs from JSON folders |

### File Writing

| Tool | Purpose |
|------|---------|
| `write_source` | Write C# source files to `src/` directories |
| `write_event` | Write JSON event files to `ModuleData/Enlisted/` |
| `write_doc` | Write/update markdown docs in `docs/Features/`, `docs/CrewAI_Plans/` |
| `update_localization` | Add/update strings in `enlisted_strings.xml` |
| `append_to_csproj` | Add new C# files to Enlisted.csproj compile list |

### Database (SQLite Knowledge Base)

**Query Tools (14 total):**

| Tool | Purpose |
|------|---------|
| `lookup_error_code` | Find error code meanings and solutions |
| `get_tier_info` | Get tier XP threshold, ranks, equipment access for specific tier |
| `get_all_tiers` | Get all 9 tiers with progression data |
| `get_system_dependencies` | Find what systems depend on/interact with a system |
| `lookup_core_system` | Get core system definitions and metadata |
| `lookup_api_pattern` | Get Bannerlord API usage examples and patterns |
| `get_balance_value` | Get specific balance value by key |
| `search_balance` | Search balance values by category |
| `get_balance_by_category` | Get all balance values for a category |
| `lookup_content_id` | Check if content ID exists in database |
| `search_content` | Search content by type, category, status |
| `get_valid_categories` | Get all 26 valid event categories |
| `get_valid_severities` | Get all 5 valid severity levels |
| `check_database_health` | Verify database integrity, find orphaned records |

**Maintenance Tools (9 total):**

| Tool | Purpose |
|------|---------|
| `add_content_item` | Register new event/decision/order in database |
| `update_content_item` | Update existing content metadata |
| `delete_content_item` | Remove content metadata when JSON deleted |
| `add_balance_value` | Register new balance value |
| `update_balance_value` | Update existing balance value |
| `add_error_code` | Register new error code pattern |
| `add_system_dependency` | Document system dependencies |
| `sync_content_from_files` | Scan all JSON files and sync to database |
| `record_implementation` | Log implementation completion for tracking |

---

## Knowledge Sources

The `knowledge/` folder contains **dynamic context** that changes with the codebase. Agents query these at runtime instead of relying on stale backstory text.

### Files

| File | Content | Used By |
|------|---------|---------|
| `core-systems.md` | System summaries, key classes | systems_analyst, feature_architect, balance_analyst |
| `error-codes.md` | Error meanings, log locations | code_analyst |
| `event-format.md` | JSON field requirements | content_analyst, content_author |
| `balance-values.md` | XP rates, tier thresholds, economy | content_analyst, content_author, balance_analyst |
| `ui-systems.md` | Menu behaviors, Gauntlet screens | csharp_implementer |
| `content-files.md` | JSON content inventory, folder locations | documentation_maintainer |
| `game-design-principles.md` | Tier-aware design, player experience | feature_architect, content_author, balance_analyst |

### Knowledge Source Mapping

```python
# In crew.py _init_knowledge_sources()
systems_knowledge → systems_analyst (core-systems)
design_knowledge → feature_architect (core-systems + game-design-principles)
code_knowledge → code_analyst (core-systems + error-codes)
content_knowledge → content_analyst, content_author (event-format + balance + game-design-principles)
balance_knowledge → balance_analyst (balance + core-systems + game-design-principles)
ui_knowledge → csharp_implementer (ui-systems + core-systems)
planning_knowledge → documentation_maintainer (core-systems + content-files)
```

**Note:** `game-design-principles.md` is loaded by agents that make player-facing decisions:
feature_architect (design), content_author (content), balance_analyst (review).

### When to Update

`documentation_maintainer` updates knowledge files when:
- Game systems change (tier thresholds, rep ranges, company needs)
- JSON schema rules change
- New error codes added
- Balance tuning happens

---

## Best Practices

### From CrewAI Official Docs

1. **"Build agents to be dependable, not impressive"** - Focus on reliability
2. **Role-based specialization** - Clear, focused roles per agent
3. **Tool assignment**: Agent level for general, task level for specific
4. **Retry limits**: 2-3 for analysis, 1-2 for execution
5. **Delegation**: Enable for coordinators, disable for executors

### Tool Count Guidelines

| Agent Type | Tool Count | Rationale |
|------------|------------|-----------|
| Coordination | 4-6 tools | Avoid decision paralysis |
| Analysis | 7-10 tools | Needs comprehensive tooling |
| Execution | 3-5 tools | Focused on specific output |

### Agent Configuration Patterns

**Architecture/Coordination Agents (GPT-5.2 high reasoning):**
```python
Agent(
    llm=GPT5_ARCHITECT,            # GPT-5.2 with 10K reasoning tokens
    tools=[...],                   # 4-6 tools for coordination
    max_iter=10,                   # Max iterations before forced stop
    max_retry_limit=3,             # Retries on error
    reasoning=True,                # Enable reflection before acting
    max_reasoning_attempts=3,      # Allow 3 reasoning cycles
    allow_delegation=True,         # Can delegate to other agents
)
```

**Analysis Agents (GPT-5.2 standard reasoning):**
```python
Agent(
    llm=GPT5_ANALYST,              # GPT-5.2 with 5K reasoning tokens
    tools=[...],                   # 7-10 tools
    max_iter=10,
    max_retry_limit=3,
    reasoning=True,                # Enable reflection before acting
    max_reasoning_attempts=3,      # Allow 3 reasoning cycles
    allow_delegation=False,        # Execution role, no delegation
)
```

**Implementation Agents (GPT-5 mini execution):**
```python
Agent(
    llm=GPT5_IMPLEMENTER,          # GPT-5 mini (fast, cost-efficient)
    tools=[...],                   # 3-8 tools
    max_iter=10,
    max_retry_limit=3,
    reasoning=True,                # Enable reflection for complex code
    max_reasoning_attempts=3,      # Allow 3 reasoning cycles
    allow_delegation=False,        # Execution role, no delegation
)
```

**Fast Validation Agents (GPT-5 mini/nano):**
```python
Agent(
    llm=GPT5_FAST,                 # GPT-5 nano (ultra-fast validation)
    tools=[...],                   # 3-5 tools
    max_iter=10,
    max_retry_limit=3,
    allow_delegation=False,        # Execution role, no delegation
)
```

### Advanced Agent Parameters

**Reasoning Agents** (`reasoning=True`, `max_reasoning_attempts=3`):
- Enabled for: systems_analyst, architecture_advisor, feature_architect, code_analyst, csharp_implementer
- Agents reflect on the task before taking action
- Creates a reasoning chain before executing tools or writing output
- Each reasoning attempt consumes tokens (included in thinking budget)
- **When to use:** Complex planning, multi-step analysis, code generation
- **When NOT to use:** Simple validation, file reading, fast execution

**Delegation** (`allow_delegation=True/False`):
- **True:** Coordinator agents (systems_analyst, architecture_advisor, feature_architect) can delegate subtasks to other agents
- **False:** Specialist agents (csharp_implementer, content_author, qa_agent) focus on their domain only
- Delegation enables hierarchical workflows within a single crew
- **Philosophy:** Architects coordinate, specialists execute

**Iteration Limits** (`max_iter=10`, `max_retry_limit=3`):
- `max_iter`: Hard stop after N iterations (prevents infinite loops)
- `max_retry_limit`: How many times to retry on tool/LLM errors
- Set higher for complex tasks, lower for simple validation
- All our agents use `max_iter=10` and `max_retry_limit=3` as conservative defaults

### Workflow Configuration Patterns

Per [CrewAI documentation](https://docs.crewai.com/), Crews support additional parameters:

```python
Crew(
    agents=[...],
    tasks=[...],
    process=Process.sequential,    # or Process.hierarchical
    verbose=True,                  # Show execution details
    memory=True,                   # Enable crew memory across tasks (short-term, long-term, entity)
    cache=True,                    # Cache tool results (avoids redundant calls)
    planning=True,                 # AgentPlanner creates step-by-step execution plan
    planning_llm=GPT5_PLANNING,    # Use GPT-5 for planning (best quality)
    embedder=EMBEDDER_CONFIG,      # text-embedding-3-large for superior knowledge retrieval
)
```

#### Planning Configuration

When `planning=True` is enabled, CrewAI's **AgentPlanner** creates a detailed execution plan before each crew iteration:

**What the Planner Does:**
1. Analyzes the task requirements
2. Identifies which agents are needed
3. Creates a step-by-step execution plan
4. Determines optimal tool usage sequence
5. Anticipates potential blockers

**Planning LLM Selection:**
```python
planning_llm=GPT5_PLANNING  # Uses GPT-5 with 10K thinking tokens
```

**Why GPT-5 for planning?**
- Best reasoning capability for complex task decomposition
- Higher success rate than GPT-5 mini on multi-step plans
- Cost difference is minimal (planning is <5% of total LLM calls)
- Worth the extra $0.01-0.03 per run for better execution plans

**All 15 internal crews** (within PlanningFlow, ImplementationFlow, BugHuntingFlow) use `planning_llm=GPT5_PLANNING`.

#### Memory Configuration

When `memory=True` is enabled, **all three memory types** are automatically active:

**1. Short-Term Memory (Execution Context)**
- Stores recent interactions during the current workflow run
- Uses RAG to recall relevant information within the current execution
- Automatically cleared at the start of each workflow

**2. Long-Term Memory (Cross-Run Learning)**
- Preserves insights and learnings from past executions
- Agents remember patterns like:
  - "Tier-aware events need variant ID suffixes (T1/T2/T3)"
  - "Always validate JSON before syncing to XML"
  - "Use GiveGoldAction for gold changes, not direct Hero.Gold manipulation"
- Persists across multiple runs in platform-specific storage

**3. Entity Memory (Relationship Tracking)**
- Tracks entities encountered during tasks (systems, classes, concepts, factions)
- Builds relationship maps for better context understanding
- Example entities: `EnlistmentBehavior`, `ContentOrchestrator`, `Hero.MainHero`, `Tier System`, `Reputation`

#### Embedder Configuration

```python
EMBEDDER_CONFIG = {
    "provider": "openai",
    "config": {
        "model_name": "text-embedding-3-large",  # 3,072 dimensions
    }
}
```

**Why text-embedding-3-large:**
- 3,072 dimensions (vs 1,536 for text-embedding-3-small)
- Superior semantic understanding for technical content
- Better retrieval accuracy for code and documentation
- Better performance with large knowledge chunks

#### Knowledge Chunking Strategy

```python
TextFileKnowledgeSource(
    file_paths=["knowledge/core-systems.md"],
    chunk_size=24000,      # ~24K chars per chunk
    chunk_overlap=2400,    # 10% overlap between chunks
)
```

**Rationale:**
- **Large chunks** (24K) preserve context for complex technical content
- **10% overlap** ensures concepts at chunk boundaries aren't lost
- Leverages text-embedding-3-large's ability to handle larger context windows
- Reduces fragmentation of long class definitions, system explanations

#### Planning Feature

When `planning=True` is enabled:
- Before each crew iteration, an `AgentPlanner` creates a step-by-step execution plan
- This plan is automatically added to each task description
- Agents understand the full workflow and their role within it

**Benefits:**
- Better task decomposition
- More coherent agent outputs
- Fewer redundant searches or tool calls
- Agents understand dependencies between tasks

#### Memory Storage Location

**Windows:**
```
C:\Users\{username}\AppData\Local\CrewAI\enlisted_crew\
├── knowledge/               # ChromaDB embeddings (chunked knowledge files)
├── short_term_memory/       # Current execution context (RAG-based)
├── long_term_memory/        # Insights from past runs (SQLite DB)
└── entities/                # Entity relationships (RAG-based)
```

**To clear memory for a fresh start:**
```bash
rm -rf ~/AppData/Local/CrewAI/enlisted_crew/long_term_memory/
rm -rf ~/AppData/Local/CrewAI/enlisted_crew/entities/
# Knowledge embeddings will rebuild automatically on next run
```

---

## Testing

### Quick Comprehensive Test

**Run everything at once:**
```bash
cd Tools/CrewAI
python test_all.py
```

This tests:
- ✅ Configuration (flows, state models)
- ✅ Database (23 tools, connectivity)
- ✅ MCP Server (8 tools, index)
- ✅ Agents (10 agents, tool assignments)
- ✅ LLM Configuration (5 tiers, prompt caching)
- ✅ Environment (API keys, paths, knowledge files)

**Exit codes:**
- `0` = All tests passed, system ready
- `1` = Some tests failed, needs attention

### Individual Test Tools

**Test flows with real execution:**
```bash
cd Tools/CrewAI
crewai test --n_iterations 3  # Official CrewAI test (benchmarking)
```

### What to Verify After Testing

**Generated Files:**
- Plan workflow: Doc in `docs/CrewAI_Plans/` with all required sections, no hallucinated references
- Implement workflow: C# in `src/Features/`, JSON in `ModuleData/Enlisted/`, XML localization updated, `.csproj` updated
- Bug workflow: Investigation report, proposed fix, validation results

**Code Quality:**
- [ ] Build succeeds: `dotnet build -c "Enlisted RETAIL" /p:Platform=x64`
- [ ] No new ReSharper warnings
- [ ] Run `python Tools/Validation/validate_content.py`
- [ ] All JSON schemas valid, no missing localization strings

**Database & Docs:**
- [ ] Check `sqlite3 enlisted_knowledge.db` - content_metadata and implementation_history updated
- [ ] Plan status updated (if implementing)
- [ ] Feature docs synchronized

**Review Changes:**
- [ ] Run `git status` and `git diff` to review all modifications
- [ ] Verify no unintended files were modified

### Performance Baselines

After initial testing, establish baselines for regression detection:

| Flow | Target Quality | Max Execution Time | Notes |
|------|---------------|--------------------|-------|
| PlanningFlow | 8.0+ | 180s | Research phase slowest |
| ImplementationFlow | 8.5+ | 300s | C# generation most complex |
| BugHuntingFlow | 8.0+ | 200s | Investigation variable |

### Troubleshooting

**"Module not found: crewai"**
```bash
cd Tools/CrewAI
.\.venv\Scripts\Activate.ps1
pip install -e .
```

**"OPENAI_API_KEY not set"**
```bash
$env:OPENAI_API_KEY = "sk-..."  # Current session
# Or add to .env file
```

**"Flow failed mid-run"**
Flows have `persist=True` - just re-run the same command. The flow will resume from the last successful step.

**"Hallucinated files/IDs in plan"**
PlanningFlow has auto-fix enabled. If validation detects hallucinations, it will automatically correct them (up to 2 attempts).

**Memory location (Windows):**
```
C:\Users\{username}\AppData\Local\CrewAI\enlisted_crew\
├── knowledge/        # ChromaDB embeddings
├── long_term_memory/ # Learnings (SQLite)
└── entities/         # Entity relationships
```

To reset: `rm -rf ~/AppData/Local/CrewAI/enlisted_crew/long_term_memory/`

---

## SQLite Knowledge Database

Structured, queryable knowledge that complements CrewAI's vector-based memory system. The database provides instant lookups for facts that would otherwise require semantic search through markdown files.

### Why Both Database + Vector Knowledge?

**Vector Knowledge (markdown files):**
- Semantic search for concepts and patterns
- Good for: "How do I implement X?", "What's the pattern for Y?"
- Requires embedding and similarity search

**Database (SQLite):**
- Instant lookups for structured facts
- Good for: "What's tier 5 XP threshold?", "What does E-MUSTER-042 mean?"
- Direct SQL queries, no LLM token cost

**Result:** Agents get best of both worlds - semantic understanding from vectors, precise facts from database.

### Setup

```powershell
cd Tools\CrewAI\database
.\setup.ps1
```

Creates `enlisted_knowledge.db` in `Tools/CrewAI/` with production data from the codebase.

### Database Schema

**1. Tier Progression** (`tier_definitions`, `culture_ranks`)
- All 9 tiers (T1-T9) with XP requirements from `progression_config.json`
- Formation selection tiers, equipment access tiers
- Officer and Commander track progression
- Culture-specific rank names (Empire, Sturgia, Aserai, etc.)

**2. Error Catalog** (`error_catalog`)
- 50+ error codes from actual codebase (E-MUSTER-*, E-UI-*, W-*, etc.)
- Category, description, common causes, suggested solutions
- Related systems for each error
- Source: Grepped from all C# logging statements

**3. Game Systems** (`game_systems`, `system_dependencies`)
- Core behaviors: EnlistmentBehavior, CompanySimulationBehavior, ContentOrchestrator, etc.
- File paths, key methods, descriptions
- System dependency graph (who calls what)
- Source: Scanned all `CampaignBehaviorBase` implementations

**4. Content Metadata** (`content_metadata`)
- All events, decisions, orders from JSON files
- **26 verified categories:** `camp_life`, `crisis`, `discipline`, `combat_aftermath`, `nco_decision`, `strategic_choice`, `routine_order`, `tactical_order`, `commander_order`, etc.
- **5 verified severities:** `routine`, `attention`, `important`, `urgent`, `critical`
- Tier ranges, localization keys, descriptions
- Source: Recursively scanned `ModuleData/Enlisted/**/*.json`

**5. API Patterns** (`api_patterns`)
- Common Bannerlord API usage examples
- Patterns for hero management, party operations, clan relationships
- Common mistakes and how to avoid them

**6. Balance Values** (`balance_values`)
- Configurable balance numbers (supply consumption rates, fatigue thresholds, etc.)
- System name, value name, current value, units, notes
- Source: Grepped from config files and C# constants

**7. Implementation History** (`implementation_history`)
- Tracks what was implemented when
- Plan file, date, implementer (human or crew), files modified, content IDs added
- Enables "what's already done?" checks for partial implementations

**8. Monitoring Tables** (see Execution Monitoring section)
- `crew_executions`, `agent_executions`, `task_executions`, `tool_usages`
- Performance tracking, cost tracking (separate from knowledge DB)

### Database Tools

**Query Tools (14 total):**
- `lookup_error_code` - Get error details by code (e.g., "E-MUSTER-042")
- `get_tier_info` - Get tier thresholds, ranks, equipment access
- `get_all_tiers` - Get all 9 tiers with full progression data
- `get_system_dependencies` - Find what systems depend on a given system
- `lookup_core_system` - Get core system definitions and metadata
- `lookup_api_pattern` - Get Bannerlord API usage examples
- `get_balance_value` - Get specific balance values by key
- `search_balance` - Search all balance values, optionally by category
- `get_balance_by_category` - Get all balance values for a category
- `lookup_content_id` - Check if content ID exists
- `search_content` - Search content by type, category, status
- `get_valid_categories` - List all 26 valid event categories
- `get_valid_severities` - List all 5 valid event severities
- `check_database_health` - Verify database integrity, find orphans

**Maintenance Tools (9 total):**
- `add_content_item` - Register new event/decision/order after creating JSON
- `update_content_item` - Update existing content metadata
- `delete_content_item` - Remove content metadata when JSON deleted
- `add_balance_value` - Register new balance value
- `update_balance_value` - Update balance value
- `add_error_code` - Register new error code
- `add_system_dependency` - Document system dependencies
- `sync_content_from_files` - Scan all JSON files and sync to database
- `record_implementation` - Log implementation completion

### How Agents Use the Database

**Content Author:**
- Queries `get_valid_categories` and `get_valid_severities` before creating events
- Calls `add_content_item` after writing new JSON files
- Uses `lookup_content_id` to avoid duplicate IDs

**C# Implementer:**
- Queries `lookup_error_code` to understand existing error patterns
- Uses `get_tier_info` to correctly implement tier-aware features
- Calls `add_system_dependency` after creating new system connections
- Uses `lookup_api_pattern` for Bannerlord API guidance

**Documentation Maintainer:**
- Calls `sync_content_from_files` after implementation to update database
- Calls `record_implementation` to log completion
- Uses `check_database_health` to verify database integrity

**Feature Architect:**
- Queries `get_system_dependencies` to understand impact of changes
- Uses `get_balance_value` to reference current balance numbers
- Queries `content_metadata` to see existing content distribution

### Database Maintenance Workflow

The database is **automatically maintained** during implementation:

1. **Content Author creates JSON** → Calls `add_content_item` with metadata
2. **Documentation Maintainer finishes** → Calls `sync_content_from_files` to catch any missed items
3. **Documentation Maintainer finishes** → Calls `record_implementation` to log completion
4. **Health Check** → `check_database_health` verifies no orphaned records

**Manual Sync** (if needed):
```powershell
# Re-scan all JSON files and update database
cd Tools\CrewAI
python -c "from enlisted_crew.tools.database_tools import sync_content_from_files; sync_content_from_files()"
```

### Database Benefits

1. **Performance** - Instant lookups vs embedding search (no LLM cost)
2. **Accuracy** - Exact values, no semantic ambiguity
3. **Validation** - Check IDs exist before referencing
4. **Auditing** - Track what was implemented when
5. **Schema Enforcement** - Only valid categories/severities allowed
6. **Cross-Referencing** - Link content IDs to systems to error codes

### Backstory Best Practices

Per [CrewAI official documentation](https://docs.crewai.com/), backstories describe
WHO the agent is (expertise, knowledge), not HOW to use tools. Prescriptive
instructions belong in task descriptions.

**Good (Natural - describes expertise):**
```yaml
backstory: >
  You are an expert at understanding multi-system game architectures. You know
  Enlisted's 9-tier progression (T1-T9), triple reputation tracks, and Company
  Needs systems. Check the knowledge files for current balance values.
```

**Bad (Prescriptive - gives instructions):**
```yaml
backstory: >
  ALWAYS call load_domain_context_tool FIRST to understand...
  SEARCH EFFICIENCY RULES (CRITICAL):
  - All search results are cached; NEVER re-search...
```

**Where prescriptive instructions go:**
- Task descriptions in `tasks.yaml` (WORKFLOW sections)
- These instructions are task-specific and can change per task type

---

## Warp Integration

### Scope

This section provides specialized context when working in `Tools/CrewAI/`.  
Root `WARP.md` rules still apply; these are additional guidance.

### File Structure

```
Tools/CrewAI/
├── src/enlisted_crew/
│   ├── config/
│   │   ├── agents.yaml      # Agent definitions
│   │   └── tasks.yaml       # Task templates
│   ├── tools/               # Custom tools (natural naming)
│   ├── crew.py              # Crew orchestration
│   └── main.py              # CLI entry point
├── knowledge/               # Dynamic context (runtime queries)
│   ├── core-systems.md      # System summaries
│   ├── error-codes.md       # Error code meanings
│   ├── event-format.md      # JSON requirements
│   ├── balance-values.md    # Balance numbers
│   ├── ui-systems.md        # Menu behaviors
│   ├── content-files.md     # Content inventory
│   └── game-design-principles.md  # Player experience
├── tests/                   # Unit tests
├── pyproject.toml           # Dependencies
└── CREWAI.md               # This file
```

### Quick Commands

```powershell
# Setup (first time)
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -e .

# Activate (subsequent)
.\.venv\Scripts\Activate.ps1

# Run
enlisted-crew <command>

# Test
pytest tests/
```

### Config Patterns (YAML + Python)

**Critical Patterns Learned 2026-01-05:**

#### 1. Tools: Python Only

❌ **Wrong:** Defining in YAML
```yaml
systems_analyst:
  tools: [validate_content_tool]  # ← Remove this
```

✅ **Correct:** Python only
```python
@agent
def systems_analyst(self) -> Agent:
    return Agent(
        config=self.agents_config["systems_analyst"],
        tools=[validate_content_tool],  # ← Python only
    )
```

#### 2. Task Context: Use Method Names

❌ **Wrong:**
```yaml
context:
  - analyze_systems  # ← KeyError
```

✅ **Correct:**
```yaml
context:
  - analyze_systems_task  # ← Matches @task method
```

#### 3. Knowledge Sources: Relative Paths

✅ **Correct:**
```python
TextFileKnowledgeSource(
    file_paths=["core-systems.md"]  # Relative to knowledge/
)
```

### Sync Requirements

When editing CrewAI config, update:

| File | Purpose |
|------|---------|
| `CREWAI.md` (this file) | Master documentation |
| `Tools/AGENT-WORKFLOW.md` | Crew usage table |
| `docs/Tools/validation-tools.md` | Tool documentation |

### Debugging

**Mod Logs:**
- `Modules/Enlisted/Debugging/Session-A_*.log` (newest)
- `Modules/Enlisted/Debugging/Conflicts-A_*.log` (Harmony conflicts)

**Native Logs:**
- `C:\ProgramData\Mount and Blade II Bannerlord\logs\crashlist.txt`
- `C:\ProgramData\Mount and Blade II Bannerlord\logs\CrashUploader.*.txt`

**Error Code Meanings:**
- `E-ENCOUNTER-*` = Battle/menu state issues
- `E-SAVELOAD-*` = Save corruption
- `E-QM-*` = Quartermaster UI
- `W-DLC-*` = Missing Naval DLC (expected)

---

## Architecture Reference

### Planning Document Versioning

The `write_planning_doc_tool` automatically versions documents saved to `docs/CrewAI_Plans/`:

**First Creation:**
```
feature-name.md
```

**Minor Updates (>70% similar):**
- Updates existing file with revision timestamp
- For iterative refinement

**Major Rewrites (<70% similar):**
- Creates versioned file
- Preserves previous version
```
feature-name.md      ← Original
feature-name-v2.md   ← Major revision
feature-name-v3.md   ← Another major change
```

**Similarity Calculation:**
- Uses Jaccard similarity (word overlap)
- Threshold: 0.70 (70%)

### Flow-Based Workflows

**BugHuntingFlow** (recommended over bug_hunting_crew):
- Structured Pydantic state models
- Conditional routing based on severity
- Better debugging and observability

```python
from enlisted_crew.flows import BugHuntingFlow

flow = BugHuntingFlow()
result = flow.kickoff(inputs={
    "description": "...",
    "error_codes": "E-ENCOUNTER-042",
})

# Structured state access
print(result.investigation.severity)
print(result.fix_proposal.summary)
```

### Model Strategy (OpenAI GPT-5 Family)

| Tier | Model | Use | Reasoning |
|------|-------|-----|-----------|
| 1 | GPT-5.2 | Architecture, system integration | High |
| 2 | GPT-5.2 | Code analysis, bug investigation, doc sync | Standard |
| 2.5 | GPT-5 mini | QA validation | Standard |
| 3 | GPT-5 mini | Code generation from specs | Low |
| 4 | GPT-5 nano | Schema checks, content generation | Minimal |

**Philosophy:** "Spend tokens on ANALYSIS and VALIDATION, save on EXECUTION. Bugs from cheap generation cost more to fix than reasoning tokens."

**Key Benefits:**
- No thinking/tool_choice conflicts (Anthropic-specific issue)
- Consistent 200K context window across all models
- Configurable reasoning effort for all models
- Cost-efficient: GPT-5 mini at $0.25/1M in, GPT-5 nano even cheaper

### Memory & Knowledge Configuration

All three workflows (plan, bug, implement) have crew memory enabled for context sharing between tasks.

**Crew Memory Settings:**
```python
Crew(
    memory=True,   # Share context across tasks
    cache=True,    # Cache tool results to avoid redundancy
    embedder=EMBEDDER_CONFIG,  # OpenAI embeddings
)
```

**Embedder Configuration:**
- **Model:** `text-embedding-3-large`
- **Dimensions:** 3,072 (2x more than 3-small)
- **Token Limit:** 8,191 tokens
- **Cost:** ~$0.13 per 1M tokens
- **Benefit:** Superior semantic understanding for technical content

**Knowledge Source Chunking:**
- **Chunk Size:** 24,000 chars (~6,000 tokens)
- **Chunk Overlap:** 2,400 chars (~600 tokens, 10% overlap)
- **Strategy:** Keep full sections together for context retention
- **Safety Margin:** ~2,000 tokens below embedding limit

This configuration maximizes context retention while staying well within the 8,191 token embedding limit. Agents can understand complete system descriptions without fragmentation.

### Integration Checklist Summary

✅ **File Structure:** All required files present  
✅ **Dependencies:** All project files referenced exist  
✅ **Tool Coverage:** 40+ tools across 8 categories  
✅ **Agent Definitions:** 9 specialized agents  
✅ **Task Definitions:** 15+ task templates  
✅ **Crew Configurations:** 6+ pre-configured workflows  
✅ **CLI Commands:** 7 commands  
✅ **Schema Validation:** Complete coverage  
✅ **Style Validation:** Complete coverage  
✅ **Integration:** validate_content.py, sync_event_strings.py, dotnet build  
✅ **Documentation:** Comprehensive

---

## Testing Your Flows

CrewAI provides a built-in testing command to evaluate crew performance across multiple iterations.

### Using `crewai test`

The official testing command runs your crew multiple times and generates performance metrics.

**Basic Usage:**
```bash
crewai test                    # Default: 2 iterations, gpt-4o-mini
crewai test -n 3 -m gpt-5      # 3 iterations with GPT-5
crewai test --n_iterations 5   # 5 iterations with default model
```

**What It Measures:**
- Task completion quality scores
- Agent effectiveness metrics
- Crew coordination efficiency
- Execution time per run
- Average performance across iterations

**Example Output:**
```
Tasks/Crew/Agents    Run 1    Run 2    Run 3    Avg. Total
---------------------------------------------------------
Task: analyze        8.5      9.0      8.8      8.77
Task: implement      7.8      8.2      8.0      8.00
Crew Overall         8.2      8.6      8.4      8.40
Execution Time (s)   126      145      138      136
```

### Testing Our Three Flows

**Manual Flow Testing:**
```bash
# Test PlanningFlow
enlisted-crew plan -f "Test Feature" -d "A simple feature to validate workflow"

# Test BugHuntingFlow
enlisted-crew hunt-bug -d "Test bug description" -e "E-TEST-001"

# Test ImplementationFlow (requires plan file)
enlisted-crew implement -p "docs/CrewAI_Plans/test-feature.md"
```

### What to Validate After Testing

1. **Generated Files:**
   - Check `docs/CrewAI_Plans/` for planning outputs
   - Verify C# code quality and style compliance
   - Review JSON content for schema adherence

2. **Database Updates:**
   - Run `sqlite3 enlisted_knowledge.db ".tables"` to verify tables exist
   - Check content_metadata was updated
   - Verify implementation_history logged the run

3. **Documentation Sync:**
   - Confirm feature docs were updated
   - Verify plan status changed to "Implemented"
   - Check INDEX.md references new features

4. **Code Changes:**
   - Run `git diff` to review all modifications
   - Verify .csproj was updated with new files
   - Check enlisted_strings.xml for new localization

### Performance Baselines

After initial testing, establish baselines for each flow:
- **PlanningFlow:** Target 8.0+ quality, <180s execution
- **ImplementationFlow:** Target 8.5+ quality, <300s execution  
- **BugHuntingFlow:** Target 8.0+ quality, <200s execution

Use these baselines to detect performance regression after configuration changes.

---

## Execution Monitoring

CrewAI Event Listeners track crew execution for performance insights and optimization.

### What Gets Monitored

**Real-Time Console Output:**
- Crew start/completion with timestamps
- Agent execution with durations
- Task completion progress
- Tool usage (success/failure)

**Database Logging:**
All execution metrics are stored in `enlisted_knowledge.db` for analysis:
- `crew_executions` - Overall workflow runs
- `agent_executions` - Individual agent performance
- `task_executions` - Task-level timing
- `tool_usages` - Tool calls and errors

### View Execution Statistics

```bash
# All crews
enlisted-crew stats

# Specific crew
enlisted-crew stats -c PlanningFlow
enlisted-crew stats -c ImplementationFlow
enlisted-crew stats -c BugHuntingFlow

# View cost tracking
enlisted-crew stats --costs
enlisted-crew stats -c PlanningFlow --costs
```

**Example Output:**
```
==================================================================
CREWAI EXECUTION STATISTICS
==================================================================

Crew: PlanningFlow
  Total Runs: 5
  Avg Duration: 142.35s (2.4m)
  Min Duration: 128.50s
  Max Duration: 167.20s

Crew: ImplementationFlow
  Total Runs: 3
  Avg Duration: 284.67s (4.7m)
  Min Duration: 265.10s
  Max Duration: 312.40s
==================================================================

==================================================================
LLM COST REPORT
==================================================================
Crew: PlanningFlow

Total LLM Calls: 124
Input Tokens: 45,320
Output Tokens: 8,940
Total Cost: $0.1620
Avg Cost/Call: $0.0013
==================================================================
```

### How Monitoring Helps

1. **Identify Bottlenecks**
   - Which agents take longest?
   - Which tasks slow down workflows?
   - Where do tool failures occur?

2. **Track Improvements**
   - Did configuration changes speed things up?
   - Are reasoning agents worth the extra time?
   - Is planning reducing overall execution?

3. **Detect Issues**
   - Which tools fail most often?
   - Are there patterns in errors?
   - Is performance degrading over time?

4. **Optimize Costs** (NEW)
   - Track token usage per workflow
   - Identify expensive operations
   - Compare model costs across runs
   - Budget API spending accurately

---

## Execution Hooks - Safety & Cost Control

CrewAI Execution Hooks provide fine-grained control during agent execution. Located in `hooks.py`, these hooks run automatically before and after every LLM call and tool execution.

### What We've Implemented

**1. LLM Call Hooks** (`@after_llm_call`)
- **Cost Tracking** - Automatic token usage and cost calculation for every LLM call
- **Database Logging** - Persist costs to `llm_costs` table for analysis
- **Real-time Display** - Console output shows cost per call
- **Running Totals** - Track cumulative costs for the current workflow run

**2. Tool Call Hooks** (`@before_tool_call`, `@after_tool_call`)
- **Safety Guards** - Validate arguments before dangerous operations execute
- **Path Validation** - Block writes outside project boundaries
- **SQL Injection Prevention** - Validate content IDs for suspicious patterns
- **Error Logging** - Track tool failures for debugging

### Hooks in Action

**Before Tool Call (Safety Validation):**
```
      [BLOCKED] write_source - Path outside project: C:\Windows\System32\file.cs
      [BLOCKED] delete_content_item - Suspicious content_id (potential SQL injection)
      [Tool executes normally if validation passes]
```

**After LLM Call (Cost Tracking):**
```
      [COST] gpt-5.2: 1240 in + 380 out = $0.0055
      [COST] gpt-5-mini: 620 in + 180 out = $0.0004
      [COST] gpt-5: 2100 in + 850 out = $0.0103
```

**End of Run Summary:**
```
======================================================================
RUN COST SUMMARY
======================================================================
Total LLM Calls: 47
Input Tokens: 125,430
Output Tokens: 38,210
Total Cost: $0.70
======================================================================

======================================================================
SAFETY GUARD SUMMARY
======================================================================
Blocked Operations: 2
  - write_source: Path outside project boundaries
  - delete_content_item: Suspicious content_id (potential SQL injection)
======================================================================
```

### Safety Features

**1. File Write Validation**
   - Blocks absolute paths outside project root
   - Detects suspicious patterns (`../../../`, `C:\Windows`, `/etc/`, `~/.ssh`)
   - Validates all file-writing tools: `write_source`, `write_event`, `write_doc`, `update_localization`, `append_to_csproj`

**2. Database Operation Protection**
   - Requires `content_id` parameter for all database operations
   - Validates content IDs for SQL injection patterns (`'`, `"`, `;`, `--`, `DROP`, `DELETE`)
   - Blocks operations with missing required parameters

**3. Denied Operations Tracking**
   - Logs all blocked operations with reason and timestamp
   - Available for post-run analysis via `get_denied_operations()`
   - Displayed in safety summary at end of run

### Cost Tracking Benefits

- **Transparency** - See exact API costs per LLM call in real-time
- **Optimization** - Identify which agents/tasks consume most tokens
- **Budgeting** - Track cumulative spending per workflow run
- **Model Comparison** - Compare GPT-5.2 vs GPT-5-mini vs GPT-5 costs
- **Historical Analysis** - Query `llm_costs` table for cost trends over time

**Cost Estimates (per 1M tokens):**
- `gpt-5.2`: $2.50 input, $10.00 output
- `gpt-5`: $2.00 input, $8.00 output
- `gpt-5-mini`: $0.10 input, $0.40 output
- `gpt-5-nano`: $0.05 input, $0.20 output

### Database Schema

**New table for cost tracking:**
```sql
CREATE TABLE llm_costs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp TEXT NOT NULL,
    model TEXT NOT NULL,
    input_tokens INTEGER NOT NULL,
    output_tokens INTEGER NOT NULL,
    cost_usd REAL NOT NULL
);
```

**Example cost analysis queries:**
```sql
-- Total cost by model
SELECT model, SUM(cost_usd) as total_cost, COUNT(*) as calls
FROM llm_costs
GROUP BY model
ORDER BY total_cost DESC;

-- Daily cost breakdown
SELECT DATE(timestamp) as date, SUM(cost_usd) as daily_cost
FROM llm_costs
GROUP BY DATE(timestamp)
ORDER BY date DESC;

-- Most expensive LLM calls
SELECT timestamp, model, input_tokens, output_tokens, cost_usd
FROM llm_costs
ORDER BY cost_usd DESC
LIMIT 10;
```

All hooks are **automatically enabled** - no configuration needed. Summaries print at the end of every workflow run.

4. **Optimize Costs**
   - Which workflows use most tokens?
   - Where can we use cheaper models?
   - Are we caching effectively?

### Monitoring Tables Schema

```sql
-- Crew-level tracking
CREATE TABLE crew_executions (
    id INTEGER PRIMARY KEY,
    crew_name TEXT,
    started_at TEXT,
    completed_at TEXT,
    duration_seconds REAL,
    status TEXT,
    output_length INTEGER,
    notes TEXT
);

-- Agent-level tracking
CREATE TABLE agent_executions (
    id INTEGER PRIMARY KEY,
    crew_execution_id INTEGER,
    agent_role TEXT,
    duration_seconds REAL,
    FOREIGN KEY (crew_execution_id) REFERENCES crew_executions(id)
);

-- Task-level tracking
CREATE TABLE task_executions (
    id INTEGER PRIMARY KEY,
    crew_execution_id INTEGER,
    task_description TEXT,
    duration_seconds REAL,
    FOREIGN KEY (crew_execution_id) REFERENCES crew_executions(id)
);

-- Tool usage tracking
CREATE TABLE tool_usages (
    id INTEGER PRIMARY KEY,
    crew_execution_id INTEGER,
    tool_name TEXT,
    executed_at TEXT,
    success BOOLEAN,
    error_message TEXT,
    FOREIGN KEY (crew_execution_id) REFERENCES crew_executions(id)
);
```

### Custom Analysis Queries

```sql
-- Find slowest agents
SELECT agent_role, AVG(duration_seconds) as avg_time
FROM agent_executions
GROUP BY agent_role
ORDER BY avg_time DESC
LIMIT 5;

-- Tool success rates
SELECT 
    tool_name,
    COUNT(*) as total_calls,
    SUM(CASE WHEN success THEN 1 ELSE 0 END) as successes,
    ROUND(100.0 * SUM(CASE WHEN success THEN 1 ELSE 0 END) / COUNT(*), 2) as success_rate
FROM tool_usages
GROUP BY tool_name
ORDER BY total_calls DESC;

-- Workflow trends over time
SELECT 
    DATE(started_at) as date,
    crew_name,
    AVG(duration_seconds) as avg_duration,
    COUNT(*) as runs
FROM crew_executions
WHERE status = 'completed'
GROUP BY DATE(started_at), crew_name
ORDER BY date DESC;
```

**Monitoring is always active.** No configuration needed - just run your workflows and check stats anytime.

---

## Conditional Tasks and Routing

CrewAI provides powerful conditional execution at two levels: **Flow routing** (`@router`) and **Task-level conditions** (`ConditionalTask`).

### Flow-Level Routing with `@router`

**When to use:** Route between major workflow steps based on state.

**Pattern:**
```python
@router(previous_step)
def route_next(self, state: MyState) -> str:
    """Decide which path to take based on state."""
    if my_condition(state):
        return "path_a"
    else:
        return "path_b"

@listen("path_a")
def handle_path_a(self, state: MyState) -> MyState:
    # Only runs if router returned "path_a"
    ...
```

**Best Practices:**
1. **Extract condition functions** - Keep routing logic testable and reusable
2. **Use logging helpers** - Provide clear visibility into routing decisions
3. **Update state.skipped_steps** - Track which paths were not taken

**Example from ImplementationFlow:**
```python
from .conditions import csharp_complete, format_routing_decision

@router(verify_existing)
def route_csharp(self, state: ImplementationState) -> str:
    if csharp_complete(state):
        print(format_routing_decision(
            condition_name="csharp_complete",
            condition_result=True,
            chosen_path="route_content",
            reason=f"C# status: {state.csharp_status.value}"
        ))
        state.skipped_steps.append("implement_csharp")
        return "route_content"
    
    return "implement_csharp"
```

### Task-Level Conditions with `ConditionalTask`

**When to use:** Conditionally execute individual tasks within a Crew based on previous task output.

**Pattern:**
```python
from crewai.tasks.conditional_task import ConditionalTask
from crewai.tasks.task_output import TaskOutput

def needs_deep_check(output: TaskOutput) -> bool:
    """Condition function evaluates previous task output."""
    return "complex" in str(output.raw).lower()

# This task only runs if needs_deep_check returns True
conditional_task = ConditionalTask(
    description="Perform deep validation for complex plans",
    expected_output="Deep validation report",
    condition=needs_deep_check,
    agent=my_agent,
)

crew = Crew(
    agents=[agent1, agent2],
    tasks=[basic_task, conditional_task],  # conditional_task may skip
    process=Process.sequential,
)
```

**Best Practices:**
1. **Pure condition functions** - No side effects, deterministic
2. **Check output.raw** - Access task output text
3. **Use TaskOutput.pydantic** - For structured output (see below)

**Example from PlanningFlow:**
```python
def needs_deep_validation(output: TaskOutput) -> bool:
    """Check if plan mentions complex systems."""
    content = str(output.raw).lower()
    complex_keywords = ["contentorchestrator", "state machine"]
    return any(keyword in content for keyword in complex_keywords)

deep_validation = ConditionalTask(
    description="Deep system integration validation",
    condition=needs_deep_validation,
    agent=get_architecture_advisor(),
)
```

### Structured Output with TaskOutput.pydantic

**Pattern:** Use Pydantic models for type-safe task output parsing.

**1. Define output model:**
```python
# In state_models.py
class ValidationOutput(BaseModel):
    status: ValidationStatus
    issues_found: List[str] = Field(default_factory=list)
    verified_items: List[str] = Field(default_factory=list)
    file_checks: int = 0
```

**2. Condition function with structured access:**
```python
def validation_failed(output: TaskOutput) -> bool:
    """Check validation status using structured output."""
    # Prefer Pydantic model if available
    if hasattr(output, 'pydantic') and output.pydantic:
        if isinstance(output.pydantic, ValidationOutput):
            return output.pydantic.status == ValidationStatus.FAILED
    
    # Fallback to text parsing
    return "status: failed" in str(output.raw).lower()
```

**3. Extract structured data:**
```python
def get_validation_issues(output: TaskOutput) -> List[str]:
    """Extract issues using structured output."""
    if hasattr(output, 'pydantic') and isinstance(output.pydantic, ValidationOutput):
        return output.pydantic.issues_found
    return []
```

### Reusable Condition Functions

All condition functions are centralized in `flows/conditions.py`:

**Planning Flow Conditions:**
- `validation_passed(state)` - Check if validation passed
- `needs_plan_fix(state)` - Check if fix attempt should run
- `validation_complete(state)` - Check if validation process done

**Implementation Flow Conditions:**
- `needs_csharp_work(state)` - Check if C# needed
- `needs_content_work(state)` - Check if JSON needed
- `csharp_complete(state)` - Check if C# done
- `content_complete(state)` - Check if content done

**Bug Hunting Flow Conditions:**
- `needs_systems_analysis(state)` - Check if complex bug
- `is_simple_bug(state)` - Check if simple bug
- `has_high_confidence(state)` - Check investigation confidence

**TaskOutput Conditions:**
- `validation_passed_in_output(output)` - Parse validation result
- `build_succeeded_in_output(output)` - Parse build result
- `get_validation_issues_from_output(output)` - Extract issues

### Routing Decision Logging

Use `format_routing_decision()` for consistent, clear logging:

```python
from .conditions import format_routing_decision

print(format_routing_decision(
    condition_name="needs_csharp_work",
    condition_result=True,
    chosen_path="implement_csharp",
    reason=f"C# status: {state.csharp_status.value} - work needed"
))

# Output:
# 🔀 CONDITIONAL ROUTING
#    Condition: needs_csharp_work
#    Result: ✓ True
#    Decision: implement_csharp
#    Reason: C# status: partial - work needed
```

### Testing Conditional Logic

Condition functions are pure and testable:

```python
def test_needs_csharp_work():
    # NOT_STARTED -> True
    state = ImplementationState(csharp_status=ImplementationStatus.NOT_STARTED)
    assert needs_csharp_work(state) == True
    
    # COMPLETE -> False
    state = ImplementationState(csharp_status=ImplementationStatus.COMPLETE)
    assert needs_csharp_work(state) == False
    
    # PARTIAL -> True
    state = ImplementationState(csharp_status=ImplementationStatus.PARTIAL)
    assert needs_csharp_work(state) == True
```

---

## MCP Servers Integration

CrewAI supports Model Context Protocol (MCP) servers for extending agent capabilities with external tools and services.

### Active MCP Servers

#### ✅ Bannerlord API MCP Server

**Status:** ✅ Implemented and Active

**Purpose:** Provides semantic access to decompiled Bannerlord source code (`C:\Dev\Enlisted\Decompile`) with fast indexed queries.

**Location:** `Tools/CrewAI/mcp_servers/bannerlord_api_server.py`

**Tools Provided:**
- `get_class_definition` - Get full class with methods, properties, interfaces
- `search_api` - Search by name with type filtering (class/method/property)
- `find_implementations` - Find all classes implementing an interface
- `find_subclasses` - Get class inheritance hierarchy
- `get_namespace_contents` - List all classes in a namespace
- `get_method_signature` - Get method parameters, return type, modifiers
- `read_source_code` - **NEW:** Read actual C# implementation code with context
- `find_usage_examples` - **NEW:** Find where/how natives USE an API (real examples)

**Agents Using MCP:**
- `code_analyst` - Verifies API usage during code analysis
- `csharp_implementer` - Validates signatures before writing code
- `feature_architect` - Explores API structure during design

**Setup:**
```powershell
cd Tools/CrewAI/mcp_servers
python build_index.py  # First time only, 2-5 minutes
# Verification included in test_all.py
```

**Performance:**
- Index Build: 2-5 minutes (one-time)
- Index Size: ~50-100 MB
- Query Speed: <50ms
- Indexed: ~8,500 classes, ~65,000 methods, ~25,000 properties

**Prompt Caching:** ✅ Enabled on all LLMs
- ~90% reduction in input costs for repeated knowledge sources
- ~85% faster time-to-first-token
- Caches: core-systems.md, error-codes.md, event-format.md, balance-values.md, etc.

**Replacement:** Deprecates the old `find_in_native_api` tool (slow grep-based search) with semantic indexing.

### Evaluated Standard MCP Servers

| MCP Server | Status | Reason |
|------------|--------|--------|
| **Git MCP** | ⚠️ Consider | Would add version control analysis (git blame, commit history) for bug hunting |
| **Filesystem MCP** | ❌ Redundant | We have `file_tools.py` and `docs_tools.py` |
| **SQLite MCP** | ❌ Redundant | We have 28 database tools in `database_tools.py` |
| **Web Search MCP** | ⚠️ Consider | Could research Bannerlord APIs |
| **Browser MCP** | ❌ Not applicable | Not needed for game modding workflow |

## Related Documentation

- **Root:** [WARP.md](../../WARP.md) - Project-wide conventions
- **Tools:** [AGENT-WORKFLOW.md](../AGENT-WORKFLOW.md) - Agent usage patterns
- **Core:** [BLUEPRINT.md](../../docs/BLUEPRINT.md) - System architecture
- **Content:** [event-system-schemas.md](../../docs/Features/Content/event-system-schemas.md)
- **Content:** [writing-style-guide.md](../../docs/Features/Content/writing-style-guide.md)
- **Planning:** [docs/CrewAI_Plans/](../../docs/CrewAI_Plans/) - Generated planning documents
