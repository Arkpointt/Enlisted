# Enlisted Game Design Principles

**Purpose:** Design guidance for features that are both technically sound AND engaging for players.

## The Enlisted Fantasy

Enlisted is NOT a party management game. The player pretends to be a soldier in someone else's army.

**Native Bannerlord:** You're a lord. You manage parties, make strategic decisions.
**Enlisted Mod:** You're a grunt (T1-T6) or commander (T7-T9). You follow orders, experience camp life, work your way up.

The mod MUST interrupt native gameplay to create this alternate reality. But interruptions should feel authentic to soldier life.

## The Three Player Experiences

Content MUST be tier-aware. The same event feels different at different tiers:

### T1-T4: Enlisted Track (Grunt)
**"Things happen TO you"**
- Auto-assigned duties (guard, patrol, firewood)
- Witness company problems, limited agency to fix them
- Experience camp life as one soldier among many
- Player verbs: "Witness", "Report", "Participate", "Survive"

### T5-T6: Officer Track (NCO)
**"You handle your people"**
- Specialist missions with skill requirements
- NCO authority over recruits and squad
- Can train lord's T1-T3 troops
- Investigate theft, mentor recruits, handle disputes
- Player verbs: "Investigate", "Mentor", "Handle", "Lead small group"

### T7-T9: Commander Track
**"You command troops"**
- Strategic orders from lord directly
- Full command over 20-40 troop retinue
- Resource allocation, discipline decisions
- Retinue events: loyalty, casualties, named veterans
- Player verbs: "Order", "Allocate", "Discipline", "Petition", "Command"

## Tier-Aware Event Design

**RULE:** Major events need tier variants with appropriate player agency.

**Example - Supply Crisis Day 5:**
```
T1-T4: "Fights breaking out over rations. You see two men wrestling over bread."
  Options: Step in / Watch / Get the NCO

T5-T6: "Two of YOUR squad are fighting over rations. Others watch you."
  Options: Punish both / Mediate / Share your own ration

T7+: "Fighting in YOUR retinue over rations. Discipline is breaking down."
  Options: Harsh punishment (discipline) / Share provisions / Petition lord / Ignore
```

Same crisis, different player verbs based on authority level.

## Soldier-Eye View

The player doesn't read management reports. They LIVE the effects.

**WRONG:** "Display: Company Supply 35%"
**RIGHT:** "Thin gruel for dinner. Men grumble at the cook."

**WRONG:** "Notification: Morale is dropping"  
**RIGHT:** "Nobody meets your eyes in camp today."

**WRONG:** "Forecast: Crisis in 2 days"
**RIGHT:** "You overhear: 'Three more days of this and I'm gone.'"

## Non-Intrusive Visibility

This is an action game. Players fight battles, travel the map, make real-time decisions.
Content should NOT spam popups. Instead:

1. **Consolidate to natural break points** - Daily Brief, Camp Hub, Muster
2. **Use passive indicators** - colors, icons, tooltip enrichment
3. **Reserve events for milestones** - Day 3/5/7 pressure arcs, not constant drip
4. **Enable player-initiated discovery** - hover for details, read reports when ready

**Ask:** "Where does the player ALREADY check their state?"
**Answer:** Daily Brief, Camp Hub menu, character screen tooltips
**Put visibility there**, not in new popups.

## The "Would They Tell a Friend?" Test

For every feature, ask: "Would a player tell a friend about this moment?"

**YES:**
- "We were on Day 6 of starvation and I gave a speech that saved us"
- "I caught one of my men stealing and had to decide whether to cover for him"
- "I got promoted to Captain and now I have my own troops"

**NO:**
- "The game has gradient-based fitness modifiers"
- "There's a +1/day recovery bonus when my rep is high"
- "The synergy system compounds pressure calculations"

If it's the second kind, the feature is systems-elegant but not player-memorable.
Make invisible math visible through events and narrative.

## Positive and Negative Arcs

Don't only track bad things getting worse. Track good things too.

**Negative Arc (Day 3/5/7 low supplies):**
- Day 3: "Rations are thin"
- Day 5: "Fights over scraps"
- Day 7: Desertion crisis

**Positive Arc (Day 3/5/7 high morale):**
- Day 3: "Men in good spirits"
- Day 5: "Songs around the campfire"
- Day 7: "Peak cohesion" - bonus event or opportunity

Hero moments during crisis, not just consequences.

## Content Sizing by Tier

| Content Type | T1-T4 | T5-T6 | T7+ |
|--------------|-------|-------|-----|
| Orders | Auto-assigned basic tasks | Skill-gated specialist missions | Strategic directives |
| Events | Things happen TO you | Handle YOUR squad | Command decisions |
| Camp Life | One soldier among many | Respected NCO | Commander with retinue |
| Rations | Issued at muster | Issued at muster | Buy from QM shop |
| Battle | Auto-assigned to formation | Auto-assigned to formation | Control your party |

## Summary Checklist for Feature Design

Before finalizing any feature plan, verify:

- [ ] Does it have tier-appropriate variants (T1-T4 / T5-T6 / T7+)?
- [ ] Does it create player-memorable moments (would they tell a friend)?
- [ ] Does it show effects through narrative, not just math?
- [ ] Does it avoid constant popup interruptions?
- [ ] Does it include positive arcs, not just negative spirals?
- [ ] Does it give players agency during crisis, not just observation?
- [ ] Does it feel like soldier life, not management simulation?
