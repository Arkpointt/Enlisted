# Formation Assignment and Teleportation System

**Summary:** The formation assignment system ensures enlisted soldiers and their squads are correctly positioned on the battlefield. The system assigns players to the Infantry formation, teleports them to the correct position within that formation, and at Tier 7+ consolidates their retinue and companions into a unified squad under their command.

**Status:** ✅ Current  
**Last Updated:** 2025-12-23  
**Related Docs:** [Core Gameplay](../Core/core-gameplay.md), [Retinue System](../Core/retinue-system.md)

**Note:** The duty-based formation selection system (Infantry/Ranged/Cavalry/Horse Archer) is planned but not yet integrated. Currently all players are assigned to Infantry formation. The code includes comments indicating this connection is in progress.

---

## Index

- [Overview](#overview)
- [How It Works](#how-it-works)
- [Positioning Logic](#positioning-logic)
- [Technical Implementation](#technical-implementation)
- [Edge Cases](#edge-cases)

---

## Overview

In Bannerlord, your map party normally spawns slightly behind the main army. This system fixes that by automatically assigning you to a formation and teleporting you and your squad to that formation's position at the start of a battle.

At Tier 2 (T1->T2 promotion), the promotion system detects your formation type from your equipped gear (Infantry/Archer/Cavalry/Horse Archer) and displays a message telling you your assigned role. However, this detection is not currently stored or used by the battle system. All enlisted soldiers are assigned to the Infantry formation regardless of their T2 "assignment."

---

## How It Works

### Formation Assignment
At the start of every mission:
1.  **Formation**: The system currently assigns you to the Infantry formation by default. The duty-based formation selection system is planned but not yet connected to battle positioning.
2.  **Squad Consolidation**: At Tier 7 and above, your active companions and retinue are moved into your formation, creating a **Unified Squad**.
3.  **Position Teleport**: The system teleports you to the correct position within your assigned formation to fix cases where your party spawned behind the main army.

### Command Authority
-   **Below Tier 7**: No command authority. You are a soldier in the ranks with no ability to issue orders.
-   **Tier 7+ (Commander)**: You are given sergeant status with direct command over your specific squad formation (your retinue and companions).
-   **Restricted Access**: You do not have access to the full Order of Battle or command over other army formations, even at Commander tier.

---

## Positioning Logic

To prevent players from being stranded far from their comrades (which happens when your map party was slightly behind the lord when battle started), the system executes a precise teleportation sequence:

### Teleport Priority
1.  **Formation Position**: Teleports to the center of all units assigned to your formation type (Infantry by default).
2.  **Solo Formation Fallback**: If your formation only contains you, the system searches for the largest populated allied formation to join.
3.  **Lord Position**: Falls back to the lord's formation location if no populated allied formation is found.
4.  **Allied Agent**: Last resort fallback to any allied soldier if the lord hasn't spawned yet.

### Context-Aware Placement
-   **Field Battles**: Positioned 5 meters behind the formation's front line to avoid spawning stuck inside other troops.
-   **Siege Assaults**: Positioned at the exact formation center to ensure you are with the initial wave of attackers.

---

## Technical Implementation

-   **Behaviors**: `EnlistedFormationAssignmentBehavior.cs` manages the mission-start logic.
-   **Retry Window**: The system makes up to 120 attempts (~2 seconds) to find a valid position, allowing time for all army agents to spawn.
-   **Mounts**: Teleportation automatically handles mounts and rider synchronization.

---

## Edge Cases

-   **Naval Battles**: Teleportation is disabled in naval missions, as the Naval DLC uses a separate ship-based spawn system.
-   **Reinforcements**: The system detects when you spawn as a reinforcement wave (spawning at map edge, not with formation) and always teleports you to the player's formation regardless of distance.
-   **Companion Commands**: The system actively removes companions from formation captain roles to keep command authority with the player. This happens both in the player's own formation and across all formations (since the native AI can assign companions as captains anywhere).
-   **Stay Back Companions**: Companions set to "stay back" are queued for removal after spawn phase completes. They are faded out without counting as casualties. This removal is deferred to avoid corrupting the native spawn loop.
-   **Squad Teleport**: When soldiers from your party (retinue/companions) are assigned to your formation at T7+, they are teleported to positions near you if they spawned far away. This ensures your entire squad spawns together rather than scattered across different spawn points.
