# Enlisted Crew

CrewAI multi-agent system for Enlisted mod development.

## Features

- **Implementation Flow**: Code implementation with validation and testing
- **Bug Hunting Flow**: Automated bug detection and analysis
- **Validation Flow**: Quality assurance and testing
- **System Analysis Flow**: ✅ Production Ready - Technical system integration analysis with natural language query support
  - Natural language queries: "analyze my gameplay flow", "find gaps in progression"
  - Automated gap detection: implementation bugs + design opportunities
  - Generator-Critic pattern with dual QA + Game Designer perspective
  - Database-driven discovery (40+ systems auto-synced from codebase)
  - Impact/effort prioritized recommendations
- **Semantic Search Tools**: Vector-based search for code and documentation
  - Code search: `src/` + `Decompile/` C# codebase
  - Doc search: `docs/` markdown files (79 files, 3,184 chunks)
  - Natural language queries find relevant content
  - ChromaDB vector index with text-embedding-3-large
- **Contextual Retrieval Memory**: Advanced memory system with +67% retrieval improvement
  - Semantic chunking (1000 tokens, 15% overlap)
  - GPT-5.2 Codex contextualization
  - Hybrid search (Vector + BM25)
  - RRF fusion
  - Cohere reranking

## Installation

```bash
pip install -e .
```

## Usage

```bash
# Bug hunting
enlisted-crew hunt-bug -d "bug description" -e "E-XXX-*"
# Reports auto-save to docs/CrewAI_Plans/bug-hunt-{description}-{timestamp}.md

# Implementation from plan
enlisted-crew implement -p "path/to/plan.md"

# Pre-commit validation
enlisted-crew validate

# System analysis - Natural language (RECOMMENDED)
enlisted-crew analyze "analyze my gameplay flow"
enlisted-crew analyze "find gaps in player progression"
enlisted-crew analyze "what's wrong with the promotion system"

# System analysis - Explicit systems
enlisted-crew analyze-system "Supply,Morale,Reputation"
# Analysis auto-saves to: docs/CrewAI_Plans/{system-name}-analysis.md
```

**Note on Planning**: For planning and design tasks, use Warp Agent directly in your terminal. It has full codebase access and is faster than multi-agent orchestration. The planning flow has been deprecated in favor of direct interaction with Warp.

## Requirements

- Python 3.10-3.13
- CrewAI with tools support
- MCP (Model Context Protocol)
- OpenAI API key (required)
- Cohere API key (optional, for reranking)

## Tools

Custom tools for Enlisted mod development:
- **Semantic search**: Code (`search_codebase`) and docs (`search_docs_semantic`)
- **Database tools**: 24 tools for instant knowledge queries
- **File operations**: Read, write, and validation
- **Code style**: ReSharper compliance checks
- **Documentation**: Read, list, and semantic search
- **Schema validation**: JSON event/order validation

## Documentation

See [docs/CREWAI.md](docs/CREWAI.md) for complete documentation including:
- Workflow details
- Agent configuration
- Memory system architecture
- Tool reference
- Testing guide

---

## Workflow Quick Reference

| Workflow | Command | Purpose | Output |
|----------|---------|---------|--------|
| **Bug Hunting** | `enlisted-crew hunt-bug -d "description" -e "E-XXX-*"` | Find and fix bugs | `docs/CrewAI_Plans/bug-hunt-{desc}-{timestamp}.md` |
| **Implementation** | `enlisted-crew implement -p "path/to/plan.md"` | Build from approved plan | Complete implementation + validation |
| **Validation** | `enlisted-crew validate` | Pre-commit quality check | Pass/fail status report |
| **Analysis (NL)** | `enlisted-crew analyze "analyze my gameplay flow"` | Natural language system analysis | `docs/CrewAI_Plans/{query-slug}-analysis.md` |
| **Analysis (Direct)** | `enlisted-crew analyze-system "Supply,Morale"` | Explicit system analysis | `docs/CrewAI_Plans/{system-name}-analysis.md` |
| **Statistics** | `enlisted-crew stats` | View execution metrics | Console output |

### System Analysis Features

- **Natural Language Queries:** "analyze my gameplay flow", "find gaps in progression", "audit visibility"
- **8-Step Flow:** Architecture → Gaps → Critic (Generator-Critic pattern) → Efficiency → Recommendations → Feasibility → Report
- **Dual Perspective:** Implementation bugs (QA) + Design opportunities (Game Designer)
- **Database-Driven:** Auto-syncs 40+ systems from codebase on first access
- **Systematic Analysis:** 4-question framework (Binary/Gradient, Gates/Weights, Arcs, Integration)
- **Actionable Output:** Impact/effort prioritized recommendations with hour estimates
