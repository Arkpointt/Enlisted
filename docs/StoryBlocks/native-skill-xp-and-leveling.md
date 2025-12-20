# Native Skill XP + Leveling (Bannerlord) — Research Notes

**Last Updated:** 2025-12-18  
**Audience:** Enlisted developers  
**Purpose:** Capture how Bannerlord awards skill XP, converts it into skill levels, and how “hero level” (attribute/focus points) advances. This is a reference for designing Enlisted “Training / Decisions / Schedule” rewards without fighting native progression.

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


