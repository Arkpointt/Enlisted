# Enlistment System

**Summary:** The enlistment system manages how players join a lord's service, progress through ranks (T1-T9), and eventually leave through discharge or desertion. This document covers the deep-dive technical details of enlistment mechanics, muster cycles, discharge types, and re-entry as a reservist.

**Status:** ✅ Current  
**Last Updated:** 2025-12-22  
**Related Docs:** [Core Gameplay](core-gameplay.md), [Onboarding & Discharge](onboarding-discharge-system.md), [Pay System](pay-system.md)

> **Note:** For high-level gameplay overview, see `core-gameplay.md`. This document provides technical implementation details.

---

## Index

- [Overview](#overview)
- [Purpose](#purpose)
- [Inputs/Outputs](#inputsoutputs)
- [Behavior](#behavior)
- [Discharge & Final Muster (Pending Discharge Flow)](#discharge--final-muster-pending-discharge-flow)
- [Re-entry & Reservist System](#re-entry--reservist-system)
- [Technical Implementation](#technical-implementation)
- [Edge Cases](#edge-cases)
- [Acceptance Criteria](#acceptance-criteria)
- [Debugging](#debugging)

---

## Overview
Core military service functionality that lets players enlist with any lord, follow their armies, participate in military life, earn XP and wages, and advance through a rank-based progression system (T1-T9). The experience is driven by orders from the chain of command and emergent identity through traits and reputation.

## Purpose
Provide the foundation for military service: enlist with a lord, follow their movements, participate in battles, progress through tiers, and earn wages via the muster ledger. Progression is gated by orders and proving events, and the player's identity is defined by their reputation with the Lord, Officers, and Soldiers.

## Inputs/Outputs

**Inputs:**
- Player dialog choice to enlist with a lord (kingdom or minor faction)
- Lord availability and relationship status
- Current campaign state (peace/war, lord location, etc.)
- Real-time monitoring of lord and army status

**Outputs:**
- Player joins lord's kingdom as a soldier.
- Player party hidden from map (`IsVisible = false`, Nameplate hidden)
- Player follows enlisted lord's movements (including naval travel)
- Daily wage accrual into muster ledger; periodic pay muster incident (inquiry fallback) handles payouts.
- XP progression and Rank advancement (T1-T9).
- Participation in lord's battles and army activities.
- Reputation tracking (Lord, Officer, Soldier).
- Company Needs management (Readiness, Morale, Supplies, Equipment, Rest).
- Managed discharge or desertion.

Related systems (shipping):
- Discipline can temporarily block promotion until the player recovers (clean service / decay). (Config: `escalation.enabled`, enabled by default.)
- The enlisted status UI (Native Game Menu) shows standing, conditions, and active orders.

## Behavior

**Enlistment Process:**
1. Talk to lord -> Express interest in military service (kingdom flow or minor-faction mercenary flow)
2. Lord evaluates player (relationship, faction status)
3. **Army Leader Restriction**: If player is leading their own army, lord will refuse with roleplay dialog explaining they must disband their army first
4. Player confirms -> Immediate enlistment with safety measures
5. Player party becomes invisible (`IsVisible = false`) and Nameplate removed via Patch
6. Begin following lord and receiving military benefits
7. **Cross-Faction Baggage Check**: If player has belongings stored with a different faction, they're prompted to handle them before proceeding:
   - **Send a courier (50g + 5% of value)**: Items arrive in 3 days, posted to personal news feed.
   - **Sell remotely (40%)**: Immediate gold at reduced rate.
   - **Abandon**: Items lost forever.
8. Bag check is deferred **1 in-game hour** after enlistment and fires as a native map incident. It only triggers when safe and falls back to the inquiry prompt.
   - **Stow it all (50g)**: stashes all inventory + equipped items into the baggage train.
   - **Sell it all (60%)**: liquidates inventory + equipped items at **60%**.
   - **I'm keeping one thing (Roguery 30+)**: attempts to keep a single item.
   - Baggage is tagged with the current faction for cross-faction tracking.
9. **Minor factions only:** Mirror the lord faction's current wars to the player clan.
10. **Onboarding Events**: New enlistees receive introductory events based on their experience track (green/seasoned/veteran). Events fire in stages (1→2→3→complete) and certain options advance the stage.

**Daily Service:**
- Follow enlisted lord's army movements.
- **Strategic Context Awareness**: Experience changes based on faction position (Desperate to Offensive) and 8 distinct strategic contexts (e.g., Grand Campaign, Winter Camp).
- Receive **Orders** every 3-5 days from the chain of command (config-driven timing), filtered by strategic tags to match the current situation.
- Manage **Company Needs** through choices and activities in the Camp menu, with predictions for upcoming requirements.
- Participate in battles when lord fights.
- Accrue daily wages into the muster ledger; periodic pay muster incident handles payouts.
- Earn XP: +25 daily, +25 per battle, +1 per enemy killed.

**Wage Breakdown (muster ledger):**
- Soldier's Pay: Base wage from config (default 10)
- Combat Exp: +1 per player level
- Rank Pay: +5 per tier
- Service Seniority: +1 per 200 XP accumulated
- Army/Campaign Bonus: +20% when lord is in army
- Order Multipliers: Completion of high-priority orders can grant bonuses.
- Probation Multiplier: Applied if on probation (reduces wage).
- Wages accrue daily into `_pendingMusterPay`; paid out at pay muster (~12 days).

**Tier Progression:**
Rank progression is gated by proving events and meeting service requirements (XP, reputation, time in rank).

| Tier | XP Required | Title (Empire Example) | Authority |
|------|-------------|------------------------|-----------|
| 1 | 0 | Tiro | Raw Recruit |
| 2 | 800 | Miles | Formation Choice |
| 3 | 3,000 | Immunes | Specialist Roles |
| 4 | 6,000 | Principalis | Squad Command (5) |
| 5 | 11,000 | Evocatus | NCO Leadership |
| 6 | 19,000 | Centurion | Officer Command |
| 7-9 | 30k+ | Strategic Ranks | Strategic Command |

**Culture-Specific Ranks:**
Rank names are determined by the enlisted lord's culture:
- Empire: Tiro -> Miles -> Immunes -> Principalis -> Evocatus -> Centurion
- Vlandia: Peasant -> Levy -> Footman -> Man-at-Arms -> Sergeant -> Knight Bachelor
- Sturgia: Thrall -> Ceorl -> Fyrdman -> Drengr -> Huskarl -> Varangian
- (See `RankHelper.cs` for all cultures)

**Reputation System:**
Identity is tracked via three reputation scales (-50 to +100):
- **Lord Reputation**: Impacted by strategic success and loyalty.
- **Officer Reputation**: Impacted by order completion and competence.
- **Soldier Reputation**: Impacted by camp activities and shared hardship.

**Orders System:**
Instead of passive assignments, players receive explicit orders:
- **T1-T3**: Group tasks and basic duties (Scouting, Guarding).
- **T4-T6**: Tactical missions and squad leadership.
- **T7-T9**: Strategic directives from the Lord.
Success improves reputation and company needs; failure or declining orders carries heavy penalties.

**Promotion Requirements (T2+):**
- XP threshold (varies by tier)
- Days in rank (minimum service time)
- Orders completed
- Battles survived
- Reputation (minimum threshold for Lord/Officer/Soldier)
- Discipline (not too high)

**Service Monitoring:**
- Continuous checking of lord status (alive, army membership, etc.)
- 14-day grace period if lord dies, is captured, or army defeated.
- The player clan stays inside the kingdom throughout the grace window.

**Service Transfer (Leave/Grace):**
- While on leave or in grace period, player can talk to other lords in the same faction to transfer service.
- Transfer preserves all progression (tier, XP, reputation, kills).

## Discharge & Final Muster (Pending Discharge Flow)

- Managed discharge is requested from the **Camp** menu.
- Selecting "Request Discharge" sets `IsPendingDischarge = true`; resolves at the next pay muster.
- Discharge rewards scale by service length (Washout <100, Honorable 100-199, Veteran/Heroic 200+).
- **Washout**: -10 lord / -10 officer rep; no pension; gear stripped; probation on re-entry.
- **Honorable**: +10 lord / +5 officer rep; severance gold; pension; keep armor, return weapons.
- **Veteran/Heroic**: +30 lord / +15 officer rep; larger severance/pension; keep armor.
- **Smuggle**: Deserter path; keep all gear; crime rating gain; reputation hit.

## Re-entry & Reservist System

- **Washout/Deserter**: Start at Tier 1, probation status (reduced wage, fatigue cap).
- **Honorable**: Start at Tier 3, XP bonus, reputation bonus.
- **Veteran/Heroic**: Start at Tier 4, larger XP/reputation bonuses.

## Technical Implementation

- **EnlistmentBehavior.cs**: Core service state and lord tracking.
- **OrderManager.cs**: Selection and execution of missions.
- **EscalationManager.cs**: Tracking of Lord, Officer, and Soldier reputation.
- **EnlistedMenuBehavior.cs**: Native Game Menu implementation for all status and camp functions.
- **CompanyNeedsManager.cs**: Tracking of company-wide needs (Readiness, Morale, etc.).

## Edge Cases
(Standard edge cases for enlistment system)

## Acceptance Criteria
- [x] Can enlist with any lord that accepts player.
- [x] Orders issue correctly based on rank and role.
- [x] Reputation changes apply based on order outcomes.
- [x] Native Game Menu displays all relevant status info.
- [x] Discharge and Re-entry flows function as intended.
- [x] Company Needs affect gameplay (e.g., equipment access).
