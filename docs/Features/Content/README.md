# Content System Documentation

**Summary:** Complete documentation for the mod's unified content system including technical architecture, JSON schemas, and a full catalog of all 274 narrative content pieces (events, decisions, orders, order events, and map incidents).

**Total Content:** 520 pieces including 17 orders, 330 order events, 33 decisions, 72 events, 51 map incidents, and 17 retinue events.

---

## Index

### Content Catalog (What content exists)

| Document | Purpose | Content Count |
|----------|---------|---------------|
| [content-index.md](content-index.md) | Master catalog: all content with IDs, titles, descriptions, requirements, effects, skill checks, organized by category (Orders, Decisions, Events, Map Incidents, Retinue Content) | 274 pieces |
| [event-catalog-by-system.md](event-catalog-by-system.md) | Events organized by feature system: lists every event ID with trigger conditions, outcomes, reputation effects, grouped by area (Core/Equipment/Combat/Retinue) for system-based lookup | 72 events |
| [orders-content.md](orders-content.md) | All 17 order definitions. Order events (330 total) are defined in JSON at `ModuleData/Enlisted/Orders/order_events/` | 17 orders + 330 events |

### Technical Implementation (How the system works)

| Document | Topic | Status |
|----------|-------|--------|
| [content-system-architecture.md](content-system-architecture.md) | Content system design: JSON-driven event framework, condition evaluation engine, event triggering rules, content file organization, localization integration | ⚠️ Mixed |
| [event-system-schemas.md](event-system-schemas.md) | Event system JSON schemas: event structure (triggers, conditions, options, outcomes), decision schemas, order schemas, dialogue schemas, validation rules | ✅ Current |
| [content-system-architecture.md](content-system-architecture.md) | Content Orchestrator: world-state driven content frequency, components (WorldStateAnalyzer, SimulationPressureCalculator, PlayerBehaviorTracker). All core phases implemented. | ✅ Implemented |
| [event-reward-choices.md](event-reward-choices.md) | Event reward system: player choice outcomes, reward types (gold, items, reputation, XP), branching consequences | ⚠️ Coded, Not Content-Implemented |
| [medical-progression-system.md](medical-progression-system.md) | CK3-style medical progression: injury system, treatment events, recovery chains, medical decisions, surgeon interactions | 📋 Specification |

---

## Quick Navigation

**Finding specific content:**
1. Use [content-index.md](content-index.md) for complete listings by content type
2. Use [event-catalog-by-system.md](event-catalog-by-system.md) to find events by feature area

**Adding new content:**
- Read [event-system-schemas.md](event-system-schemas.md) for JSON structure and validation rules
- See [content-system-architecture.md](content-system-architecture.md) for orchestrator integration

**Understanding the system:**
- Start with [content-system-architecture.md](content-system-architecture.md) for the full technical picture
- See [event-system-schemas.md](event-system-schemas.md) for JSON schemas and validation

**Native game reference:**
- [native-map-incidents.md](../../Reference/native-map-incidents.md) - Vanilla Bannerlord map incidents
- [native-skill-xp.md](../../Reference/native-skill-xp.md) - Native skill progression rates
- [map-incidents-warsails.md](../../Reference/map-incidents-warsails.md) - Warsails/Naval DLC incidents

---

## Content Summary

| Category | Count | Description |
|----------|-------|-------------|
| **Orders** | 16 | Military directives from chain of command (6 T1-T3, 5 T4-T6, 5 T7-T9) |
| **Order Events** | 330 | Events that fire during order execution (defined in `ModuleData/Enlisted/Orders/order_events/*.json`) |
| **Decisions** | 33 | Player-initiated Camp Hub actions (38 before Phase 6G → deleted 35 old + kept 3 + added 30 new = 33 total) |
| **Events** | 72 | Context-triggered narrative situations (14 escalation + 5 crisis + 49 role/universal + 4 medical orchestration) |
| **Map Incidents** | 51 | Battle, siege, and settlement-triggered encounters (45 general + 6 retinue) |
| **Retinue Content** | 23 | T7+ commander content (17 events + 6 incidents, included in counts above) |
| **Total** | **274** | Complete content catalog |

---
