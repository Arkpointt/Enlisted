# Feature Specifications

Comprehensive documentation for all major features in the Enlisted mod. Each spec follows a consistent structure for easy reference and implementation.

---

## Core Systems

The foundational systems that enable military service.

- **[Enlistment System](Core/enlistment.md)** - Core service mechanics: wages, XP, kills, retirement, grace periods, army following
- **[Companion Management](Core/companion-management.md)** - Companion behavior during enlistment and missions
- **[Duties System](Core/duties-system.md)** - Military roles and assignments with skill bonuses
- **[Formation Training](Core/formation-training.md)** - Automatic formation-based skill XP progression

---

## User Interface

Menu systems and player interaction interfaces.

- **[Menu Interface](UI/menu-interface.md)** - Main enlisted status menu and navigation
- **[Command Tent](UI/command-tent.md)** - Service records, personal retinue (Tier 4+), companion management hub
- **[Dialog System](UI/dialog-system.md)** - Conversation management with lords
- **[Quartermaster](UI/quartermaster.md)** - Equipment selection UI and variant management

---

## Gameplay Features

Additional gameplay mechanics and player choices.

- **[Temporary Leave](Gameplay/temporary-leave.md)** - 14-day leave system with desertion penalties
- **[Troop Selection](Gameplay/troop-selection.md)** - Real troop choice for promotions and formations
- **[Town Access System](Gameplay/town-access-system.md)** - Settlement exploration and access control

---

## Missions

Special mission types available to enlisted players.

- **[Recon Mission](Missions/recon-mission.md)** - Tier 4+ scouting missions with intel gathering, risk/reward mechanics, and enemy encounters

---

## Technical Systems

Low-level systems that ensure stability and prevent issues.

- **[Encounter Safety](Technical/encounter-safety.md)** - Map encounter crash prevention and safety measures

---

## Spec Structure

Each feature specification follows this structure:

- **Overview**: One sentence summary of what the feature does
- **Purpose**: Why the feature exists and what problem it solves
- **Inputs/Outputs**: What data flows in and out of the system
- **Behavior**: Detailed description of how the feature works
- **Technical Implementation**: Key files, APIs, and code patterns
- **Edge Cases**: What can go wrong and how it's handled
- **Acceptance Criteria**: Checklist for implementation verification

---

## Quick Reference

**By Tier Requirement:**
- Tier 1+: Enlistment, Duties, Formation Training, Troop Selection, Companion Management, Command Tent (basic)
- Tier 2+: Quartermaster, Menu Interface
- Tier 4+: Personal Retinue (Command Tent), Recon Mission

**By Category:**
- **Progression**: Enlistment, Duties, Formation Training, Troop Selection
- **Party Management**: Companion Management, Personal Retinue (Command Tent)
- **Interaction**: Dialog System, Menu Interface, Town Access, Command Tent
- **Equipment**: Quartermaster
- **Special**: Temporary Leave, Recon Mission
- **Safety**: Encounter Safety
