# Formation Assignment and Teleportation System

The **Formation Assignment and Teleportation System** ensures that enlisted soldiers and their squads are correctly positioned on the battlefield according to their unit type and specialization.

## Index

- [Overview](#overview)
- [How It Works](#how-it-works)
- [Positioning Logic](#positioning-logic)
- [Technical Implementation](#technical-implementation)
- [Edge Cases](#edge-cases)

---

## Overview

In Bannerlord, your map party normally spawns slightly behind the main army. This system fixes that by automatically assigning you to the correct formation (e.g., Infantry, Ranged) and teleporting you and your squad to that formation's center at the start of a battle.

---

## How It Works

### Formation Assignment
At the start of every mission:
1.  **Detection**: The system identifies your specialization (set during your T1->T2 promotion or through your dominant traits).
2.  **Mapping**: You are assigned to the corresponding formation class (Infantry, Ranged, Cavalry, or Horse Archer).
3.  **Squad Consolidation**: At Tier 4 and above, your active companions and retinue are moved into your formation, creating a **Unified Squad**.

### Command Authority
-   **Sergeant Role**: You are given direct command over your specific squad members.
-   **Restricted Access**: You do not have access to the full Order of Battle or command over other army formations.

---

## Positioning Logic

To prevent players from being stranded far from their comrades, the system executes a precise teleportation sequence:

### Teleport Priority
1.  **Formation Position**: Teleports to the center of all units assigned to your formation type.
2.  **Lord Position**: Falls back to the lord's location if the formation is not yet populated.
3.  **Allied Agent**: Last resort fallback to any allied soldier.

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
-   **Reinforcements**: The system detects when you spawn as a reinforcement wave and teleports you directly to the active formation line.
-   **Companion Commands**: Harmony patches prevent your high-power companions from being assigned as captains of other formations, keeping them within your squad.
