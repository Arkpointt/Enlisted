# WARP.md - CrewAI Integration

> **Scope:** This file provides specialized context when working in `Tools/CrewAI/`.
> Root `WARP.md` rules still apply; these are additional guidance.

## 🎯 Purpose

CrewAI handles complex multi-agent tasks that benefit from specialized roles:
- **Bug hunting:** Trace crashes to root cause with log analysis
- **Planning:** Design docs without implementation (ANEWFEATURE/)
- **Full features:** End-to-end design → implement → test → document
- **Validation:** Pre-commit content/code quality checks

## 📁 Structure

```
Tools/CrewAI/
├── src/enlisted_crew/
│   ├── config/
│   │   ├── agents.yaml      # Agent definitions (role, goal, backstory)
│   │   └── tasks.yaml       # Task templates
│   ├── tools/               # Custom tools (docs_tools.py, validation_tools.py)
│   ├── crew.py              # Crew orchestration (bug_hunting_crew, planning_crew, etc.)
│   └── main.py              # Entry point
├── knowledge/               # Dynamic context files (queried at runtime)
│   ├── enlisted-systems.md  # System summaries, key classes, effect details
│   ├── error-codes.md       # Error code meanings, log locations
│   ├── json-schemas.md      # JSON field requirements
│   ├── balance-values.md    # XP rates, tier thresholds, economy
│   └── content-files.md     # JSON content inventory, folder locations
├── tests/                   # Unit tests
└── pyproject.toml           # Dependencies
```

## 📚 Knowledge Folder (CrewAI Knowledge Sources)

The `knowledge/` folder contains **dynamic context** that changes with the codebase.

**Backstories** = durable, architectural ("Check src/Features/Enlistment/ for XP logic")
**Knowledge files** = implementation details ("T2=800 XP, T3=3000 XP")

When systems change, `documentation_maintainer` updates knowledge files.
Agents query these at runtime instead of relying on stale backstory text.

### Knowledge Source Wiring

Knowledge sources are wired in `crew.py` via `_init_knowledge_sources()` and passed to agents:

```python path=null start=null
# In crew.py
from crewai.knowledge.source.text_file_knowledge_source import TextFileKnowledgeSource

self.systems_knowledge = TextFileKnowledgeSource(
    file_paths=["enlisted-systems.md"]  # Relative to knowledge/ folder
)

@agent
def systems_analyst(self) -> Agent:
    return Agent(
        config=self.agents_config["systems_analyst"],
        llm=OPUS_DEEP,
        tools=[...],
        knowledge_sources=[self.systems_knowledge],  # Attach knowledge
    )
```

**Current mapping:**
- `systems_knowledge` → systems_analyst, feature_architect
- `code_knowledge` → code_analyst (systems + error-codes)
- `content_knowledge` → content_analyst, content_author (schemas + balance)
- `balance_knowledge` → balance_analyst (balance + systems)
- `ui_knowledge` → csharp_implementer (ui-systems + systems)
- `planning_knowledge` → documentation_maintainer (systems + content-files)

## ⚡ Quick Commands

**Requires:** Python 3.10-3.13, virtual environment with CrewAI installed.

```powershell path=null start=null
# First-time setup (if .venv doesn't exist)
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -e .

# Activate virtual environment (subsequent uses)
.\.venv\Scripts\Activate.ps1

# Run a crew (example: bug hunting)
crewai run --inputs '{"task_type": "bug_hunt", "context": "E-ENCOUNTER-042 crash"}'

# Run tests
pytest tests/
```

## 🤖 Available Crews

| Crew | Agents | Use Case |
|------|--------|----------|
| `bug_hunting_crew()` | code_analyst → systems_analyst → csharp_implementer → qa_agent | Crash investigation |
|| `planning_crew()` | systems_analyst → feature_architect → documentation_maintainer → code_analyst | Design docs with validation |
| `full_feature_crew()` | All agents | Complete feature development |
| `validation_crew()` | qa_agent → documentation_maintainer | Pre-commit checks |
| `content_creation_crew()` | narrative_designer → qa_agent | JSON events/decisions |
| `documentation_crew()` | documentation_maintainer | Update docs after implementation |

## 🔑 Agent Design Best Practices

From CrewAI research:

1. **Backstory is critical** - It's not flavor text, it's a core system prompt. Include:
   - Domain expertise relevant to the Enlisted mod
   - Key file paths and error code meanings
   - What tools the agent should prefer

2. **Specialized > Generalist** - Monolithic agents fail on complex tasks because:
   - Context window gets polluted
   - Focus becomes diluted
   - Can't switch between thinking modes

3. **Task specificity** - Include:
   - Clear `expected_output` format
   - `context` from prior tasks when needed
   - Specific file paths to focus on

## ⚠️ Sync Requirements

When editing CrewAI configuration, ensure consistency with:

| File | Updates Needed |
|------|----------------|
| `WARP.md` (root) | CrewAI command examples |
| `Tools/AGENT-WORKFLOW.md` | Crew usage table, workflow descriptions |
| `Tools/Validation/validate_content.py` | If adding validation logic |
| `Tools/Validation/sync_event_strings.py` | If adding string sync logic |
| `docs/Tools/validation-tools.md` | Tool documentation |

## 🐛 Debugging

**Enlisted Mod Logs:**
- `<bannerlord>\Modules\Enlisted\Debugging\Session-A_*.log`
- `<bannerlord>\Modules\Enlisted\Debugging\Conflicts-A_*.log`

**Native Game Logs:**
- `C:\ProgramData\Mount and Blade II Bannerlord\logs\crashlist.txt`
- `C:\ProgramData\Mount and Blade II Bannerlord\logs\CrashUploader.*.txt`
- `C:\ProgramData\Mount and Blade II Bannerlord\rgl_log.txt`

**Error Code Meanings:**
- `E-ENCOUNTER-*` = Battle/menu state issues
- `E-SAVELOAD-*` = Save corruption or migration
- `E-QM-*` = Quartermaster UI issues
- `E-CAMPUI-*` = Camp menu display
- `W-DLC-*` = Missing Naval DLC (expected warning)
- `W-REFLECT-*` = Native API drift

## 📋 Adding New Agents/Tasks

1. Define agent in `config/agents.yaml` with detailed backstory
2. Define task in `config/tasks.yaml` with expected_output
3. Add method to `crew.py` with `@agent`/`@task` decorator
4. Add new crew if needed with `@crew` decorator
5. Update this WARP.md and `Tools/AGENT-WORKFLOW.md`
6. Run `pytest tests/` to verify

## ⚙️ Config Patterns (YAML + Python)

CrewAI uses `@CrewBase` with YAML configs. Critical patterns learned 2026-01-05:

### 1. Tools: Python Only, NOT YAML

❌ **Wrong:** Defining tools in both YAML and Python
```yaml path=null start=null
# agents.yaml - DO NOT DO THIS
systems_analyst:
  tools:  # ← REMOVE THIS
    - validate_content_tool
```

✅ **Correct:** Define tools only in Python
```python path=null start=null
# crew.py
@agent
def systems_analyst(self) -> Agent:
    return Agent(
        config=self.agents_config["systems_analyst"],
        tools=[validate_content_tool, run_build_tool],  # ← Python only
    )
```

**Why:** When using `@CrewBase` with `config=`, CrewAI tries to resolve YAML tool names via a `tool_functions` dict that doesn't exist. Tools passed directly in Python work correctly.

### 2. Task Context: Use Method Names

❌ **Wrong:** YAML task name without suffix
```yaml path=null start=null
# tasks.yaml
full_feature_workflow:
  context:
    - analyze_systems  # ← KeyError: method is analyze_systems_task()
```

✅ **Correct:** Match the Python method name
```yaml path=null start=null
# tasks.yaml
full_feature_workflow:
  context:
    - analyze_systems_task  # ← Matches @task method name
```

### 3. Knowledge Sources: Relative Paths

✅ **Correct:** Use relative paths (CrewAI resolves from knowledge/ folder)
```python path=null start=null
self.systems_knowledge = TextFileKnowledgeSource(
    file_paths=["enlisted-systems.md"]  # Relative to knowledge/
)
```

❌ **Wrong:** Using Path objects or absolute paths
```python path=null start=null
# Don't do this:
knowledge_dir = Path(__file__).parent / "knowledge"
file_paths=[str(knowledge_dir / "enlisted-systems.md")]  # Unnecessary
```
