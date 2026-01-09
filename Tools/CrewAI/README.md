# Enlisted Crew

CrewAI multi-agent system for Enlisted mod development.

## Features

- **Implementation Flow**: Code implementation with validation and testing
- **Bug Hunting Flow**: Automated bug detection and analysis
- **Validation Flow**: Quality assurance and testing
- **Semantic Search Tools**: Vector-based search for code and documentation
  - Code search: `src/` + `Decompile/` C# codebase
  - Doc search: `docs/` markdown files (79 files, 3,184 chunks)
  - Natural language queries find relevant content
  - ChromaDB vector index with text-embedding-3-large
- **Contextual Retrieval Memory**: Advanced memory system with +67% retrieval improvement
  - Semantic chunking (1000 tokens, 15% overlap)
  - GPT-5.2 contextualization
  - Hybrid search (Vector + BM25)
  - RRF fusion
  - Cohere reranking

## Installation

```bash
pip install -e .
```

## Usage

```bash
enlisted-crew hunt-bug -d "bug description" -e "E-XXX-*"
enlisted-crew implement -p "path/to/plan.md"
enlisted-crew validate
enlisted-crew stats
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
