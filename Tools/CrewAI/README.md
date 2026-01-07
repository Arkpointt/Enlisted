# Enlisted Crew

CrewAI multi-agent system for Enlisted mod development with intelligent escalation and human-in-the-loop workflows.

## Features

- Planning Flow with automatic hallucination detection
- Implementation Flow with code generation
- Bug Hunting Flow for issue investigation
- Validation Flow for quality assurance
- Manager escalation system for critical issues

## Installation

```bash
pip install -e .
```

## Usage

```bash
enlisted-crew plan -f "feature-name" -d "Feature description"
enlisted-crew implement -f "feature-name"
enlisted-crew hunt -i "issue-description"
enlisted-crew validate -f "feature-name"
```
