# Feature Spec: Military Duties System

## Overview
Data-driven duty system that lets enlisted players pick an active military role. Duties grant daily skill XP and modify service pay via wage multipliers. This system also owns formation-based daily training XP.

## Purpose
Add variety and specialization to military service. Different duties provide different benefits (skill bonuses, equipment access, special abilities) and make each playthrough feel different.

## Inputs/Outputs

**Inputs:**
- Player's current formation type (`infantry`, `archer`, `cavalry`, `horsearcher`, `naval`)
- Player's enlistment tier (tier-gates some duties)
- Current enlisted lord party (for optional officer-role assignment)
- JSON configuration: `ModuleData/Enlisted/duties_system.json`

**Outputs:**
- Active duty assignments with real benefits
- Skill bonuses applied daily/on events
- Service wage multiplier changes (Pay System consumes these values)
- Optional officer-role assignment (Quartermaster/Engineer/Surgeon/Scout) when enabled by the mod
- Trigger surface for story/event systems (`has_duty:{id}` tokens)
- Status display in the enlisted status UI

## Behavior

### Duty Assignment

**T1 Players:**
- Auto-assigned "Runner" duty (grunt work) upon enlistment
- No duty selection available until T2

**T2+ Players (Proving Event):**
- Formation choice is made during the T1->T2 proving event
- Starter duty is auto-assigned based on chosen formation:
  | Formation | Starter Duty |
  |-----------|--------------|
  | Infantry | Runner |
  | Archer | Lookout |
  | Cavalry | Messenger |
  | Horse Archer | Scout |
  | Naval | Boatswain |

**Duty Request System (T2+):**
- Players cannot freely switch duties; they must **request a duty change**
- Request approval depends on:
  - **Cooldown**: 14 days between requests
  - **Lance Reputation**: Minimum 10 required
  - **Tier Requirement**: Duty's minimum tier must be met
  - **Formation Requirement**: Duty must be compatible with player's formation
- Approved requests show: "{LANCE_LEADER_SHORT} approves your transfer to {DUTY}."
- Denied requests show the specific blocking reason

**Duty Selection UI (Camp Management → Duties tab):**

The Duties tab uses a **Kingdom/Policies style pattern** with select-then-confirm:
- Left panel: List of all duties as selectable items
- Right panel: Selected duty's detailed description, effects, and requirements
- **Click a duty to view details** - does NOT assign automatically
- **Click "Request Assignment" button** to actually change your duty
- **Lance Rep bar at bottom** - shows your current lance reputation (affects approval)
- **Current duty persists** until you successfully request a change

**Duty List Display:**
| State | Display | Behavior |
|-------|---------|----------|
| Current duty | Highlighted/selected, "(Current)" badge | Shows "This is your current duty assignment. It persists until you request a change." |
| Available | Normal style, clickable | Click → request assignment (T2+) or immediate assignment (T1) |
| Tier locked | Greyed out, "Requires {Rank}" | Click shows requirements in details panel |
| Formation locked | Greyed out, "{Formations} only" | Click shows requirements in details panel |
| On cooldown | Greyed out, "Cooldown: Xd" | Shows cooldown time remaining |

**Details Panel (always visible on right):**
- Duty name and description (fluff text for immersion)
- Effects list: skill XP bonuses, wage multipliers, special abilities
- Requirements: tier, formation, reputation needed
- Current status: why it's locked (if applicable)
- Request button with context-appropriate text

**Assignment Flow:**
- Select a duty from the list to view its details in the right panel
- Review effects, requirements, and your current Lance Reputation
- **T1 Players**: Click "Assign Duty" button (no approval needed, free to change)
- **T2+ Players**: Click "Request Assignment" button (requires lance leader approval)
- Success message shows approval: "{LANCE_LEADER_SHORT} approves your transfer to {DUTY}."
- Failure shows specific blocking reason (cooldown, reputation, tier, etc.)
- **Duties persist** across sessions - your current duty stays active until changed

### Daily Processing
- Skill bonuses awarded based on active duties
- Formation-based skill training applied automatically (see Formation Training section below)
- Wage multipliers applied by the Pay System at pay muster time
- Any duty that is no longer valid (missing from config) is removed safely

### Formation Training System
- Automatic daily skill XP based on player's military formation
- Configured per-formation in `duties_system.json` under `formation_training`
- Continues during temporary leave (training does not stop while on leave)
- Uses authentic military training descriptions for immersion

### Formation-Based Filtering
- Duties have `required_formations` in `duties_system.json` that determine which formations can select them:
  - Infantry: `runner`, `quartermaster`, `field_medic`, `armorer`, `engineer`
  - Archer: `scout`, `lookout`
  - Cavalry: `scout`, `messenger`
  - Horse Archer: `scout`, `messenger`
  - Naval (War Sails): `boatswain`, `navigator`
- All duties are **shown** in the menu, but formation-incompatible duties are greyed out with a tooltip

**Expansion gating (War Sails):**
- Naval duties (`boatswain`, `navigator`) are only available when the War Sails expansion is detected.

## Technical Implementation

**Files:**
- `EnlistedDutiesBehavior.cs` - Core duty management, benefit application, formation training, duty request system, and duty filtering APIs
- `CampDutiesVM.cs` - Camp Management Duties tab ViewModel (orders-screen style UI)
- `CampManagementScreen.cs` - Full-screen Camp Management interface
- `DutyConfiguration.cs` - JSON loading and validation  
- `ModuleData/Enlisted/duties_system.json` - Duty definitions + formation training configuration

**Configuration Structure:**
```json
{
  "schemaVersion": 1,
  "enabled": true,
  "duties": { "...": "..." },
  "formation_training": { "enabled": true, "formations": { "...": "..." } }
}
```

**Duty Request APIs:**
```csharp
// Request a duty change (T2+ players)
DutyRequestResult RequestDutyChange(string newDutyId);

// Check if duty request is on cooldown
bool IsDutyRequestOnCooldown();

// Get days remaining on cooldown
int GetDutyRequestCooldownRemaining();
```

**Duty APIs:**
- `GetAllDuties()` - Returns all duties (expansion-gated only)
- `IsDutyCompatibleWithFormation(DutyDefinition)` - Checks if duty matches player's formation
- `IsExpansionActive(string)` - Checks if an expansion (e.g., "war_sails") is active
- `IsDutySelectableByPlayer(DutyDefinition)` - Checks tier and other requirements
- `GetDutyById(string)` - Lookup duty definition by ID

**Benefit Application:**
- Formation Training: `Hero.MainHero.AddSkillXp(skill, amount)` applied daily for all formation skills
- Duty Skills: `Hero.MainHero.AddSkillXp(skill, bonusAmount)` for active duty assignments
- Officer roles: optional integration with the party role system (only where safe and supported)

**Event integration:**
- Duties expose a stable trigger token for event content: `has_duty:{id}`.
- Each duty in `duties_system.json` includes an `event_prefix` to keep duty event IDs consistent (e.g. `qm_`, `med_`, `arm_`, `eng_`).
- The `HasActiveDuty(string dutyId)` API is used by the Lance Life Events trigger evaluator.

**Formation Detection:**
- Formation chosen during T1->T2 proving event (replaces old troop selection)
- Stored in `_playerFormation` field for consistency across sessions
- Existing saves migrate by detecting formation from troop or equipment

## Edge Cases

**Invalid JSON Configuration:**
- Validation on load prevents crashes from bad config
- Default fallback configuration if file corrupted
- Error logging with specific validation failure details

**Formation Type Changes:**
- Formation is locked after T1->T2 proving event
- Incompatible duties are removed if formation somehow changes
- Notify player of duty changes

**Duty Slot Limits:**
- Enforce maximum duties based on tier (1 at low tier, 3 at high tier)
- Handle tier decrease (rare but possible) by removing excess duties
- Priority system for which duties to keep

**Save/Load Compatibility:**
- Active duties persist through save/load correctly
- Benefits recalculated on load to handle config changes
- Graceful handling of missing duty definitions in saves
- Existing saves migrate: starter duty auto-assigned if T2+ with no duties

## Acceptance Criteria

- [x] JSON configuration loads and validates correctly
- [x] All duties shown in menu (incompatible ones greyed out with tooltip)
- [x] Skill bonuses applied correctly and consistently  
- [x] Optional officer-role assignment works safely when enabled (Quartermaster/Engineer/Surgeon/Scout)
- [x] Duty selection constraints enforced based on tier and formation
- [x] Configuration changes work without recompiling mod
- [x] Save/load maintains duty assignments correctly
- [x] Duty request system enforces cooldown and approval requirements
- [x] Culture-specific ranks shown in tier requirements

## Debugging

**Common Issues:**
- **Duties not showing**: Check expansion gating (naval requires War Sails)
- **Benefits not applying**: Verify daily tick events are firing correctly
- **Config not loading**: Check JSON syntax and file location
- **Request denied unexpectedly**: Check lance reputation and cooldown status

**Log Categories:**
- "Duties" - Duty assignment, request approval, and benefit application
- "ConfigManager" - JSON loading and validation
- Look in `ModuleData/Enlisted/duties_system.json` for configuration structure
