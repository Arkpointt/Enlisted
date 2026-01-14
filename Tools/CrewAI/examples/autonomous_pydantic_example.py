"""
Example: Autonomous Pydantic Output Fixing

Demonstrates how the system automatically detects and fixes Pydantic validation
failures without manual intervention.
"""

from pydantic import BaseModel, Field
from typing import List
from enum import Enum

from crewai import Agent, Crew, Task, LLM

# Import the autonomous fixer
from enlisted_crew.hooks import with_pydantic_retry, create_pydantic_safe_task


# Define your Pydantic models
class ImpactLevel(str, Enum):
    HIGH = "high"
    MEDIUM = "medium"
    LOW = "low"


class Recommendation(BaseModel):
    """A single recommendation."""
    title: str = Field(description="Short recommendation title")
    description: str = Field(description="What to implement")
    benefit: str = Field(description="Why it matters")
    effort_hours: int = Field(description="Estimated hours", ge=1, le=100)
    impact: ImpactLevel = Field(description="Impact level")
    files: List[str] = Field(description="Affected files")


class RecommendationsOutput(BaseModel):
    """Structured recommendations output."""
    quick_wins: List[Recommendation] = Field(description="High impact, low effort")
    major_items: List[Recommendation] = Field(description="High impact, medium effort")


def example_reactive_approach():
    """
    REACTIVE: Try normally, auto-fix if it fails.
    
    Best for: Existing code where you want automatic fallback.
    """
    print("\n" + "="*60)
    print("EXAMPLE 1: Reactive Approach (Auto-Retry)")
    print("="*60)
    
    # Create agent with tools (makes Pydantic output harder)
    agent = Agent(
        role="Architect",
        goal="Generate recommendations",
        backstory="You create practical recommendations.",
        llm=LLM(model="gpt-4o-mini"),
        tools=[],  # In real use: multiple tools here
        max_iter=3,
    )
    
    # Standard task creation
    task = Task(
        description="""
Generate 2 recommendations for improving code quality.

Focus on:
- Quick wins (under 4 hours)
- High impact changes

Each recommendation needs:
- Clear title
- Description of what to do
- Why it benefits the project
- Effort estimate in hours
- Impact level (high/medium/low)
- List of files to modify
        """,
        expected_output="Structured recommendations",
        output_pydantic=RecommendationsOutput,  # Want structured output
        agent=agent,
    )
    
    crew = Crew(agents=[agent], tasks=[task], verbose=False)
    
    # AUTONOMOUS FIX: Wrap with retry logic
    print("\n[INFO] Running with autonomous retry...")
    result = with_pydantic_retry(crew, task, max_retries=1)
    
    # Check result
    if hasattr(result, 'pydantic') and result.pydantic:
        print("\n[SUCCESS] Got structured Pydantic output!")
        print(f"  Quick wins: {len(result.pydantic.quick_wins)}")
        print(f"  Major items: {len(result.pydantic.major_items)}")
        
        if result.pydantic.quick_wins:
            print(f"\n  Example: {result.pydantic.quick_wins[0].title}")
            print(f"    Effort: {result.pydantic.quick_wins[0].effort_hours} hours")
            print(f"    Impact: {result.pydantic.quick_wins[0].impact}")
    else:
        print("\n[FALLBACK] Pydantic failed, got text output")
        print(f"  Output length: {len(str(result))} chars")


def example_proactive_approach():
    """
    PROACTIVE: Pre-enhance the prompt before running.
    
    Best for: New code where structured output is critical.
    """
    print("\n" + "="*60)
    print("EXAMPLE 2: Proactive Approach (Pre-Enhancement)")
    print("="*60)
    
    agent = Agent(
        role="Architect",
        goal="Generate recommendations",
        backstory="You create practical recommendations.",
        llm=LLM(model="gpt-4o-mini"),
        max_iter=3,
    )
    
    # AUTONOMOUS FIX: Use helper that pre-enhances
    print("\n[INFO] Creating pre-enhanced task...")
    task = create_pydantic_safe_task(
        description="""
Generate 2 recommendations for improving code quality.

Focus on quick wins and high impact changes.
        """,
        expected_output="Structured recommendations",
        output_pydantic=RecommendationsOutput,
        agent=agent,
    )
    
    print("[INFO] Task description enhanced with JSON example")
    print(f"  Original length: ~200 chars")
    print(f"  Enhanced length: {len(task.description)} chars")
    print(f"  Added: {len(task.description) - 200} chars of JSON example\n")
    
    crew = Crew(agents=[agent], tasks=[task], verbose=False)
    
    # Run normally - already has enhanced prompt
    print("[INFO] Running with pre-enhanced prompt...")
    result = crew.kickoff()
    
    if hasattr(result, 'pydantic') and result.pydantic:
        print("\n[SUCCESS] Got structured output on first try!")
    else:
        print("\n[INFO] Fell back to text (but that's okay)")


def example_comparison():
    """
    COMPARISON: Show what the fixer adds to prompts.
    """
    print("\n" + "="*60)
    print("EXAMPLE 3: What Gets Added (Comparison)")
    print("="*60)
    
    from enlisted_crew.hooks.pydantic_output_fixer import (
        _enhance_task_description_with_schema,
        _generate_schema_example,
    )
    
    original = """
Generate recommendations for code improvements.

**WORKFLOW:**
1. Analyze the codebase
2. Identify issues
3. Create recommendations
"""
    
    print("\nORIGINAL PROMPT:")
    print("-" * 60)
    print(original)
    
    enhanced = _enhance_task_description_with_schema(
        original,
        RecommendationsOutput
    )
    
    print("\nADDED SECTION:")
    print("-" * 60)
    # Show what was added (everything between original and workflow)
    added_text = enhanced.split("**WORKFLOW:")[0][len(original):]
    print(added_text[:800])  # First 800 chars of addition
    print("...")
    
    print("\n[INFO] The fixer:")
    print("  ✓ Generated JSON example from Pydantic model")
    print("  ✓ Added explicit formatting rules")
    print("  ✓ Preserved original workflow section")
    print("  ✓ No manual schema documentation needed!")


def main():
    """Run all examples."""
    print("\n" + "="*60)
    print("AUTONOMOUS PYDANTIC OUTPUT FIXING - EXAMPLES")
    print("="*60)
    print("\nThese examples show how the system automatically fixes")
    print("Pydantic output failures without manual intervention.\n")
    
    # Example 3: Show what gets added (no LLM call)
    example_comparison()
    
    # Uncomment to run LLM examples (costs API credits):
    # example_reactive_approach()
    # example_proactive_approach()
    
    print("\n" + "="*60)
    print("SUMMARY")
    print("="*60)
    print("\nThe autonomous fixer:")
    print("  1. Detects Pydantic validation failures")
    print("  2. Generates JSON examples from Pydantic schemas")
    print("  3. Enhances prompts with explicit formatting")
    print("  4. Retries automatically")
    print("  5. All without human intervention")
    print("\nNo more manual schema documentation!")
    print("\nTo run LLM examples, uncomment lines 143-144")
    print("="*60)


if __name__ == "__main__":
    main()
