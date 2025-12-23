# Native Skill XP + Leveling (Bannerlord) — Research Notes

**Last Updated:** 2025-12-22  
**Audience:** Enlisted developers  
**Purpose:** Capture how Bannerlord awards skill XP, converts it into skill levels, and how "hero level" (attribute/focus points) advances. This is a reference for designing Enlisted "Training / Decisions / Schedule" rewards without fighting native progression.

---

## Overview

Bannerlord has **two related but distinct progression tracks**:

- **Skill progression** (e.g., `OneHanded`, `Riding`, `Charm`):
  - XP is stored per skill and can increase the **skill value** when thresholds are crossed.
  - XP gain is affected by **learning rate** (focus + attributes + learning limit) and a small **global XP multiplier** model.

- **Hero “level” progression** (character level):
  - Driven by the hero’s **total accumulated raw XP** (tracked in the hero developer).
  - Level-ups grant **focus points** (every level) and **attribute points** (every N levels).

For Enlisted, the actionable takeaway is:

- When we want native skill progression behavior, we should award XP via **`Hero.AddSkillXp(skill, xp)`**, and let native systems handle learning rates and level-ups.

---

## Primary Native Reference Files (Local Decompile)

Use the local decompile as the authority (paths below are in this repo’s decompile workspace):

- **Skills list and attributes**
  - `Decompile/TaleWorlds.Core/DefaultSkills.cs`
  - `Decompile/TaleWorlds.Core/SkillObject.cs`

- **Skill XP storage and application**
  - `Decompile/TaleWorlds.CampaignSystem/TaleWorlds/CampaignSystem/CharacterDevelopment/HeroDeveloper.cs`

- **Skill XP thresholds + learning rate + hero level thresholds**
  - `Decompile/TaleWorlds.CampaignSystem/TaleWorlds/CampaignSystem/GameComponents/DefaultCharacterDevelopmentModel.cs`
  - `Decompile/TaleWorlds.CampaignSystem/TaleWorlds/CampaignSystem/ComponentInterfaces/CharacterDevelopmentModel.cs`

- **Generic XP multiplier**
  - `Decompile/TaleWorlds.CampaignSystem/TaleWorlds/CampaignSystem/ComponentInterfaces/GenericXpModel.cs`
  - `Decompile/TaleWorlds.CampaignSystem/TaleWorlds/CampaignSystem/GameComponents/DefaultGenericXpModel.cs`

- **Example: combat skill XP awarding**
  - `Decompile/TaleWorlds.CampaignSystem/TaleWorlds/CampaignSystem/CharacterDevelopment/DefaultSkillLevelingManager.cs`

- **Combat XP calculation and awarding**
  - `Decompile/TaleWorlds.CampaignSystem/TaleWorlds/CampaignSystem/ComponentInterfaces/CombatXpModel.cs`
  - `Decompile/TaleWorlds.CampaignSystem/TaleWorlds/CampaignSystem/GameComponents/DefaultCombatXpModel.cs`
  - `Decompile/TaleWorlds.CampaignSystem/TaleWorlds/CampaignSystem/AgentOrigins/SimpleAgentOrigin.cs`
  - `Decompile/TaleWorlds.CampaignSystem/TaleWorlds/CampaignSystem/AgentOrigins/PartyAgentOrigin.cs`
  - `Decompile/TaleWorlds.MountAndBlade/TaleWorlds/MountAndBlade/CustomBattleAgentLogic.cs`
  - `Decompile/TaleWorlds.Core/IAgentOriginBase.cs`

---

## How Combat XP Works (Kill/Damage in Battle)

When a player or any hero deals damage in battle, the XP system follows this flow:

### 1. Combat Hit Detection

When an agent hits another agent, `CustomBattleAgentLogic.OnAgentHit()` is called. It determines:

- **Is the hit fatal?** `affectedAgent.Health - blow.InflictedDamage < 1.0`
- **Is it team kill?** `affectedAgent.Team.Side == affectorAgent.Team.Side`

Then calls: `affectorAgent.Origin.OnScoreHit(victim, captain, damage, isFatal, isTeamKill, weapon)`

Key parameters:
- `victim` - The character who was hit
- `captain` - The attacker's formation captain (can provide bonuses)
- `damage` - Actual damage dealt
- `isFatal` - Whether this hit killed the target
- `isTeamKill` - Whether this was friendly fire (no XP if true)
- `weapon` - Weapon component data (determines which skill to train)

### 2. Agent Origin XP Handling

**Critical distinction**: Only certain agent origins award XP, and only to heroes.

**`SimpleAgentOrigin.OnScoreHit()`** (AWARDS XP):
- Used for heroes in most singleplayer scenarios
- Checks: `if (!isTeamKill)` - no XP for friendly fire
- Calls `CombatXpModel.GetXpFromHit()` to calculate XP
- If troop is a hero and has a weapon: `troop.HeroObject.AddSkillXp(skill, xp)`

**`PartyAgentOrigin.OnScoreHit()`** (NO XP):
- Used for regular troops in parties
- This method is empty - regular troops don't gain XP from combat hits
- Troops gain XP through other systems (party training, battle recovery, etc.)

### 3. Combat XP Calculation (`DefaultCombatXpModel`)

The XP calculation formula (simplified):

```
baseXP = 0.4 × (attackerPower + 0.5) × (victimPower + 0.5) × (min(damage, victimMaxHP) + (isFatal ? victimMaxHP : 0)) × missionTypeMultiplier
```

**Components**:
- **Attacker power**: `MilitaryPowerModel.GetTroopPower(attackerTroop, side, context, leaderModifier)`
- **Victim power**: `MilitaryPowerModel.GetTroopPower(victimTroop, oppositeSide, context, leaderModifier)`
- **Damage component**: Both the damage dealt AND full HP if it's a killing blow
- **Mission type multipliers**:
  - Battle: 1.0
  - SimulationBattle: 0.9
  - Tournament: 0.33
  - PracticeFight: 0.0625 (1/16)
  - NoXp: 0.0

**Perk bonuses** (applied via `GetBattleXpBonusFromPerks`):
- Leadership.InspiringLeader (if captain has perk): +20% XP
- OneHanded.Trainer: bonus for melee troops
- TwoHanded.BaptisedInBlood: bonus for two-handed users
- Throwing.Resourceful: bonus for troops with throwing weapons
- OneHanded.CorpsACorps: bonus for infantry
- OneHanded.LeadByExample: general bonus
- Crossbow.MountedCrossbowman: bonus for mounted crossbowmen
- Bow.BullsEye: bonus for archers
- Roguery.NoRestForTheWicked: bonus when fighting bandits
- Various garrison perks if in settlement

### 4. Weapon Skill Mapping (`GetSkillForWeapon`)

The XP is awarded to the skill matching the weapon used:

- Siege engine hits → `Engineering`
- Weapon-specific:
  - One-handed weapons → `OneHanded`
  - Two-handed weapons → `TwoHanded`
  - Polearms → `Polearm`
  - Bows → `Bow`
  - Crossbows → `Crossbow`
  - Thrown weapons → `Throwing`
  - No weapon/unarmed → `Athletics`

**Source**: `weapon.RelevantSkill` property determines the skill.

### 5. Final XP Application

The calculated XP is passed to `Hero.AddSkillXp(skill, xp)`, which:
- Applies the generic XP multiplier
- Applies the learning rate (based on focus and attributes)
- Adds to skill XP pool
- Checks for skill level-up
- Adds to total hero XP for level progression

### Key Takeaways for Enlisted

1. **Only heroes gain XP from combat hits** - Regular troops use different progression systems
2. **XP scales with both attacker and victim power** - Killing stronger enemies gives more XP
3. **Killing blows give extra XP** - The victim's full HP is added to the calculation
4. **Formation captains matter** - Some perks grant bonuses if your captain has them
5. **Team kills give no XP** - Friendly fire is not rewarded
6. **The weapon determines the skill** - One-handed sword = OneHanded XP, bow = Bow XP, etc.
7. **Perk bonuses stack** - Multiple applicable perks can significantly boost XP gain

---

## How Troop (Non-Hero) XP Works

Regular troops in parties don't gain XP from combat hits directly. Instead, they gain XP through several other systems:

### 1. Party Training Behavior (`MobilePartyTrainingBehavior`)

Daily passive training for troops in mobile parties:
- Calculates effective daily XP based on party size, leader skills, and perks
- Calls `party.MemberRoster.AddXpToTroop(troop, xpAmount)` for each troop
- Training-related perks (e.g., Bow.Trainer) can grant additional XP

### 2. Battle Recovery (`CampaignBattleRecoveryBehavior`)

After battles, surviving wounded troops may gain XP:
- `party.MemberRoster.AddXpToTroop(troop, xp * count)`
- Represents experience gained from surviving combat

### 3. Garrison Training (`GarrisonRecruitmentCampaignBehavior`)

Troops in town garrisons receive daily training XP:
- `town.GarrisonParty.MemberRoster.AddXpToTroop(troop, dailyXpBonus * xpMultiplier * troopCount)`
- Scales with town prosperity and governor perks

### 4. Recruitment and Leadership Perks

When recruiting troops:
- `recruiter.PartyBelongedTo.MemberRoster.AddXpToTroop(troop, xp * count)` (Leadership.FamousCommander perk)
- Newly recruited troops can start with bonus XP

### 5. Prisoner Training

Prisoners can gain XP while held:
- `mobileParty.PrisonRoster.AddXpToTroop(troop, xpAmount)`

### 6. Siege Engineering Work

Troops working on siege engines gain Engineering-related XP:
- `siegeParty.MemberRoster.AddXpToTroop(troopCharacter, engineeringXp * troopCount)` (Engineering.Apprenticeship perk)

### Troop XP API: `TroopRoster.AddXpToTroop()`

The roster methods for troop XP:
- `AddXpToTroop(CharacterObject character, int xpAmount)` - Add XP to a specific troop type
- `AddXpToTroopAtIndex(int index, int xpAmount)` - Add XP to troops at a specific roster index
- `CanTroopGainXp(PartyBase party, CharacterObject character, out int maxXp)` - Check if troop can gain XP (not at max tier)

**Important**: Troops stop gaining XP when they reach their maximum upgrade tier (no further upgrades available).

### Enlisted Implications

For the Enlisted mod:
- **Hero XP**: Use `Hero.AddSkillXp()` for the player and companion progression
- **Troop XP**: If implementing troop training systems, use `MemberRoster.AddXpToTroop()` for party troops
- **Check upgrade availability**: Always verify troops have valid upgrades before awarding XP
- **Respect party context**: Garrison training is separate from field party training

---

## Skills Available (DefaultSkills)

Bannerlord’s default skill set (from `DefaultSkills`) includes:

**Combat**
- `DefaultSkills.OneHanded` (Attribute: Vigor)
- `DefaultSkills.TwoHanded` (Vigor)
- `DefaultSkills.Polearm` (Vigor)
- `DefaultSkills.Bow` (Control)
- `DefaultSkills.Crossbow` (Control)
- `DefaultSkills.Throwing` (Control)

**Movement / Physical**
- `DefaultSkills.Riding` (Endurance)
- `DefaultSkills.Athletics` (Endurance)
- `DefaultSkills.Crafting` (Endurance) *(Smithing)*

**Party / Map / Social**
- `DefaultSkills.Tactics` (Cunning)
- `DefaultSkills.Scouting` (Cunning)
- `DefaultSkills.Roguery` (Cunning)
- `DefaultSkills.Charm` (Social)
- `DefaultSkills.Leadership` (Social)
- `DefaultSkills.Trade` (Social)
- `DefaultSkills.Steward` (Intelligence)
- `DefaultSkills.Medicine` (Intelligence)
- `DefaultSkills.Engineering` (Intelligence)

Notes:
- A `SkillObject` includes its owning **attributes** (`SkillObject.Attributes`).
- Skill “how to learn” helper text exists via `SkillObject.HowToLearnSkillText` (GameTexts lookup).

---

## How Skill XP is Stored + Converted into Skill Levels

Native stores per-skill XP on `HeroDeveloper`:

- `HeroDeveloper._skillXps`: dictionary keyed by `SkillObject`
- `HeroDeveloper._totalXp`: “raw XP” used for hero level progression

When XP is awarded:

1. **Call path** (typical):
   - `Hero.AddSkillXp(skill, xp)` (Hero convenience)  
   - → `HeroDeveloper.AddSkillXp(skill, rawXp, isAffectedByFocusFactor: true, shouldNotify: true)`

2. **Multipliers applied**
   - **Generic XP multiplier**: `Campaign.Current.Models.GenericXpModel.GetXpMultiplier(hero)`
     - Default: 1.0, except some companion/perk cases.
   - **Learning rate / focus factor**:
     - `HeroDeveloper.GetFocusFactor(skill)`
     - Uses `CharacterDevelopmentModel.CalculateLearningRate(...)`
     - Based on:
       - relevant attribute(s)
       - focus points in that skill
       - learning limit (attribute + focus)
       - skill value over the limit reduces the rate

3. **Threshold check and level-up**
   - `CharacterDevelopmentModel.GetSkillLevelChange(hero, skill, skillXpAfter)`
   - Skill levels increase immediately if thresholds are crossed.

### Skill XP thresholds
The XP required per skill level is generated once in `DefaultCharacterDevelopmentModel.InitializeXpRequiredForSkillLevel()` and stored in an internal table up to 1024.

**Important quirk:** If the campaign is in **AccelerationMode.Fast**, the XP requirements are scaled down (multiplied by ~0.3). This changes “how fast skills grow” in accelerated campaigns.

---

## How Hero Level Progression Works (Focus/Attribute Points)

Hero level is driven by `HeroDeveloper._totalXp`, which increments when skill XP is added with `isAffectedByFocusFactor == true` (the default path).

### Level thresholds
`DefaultCharacterDevelopmentModel.InitializeSkillsRequiredForLevel()` builds a cumulative threshold table (up to level 62). The model exposes:

- `CharacterDevelopmentModel.SkillsRequiredForLevel(level)`  

This is what `HeroDeveloper.CheckLevel(...)` uses to determine level-ups.

### Points gained
From `DefaultCharacterDevelopmentModel` constants:

- **Focus points per level**: 1
- **Attribute points every N levels**: 1 per 4 levels
- **Starting focus points**: 5
- **Starting attribute points**: 15

---

## Practical Guidance for Enlisted “Training / Decisions” Rewards

### Preferred API for granting skill progression
Use:

- `Hero.MainHero.AddSkillXp(DefaultSkills.<Skill>, xpAmount)`

This ensures:
- learning rates apply naturally (focus/attributes)
- skill thresholds apply naturally
- hero level progression tracks naturally

### When *not* to use raw skill XP
Avoid directly setting skill values or bypassing native learning unless you have a very specific reason (e.g., save migration, one-off debug).

### Tuning note for Enlisted
If Enlisted adds expensive “Free Time” training (4–6 fatigue), it should grant XP in a way that feels consistent with:
- combat hits (native `DefaultSkillLevelingManager` calls `hero.AddSkillXp(...)`)
- party/map actions (trade profits, scouting, etc. also route through skill XP)

So: prefer fewer, higher-cost actions that award XP to **the skills that match current equipment/formation**, rather than huge static lists.

---

## Acceptance Checklist (for future Enlisted implementations)

- We award skill XP via `Hero.AddSkillXp(...)` (not direct level edits) except for migrations/debug.
- We do not bypass native learning rate unless intentional and documented.
- We keep rewards stable under different campaign acceleration settings.
- We can trace any “skill gained” behavior back to one of the native model layers:
  - GenericXpModel multiplier
  - CharacterDevelopmentModel learning rate / limit
  - Skill XP threshold table


