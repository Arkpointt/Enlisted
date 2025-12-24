# Enlisted Documentation - Complete Index

**Summary:** Master index of all documentation files organized by category. Use this to find documentation for specific topics or systems.

**Last Updated:** 2025-12-24  
**Total Documents:** 42

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
2. [Features](#features)
3. [Content & Narrative](#content--narrative)
4. [Research & Reference](#research--reference)

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
| [core-gameplay.md](Features/Core/core-gameplay.md) | Complete gameplay overview | ✅ Current |
| [enlistment.md](Features/Core/enlistment.md) | Enlistment system mechanics | ✅ Current |
| [orders-system.md](Features/Core/orders-system.md) | Orders from chain of command: 17 orders across 3 tiers, strategic filtering, success/failure resolution | ✅ Current |
| [promotion-system.md](Features/Core/promotion-system.md) | Rank progression T1-T9: XP sources, multi-factor requirements (days/battles/reputation/discipline), proving events, culture-specific ranks, equipment unlocks | ✅ Current |
| [pay-system.md](Features/Core/pay-system.md) | Wages and payment | ✅ Current |
| [muster-menu-revamp.md](Features/Core/muster-menu-revamp.md) | Multi-stage muster menu: rank progression display, period summary, event integration, comprehensive reporting | 📋 Specification |
| [company-events.md](Features/Core/company-events.md) | Company-wide events | ✅ Current |
| [retinue-system.md](Features/Core/retinue-system.md) | Commander's retinue (T7+): formation selection, context-aware trickle, relation-based reinforcements, loyalty tracking, 11 narrative events, 6 post-battle incidents, 4 camp decisions, named veterans | ✅ Current |
| [companion-management.md](Features/Core/companion-management.md) | Companion integration | ✅ Current |
| [camp-fatigue.md](Features/Core/camp-fatigue.md) | Rest and fatigue system | ✅ Current |
| [onboarding-discharge-system.md](Features/Core/onboarding-discharge-system.md) | Onboarding and discharge | ✅ Current |

### Equipment & Logistics
**Location:** `Features/Equipment/`

| Document | Topic | Status |
|----------|-------|--------|
| [quartermaster-system.md](Features/Equipment/quartermaster-system.md) | Complete quartermaster system: JSON dialogue, first-meeting intro, equipment quality, upgrades, provisions UI, tier gates, sell gating | ✅ Current |
| [provisions-rations-system.md](Features/Equipment/provisions-rations-system.md) | Food and rations (T1-T4 issued, T5+ provisions shop) | ✅ Current |
| [company-supply-simulation.md](Features/Equipment/company-supply-simulation.md) | Company supply tracking and effects (core implemented, enhancements planned) | ⚠️ Mixed |
| [baggage-train-availability.md](Features/Equipment/baggage-train-availability.md) | Baggage train access gating based on march state, rank, and context | 📋 Specification |

### Identity & Traits
**Location:** `Features/Identity/`

| Document | Topic | Status |
|----------|-------|--------|
| [README.md](Features/Identity/README.md) | Identity folder overview | ✅ Current |
| [identity-system.md](Features/Identity/identity-system.md) | Trait and identity system | ✅ Current |

### Combat & Training
**Location:** `Features/Combat/`

| Document | Topic | Status |
|----------|-------|--------|
| [README.md](Features/Combat/README.md) | Combat folder overview | ✅ Current |
| [training-system.md](Features/Combat/training-system.md) | Training and XP system | ✅ Current |
| [formation-assignment.md](Features/Combat/formation-assignment.md) | Battle formation logic | ✅ Current |

### Campaign & World
**Location:** `Features/Campaign/`

| Document | Topic | Status |
|----------|-------|--------|
| [README.md](Features/Campaign/README.md) | Campaign folder overview | ✅ Current |
| [camp-life-simulation.md](Features/Campaign/camp-life-simulation.md) | Camp activities | ✅ Current |
| [temporary-leave.md](Features/Campaign/temporary-leave.md) | Leave system | ✅ Current |
| [town-access-system.md](Features/Campaign/town-access-system.md) | Town access rules | ✅ Current |

### Content System
**Location:** `Features/Content/`

| Document | Topic | Status |
|----------|-------|--------|
| [README.md](Features/Content/README.md) | Content folder overview | ✅ Current |
| [content-system-architecture.md](Features/Content/content-system-architecture.md) | Content system design | ✅ Current |
| [event-system-schemas.md](Features/Content/event-system-schemas.md) | Event system JSON schemas | ✅ Current |
| [event-reward-choices.md](Features/Content/event-reward-choices.md) | Event reward system | 📋 Specification |
| [medical-progression-system.md](Features/Content/medical-progression-system.md) | CK3-style medical progression | 📋 Specification |

### Technical Systems
**Location:** `Features/Technical/`

| Document | Topic | Status |
|----------|-------|--------|
| [conflict-detection-system.md](Features/Technical/conflict-detection-system.md) | Conflict detection and prevention across mod systems | ✅ Current |
| [commander-track-schema.md](Features/Technical/commander-track-schema.md) | Commander tracking schema | 📋 Specification |
| [encounter-safety.md](Features/Technical/encounter-safety.md) | Encounter safety patterns | ✅ Current |

### UI Systems
**Location:** `Features/UI/`

| Document | Topic | Status |
|----------|-------|--------|
| [README.md](Features/UI/README.md) | UI systems overview | ✅ Current |
| [ui-systems-master.md](Features/UI/ui-systems-master.md) | Complete UI reference | ✅ Current |
| [color-scheme.md](Features/UI/color-scheme.md) | Professional color palette for menus | ✅ Current |
| [news-reporting-system.md](Features/UI/news-reporting-system.md) | News feeds and Daily Brief | ✅ Current |

---

## Content & Narrative

**Location:** `Content/`

| Document | Purpose | Status |
|----------|---------|--------|
| [README.md](Content/README.md) | Content catalog overview | ✅ Current |
| [content-index.md](Content/content-index.md) | Complete content catalog | ✅ Current |
| [event-catalog-by-system.md](Content/event-catalog-by-system.md) | Events organized by system | ✅ Current |

---

## Reference & Research

**Location:** `Reference/`

| Document | Purpose | Status |
|----------|---------|--------|
| [README.md](Reference/README.md) | Reference overview | ✅ Current |
| [native-apis.md](Reference/native-apis.md) | Campaign System API reference | 📚 Reference |
| [native-skill-xp.md](Reference/native-skill-xp.md) | Skill progression reference | 📚 Reference |
| [native-map-incidents.md](Reference/native-map-incidents.md) | Native game incidents | 📚 Reference |
| [map-incidents-warsails.md](Reference/map-incidents-warsails.md) | Naval DLC map incidents | 📚 Reference |
| [ai-behavior-analysis.md](Reference/ai-behavior-analysis.md) | AI behavior analysis | 📚 Reference |
| [opportunities-system-spec.md](Reference/opportunities-system-spec.md) | Automatic opportunities system | 📋 Specification |

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


