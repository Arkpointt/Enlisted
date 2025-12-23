# Enlisted Mod - Roadmap

**Summary:** Future development plans for the Enlisted mod, organized by priority and estimated effort. This roadmap represents planned enhancements beyond the current feature set.

**Status:** ⚠️ Planning  
**Last Updated:** 2025-12-23  
**Current Version:** v0.9.0  
**Target Game:** Bannerlord v1.3.11

**Recent Completions (2025-12-23):**
- ✅ Retinue System Complete (Phases 0-8): Formation selection, context-aware trickle, loyalty tracking, 11 events, 6 incidents, 4 decisions, named veterans
- ✅ Retinue Phase 8: Named Veterans - Emergence after 3+ battles, death detection, memorial events
- ✅ Retinue Phase 7: Camp Hub Decisions - 4 retinue decisions (Inspect, Drill, Share Rations, Address Men)
- ✅ Retinue Phase 6: Post-Battle Incidents - 6 map incidents for T7+ commanders (wounded soldier, first command, cowardice, looting, prisoner mercy, recognition)
- ✅ Phase 9: First Meeting Introduction - Player chooses relationship tone, 28 archetype×tone acknowledgments
- ✅ Phase 8: Provisions Gauntlet UI - Visual grid for food item purchases with Buy 1/Buy All
- ✅ Phase 7: Inventory & Pricing System - Stock tracking, supply/rep pricing, muster refresh
- ✅ Phase 6: Sell Reputation Gating - Reputation-based sell access with under-the-table dialogue
- ✅ Phase 5: Tier Gate RP Responses - 30+ archetype variants for tier/rep restrictions
- ✅ Phase 4: Hub Greeting Variants - 5 distinct supply-aware greetings with real-time evaluation
- ✅ Phase 3: Context-Aware Browse Responses - QM reacts to supply levels before store opens
- ✅ Option B Architecture: Dynamic runtime context checking via delegates (fixes stale dialogue)
- ✅ Quartermaster Conversation Refactor: Full JSON/XML data-driven infrastructure

---

## Index

1. [Vision](#vision)
2. [Recent Completions](#recent-completions)
3. [Near-Term (Next Release)](#near-term-next-release)
4. [Medium-Term (Future Releases)](#medium-term-future-releases)
5. [Long-Term (Major Enhancements)](#long-term-major-enhancements)
6. [Modding Support](#modding-support)
7. [Technical Debt](#technical-debt)

---

## Recent Completions

### Quartermaster First Meeting Introduction - Phase 9 (2025-12-23)

**Status:** ✅ Complete  
**Build:** 0 errors

First-meeting introduction system where the QM asks the player's name and the player chooses a relationship tone.

**Key Features:**
- **Archetype Greetings:** 7 unique intro greetings per QM archetype (veteran, merchant, bookkeeper, scoundrel, believer, eccentric, default)
- **Player Tone Selection:** 4 tone options with tooltips showing reputation impact
  - Direct: "I'm {PLAYER_NAME}. Just tell me what you've got." (-2 rep)
  - Military: "{PLAYER_NAME}, reporting for kit." (0 rep)
  - Friendly: "Name's {PLAYER_NAME}. Looking forward to working with you." (+3 rep)
  - Flattering: "They call me {PLAYER_NAME}. I've heard you're the one to know." (+5 rep)
- **28 Acknowledgments:** Each tone × archetype combination has unique QM response
- **Style Persistence:** Player's chosen style stored in `_qmPlayerStyle` for future dialogue flavor
- **Flag Persistence:** `_hasMetQuartermaster` flag persists across saves via SyncData

**New Files:**
- `ModuleData/Enlisted/Dialogue/qm_intro.json` (35 dialogue nodes)

**Modified Files:**
- `ModuleData/Languages/enlisted_qm_dialogue.xml` (+34 localization strings)
- `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` (player style storage)
- `src/Features/Conversations/Behaviors/EnlistedDialogManager.cs` (intro flow + set_player_style action)

---

### Quartermaster Provisions UI - Phase 8 (2025-12-23)

**Status:** ✅ Complete  
**Build:** 0 errors

Visual grid UI for food item purchases, matching the equipment interface style.

**Key Features:**
- **Gauntlet Grid UI:** Food items displayed in visual cards with icons, prices, quantities
- **Tier-Based Behavior:** T1-T6 see ration status info, T7+ see full provisions shop
- **Buy 1 / Buy All:** Quantity selection for bulk purchases
- **Stock Tracking:** Integrated with Phase 7 QMInventoryState (12-day muster refresh)
- **QM Rep Pricing:** 1.5× (Trusted) to 2.2× (Hostile) markup on base prices
- **Supply Pricing:** +10% to +50% markup at low supply levels
- **Out-of-Stock UI:** Greyed cards, disabled buttons, restock info

**New Files:**
- `src/Features/Equipment/UI/QuartermasterProvisionsVM.cs`
- `src/Features/Equipment/UI/QuartermasterProvisionItemVM.cs`
- `src/Features/Equipment/UI/QuartermasterProvisionsBehavior.cs`
- `GUI/Prefabs/Equipment/QuartermasterProvisionsGrid.xml`
- `GUI/Prefabs/Equipment/QuartermasterProvisionCard.xml`

---

### Quartermaster Inventory System - Phase 7 (2025-12-23)

**Status:** ✅ Complete  
**Build:** 0 errors

Stock tracking and pricing system for quartermaster commerce.

**Key Features:**
- **Muster-Based Refresh:** Inventory regenerates every 12 days at muster
- **Supply-Based Stock:** Variety (25%-100%) and quantity (1-5) scale with supply level
- **Combined Pricing:** Supply scarcity (+10% to +50%) × QM rep (-15% to +25%)
- **Stock Tracking:** `QMInventoryState` persists across saves
- **Out-of-Stock UI:** Cards greyed out, buttons disabled, "Restocks at muster" hint

---

### Quartermaster Conversation Refactor - Phase 6 (2025-12-23)

**Status:** ✅ Complete  
**Build:** 0 warnings, 0 errors

Dynamic contextual dialogue system for the Quartermaster with full localization support:

**Key Features:**
- **Contextual Responses:** Dialogue varies based on supply levels, reputation, mood, archetype, and strategic context
- **6 Archetype Personalities:** Veteran, Merchant, Bookkeeper, Scoundrel, Believer, Eccentric (each with unique voice)
- **Dynamic Supply Reports:** Real-time awareness of company needs integrated into dialogue
- **XML Localization:** ~150 dialogue strings moved to XML for full translation support
- **Edge Case Hardening:** Comprehensive validation, error handling, and fallback strings ensure bulletproof operation

**Technical Achievements:**
- Safe string loading with automatic fallbacks
- Input validation (null checks, archetype validation, value clamping)
- Exception handling that logs errors without breaking conversations
- Compatible with .NET Framework 4.x (Bannerlord runtime)

**Documentation:** See [Quartermaster System](Features/Equipment/quartermaster-system.md) for complete system documentation.

---

## Vision

The Enlisted mod aims to create an **emergent, choice-driven military experience** where:

- **Identity emerges** from choices, traits, and reputation
- **Orders from chain of command** provide structure and meaningful missions
- **Strategic context** makes missions feel connected to actual campaign plans
- **Rank progression** (T1-T9) gates authority and responsibility
- **Reputation matters** with lord, officers, and soldiers
- **Company-wide consequences** make actions impact the entire unit

**Design Philosophy:**
- Integrate with native Bannerlord systems
- Avoid custom UI where possible (use native game menus)
- Data-driven content (JSON + XML)
- Emergent gameplay over prescribed systems

---

## Near-Term (Next Release)

### Retinue System

**Status:** ✅ Complete (All Phases 0-8)  
**Effort:** ~60 hours total (completed 2025-12-23)  
**Docs:** [retinue-system.md](Features/Core/retinue-system.md)

Complete retinue system for T7+ commanders with formation selection, context-aware reinforcements, loyalty tracking, narrative content, and named veterans.

**✅ Completed Phases:**
- Phase 0: Critical Bug Fixes (tier cap, notification text, comment corrections, proving events)
- Phase 1: Formation Selection (dialog at T7, culture restrictions, persistence)
- Phase 2: Replenishment System (context-aware trickle, relation-based requests, post-battle volunteer event)
- Phase 3: Effect Infrastructure (retinueLoyalty, retinueLoss, retinueWounded, retinueGain effects)
- Phase 4: Retinue Loyalty Track (0-100 scale, threshold events, modifiers)
- Phase 5: Retinue Events (10 narrative events in events_retinue.json)
- Phase 6: Post-Battle Incidents (6 map incidents in incidents_retinue.json)
- Phase 7: Camp Hub Decisions (4 retinue decisions: Inspect, Drill, Share Rations, Address Men)
- Phase 8: Named Veterans (emergence after 3+ battles, death detection, memorial events, 5 max)
- News Integration: Personal Feed, Daily Brief, casualty reports, veteran activities

**Content Added:**
- 11 retinue events (10 narrative + 1 post-battle volunteer)
- 6 post-battle map incidents
- 4 camp hub decisions
- Named veteran system with emergence/death mechanics
- 100+ localization strings

---

---

### Strategic Context Enhancement

**Status:** In Progress (Phase 5.6)  
**Effort:** ~15 hours

Enhance existing features to understand strategic intent from vanilla AI:

**What:**
- Read Bannerlord's AI strategic data (party decisions, objectives)
- Make orders reflect lord's actual strategic plans
- Make reports explain WHY behind tactical WHAT
- Predict company needs based on upcoming operations

**Impact:**
```
Before: "Scout the area" (generic task)
After: "Scout approaches to Pravend - Lord planning offensive. 
        Your intelligence will inform assault plans."
```

**Files Affected:**
- `ArmyContextAnalyzer.cs` - Strategic intent detection
- `OrderCatalog.cs` - Strategic context filtering
- `EnlistedNewsBehavior.cs` - Strategic narratives
- `CompanyNeedsManager.cs` - Predictive needs

### UI Polish

**Effort:** ~5 hours

- Add tooltips to all menu options
- Improve status display formatting
- Add color coding for reputation/escalation
- Keyboard shortcuts for common actions

---

## Medium-Term (Future Releases)

### Baggage Train Availability System

**Status:** 📋 Specification Complete  
**Effort:** ~50 hours (9 phases)  
**Spec:** [baggage-train-availability.md](Features/Equipment/baggage-train-availability.md)

Transform the baggage stash from always-accessible storage into a realistic logistics element with conditional access based on march state, rank, and strategic context.

**Phase 1-3: Core System** (18 hours)
- Create `BaggageTrainManager` with priority-based access resolution
- Define `BaggageAccessState` enum (FullAccess, TemporaryAccess, NoAccess, Locked)
- Gate Camp Hub menu options with contextual tooltips
- Add "Request Baggage Access" emergency option (T3+)
- Periodic "baggage caught up" events (~25%/day on march)
- Muster and night halt access windows

**Phase 4: Baggage Events** (10 hours)
- `evt_baggage_arrived` - positive access window
- `evt_baggage_delayed` - weather/terrain delays
- `evt_baggage_raided` - combat/loss in enemy territory
- `evt_baggage_theft` - item loss when soldier rep low

**Phase 5-6: Progression & News** (7 hours)
- Rank-gated access privileges (T1-T2: limited, T7+: full control)
- Daily Brief includes baggage status
- Personal feed shows baggage events

**Phase 7-8: Cross-System Integration** (11 hours)
- Leave system: FullAccess when on leave
- Grace period: FullAccess, preserve state on re-enlist
- Combat/Reserve: NoAccess during battle or reserve mode
- Capture: Freeze delay timer during captivity
- Siege: Context-aware access (defender vs. attacker)
- Discharge: Forfeit baggage on desertion, reclaim QM items on dishonorable
- Retinue/Promotion: Queue formation selection if baggage delayed

**Phase 9: Polish & QA** (4 hours)
- QM conversation about baggage status
- Performance optimization (cached state with dirty flag)
- Comprehensive edge case testing

**Gameplay Impact:**
- Preparation pressure before campaigns
- Strategic decisions about what to store vs. carry
- Events add risk/reward to storage
- Rank progression provides meaningful privileges
- Consequences for desertion (baggage forfeiture)

---

### Advanced Order System

**Effort:** ~20 hours

**Conditional Order Chains:**
- Next order depends on previous outcome
- Example: "Complete patrol → IF enemies spotted → THEN recon mission"

**Multi-Objective Orders:**
- Complex missions with multiple success conditions
- Partial success/failure states
- Example: "Scout + Capture prisoner + Return undetected"

**Timed Orders:**
- Must respond within X hours or auto-decline
- Creates urgency and meaningful choices
- "Lord needs answer now - accept or refuse?"

**Squad Orders (T7+):**
- Lead retinue on group missions
- Manage multiple soldiers simultaneously
- Delegate sub-tasks

### Culture-Specific Content

**Effort:** ~30 hours

**Culture-Specific Orders:**
- Vlandian: Knightly tournaments, feudal duties
- Battanian: Forest warfare, ambush tactics
- Aserai: Desert navigation, caravan protection
- Empire: Legion discipline, fortification
- Sturgian: Cold weather operations, river combat
- Khuzait: Horse archery, steppe raiding

**Culture-Specific Events:**
- Cultural traditions and customs
- Different military structures
- Unique challenges per faction

### Content Expansion

**Effort:** ~40 hours

**New Role Content:**
- Siege engineer orders (engineer role)
- Diplomatic missions (operative role, T8+)
- Training orders (NCO role, T6+)
- Supply chain management (steward role)

**New Event Categories:**
- Pre-battle tension events
- Post-battle aftermath events
- Long-term relationship events
- Political intrigue events

**New Decision Types:**
- Advanced combat training
- Leadership development
- Specialized skill training
- Social manipulation

---

## Long-Term (Major Enhancements)

### Advanced Strategic AI

**Effort:** ~80 hours  
**Note:** Would modify vanilla AI behavior

**Faction Strategic Objectives:**
- Coordinated multi-lord objectives
- Faction-wide campaign plans
- Player as part of larger strategy

**Player Threat Response:**
- AI specifically counters player reputation/actions
- Adaptive difficulty
- "The enemy knows your name"

**HTN Multi-Step Planning:**
- AI lords with coherent multi-turn plans
- Predictable but intelligent behavior
- "Lord has a 3-step campaign strategy"

**Influence Mapping:**
- Spatial reasoning for strategic positioning
- Control zones and supply lines
- Territory importance calculations

**Risk Assessment:**
- AI evaluates risk vs reward
- Contextual decision-making
- "Lord retreats when odds poor"

**Reference:** See `docs/Reference/ai-behavior-analysis.md` for detailed analysis

### Dynamic Difficulty System

**Effort:** ~15 hours

**Skill Check Scaling:**
- Order skill checks adjust based on player level
- Higher level = harder challenges
- Maintains difficulty curve throughout game

**Context-Aware Difficulty:**
- Army size affects mission difficulty
- Strategic situation affects challenge
- Player reputation increases expectations

### Modding Support

**Effort:** ~25 hours

**Order Modding API:**
- Community-created orders
- JSON schema documentation
- Validation tools

**Event Modding API:**
- Custom events and decisions
- Content validation
- Localization support

**Trait Modding API:**
- Custom traits and progression
- Role definitions
- XP award patterns

---

## Modding Support

### Order Modding API

**Priority:** Medium  
**Effort:** ~10 hours

- Document JSON schema for orders
- Create order validation tool
- Example order templates
- Modder's guide documentation

### Event Modding API

**Priority:** Medium  
**Effort:** ~10 hours

- Document JSON schema for events/decisions
- Content validation tool
- Example event/decision templates
- Localization guide

### Trait Modding API

**Priority:** Low  
**Effort:** ~5 hours

- Document trait system
- Custom role definitions
- XP award patterns
- Integration examples

---

## Technical Debt

### Performance Optimization

**Priority:** Medium  
**Effort:** ~10 hours

- Cache strategic context calculations
- Optimize daily tick processing
- Reduce news feed memory usage
- Profile and optimize hot paths

### Code Cleanup

**Priority:** Low  
**Effort:** ~15 hours

- Consolidate duplicate code
- Improve naming consistency
- Extract magic numbers to config
- Add missing null checks

### Testing Infrastructure

**Priority:** Low  
**Effort:** ~20 hours

- Unit test framework
- Event validation tests
- Order requirement tests
- Regression test suite

### Documentation

**Priority:** High  
**Effort:** Ongoing

- Complete API documentation
- Update feature documentation
- Add inline code documentation
- Create modder's guide

---

## Contributing

We welcome contributions! Priority areas:

**High Priority:**
- Culture-specific orders/events
- Event content (always needed)
- Bug reports and fixes
- Documentation improvements

**Medium Priority:**
- Advanced order types
- New decision categories
- UI/UX enhancements
- Performance optimizations

**Low Priority:**
- New features (discuss first)
- Major refactoring
- API changes

**Contact:** See README.md for contribution guidelines

---

## Version History

**v0.9.0 (Current)**
- Core enlistment system
- Native game menu interface
- Trait-based identity system
- Orders system (17 orders across 3 tiers)
- Content system (events, decisions)
- Company needs simulation
- Quartermaster & provisions
- Training & XP system

**Planned v1.0.0**
- Strategic context enhancement
- UI polish
- Content expansion
- Documentation completion
- Modding API basics

**Planned v1.1.0**
- Advanced order system
- Culture-specific content
- Dynamic difficulty

**Planned v2.0.0**
- Advanced strategic AI (optional)
- Full modding support
- Performance optimization

---

## Notes

**Development Philosophy:**
- Features must enhance core gameplay loop
- Avoid feature creep - depth over breadth
- Integration with native systems preferred
- Performance matters (this is a large-scale game)
- Mod compatibility is important

**Not Planned:**
- Multiplayer support (out of scope)
- Total conversion (remains a mod)
- Custom models/assets (text-based experience)
- Savegame editing tools (use native system)

---

**End of Document**

