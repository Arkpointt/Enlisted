# Enlisted Crew

CrewAI system for Enlisted mod development with single-agent Flow pattern.

## Features

- **Implementation Flow**: Build from approved plans (8 Flow steps, 366 lines)
- **Bug Hunting Flow**: Automated bug investigation (8 Flow steps, 614 lines)
- **Validation Flow**: Quality assurance (6 Flow steps, 557 lines)
- **Single-Agent Architecture**: Optimal performance with focused tool usage
- **Advanced Memory**: Contextual Retrieval with hybrid search (+67% retrieval improvement)
- **Semantic Search**: Code and documentation search via ChromaDB vector index
- **57 Custom Tools**: Database, file operations, validation, MCP integration

## Installation

```bash
pip install -e .
```

## Usage

```bash
# Bug hunting
enlisted-crew hunt-bug -d "bug description" -e "E-XXX-*"

# Implementation from plan
enlisted-crew implement -p "docs/CrewAI_Plans/feature.md"

# Validation
enlisted-crew validate

# Execution statistics
enlisted-crew stats
```

**Note on Planning**: For planning/design tasks, use Warp Agent directly. It has full codebase access and is faster than multi-agent orchestration.
