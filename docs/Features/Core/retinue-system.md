# Retinue System — Commander's Personal Force

**Status**: Implemented (v3.0)  
**Category**: Core Feature  
**Dependencies**: Formation Training, Companion Management, Provisions System

## Overview

The retinue system provides enlisted players with a personal military force that grows with their rank. Companions can be managed from T1, and at T7+ (Commander track) players receive personal command of regular soldiers—starting with raw recruits that must be trained and developed through combat.

## System (v3.0)

- **T1 (All tiers)**: Companion management
- **T7 (Commander I)**: 15 raw recruits + companions
- **T8 (Commander II)**: 25 soldiers + companions
- **T9 (Commander III)**: 35 soldiers + companions
- Initial recruits granted automatically on promotion
- Reinforcements via trickle system (1 recruit every 2-3 days)
- Provisions system for feeding retinue (v3.0)

---

## Table of Contents

- [Quick Reference](#quick-reference)
- [Design Philosophy](#design-philosophy)
- [Companion Management (T1+)](#companion-management-t1)
- [Commander's Retinue (T7-T9)](#commanders-retinue-t7-t9)
- [Recruit Training System](#recruit-training-system)
- [Reinforcement & Trickle System](#reinforcement--trickle-system)
- [Battle Integration](#battle-integration)
- [Casualty Tracking](#casualty-tracking)
- [Implementation Phases](#implementation-phases)
- [Technical Details](#technical-details)
- [API Reference](#api-reference)

---

## Quick Reference

| Tier | Rank Track | Companions | Soldiers | Total Force | Grant Method |
|------|------------|------------|----------|-------------|--------------|
| T1-T6 | Enlisted/Officer | Managed | 0 | Companions only | N/A |
| T7 | Commander I | Managed | 15 | 15 + companions | Auto-grant raw recruits |
| T8 | Commander II | Managed | 25 | 25 + companions | +10 recruits on promotion |
| T9 | Commander III | Managed | 35 | 35 + companions | +10 recruits on promotion |

**Key Features:**
- Companions managed from day one (T1)
- Soldiers granted as raw recruits, not purchased
- Automatic reinforcement via trickle system (2-3 days per soldier)
- Formation-specific recruit types match player's specialization
- Training progression: Recruit -> Regular -> Veteran
- Battle participation toggle for companions
- Unified formation command in battles

---

## Design Philosophy

### Earned Command, Not Purchased Power

**Core Principles:**
1. **Progression Reward**: Commander rank grants personal force automatically
2. **Investment Required**: Raw recruits need time and battle experience to develop
3. **Strategic Choices**: Formation specialization determines recruit type
4. **Living Force**: Casualties matter, reinforcements take time
5. **Companion Value**: Early-game companions remain valuable throughout career

### Why Raw Recruits?

- **Authenticity**: Mirrors real military command progression
- **Challenge**: Managing green troops teaches player to protect their force
- **Satisfaction**: Watching recruits grow into veterans creates attachment
- **Balance**: Prevents instant powerful army; requires player investment
- **Story Integration**: Recruit training events, hazing rituals, veteran bonding

---

## Companion Management (T1+)

### Overview

From the moment of enlistment (T1), players can manage their companions' battle participation. Companions serve in the player's personal roster throughout entire career.

### Location & Control

| Tier | Companion Location | Battle Control |
|------|-------------------|----------------|
| T1-T9 | Player's party roster | Full toggle control |

**Key Changes from Previous System:**
- **Removed**: T1-T3 companions in lord's party
- **New**: Companions always in player's party from T1
- **Enhanced**: Battle participation toggle available immediately

### Battle Participation Toggle

**Location**: Camp -> Companion Assignments

**Options per companion:**
- **Fight**: Spawns in battle, faces all risks, gains experience
- **Stay Back**: Remains in roster, immune to casualties, no XP gain

**Use Cases:**
- Protect valuable companions during dangerous battles
- Control which heroes gain combat experience
- Manage wounds and recovery strategically
- Avoid permadeath risk for favorite characters

### Formation Integration

At T7+, companions fight alongside the player's retinue in unified squad:
- All spawn in player's formation (Infantry/Archer/Cavalry/Horse Archer)
- Player commands own unit (not entire army)
- Companions cannot become formation captains or generals
- "Stay Back" companions don't spawn, remain safe in roster

---

## Commander's Retinue (T7-T9)

### Unlock & Initial Grant

**T6->T7 Promotion Event: "Commander's Commission"**

When promoted to T7, player receives initial command:
- **15 raw recruits** automatically added to roster
- Recruit type matches player's formation specialization
- Event narrative: Lord recognizes leadership, assigns first command
- No cost, no shopping — command is granted as reward

**Example Flow:**
1. Player reaches T7 XP threshold
2. Proving event: "You're ready to lead. Here are your soldiers."
3. 15 recruits (Recruit I) appear in party roster
4. Formation type: Infantry recruits if player is infantry specialist, etc.

### Capacity Progression

| Tier | Capacity | Grant on Promotion | Existing Soldiers |
|------|----------|-------------------|-------------------|
| T7 | 15 | +15 raw recruits | 0 -> 15 |
| T8 | 25 | +10 recruits | 15 -> 25 |
| T9 | 35 | +10 recruits | 25 -> 35 |

**Promotion Grants:**
- T7: Initial grant of 15
- T8: +10 more (if under capacity)
- T9: +10 more (if under capacity)
- Always raw recruits (Recruit I tier)
- Formation-specific type (Infantry/Archer/Cavalry/Horse Archer)

### Formation Specialization

Recruit type determined by player's formation choice (made at T1->T2):

| Player Formation | Recruit Type | Example Troops |
|-----------------|--------------|----------------|
| Infantry | Infantry Recruits | Levy, Footmen, Militia |
| Archer | Ranged Recruits | Archer Recruits, Crossbow Militia |
| Cavalry | Cavalry Recruits | Mounted Recruits, Light Cavalry |
| Horse Archer | Horse Archer Recruits | Mounted Archers, Nomad Recruits |

**Culture Integration:**
- Recruit pool matches enlisted lord's culture
- Empire infantry, Vlandia cavalry, Khuzait horse archers, etc.
- Authentic unit names and progression trees

---

## Recruit Training System

### Troop Progression

Raw recruits develop through combat experience and time served:

```
Recruit I (raw) -> Recruit II (trained) -> Regular I -> Regular II -> Veteran I -> Veteran II
  Grant          +100 XP            +200 XP      +400 XP       +800 XP     +1600 XP
  (free)        7-14 days          2-3 battles  4-6 battles  10+ battles  Elite status
```

**Progression Mechanics:**
- Native Bannerlord troop upgrade system (XP-based)
- Automatic upgrades when XP thresholds met
- Player can manually upgrade with gold (accelerated path)
- Participation in battles grants XP naturally
- Wounded soldiers continue gaining passive XP while recovering

### Training Events (Optional Flavor)

**Lance Life Integration:**
- "Recruit Hazing" — Veterans test new arrivals (Discipline check)
- "Field Promotion" — Exceptional recruit earns early upgrade
- "Training Accident" — Recruit injured during drills (Medical event)
- "Veteran Mentorship" — Experienced soldier trains recruits (+XP boost)

**Benefits:**
- Creates narrative attachment to retinue
- Provides player choices affecting development speed
- Integrates with existing lance life event system
- Optional: Can be disabled in config if player prefers pure combat progression

### Manual Training

**Location**: Camp -> Retinue Management -> Train Soldiers

**Options:**
- **Drill Soldiers** (costs gold): Grants bonus XP to all recruits
- **Individual Training** (costs gold + time): Upgrade specific soldier immediately
- **Veteran Mentorship** (costs influence): Pair recruit with veteran for XP boost

---

## Reinforcement & Trickle System

### Existing Trickle System (Preserved)

**Current Implementation:**
- 1 soldier added every 2-3 days automatically
- Free reinforcement (no cost)
- Continues until capacity reached
- Formation-specific type

**Enhancements for New System:**
- Trickle soldiers arrive as raw recruits (Recruit I)
- Player sees notification: "New recruit reports for duty"
- Integration with camp life: Logistics strain affects trickle rate
- Higher tension = slower reinforcements (3-5 days instead of 2-3)

### Casualty Replacement

**Smart Trickle Priority:**
1. If below capacity due to casualties -> priority trickle (2 days)
2. If near capacity -> normal trickle (2-3 days)
3. If at capacity -> trickle paused

**Example:**
- Player has 12/15 soldiers (3 casualties)
- Next trickle in 2 days (priority)
- Recruit arrives, now 13/15
- Next trickle in 2 days (still priority)
- Reaches 15/15, trickle pauses

### Desertion Impact

When soldiers desert due to high pay tension (80-100):
- Retinue soldiers can desert (1-5% chance per day at high tension)
- Creates capacity gaps for trickle system to fill
- Reinforcements help rebuild after morale crisis
- Narrative: "Your command is falling apart, need fresh recruits"

---

## Battle Integration

### Unified Squad Formation

**T7-T9 Battle Deployment:**
- Player + Companions + Retinue = Unified Squad
- All spawn in player's formation (Infantry/Archer/Cavalry/Horse Archer)
- Player commands own unit (F1-F6 keys work for own formation)
- Cannot command other formations or full army (no Order of Battle screen)

**Formation Assignment:**
```
Player Formation = Infantry
  - Player (Infantry)
  - Companion 1 (Infantry, reassigned from their natural class)
  - Companion 2 (Infantry, reassigned)
  - 15x Infantry retinue soldiers
  - Total: 18 units in player's command
```

### Battle Behavior

**Reinforcement Spawning:**
- Initial deployment: Squad spawns together near formation position
- Reinforcement phase: Squad spawns at map edge, teleported to formation
- Stay Back companions don't spawn, remain safe in roster

**Casualty Handling:**
- Native system handles kills/wounds/captures automatically
- Casualty tracker reconciles retinue state post-battle
- Wounded soldiers: Recovery time based on severity (1-7 days)
- Killed soldiers: Permanent loss, trickle system replaces over time

**Formation Reassignment:**
- Retinue soldiers may spawn in "wrong" formation (e.g., cavalry in infantry)
- System detects and reassigns to player's formation
- Soldiers teleported to player's position
- All fight together regardless of natural troop class

---

## Casualty Tracking

### Existing System (Enhanced)

**Pre-Battle Snapshot:**
- System records exact troop counts before battle
- Tracks by CharacterObject ID (troop type)

**Post-Battle Reconciliation:**
- Compare snapshot vs. actual roster
- Calculate: casualties = pre-battle count - post-battle count
- Update retinue state tracking
- Enable trickle replacements

**Wound Recovery:**
- Wounded soldiers remain in roster
- Daily recovery check (native system)
- Retinue state doesn't change during recovery
- Fully healed soldiers available again

**Permadeath:**
- Killed soldiers removed from roster permanently
- Retinue state decrements immediately
- Trickle system begins replacement process
- Player sees: "Lost 3 soldiers in battle, reinforcements in 6 days"

---

## Implementation Phases

### Phase 1: Companion Management Update (T1+) Priority

**Goal**: Move companions to player party at T1, enable battle toggle immediately

**Tasks:**
1. **Update companion transfer logic** (`CompanionAssignmentManager.cs`)
   - Remove T4 tier gate
   - Transfer companions to player party at enlistment (T1)
   - Companions stay in player party for entire enlisted career

2. **Update companion battle participation** (Already exists)
   - Verify toggle works at T1
   - Test "Stay Back" mechanic with lower-tier battles

3. **Update formation assignment** (`EnlistedFormationAssignmentBehavior.cs`)
   - Companions fight in player's formation from T1
   - All tiers (T1-T9) use same formation logic
   - Remove T4 gate from unified squad behavior

4. **Documentation updates**
   - `companion-management.md`: Update tier tables T1+ for all features
   - `README.md`: Update companion section to reflect T1 availability

**Testing:**
- Enlist at T1, verify companions in player party
- Test battle toggle at T1-T3
- Verify companions fight in player's formation

**Estimated Effort**: 4-6 hours

---

### Phase 2: Capacity & Tier Updates (T7-T9) Required

**Goal**: Update tier capacity system for new T7-T9 commander structure

**Tasks:**
1. **Update `RetinueManager.cs` constants**
   ```csharp
   // OLD
   public const int LanceTier = 4;      // 5 soldiers
   public const int SquadTier = 5;      // 10 soldiers
   public const int RetinueTier = 6;    // 20 soldiers
   
   // NEW
   public const int CommanderTier1 = 7; // 15 soldiers
   public const int CommanderTier2 = 8; // 25 soldiers
   public const int CommanderTier3 = 9; // 35 soldiers
   ```

2. **Update `GetTierCapacity()` method**
   ```csharp
   public static int GetTierCapacity(int tier)
   {
       return tier switch
       {
           >= CommanderTier3 => 35,
           CommanderTier2 => 25,
           CommanderTier1 => 15,
           _ => 0  // No soldiers T1-T6
       };
   }
   ```

3. **Update `GetUnitName()` method**
   ```csharp
   public static string GetUnitName(int tier)
   {
       return tier switch
       {
           >= CommanderTier3 => "Command (Elite)",
           CommanderTier2 => "Command (Veteran)",
           CommanderTier1 => "Command (Regular)",
           _ => "None"
       };
   }
   ```

4. **Update UI strings** (`enlisted_strings.xml`)
   - Commander rank titles
   - Capacity descriptions
   - Grant notifications

**Testing:**
- Verify capacity at each tier: 0 (T1-T6), 15 (T7), 25 (T8), 35 (T9)
- Check UI displays correct unit names
- Test trickle system respects new capacities

**Estimated Effort**: 3-4 hours

---

### Phase 3: Auto-Grant System (T7 Initial, T8-T9 Expansion) - Core Feature

**Goal**: Automatically grant raw recruits on promotion instead of requiring purchase

**Tasks:**
1. **Create recruitment grant system** (new file: `RetinueRecruitmentGrant.cs`)
   ```csharp
   public sealed class RetinueRecruitmentGrant
   {
       /// <summary>
       /// Grants raw recruits when player reaches commander tier.
       /// Called during promotion event completion.
       /// </summary>
       public static void GrantCommanderRetinue(int newTier)
       {
           var count = newTier switch
           {
               7 => 15,  // Initial grant
               8 => 10,  // Expansion
               9 => 10,  // Final expansion
               _ => 0
           };
           
           if (count > 0)
           {
               GrantRawRecruits(count, GetPlayerFormationType());
           }
       }
       
       private static void GrantRawRecruits(int count, string formationType)
       {
           // Find appropriate recruit troop from enlisted lord's culture
           var recruitTroop = FindCultureRecruit(formationType);
           
           // Add to player party roster
           var party = MobileParty.MainParty;
           party.MemberRoster.AddToCounts(recruitTroop, count);
           
           // Update retinue state
           RetinueManager.Instance.State.AddSoldiers(recruitTroop.StringId, count);
           
           // Notification
           ShowRecruitGrantNotification(count);
       }
   }
   ```

2. **Hook into promotion system** (`PromotionBehavior.cs`)
   - Call `RetinueRecruitmentGrant.GrantCommanderRetinue(newTier)` after T7/T8/T9 proving events
   - Add confirmation dialog: "You now command 15 soldiers"

3. **Create culture-based recruit finder**
   ```csharp
   private static CharacterObject FindCultureRecruit(string formationType)
   {
       var lord = EnlistmentBehavior.Instance?.CurrentLord;
       var culture = lord?.Culture?.StringId ?? "empire";
       
       // Search for lowest-tier troop matching formation and culture
       // Examples:
       // - Empire + Infantry = "imperial_recruit" or "imperial_levy"
       // - Vlandia + Cavalry = "vlandian_light_cavalry_recruit"
       // - Khuzait + Horse Archer = "khuzait_nomad"
       
       return FindLowestTierTroop(culture, formationType);
   }
   ```

4. **Remove purchase UI for T7+ soldiers**
   - Disable "Add Soldiers" button at T7-T9
   - Show message: "Recruits granted automatically on promotion"
   - Keep purchase UI for T4-T6 (legacy compatibility during transition)

**Testing:**
- Promote to T7, verify 15 raw recruits appear
- Check recruits match player formation type
- Verify culture-appropriate troops (Empire gets Empire recruits, etc.)
- Promote to T8, verify +10 more recruits
- Test with different formations and cultures

**Estimated Effort**: 8-12 hours

---

### Phase 4: Raw Recruit Identification & Progression - Quality of Life

**Goal**: Ensure granted recruits are lowest tier, track progression visually

**Tasks:**
1. **Create recruit tier identifier**
   ```csharp
   public static bool IsRawRecruit(CharacterObject troop)
   {
       // Check if troop is lowest tier in upgrade tree
       // Method 1: Has no downgrade path (is root)
       // Method 2: Level <= 5
       // Method 3: Name contains "Recruit" or "Levy"
       
       return troop.Level <= 5 && 
              troop.UpgradeTargets?.Length > 0 &&
              !HasDowngradePath(troop);
   }
   ```

2. **Add progression tracking** to retinue state
   ```csharp
   // Track recruit levels for UI display
   public Dictionary<string, int> TroopLevels { get; set; }
   
   // Count recruits vs. regulars vs. veterans
   public int CountByTier(TroopTier tier)
   {
       // Recruit: Level 0-10
       // Regular: Level 11-20
       // Veteran: Level 21+
   }
   ```

3. **UI enhancements** (Camp -> Retinue Management)
   - Show troop progression: "5 Recruits, 7 Regulars, 3 Veterans"
   - Color-code by experience: Green (recruit), Blue (regular), Gold (veteran)
   - Display average retinue level
   - Show XP to next upgrade per soldier type

4. **Training event integration** (optional, Phase 5)
   - Lance life events trigger for raw recruits
   - "Green Recruit Mistakes" event at recruit tier
   - "Veteran Recognition" event when soldier reaches veteran

**Testing:**
- Verify granted troops are lowest tier in culture's tree
- Check UI shows correct recruit/regular/veteran counts
- Test progression tracking after battles (XP gain)

**Estimated Effort**: 6-8 hours

---

### Phase 5: Training Events & Flavor (Optional) ðŸŽ¨ Enhancement

**Goal**: Add narrative flavor to recruit development, integrate with lance life system

**Tasks:**
1. **Create training event pack** (`events_retinue_training.json`)
   - "First Blood" — Recruit's first kill (+morale, +XP)
   - "Veteran's Lesson" — Old soldier trains recruits (+XP to all recruits)
   - "Training Accident" — Recruit injured during drills (Medical event)
   - "Promotion Ceremony" — Soldier reaches Regular status (morale event)
   - "Deserter Contemplates" — Low morale recruit considers leaving (tension tie-in)

2. **Add retinue triggers to event system**
   ```json
   "triggers": {
       "all": ["has_recruits", "camp"],
       "any": []
   }
   ```

3. **Create retinue persona system** (expansion of lance personas)
   - Generate simple names for retinue soldiers
   - Track individual soldier stories (kills, battles survived, wounds)
   - Surface in events: "{RECRUIT_NAME} looks nervous before the battle"

4. **Integration with existing systems**
   - Heat/Discipline: Training accidents raise heat
   - Medical: Wounded recruits in training
   - Pay Tension: Recruits more likely to desert when unpaid
   - Fatigue: Training drills cost fatigue

**Testing:**
- Verify events fire at appropriate times
- Check persona names generate correctly
- Test event outcomes affect recruit XP/stats

**Estimated Effort**: 12-16 hours (optional, can be Phase 6+)

---

### Phase 6: Documentation & Polish - Required

**Goal**: Update all documentation to reflect new system

**Tasks:**
1. **Create this document** (`retinue-system.md`) (done)
2. **Update existing docs**
   - `companion-management.md`: T1+ changes
   - `README.md`: Commander retinue section
   - `lance-assignments.md`: Commander rank descriptions
   - `pay-system.md`: Retinue impact on finances (optional: soldier pay?)

3. **Create modding guide**
   - How to add custom recruit types
   - How to create training events
   - How to modify capacity/trickle rates

4. **UI/UX polish**
   - Better recruit grant notifications
   - Retinue management menu improvements
   - Formation type display in retinue UI
   - Average level / experience bar

5. **Localization**
   - All new strings in `enlisted_strings.xml`
   - French translations (if available)
   - Support for other languages

**Testing:**
- Full playthrough T1->T9
- Test all companion management features
- Verify auto-grant at T7/T8/T9
- Check UI displays correctly
- Test with different cultures and formations

**Estimated Effort**: 6-8 hours

---

## Technical Details

### Companion Transfer (Phase 1)

**Current Code** (`CompanionAssignmentManager.cs` or similar):
```csharp
// OLD: Transfer companions at T4
if (tier >= 4)
{
    TransferCompanionsToPlayer();
}
```

**New Code**:
```csharp
// NEW: Companions always with player from T1
public void OnEnlistmentStart()
{
    // All companions immediately transfer to player party
    TransferCompanionsToPlayer();
}
```

### Capacity System (Phase 2)

**File**: `RetinueManager.cs`

**Changes**:
```csharp
// Update constants
public const int CommanderTier1 = 7;
public const int CommanderTier2 = 8;
public const int CommanderTier3 = 9;

public const int CommanderCapacity1 = 15;
public const int CommanderCapacity2 = 25;
public const int CommanderCapacity3 = 35;

// Update capacity logic
public static int GetTierCapacity(int tier)
{
    return tier switch
    {
        >= CommanderTier3 => CommanderCapacity3,
        CommanderTier2 => CommanderCapacity2,
        CommanderTier1 => CommanderCapacity1,
        _ => 0  // T1-T6 have no soldier retinue (companions only)
    };
}
```

### Auto-Grant System (Phase 3)

**New File**: `src/Features/CommandTent/Core/RetinueRecruitmentGrant.cs`

**Key Methods**:
```csharp
public sealed class RetinueRecruitmentGrant
{
    private const string LogCategory = "RecruitGrant";
    
    /// <summary>
    /// Grants recruits when player reaches commander tier.
    /// </summary>
    public static void GrantCommanderRetinue(int newTier, int previousTier)
    {
        // Only grant on promotion to T7/T8/T9
        if (newTier < 7 || previousTier >= newTier)
        {
            return;
        }
        
        var count = CalculateGrantCount(newTier, previousTier);
        if (count <= 0)
        {
            return;
        }
        
        var formation = GetPlayerFormationType();
        var culture = GetEnlistedLordCulture();
        
        GrantRawRecruits(count, formation, culture);
    }
    
    private static int CalculateGrantCount(int newTier, int previousTier)
    {
        // Initial grant at T7
        if (newTier >= 7 && previousTier < 7)
        {
            return 15;
        }
        
        // Expansion grants
        if (newTier == 8 && previousTier < 8)
        {
            return 10;
        }
        
        if (newTier == 9 && previousTier < 9)
        {
            return 10;
        }
        
        return 0;
    }
    
    private static void GrantRawRecruits(int count, string formation, string culture)
    {
        var recruitTroop = FindCultureRecruit(culture, formation);
        if (recruitTroop == null)
        {
            ModLogger.Error(LogCategory, 
                $"Could not find recruit for culture={culture}, formation={formation}");
            return;
        }
        
        // Add to player party
        var party = MobileParty.MainParty;
        party.MemberRoster.AddToCounts(recruitTroop, count);
        
        // Update retinue state
        var manager = RetinueManager.Instance;
        manager?.State.AddSoldiers(recruitTroop.StringId, count);
        
        ModLogger.Info(LogCategory, 
            $"Granted {count}x {recruitTroop.Name} (culture={culture}, formation={formation})");
        
        ShowGrantNotification(count, recruitTroop.Name);
    }
    
    private static CharacterObject FindCultureRecruit(string culture, string formation)
    {
        // Search troop tree for lowest-tier troop matching formation
        var allTroops = CharacterObject.All;
        
        var formationClass = formation.ToLower() switch
        {
            "infantry" => FormationClass.Infantry,
            "archer" => FormationClass.Ranged,
            "cavalry" => FormationClass.Cavalry,
            "horsearcher" => FormationClass.HorseArcher,
            _ => FormationClass.Infantry
        };
        
        var candidates = allTroops
            .Where(t => t.Culture?.StringId == culture)
            .Where(t => t.IsBasicTroop)
            .Where(t => MatchesFormation(t, formationClass))
            .Where(t => t.Level <= 10)  // Raw recruits only
            .Where(t => !t.IsHero)
            .OrderBy(t => t.Level)
            .ThenBy(t => t.Tier)
            .ToList();
        
        return candidates.FirstOrDefault();
    }
    
    private static bool MatchesFormation(CharacterObject troop, FormationClass formation)
    {
        // Check if troop's default formation matches desired formation
        return troop.DefaultFormationClass == formation;
    }
    
    private static void ShowGrantNotification(int count, TextObject troopName)
    {
        var msg = new TextObject(
            "{=retinue_grant}{COUNT} {TROOP_TYPE} have been assigned to your command.")
            .SetTextVariable("COUNT", count)
            .SetTextVariable("TROOP_TYPE", troopName);
        
        MBInformationManager.AddQuickInformation(msg, 
            soundEventPath: "event:/ui/notification/army_created");
    }
}
```

### Promotion Integration (Phase 3)

**File**: `src/Features/Ranks/Behaviors/PromotionBehavior.cs`

**Hook Point**:
```csharp
private void OnProvingEventCompleted(int newTier)
{
    // ... existing promotion logic ...
    
    // Grant commander retinue if reaching T7/T8/T9
    if (newTier >= 7)
    {
        var previousTier = _currentTier;  // Stored before promotion
        RetinueRecruitmentGrant.GrantCommanderRetinue(newTier, previousTier);
    }
    
    // ... rest of promotion logic ...
}
```

### Trickle System Updates (Phase 3)

**File**: `RetinueTrickleSystem.cs`

**Changes**:
```csharp
// Update tier gate from T4 to T7
private static bool CanTrickleReplenish(out string reason)
{
    // ... existing checks ...
    
    if (enlistment.EnlistmentTier < RetinueManager.CommanderTier1)
    {
        reason = $"tier {enlistment.EnlistmentTier} < {RetinueManager.CommanderTier1}";
        return false;
    }
    
    // ... rest of checks ...
}
```

**Ensure recruits are granted as raw:**
```csharp
private void TrickleAddSoldier()
{
    // Get formation type and culture
    var formation = GetPlayerFormationType();
    var culture = GetEnlistedLordCulture();
    
    // Find raw recruit (same logic as grant system)
    var recruitTroop = RetinueRecruitmentGrant.FindCultureRecruit(culture, formation);
    
    // Add to party
    var party = MobileParty.MainParty;
    party.MemberRoster.AddToCounts(recruitTroop, 1);
    
    // Update state
    RetinueManager.Instance.State.AddSoldiers(recruitTroop.StringId, 1);
}
```

---

## API Reference

### RetinueManager

```csharp
// Capacity queries
public static int GetTierCapacity(int tier);
public static string GetUnitName(int tier);
public bool CanHaveRetinue(int tier);

// Current state
public RetinueState State { get; }
public int CurrentCapacity { get; }
public int AvailableSlots { get; }

// Soldier management
public bool TryAddSoldiers(int count, string typeId, out int added, out string message);
public bool TryRemoveSoldiers(string typeId, int count);
public void ClearRetinue(string reason);

// Formation queries
public static bool IsFormationAvailable(string culture, string formationType);
public CharacterObject GetDefaultRecruitForFormation(string formation);
```

### RetinueRecruitmentGrant (NEW)

```csharp
// Grant system
public static void GrantCommanderRetinue(int newTier, int previousTier);
public static CharacterObject FindCultureRecruit(string culture, string formation);

// Helpers
public static bool IsRawRecruit(CharacterObject troop);
public static int GetRecruitLevel(CharacterObject troop);
public static string GetFormationType(CharacterObject troop);
```

### CompanionAssignmentManager

```csharp
// Companion control (updated for T1+)
public void SetCompanionBattleParticipation(Hero companion, bool shouldFight);
public bool ShouldCompanionFight(Hero companion);
public Dictionary<string, bool> GetAllCompanionSettings();

// Companion transfers (T1 immediate)
public void TransferCompanionsToPlayer();
public void RestoreCompanionsToPlayer();
public List<Hero> GetPlayerCompanions();
```

### RetinueTrickleSystem

```csharp
// Trickle configuration
public const int TrickleMinDays = 2;
public const int TrickleMaxDays = 3;
public const int SoldiersPerTrickle = 1;

// State queries
public bool CanTrickleReplenish(out string reason);
public int GetTrickleInterval();
public int DaysSinceLastTrickle { get; }
```

---

## Config Reference

### enlisted_config.json

```json
{
  "retinue": {
    "enabled": true,
    "companion_management_tier": 1,
    "commander_tier_1": 7,
    "commander_capacity_1": 15,
    "commander_tier_2": 8,
    "commander_capacity_2": 25,
    "commander_tier_3": 9,
    "commander_capacity_3": 35,
    "auto_grant_enabled": true,
    "grant_as_raw_recruits": true,
    "trickle_enabled": true,
    "trickle_min_days": 2,
    "trickle_max_days": 3,
    "priority_trickle_when_casualties": true,
    "training_events_enabled": true
  }
}
```

---

## Testing Checklist

### Phase 1: Companion Management
- [ ] Companions in player party at T1 enlistment
- [ ] Battle toggle available at T1
- [ ] "Stay Back" companions don't spawn in battle
- [ ] "Fight" companions spawn and gain XP
- [ ] Companions fight in player's formation (T1-T9)
- [ ] No crashes with 0-6 companions

### Phase 2: Capacity Updates
- [ ] T1-T6: 0 soldier capacity, companions only
- [ ] T7: 15 soldier capacity
- [ ] T8: 25 soldier capacity
- [ ] T9: 35 soldier capacity
- [ ] UI shows correct capacity limits
- [ ] Trickle system respects new capacities

### Phase 3: Auto-Grant
- [ ] T6->T7: 15 raw recruits granted automatically
- [ ] T7->T8: +10 more recruits granted
- [ ] T8->T9: +10 more recruits granted
- [ ] Recruits match player formation type
- [ ] Recruits match enlisted lord's culture
- [ ] Recruits are lowest tier (raw)
- [ ] No purchase UI at T7-T9
- [ ] Grant notification displays correctly

### Phase 4: Progression
- [ ] Recruits upgrade via combat XP
- [ ] UI shows recruit/regular/veteran counts
- [ ] Manual training option works
- [ ] Progression tracking persists across saves

### Phase 5: Training Events (Optional)
- [ ] Training events fire for raw recruits
- [ ] Events grant appropriate XP bonuses
- [ ] Persona names generate for soldiers
- [ ] Events integrate with heat/discipline/medical

### Phase 6: Full Integration
- [ ] Complete T1->T9 playthrough
- [ ] All documentation updated
- [ ] All UI strings localized
- [ ] No console errors or crashes
- [ ] Save/load preserves all state

---

## Related Documentation

- [Companion Management](companion-management.md) — Detailed companion system
- [Lance Assignments](lance-assignments.md) — Culture ranks and unit identity
- [Formation Training](formation-training.md) — Daily skill XP by formation
- [Pay System](pay-system.md) — Wages and financial management
- [Lance Life Events](../Gameplay/lance-life.md) — Story event system

---

## Implementation Timeline

| Phase | Priority | Effort | Dependencies | Deliverable |
|-------|----------|--------|--------------|-------------|
| 1 | High | 4-6h | None | Companions at T1 |
| 2 | High | 3-4h | Phase 1 | T7-T9 capacities |
| 3 | High | 8-12h | Phase 2 | Auto-grant recruits |
| 4 | Medium | 6-8h | Phase 3 | Progression tracking |
| 5 | Low | 12-16h | Phase 4 | Training events (optional) |
| 6 | Medium | 6-8h | All phases | Documentation & polish |

**Total Estimated Effort**: 39-54 hours (27-38 hours if Phase 5 skipped)

**Recommended Order**: Phase 1 -> Phase 2 -> Phase 3 -> Phase 6 (skip Phase 4-5 for MVP)

---

## Future Enhancements (Post-Launch)

### Retinue Specialization
- Choose formation type for retinue independent of player formation
- Mixed-formation retinues (3 infantry + 2 archers, etc.)
- Elite unit selection (choose specific troops beyond raw recruits)

### Advanced Training
- Training camp mini-game
- Veteran soldier "mentor" assignment
- Formation drills that grant passive XP
- Morale-based training effectiveness

### Permadeath & Legacy
- Soldiers with high kill counts become "heroes"
- Funeral events for fallen veterans
- Memorial system tracking lost soldiers
- "Replacement" recruits arrive with names of fallen comrades

### Command Abilities
- Special formation orders unlocked at T7-T9
- Retinue-specific tactics in battle
- Group charge, shield wall, archer volley commands
- Integration with native tactical AI

---

**Document Version**: 1.0  
**Last Updated**: 2025-01-13  
**Status**: Implementation Ready
