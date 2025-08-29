# Architecture Decision Records (ADRs)

This directory contains all Architecture Decision Records for the Enlisted mod, documenting both technical decisions and feature designs.

## Index

### Technical Architecture
- **[ADR-001: Enlistment System Implementation](ADR-001-enlistment-system-implementation.md)** — Current enlistment lifecycle, dialogs, and state
- **[ADR-010: Robust Logging System](ADR-010-robust-logging-system.md)** — Centralized logging approach and file layout
 - **[ADR-011: Map Tracker Redirection and Main Party Nameplate Suppression](ADR-011-map-tracker-and-nameplate-suppression.md)** — Visual integration while enlisted

### Feature Designs
// Removed outdated feature ADRs; recreate as features are reintroduced

## ADR Status

| ADR | Status | Category | Impact |
|-----|--------|----------|---------|
| 001 | ✅ Accepted | Architecture | High |
| 010 | ✅ Accepted | Architecture | High |
| 011 | ✅ Accepted | Architecture | Medium |

## Usage Guidelines

### When to Create an ADR
- **New Features**: Document design decisions for major new functionality
- **Architecture Changes**: Structural modifications to the codebase
- **Technology Adoption**: Integration of new libraries or frameworks
- **Breaking Changes**: Modifications that affect mod compatibility or save games

### ADR Template
```markdown
# ADR-XXX: [Title]

**Status:** [Proposed|Accepted|Deprecated|Superseded]
**Date:** YYYY-MM-DD
**Deciders:** [Team/Individual]

## Context
[Describe the situation and problem]

## Decision
[State the chosen solution]

## Consequences
[Document positive, negative, and neutral outcomes]

## Compliance
[Reference relevant blueprint sections]
```

### Review Process
1. Create ADR in draft form
2. Team review and discussion
3. Update based on feedback
4. Mark as "Accepted" when consensus reached
5. Update index with new entry

## Related Documentation

- **[BLUEPRINT.md](../BLUEPRINT.md)** — Engineering guidelines
- **[DEBUGGING.md](../DEBUGGING.md)** — Debugging and log locations
