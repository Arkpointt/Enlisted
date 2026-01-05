# Enlisted Balance Values

**Config Source:** `ModuleData/Enlisted/Config/progression_config.json`

> These values WILL change during balancing. Query this file rather than hardcoding.

## Tier Progression

| Tier | XP Required | Rank (Generic) | Duration |
|------|-------------|----------------|----------|
| T1 | 0 | Follower | 1-2 weeks |
| T2 | 800 | Recruit | 2-4 weeks |
| T3 | 3,000 | Free Sword | 1-2 months |
| T4 | 6,000 | Veteran | 2-3 months |
| T5 | 11,000 | Blade | 3-4 months |
| T6 | 19,000 | Chosen | 4-6 months |
| T7 | 30,000 | Captain | 6+ months |
| T8 | 45,000 | Commander | Officer track |
| T9 | 65,000 | Marshal | Endgame |

### Tracks
- **Enlisted Track:** T1-T4 (raw recruit to veteran soldier)
- **Officer Track:** T5-T6 (elite soldier to NCO leader)
- **Commander Track:** T7-T9 (captain to strategic leader)

## XP Sources

From `progression_config.json.xp_sources`:
- Daily base XP: **25**
- Battle participation: **25**
- XP per kill: **2**

## Wage System

From `progression_config.json.wage_system`:
- Daily base wage: **10 gold**
- Tier bonus per level: **+5 gold**
- Maximum base wage: **200 gold**

### Assignment Multipliers
| Assignment | Multiplier |
|------------|------------|
| Grunt work | ×0.8 |
| Guard duty | ×0.9 |
| Cook | ×0.9 |
| Foraging | ×1.0 |
| Scout | ×1.1 |
| Quartermaster | ×1.2 |
| Surgeon | ×1.3 |
| Engineer | ×1.4 |
| Sergeant | ×1.5 |
| Strategist | ×1.6 |

## Activity Level Modifiers

From `ContentOrchestrator.cs`:
- Quiet: ×0.3 (garrison + peacetime)
- Routine: ×0.6 (garrison + war)
- Active: ×1.0 (campaign + war)
- Intense: ×1.5 (siege, desperate war)

## Opportunity Budget by Phase

From `DetermineOpportunityBudget()`:
| Situation | Dawn | Midday | Dusk | Night |
|-----------|------|--------|------|-------|
| Garrison | 3 | 2 | 3 | 1 |
| War Marching | 1 | 1 | 2 | 1 |
| Siege Attacking | 1 | 1 | 1 | 1 |
| Siege Defending | 0 | 0 | 0 | 0 |

## Reputation Thresholds

Reputation tracks (0-100) managed by `EscalationManager`:

|| Status | Threshold | Effects |
||--------|-----------|----------|
|| **Critical** | < 20 | Severe penalties, blocked promotions |
|| **Poor** | 20-39 | Moderate penalties |
|| **Fair** | 40-59 | Neutral standing |
|| **Good** | 60-79 | Positive effects |
|| **Excellent** | 80+ | Strong bonuses |

**Tracks:** Soldier Reputation, Officer Reputation, Lord Reputation
**Starting value:** 50 (Fair)

## Escalation Track Thresholds

Escalation tracks managed by `EscalationManager`:

|| Track | Range | Meaning |
||-------|-------|----------|
|| **Scrutiny** | 0-10 | Officer attention level |
|| **Discipline** | 0-10 | Enforcement strictness |
|| **Medical Risk** | 0-5 | Illness onset at 3+ |

**Passive decay:** Daily reduction towards baseline
**Threshold events:** Trigger at milestones (e.g., scrutiny 3, 7, 9)

## Company Needs Thresholds

Company needs are 0-100 tracks monitored by `CompanySimulationBehavior`:

|| Status | Threshold | Effects |
||--------|-----------|----------|
|| **Critical** | < 20 | Crisis events trigger |
|| **Low** | 20-40 | Negative pressure accumulates |
|| **Normal** | 40-70 | Baseline state |
|| **Good** | > 70 | Positive morale effects |

**Tracks:** Supplies, Morale, Rest, Readiness, Equipment

## Simulation Rates

From `simulation_config.json` (per 100 soldiers per day):
- Base sickness rate: **2.0%** (2 sick per day in 100-man company)
- Base injury rate: **1.0%**
- Base desertion rate: **0.5%**
- Base recovery chance: **15%**
- Base death chance: **2%** (for wounded/sick)

**Pressure thresholds** (days before crisis):
- Low supplies: 3 days
- Low morale: 3 days
- Low rest: 2 days
- High sickness: 2 days

## Config File Locations

| Config | File |
|--------|------|
| Progression | `ModuleData/Enlisted/Config/progression_config.json` |
| Orchestrator | `ModuleData/Enlisted/Config/orchestrator_overrides.json` |
| Simulation | `ModuleData/Enlisted/Config/simulation_config.json` |
| Baggage | `ModuleData/Enlisted/Config/baggage_config.json` |
| Settings | `ModuleData/Enlisted/Config/settings.json` (log levels) |
