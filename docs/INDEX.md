# Enlisted Documentation - Complete Index

**Summary:** Master index of all documentation files organized by category. Use this to find documentation for specific topics or systems.

**Last Updated:** 2025-12-29 (Added Progression System Schema, updated medical-progression-system.md)  
**Total Documents:** 49

> **Note:** Documents marked "⚠️ Mixed" have core features implemented but also contain planned/designed features not yet in code. Check their Implementation Checklist sections for details.

---

## Quick Start Guide

**New to this project? Start here:**

1. **Read [BLUEPRINT.md](BLUEPRINT.md)** - Architecture, coding standards, project constraints
2. **Read this INDEX** - Navigate to relevant docs for your task
3. **Read [Features/Core/core-gameplay.md](Features/Core/core-gameplay.md)** - Best overview of how systems work together

**Finding What You Need:**

| I need to... | Go to... |
|--------------|----------|
| Understand the project scope | [BLUEPRINT.md](BLUEPRINT.md) → "For AI Assistants" |
| Learn core game mechanics | [Features/Core/core-gameplay.md](Features/Core/core-gameplay.md) |
| Understand rank progression | [Features/Core/promotion-system.md](Features/Core/promotion-system.md) |
| Find a specific feature quickly | [Feature Lookup Quick Reference](#feature-lookup-quick-reference) ⭐ NEW |
| Find how a feature works | Search this INDEX, check [Features/Core/](#core-systems) |
| See all events/decisions/orders | [Content/event-catalog-by-system.md](Content/event-catalog-by-system.md) |
| Verify Bannerlord APIs | [Reference/native-apis.md](Reference/native-apis.md) |
| Build/deploy the mod | [DEVELOPER-GUIDE.md](DEVELOPER-GUIDE.md) |
| Create new documentation | [BLUEPRINT.md](BLUEPRINT.md) → "Creating New Documentation" |

**Documentation Structure:**
- **Features/** - How systems work (organized by category: Core, Equipment, Combat, etc.)
- **Content/** - Complete catalog of events, decisions, orders, map incidents
- **Reference/** - Technical references: native APIs, AI analysis, skill mechanics

---

## Index

1. [Root Documentation](#root-documentation)
2. [Feature Lookup Quick Reference](#feature-lookup-quick-reference)
3. [Features](#features)
4. [Active Feature Development (AFEATURE)](#active-feature-development-afeature)
5. [Content & Narrative](#content--narrative)
6. [Research & Reference](#research--reference)

---

## Feature Lookup Quick Reference

**Can't find a specific feature? Use this lookup table:**

| Feature / System | Found In Document | Section |
|------------------|-------------------|---------|
| **Baggage Checks/Inspections** | [quartermaster-system.md](Features/Equipment/quartermaster-system.md) | Baggage Checks |
| **Buyback System** | [quartermaster-system.md](Features/Equipment/quartermaster-system.md) | Buyback System |
| **Camp Activities (Current)** | [camp-life-simulation.md](Features/Campaign/camp-life-simulation.md) | — |
| **Camp Activities (Future)** | [camp-life-simulation.md](AFEATURE/camp-life-simulation.md) | Living camp simulation (Phase 6) |
| **Camp Hub (Custom Gauntlet)** | [camp-hub-custom-gauntlet.md](Features/UI/camp-hub-custom-gauntlet.md) | — |
| **Company Events** | [company-events.md](Features/Core/company-events.md) | — |
| **Companion Integration** | [companion-management.md](Features/Core/companion-management.md) | — |
| **Contraband Detection** | [quartermaster-system.md](Features/Equipment/quartermaster-system.md) | Baggage Checks |
| **Contextual Dialogue** | [quartermaster-system.md](Features/Equipment/quartermaster-system.md) | Contextual Dialogue System |
| **Discharge Process** | [onboarding-discharge-system.md](Features/Core/onboarding-discharge-system.md) | Discharge |
| **Discounts (QM)** | [quartermaster-system.md](Features/Equipment/quartermaster-system.md) | Reputation System |
| **Enlistment** | [enlistment.md](Features/Core/enlistment.md) | — |
| **Equipment Purchasing** | [quartermaster-system.md](Features/Equipment/quartermaster-system.md) | Equipment Purchasing |
| **Equipment Quality/Tiers** | [quartermaster-system.md](Features/Equipment/quartermaster-system.md) | Equipment Quality System |
| **Equipment Upgrades** | [quartermaster-system.md](Features/Equipment/quartermaster-system.md) | Equipment Upgrade System |
| **Event System (JSON)** | [event-system-schemas.md](Features/Content/event-system-schemas.md) | — |
| **Fatigue System** | [camp-fatigue.md](Features/Core/camp-fatigue.md) | — |
| **First Meeting (QM)** | [quartermaster-system.md](Features/Equipment/quartermaster-system.md) | Quartermaster NPC |
| **Food & Rations** | [provisions-rations-system.md](Features/Equipment/provisions-rations-system.md) | — |
| **Formation Assignment** | [formation-assignment.md](Features/Combat/formation-assignment.md) | — |
| **Leave System** | [temporary-leave.md](Features/Campaign/temporary-leave.md) | — |
| **Muster System (Pay Day Ceremony)** | [muster-system.md](Features/Core/muster-system.md) | Menu Flow, All 8 Stages |
| **Officers Armory** | [quartermaster-system.md](Features/Equipment/quartermaster-system.md) | Officers Armory |
| **Onboarding** | [onboarding-discharge-system.md](Features/Core/onboarding-discharge-system.md) | Onboarding |
| **Orders (Chain of Command)** | [order-progression-system.md](AFEATURE/order-progression-system.md) | — |
| **Pay & Wages** | [pay-system.md](Features/Core/pay-system.md) | — |
| **Progression System** | [event-system-schemas.md](Features/Content/event-system-schemas.md#progression-system-schema-future-foundation) | Generic probabilistic daily rolls for escalation tracks |
| **Promotion & Rank Progression** | [promotion-system.md](Features/Core/promotion-system.md) | — |
| **Provisions Shop** | [provisions-rations-system.md](Features/Equipment/provisions-rations-system.md) | T7+ Officer Provisions |
| **Quartermaster System** | [quartermaster-system.md](Features/Equipment/quartermaster-system.md) | — |
| **Reputation (QM)** | [quartermaster-system.md](Features/Equipment/quartermaster-system.md) | Reputation System |
| **Reputation (Soldier)** | [identity-system.md](Features/Identity/identity-system.md) | — |
| **Retinue (Commander's)** | [retinue-system.md](Features/Core/retinue-system.md) | — |
| **Supply Tracking** | [company-supply-simulation.md](Features/Equipment/company-supply-simulation.md) | — |
| **Tier Gates/Restrictions** | [quartermaster-system.md](Features/Equipment/quartermaster-system.md) | Multiple sections |
| **Town Access** | [town-access-system.md](Features/Campaign/town-access-system.md) | — |
| **Training & XP** | [training-system.md](Features/Combat/training-system.md) | — |
| **Traits & Identity** | [identity-system.md](Features/Identity/identity-system.md) | — |

## Root Documentation

**Start here for entry points and core reference:**

| Document | Purpose | Status |
|----------|---------|--------|
| [README.md](README.md) | Main entry point and mod overview | ✅ Current |
| [BLUEPRINT.md](BLUEPRINT.md) | Project architecture and coding standards | ✅ Current |
| [DEVELOPER-GUIDE.md](DEVELOPER-GUIDE.md) | Build guide and development patterns | ✅ Current |

---

## Features

### Core Systems
**Location:** `Features/Core/`

| Document | Topic | Status |
|----------|-------|--------|
| [index.md](Features/Core/index.md) | Core features index | ✅ Current |
| [core-gameplay.md](Features/Core/core-gameplay.md) | Complete gameplay overview covering all major systems and how they interact | ✅ Current |
| [enlistment.md](Features/Core/enlistment.md) | Enlistment system: joining process, lord selection, initial rank assignment, contract terms | ✅ Current |
| [orders-system.md](Features/Core/orders-system.md) | ⚠️ **LEGACY** - Replaced by [Order Progression System](AFEATURE/order-progression-system.md) | 🗄️ Deprecated |
| [order-progression-system.md](AFEATURE/order-progression-system.md) | **NEW** - Multi-day order execution: FORECAST → SCHEDULED → PENDING → ACTIVE → COMPLETE flow, 4 phases per day (6am/12pm/6pm/12am), slot events during duty (15-35% chance), consequence accumulation, orchestrator-coordinated timing | ✅ Active |
| [promotion-system.md](Features/Core/promotion-system.md) | Rank progression T1-T9: XP sources (combat, orders, training), multi-factor requirements (service days, battles fought, reputation thresholds, discipline score), proving events (rank-up challenges), culture-specific rank titles, equipment tier unlocks, officer privileges (T7+) | ✅ Current |
| [pay-system.md](Features/Core/pay-system.md) | Wages and payment: 12-day muster cycle, rank-based pay scales, wage modifiers (performance, reputation, lord wealth), pay tension (mutiny risk), deductions (fines, missing gear) | ✅ Current |
| [muster-system.md](Features/Core/muster-system.md) | Muster System: 8-stage GameMenu sequence for pay day ceremonies, rank progression display, period summary (12-day recap), event integration (baggage/inspection/recruit), comprehensive reporting (combat/training/orders/XP breakdown), pay options, promotion recap, retinue muster (T7+), direct Quartermaster access | ✅ Current |
| [company-events.md](Features/Core/company-events.md) | Company-wide events: march events, camp life events, morale events, supply events | ✅ Current |
| [retinue-system.md](Features/Core/retinue-system.md) | Commander's retinue (T7+ officers): formation selection (player chooses retinue troops), context-aware trickle (1-3 troops per battle), relation-based reinforcements, loyalty tracking, 11 narrative events (character development), 6 post-battle incidents (heroism/casualties), 4 camp decisions (discipline/rewards), named veterans (persistent characters) | ✅ Current |
| [companion-management.md](Features/Core/companion-management.md) | Companion integration: how companions work with enlisted systems, role assignments, special interactions | ✅ Current |
| [camp-fatigue.md](Features/Core/camp-fatigue.md) | Rest and fatigue: fatigue accumulation (marching, fighting), rest recovery (camp actions), fatigue effects on performance | ✅ Current |
| [onboarding-discharge-system.md](Features/Core/onboarding-discharge-system.md) | Onboarding (initial training, orientation, first conversations) and discharge (equipment reclamation, final pay settlement, retirement options) | ✅ Current |

### Equipment & Logistics
**Location:** `Features/Equipment/`

| Document | Topic | Status |
|----------|-------|--------|
| [quartermaster-system.md](Features/Equipment/quartermaster-system.md) | Complete quartermaster system with 10+ subsystems: equipment purchasing (category browsing, reputation discounts 0-30%), quality modifiers (6 tiers affecting stats/prices), upgrade system (Gauntlet grid UI, sequential quality improvements with real stat bonuses, native ItemModifier system), buyback service (sell QM gear back at 30-65%), provisions/rations (T1-T6 issued, T7+ shop with Gauntlet grid UI), baggage inspections (contraband checks, rep-based outcomes), Officers Armory (T7+, elite gear), tier gates (rank-based access control), supply integration (equipment blocked <30%), first-meeting intro, contextual dialogue (150+ dynamic responses) | ✅ Current |
| [provisions-rations-system.md](Features/Equipment/provisions-rations-system.md) | Food and rations: T1-T6 issued rations (12-day cycle, reclaimed at muster, quality by rep), T7+ provisions shop (premium prices 2.0-3.2x town markets, stock by supply level, Gauntlet grid UI with rank-based button gating), provision bundles (morale/fatigue boosts) | ✅ Current |
| [company-supply-simulation.md](Features/Equipment/company-supply-simulation.md) | Company supply tracking (0-100% scale, 40% observed from lord's food, 60% simulated logistics), supply effects (equipment access gates, ration availability, QM greeting tone, stock levels), supply-based messaging | ⚠️ Mixed |
| [baggage-train-availability.md](Features/Equipment/baggage-train-availability.md) | Baggage train access gating based on march state, rank, and context | 📋 Specification |

### Identity & Traits
**Location:** `Features/Identity/`

| Document | Topic | Status |
|----------|-------|--------|
| [README.md](Features/Identity/README.md) | Identity folder overview | ✅ Current |
| [identity-system.md](Features/Identity/identity-system.md) | Trait and identity system: personality traits (acquired through events/actions), trait effects on events/dialogues/options, reputation tracking (soldier rep, QM rep, discipline), identity development over career | ✅ Current |

### Combat & Training
**Location:** `Features/Combat/`

| Document | Topic | Status |
|----------|-------|--------|
| [README.md](Features/Combat/README.md) | Combat folder overview | ✅ Current |
| [training-system.md](Features/Combat/training-system.md) | Training and XP: camp training actions (weapon drills, fitness), skill progression (XP rates, skill caps by rank), training events (success/injury/fatigue), cooldowns | ✅ Current |
| [formation-assignment.md](Features/Combat/formation-assignment.md) | Battle formation logic: T1-T6 soldiers auto-assigned to formation based on equipped weapons (bow→Ranged, horse→Cavalry, both→Horse Archer, melee→Infantry), teleported to formation position. T7+ commanders control their own party, no auto-assignment. | ✅ Current |

### Campaign & World
**Location:** `Features/Campaign/`

| Document | Topic | Status |
|----------|-------|--------|
| [README.md](Features/Campaign/README.md) | Campaign folder overview | ✅ Current |
| [camp-life-simulation.md](Features/Campaign/camp-life-simulation.md) | Camp activities: daily routine events, social interactions, training opportunities, rest actions, morale management in camp | ✅ Current |
| [temporary-leave.md](Features/Campaign/temporary-leave.md) | Leave system: requesting leave (rank-based approval), leave duration limits, leave activities (visit family, trade, rest), return requirements, AWOL consequences | ✅ Current |
| [town-access-system.md](Features/Campaign/town-access-system.md) | Town access rules: rank-based restrictions (T1-T4 limited, T5+ more freedom), permission requirements, town activities available by rank, leave of absence system | ✅ Current |

### Content System
**Location:** `Features/Content/`

| Document | Topic | Status |
|----------|-------|--------|
| [README.md](Features/Content/README.md) | Content folder overview | ✅ Current |
| [content-system-architecture.md](Features/Content/content-system-architecture.md) | Content system design: JSON-driven event framework, condition evaluation engine, event triggering rules, content file organization, localization integration | ⚠️ Mixed |
| [event-system-schemas.md](Features/Content/event-system-schemas.md) | Event system JSON schemas: event structure (triggers, conditions, options, outcomes), decision schemas, order schemas, dialogue schemas, **Progression System Schema** (generic probabilistic daily rolls for escalation tracks), camp opportunities schema, validation rules | ✅ Current |
| [content-orchestrator-plan.md](Features/Content/content-orchestrator-plan.md) | Content Orchestrator (Sandbox Life Simulator): replaces schedule-driven pacing with world-state driven frequency, components (WorldStateAnalyzer, SimulationPressureCalculator, PlayerBehaviorTracker), 5-week implementation plan, migration strategy, **Future Expansion: Progression System integration** | 📋 Specification |
| [event-reward-choices.md](Features/Content/event-reward-choices.md) | Event reward system: player choice outcomes, reward types (gold, items, reputation, XP), branching consequences | 📋 Specification |
| [medical-progression-system.md](Features/Content/medical-progression-system.md) | First implementation of Progression System: CK3-style probabilistic daily rolls for Medical Risk track, treatment decisions, context/skill modifiers, threshold events. Uses generic [Progression System Schema](Features/Content/event-system-schemas.md#progression-system-schema-future-foundation) | 📋 Specification |

### Technical Systems
**Location:** `Features/Technical/`

| Document | Topic | Status |
|----------|-------|--------|
| [conflict-detection-system.md](Features/Technical/conflict-detection-system.md) | Conflict detection and prevention: event cooldown tracking, mutually exclusive events, resource conflicts (gold/items), state validation, anti-spam protections | ✅ Current |
| [commander-track-schema.md](Features/Technical/commander-track-schema.md) | Commander tracking schema: save data structure for player progress, persistence patterns, serialization rules | 📋 Specification |
| [encounter-safety.md](Features/Technical/encounter-safety.md) | Encounter safety patterns: null checks, save/load safety, state validation, edge case handling, external interruptions (battles, captures) | ✅ Current |

### UI Systems
**Location:** `Features/UI/`

| Document | Topic | Status |
|----------|-------|--------|
| [README.md](Features/UI/README.md) | UI systems overview | ✅ Current |
| [ui-systems-master.md](Features/UI/ui-systems-master.md) | Complete UI reference: all menus, screens, and interfaces (camp menu, muster menu, QM interfaces, equipment grids, dialogue flows), Gauntlet implementation patterns, UI technical requirements | ✅ Current |
| [camp-hub-custom-gauntlet.md](Features/UI/camp-hub-custom-gauntlet.md) | Custom Gauntlet main hub: replaces `enlisted_status` GameMenu with custom layout (horizontal buttons, dynamic order cards, settlement access), all submenus stay native GameMenu, complete implementation spec with ViewModel/XML/Behavior code | 📋 Specification |
| [color-scheme.md](Features/UI/color-scheme.md) | Professional color palette: hex codes for all UI elements (backgrounds, text, buttons, status indicators), quality tier colors, reputation colors, accessibility considerations | ✅ Current |
| [news-reporting-system.md](Features/UI/news-reporting-system.md) | News feeds and Daily Brief: event logging, combat summaries, period recaps, notification system, Daily Brief UI (shows last 12 days of activity) | ✅ Current |

---

## Active Feature Development (AFEATURE)

**Location:** `AFEATURE/`

Active feature specifications being developed. These are implementation-ready specs that build on existing systems. Build in order as dependencies require.

| Document | Topic | Dependency | Status |
|----------|-------|------------|--------|
| [content-orchestrator-plan.md](AFEATURE/content-orchestrator-plan.md) | Content Orchestrator: replaces schedule-driven event pacing with world-state-driven frequency. Phases 1-5. | None | 📋 Specification |
| [content-orchestrator-prompts.md](AFEATURE/content-orchestrator-prompts.md) | Implementation prompts for Content Orchestrator and Camp Life. Copy-paste ready. | Reference only | 📋 Specification |
| [camp-life-simulation.md](AFEATURE/camp-life-simulation.md) | Living breathing camp simulation: 4-layer intelligence (world/camp/player/history), time-of-day cycles, weekly rhythm, learning system. Phase 6. | Content Orchestrator (Phases 1-5) | 📋 Specification |
| [order-progression-system.md](AFEATURE/order-progression-system.md) | Multi-day order execution replacing instant resolution. | Content Orchestrator (Phase 4) | 📋 Specification |
| [ORDER-SYSTEM-MIGRATION.md](AFEATURE/ORDER-SYSTEM-MIGRATION.md) | Migration guide for order system changes. | order-progression-system.md | 📋 Reference |
| [orders-content.md](AFEATURE/orders-content.md) | Order content definitions (16 orders). | order-progression-system.md | 📋 Specification |
| [order-events-master.md](AFEATURE/order-events-master.md) | Order event master reference (85 events across 16 orders). | order-progression-system.md | 📋 Specification |

**Implementation Order:**
1. Content Orchestrator Phases 1-5 (foundation)
2. Order Progression System (Phase 4 integration)
3. Camp Life Simulation (Phase 6)
4. Content Variants (Phase 7, JSON-only)

---

## Content & Narrative

**Location:** `Content/`

| Document | Purpose | Status |
|----------|---------|--------|
| [README.md](Content/README.md) | Content catalog overview and how to use content docs | ✅ Current |
| [content-index.md](Content/content-index.md) | Complete content catalog: all events (200+ events across march/camp/combat/retinue/baggage), all decisions (equipment/camp/quartermaster), all orders (16 orders with outcomes), all map incidents, JSON file locations | ✅ Current |
| [event-catalog-by-system.md](Content/event-catalog-by-system.md) | Events organized by system: lists every event ID with title, trigger conditions, outcomes, reputation effects, organized by feature area (Core/Equipment/Combat/Retinue/etc) for easy lookup | ✅ Current |

---

## Reference & Research

**Location:** `Reference/`

| Document | Purpose | Status |
|----------|---------|--------|
| [README.md](Reference/README.md) | Reference overview and how to use reference docs | ✅ Current |
| [native-apis.md](Reference/native-apis.md) | Campaign System API reference: Bannerlord API patterns, CampaignBehavior structure, common APIs (Hero, Party, Clan, Settlement), event hooks, save/load patterns - use for API verification against decompiled source | 📚 Reference |
| [native-skill-xp.md](Reference/native-skill-xp.md) | Skill progression reference: native XP rates per skill, skill learning caps, focus point effects, XP calculation formulas - use when implementing training/skill systems | 📚 Reference |
| [native-map-incidents.md](Reference/native-map-incidents.md) | Native game incidents: all vanilla map incidents (bandits, prisoners, travelers), trigger conditions, outcomes, loot tables - use to avoid conflicts with native content | 📚 Reference |
| [map-incidents-warsails.md](Reference/map-incidents-warsails.md) | Naval DLC map incidents: Warsails expansion content, naval encounters, coastal events - use to avoid DLC conflicts | 📚 Reference |
| [ai-behavior-analysis.md](Reference/ai-behavior-analysis.md) | AI behavior analysis: native AI decision-making patterns, party movement logic, combat AI, lord behavior - use for AI-aware feature design | 📚 Reference |
| [opportunities-system-spec.md](Reference/opportunities-system-spec.md) | ⚠️ **LEGACY** - Replaced by [Camp Life Simulation](AFEATURE/camp-life-simulation.md) | 🗄️ Deprecated |
| [camp-life-simulation.md](AFEATURE/camp-life-simulation.md) | **NEW** - Orchestrator-driven camp life: fitness scoring, learning system, 0-3 opportunities per open, order-decision tension, forecast system, cached UI sections | ✅ Active |
| [content-orchestrator-plan.md](AFEATURE/content-orchestrator-plan.md) | **NEW** - World-state-driven content delivery: replaces schedule-driven events with context-aware orchestration, frequency by situation (garrison quiet, siege intense) | ✅ Active |

---

## Status Legend

- ✅ **Current** - Actively maintained, reflects current implementation
- ⚠️ **In Progress** - Feature partially implemented, doc evolving
- ⚠️ **Planning** - Design document for future feature
- ⚠️ **Mixed** - Contains both completed and outdated/future content
- ❓ **Verify** - Implementation status needs verification
- 📦 **Archived** - Historical reference, no longer maintained
- ❌ **Deprecated** - Replaced by newer docs, kept for reference only

---

**Last reorganization:** 2025-12-22 (Phases 1-10 complete)


