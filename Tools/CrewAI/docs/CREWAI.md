# Enlisted CrewAI - Master Documentation

**Summary:** Four production-ready AI workflows for Enlisted Bannerlord mod development with GPT-5.2 Codex (optimized reasoning levels), single-agent Flow pattern, advanced conditional routing, natural language system analysis, Bannerlord API MCP server, SQLite knowledge base (24 database tools + batch capabilities + auto-sync), automatic prompt caching, Contextual Retrieval Memory System with hybrid search (BM25 + vector + Cohere reranking), and semantic search via ChromaDB vector index for fast code and documentation retrieval.  
**Status:** ✅ Production Ready  
**Architecture:** Single-agent Crews + Pure Python for optimal performance  
**Last Updated:** 2026-01-11

---

## 📑 Quick Start

```bash
# Find and fix bugs  
enlisted-crew hunt-bug -d "bug description" -e "E-XXX-*"
# Reports auto-save to docs/CrewAI_Plans/bug-hunt-{description}-{timestamp}.md

# Build from approved plan
enlisted-crew implement -p "docs/CrewAI_Plans/feature.md"

# Pre-commit validation
enlisted-crew validate

# View execution statistics
enlisted-crew stats

# Analyze systems - Natural language (RECOMMENDED)
enlisted-crew analyze "analyze my gameplay flow"
enlisted-crew analyze "find gaps in player progression"
enlisted-crew analyze "audit player visibility"

# Analyze systems - Explicit system names
enlisted-crew analyze-system "Supply,Morale"
# Analysis auto-saves to: docs/CrewAI_Plans/{system-name}-analysis.md
```

---

## 🚀 How to Run Each Workflow

### Bug Hunting (BugHuntingFlow)
```bash
enlisted-crew hunt-bug -d "Crash when opening camp menu" -e "E-CAMPUI-042"

# With reproduction steps
enlisted-crew hunt-bug -d "Bug description" -e "E-XXX-*" -r "Steps to reproduce"

# With user logs
enlisted-crew hunt-bug -d "Bug description" -l "path/to/user.log"

# Output: docs/CrewAI_Plans/bug-hunt-{description}-{timestamp}.md
```

### Implementation (ImplementationFlow)
```bash
enlisted-crew implement -p "docs/CrewAI_Plans/feature-plan.md"

# Re-run same plan safely (skips completed work)
enlisted-crew implement -p "docs/CrewAI_Plans/feature-plan.md"

# Output: Completed implementation + validation report
```

### Validation (ValidationFlow)
```bash
enlisted-crew validate

# Output: Pass/fail status for content, build, localization
```

### System Analysis (SystemAnalysisFlow)

**Natural Language (Recommended):**
```bash
# Auto-detects relevant systems
enlisted-crew analyze "analyze my gameplay flow"
enlisted-crew analyze "find gaps in player progression"
enlisted-crew analyze "what's wrong with the promotion system"
enlisted-crew analyze "audit player visibility"

# With focus options
enlisted-crew analyze "find bottlenecks" --focus efficiency
enlisted-crew analyze "audit visibility" --focus integration

# Output: docs/CrewAI_Plans/{query-slug}-analysis.md
```

**Explicit System Names:**
```bash
# Single or multiple systems
enlisted-crew analyze-system "Supply"
enlisted-crew analyze-system "Supply,Morale,Reputation"

# All major systems
enlisted-crew analyze-system --all

# Focus options
enlisted-crew analyze-system "Supply" --focus integration    # Integration gaps only
enlisted-crew analyze-system "Supply" --focus efficiency     # Performance only
enlisted-crew analyze-system "Supply" --focus both           # Both (default)

# Subsystem mode (narrower scope)
enlisted-crew analyze-system "CompanyNeedsManager" --subsystem

# Custom output path
enlisted-crew analyze-system "Supply" -o "custom/path.md"

# Output: docs/CrewAI_Plans/{system-name}-analysis.md
```

### Execution Statistics
```bash
# View all workflow stats
enlisted-crew stats

# Filter by workflow
enlisted-crew stats -c BugHuntingFlow
enlisted-crew stats -c ImplementationFlow
enlisted-crew stats -c SystemAnalysisFlow

# Show cost tracking
enlisted-crew stats --costs
```

---

## Architecture

**Single-Agent Flow Pattern:** Each workflow uses CrewAI Flows to coordinate single-agent Crews and pure Python steps for optimal performance.

**Key Features:**
- **State persistence** - Resume workflows on failure
- **Conditional routing** - Skip completed work automatically  
- **Single-agent Crews** - Each step uses one focused agent with minimal tools
- **Pure Python steps** - Deterministic operations (validation, builds) don't need LLMs
- **GPT-5.2 Codex unified** - All agents use same model with optimized reasoning levels

**Performance:**
- 10-15 tool calls per workflow (vs 80-100 with multi-agent)
- 1-2 minute execution (vs 3-5 minutes)
- 80% cost reduction from focused tool usage

---

### 1. Hunt Bug - Find & Fix Issues (BugHuntingFlow)
```bash
enlisted-crew hunt-bug -d "Crash when opening camp menu" -e "E-CAMPUI-042"
# Report auto-saves to: docs/CrewAI_Plans/bug-hunt-crash-when-opening-camp-menu-{timestamp}.md
```

**Workflow (8 Flow steps):**
1. **load_bug_report** - Parse bug report (pure Python)
2. **investigate_bug** - Single-agent Crew investigates root cause
   - Tools: search_debug_logs_tool, search_codebase, search_docs_semantic, lookup_error_code, read_source_section
   - Parses severity, confidence, root cause
3. **route_complexity** - @router decides if systems analysis needed
   - Routes to analyze_systems OR propose_fix based on severity/complexity
4. **analyze_systems** - Single-agent Crew analyzes system dependencies (conditional)
   - Tools: get_system_dependencies, lookup_core_system, get_game_systems
   - Skips for simple bugs
5. **propose_fix** - Single-agent Crew proposes minimal fix
   - Tools: read_source, search_codebase, search_docs_semantic, lookup_api_pattern
   - Guardrails: validate_fix_is_minimal, validate_fix_has_code, validate_fix_explains_root_cause
6. **validate_fix** - Build validation (pure Python)
7. **check_for_issues** - Pattern-based issue detection (pure Python)
8. **generate_report** - Final report (pure Python)

**Architecture:**
- 8 Flow steps: 3 single-agent Crews + 5 pure Python steps
- ~10-15 tool calls per execution
- ~1-2 minute average execution time
- 614 lines of code

**Output:** Bug report + validated fix ready to apply.

---
### 2. Implement - Build from Plan (ImplementationFlow)
```bash
enl isted-crew implement -p "docs/CrewAI_Plans/reputation-integration.md"
```

**Workflow (8 Flow steps):**
1. **load_plan** - Read planning document (pure Python)
2. **verify_existing** - Single-agent Crew checks what exists
   - Tools: parse_plan, lookup_content_ids_batch, verify_file_exists_tool, search_codebase, search_docs_semantic
   - Sets C# status: COMPLETE / PARTIAL / NOT_STARTED
   - Sets Content status: COMPLETE / PARTIAL / NOT_STARTED
3. **implement_csharp** - Single-agent Crew writes C# (conditional)
   - Tools: search_codebase, search_docs_semantic, read_source, write_source, append_to_csproj
   - Skips if C# status is COMPLETE
4. **implement_content** - Single-agent Crew creates JSON (conditional)
   - Tools: write_event, update_localization, lookup_content_id, add_content_item, get_valid_categories, get_style_guide, search_docs_semantic
   - Skips if Content status is COMPLETE
5. **validate_build** - Dotnet build (pure Python)
6. **validate_content_check** - Content validator (pure Python)
7. **sync_docs** - Database sync + implementation log (pure Python)
8. **generate_report** - Final summary (pure Python)

**Architecture:**
- 8 Flow steps: 3 single-agent Crews + 5 pure Python steps
- ~10-15 tool calls per execution
- ~1-2 minute average execution time
- 366 lines of code
- Memory: All Crew steps use `get_memory_config()` with optional advanced mode

**Key Feature:** Smart conditional routing - skips already-completed work. Re-run same plan multiple times safely.

**Output:** Complete implementation + updated documentation.

---
### 3. Validate - Pre-Commit Quality Assurance (ValidationFlow)
```bash
enlisted-crew validate
```

**Workflow (6 Flow steps):**
1. **validate_content_check** - Single-agent Crew validates JSON schemas
   - Tools: validate_content, check_event_format
   - Checks all event/decision/order files
   - Guardrails: validate_content_check_ran
2. **validate_build_check** - Single-agent Crew validates C# build
   - Tools: build, review_code
   - Runs dotnet build, checks for errors
   - Guardrails: validate_build_output_parsed
3. **sync_localization** - Sync XML strings (pure Python)
4. **check_for_issues** - Pattern-based issue detection (pure Python)
5. **route_after_check** - @router (always proceeds to report)
6. **generate_report** - Final validation report (pure Python)

**Architecture:**
- 6 Flow steps: 2 single-agent Crews + 4 pure Python steps
- ~4-6 tool calls per execution
- ~30-45 second average execution time
- 557 lines of code
- Memory: Both Crew steps use `get_memory_config()` with optional advanced mode

**Output:** Validation report with pass/fail status for content, build, and localization.

---

### 4. Analyze System - Technical Integration Analysis (SystemAnalysisFlow)

**Two Entry Points:**

**1. Natural Language Analysis (Recommended):**
```bash
enlisted-crew analyze "analyze my gameplay flow"
enlisted-crew analyze "find gaps in player progression"
enlisted-crew analyze "what's wrong with the promotion system"
enlisted-crew analyze "audit player visibility"
# Auto-detects relevant systems from your query
```

**2. Explicit System Names:**
```bash
enlisted-crew analyze-system "Supply,Morale,Reputation"
# Analysis saves to: docs/CrewAI_Plans/supply-morale-reputation-analysis.md
```

**Database Auto-Sync:** The systems database (`core_systems` table) automatically syncs with the codebase on first database tool access. This ensures code is always truth - if you add/rename/delete a system file, the database updates automatically. See Tools/Validation/sync_systems_db.py for details.

#### Natural Language Flow (analyze command)

**How It Works:**
1. **Query Interpreter Agent** (GPT-5.2 Codex high reasoning)
   - Understands conceptual queries ("gameplay flow" = player progression through mod)
   - Has architectural knowledge: all system names, conceptual mappings
   - **Explores codebase** using tools: `get_game_systems`, `search_codebase`, `search_docs_semantic`
   - Outputs: `SYSTEMS: Enlistment, Orders, Ranks, Reputation, Interface`

2. **Parser** extracts system names from natural language response
   - Validates against known systems (Enlistment, Promotion, Ranks, Reputation, Morale, Supply, Orders, etc.)
   - Falls back to default gameplay flow if parsing fails

3. **SystemAnalysisFlow** kicks off with identified systems

**Example:**
- Query: `"analyze my gameplay flow"`
- Interpreter thinks: "gameplay flow" = player journey, checks Enlistment/Orders/Ranks systems
- Identifies: Enlistment, Orders, Ranks, Reputation, Interface
- Runs deep analysis on those 5 systems

#### Core SystemAnalysisFlow (8 Flow steps)

**Step 1: load_systems** (Pure Python)
- Discovers system files in `src/Features/` (Manager.cs, Behavior.cs, State.cs patterns)
- Sets output path: `docs/CrewAI_Plans/{system-slug}-analysis.md`
- **Triggers lazy database sync** (code → database, happens once per Python process)

**Step 2: analyze_architecture** (Single-agent Crew)
- **Agent:** Systems Analyst (GPT-5.2 Codex high reasoning)
- **Tool Budget:** 8 calls max (forces efficient search)
- **Tools:** search_codebase, search_docs_semantic, read_source, get_game_systems, get_system_dependencies
- **Output:** Architecture overview with components, data flows, integration points
- **Prompt includes:** ARCHITECTURE_PATTERNS context for guided analysis

**Step 3: identify_gaps** (Single-agent Crew - conditional - **De-duplication**)
- **Agent:** Architecture Advisor (GPT-5.2 Codex architect reasoning)
- **Tool Budget:** 12 calls (needs exploration)
- **Tools:** search_codebase, search_docs_semantic, lookup_api_pattern, get_system_dependencies, search_content, get_balance_value, search_balance
- **Domain Knowledge Injected:**
  - Enlisted is a soldier career mod (enlist with lords, follow orders)
  - JSON-driven content (ModuleData/Enlisted/) + XML localization
  - Players see systems via: Camp Hub, Daily Brief, Tooltips, event text
- **Finds:** Missing integrations, unused system values, invisible tracking, silent JSON effects
- **Skips if:** `--focus efficiency` only
- **Parsing:** Multi-pattern matching (emoji markers ❌⚠️, numbered items, bold text)
- **De-duplication:** Normalizes titles (removes markdown/emojis/numbers), checks for near-duplicates before adding
  - Reports duplicates skipped: `[GAPS] Identified 32 integration gaps (18 duplicates skipped)`

**Step 4: critique_analysis** (Single-agent Crew - Generator-Critic Pattern - conditional - **De-duplication**)
- **Agent:** Systems Critic & Game Designer (GPT-5.2 Codex architect reasoning)
- **Tool Budget:** 15 calls (highest - must verify everything)
- **Dual Perspective:**
  - **TYPE A - Implementation Gaps (QA):** Systems work but invisible, JSON effects ignored, features undocumented, stale database data
  - **TYPE B - Design/Utilization Gaps (Game Designer):** Systems underutilized, binary-only usage (not gradient), no narrative arcs, values don't weight content selection
- **Discovery-First Approach:**
  - Phase 1: Call `list_all_core_systems()` → discover all 40 systems from database
  - Phase 1: Call `get_all_tiers()` → tier progression table
  - Phase 1: Call `get_balance_by_category("economy")`, `get_balance_by_category("xp")`
  - Phase 2: For each system - audit visibility, handlers, consistency
  - Phase 3: For each system - ask 4 questions:
    - **Q1: Binary or Gradient?** (value=40 feel different than value=90?)
    - **Q2: Gates or Weights?** (affects WHAT appears or just unlock/block?)
    - **Q3: Narrative Arcs?** (duration tracking, escalating content?)
    - **Q4: Cross-System Influence?** (systems isolated when they could interact?)
- **Output:** Additional gaps missed by initial analysis, both implementation AND design opportunities
- **De-duplication:** Checks against existing gaps from Step 3 (prevents duplicates across generator + critic)
  - Reports: `[CRITIC] Added 12 unique gaps, skipped 8 duplicates (total: 44)`
- **Skips if:** `--focus efficiency` only

**Step 5: analyze_efficiency** (Single-agent Crew - conditional)
- **Agent:** Code Analyst (GPT-5.2 Codex medium reasoning)
- **Tool Budget:** 10 calls
- **Tools:** search_codebase, read_source, verify_file_exists_tool
- **Focus:** Performance hotspots (Update/Tick/Calculate methods), duplicate logic, unnecessary allocations
- **Skips if:** `--focus integration` only

**Step 6: propose_improvements** (Single-agent Crew - **Structured Pydantic Output with Autonomous Fixing**)
- **Agent:** Feature Architect (GPT-5.2 Codex architect reasoning)
- **Tool Budget:** 8 calls
- **Tools:** search_docs_semantic, lookup_api_pattern, verify_file_exists_tool
- **Structured Output:** Uses `output_pydantic=RecommendationsOutput` to force validated structure
  - CrewAI embeds schema into prompt, validates output, retries if malformed
  - **NEW:** Autonomous Pydantic output fixer automatically enhances prompts if validation fails
  - Fallback text parsing if all retries exhausted
- **Output Schema (Pydantic):**
  ```python
  class Recommendation:
      title: str                    # Short name
      description: str              # What to implement
      benefit: str                  # Why it matters
      effort_hours: int             # 1-100 estimate
      impact: ImpactLevel           # high/medium/low
      files: List[str]              # Affected paths
  
  class RecommendationsOutput:
      priority_1_quick_wins: List[Recommendation]  # High Impact, Low Effort (< 4hrs)
      priority_2_major: List[Recommendation]       # High Impact, Medium Effort (4-16hrs)
      priority_3_minor: List[Recommendation]       # Medium Impact, Low Effort
  ```
- **Process:**
  1. Review all gaps + efficiency issues
  2. Estimate impact (high/medium/low) and effort (hours) for each
  3. Prioritize by impact/effort ratio
  4. Group into 3 priority buckets with detailed metadata
- **Context:** Receives architecture summary (2500 chars), top 15 gaps, top 10 efficiency issues

**Step 7: validate_feasibility** (Pure Python)
- Pattern-based compatibility checks
- Warns on: breaking changes, save migrations, API version mismatches

**Step 8: generate_analysis_doc** (Pure Python)
- Builds markdown document with: header, executive summary, architecture, gaps, efficiency issues, recommendations, compatibility warnings, next steps
- Auto-saves to `docs/CrewAI_Plans/{system-name}-analysis.md`

#### Architecture Summary

- **8 Flow steps:** 5 single-agent Crews + 3 pure Python steps
- **Tool calls:** ~30-40 per execution (strict per-step budgets prevent runaway usage)
- **Execution time:** ~3-5 minutes
- **Lines of code:** 942 (system_analysis_flow.py) + 576 (main.py natural language interpreter)

#### CLI Options

**Natural Language:**
```bash
enlisted-crew analyze "your query here"                      # Auto-detect systems
enlisted-crew analyze "audit visibility" --focus integration # Integration only
enlisted-crew analyze "find bottlenecks" --focus efficiency  # Efficiency only
```

**Explicit Systems:**
```bash
enlisted-crew analyze-system "Supply"                        # Single system
enlisted-crew analyze-system "Supply,Morale"                 # Multiple systems
enlisted-crew analyze-system --all                           # All major systems
enlisted-crew analyze-system "Supply" --focus integration    # Integration only
enlisted-crew analyze-system "Supply" --focus efficiency     # Efficiency only
enlisted-crew analyze-system "CompanyNeedsManager" --subsystem  # Subsystem mode
```

#### Tool Budget System

Each step has a strict tool call budget to prevent runaway usage:
- **Step 2 (Architecture):** 8 calls - broad discovery
- **Step 3 (Gaps):** 12 calls - needs exploration
- **Step 4 (Critic):** 15 calls - highest budget, must verify everything
- **Step 5 (Efficiency):** 10 calls - mechanical analysis
- **Step 6 (Recommendations):** 8 calls - synthesis

Budgets print at end of each step: `[BUDGET] Step used 7/8 tool calls`

#### Key Features

1. **Two-Tier Architecture:** Natural language → system identification → deep analysis
2. **Discovery-First Critic:** Database lists ALL systems before code search (prevents blind spots)
3. **Generator-Critic Pattern:** Initial analysis + skeptical second review
4. **Dual Perspective Critic:** Finds BOTH implementation bugs AND design opportunities
5. **Systematic Gap Analysis:** 4-question framework (Binary/Gradient, Gates/Weights, Arcs, Integration)
6. **Strict Tool Budgets:** Forces efficient information gathering
7. **Database-Driven Discovery:** Lazy sync ensures code is always truth
8. **Structured Pydantic Output:** Recommendations use validated schemas (effort, impact, files, priority)
9. **Gap De-duplication:** Normalizes and de-duplicates findings (prevents inflated counts)
10. **Domain Knowledge Injection:** Prevents hallucinations with mod-specific context
11. **Impact/Effort Prioritization:** Recommendations grouped by priority tiers with hour estimates
12. **Memory Disabled:** Analysis workflows use `memory=False` to prevent contamination loops

**Output:** Comprehensive analysis document with architecture overview, integration gaps, design opportunities, efficiency issues, and prioritized recommendations with effort estimates.

---

## Agents (10 total)

Each workflow uses the agents it needs:

| Agent | Role | Model |
|-------|------|-------|
| systems_analyst | Research existing systems | GPT-5.2 Codex (high reasoning) |
| architecture_advisor | Suggest best practices | GPT-5.2 Codex (high reasoning) |
| feature_architect | Design technical specs | GPT-5.2 Codex (`reasoning_effort="high"`) |
| code_analyst | Find bugs, validate plans | GPT-5.2 Codex (`reasoning_effort="medium"`) |
| csharp_implementer | Write C# code | GPT-5.2 Codex (`reasoning_effort="low"`) |
| content_author | Write JSON events | GPT-5.2 Codex (`reasoning_effort="none"`) |
| content_analyst | Validate JSON schemas | GPT-5.2 Codex (`reasoning_effort="none"`) |
| qa_agent | Final validation | GPT-5.2 Codex (`reasoning_effort="medium"`) |
| documentation_maintainer | Write/update docs | GPT-5.2 Codex (`reasoning_effort="medium"`) |
| balance_analyst | Review game balance | GPT-5.2 Codex (`reasoning_effort="none"`) |

---

## Setup

### 1. Create Virtual Environment

Requires **Python 3.10-3.13**.

**Package Update (2026-01-07):** Migrated from deprecated standalone `crewai-tools` package (archived Nov 2025) to modern `crewai[tools]>=0.95.0`. Tools are now integrated in the main CrewAI repository.

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

### Semantic Search (Vector-Based)

| Tool | Purpose |
|------|----------|
| `search_codebase` | Semantic search of `src/` + `Decompile/` C# code using ChromaDB vector index |
| `search_docs_semantic` | Semantic search of `docs/` markdown files using ChromaDB vector index |

**Codebase Search Features:**
- Fast vector-based search (replaces slow grep)
- Smart C# chunking at method/property boundaries
- Rich metadata: file paths, line numbers, class/method names
- Returns top 5 relevant code examples
- Optional path filtering: `search_codebase("morale calc", "src/Features/")`

**Documentation Search Features:**
- Natural language queries → relevant doc sections
- Finds docs even with different terminology ("equipment purchasing" finds quartermaster-system.md)
- Smart markdown chunking at heading boundaries
- Rich metadata: file paths, section titles, line numbers
- Returns top 5 relevant doc sections
- Optional folder filtering: `search_docs_semantic("hero safety", "Reference")`

**Usage:**
```bash
# Index codebase (one-time, ~$0.014)
python -m enlisted_crew.rag.codebase_indexer --index-all
python -m enlisted_crew.rag.codebase_indexer --stats

# Index documentation (one-time, ~$0.08)
python -m enlisted_crew.rag.docs_indexer --index-all
python -m enlisted_crew.rag.docs_indexer --stats
```

**Implementation:**
- Location: `Tools/CrewAI/src/enlisted_crew/rag/`
- Codebase: `codebase_indexer.py` (354 lines), `codebase_rag_tool.py` (118 lines)
- Docs: `docs_indexer.py` (296 lines), search tool in `docs_tools.py`
- Embeddings: `text-embedding-3-large` (3,072 dimensions)
- Indexed: 79 markdown files → 3,184 chunks

### Validation

|| Tool | Purpose |
||------|---------|
|| `validate_content` | Runs `validate_content.py` |
|| `sync_strings` | Runs `sync_event_strings.py` |
|| `sync_systems_db` | Auto-syncs core_systems DB table with codebase (automatic on first DB access) |
|| `build` | Runs `dotnet build` |
|| `analyze_issues` | Generates prioritized report |

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
| `search_docs_semantic` | **Semantic search** across docs/ (vector-based, natural language queries) |
| `read_doc_tool` | Read project docs |
| `list_docs_tool` | List doc files |
| `find_in_docs` | Basic string search across docs (use `search_docs_semantic` instead for better results) |
| `read_source` | Read C# source |
| `read_source_section` | Read specific sections |
| `list_feature_files_tool` | List `src/Features/` files |

### Debug & Native API

| Tool | Purpose |
|------|---------|
| `read_debug_logs_tool` | Read mod debug logs |
| `search_debug_logs_tool` | Search for error codes |
| `read_native_crash_logs_tool` | Read native game crash logs |
| ~~`find_in_native_api`~~ | **[DEPRECATED - Phase 5]** Use MCP Bannerlord API server instead |

### Context Loaders

**[DEPRECATED - Phase 5]** These tools were removed. Use pre-loaded context in Flow task descriptions instead.

| Tool | Purpose | Replacement |
|------|---------|-------------|
| ~~`get_game_systems`~~ | Loads game systems knowledge | Pre-loaded in Flow `state.cached_*` fields |
| ~~`get_architecture`~~ | Loads BLUEPRINT, patterns | Pre-loaded in Flow `state.cached_*` fields |
| ~~`get_dev_reference`~~ | Loads dev guide, APIs | Pre-loaded in Flow `state.cached_*` fields |
| ~~`get_writing_guide`~~ | Loads style guide, schemas | Database tools: `get_style_guide` |

### Planning

**[DEPRECATED - Phase 5]** Planning tools removed. Use Flow state management.

| Tool | Purpose | Replacement |
|------|---------|-------------|
| ~~`save_plan`~~ | Writes to `docs/CrewAI_Plans/` with versioning | `write_doc` tool |
| `load_plan` | Reads planning document | Still active (used in ImplementationFlow) |

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

**24 Total Database Tools:** 14 query tools (including 1 batch tool) + 9 maintenance tools for structured, instant lookups with no LLM cost.

**Query Tools (14 total, including batch capabilities):**

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
| `lookup_content_ids_batch` | Check multiple content IDs at once (batch processing, 1 query vs N queries) |
| `search_content` | Search content by type, category, status |
| `get_valid_categories` | Get all 26 valid event categories |
| `get_valid_severities` | Get all 5 valid severity levels |
| `check_database_health` | Verify database integrity, find orphaned records |

**Maintenance Tools (10 total):**

|| Tool | Purpose |
||------|---------|
|| `add_content_item` | Register new event/decision/order in database |
|| `update_content_item` | Update existing content metadata |
|| `delete_content_item` | Remove content metadata when JSON deleted |
|| `add_balance_value` | Register new balance value |
|| `update_balance_value` | Update existing balance value |
|| `add_error_code` | Register new error code pattern |
|| `add_system_dependency` | Document system dependencies |
|| `sync_content_from_files` | Scan all JSON files and sync to database |
|| `sync_systems_database` | Auto-sync core_systems table with src/Features/ (runs automatically on first DB access) |
|| `record_implementation` | Log implementation completion for tracking |

**Why Custom Tools Over NL2SQL:**
- **Faster:** Pre-written SQL queries execute instantly (no LLM generation step)
- **Cacheable:** Input normalization ensures same queries = cache hits
- **Type-safe:** Validated parameters, structured outputs
- **Domain-specific:** Optimized for Enlisted knowledge base queries
- **Zero token cost:** Normalized lookups hit cache directly, no LLM calls needed

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
5. **Hierarchical process**: Use manager agents for quality control (see below)
6. **Delegation**: Enable for managers only, disable for specialists

### Tool Count Guidelines

| Agent Type | Tool Count | Rationale |
|------------|------------|-----------|
| Coordination | 4-6 tools | Avoid decision paralysis |
| Analysis | 7-10 tools | Needs comprehensive tooling |
| Execution | 3-5 tools | Focused on specific output |

### Agent Configuration Patterns

**Manager Agents (GPT-5.2 Codex - reasoning DISABLED):**
```python
Agent(
    llm=GPT5_ARCHITECT,            # GPT-5.2 Codex with reasoning enabled
    tools=[...],                   # 4-6 tools for coordination (optional)
    max_iter=20,                   # Minimum for manager coordination (test requirement)
    max_retry_limit=3,
    reasoning=False,               # DISABLED: Prevents reasoning loops (2026 best practice)
    allow_delegation=True,         # REQUIRED for managers in hierarchical process
    backstory="""Concise ~50-word backstory focused on delegation and coordination.""",
)
```

**Specialist Agents (GPT-5.2 Codex variable reasoning):**
```python
Agent(
    llm=GPT5_ANALYST,              # Or GPT5_IMPLEMENTER, GPT5_FAST based on task
    tools=[...],                   # 3-10 tools based on role
    max_iter=15,                   # Lower for specialists (focused execution)
    max_retry_limit=3,
    reasoning=True,                # Enable for complex tasks
    max_reasoning_attempts=3,
    allow_delegation=False,        # Specialists don't delegate
)
```

**See "Hierarchical Process with Manager Agents" section below for complete patterns and all current manager/specialist configurations.**

### Advanced Agent Parameters

**Reasoning** (`reasoning=True`, `max_reasoning_attempts=3`):
- DISABLED for all 4 manager agents (prevents reasoning loops)
- Enabled for specialists: systems_analyst, architecture_advisor, feature_architect, code_analyst, csharp_implementer
- Agents reflect on the task before taking action
- Creates a reasoning chain before executing tools or writing output
- **When to use:** Complex planning, multi-step analysis, code generation (specialists only)
- **When NOT to use:** Manager agents, simple validation, file reading, fast execution

**Delegation** (`allow_delegation`):
- **True:** Manager agents only - required for hierarchical process coordination
- **False:** All specialist agents - focused domain execution
- See "Hierarchical Process" section for complete manager/specialist patterns

**Iteration Limits** (`max_iter`, `max_retry_limit`):
- Managers: `max_iter=20` (minimum for proper coordination, per test requirements)
- Specialists: `max_iter=15-25` (based on task complexity)
- `max_retry_limit=3` for all agents (retries on tool/LLM errors)

**Manager Optimization (2026-01-07, updated 2026-01-08):**
- All 4 manager agents now use `reasoning=False` (prevents reasoning loops - 2026 best practice)
- Backstories reduced from ~100 words to ~50 words (85% token savings)
- `max_iter` set to 20 (minimum required for manager coordination per test suite)
- Token limits increased: 8K→12K for comprehensive outputs

**Function Calling LLM Optimization (2026-01-07):**
- All 4 Crews now use `function_calling_llm=GPT5_FUNCTION_CALLING` (PlanningFlow, BugHuntingFlow, ImplementationFlow, ValidationFlow)
- Separates tool parameter extraction from main agent reasoning
- Prevents heavyweight reasoning LLM from being used for every tool call
- Significant token savings on tool-heavy workflows

**Task Description Optimization (2026-01-07):**
- All task descriptions now include "WORKFLOW (execute in order)" with numbered steps
- "TOOL EFFICIENCY RULES" section added to every task with specific tool call limits
- "DO NOT re-search" / "DO NOT re-verify" instructions prevent redundant tool calls
- Pre-loaded context emphasized in PlanningFlow task descriptions (3000 char limit)
- Tool call limits by task type:
  - Investigation tasks: 5-10 calls
  - Systems analysis: 4-6 calls
  - Implementation: 10-15 calls
  - Validation: 1-4 calls
  - Documentation: 4-5 calls

**Agent Backstory Optimization (2026-01-07):**
- All agents with database tools now have "DATABASE TOOLS FIRST" instructions in backstories
- Pattern: "CRITICAL - DATABASE BEFORE CODE/LOGS/CREATING" with numbered steps
- Agents prioritize fast database lookups over slow code/log searches
- Updated agents:
  - `systems_analyst` (planning): get_game_systems → get_system_dependencies → lookup_api_pattern → THEN find_in_code
  - `feature_architect` (planning): lookup_content_id → list_event_ids → get_tier_info → THEN find_in_code
  - `code_analyst` (planning): lookup_content_id → search_content → verify_file_exists_tool → THEN find_in_code
  - `code_analyst` (bug_hunting): lookup_error_code FIRST → THEN search_debug_logs
  - `content_author` (implementation): lookup_content_id → get_valid_categories → get_valid_severities → THEN create

**Database Tool Input Normalization (2026-01-07 - Phase 3):**
- All 14 query tools now normalize inputs for better cache hit rates
- Normalization strategies:
  - Content IDs/categories: `.strip().lower()` + `LOWER()` in SQL (snake_case convention)
  - Error codes: `.strip().upper()` + `UPPER()` in SQL (E-PREFIX-NNN convention)
  - System/API names: `.strip()` + `COLLATE NOCASE` in SQL (PascalCase preserved)
- Result: `"EQUIPMENT"`, `"equipment"`, and `"  equipment  "` all produce identical queries and cache hits
- Zero LLM token cost for normalized lookups (direct cache hits)

**Tool Count Reduction (2026-01-07 - Phase 4):**
- Reduced tool counts per agent to prevent decision paralysis and improve focus
- Previous: 9-14 tools per agent; Now: 4-8 tools per agent
- Removed redundant tools from agents (e.g., `find_in_docs` when `find_in_code` sufficient)
- Removed deprecated tools (`find_in_native_api` - use MCP server instead)
- Each agent now has only the tools essential for their specific role
- Updated agents:
  - `systems_analyst` (impl): 12→9 tools (added batch tool)
  - `csharp_implementer`: 10→5 tools
  - `content_author`: 9→6 tools (re-added add_content_item)
  - `documentation_maintainer` (impl): 7→4 tools
  - `code_analyst` (bug): 14→6 tools
  - `systems_analyst` (bug): 12→5 tools
  - `implementer` (bug): 9→4 tools

**Batch Database Tools (2026-01-07 - Phase 6):**
- Created `lookup_content_ids_batch` for pseudo-parallel content ID lookups
- Uses single SQL query with `WHERE IN` clause instead of N separate queries
- Format: `lookup_content_ids_batch("id1,id2,id3")` - comma-separated string, NOT array
- Added to `systems_analyst` in ImplementationFlow (9 tools total)
- Usage guidance in backstory: Use batch for 3+ IDs, single `lookup_content_id` for 1-2 IDs
- Workaround for CrewAI's lack of native parallel tool calling (confirmed via GitHub Issue #2239)
- Result: 1 database query instead of N queries when verifying multiple content IDs
- Future expansion possible: Batch tools for other high-frequency operations

**Example Usage:**
```python
# Single lookup (1-2 IDs)
result = lookup_content_id("equipment_inspection")

# Batch lookup (3+ IDs) - MUCH faster
result = lookup_content_ids_batch("equipment_inspection,supply_low,morale_check")
# Returns: Found 3/3 with details + Not found list if any missing
```

---

## 🐛 Bug Fixes (2026-01-07)

Three critical bugs were discovered and fixed during PlanningFlow testing:

### Bug 1: Cache Pre-loading Tool Invocation
**Issue:** Lines 465, 473, 481 in `planning_flow.py` called tools incorrectly:
```python
# WRONG - direct call on Tool object
state.cached_game_systems = get_game_systems()
```

**Problem:** Context loader tools are CrewAI `Tool` objects, not plain Python functions. Direct invocation fails.

**Fix:** Use the `.run()` method:
```python
# CORRECT - use Tool.run() method
state.cached_game_systems = get_game_systems.run()
```

**Affected Tools:** `get_game_systems`, `get_architecture`, `get_dev_reference`  
**Status:** ✅ Fixed in commit "Fix: Cache pre-loading in PlanningFlow - use tool.run() not direct call"

### Bug 2: Agent Tool Assignment Conflicts
**Issue:** Context loader tools were BOTH pre-loaded in task descriptions AND assigned to agent tool lists.

**Problem:** Double strategy conflict - agents tried to call tools that are meant to be pre-loaded, not called.
- `systems_analyst` had `get_game_systems` in tools list
- `architecture_advisor` had `get_architecture` in tools list
- `feature_architect` had `get_dev_reference` in tools list
- But these tools were already being pre-loaded and embedded in task descriptions

**Fix:** Removed context loader tools from agent tool lists:
- `systems_analyst`: Removed `get_game_systems` (8→7 tools)
- `architecture_advisor`: Removed `get_architecture` (7→6 tools)
- `feature_architect`: Removed `get_dev_reference` (7→6 tools)
- Updated backstories to reference "PRE-LOADED CONTEXT" instead of calling tools

**Pattern:**
```python
# In flow task description:
task_description = f"""
=== PRE-LOADED CONTEXT (DO NOT RE-FETCH) ===
{state.cached_game_systems[:3000]}
=== END PRE-LOADED CONTEXT ===

WORKFLOW:
1. FIRST: Extract info from PRE-LOADED CONTEXT above
2. Call get_system_dependencies for specific details
3. DO NOT call get_game_systems - it's already above
"""

# Agent backstory:
backstory = """You research using PRE-LOADED CONTEXT first.
Game systems overview is PRE-LOADED in your task - read it there.
DO NOT call get_game_systems."""
```

**Status:** ✅ Fixed in commit "Fix: Remove context loader tools from Planning agents - use pre-loaded cache"

### Bug 3: Excessive Reasoning Performance Impact
**Issue:** High reasoning effort on multiple agents causing 10-30 minute execution times.

**Root Cause:** Manager agents with `reasoning=True` got stuck in reasoning loops, creating elaborate plans instead of executing.

**Fix (2026-01-08):** Disabled reasoning on all 4 manager agents:
- `get_planning_manager()` - `reasoning=False`
- `get_implementation_manager()` - `reasoning=False`
- `get_bug_hunting_manager()` - `reasoning=False`
- `get_validation_manager()` - `reasoning=False`

**Rationale (from 2026 research):**
- CrewAI official docs: "If None (default), the agent will continue refining until it's ready" - can cause infinite loops
- Towards Data Science (Nov 2025): "The hierarchical manager-worker process does not function as documented"
- CrewAI Blog (Dec 2025): "Putting everything in agents when some of it should be code" causes unpredictable behavior
- Manager agents should coordinate, not plan extensively
- Specialists retain `reasoning=True` for complex analysis/implementation

**Status:** ✅ Fixed

---

## 📋 Troubleshooting

### Tool Invocation Errors
**Symptom:** `'Tool' object is not callable`  
**Cause:** Calling CrewAI Tool directly instead of using `.run()` method  
**Fix:** Use `tool_name.run(args)` not `tool_name(args)`

### Agent Calling Pre-loaded Tools
**Symptom:** Agent tries to call `get_game_systems` when context already provided  
**Cause:** Tool is in agent's tool list AND pre-loaded in task description  
**Fix:** Remove tool from agent's tool list, reference "PRE-LOADED CONTEXT" in backstory

### Slow Workflow Execution
**Symptom:** Crew takes 10+ minutes to complete simple tasks  
**Cause:** Manager agents with `reasoning=True`, or excessive tool calls  
**Fix:** Disable reasoning on managers, add strict tool budgets to task descriptions

### How to Run SystemAnalysisFlow
**Symptom:** Confusion about how to invoke system analysis workflow  
**Solution:** Two entry points available:

1. **Natural Language (Recommended):**
```bash
enlisted-crew analyze "analyze my gameplay flow"
enlisted-crew analyze "find gaps in player progression"
```

2. **Explicit System Names:**
```bash
enlisted-crew analyze-system "Supply,Morale,Reputation"
enlisted-crew analyze-system "Supply" --focus integration
enlisted-crew analyze-system --all
```

**Note:** NOT `analyze.py` or `analyze-script` - these don't exist. Use `enlisted-crew analyze` or `enlisted-crew analyze-system` commands.

### Database Not Syncing
**Symptom:** `core_systems` table has outdated/missing systems  
**Solution:** Database auto-syncs on first access - just run any workflow that uses database tools. Or manually run:
```bash
python3 Tools/Validation/sync_systems_db.py
```
**Cause:** High reasoning effort on too many agents  
**Fix:** Review `reasoning_effort` settings - use "high" only for true design/architecture work

### Batch Tool Format Errors
**Symptom:** Batch tool fails with "expected string, got list"  
**Cause:** Passing array instead of comma-separated string  
**Fix:** Use `lookup_content_ids_batch("id1,id2,id3")` NOT `lookup_content_ids_batch(["id1", "id2"])`

---

### Workflow Configuration Patterns

Per [CrewAI documentation](https://docs.crewai.com/), Crews support additional parameters:

```python
Crew(
    agents=[...],
    tasks=[...],
    process=Process.sequential,    # Tasks execute in defined order (recommended)
    verbose=True,                  # Show execution details
    memory=True,                   # Enable crew memory across tasks (short-term, long-term, entity)
    cache=True,                    # Cache tool results (avoids redundant calls)
    planning=True,                 # AgentPlanner creates step-by-step execution plan
    planning_llm=GPT5_PLANNING,    # Use GPT-5 for planning (best quality)
    function_calling_llm=GPT5_FUNCTION_CALLING,  # Lightweight LLM for tool parameter extraction
    embedder=EMBEDDER_CONFIG,      # text-embedding-3-large for superior knowledge retrieval
)
```

**Function Calling LLM (`function_calling_llm`):**
- Per [CrewAI docs](https://docs.crewai.com/core-concepts/Collaboration/), this manages "language models for executing tasks and tools"
- Crew-level setting applies to ALL agents in the crew for tool calls
- Individual agents can override with their own `function_calling_llm` parameter
- Without this, agents use their heavyweight reasoning LLM for every tool parameter extraction
- Significant token savings on tool-heavy workflows

```python
# LLM definition (in flow files)
GPT5_FUNCTION_CALLING = _get_env("ENLISTED_LLM_FUNCTION_CALLING", "gpt-5.2-codex")
```

#### Sequential Process with Flow Coordination

All 4 Enlisted flows use **sequential process** with Flow-based coordination.

**Why Sequential + Flow:**
- **Flow handles coordination** - Deterministic task routing, no LLM overhead
- **Predictable execution** - Tasks execute in explicit order, easier debugging
- **Better caching** - Static prompts improve cache hit rates
- **Quality preserved** - Guardrails + `check_for_issues()` Flow step provide validation

**Current Pattern:**
```python
from crewai import Crew, Process, Task
from crewai.flow.flow import Flow, listen, start

# Flow provides deterministic coordination
class ImplementationFlow(Flow):
    @start()
    def load_plan(self) -> ImplementationState:
        # Deterministic setup - no LLM needed
        ...
    
    @listen(load_plan)
    def run_implementation_crew(self, state: ImplementationState):
        # Sequential crew - tasks execute in order
        crew = Crew(
            agents=[analyst, implementer, qa, docs],
            tasks=[verify_task, csharp_task, validate_task, docs_task],
            # manager_agent REMOVED - Flow handles coordination
            process=Process.sequential,  # Deterministic, cacheable
            verbose=True,
            **get_memory_config(),
            cache=True,
            planning=True,
            planning_llm=GPT5_PLANNING,
            function_calling_llm=GPT5_FUNCTION_CALLING,
        )
        return crew.kickoff()
    
    @listen(run_implementation_crew)
    def check_for_issues(self, state: ImplementationState):
        # Quality review - pattern detection, not LLM coordination
        # This IS the manager's review function, now deterministic
        ...
```

**Flow-Based Quality Control:**
The `check_for_issues()` step in each Flow provides quality validation:
- Pattern detection for hallucinated files/APIs
- Architecture violation detection
- Scope creep detection
- Routes to auto-fix or logs for review

**Current Sequential Crews:**
- `ImplementationFlow` → verify_task → csharp_task → content_task → validate_task → docs_task
- `BugHuntingFlow` → investigate_task → systems_task → fix_task → validate_task
- `ValidationFlow` → content_task → build_task → report_task

**Execution Flow:**
```
Flow.start() → load state
           → run_crew() → Task A (sequential)
                        → Task B (sequential, uses A's output)
                        → Task C (sequential, uses A+B output)
           → check_for_issues() → pattern-based validation
           → generate_report()
```

#### Prompt Caching Optimization

All LLM definitions use OpenAI's extended prompt caching for 60%+ cost reduction on repeated workflows.

**Configuration:**
```python
GPT5_ARCHITECT = LLM(
    model="gpt-5.2-codex",
    max_completion_tokens=16000,
    reasoning_effort="medium",
    prompt_cache_retention="24h",  # Cache prompts for 24 hours
)
```

**How It Works:**
- Prompts ≥1024 tokens are automatically cached by OpenAI
- Cached content costs 90% less ($0.175/1M vs $1.75/1M)
- Cache persists for 24 hours across workflow runs
- Expected cache hit rate: 50-70% on repeated/similar workflows

**Static-First Prompt Structure:**
Task descriptions use static templates (1500+ tokens) followed by dynamic content (100-200 tokens):

```python
from enlisted_crew.prompts import ARCHITECTURE_PATTERNS, RESEARCH_WORKFLOW

# Static content gets cached (1500+ tokens)
task = Task(
    description=f"""{ARCHITECTURE_PATTERNS}
{RESEARCH_WORKFLOW}

=== YOUR TASK ===
Feature: {feature_name}  # Only this changes between runs
""",
    agent=analyst,
)
```

**Prompt Templates:**
Static templates in `enlisted_crew/prompts/templates.py`:
- `ARCHITECTURE_PATTERNS` - Code style, API patterns, file organization
- `RESEARCH_WORKFLOW` - Research methodology steps
- `DESIGN_WORKFLOW` - Design specification process
- `IMPLEMENTATION_WORKFLOW` - Implementation steps
- `VALIDATION_WORKFLOW` - Validation procedures
- `BUG_INVESTIGATION_WORKFLOW` - Debugging methodology
- `CODE_STYLE_RULES` - C# style requirements with examples
- `TOOL_EFFICIENCY_RULES` - Tool usage optimization

**Cost Impact:**
- 50-70% of prompt content cached on repeated workflows
- 60%+ reduction in input token costs
- Example: Workflow costs reduced by 60%+ with effective caching

**Dependencies:**

**chromadb:** Version 1.4.0
- Required by `langchain-chroma` package
- Pip may show warning "crewai 1.8.0 requires chromadb~=1.1.0" - this is a false positive
- Both RAG tool and memory system work correctly with 1.4.0

**Alternative Process:**
To use hierarchical process instead of sequential:
1. Change `process=Process.sequential` → `process=Process.hierarchical`
2. Add `manager_agent=get_*_manager()` to Crew definitions
3. Manager agent functions are available in flow files

#### Task Guardrails for Output Validation

**Status:** ✅ Implemented on all critical tasks (2025-01-07)

Guardrails validate task outputs **before proceeding** to the next task. Failed guardrails trigger automatic retries (up to `guardrail_max_retries`).

**Pattern:**
```python
from typing import Tuple, Any
from crewai.tasks.task_output import TaskOutput

def validate_has_code(output: TaskOutput) -> Tuple[bool, Any]:
    """Ensure output includes actual code.
    
    Returns (True, output) if valid, (False, error_message) if invalid.
    """
    content = str(output.raw)
    if "```" not in content:
        return (False, "Output must include code in triple-backtick blocks.")
    return (True, output)

task = Task(
    description="Implement feature...",
    expected_output="Implementation with code",
    agent=get_csharp_implementer(),
    guardrails=[validate_has_code],
    guardrail_max_retries=2,  # Retry up to 2 times on failure
)
```

**Guardrail Benefits:**
- **Early error detection** - Catches issues before they propagate
- **Automatic retry** - Agent gets specific feedback and re-tries
- **Better error messages** - Clear validation failure reasons
- **Reduced manual review** - Common mistakes caught automatically

**Implemented Guardrails (10 total):**

**ImplementationFlow (csharp_task, content_task):**
- `validate_csharp_braces` - Balanced `{}` in C# code
- `validate_csharp_has_code` - Implementation includes actual code
- `validate_json_syntax` - JSON blocks are parseable
- `validate_content_ids_format` - snake_case content IDs (not camelCase)

**BugHuntingFlow (fix_task):**
- `validate_fix_is_minimal` - Prevents "complete rewrite" proposals
- `validate_fix_has_code` - Fix includes actual code
- `validate_fix_explains_root_cause` - Requires root cause explanation

**ValidationFlow (content_task, build_task, report_task):**
- `validate_content_check_ran` - Validation actually executed
- `validate_build_output_parsed` - Build output was processed
- `validate_report_has_status` - Report has clear pass/fail status

**Guardrail Guidelines:**

1. **Keep guardrails fast** - Simple checks only (no LLM calls)
2. **Return clear error messages** - Agent needs specific guidance
3. **Use `guardrail_max_retries=2`** - Balance between fixing and moving on
4. **Check obvious patterns** - Missing code blocks, unbalanced braces, placeholder text
5. **Validate structure** - Required sections, expected keywords

**When to Add Guardrails:**
- ❌ Simple file reading/listing - No validation needed
- ✅ Code generation - Check syntax, structure, completeness
- ✅ Content creation - Check IDs, formatting, required fields
- ✅ Planning/documentation - Check required sections, no placeholders
- ✅ Bug fixes - Check minimal scope, includes code, explains root cause

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
planning_llm=GPT5_PLANNING  # Uses GPT-5 for best planning quality
```

**Why GPT-5.2 Codex with low reasoning for planning?**
- Structured planning prompts don't need deep reasoning
- `reasoning_effort="low"` provides fast, reliable planning
- Auto-switches to instant mode when prompts are clear
- Consistent quality across all planning operations

**All internal crews** (within ImplementationFlow, BugHuntingFlow, ValidationFlow) use `planning_llm=GPT5_PLANNING` (GPT-5.2 Codex with `reasoning_effort="low"`).

#### Memory Configuration

When `memory=True` is enabled, **all three memory types** are automatically active:

**1. Short-Term Memory (Execution Context)**
- Stores recent interactions during the current workflow run
- Uses ChromaDB with RAG to recall relevant information
- Uses embeddings (8,192 token limit per query)
- Cleared between workflow runs

**2. Long-Term Memory (Cross-Run Learning)**
- Preserves insights and learnings from past executions
- Uses SQLite (no token limits)
- Agents remember patterns like:
  - "Tier-aware events need variant ID suffixes (T1/T2/T3)"
  - "Always validate JSON before syncing to XML"
  - "Use GiveGoldAction for gold changes, not direct Hero.Gold manipulation"
- Persists across multiple runs

**3. Entity Memory (Relationship Tracking)**
- Tracks entities encountered during tasks (systems, classes, concepts, factions)
- Uses ChromaDB with RAG (8,192 token limit)
- Example entities: `EnlistmentBehavior`, `ContentOrchestrator`, `Hero.MainHero`, `Tier System`

#### Memory Configuration

**CrewAI 1.8.0 Memory System:**

Use `get_memory_config()` for memory with optional advanced features:

```python
from enlisted_crew.memory_config import get_memory_config

crew = Crew(
    agents=[...],
    tasks=[...],
    **get_memory_config(),  # Basic: memory=True + embedder config
    cache=True,
    planning=True,
)
```

**Basic mode (default):**
- `memory=True` - Enables short-term, long-term, and entity memory
- `embedder` - Uses `text-embedding-3-large` for best semantic quality
- CrewAI's built-in memory (ChromaDB + SQLite) - simple and reliable

**Advanced mode (Anthropic Contextual Retrieval):**
```python
**get_memory_config(use_advanced=True)  # ✅ BM25+Cohere hybrid search
```
- Custom `ContextualRAGStorage` with BM25 keyword search
- Reciprocal Rank Fusion for vector+BM25 combination
- Cohere reranking (rerank-v3.5) with 10s timeout
- FILCO post-retrieval filtering
- +67% better retrieval than basic RAG (Anthropic research)
- **Status:** ✅ Fixed for CrewAI 1.8.0 compatibility (2026-01-09)

---

## Advanced Memory Architecture (Optional)

Based on Anthropic's Contextual Retrieval research, now compatible with CrewAI 1.8.0.

**Problem:** Short-Term and Entity memory use embeddings with an 8,192 token limit. Large agent outputs (9,000+ tokens) crash the embedding API. Simple truncation loses 24-67% of retrieval quality.

**Architecture (based on Anthropic's Contextual Retrieval research):**
```
Agent Output (9,000+ tokens)
        │
        ▼
┌─────────────────────────────────┐
│  1. CHUNKING                    │
│  - Semantic boundaries          │
│  - 1000 tokens per chunk        │
│  - 15% overlap between chunks   │
└─────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────┐
│  2. CONTEXTUALIZATION (GPT-5.2 Codex) │
│  - LLM generates context prefix │
│  - "This chunk is from [flow]..."│
│  - ~$0.001 per chunk            │
└─────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────┐
│  3. DUAL STORAGE                │
│  - Vector DB (ChromaDB)         │
│  - SQL table (contextual_memory)│
└─────────────────────────────────┘
```

**Retrieval Pipeline (Hybrid Search + Reranking + FILCO):**
```
Query → Vector Search (top 20)
      → BM25 Search (top 20)
      → RRF Fusion (combines rankings)
      → Cohere Rerank (rerank-v3.5)
      → FILCO Filter (remove low-utility spans)
      → Return filtered results
```

**Components (in memory_config.py, disabled by default):**
- `chunk_content()` - Semantic chunking at paragraph boundaries
- `contextualize_chunk()` - GPT-5.2 Codex generates context prefix (reasoning=none for speed)
- `store_chunk_in_sql()` - Stores in contextual_memory table for BM25 indexing
- `BM25Index` - In-memory keyword index, rebuilds automatically on new chunks
- `reciprocal_rank_fusion()` - Combines vector + BM25 results (k=60)
- `rerank_results()` - Cohere rerank API for final refinement
- `filco_filter()` - Post-retrieval filtering: removes low-utility spans, noise patterns, duplicates
- `ContextualRAGStorage` - Custom storage with overridden `save()` and `search()` methods

**Research-Based Improvement:**
- Simple truncation: baseline (loses data)
- Chunking only: +24% better retrieval
- Contextual + BM25: +49% better retrieval
- Contextual + BM25 + Reranking: +67% better retrieval
- Contextual + BM25 + Reranking + FILCO: 67%+ with reduced noise ✅

**Cost per multi-flow session (advanced mode):**
- Chunking: $0 (local)
- Contextualization: ~$0.003 (GPT-5.2 Codex, ~50 chunks)
- Embeddings: ~$0.005 (text-embedding-3-large)
- BM25: $0 (local)
- Reranking: ~$0.002 (Cohere rerank-v3.5, 10 searches)
- FILCO: $0 (local filtering)
- **Total: ~$0.01 per session**

**Configuration Options (in memory_config.py):**
```python
# Reranking (only used in advanced mode)
RERAN_ENABLED = True  # Set False to skip reranking
RERAN_MODEL = "rerank-v3.5"  # Or rerank-v4.0-fast/pro

# FILCO post-retrieval filtering (only used in advanced mode)
FILCO_ENABLED = True  # Set False to disable FILCO
FILCO_RELEVANCE_THRESHOLD = 0.35  # Minimum score to keep
FILCO_MIN_RESULTS = 3  # Always keep at least this many
```

**Required API Keys:**
- `OPENAI_API_KEY` - For embeddings and contextualization
- `COHERE_API_KEY` - For reranking (optional, falls back gracefully)

**Console output when advanced mode activated:**
```
[MEMORY] WARNING: Advanced memory mode enabled (experimental).
[MEMORY] Using custom ContextualRAGStorage with hybrid search.
[MEMORY] Content exceeds limit (9390 tokens), applying contextual chunking...
[MEMORY] Split into 12 chunks
[MEMORY] Contextual chunking complete: 12 chunks stored
[MEMORY] BM25 index rebuilt with 12 chunks
[MEMORY] Hybrid search: 8 vector + 10 BM25 → 15 fused
[MEMORY] Reranked 15 → 10 results (top score: 0.891)
[MEMORY] FILCO: filtered 2 low-utility results (10 → 8)
```

#### Embedder Configuration

```python
# From memory_config.py
EMBEDDER_CONFIG = {
    "provider": "openai",
    "config": {
        "model": "text-embedding-3-large",  # 8,192 token limit
    }
}
```

**Why text-embedding-3-large:**
- 3,072 dimensions for best semantic quality
- Superior retrieval accuracy for technical content
- Same 8,192 token limit as small (truncating storage handles this)
- Cost: ~$0.13 per 1M tokens

#### Memory Storage Location

**Workspace-Relative (Cross-Platform):**
```
Tools/CrewAI/memory/
├── knowledge/               # ChromaDB embeddings (chunked knowledge files)
├── short_term_memory/       # Current execution context (RAG-based)
├── long_term_memory/        # Insights from past runs (ChromaDB)
├── long_term_memory_storage.db  # SQLite database for task results
└── entities/                # Entity relationships (RAG-based)
```

**Why In Workspace:**
- Synced via Git for cross-platform consistency
- Windows and Linux share the same AI learning
- No per-platform memory divergence

#### Resetting Memory After Refactors

**When to reset:**
- After renaming systems/files
- After major architectural refactors
- After deleting/deprecating systems
- If agents reference outdated patterns

**Using PowerShell script:**
```powershell
cd Tools/CrewAI
.\reset-memory.ps1           # Clear ALL memory
.\reset-memory.ps1 -Long     # Clear only long-term memory
.\reset-memory.ps1 -Help     # Show all options
```

**Using Python:**
```python
from enlisted_crew.memory_config import clear_memory

clear_memory()                    # Clear all
clear_memory(['long'])            # Clear only long-term
clear_memory(['short', 'entity']) # Clear short-term and entity
```

**Note:** Agents re-learn patterns within 1-2 runs after a reset.

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

### After Bannerlord Patch (Game Updates)

When Bannerlord updates and you decompile a new version:

```powershell
cd Tools/CrewAI
.\update_after_patch.ps1
```

This script:
1. ✅ Checks decompile folder exists (`Decompile/` in workspace root)
2. ✅ Rebuilds MCP index with new API signatures
3. ✅ Runs all tests to verify everything works

**Automatic Staleness Detection:**
- `test_all.py` warns if MCP index is older than decompiled code
- `EnlistedCrew` warns on initialization if indexes are stale
- No need to remember - the system will tell you!

**When to Run:**
| Update Type | Action |
|-------------|--------|
| Hotfix (bug fix) | Usually skip - no API changes |
| Minor patch (e.x.x) | Run update script |
| Major patch (x.x.0) | Run update script + review failures |

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

**"This model's maximum context length is 8192 tokens"**
This is an embedding token limit error in Short-Term or Entity memory. The `memory_config.py` module should prevent this automatically. If you see this error:
1. Ensure you're using `**get_memory_config()` in Crew definitions
2. If using custom Crew config, add `short_term_memory` and `entity_memory` from `memory_config.py`
3. As a fallback, set `memory=False` to disable memory entirely

**Memory hangs at "Retrieving..." forever**
~~This was caused by the custom `ContextualRAGStorage` being incompatible with CrewAI 1.8.0.~~ **Fixed 2026-01-09.**

If you still experience hangs:
1. Update to latest code: `git pull origin development`
2. Test basic mode: `**get_memory_config()` (built-in memory)
3. Test advanced mode: `**get_memory_config(use_advanced=True)` (hybrid search)
4. If issues persist, reset memory: `crewai reset-memories -a`
5. Check for stale ChromaDB data in `/tmp/crewai_*` or `~/.config/CrewAI/`

**"Agents reference outdated patterns after refactor"**
Reset memory to clear stale learnings:
```powershell
cd Tools/CrewAI
.\reset-memory.ps1
```

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

| Reasoning Level | Speed | Use Case | Model |
|-----------------|-------|----------|-------|
| `high` | ~8s | Architecture, complex decisions, deep system analysis | GPT-5.2 Codex |
| `medium` | ~5s | Bug analysis, QA validation, code review, doc sync | GPT-5.2 Codex |
| `low` | ~5s | Implementation from specs, documentation, planning | GPT-5.2 Codex |
| `none` | ~1s | Schema validation, formatting, simple content | GPT-5.2 Codex |

**Philosophy:** "Use reasoning where it matters: architecture & analysis. Save on execution & validation."

**Key Benefits:**
- **Single model (GPT-5.2 Codex)** - No context/capability mismatches between agents
- **Auto mode-switching** - Automatically uses instant mode when appropriate
- **Optimized costs** - Only pay for reasoning when needed via `reasoning_effort` parameter
- **200K context** - Consistent across all agents

### Memory & Knowledge Configuration

All three workflows (implement, bug-hunt, validate) have crew memory enabled for context sharing between tasks.

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
# Test BugHuntingFlow
enlisted-crew hunt-bug -d "Test bug description" -e "E-TEST-001"

# Test ImplementationFlow (requires plan file)
enlisted-crew implement -p "docs/CrewAI_Plans/test-feature.md"

# Test ValidationFlow
enlisted-crew validate
```

### What to Validate After Testing

1. **Generated Files:**
   - Verify C# code quality and style compliance
   - Review JSON content for schema adherence
   - Check implementation reports

2. **Database Updates:**
   - Run `sqlite3 enlisted_knowledge.db ".tables"` to verify tables exist
   - Check content_metadata was updated
   - Verify implementation_history logged the run

3. **Documentation Sync:**
   - Confirm feature docs were updated
   - Check INDEX.md references new features
   - Verify implementation history logged

4. **Code Changes:**
   - Run `git diff` to review all modifications
   - Verify .csproj was updated with new files
   - Check enlisted_strings.xml for new localization

### Performance Baselines

After initial testing, establish baselines for each flow:
- **ImplementationFlow:** Target 8.5+ quality, <120s execution (down from 300s pre-refactor)
- **BugHuntingFlow:** Target 8.0+ quality, <80s execution (down from 200s pre-refactor)
- **ValidationFlow:** Target 8.0+ quality, <60s execution

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
enlisted-crew stats -c ImplementationFlow
enlisted-crew stats -c BugHuntingFlow
enlisted-crew stats -c ValidationFlow

# View cost tracking
enlisted-crew stats --costs
enlisted-crew stats -c ImplementationFlow --costs

# Performance trend analysis (requires 15+ runs)
enlisted-crew stats --trends
enlisted-crew stats -c ImplementationFlow --trends
```

**Example Output:**
```
==================================================================
CREWAI EXECUTION STATISTICS
==================================================================

Crew: ImplementationFlow
  Total Runs: 12
  Avg Duration: 98.45s (1.6m)
  Min Duration: 85.20s
  Max Duration: 115.30s

Crew: BugHuntingFlow
  Total Runs: 8
  Avg Duration: 72.15s (1.2m)
  Min Duration: 65.10s
  Max Duration: 85.40s
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

4. **Optimize Costs**
   - Track token usage per workflow
   - Identify expensive operations
   - Compare model costs across runs
   - Budget API spending accurately

5. **Performance Trends** (NEW)
   - Automatic detection of performance degradation
   - Compare recent runs vs historical average
   - Time-range filtering for targeted analysis
   - Track improvements from optimization work

### Time-Range Filtering and Trend Analysis

**Python API for custom queries:**

```python
from enlisted_crew.monitoring import (
    get_execution_history,
    get_performance_trends,
    print_trend_report
)

# Get executions from a date range
history = get_execution_history(
    crew_name="PlanningFlow",
    start_date="2026-01-01",
    end_date="2026-01-31",
    limit=50
)

# Analyze performance trends
trends = get_performance_trends(
    crew_name="PlanningFlow",
    window_size=10  # Compare last 10 runs to historical
)

# Print formatted trend report
print_trend_report(crew_name="PlanningFlow", window_size=10)
```

**Trend Analysis Output:**
```
======================================================================
PERFORMANCE TREND ANALYSIS (Last 10 runs vs Historical)
======================================================================

Crew: PlanningFlow
  Total Runs: 47
  Historical Avg: 142.35s (2.4m)
  Recent Avg: 128.50s (2.1m)
  Change: -9.7%
  Trend: [+] IMPROVING

Crew: ImplementationFlow
  Total Runs: 31
  Historical Avg: 284.20s (4.7m)
  Recent Avg: 312.50s (5.2m)
  Change: +10.0%
  Trend: [-] DEGRADING

======================================================================
```

**Trend Detection Rules:**
- `[+] IMPROVING`: Recent runs >5% faster than historical average
- `[-] DEGRADING`: Recent runs >5% slower than historical average
- `[=] STABLE`: Within ±5% of historical average
- Requires at least 15 total runs (10 recent + 5 historical minimum)

**Use Cases:**
- **After optimization:** Validate that changes actually improved performance
- **Continuous monitoring:** Detect gradual performance degradation
- **A/B testing:** Compare performance before/after config changes
- **Cost validation:** Confirm optimization plan achieved 40-50% reduction

---

## Execution Hooks - Safety & Cost Control

CrewAI Execution Hooks provide fine-grained control during agent execution. Located in `hooks.py`, these hooks run automatically before and after every LLM call and tool execution.

### What We've Implemented

**1. LLM Call Hooks** (`@after_llm_call`)
- **Cost Tracking** - Automatic token usage and cost calculation for every LLM call
- **Cache Hit Tracking** - Monitors OpenAI prompt caching effectiveness (Phase 3 optimization)
- **Database Logging** - Persist costs and cache metrics to `llm_costs` table for analysis
- **Real-time Display** - Console output shows cost per call with cache hit percentages
- **Running Totals** - Track cumulative costs, cache savings, and hit rates for current workflow run

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

**After LLM Call (Cost Tracking with Cache Metrics):**
```
      [COST] gpt-5.2-codex: 2000 in (1000 cached, 50%) + 500 out = $0.0075 (saved $0.0025)
      [COST] gpt-5.2-codex: 2500 in (2000 cached, 80%) + 600 out = $0.0072 (saved $0.0050)
      [COST] gpt-5.2-codex: 1500 in (1500 cached, 100%) + 400 out = $0.0040 (saved $0.0037)
```

**End of Run Summary (with Cache Analytics):**
```
======================================================================
RUN COST SUMMARY
======================================================================
Total LLM Calls: 47
Input Tokens: 125,430
  - Cached: 85,200 (68% hit rate)
  - Uncached: 40,230
Output Tokens: 38,210
Actual Cost: $0.45
Cache Savings: $0.32 (42% reduction)
Full Cost (no cache): $0.77
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
- **Cache Visibility** - Monitor prompt caching effectiveness with hit rates and savings
- **Optimization** - Identify which agents/tasks consume most tokens
- **Budgeting** - Track cumulative spending per workflow run
- **Model Comparison** - Compare GPT-5.2 Codex vs GPT-5-mini vs GPT-5 costs
- **Historical Analysis** - Query `llm_costs` table for cost trends and cache performance over time
- **Phase 3 Validation** - Verify 50-70% cache hit rate target from optimization plan

**Cost Estimates (per 1M tokens):**
- `gpt-5.2-codex`: $1.75 input, $14.00 output (updated January 2026)
- `gpt-5.2-codex` (cached): $0.175 input (90% discount, 24-hour retention)
- `gpt-5`: $2.00 input, $8.00 output
- `gpt-5-mini`: $0.10 input, $0.40 output
- `gpt-5-nano`: $0.05 input, $0.20 output

**Cost Optimization (Phase 3 Complete):**
- ✅ **Prompt caching**: Static-first task descriptions with 24-hour retention
- ✅ **Cache tracking**: Real-time monitoring of cache hit rates and cost savings
- ✅ **Semantic codebase RAG**: Vector search replaces expensive grep operations
- 🎯 **Target**: 50-70% cache hit rate, 60%+ input token cost reduction
- 📊 **Monitoring**: `enlisted-crew stats --costs` shows cache analytics
- See `docs/OPENAI_COST_OPTIMIZATION_PLAN.md` for complete implementation details

### Database Schema

**Cost tracking with cache metrics:**
```sql
CREATE TABLE llm_costs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp TEXT NOT NULL,
    model TEXT NOT NULL,
    input_tokens INTEGER NOT NULL,
    output_tokens INTEGER NOT NULL,
    cached_tokens INTEGER DEFAULT 0,          -- Phase 3: Cache hit tracking
    cost_usd REAL NOT NULL,
    cache_savings_usd REAL DEFAULT 0.0        -- Phase 3: Savings from cache
);
```

**Example cost analysis queries:**
```sql
-- Total cost by model with cache metrics
SELECT 
    model,
    COUNT(*) as calls,
    SUM(input_tokens) as total_input,
    SUM(cached_tokens) as total_cached,
    ROUND(100.0 * SUM(cached_tokens) / SUM(input_tokens), 1) as cache_hit_pct,
    SUM(cost_usd) as actual_cost,
    SUM(cache_savings_usd) as saved_from_cache
FROM llm_costs
GROUP BY model
ORDER BY actual_cost DESC;

-- Daily cost breakdown with cache savings
SELECT 
    DATE(timestamp) as date,
    COUNT(*) as calls,
    SUM(cost_usd) as daily_cost,
    SUM(cache_savings_usd) as daily_savings,
    ROUND(100.0 * SUM(cached_tokens) / SUM(input_tokens), 1) as cache_hit_pct
FROM llm_costs
GROUP BY DATE(timestamp)
ORDER BY date DESC;

-- Most expensive LLM calls (with cache info)
SELECT 
    timestamp,
    model,
    input_tokens,
    cached_tokens,
    ROUND(100.0 * cached_tokens / input_tokens, 0) as cache_pct,
    output_tokens,
    cost_usd,
    cache_savings_usd
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

## Autonomous Pydantic Output Fixer

**Status:** ✅ Implemented (2026-01-11)

Automatic detection and fixing of Pydantic output validation failures in CrewAI tasks.

### Problem

When using CrewAI's `output_pydantic` parameter with tool-enabled agents and complex prompts, the LLM often produces text instead of valid JSON, causing Pydantic validation to fail. This happens because:

1. **Tool usage distracts the agent** - Agents with tools focus on using them, then forget to output structured JSON
2. **Complex context** - Large prompts with many instructions dilute the JSON format requirement
3. **Implicit schema** - CrewAI embeds the Pydantic schema but doesn't show a concrete example

### Solution

The `pydantic_output_fixer` module (`src/enlisted_crew/hooks/pydantic_output_fixer.py`) provides **automatic detection and retry** with enhanced prompts.

### Usage

**Option 1: Automatic Retry Wrapper (Recommended)**

Wrap your crew execution to automatically retry on failure:

```python
from enlisted_crew.hooks import with_pydantic_retry

task = Task(
    description="Generate recommendations...",
    output_pydantic=RecommendationsOutput,
    agent=architect,
)

crew = Crew(agents=[architect], tasks=[task])

# Automatic retry with enhanced prompt if validation fails
result = with_pydantic_retry(crew, task, max_retries=1)
```

**Option 2: Proactive Enhancement**

Pre-enhance the task description before creating it:

```python
from enlisted_crew.hooks import create_pydantic_safe_task

# Automatically adds JSON schema example to the description
task = create_pydantic_safe_task(
    description="Generate recommendations...",
    output_pydantic=RecommendationsOutput,
    agent=architect,
)

crew = Crew(agents=[architect], tasks=[task])
result = crew.kickoff()  # Higher chance of success on first try
```

### How It Works

**Autonomous Flow:**
1. **Execute normally** - Try the task as-is
2. **Detect failure** - Check if `result.pydantic` is None
3. **Auto-enhance** - Generate JSON example from Pydantic model schema
4. **Retry** - Re-run with enhanced prompt
5. **Return** - Success or graceful degradation

**Zero manual intervention required** - The system fixes itself.

### What Gets Auto-Generated

From any Pydantic model, the system automatically generates an explicit JSON example:

```python
class Recommendation(BaseModel):
    title: str
    priority: int
    files: List[str]
```

Becomes:

```markdown
**CRITICAL - OUTPUT FORMAT:**
You MUST output ONLY valid JSON matching this EXACT structure:

```json
{
  "title": "example_string",
  "priority": 1,
  "files": ["example_string"]
}
```

**RULES:**
- Output ONLY the JSON object above
- No markdown code blocks
- All field names must match exactly (case-sensitive)
- All required fields must be present
```

### Key Features

✅ **Autonomous** - No manual schema documentation needed  
✅ **Schema-driven** - Generates examples from Pydantic models  
✅ **Smart insertion** - Preserves existing prompt structure  
✅ **Duplicate prevention** - Won't re-enhance already enhanced prompts  
✅ **Graceful degradation** - Returns result even if retry fails  

### Integration

**SystemAnalysisFlow** already uses manual enhancement (lines 776-792). For new flows, use the helpers:

```python
# Manual approach (already done in SystemAnalysisFlow)
task = Task(
    description=f"...
    
**OUTPUT MUST BE VALID JSON:**
```json
{schema_example}
```
...",
    output_pydantic=RecommendationsOutput,
    agent=agent,
)

# Or use helper for automatic enhancement
task = create_pydantic_safe_task(
    description="...",
    output_pydantic=RecommendationsOutput,
    agent=agent,
)
```

### Performance Impact

- **Detection**: ~0ms (attribute check)
- **Enhancement**: ~10-50ms (schema generation once)
- **Retry**: +30-120 seconds (one additional LLM call)
- **Tokens**: +200-500 tokens for JSON example

### Testing

```bash
cd Tools/CrewAI
uv run python test_pydantic_fixer.py
```

Tests verify:
- ✓ Schema generation from Pydantic models
- ✓ Description enhancement preserves original content
- ✓ Duplicate prevention (doesn't re-enhance)

### Documentation

Complete documentation:
- `docs/pydantic-output-fixer.md` - Full usage guide
- `AUTONOMOUS-PYDANTIC-FIX.md` - Solution overview
- `examples/autonomous_pydantic_example.py` - Working examples

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

**Note:** ConditionalTask works with both sequential and hierarchical processes. In hierarchical mode, the manager evaluates the condition and routes accordingly.

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
    manager_agent=get_manager(),           # Use hierarchical for better quality control
    process=Process.hierarchical,
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

## Manager Analysis Framework

**Status:** ✅ Implemented in all 4 flows (2026-01-07)

Managers in all flows intelligently analyze outputs and detect issues. Critical issues are logged with detailed analysis but workflows continue automatically without blocking. This allows for post-run review while maintaining fully automated execution.

### How It Works

After each crew execution, the manager:

1. **Analyzes outputs** for issue patterns (hallucinated APIs, architecture violations, etc.)
2. **Classifies each issue** by type, severity, and confidence
3. **Logs critical issues** using `should_escalate_to_human()` rules (for detection only)
4. **Always routes to completion:**
   - Critical issues → Logged with detailed analysis, workflow continues
   - Auto-fixable issues → Handled silently
   - Minor issues → Logged and continued
   - **No blocking** → All flows run fully automated

### Detection Rules

| Condition | Logged as Critical? | Reason |
|-----------|---------------------|--------|
| CRITICAL severity | ✅ Always | Important to flag for review |
| Security concern | ✅ Always | Requires post-run review |
| Data loss risk | ✅ Always | Requires post-run review |
| Breaking change | ✅ Always | Requires post-run review |
| Conflicting requirements | ✅ Always | May need clarification |
| Architecture violation | ✅ Always | Structural considerations |
| HIGH severity + LOW confidence | ✅ Yes | Uncertain about serious issue |
| HIGH severity + HIGH confidence + auto-fixable | ❌ No | Can safely auto-fix |
| MEDIUM severity | ❌ Usually no | Auto-handle unless threshold raised |
| LOW severity | ❌ Never | Always auto-handle |

**Note:** All detection is for logging purposes only. Workflows never block on issues.

### Issue Types by Flow

**PlanningFlow:**
- `HALLUCINATED_FILE` - Plan references non-existent files
- `CONFLICTING_REQUIREMENTS` - Design has contradictions
- `HALLUCINATED_API` - Unclear gaps needing classification
- `DEPRECATED_SYSTEM` - References to obsolete code

**ImplementationFlow:**
- `HALLUCINATED_API` - Uses non-existent methods
- `ARCHITECTURE_VIOLATION` - Breaks established patterns
- `SCOPE_CREEP` - Implements beyond plan scope
- `BREAKING_CHANGE` - Affects save/load compatibility

**BugHuntingFlow:**
- `SCOPE_CREEP` - Fix includes unrelated changes
- `BREAKING_CHANGE` - Fix affects compatibility
- `SECURITY_CONCERN` - Security-related bug/fix
- `DATA_LOSS_RISK` - Bug/fix involves data integrity

**ValidationFlow:**
- `ARCHITECTURE_VIOLATION` - Critical build failures
- `DATA_LOSS_RISK` - Data integrity issues
- `SECURITY_CONCERN` - Security patterns in content

### Human Feedback (DISABLED)

**Status:** Human-in-the-loop functionality has been completely disabled as of 2026-01-07.

All flows run fully automated without interactive prompts. Critical issues are detected and logged but never block execution. The detection framework remains active for post-run analysis.

**Rationale:** Blocking workflows interrupts automated CI/CD pipelines. Issue detection is preserved for review, but execution is never halted.

### Detection Framework Code

**Location:** `src/enlisted_crew/flows/escalation.py`

**Core Components:**
```python
# Issue classification
class IssueType(Enum):
    HALLUCINATED_API = "hallucinated_api"
    SECURITY_CONCERN = "security_concern"
    BREAKING_CHANGE = "breaking_change"
    # ... 13 total issue types

class IssueSeverity(Enum):
    CRITICAL = "critical"  # Always escalate
    HIGH = "high"          # Escalate if low confidence
    MEDIUM = "medium"      # Usually auto-fix
    LOW = "low"            # Always auto-fix

class IssueConfidence(Enum):
    HIGH = "high"      # Manager is certain
    MEDIUM = "medium"  # Fairly sure
    LOW = "low"        # Unsure - escalate if high severity

@dataclass
class DetectedIssue:
    issue_type: IssueType
    severity: IssueSeverity
    confidence: IssueConfidence
    description: str
    affected_component: str
    evidence: str
    manager_recommendation: str
    auto_fixable: bool = False

# Core decision function
def should_escalate_to_human(issue: DetectedIssue) -> bool:
    # Critical always escalates
    if issue.severity == IssueSeverity.CRITICAL:
        return True
    # Security/data loss always escalates
    if issue.issue_type in [SECURITY_CONCERN, DATA_LOSS_RISK, BREAKING_CHANGE]:
        return True
    # High + low confidence escalates
    if issue.severity == IssueSeverity.HIGH and issue.confidence == IssueConfidence.LOW:
        return True
    # ... other rules
    return False
```

### Flow Integration Pattern

Each flow adds escalation after crew execution:

```python
@listen(run_xxx_crew)
def check_for_issues(self, state: XxxState) -> XxxState:
    """Manager analyzes outputs for issues."""
    issues = []
    
    # Detect issues from output
    if "method not found" in state.output.lower():
        issues.append(DetectedIssue(
            issue_type=IssueType.HALLUCINATED_API,
            severity=IssueSeverity.HIGH,
            confidence=IssueConfidence.MEDIUM,
            description="Uses non-existent API",
            affected_component="Implementation Output",
            evidence="Pattern found in output",
            manager_recommendation="Verify with find_in_code",
            auto_fixable=True,
        ))
    
    # Determine escalation
    critical_issues = [i for i in issues if should_escalate_to_human(i)]
    
    if critical_issues:
        state.needs_human_review = True
        state.manager_analysis = format_critical_issues(critical_issues)
    else:
        for issue in issues:
            if issue.auto_fixable:
                print(f"[MANAGER] Auto-handling: {issue.description}")
    
    return state

@router(check_for_issues)
def route_after_check(self, state: XxxState) -> str:
    """Route to next step (human feedback disabled)."""
    if state.needs_human_review:
        print("[ROUTER] Critical issues detected but auto-proceeding (HITL disabled)")
        print(f"[ROUTER] Issues logged: {len(state.critical_issues)}")
        state.needs_human_review = False
    print("[ROUTER] -> [next_step]")
    return "[next_step]"  # Always continues, never blocks
```

### State Model Fields

All state models include escalation fields:

```python
class XxxState(BaseModel):
    # ... existing fields ...
    
    # Manager escalation (human-in-the-loop)
    needs_human_review: bool = False
    critical_issues: List[str] = Field(default_factory=list)
    manager_analysis: str = ""
    manager_recommendation: str = ""
    human_guidance: str = ""  # Human's response if escalated
```

### Testing Escalation

Run the escalation framework tests:

```bash
cd Tools/CrewAI
python test_escalation.py
```

**Tests cover:**
- ✅ Critical severity always escalates
- ✅ Security/data loss always escalate
- ✅ High confidence auto-fixes don't escalate
- ✅ Low confidence + high severity escalate
- ✅ Low severity never escalates
- ✅ Threshold settings work correctly
- ✅ Issue formatting displays correctly
- ✅ Escalation messages include all required info

---

## MCP Servers Integration

CrewAI supports Model Context Protocol (MCP) servers for extending agent capabilities with external tools and services.

### Active MCP Servers

#### ✅ Bannerlord API MCP Server

**Status:** ✅ Implemented and Active

**Purpose:** Provides semantic access to decompiled Bannerlord source code (`Decompile/` in workspace root) with fast indexed queries.

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

## Troubleshooting

### "Received None or empty response from LLM call" (during Planning)

**Cause:** Using an `LLM()` object with `reasoning_effort` parameter for `planning_llm`. The CrewAI AgentPlanner doesn't properly handle LLM objects with extra parameters.

**Solution:** Use a **simple string** for `planning_llm`, not an LLM object:

```python
# ❌ BROKEN - LLM object with reasoning_effort
planning_llm=LLM(
    model="gpt-5.2-codex",
    max_completion_tokens=4000,
    reasoning_effort="low",  # This breaks AgentPlanner!
)

# ✅ WORKS - Simple string (matches official docs)
planning_llm="gpt-5.2-codex"
```

This matches the [official CrewAI documentation](https://docs.crewai.com/en/concepts/planning) which only shows simple strings in examples.

**Note:** You CAN use `LLM()` objects with `reasoning_effort` for agent LLMs - just not for `planning_llm`.

### Code Style: No Emojis

**Status:** All emojis removed from CrewAI code as of 2026-01-07.

To maintain compatibility with Warp AI agent output standards (WARP.md: "Only use emojis if the user explicitly requests it"), all emoji characters have been removed from:
- Tool output messages (`docs_tools.py`, `planning_tools.py`)
- Flow print statements (all flow files)
- Status indicators (replaced with text: "CRASH:", "RGL LOG")
- Escalation messages (`escalation.py`)

**Note:** Emojis in documentation (this file) are acceptable - the restriction applies only to Python code output.

---

## Related Documentation

- **Root:** [WARP.md](../../WARP.md) - Project-wide conventions
- **Tools:** [AGENT-WORKFLOW.md](../AGENT-WORKFLOW.md) - Agent usage patterns
- **Core:** [BLUEPRINT.md](../../docs/BLUEPRINT.md) - System architecture
- **Content:** [event-system-schemas.md](../../docs/Features/Content/event-system-schemas.md)
- **Content:** [writing-style-guide.md](../../docs/Features/Content/writing-style-guide.md)
- **Planning:** [docs/CrewAI_Plans/](../../docs/CrewAI_Plans/) - Generated planning documents
