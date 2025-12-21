# Enlisted – Core Gameplay (Consolidated)

**last updated:** December 20, 2025  
**Goal:** One maintained document for the mod’s core gameplay loop and the systems that implement it (Mod Version v0.9.0, Game Target v1.3.12).

This file replaces the older split docs under:
- `docs/Features/Core/*.md`
- `docs/Features/Gameplay/*.md`

---

## Index

- [The Three Pillars](#the-three-pillars)
- [System Map (Code + Data)](#system-map-code--data)
- [Enlistment](#enlistment)
- [Orders System (Chain of Command)](#orders-system-chain-of-command)
- [Emergent Identity (Traits + Reputation)](#emergent-identity-traits--reputation)
- [Native Game Menu (Interface)](#native-game-menu-interface)
- [Company Needs](#company-needs)
- [Pay System (Muster Ledger)](#pay-system-muster-ledger)
- [Companions + Retinue](#companions--retinue)
- [Fatigue + Conditions](#fatigue--conditions)
- [Town Access + Temporary Leave](#town-access--temporary-leave)
- [Content Authoring (Quick Rules)](#content-authoring-quick-rules)

---

## The Three Pillars

The mod is built on three core transformations introduced in v2.0:

1.  **Emergent Identity**: Identity emerges from choices, traits, and reputation rather than prescribed systems. Your specialization (Scout, Medic, Officer) is determined by your actions and native Bannerlord traits.
2.  **Orders from Chain of Command**: Passive duties are replaced by explicit directives from your superiors. You receive orders from Sergeants, Lieutenants, or your Lord, with meaningful stakes for success or failure.
3.  **Native Game Menu Interface**: All custom UI (ViewModels/XML) has been replaced by the native Bannerlord Game Menu system. It is fast, keyboard-friendly, and integrates seamlessly with the base game.

---

## System Map (Code + Data)

### Core Code Areas

- **Enlistment + State**: `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
- **Orders System**: `src/Features/Orders/Behaviors/OrderManager.cs`
- **Identity & Traits**: `src/Features/Identity/EnlistedStatusManager.cs`
- **Reputation & Escalation**: `src/Features/Escalation/EscalationManager.cs`
- **Native Menus**: `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`
- **Company Needs**: `src/Features/Company/CompanyNeedsManager.cs`
- **Context Analysis**: `src/Features/Context/ArmyContextAnalyzer.cs`
- **News & Reports**: `src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs`
- **Quartermaster**: `src/Features/Equipment/Behaviors/QuartermasterManager.cs`
- **Medical Care**: `src/Features/Conditions/EnlistedMedicalMenuBehavior.cs`

### Core Data Sources (ModuleData)

- **Config**: `ModuleData/Enlisted/enlisted_config.json`
- **Progression**: `ModuleData/Enlisted/progression_config.json`
- **Orders Catalog**: `ModuleData/Enlisted/Orders/*.json`
- **Events Catalog**: `ModuleData/Enlisted/Events/*.json` (Role-based & Contextual)
- **Rank Titles**: `ModuleData/Enlisted/progression_config.json` (Culture-specific)

---

## Enlistment

Enlisted turns Bannerlord into a “soldier career” loop:

1.  **Enlist** with a lord. Your party is hidden, and you follow the lord's army.
2.  **Live the Routine**: Receive **Orders**, manage **Company Needs**, and navigate **Events**.
3.  **Fight as a Unit**: Participate in the lord's battles. At higher tiers, you lead a **Retinue**.
4.  **Get Paid**: Wages accrue in the **Muster Ledger** and are paid during Pay Muster.
5.  **Progress**: Advance from T1 to T9. Rank gates authority and the scale of orders you receive.
6.  **Leave**: Discharge honorably, transfer, or desert.

---

## Orders System (Chain of Command)

Replaces the legacy passive duties system with explicit, mission-driven tasks.

-   **Frequency**: Orders are issued every ~3 days (adjusted by rank and campaign tempo).
-   **Chain of Command**: Your rank determines who issues the order (e.g., T1-T2 get orders from a Sergeant; T7+ receive strategic orders from the Lord).
-   **Requirements**: Orders are filtered by your current Rank, Skills, and Traits.
-   **Outcomes**: Success or failure impacts **Reputation**, **Company Needs**, and grants **Skill/Trait XP**.
-   **Discharge Risk**: Repeatedly declining orders (5+ times) triggers a risk of dishonorable discharge.

---

## Emergent Identity (Traits + Reputation)

### Traits & Roles
Your "role" in the company is detected dynamically from your native traits:
-   **Scout**: High Scouting/Rogue skills and traits.
-   **Medic**: High Surgeon trait and Medicine skill.
-   **Officer**: High Commander trait and Leadership skill.
-   **Soldier**: The default role for general combatants.

### Expanded Reputation
We track three distinct reputation values (-50 to +100):
1.  **Lord Reputation**: Your standing with the lord you serve.
2.  **Officer Reputation**: How the NCOs and officers perceive your competence.
3.  **Soldier Reputation**: Your popularity and respect among the rank-and-file.

---

## Native Game Menu (Interface)

All interactions occur through the **Enlisted Status** menu hub:
-   **Enlisted Status**: View rank, active orders, and report summaries.
-   **Camp Submenu**: Perform activities like Rest, Train, or Equipment Checks.
-   **Reports Submenu**: Access the Daily Brief, Service Record, and Company Status.
-   **Decisions Submenu**: Handle pending event choices.
-   **Status Submenu**: Detailed view of reputation, traits, and role.

---

## Company Needs

The company's effectiveness is tracked via five core needs:
-   **Readiness**: Combat effectiveness and preparation.
-   **Morale**: The unit's will to fight.
-   **Supplies**: Food and basic consumables.
-   **Equipment**: Maintenance and quality of gear.
-   **Rest**: Recovery from fatigue.

Orders and events directly impact these needs. Low supplies or equipment will restrict access to certain services (like the Quartermaster).

---

## Pay System (Muster Ledger)

Wages accrue into a **Muster Ledger** based on your rank and performance. They are paid out periodically at **Pay Muster** events.
-   **Pay Tension**: Rises when pay is late or disrupted, leading to potential corruption or desertion.

---

## Companions + Retinue

-   **Companions**: Stay with you during enlistment and fight alongside you in your formation.
-   **Retinue (T4+)**: You gain command over a small squad of soldiers. Their size and quality grow as you reach higher tiers (up to T9).

---

## Fatigue + Conditions

-   **Fatigue**: A budget consumed by orders and camp activities. Restores while resting in camp.
-   **Conditions**: Tracks injuries, illnesses, and exhaustion. These must be managed via the Medical Care menu in camp.

---

## Town Access + Temporary Leave

-   **Town Access**: Safely explore settlements while your party is hidden.
-   **Temporary Leave**: Suspend service for a limited time (e.g., to handle clan business). Expiration without return is treated as desertion.

---

## Content Authoring (Quick Rules)

-   **Events**: Tag with `role` and `context` (e.g., `scout`, `war`, `camp`).
-   **Orders**: Defined in JSON with specific requirements and tiered consequences.
-   **Strings**: All text must be localized in `enlisted_strings.xml`.
