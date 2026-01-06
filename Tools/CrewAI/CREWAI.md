# Enlisted CrewAI - Master Documentation

**Summary:** Three AI workflows for Enlisted Bannerlord mod development.  
**Status:** ✅ Implemented  
**Last Updated:** 2026-01-05

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
    llm=GPT5_ARCHITECT,            # High reasoning capability
    tools=[...],                   # 4-6 tools for coordination
    max_retry_limit=2,
    allow_delegation=True,         # Can delegate
)
```

**Analysis Agents (GPT-5.2 standard reasoning):**
```python
Agent(
    llm=GPT5_ANALYST,              # Standard reasoning
    tools=[...],                   # 7-10 tools
    max_retry_limit=2,             # Allow recovery
    allow_delegation=False,        # Execution role
)
```

**Implementation Agents (GPT-5 mini execution):**
```python
Agent(
    llm=GPT5_IMPLEMENTER,          # Fast, cost-efficient
    tools=[...],                   # 3-8 tools
    max_retry_limit=1,
    allow_delegation=False,
)
```

**Fast Validation Agents (GPT-5 nano):**
```python
Agent(
    llm=GPT5_FAST,                 # Ultra-fast validation
    tools=[...],                   # 3-5 tools
)
```

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
    embedder=EMBEDDER_CONFIG,      # text-embedding-3-large for superior knowledge retrieval
)
```

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

```bash
cd Tools/CrewAI
crewai test --n_iterations 3
```

This runs your crew multiple times and reports success rate, token usage, and execution time.

**What to verify:**
- Plan workflow: Creates doc in `docs/CrewAI_Plans/`, has all required sections
- Implement workflow: Code compiles, JSON validates, XML synced, `git diff` shows expected changes
- Bug workflow: Investigation identifies root cause, fix compiles

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

Structured, queryable knowledge that complements CrewAI's automatic memory.

**Setup:**
```powershell
.\Tools\CrewAI\setup_database.ps1
```

Creates `C:\Dev\SQLite3\enlisted_knowledge.db` with:
- `tier_definitions` - XP thresholds from `progression_config.json`
- `error_catalog` - Error codes from source (E-MUSTER-*, E-UI-*, etc.)
- `game_systems` - Core behaviors (EnlistmentBehavior, ContentOrchestrator, etc.)
- `content_metadata` - All events/decisions/orders with categories/severities
- `api_patterns` - Bannerlord API usage examples
- `implementation_history` - What was built when

**Database tools:** `lookup_error_code`, `get_tier_info`, `get_system_dependencies`, `lookup_api_pattern`, `record_implementation`, `scan_content_files`

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

**Automated Test Script:**
```bash
# From project root
.\Tools\CrewAI\test_flows.ps1
```

This script tests all three flows with real-world scenarios:
1. **PlanningFlow** - Generate a test feature design
2. **ImplementationFlow** - (Requires existing plan)
3. **BugHuntingFlow** - Investigate and fix a test bug

**Manual Testing:**
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

## Related Documentation

- **Root:** [WARP.md](../../WARP.md) - Project-wide conventions
- **Tools:** [AGENT-WORKFLOW.md](../AGENT-WORKFLOW.md) - Agent usage patterns
- **Core:** [BLUEPRINT.md](../../docs/BLUEPRINT.md) - System architecture
- **Content:** [event-system-schemas.md](../../docs/Features/Content/event-system-schemas.md)
- **Content:** [writing-style-guide.md](../../docs/Features/Content/writing-style-guide.md)
- **Planning:** [docs/CrewAI_Plans/](../../docs/CrewAI_Plans/) - Generated planning documents
