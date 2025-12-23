# Enlisted Mod - Roadmap

**Summary:** Future development plans for the Enlisted mod, organized by priority and estimated effort. This roadmap represents planned enhancements beyond the current feature set.

**Status:** ⚠️ Planning  
**Last Updated:** 2025-12-23  
**Current Version:** v0.9.0  
**Target Game:** Bannerlord v1.3.11

**Recent Completions (2025-12-23):**
- ✅ Quartermaster Conversation Refactor Phase 6: Contextual dialogue with XML localization and edge case hardening

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

