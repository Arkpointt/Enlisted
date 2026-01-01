# Content System Documentation

**Summary:** Complete documentation for the mod's unified content system including technical architecture, JSON schemas, and a full catalog of all 275 narrative content pieces (events, decisions, orders, order events, and map incidents).

**Total Content:** 275 pieces including 16 orders, 85 order events, 38 decisions, 68 events, and 51 map incidents (45 general + 6 retinue), plus 17 retinue events.

---

## Index

### Content Catalog (What content exists)

| Document | Purpose | Content Count |
|----------|---------|---------------|
| [content-index.md](content-index.md) | Master catalog: all content with IDs, titles, descriptions, requirements, effects, skill checks, organized by category (Orders, Decisions, Events, Map Incidents, Retinue Content) | 275 pieces |
| [event-catalog-by-system.md](event-catalog-by-system.md) | Events organized by feature system: lists every event ID with trigger conditions, outcomes, reputation effects, grouped by area (Core/Equipment/Combat/Retinue) for system-based lookup | 68 events |
| [../../AFEATURE/order-events-master.md](../../AFEATURE/order-events-master.md) | Complete catalog of all 85 order events across 16 event pools with full details | 85 order events |
| [../../AFEATURE/orders-content.md](../../AFEATURE/orders-content.md) | All 16 order definitions with JSON file references | 16 orders |

### Technical Implementation (How the system works)

| Document | Topic | Status |
|----------|-------|--------|
| [content-system-architecture.md](content-system-architecture.md) | Content system design: JSON-driven event framework, condition evaluation engine, event triggering rules, content file organization, localization integration | ⚠️ Mixed |
| [event-system-schemas.md](event-system-schemas.md) | Event system JSON schemas: event structure (triggers, conditions, options, outcomes), decision schemas, order schemas, dialogue schemas, validation rules | ✅ Current |
| [../../AFEATURE/content-orchestrator-plan.md](../../AFEATURE/content-orchestrator-plan.md) | Content Orchestrator (Sandbox Life Simulator): replaces schedule-driven pacing with world-state driven frequency, components (WorldStateAnalyzer, SimulationPressureCalculator, PlayerBehaviorTracker), implementation status. Phases 1-6F complete, phases 6G/9-10 are future enhancements. | ✅ Core Complete |
| [event-reward-choices.md](event-reward-choices.md) | Event reward system: player choice outcomes, reward types (gold, items, reputation, XP), branching consequences | ⚠️ Coded, Not Content-Implemented |
| [medical-progression-system.md](medical-progression-system.md) | CK3-style medical progression: injury system, treatment events, recovery chains, medical decisions, surgeon interactions | 📋 Specification |

---

## Quick Navigation

**Finding specific content:**
1. Use [content-index.md](content-index.md) for complete listings by content type
2. Use [event-catalog-by-system.md](event-catalog-by-system.md) to find events by feature area

**Adding new content:**
- Read [event-system-schemas.md](event-system-schemas.md) for JSON structure and validation rules
- See [../../AFEATURE/content-orchestrator-prompts.md](../../AFEATURE/content-orchestrator-prompts.md) for content generation guidelines

**Understanding the system:**
- Start with [content-system-architecture.md](content-system-architecture.md) for the full technical picture
- See [../../AFEATURE/content-orchestrator-plan.md](../../AFEATURE/content-orchestrator-plan.md) for orchestrator implementation status and future enhancements

**Native game reference:**
- [native-map-incidents.md](../../Reference/native-map-incidents.md) - Vanilla Bannerlord map incidents
- [native-skill-xp.md](../../Reference/native-skill-xp.md) - Native skill progression rates
- [map-incidents-warsails.md](../../Reference/map-incidents-warsails.md) - Warsails/Naval DLC incidents

---

## Content Summary

| Category | Count | Description |
|----------|-------|-------------|
| **Orders** | 16 | Military directives from chain of command (6 T1-T3, 5 T4-T6, 5 T7-T9) |
| **Order Events** | 85 | Events that fire during order execution (16 event pools, see [order-events-master.md](../../AFEATURE/order-events-master.md)) |
| **Decisions** | 38 | Player-initiated Camp Hub actions (34 core + 4 retinue T7+) |
| **Events** | 68 | Context-triggered narrative situations (14 escalation + 5 crisis + 49 role/universal) |
| **Map Incidents** | 51 | Battle, siege, and settlement-triggered encounters (45 general + 6 retinue) |
| **Retinue Content** | 23 | T7+ commander content (17 events + 6 incidents, included in counts above) |
| **Total** | **275** | Complete content catalog |

---
