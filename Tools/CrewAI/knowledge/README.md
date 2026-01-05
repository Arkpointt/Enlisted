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
| `core-systems.md` | High-level system summaries, key classes, relationships | Architecture changes |
| `error-codes.md` | Error code meanings (E-*, W-*) and log locations | New error codes added |
| `event-format.md` | Current JSON field requirements, ordering rules | Schema changes |
| `balance-values.md` | XP rates, tier thresholds, economy values | Balance tuning |
| `ui-systems.md` | Menu behaviors, event delivery, Gauntlet screens | UI changes |
| `game-design-principles.md` | Tier-aware design, player experience, engagement rules | Design philosophy updates |
| `content-files.md` | JSON content inventory, folder structure, ID conventions | Content additions |

## How Agents Use This

Agents are configured with `knowledge_sources` pointing to this folder.
At runtime, they query relevant files instead of relying on static backstory text.

### Agent → Knowledge Mapping

| Knowledge Source | Files Included | Used By |
|------------------|----------------|---------|
| `systems_knowledge` | core-systems | systems_analyst |
| `design_knowledge` | core-systems, game-design-principles | feature_architect |
| `code_knowledge` | core-systems, error-codes | code_analyst |
| `content_knowledge` | event-format, balance-values, game-design-principles | content_author, content_analyst |
| `balance_knowledge` | balance-values, core-systems, game-design-principles | balance_analyst |
| `ui_knowledge` | ui-systems, core-systems | csharp_implementer |
| `planning_knowledge` | core-systems, content-files | documentation_maintainer |

Example in `crew.py`:
```python
self.design_knowledge = TextFileKnowledgeSource(
    file_paths=["core-systems.md", "game-design-principles.md"]
)
```

## Maintenance

`documentation_maintainer` agent is responsible for:
1. Creating new knowledge files when new systems are documented
2. Updating existing files when implementations change
3. Flagging when backstories reference outdated details

This happens incrementally as part of normal documentation sync tasks.
