# Architecture Decision Records (ADRs)

This directory contains all Architecture Decision Records for the Enlisted mod, documenting both technical decisions and feature designs.

## Index

### Technical Architecture
- **[ADR-001: Package-by-Feature Architecture](ADR-001-package-by-feature-architecture.md)** - Restructured codebase organization
- **[ADR-002: Isolate Game API Interactions](ADR-002-isolate-game-api-interactions.md)** - Game integration patterns  
- **[ADR-003: Centralized Configuration](ADR-003-centralized-configuration.md)** - Configuration management approach
- **[ADR-004: Remove Static Singleton Patterns](ADR-004-remove-static-singleton-patterns.md)** - Dependency injection adoption

### Feature Designs
- **[ADR-005: Enlistment Feature](ADR-005-enlistment-feature.md)** - Core military enlistment system
- **[ADR-006: Promotion System](ADR-006-promotion-system.md)** - XP-based rank advancement
- **[ADR-007: Wage System](ADR-007-wage-system.md)** - Daily compensation for enlisted service
- **[ADR-008: Game Integration Patches](ADR-008-game-integration-patches.md)** - Harmony patches for game system integration

## ADR Status

| ADR | Status | Category | Impact |
|-----|--------|----------|---------|
| 001 | ✅ Accepted | Architecture | High |
| 002 | ✅ Accepted | Architecture | High |
| 003 | ✅ Accepted | Architecture | Medium |
| 004 | ✅ Accepted | Architecture | Medium |
| 005 | ✅ Accepted | Feature | High |
| 006 | ✅ Accepted | Feature | Medium |
| 007 | ✅ Accepted | Feature | Low |
| 008 | ✅ Accepted | Integration | High |

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

- **[ARCHITECTURE.md](../ARCHITECTURE.md)** - High-level architectural overview
- **[BLUEPRINT.md](../BLUEPRINT.md)** - Detailed engineering guidelines  
- **[TESTING.md](../TESTING.md)** - Testing strategies and conventions
