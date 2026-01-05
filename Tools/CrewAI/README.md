# Enlisted CrewAI

**Summary:** Multi-agent AI workflows for Enlisted Bannerlord mod development.
**Status:** ✅ Implemented
**Last Updated:** 2026-01-04 (Anthropic Claude setup, tiered thinking, domain context)
**Related Docs:** [AGENT-WORKFLOW.md](../AGENT-WORKFLOW.md), [BLUEPRINT.md](../../docs/BLUEPRINT.md)

---

## Overview

This CrewAI integration provides specialized AI agents for:
- **Systems Analysis** - Complex system design with Opus 4.5 deep thinking
- **Content Creation** - Event writing following the style guide
- **Code Analysis** - C# pattern checking for Enlisted-specific issues
- **Balance Review** - Effects, XP rewards, and progression balance
- **Quality Assurance** - Full validation pipeline and build checks
- **Documentation Sync** - Keep docs and AI context current

## Setup

### 1. Create Virtual Environment

Requires **Python 3.10-3.13**.

```powershell
cd Tools/CrewAI
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -e .
```

Or with uv:
```powershell
uv venv .venv
.\.venv\Scripts\Activate.ps1
uv pip install -e .
```

### 2. Configure API Key

Copy the example environment file and add your Anthropic API key:

```bash
cp .env.example .env
```

Edit `.env`:
```
ANTHROPIC_API_KEY=sk-ant-api03-your-key-here
```

> **Security:** The `.env` file is gitignored. Never commit API keys.

### 3. Set Project Root (Optional)

If running from outside the project directory:
```bash
export ENLISTED_PROJECT_ROOT=C:\Dev\Enlisted\Enlisted
```

## Usage

### CLI Commands

```bash
# Run full validation on all content
enlisted-crew validate

# Validate a specific event file
enlisted-crew validate-file ModuleData/Enlisted/Events/camp_events.json

# Create a new event
enlisted-crew create-event --theme "gambling in camp" --tier 1-3

# Review writing style
enlisted-crew style-review ModuleData/Enlisted/Decisions/general_decisions.json

# Review C# code changes
enlisted-crew code-review Services/PayService.cs Systems/TierSystem.cs

# Full content review
enlisted-crew full-review ModuleData/Enlisted/Events/*.json
```

### Python API

```python
from enlisted_crew import EnlistedCrew

crew = EnlistedCrew()

# Run validation
result = crew.validation_crew().kickoff()

# Create content with parameters
result = crew.content_creation_crew().kickoff(inputs={
    "event_type": "decision",
    "category": "general", 
    "theme": "soldier discovers hidden cache",
    "tier_range": "2-5"
})
```

## Agents

| Agent | Role | Model | Thinking |
|-------|------|-------|----------|
| systems_analyst | Complex system analysis | Opus 4.5 | 10k tokens |
| feature_architect | Multi-file design | Opus 4.5 | 10k tokens |
| code_analyst | C# pattern analysis | Sonnet 4.5 | 5k tokens |
| qa_agent | Final validation gate | Sonnet 4.5 | 3k tokens |
| documentation_maintainer | Doc sync + standards | Sonnet 4.5 | 5k tokens |
| csharp_implementer | Code generation | Sonnet 4.5 | None |
| content_analyst | Schema validation | Haiku 4.5 | None |
| content_author | Event writing | Haiku 4.5 | None |
| balance_analyst | Balance review | Haiku 4.5 | None |

## Custom Tools

### Validation Tools
- `validate_content_tool` - Runs validate_content.py
- `sync_localization_tool` - Runs sync_event_strings.py  
- `run_build_tool` - Runs dotnet build
- `analyze_validation_report_tool` - Generates prioritized report

### Style Tools
- `check_writing_style_tool` - Checks text against writing-style-guide.md
- `check_tooltip_style_tool` - Validates tooltips (<80 chars)
- `suggest_style_improvements_tool` - Rewrite suggestions

### Schema Tools
- `validate_event_schema_tool` - JSON schema validation
- `create_event_json_tool` - Creates properly structured events
- `read_event_file_tool` - Reads event files
- `list_event_files_tool` - Lists all event/decision files

### Code Style Tools
- `check_code_style_tool` - Validates Allman braces, _camelCase fields, XML docs
- `check_bannerlord_patterns_tool` - Detects TextObject concat, Hero/Equipment/Gold/Reputation patterns
- `check_framework_compatibility_tool` - Validates .NET Framework 4.7.2 compatibility
- `check_csharp_file_tool` - Reads and analyzes C# files (combines all checks)

### Documentation Tools
- `read_doc_tool` - Read project documentation (BLUEPRINT.md, specs, etc.)
- `list_docs_tool` - List documentation files in folders
- `search_docs_tool` - Search text across all documentation
- `read_csharp_tool` - Read C# source files
- `list_feature_files_tool` - List files in src/Features/ folders

## Project Integration

The tools automatically detect the Enlisted project root by looking for `Enlisted.csproj`. They wrap existing validation scripts:

- `Tools/validate_content.py` - JSON and schema validation
- `Tools/sync_event_strings.py` - Localization synchronization
- `Docs/writing-style-guide.md` - Writing rules reference
- `Docs/event-system-schemas.md` - JSON schema reference

## Workflows

### Pre-Commit Validation
```bash
enlisted-crew validate
```

### Content Development
1. Create event: `enlisted-crew create-event --theme "..." --tier 1-3`
2. Style review: `enlisted-crew style-review <file>`
3. Validate: `enlisted-crew validate-file <file>`

### Code Review
```bash
enlisted-crew code-review <changed-files>
```

### Release Preparation
```bash
enlisted-crew full-review ModuleData/Enlisted/**/*.json
```
