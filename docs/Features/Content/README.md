# Content System Documentation

**Summary:** Complete documentation for the mod's unified content system including technical architecture, JSON schemas, and a full catalog of all 200+ narrative content pieces (events, decisions, orders, and map incidents).

**Total Content:** 200+ pieces including 17 orders, 38 decisions, 80+ events, and 51 map incidents.

---

## Index

### Content Catalog (What content exists)

| Document | Purpose | Content Count |
|----------|---------|---------------|
| [content-index.md](content-index.md) | Master catalog: all content with IDs, titles, descriptions, requirements, effects, skill checks, organized by category (Orders, Decisions, Events, Map Incidents, Retinue Content) | 207 pieces |
| [event-catalog-by-system.md](event-catalog-by-system.md) | Events organized by feature system: lists every event ID with trigger conditions, outcomes, reputation effects, grouped by area (Core/Equipment/Combat/Retinue) for system-based lookup | 80+ events |
| [content-orchestrator-prompts.md](content-orchestrator-prompts.md) | AI prompts and guidelines for content generation and orchestration |

### Technical Implementation (How the system works)

| Document | Topic | Status |
|----------|-------|--------|
| [content-system-architecture.md](content-system-architecture.md) | Content system design: JSON-driven event framework, condition evaluation engine, event triggering rules, content file organization, localization integration | ⚠️ Mixed |
| [event-system-schemas.md](event-system-schemas.md) | Event system JSON schemas: event structure (triggers, conditions, options, outcomes), decision schemas, order schemas, dialogue schemas, validation rules | ✅ Current |
| [content-orchestrator-plan.md](content-orchestrator-plan.md) | Content Orchestrator (Sandbox Life Simulator): replaces schedule-driven pacing with world-state driven frequency, components (WorldStateAnalyzer, SimulationPressureCalculator, PlayerBehaviorTracker), 5-week implementation plan, migration strategy | 📋 Specification |
| [event-reward-choices.md](event-reward-choices.md) | Event reward system: player choice outcomes, reward types (gold, items, reputation, XP), branching consequences | 📋 Specification |
| [medical-progression-system.md](medical-progression-system.md) | CK3-style medical progression: injury system, treatment events, recovery chains, medical decisions, surgeon interactions | 📋 Specification |

---

## Quick Navigation

**Finding specific content:**
1. Use [content-index.md](content-index.md) for complete listings by content type
2. Use [event-catalog-by-system.md](event-catalog-by-system.md) to find events by feature area

**Adding new content:**
- Read [event-system-schemas.md](event-system-schemas.md) for JSON structure and validation rules
- See [content-orchestrator-prompts.md](content-orchestrator-prompts.md) for content generation guidelines

**Understanding the system:**
- Start with [content-system-architecture.md](content-system-architecture.md) for the full technical picture
- See [content-orchestrator-plan.md](content-orchestrator-plan.md) for future orchestration enhancements

**Native game reference:**
- [native-map-incidents.md](../../Reference/native-map-incidents.md) - Vanilla Bannerlord map incidents
- [native-skill-xp.md](../../Reference/native-skill-xp.md) - Native skill progression rates
- [map-incidents-warsails.md](../../Reference/map-incidents-warsails.md) - Warsails/Naval DLC incidents

---

## Content Summary

| Category | Count | Description |
|----------|-------|-------------|
| **Orders** | 17 | Military directives from chain of command (6 T1-T3, 6 T4-T6, 5 T7-T9) |
| **Decisions** | 38 | Player-initiated Camp Hub actions (34 core + 4 retinue T7+) |
| **Events** | 80+ | Context-triggered narrative situations |
| **Map Incidents** | 45 | Battle, siege, and settlement-triggered encounters |
| **Retinue Content** | 23 | T7+ commander content (17 events + 6 incidents) |

---
