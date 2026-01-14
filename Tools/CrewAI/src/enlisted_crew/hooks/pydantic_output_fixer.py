"""
Automatic Pydantic Output Fixer

Detects when output_pydantic fails and automatically retries with enhanced prompts.

Usage:
    from enlisted_crew.hooks.pydantic_output_fixer import with_pydantic_retry
    
    task = Task(
        description="Generate recommendations...",
        output_pydantic=RecommendationsOutput,
        agent=agent
    )
    
    crew = Crew(agents=[agent], tasks=[task])
    result = with_pydantic_retry(crew, task)
"""

from typing import Any, Optional, Type
from pydantic import BaseModel
import json


def _generate_schema_example(pydantic_model: Type[BaseModel]) -> str:
    """Generate a JSON example from a Pydantic model schema.
    
    Args:
        pydantic_model: The Pydantic model class
        
    Returns:
        Pretty-printed JSON example string
    """
    schema = pydantic_model.model_json_schema()
    
    def create_example(schema_obj: dict, depth: int = 0) -> Any:
        """Recursively create example values from schema."""
        schema_type = schema_obj.get("type")
        
        if schema_type == "object":
            properties = schema_obj.get("properties", {})
            return {
                key: create_example(prop, depth + 1)
                for key, prop in properties.items()
            }
        elif schema_type == "array":
            items_schema = schema_obj.get("items", {})
            # Generate 1 example item for arrays
            return [create_example(items_schema, depth + 1)]
        elif schema_type == "string":
            enum = schema_obj.get("enum")
            if enum:
                return enum[0]
            return "example_string"
        elif schema_type == "integer":
            return 1
        elif schema_type == "number":
            return 1.0
        elif schema_type == "boolean":
            return True
        else:
            return None
    
    example = create_example(schema)
    return json.dumps(example, indent=2)


def _enhance_task_description_with_schema(
    original_description: str,
    pydantic_model: Type[BaseModel]
) -> str:
    """Add explicit JSON schema example to task description.
    
    Args:
        original_description: Original task description
        pydantic_model: The Pydantic model to generate example from
        
    Returns:
        Enhanced description with JSON example
    """
    schema_example = _generate_schema_example(pydantic_model)
    
    enhancement = f"""

**CRITICAL - OUTPUT FORMAT:**
You MUST output ONLY valid JSON matching this EXACT structure:

```json
{schema_example}
```

**RULES:**
- Output ONLY the JSON object above
- No markdown code blocks (```json)
- No explanatory text before or after
- All field names must match exactly (case-sensitive)
- All required fields must be present
- Follow the data types shown (strings, numbers, arrays)

If you use tools, complete your tool usage FIRST, then output ONLY the JSON.
"""
    
    # Check if description already has JSON example (avoid duplicating)
    if "```json" in original_description and "OUTPUT MUST BE" in original_description:
        return original_description
    
    # Insert before any existing workflow/rules sections
    if "**WORKFLOW" in original_description:
        parts = original_description.split("**WORKFLOW", 1)
        return parts[0] + enhancement + "\n**WORKFLOW" + parts[1]
    else:
        return original_description + enhancement


def detect_pydantic_failure(result: Any) -> bool:
    """Check if Pydantic output parsing failed.
    
    Args:
        result: CrewAI task result
        
    Returns:
        True if Pydantic parsing likely failed
    """
    # Check if result has pydantic attribute but it's None
    if hasattr(result, 'pydantic'):
        return result.pydantic is None
    
    # Check if result is a string (text fallback)
    if isinstance(result, str):
        return True
    
    return False


def with_pydantic_retry(
    crew: Any,
    task: Any,
    max_retries: int = 1
) -> Any:
    """Execute crew with automatic Pydantic output retry on failure.
    
    This wrapper:
    1. Runs the crew normally
    2. Checks if Pydantic output succeeded
    3. If failed, enhances the task prompt with explicit JSON schema
    4. Retries up to max_retries times
    
    Args:
        crew: CrewAI Crew instance
        task: Task with output_pydantic set
        max_retries: Maximum retry attempts (default: 1)
        
    Returns:
        Task result (hopefully with valid Pydantic output)
    """
    # Ensure task has output_pydantic set
    if not hasattr(task, 'output_pydantic') or task.output_pydantic is None:
        print("[PYDANTIC_FIXER] Warning: Task has no output_pydantic set, skipping retry logic")
        return crew.kickoff()
    
    # First attempt
    result = crew.kickoff()
    
    # Check if it succeeded
    if not detect_pydantic_failure(result):
        print("[PYDANTIC_FIXER] ✓ Pydantic output succeeded on first try")
        return result
    
    print("[PYDANTIC_FIXER] ✗ Pydantic output failed, will retry with enhanced prompt")
    
    # Retry with enhanced prompt
    for attempt in range(max_retries):
        print(f"[PYDANTIC_FIXER] Retry {attempt + 1}/{max_retries}: Enhancing task prompt...")
        
        # Store original description
        original_desc = task.description
        
        # Enhance with explicit schema
        task.description = _enhance_task_description_with_schema(
            original_desc,
            task.output_pydantic
        )
        
        # Retry
        result = crew.kickoff()
        
        # Check if it worked
        if not detect_pydantic_failure(result):
            print(f"[PYDANTIC_FIXER] ✓ Success on retry {attempt + 1}")
            return result
        
        print(f"[PYDANTIC_FIXER] ✗ Retry {attempt + 1} failed")
    
    print(f"[PYDANTIC_FIXER] ✗ All retries exhausted, returning last result")
    return result


def create_pydantic_safe_task(
    description: str,
    output_pydantic: Type[BaseModel],
    agent: Any,
    **kwargs
) -> Any:
    """Create a Task with auto-enhanced Pydantic output prompt.
    
    This is a drop-in replacement for Task() that automatically adds
    explicit JSON schema examples to the description.
    
    Args:
        description: Task description
        output_pydantic: Pydantic model for output
        agent: Agent to assign task to
        **kwargs: Other Task parameters
        
    Returns:
        Task with enhanced description
    """
    from crewai import Task
    
    enhanced_description = _enhance_task_description_with_schema(
        description,
        output_pydantic
    )
    
    return Task(
        description=enhanced_description,
        output_pydantic=output_pydantic,
        agent=agent,
        **kwargs
    )
