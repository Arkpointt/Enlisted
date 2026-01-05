# Enlisted CrewAI - Master Documentation

**Summary:** Multi-agent AI workflows for Enlisted Bannerlord mod development.  
**Status:** ✅ Implemented  
**Last Updated:** 2026-01-05

---

## 📑 Table of Contents

1. [Overview](#overview)
2. [Setup](#setup)
3. [CLI Commands](#cli-commands)
4. [Agents](#agents)
5. [Crews (Workflows)](#crews-workflows)
6. [Custom Tools](#custom-tools)
7. [Knowledge Sources](#knowledge-sources)
8. [Best Practices](#best-practices)
9. [Warp Integration](#warp-integration)
10. [Architecture Reference](#architecture-reference)

---

## Overview

Specialized AI agents for Enlisted mod development:

| Capability | Agent | Model |
|------------|-------|-------|
| **Systems Analysis** | systems_analyst | Opus 4.5 (10k thinking) |
| **Feature Design** | feature_architect | Opus 4.5 (10k thinking) |
| **Code Analysis** | code_analyst | Sonnet 4.5 (5k thinking) |
| **Planning Docs** | documentation_maintainer | Sonnet 4.5 (5k thinking) |
| **Quality Assurance** | qa_agent | Sonnet 4.5 (3k thinking) |
| **Content Writing** | content_author | Haiku 4.5 (fast) |
| **Schema Validation** | content_analyst | Haiku 4.5 (fast) |
| **Balance Review** | balance_analyst | Haiku 4.5 (fast) |
| **Architecture Advisory** | architecture_advisor | Opus 4.5 (10k thinking) |
| **Implementation** | csharp_implementer | Sonnet 4.5 (execution) |

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

## CLI Commands

```bash
# Activate environment first
.\.venv\Scripts\Activate.ps1

# Validation
enlisted-crew validate                          # Full validation
enlisted-crew validate-file <path>              # Single file

# Content creation
enlisted-crew create-event --theme "..." --tier 1-3
enlisted-crew style-review <path>

# Code review
enlisted-crew code-review src/Features/*.cs

# Planning
enlisted-crew plan -f feature-name -d "description"

# Architecture advisory
enlisted-crew advise -f feature-name -p "problem-description"

# Bug hunting
enlisted-crew hunt-bug -d "description" -e "error-codes"
```

---

## Agents

### Systems Analyst (Coordination)
**Model:** Opus 4.5 with 10k thinking tokens  
**Role:** Complex system integration analysis  
**Tools:** 4 tools (domain context, docs, code search)  
**Configuration:**
- `max_retry_limit=2` (increased from 1)
- `allow_delegation=True` (coordination role)

### Feature Architect (Design)
**Model:** Opus 4.5 with 10k thinking tokens  
**Role:** Multi-file technical design  
**Tools:** 6 tools (feature context, docs, code snippets, validation)  
**Configuration:**
- `max_retry_limit=2`
- `allow_delegation=True`

### Code Analyst (Analysis)
**Model:** Sonnet 4.5 with 5k thinking tokens  
**Role:** C# pattern detection, bug investigation  
**Tools:** 7 tools (code context, code reading, pattern checks, logs)  
**Configuration:**
- `max_retry_limit=2`
- `allow_delegation=False` (execution role)

### Documentation Maintainer (Planning)
**Model:** Sonnet 4.5 with 5k thinking tokens  
**Role:** Planning docs, doc sync, standards  
**Tools:** 9 tools (planning writer, docs, code search)  
**Configuration:**
- Writes to `docs/CrewAI_Plans/` with intelligent versioning
- Updates existing docs after implementation

### QA Agent (Validation)
**Model:** Sonnet 4.5 with 3k thinking tokens  
**Role:** Final validation gate  
**Tools:** 5 tools (validation, localization, build, analysis)  
**Philosophy:** "Spend tokens on validation, not generation"

### Content Author (Generation)
**Model:** Haiku 4.5 (fast)  
**Role:** Event writing following style guide  
**Tools:** 7 tools (content context, style checks, schema validation)

### Content Analyst (Validation)
**Model:** Haiku 4.5 (fast)  
**Role:** Schema validation specialist  
**Tools:** 4 tools (schema tools, event files, docs)

### Balance Analyst (Review)
**Model:** Haiku 4.5 (fast)  
**Role:** Game balance review  
**Tools:** 5 tools (domain context, event files, docs)

### Architecture Advisor (Proactive Improvement)
**Model:** Opus 4.5 with 10k thinking tokens  
**Role:** Suggests architecture improvements based on industry patterns + Bannerlord constraints  
**Tools:** 10 tools (systems knowledge, docs, code analysis, patterns)  
**Configuration:**
- Analyzes existing systems and recommends improvements
- Provides prioritized suggestions (quick wins, technical debt, feature gaps)
- Considers .NET 4.7.2, Harmony, SaveableTypeDefiner constraints
- Industry patterns: state machines, event-driven, data-driven, progression systems

### C# Implementer (Execution)
**Model:** Sonnet 4.5 (no thinking - execution)  
**Role:** Code generation from specs  
**Tools:** 10 tools (build, validation, code style, native API search)

---

## Crews (Workflows)

### validation_crew
**Agents:** qa_agent, documentation_maintainer  
**Use:** Pre-commit validation, CI checks

### planning_crew
**Agents:** systems_analyst → feature_architect → documentation_maintainer → code_analyst  
**Use:** Create design docs WITHOUT implementation  
**Output:** `docs/CrewAI_Plans/feature-name.md` (Status: 📋 Planning)  
**Context Flow:** design receives analyze output, create_doc receives [analyze, design], validate receives create_doc  
**Validation:** code_analyst verifies file paths and event IDs before finalizing

### full_feature_crew
**Agents:** All 9 agents  
**Use:** Complete feature development (design → implement → test → document)  
**Process:** Hierarchical with feature_architect as manager

### bug_hunting_crew (deprecated - use BugHuntingFlow)
**Agents:** code_analyst → systems_analyst → csharp_implementer → qa_agent  
**Use:** Crash investigation  
**Replaced by:** Flow-based `BugHuntingFlow` with better state management

### content_creation_crew
**Agents:** content_author, content_analyst, balance_analyst, qa_agent  
**Use:** Create and validate JSON events

### documentation_crew
**Agents:** documentation_maintainer  
**Use:** Update docs after implementation

### advisory_crew
**Agents:** systems_analyst → architecture_advisor → documentation_maintainer → code_analyst  
**Use:** Analyze systems and suggest architecture improvements  
**Output:** `docs/CrewAI_Plans/[feature]-recommendations.md`  
**Context Flow:** suggest receives analyze, create_doc receives [analyze, suggest], validate receives create_doc  
**Validation:** code_analyst verifies suggestions against existing code

### feature_design_crew
**Agents:** systems_analyst → code_analyst → feature_architect → balance_analyst → code_analyst  
**Use:** Complex architectural design with validation  
**Output:** Technical specifications  
**Context Flow:** design receives [analyze_systems, analyze_code], validate receives design  
**Validation:** Catches hallucinated file paths and event IDs

### feature_implementation_crew
**Agents:** csharp_implementer → content_author → qa_agent  
**Use:** Execute approved feature specs  
**Context Flow:** validate receives [impl_csharp, impl_content]  
**Validation:** Comprehensive build and content validation

### code_review_crew
**Agents:** code_analyst → qa_agent  
**Use:** PR reviews, code audits  
**Context Flow:** Sequential (auto-passes review to validation)

---

## Custom Tools

### Validation Tools
| Tool | Purpose |
|------|---------|
| `validate_content_tool` | Runs `validate_content.py` |
| `sync_localization_tool` | Runs `sync_event_strings.py` |
| `run_build_tool` | Runs `dotnet build` |
| `analyze_validation_report_tool` | Generates prioritized report |

### Style Tools
| Tool | Purpose |
|------|---------|
| `check_writing_style_tool` | Validates against `writing-style-guide.md` |
| `check_tooltip_style_tool` | Checks tooltips (<80 chars) |
| `suggest_style_improvements_tool` | Provides rewrites |
| `read_writing_style_guide_tool` | Loads style guide |

### Schema Tools
| Tool | Purpose |
|------|---------|
| `validate_event_schema_tool` | JSON structure validation |
| `create_event_json_tool` | Generates valid events |
| `read_event_file_tool` | Reads event files |
| `list_event_files_tool` | Lists all events |

### Code Style Tools
| Tool | Purpose |
|------|---------|
| `check_code_style_tool` | Allman braces, _camelCase, XML docs |
| `check_bannerlord_patterns_tool` | TextObject, Hero, Equipment patterns |
| `check_framework_compatibility_tool` | .NET Framework 4.7.2 compatibility |
| `check_csharp_file_tool` | Combined C# analysis |

### Documentation Tools
| Tool | Purpose |
|------|---------|
| `read_doc_tool` | Read project docs |
| `list_docs_tool` | List doc files |
| `search_docs_tool` | Search across docs |
| `read_csharp_tool` | Read C# source |
| `search_csharp_tool` | Search C# codebase |
| `read_csharp_snippet_tool` | Read specific sections |
| `list_feature_files_tool` | List `src/Features/` files |

### Debug Tools
| Tool | Purpose |
|------|---------|
| `read_debug_logs_tool` | Read mod debug logs |
| `search_debug_logs_tool` | Search for error codes |
| `read_native_crash_logs_tool` | Read native game crash logs |
| `search_native_api_tool` | Search Bannerlord API docs |

### Context Loader Tools
| Tool | Purpose |
|------|---------|
| `load_domain_context_tool` | Loads game systems knowledge |
| `load_feature_context_tool` | Loads BLUEPRINT, patterns |
| `load_code_context_tool` | Loads dev guide, APIs |
| `load_content_context_tool` | Loads style guide, schemas |

### Planning Tools
|| Tool | Purpose |
||---------|---------|
|| `write_planning_doc_tool` | Writes to `docs/CrewAI_Plans/` with versioning |

### Verification Tools
|| Tool | Purpose |
||---------|---------|
|| `verify_file_exists_tool` | Validates C# file paths exist before including in plans |
|| `list_json_event_ids_tool` | Lists all event/opportunity IDs from JSON folders |

---

## Knowledge Sources

The `knowledge/` folder contains **dynamic context** that changes with the codebase. Agents query these at runtime instead of relying on stale backstory text.

### Files

| File | Content | Used By |
|------|---------|---------|
| `enlisted-systems.md` | System summaries, key classes | systems_analyst, feature_architect, balance_analyst |
| `error-codes.md` | Error meanings, log locations | code_analyst |
| `json-schemas.md` | JSON field requirements | content_analyst, content_author |
| `balance-values.md` | XP rates, tier thresholds, economy | content_analyst, content_author, balance_analyst |
|| `ui-systems.md` | Menu behaviors, Gauntlet screens | csharp_implementer |
|| `content-files.md` | JSON content inventory, folder locations | documentation_maintainer |

### Knowledge Source Mapping

```python
# In crew.py _init_knowledge_sources()
systems_knowledge → systems_analyst, feature_architect
code_knowledge → code_analyst (systems + error-codes)
content_knowledge → content_analyst, content_author (schemas + balance)
balance_knowledge → balance_analyst (balance + systems)
ui_knowledge → csharp_implementer (ui-systems + systems)
planning_knowledge → documentation_maintainer (systems + content-files)
```

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

### Backstory Best Practices

**Good:**
```yaml
backstory: >
  You understand the 9-tier progression (T1-T9), triple reputation tracks,
  and Company Needs systems. Start by loading domain context, then search
  documentation to understand system integration points.
```

**Bad (Too Prescriptive):**
```yaml
backstory: >
  ALWAYS call load_domain_context_tool FIRST to understand...
  SEARCH EFFICIENCY RULES (CRITICAL):
  - All search results are cached; NEVER re-search...
```

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
│   ├── tools/               # Custom tools
│   ├── crew.py              # Crew orchestration
│   └── main.py              # CLI entry point
├── knowledge/               # Dynamic context (runtime queries)
│   ├── enlisted-systems.md
│   ├── error-codes.md
│   ├── json-schemas.md
│   ├── balance-values.md
│   ├── ui-systems.md
│   └── content-files.md
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
    file_paths=["enlisted-systems.md"]  # Relative to knowledge/
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
