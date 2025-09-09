# Feature Specifications

This folder contains functional specifications for major features in the military service system.

## Features

- **[quartermaster.md](quartermaster.md)** - Grid UI for equipment selection
- **[dialog-system.md](dialog-system.md)** - Centralized conversation management  
- **[troop-selection.md](troop-selection.md)** - Real troop choice system for promotions
- **[temporary-leave.md](temporary-leave.md)** - Leave system with companion management
- **[encounter-safety.md](encounter-safety.md)** - Preventing map encounter crashes with enhanced leave fixes
- **[duties-system.md](duties-system.md)** - Military roles and assignments
- **[formation-training.md](formation-training.md)** - **NEW** - Formation-based daily skill XP system
- **[enlistment.md](enlistment.md)** - Core military service functionality
- **[battle-commands.md](battle-commands.md)** - **NEW** - Automatic formation-based battle command filtering
- **[menu-interface.md](menu-interface.md)** - **NEW** - Professional menu system with organized sections and descriptions
- **[town-access-system.md](town-access-system.md)** - **NEW** - Synthetic outside encounter approach for settlement exploration

## What Feature Specs Are For

Feature specs explain **what each feature does** and **how it works**, not just why we built it. They help with:

- **Understanding**: What does this feature actually do?
- **Recreation**: How would I build this again?  
- **Debugging**: How does it work and what can go wrong?
- **Extension**: How could I modify or extend this feature?

Each spec covers:
- **Overview**: What the feature does in one sentence
- **Purpose**: Why players need this feature
- **Inputs/Outputs**: What goes in, what comes out
- **Behavior**: How it works step by step
- **Technical Implementation**: Key files and APIs
- **Edge Cases**: What can go wrong and how it's handled
- **Acceptance Criteria**: How you know it's working correctly
- **Debugging**: Common issues and how to fix them

## When to Create a Feature Spec

Write a spec when you build a feature that:
- Has complex behavior or multiple steps
- Interacts with game systems in non-obvious ways  
- Has edge cases that need specific handling
- Would be hard to recreate from code alone
- Other developers (or AIs) might need to modify

Don't write specs for:
- Simple utility functions
- Straightforward data models
- Features with obvious implementations
