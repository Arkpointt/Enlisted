# Automatic Pydantic Output Fixer

## Problem

When using CrewAI's `output_pydantic` parameter with tool-enabled agents, the LLM sometimes produces text output instead of valid JSON, causing Pydantic validation to fail. This happens because:

1. **Tool usage distracts the agent** - Agents with tools focus on using them, then forget to output structured JSON
2. **Complex context** - Large prompts with many instructions can dilute the JSON format requirement
3. **Implicit schema** - CrewAI embeds the Pydantic schema but doesn't show a concrete example

## Solution

The `pydantic_output_fixer` module provides **automatic detection and retry** with enhanced prompts.

## Usage

### Option 1: Automatic Retry Wrapper (Recommended)

Wrap your crew execution to automatically retry on failure:

```python
from enlisted_crew.hooks import with_pydantic_retry
from crewai import Agent, Crew, Task

# Create your task with output_pydantic
task = Task(
    description="Generate recommendations...",
    output_pydantic=RecommendationsOutput,
    agent=architect,
    expected_output="Structured recommendations"
)

crew = Crew(agents=[architect], tasks=[task])

# Use the wrapper - automatically retries with enhanced prompt if needed
result = with_pydantic_retry(crew, task, max_retries=1)

# Check success
if hasattr(result, 'pydantic') and result.pydantic:
    recommendations = result.pydantic
else:
    # Fell back to text parsing
    recommendations = parse_text_fallback(str(result))
```

**How it works:**
1. Runs crew normally
2. If Pydantic output fails, enhances the task description with an explicit JSON example
3. Retries up to `max_retries` times
4. Returns result (with or without valid Pydantic output)

### Option 2: Proactive Enhancement

Pre-enhance the task description before creating it:

```python
from enlisted_crew.hooks import create_pydantic_safe_task

# This automatically adds JSON schema example to the description
task = create_pydantic_safe_task(
    description="Generate recommendations...",
    output_pydantic=RecommendationsOutput,
    agent=architect,
    expected_output="Structured recommendations"
)

crew = Crew(agents=[architect], tasks=[task])
result = crew.kickoff()  # Higher chance of success on first try
```

## What Gets Added

The fixer automatically generates a JSON example from your Pydantic model and adds:

```markdown
**CRITICAL - OUTPUT FORMAT:**
You MUST output ONLY valid JSON matching this EXACT structure:

```json
{
  "priority_1_quick_wins": [
    {
      "title": "example_string",
      "description": "example_string",
      "effort_hours": 1,
      "impact": "high",
      "files": ["example_string"]
    }
  ],
  "priority_2_major": [],
  "priority_3_minor": []
}
```

**RULES:**
- Output ONLY the JSON object above
- No markdown code blocks
- No explanatory text before or after
- All field names must match exactly (case-sensitive)
- All required fields must be present

If you use tools, complete your tool usage FIRST, then output ONLY the JSON.
```

This gets inserted **before** your existing workflow/rules sections, preserving your original prompt structure.

## Detection Logic

The fixer detects failure by checking:

1. `result.pydantic is None` - CrewAI validation failed
2. `isinstance(result, str)` - Fell back to text output

## Integration with SystemAnalysisFlow

Already integrated in `system_analysis_flow.py` at line 813:

```python
task = Task(
    description=f"...",
    output_pydantic=RecommendationsOutput,  # Explicit schema
    agent=agent
)
```

The manual enhancement (lines 776-792) was added directly to the prompt. For new tasks, use the helper instead.

## Best Practices

### When to Use

- **Tool-enabled agents** - Agents with >2 tools benefit most
- **Complex prompts** - Long descriptions (>1000 chars) with many sections
- **Critical structured output** - When you MUST have valid Pydantic output

### When NOT to Use

- **Simple agents** - No tools + short prompt usually work fine
- **Already enhanced** - Don't stack multiple JSON examples
- **Non-Pydantic tasks** - Only use when `output_pydantic` is set

### Performance Impact

- **First try**: No overhead (just schema generation at init)
- **Retry**: One additional crew.kickoff() call (~30-120 seconds depending on task)
- **Token cost**: +200-500 tokens for the JSON example

## Examples

### Simple Model

```python
from pydantic import BaseModel, Field
from typing import List

class Recommendation(BaseModel):
    title: str
    priority: int
    files: List[str]

# Generates:
# {
#   "title": "example_string",
#   "priority": 1,
#   "files": ["example_string"]
# }
```

### Nested Model

```python
class Output(BaseModel):
    recommendations: List[Recommendation]
    summary: str

# Generates:
# {
#   "recommendations": [
#     {
#       "title": "example_string",
#       "priority": 1,
#       "files": ["example_string"]
#     }
#   ],
#   "summary": "example_string"
# }
```

### With Enums

```python
from enum import Enum

class ImpactLevel(str, Enum):
    HIGH = "high"
    MEDIUM = "medium"
    LOW = "low"

class Recommendation(BaseModel):
    title: str
    impact: ImpactLevel

# Generates:
# {
#   "title": "example_string",
#   "impact": "high"  # Uses first enum value
# }
```

## Testing

Run the test suite:

```bash
cd Tools/CrewAI
uv run python test_pydantic_fixer.py
```

Tests verify:
- ✓ Schema generation from Pydantic models
- ✓ Description enhancement preserves original content
- ✓ Duplicate prevention (doesn't re-enhance)

## Future Improvements

Potential enhancements:

1. **Smart example values** - Use field descriptions to generate realistic examples
2. **Token budget aware** - Compress JSON example if prompt is too long
3. **Multi-model support** - Handle Union types and Optional fields better
4. **Telemetry** - Track success/failure rates to optimize retry strategy

## Related

- CrewAI Pydantic Output: https://docs.crewai.com/concepts/tasks#task-output
- Pydantic JSON Schema: https://docs.pydantic.dev/latest/concepts/json_schema/
- January 2026 CrewAI Best Practices (Medium)
