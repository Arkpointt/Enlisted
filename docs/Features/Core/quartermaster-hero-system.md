# Quartermaster Hero System

**Status**: Implemented (v3.0)  
**Category**: Core Feature  
**Dependencies**: Camp Life Simulation, Dialog System, Pay System

---

## Overview

The **Quartermaster Hero System** transforms equipment management from a menu-based transaction into an immersive character relationship. Each lord has a persistent NPC quartermaster with a unique personality, appearance, and dialogue that evolves with your service.

### Key Features
- **Persistent NPC**: Same quartermaster for your entire service with that lord
- **Multiple Archetypes**: Veteran, Merchant, Bookkeeper, Scoundrel, Believer, or Eccentric - each with unique dialogue
- **Relationship System**: Build trust (0-100) for discounts and special interactions
- **Mood Integration**: Reacts to camp conditions and pay tension
- **Provisions Shop**: Purchase rations for morale and fatigue benefits
- **Retinue Provisioning**: Feed your personal soldiers (T7+)
- **PayTension-Aware**: Offers archetype-specific advice during financial crisis

---

## Quartermaster Archetypes

Each lord's quartermaster is assigned one of several archetypes:
- Veteran
- Merchant
- Bookkeeper
- Scoundrel
- Believer
- Eccentric

### Veteran
**Personality**: Pragmatic old soldier, no-nonsense attitude  
**Dialogue Style**: Direct, practical, focused on survival  
**Backstory**: Lost their sword arm in battle, given quartermaster posting by lord  
**PayTension Advice**: Practical survival tips, warns about desertion risks  

**Sample Lines:**
- "Equipment requisition. Let me see what I've got."
- "Lost my sword arm at Pendraic. The lord gave me this posting. Beats starving."
- "Things are bad, I won't lie. Keep your head down and watch your purse."

### Merchant
**Personality**: Trade-minded and opportunistic  
**Dialogue Style**: Salesmanship, prices, “everything has a cost”  
**Backstory**: Former caravan/trader who “knows the market”  
**PayTension Advice**: Business-like warnings and “opportunities”

**Sample Lines:**
- "Everything here has a price, soldier. Even loyalty."
- "Prices are up. Supply issues. What do you need?"

### Bookkeeper
**Personality**: Bureaucratic clerk type, obsessed with forms and ledgers  
**Dialogue Style**: Dry, procedural, paperwork-driven  
**Backstory**: Former clerk/bureaucrat drafted into wartime logistics  
**PayTension Advice**: “The ledgers don’t balance” warnings

**Sample Lines:**
- "Please familiarize yourself with Form 14-C for equipment requisitions."
- "The accounts... they're a disaster."

### Scoundrel
**Personality**: Opportunistic, knows the black market  
**Dialogue Style**: Sly, hints at "creative solutions"  
**Backstory**: Disagreements with city guard, chose quartermaster duty over the noose  
**PayTension Advice**: Black market contacts, ways to make extra coin  

**Sample Lines:**
- "Official channels or... off the books?"
- "Let's just say I had some disagreements with the city guard."
- "Alternative supplies? I might know some people. For a cut, of course."

### Believer
**Personality**: Pious and moral, offers spiritual guidance  
**Dialogue Style**: Thoughtful, appeals to honor and faith  
**Backstory**: Former priest, now serves by feeding and arming the faithful  
**PayTension Advice**: Moral encouragement, suggests helping the lord  

**Sample Lines:**
- "The armory is open. May you choose wisely."
- "I served as a priest before the war. Faith without works is dead."
- "These are trying times, but faith endures. Remember why you serve."

### Eccentric
**Personality**: Superstitious and odd  
**Dialogue Style**: Omens, strange observations, unsettling humor  
**Backstory**: “The stars told me…” type, but still reliable at the job  
**PayTension Advice**: Ominous warnings framed as fate/fortune

**Sample Lines:**
- "The stars say you'll need good armor."
- "Dark omens everywhere. What?"

---

## Relationship System

Build trust with your quartermaster through interactions:

### Relationship Levels

| Level | Relationship | Discount | Features |
|-------|-------------|----------|----------|
| **Stranger** | 0-19 | 0% | Basic equipment access only |
| **Known** | 20-39 | 0% | Chat option unlocked |
| **Trusted** | 40-59 | **5%** | Black market hints, better dialogue |
| **Respected** | 60-79 | **10%** | Enhanced dialogue options |
| **Battle Brother** | 80-100 | **15%** | Special items, best treatment |

### Gaining Relationship
- **First Meeting**: +5 relationship
- **Chat Interaction**: +3 relationship
- **Equipment Purchase**: +1 relationship per transaction
- **Provisions Purchase**: +1 relationship
- **PayTension Dialog**: +2 to +5 (depends on option)

### Relationship Benefits
- **Discounts**: 5-15% off all equipment and provisions
- **Better Dialogue**: More personal and helpful responses
- **PayTension Options**: Access to archetype-specific advice and opportunities
- **Trust**: Quartermaster remembers your loyalty

---

## Provisions System

Purchase rations to boost morale and restore fatigue:

### Personal Rations

| Tier | Base Cost | Duration | Morale | Fatigue Recovery |
|------|------|----------|--------|------------------|
| **Supplemental Rations** | 10g | 1 day | +2 | None |
| **Officer's Fare** | 30g | 2 days | +4 | +2 (immediate) |
| **Commander's Feast** | 75g | 3 days | +8 | +5 (immediate) |

- **Stacking**: Only one rations tier active at a time
- **Expiration**: Benefits expire after duration
- **Fatigue**: Higher tiers grant immediate fatigue relief on purchase

### Retinue Provisioning (T7+)

Feed your personal soldiers to maintain morale:

| Tier | Base Cost per Soldier | Duration | Morale Effect |
|------|------------------|----------|---------------|
| **Bare Minimum** | 2g | 7 days | -5 morale |
| **Standard** | 5g | 7 days | No modifier |
| **Good Fare** | 10g | 7 days | +5 morale |
| **Officer Quality** | 20g | 7 days | +10 morale |

**Warning System:**
- Warning at **2 days** before expiration
- **Starvation penalties** if allowed to expire:
  - Severe morale penalty
  - Risk of desertions

---

## PayTension Integration

When pay is late (PayTension 40+), the quartermaster offers archetype-specific dialogue:

### Scoundrel: Black Market Access
**Unlocks at**: PayTension 40+  
**Effect**: +3 relationship, access to illicit trading opportunities  
**Dialogue**: "Looking for opportunities? I might know some people who can help... for a price."

### Believer: Moral Guidance
**Unlocks at**: PayTension 60+  
**Effect**: +2 fatigue, +5 relationship  
**Dialogue**: "These are trying times, but faith endures. Remember why you serve."

### Veteran: Survival Advice
**Unlocks at**: PayTension 80+  
**Effect**: Advice on desertion if applicable, +2 relationship  
**Dialogue**: "I've seen armies fall apart before. Keep your head down and watch for opportunities."

### Any Archetype: Help the Lord
**Unlocks at**: PayTension 40+  
**Effect**: +3 relationship, hints about loyalty missions  
**Dialogue**: "There are ways a loyal soldier could help. Collect debts, escort merchants, that sort of thing."

---

## Accessing the Quartermaster

### Menu Flow
1. **Enlisted Status** -> **Visit Quartermaster**
2. Opens **conversation** with quartermaster Hero
3. Choose from dialogue options:
   - "I need equipment" -> Opens equipment menu
   - "I want to sell some equipment" -> Opens sell menu
   - "I need better provisions" -> Opens rations menu
   - "How did you end up as quartermaster?" -> Chat (relationship +3)
   - PayTension options (if applicable)
   - "I'll be going" -> Ends conversation

### Equipment Menu Integration
- Dialogue seamlessly opens existing equipment menus
- No functional changes to equipment system
- Added layer of immersion and character

---

## Technical Implementation

### Core Files
- **Hero Creation**: `EnlistmentBehavior.cs` (GetOrCreateQuartermaster)
- **Dialog System**: `EnlistedDialogManager.cs` (AddQuartermasterDialogs)
- **Equipment Menu**: `QuartermasterManager.cs` (existing menu system)
- **Provisions**: `QuartermasterManager.cs` (AddRationsMenuOptions)
- **Relationship**: `EnlistmentBehavior.cs` (relationship methods)

### Persistence
- Quartermaster Hero stored in save data via `IDataStore.SyncData()`
- Relationship value persists across sessions
- Archetype assigned once and never changes
- Hero follows lord's party

### Hero Characteristics
- **Appearance**: Based on lord's culture sergeant-tier troops
- **Name**: Culture-appropriate random name
- **Equipment**: Tier 3-4 infantry gear from culture
- **Occupation**: Wanderer (non-combatant)
- **Age**: 30-50 years

### Cleanup
- Hero reference nulled on discharge/service end
- Hero remains with lord's party
- Can encounter same quartermaster if re-enlisting with same lord

---

## Player Experience

### First Meeting
1. Visit quartermaster from Enlisted Status menu
2. Opens dialogue instead of menu
3. Quartermaster introduces themselves based on archetype
4. Relationship starts at 0 (Stranger)
5. Player can chat to learn backstory (+5 relationship)

### Routine Interactions
1. Visit quartermaster for equipment/provisions
2. Brief archetype-appropriate greeting
3. Choose action (buy, sell, provisions, chat)
4. Gain +1 relationship per purchase
5. Build trust over time for discounts

### PayTension Crisis
1. Pay is late (tension 40+)
2. New dialogue options appear based on archetype
3. Quartermaster offers advice or opportunities
4. Player can explore corruption or loyalty paths
5. Relationship deepens through shared hardship

---

## Future Expansion Opportunities

- **Quartermaster Quests**: Special missions from trusted quartermaster
- **Black Market Trading**: Unlocked through Scoundrel relationship
- **Victory Feasts**: Post-battle celebrations offered by quartermaster
- **Promotion Ceremonies**: Quartermaster organizes formal promotion events
- **Quartermaster Stories**: Personal backstory events for high relationship
- **Contraband System**: Manage illicit goods through quartermaster
- **Cultural Variations**: Expanded archetype traits per culture

---

## Design Philosophy

### Immersion Over Efficiency
The quartermaster system prioritizes character and world-building over pure efficiency. Players who want fast equipment access can skip dialogue quickly, but those who engage are rewarded with:
- Discounts (up to 15%)
- Memorable character interactions
- Unique dialogue based on circumstances
- Sense of belonging in the army

### Respect Player Time
- **Quick Paths**: All dialogue offers direct menu access
- **No Forced Conversations**: Skip dialogue if desired
- **Functional First**: Equipment system unchanged
- **Rewards Engagement**: Benefits for those who invest in relationship

### Living World
The quartermaster reacts to:
- Camp conditions (via mood integration)
- Pay status (PayTension-aware dialogue)
- Player rank and reputation
- Duration of service
- Relationship history

---

## Configuration

No configuration file needed - system is fully integrated with:
- `enlisted_config.json` (camp_life.enabled must be true)
- `equipment_kits.json` (equipment availability)
- `equipment_pricing.json` (pricing structure)

---

## Related Systems

- [Equipment System](../../UI/quartermaster.md)
- [Camp Life Simulation](../../Gameplay/camp-life-simulation.md)
- [Pay System](pay-system.md)
- [Dialog System](../../UI/dialog-system.md)
- [Provisions (Food System)](../../Gameplay/provisions-system.md)
