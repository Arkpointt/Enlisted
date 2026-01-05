# CrewAI Knowledge Sources

This folder contains **dynamic context** that CrewAI agents query at runtime.

Unlike agent backstories (which should be durable/architectural), these files contain
implementation details that change as the codebase evolves.

## Purpose

- Agents query these files instead of hardcoding details in backstories
- When systems change, update these files — not every agent's backstory
- `documentation_maintainer` keeps these in sync during doc updates

## Files

| File | Content | Updated When |
|------|---------|--------------|
| `enlisted-systems.md` | High-level system summaries, key classes, relationships | Architecture changes |
| `error-codes.md` | Error code meanings (E-*, W-*) and log locations | New error codes added |
| `json-schemas.md` | Current JSON field requirements, ordering rules | Schema changes |
| `balance-values.md` | XP rates, tier thresholds, economy values | Balance tuning |
| `ui-systems.md` | Menu behaviors, event delivery, Gauntlet screens | UI changes |

## How Agents Use This

Agents are configured with `knowledge_sources` pointing to this folder.
At runtime, they query relevant files instead of relying on static backstory text.

Example in `crew.py`:
```python
from crewai.knowledge.source.text_file_knowledge_source import TextFileKnowledgeSource

knowledge = TextFileKnowledgeSource(
    file_paths=["knowledge/enlisted-systems.md", "knowledge/error-codes.md"]
)
```

## Maintenance

`documentation_maintainer` agent is responsible for:
1. Creating new knowledge files when new systems are documented
2. Updating existing files when implementations change
3. Flagging when backstories reference outdated details

This happens incrementally as part of normal documentation sync tasks.
