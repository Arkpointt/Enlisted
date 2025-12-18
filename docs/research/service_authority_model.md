# Service & Authority Model - Design Notes

**Date:** December 16, 2025  
**Status:** Foundational Design Document  
**Purpose:** Clarify ownership, command structure, and terminology for all Enlisted features

---

## Core Principle: You Serve, You Don't Own

The player is **enlisted in** the lord's party, not commanding it. This is the fundamental framing for all game systems and UI language.

### The Misconception to Avoid

X **WRONG:** "Your company" / "Your army" / "Manage your party"  
[x] **RIGHT:** "Serving in [Lord]'s party" / "Leadership assigns" / "Earn command authority"

---

## Authority Hierarchy

```
Lord/Commander
    │
    ├─-> Strategic Decisions (where party goes, who to fight)
    │   Player has ZERO input here
    │
    └─-> Leadership (AI or senior officers)
        │
        ├─-> Tactical Decisions (daily schedule, resource allocation)
        │   Player earns input here through progression
        │
        └─-> Player's Lance
            │
            ├─-> Lance Leader (player if T6+, or NPC if lower)
            │   Controls schedule IF player has authority
            │
            └─-> Lance Members (player's immediate unit)
                Player is one of these until T6+
```

---

## Terminology Dictionary

| Term | Meaning | Code Reference | UI Usage |
|------|---------|----------------|----------|
| **Lord's Party** | The `MobileParty` player enlisted in | `EnlistmentBehavior.EnlistedLord.PartyBelongedTo` | Technical docs only |
| **Party** | Short form, neutral | Used in tab names | "Party" tab (for NPC lances) |
| **Leadership** | Lord + senior officers (AI) | Abstract concept | "Leadership has assigned..." |
| **Command** | Authority to make decisions | `CanUseManualManagement()` | "In command of lance" (T6+) |
| **Lance** | Player's squad (8-12 troops) | `LanceBehavior` | "Your lance" (player's unit) |
| **Company** | Flavor text for lord's party | Military term | Rarely used, flavor only |

### What NOT to Use

| X Avoid | Why It's Wrong | [x] Use Instead |
|---------|----------------|----------------|
| "Your army" | Player doesn't own an army | "The lord's party you serve in" |
| "Your company" | Implies ownership | "Your lance" (for player's squad) |
| "Manage party" | Player can't manage lord's party | "Manage lance schedule" (if T6+) |
| "Command your troops" | T1-T4 can't command | "Follow assigned duties" |

---

## Schedule Ownership Model

### Who Controls the Schedule?

| Tier/Role | Schedule Set By | Player Authority | Daily Reset Behavior |
|-----------|-----------------|------------------|----------------------|
| **T1-T2** | AI/Lord | None | AI regenerates every dawn |
| **T3-T4** | AI/Lord | Request changes only | AI regenerates, player can request modifications |
| **T5-T6 (not Lance Leader)** | AI/Lord (with player input) | Guided control | AI recommends, player approves/modifies |
| **Lance Leader (T5-T6)** | Player | Full control | Player sets, persists until player changes |

**Note:** Lance Leader is a **promotion/role** that happens when there's a vacancy and you meet requirements. Not all T5-T6 players are Lance Leaders.

### Key Mechanics

1. **Daily Regeneration (T1-T5):**
   - Each dawn, AI analyzes lord's objective and lance needs
   - Generates new schedule for the day
   - T3-T4 requests don't carry over (must re-request)

2. **Manual Mode (Lance Leader):**
   - Once promoted to Lance Leader, player sets schedule and it persists
   - AI does NOT override unless player reverts to "auto mode"
   - `ScheduleBehavior.IsManualScheduleMode` = true
   - Applies whether Lance Leader is T5 or T6

3. **"In Command of the Lance" Unlocks:**
   - **Lance Leader is a ROLE/PROMOTION, not automatic with tier**
   - Can be promoted to Lance Leader at **T5 or T6** (when vacancy exists and requirements met)
   - Player can SET activities directly once promoted to Lance Leader
   - Player can access command-level camp activities
   - Player earns this through meeting promotion requirements (XP, days served, lance reputation, etc.)

---

## UI Language Guidelines

### Orders Tab (Schedule Management)

**T1-T2 Players:**
```
Today's Orders
Leadership has assigned your lance these duties:

Dawn: Morning Drill
Morning: Guard Duty
[...etc]

You cannot modify this schedule at your current rank.
```

**T3-T4 Players:**
```
Today's Orders
Leadership has assigned your lance these duties:

Dawn: Morning Drill -> [Request Change] (35% approval)
Morning: Guard Duty -> [Request Change] (50% approval)

[Approval based on lord relation, activity fit, and lance needs]
```

**T5-T6 Players (Not Lance Leader):**
```
Today's Orders
Recommended schedule based on leadership objectives:

Dawn: Morning Drill -> [Approve] [Change]
Morning: Guard Duty -> [Approve] [Change]

[AI explains why each activity is recommended]
```

**Lance Leader (T5 or T6):**
```
Lance Schedule (You are in command)
Set your lance's activities for today:

Dawn: Morning Drill -> [Set Activity]
Morning: Guard Duty -> [Set Activity]

[Full control, no restrictions, no approval rolls]
[Requires Lance Leader promotion - not automatic with tier]
```

### Activities Tab

**Activity Descriptions:**
- "Assigned activities" (T1-T4 viewing schedule-locked activities)
- "Available activities" (Activities player can choose from for their own time)
- "Command activities" (T5+ only activities, like "Brief Squad" or "Coordinate Patrols")

**Gating Messages:**
- X "You don't have permission to do this"
- [x] "Requires Lance Leader promotion"
- [x] "This activity is for lance leaders and officers"
- [x] "This activity is for senior NCOs and officers"

### Party Tab (T5+ Only)

**Tab Label:** "Party" or "Other Lances"

**Header:** "Lances in [Lord]'s Party"

**Tooltip:** "View status of other lances serving alongside yours. Available to senior NCOs and officers."

**Lance List Items:**
```
3rd Lance (Ironsides Warband)
Status: On Patrol | Readiness: 75% | Commander: Torvig
[View Details]
```

---

## Authority Progression Design

### Tier 1-2: Following Orders
- **Player Experience:** "I'm a soldier, I do what I'm told"
- **Schedule:** View only, no input
- **Activities:** Basic (Rest, Medical, Training)
- **Story Context:** New recruit, learning the ropes

### Tier 3-4: Earning Respect
- **Player Experience:** "I can ask, but they decide"
- **Schedule:** Request changes (approval roll)
- **Activities:** Squad leader tasks (Check Equipment, Inspect Lance)
- **Story Context:** Veteran soldier, some initiative allowed

### Tier 5-6: Senior NCO / Officer (Not Yet Lance Leader)
- **Player Experience:** "They trust my judgment, but guide me"
- **Schedule:** AI recommends, player approves/modifies
- **Activities:** Leadership tasks (Brief Squad, Review Reports)
- **Story Context:** Lance Second or senior NCO, training for command
- **Party Tab:** Unlocked (see peer lances)
- **Promotion Path:** Eligible for Lance Leader promotion when vacancy occurs

### Lance Leader (Promotion at T5 or T6)
- **Player Experience:** "I lead this lance, I make the calls"
- **Schedule:** Full control
- **Activities:** Command tasks (Strategic Planning, Coordinate)
- **Story Context:** Lance Leader, tactical autonomy
- **Party Tab:** Can see and coordinate with other lances
- **Note:** This is a **role/promotion**, not automatic with tier. Requires:
  - Vacancy in lance leadership position
  - Meeting promotion requirements (XP, days served, lance reputation, discipline, skills)
  - Completing proving event (if applicable)

---

## Implementation Notes

### Data Model Changes Needed

**ScheduleActivityDefinition.cs:**
```csharp
public class ScheduleActivityDefinition
{
    // Existing fields...
    public string ActivityId { get; set; }
    public string TitleKey { get; set; }
    public string DescriptionKey { get; set; }
    
    // NEW: Tier gating
    public int MinTier { get; set; } = 1;  // Default: available to all
    public int MaxTier { get; set; } = 6;  // Default: available to all
    
    // NEW: Display text (if not using localization keys)
    public string Title { get; set; }
    public string Description { get; set; }
}
```

**schedule_config.json:**
```json
{
  "activityId": "brief_squad",
  "titleKey": "str_activity_brief_squad",
  "descriptionKey": "str_activity_brief_squad_desc",
  "minTier": 5,
  "maxTier": 6,
  "activityType": "LeadershipBrief",
  "...": "..."
}
```

### ViewModel Filtering

**CampScheduleVM.cs:**
```csharp
private void RefreshAvailableActivities()
{
    int playerTier = EnlistmentBehavior.Instance.EnlistmentTier;
    
    var filtered = ScheduleBehavior.Instance.Config.Activities
        .Where(a => a.MinTier <= playerTier && a.MaxTier >= playerTier)
        .Select(a => new ActivityItemVM(a))
        .ToList();
    
    AvailableActivities = new MBBindingList<ActivityItemVM>(filtered);
}
```

**CampManagementVM.cs (Party tab visibility):**
```csharp
public bool IsPartyTabVisible
{
    get => EnlistmentBehavior.Instance.EnlistmentTier >= 5;
}
```

---

## Framing for Story Events

### Promotion to Lance Leader (T5 or T6)
```
[Lord's Name] calls you to his tent.

"You've proven yourself. I'm putting you in command of your lance.
From now on, you decide their daily duties. Don't make me regret this."

YOU ARE NOW LANCE LEADER
- Set your lance's daily schedule
- Access command-level activities
- Coordinate with other lance leaders
- Note: This promotion can occur at T5 or T6 when a vacancy exists
```

### Demotion from Lance Leader (if implemented)
```
[Lord's Name] summons you.

"Your recent decisions have been... questionable. I'm reassigning 
command until you prove you're ready."

LANCE LEADER AUTHORITY REVOKED
- Leadership will set your schedule again
- Focus on rebuilding trust
- Note: You keep your tier (T5/T6), but lose Lance Leader role
```

---

## FAQ for Future Development

**Q: Can the player ever own a party/company?**  
A: Not in this mod's scope. This is about enlisted service, not independent command. If player wants that, they leave enlistment and become an independent lord (vanilla Bannerlord).

**Q: Why not call it "Company" everywhere?**  
A: "Company" implies a self-contained unit. The lord's party is larger and includes multiple lances. "Party" is Bannerlord's neutral term. "Company" can be flavor text but not technical.

**Q: What if player is Lance Leader but then gets demoted?**  
A: `CanUseManualManagement()` should check Lance Leader role, not just tier. If demoted from Lance Leader role, manual mode disabled, AI resumes control. Player keeps their tier (T5/T6) but loses schedule authority. Player's custom schedule is saved but overridden until re-promoted to Lance Leader.

**Q: Can a T5 be Lance Leader?**  
A: Yes! Lance Leader is a role/promotion that can happen at T5 or T6 (when there's a vacancy and you meet requirements). Not all T5/T6 players are Lance Leaders - it's about the position, not just the tier.

**Q: How do I become Lance Leader?**  
A: You must:
1. Reach T5 or T6 tier
2. Meet promotion requirements (XP threshold, days served, lance reputation, low discipline, skill requirements, leader relation)
3. Wait for a vacancy in the Lance Leader position (current leader promoted, transferred, injured, killed, or retired)
4. Complete proving event (if applicable)
See `docs/Features/Gameplay/lance-life.md` and `docs/Features/Core/enlistment.md` for the Lance Leader/progression pieces that are implemented.

**Q: Can player refuse orders at T1-T4?**  
A: Not in this version. That's a future feature (disciplinary system). For now, schedule is informational and contextual, not enforced.

**Q: Does the lord ever acknowledge player's schedule decisions?**  
A: Future feature. For v0.7.0-v0.9.0, focus on mechanical implementation. Lord reaction system can come in v1.1.0+.

---

## Related Documents

- `/docs/ImplementationPlans/KINGDOM_STYLE_CAMP_SCREEN.md` - Full UI implementation plan
- `docs/Features/core-gameplay.md` - Schedule system pointers (code + data locations)
- `/ROADMAP.md` - Version planning with authority progression

---

**Last Reviewed:** December 16, 2025  
**Reviewed By:** AI Assistant + User Clarification  
**Status:** Canonical - use this as reference for all future work

