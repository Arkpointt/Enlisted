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

## Three Workflows

### 1. 🎯 Plan - Design a Feature
```bash
enlisted-crew plan -f "reputation-integration" -d "Connect reputation to morale/supply"
```

**What it does:**
1. **Research** - systems_analyst investigates existing code
2. **Advise** - architecture_advisor suggests best practices
3. **Design** - feature_architect creates technical spec
4. **Document** - documentation_maintainer writes to `docs/CrewAI_Plans/`
5. **Validate** - code_analyst verifies no hallucinated files/IDs

**Output:** Validated planning doc ready for implementation.

---

### 2. 🐛 Hunt Bug - Find & Fix Issues
```bash
enlisted-crew hunt-bug -d "Crash when opening camp menu" -e "E-CAMPUI-042"
```

**What it does:**
1. **Investigate** - code_analyst searches logs, finds bug location
2. **Analyze** - systems_analyst checks related systems for impact
3. **Fix** - csharp_implementer proposes minimal code fix
4. **Validate** - qa_agent verifies fix builds and doesn't break anything

**Output:** Bug report + validated fix ready to apply.

---

### 3. 🛠️ Implement - Build from Plan
```bash
enlisted-crew implement -p "docs/CrewAI_Plans/reputation-integration.md"
```

**What it does:**
1. **Analyze** - systems_analyst reads plan, understands scope
2. **Code** - csharp_implementer writes C# code
3. **Content** - content_author writes JSON events/decisions
4. **Validate** - qa_agent runs build + content checks
5. **Document** - documentation_maintainer updates docs

**Output:** Complete implementation + updated documentation.

---

## Agents (10 total)

Each workflow uses the agents it needs:

| Agent | Role | Model |
|-------|------|-------|
| systems_analyst | Research existing systems | Opus 4.5 (10k thinking) |
| architecture_advisor | Suggest best practices | Opus 4.5 (10k thinking) |
| feature_architect | Design technical specs | Opus 4.5 (10k thinking) |
| code_analyst | Find bugs, validate plans | Sonnet 4.5 (5k thinking) |
| csharp_implementer | Write C# code | Sonnet 4.5 (execution) |
| content_author | Write JSON events | Haiku 4.5 (fast) |
| content_analyst | Validate JSON schemas | Haiku 4.5 (fast) |
| qa_agent | Final validation | Haiku 4.5 (3k thinking) |
| documentation_maintainer | Write/update docs | Sonnet 4.5 (5k thinking) |
| balance_analyst | Review game balance | Haiku 4.5 (fast) |

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
ANTHROPIC_API_KEY=sk-ant-api03-your-key-here
OPENAI_API_KEY=sk-your-openai-key-here  # For embeddings only
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

**Analysis Agents (needs thinking):**
```python
Agent(
    llm=SONNET_ANALYSIS,           # 5k thinking tokens
    tools=[...],                   # 4-10 tools
    max_retry_limit=2,             # Allow recovery
    allow_delegation=False,        # Execution role
)
```

**Coordination Agents (delegation):**
```python
Agent(
    llm=OPUS_DEEP,                 # 10k thinking tokens
    tools=[...],                   # 4-6 tools
    max_retry_limit=2,
    allow_delegation=True,         # Can delegate
)
```

**Execution Agents (no thinking):**
```python
Agent(
    llm=SONNET_EXECUTE,            # No thinking
    tools=[...],                   # 3-5 tools
    max_retry_limit=1,
    allow_delegation=False,
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
    memory=True,                   # Enable crew memory across tasks
    cache=True,                    # Cache tool results (avoids redundant calls)
)
```

| Parameter | Purpose | When to Use |
|-----------|---------|-------------|
| `memory=True` | Share context between tasks | Multi-step workflows |
| `cache=True` | Cache tool results | Avoid redundant API/search calls |
| `verbose=True` | Show execution details | Development/debugging |

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

### Model Strategy (Claude 4.5 Family)

| Tier | Model | Use | Thinking |
|------|-------|-----|----------|
| 1 | Opus 4.5 | Architecture, complex design | 10k tokens |
| 2 | Sonnet 4.5 | Code analysis, doc sync | 5k tokens |
| 2.5 | Haiku 4.5 | QA validation | 3k tokens |
| 3 | Sonnet 4.5 | Code generation from specs | None |
| 4 | Haiku 4.5 | Schema checks, content generation | None |

**Philosophy:** "Spend tokens on ANALYSIS and VALIDATION, save on EXECUTION. Bugs from cheap generation cost more to fix than thinking tokens."

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

## Related Documentation

- **Root:** [WARP.md](../../WARP.md) - Project-wide conventions
- **Tools:** [AGENT-WORKFLOW.md](../AGENT-WORKFLOW.md) - Agent usage patterns
- **Core:** [BLUEPRINT.md](../../docs/BLUEPRINT.md) - System architecture
- **Content:** [event-system-schemas.md](../../docs/Features/Content/event-system-schemas.md)
- **Content:** [writing-style-guide.md](../../docs/Features/Content/writing-style-guide.md)
- **Planning:** [docs/CrewAI_Plans/](../../docs/CrewAI_Plans/) - Generated planning documents
