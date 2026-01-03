# Enlisted Mod - Development Roadmap

**Summary:** Future development priorities and planned enhancements.

**Status:** ⚠️ Planning  
**Last Updated:** 2025-12-24  
**Current Version:** v0.9.0  
**Target Game:** Bannerlord v1.3.13

**Recent Updates:**
- Completed Baggage Train Daily Brief integration with severity-based news system (2025-12-24)
- Added Muster Menu System Revamp specification (2025-12-24)

---

## Index

1. [Vision](#vision)
2. [In Progress](#in-progress)
3. [Near-Term](#near-term)
4. [Medium-Term](#medium-term)
5. [Long-Term](#long-term)
6. [Technical Debt](#technical-debt)

---

## Vision

The Enlisted mod creates an **emergent, choice-driven military experience** where:

- **Identity emerges** from choices, traits, and reputation
- **Orders from chain of command** provide structure and meaningful missions
- **Strategic context** makes missions feel connected to campaign plans
- **Rank progression** (T1-T9) gates authority and responsibility
- **Reputation matters** with lord, officers, and soldiers
- **Company-wide consequences** make actions impact the entire unit

**Design Philosophy:**
- Integrate with native Bannerlord systems
- Avoid custom UI where possible (use native game menus)
- Data-driven content (JSON + XML)
- Emergent gameplay over prescribed systems

---

## In Progress

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

## Near-Term

### Muster Menu System Revamp

**Status:** 📋 Specification Complete  
**Effort:** ~30 hours (implementation + testing)  
**Spec:** [muster-menu-revamp.md](Features/Core/muster-menu-revamp.md)

Replace the single-popup pay muster with a multi-stage GameMenu sequence that creates a comprehensive muster experience with period reporting, rank progression display, and integrated events.

**Core Implementation** (20 hours)
- Create `MusterMenuHandler.cs` with 6-stage menu flow
- Implement `MusterSessionState` for tracking outcomes
- Convert recruit event to menu stage
- Build promotion recap system (acknowledges promotions that occurred during period)
- Integrate strategic context, orders summary, health/fatigue status
- Add T7+ retinue muster stage with casualties report
- Generate comprehensive muster reports for news feed

**Key Features:**
- **6-Stage Flow:** Intro → Pay → Recruit → Promotion Recap → Retinue (T7+) → Complete
- **Period Summary:** Shows events, battles, XP sources, orders completed since last muster
- **Rank Progression:** Current rank, XP progress, next rank requirements, promotion acknowledgment
- **Event Integration:** Recruit mentoring as menu stage (not popup)
- **Post-Muster Actions:** Visit Quartermaster, Review Records, Request Temporary Leave
- **News Recording:** Full muster outcomes saved to personal feed for historical review

**Testing & Polish** (10 hours)
- Edge case handling (multi-tier promotions, no contraband, skip conditions)
- Save/load mid-muster state restoration
- Localization (~50 new string entries)
- Performance optimization (lazy-load data queries)
- Comprehensive acceptance criteria verification

**Gameplay Impact:**
- Muster feels like a formal military event, not a quick popup
- Player sees comprehensive service record at each muster
- All muster-related events occur in one cohesive sequence
- Rank progression clearly displayed with path forward
- Strategic context connects muster to ongoing operations

**Migration:** Feature flag allows parallel operation with old system during testing phase

---

### Baggage Train Availability System

**Status:** ⏸️ Paused (Phases 1-6 Complete, 7-9 Deferred)  
**Effort:** ~50 hours (9 phases)  
**Spec:** [baggage-train-availability.md](Features/Equipment/baggage-train-availability.md)

Transform the baggage stash from always-accessible storage into a realistic logistics element with conditional access based on march state, rank, and strategic context.

**✅ Phase 1-3: Core System** (COMPLETE - 18 hours)
- ✅ Create `BaggageTrainManager` with priority-based access resolution
- ✅ Define `BaggageAccessState` enum (FullAccess, TemporaryAccess, NoAccess, Locked)
- ✅ Gate Camp Hub menu options with contextual tooltips
- ✅ Add "Request Baggage Access" emergency option (T3+)
- ✅ Periodic "baggage caught up" events (~25%/day on march)
- ✅ Muster and night halt access windows

**✅ Phase 4: Baggage Events** (COMPLETE - 10 hours)
- ✅ `evt_baggage_arrived` - positive access window
- ✅ `evt_baggage_delayed` - weather/terrain delays
- ✅ `evt_baggage_raided` - combat/loss in enemy territory
- ✅ `evt_baggage_theft` - item loss when soldier rep low

**✅ Phase 5-6: Progression & News** (COMPLETE - 7 hours)
- ✅ Rank-gated access privileges (T1-T2: limited, T7+: full control)
- ✅ Daily Brief includes baggage status with raid/arrival tracking
- ✅ Personal feed shows baggage events with severity-based priorities
- ✅ News system: Color-coded events (green/yellow/red/critical)
- ✅ News system: Duration-based persistence (6h/12h/24h/48h by severity)
- ✅ News system: Higher severity prevents replacement by lower priority items

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

## Medium-Term

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

## Long-Term

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

## Development Philosophy

**Core Principles:**
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
