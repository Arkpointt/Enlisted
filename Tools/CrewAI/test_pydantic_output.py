#!/usr/bin/env python3
"""
Test if CrewAI output_pydantic actually works with our models.
"""
import os
import sys

# Add src to path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), 'src'))

from crewai import Agent, Task, Crew, Process, LLM
from enlisted_crew.flows.state_models import RecommendationsOutput, Recommendation, ImpactLevel

# Simple test agent + task
print("🧪 Testing CrewAI output_pydantic with RecommendationsOutput...\n")

test_llm = LLM(model="gpt-4o-mini", max_completion_tokens=1000)

agent = Agent(
    role="Test Architect",
    goal="Generate test recommendations",
    backstory="You generate structured recommendations for testing",
    llm=test_llm,
    tools=[],
    allow_delegation=False,
    verbose=False,
)

task = Task(
    description="""
Generate 2 test recommendations:

1. A quick win (high impact, low effort)
2. A major improvement (high impact, medium effort)

For EACH recommendation include:
- title: Short name
- description: What to do
- benefit: Why it matters
- effort_hours: Integer 1-100
- impact: "high", "medium", or "low"
- files: List of file paths (can be empty)

Output in this structure:
{
  "priority_1_quick_wins": [
    {
      "title": "Example",
      "description": "Do something",
      "benefit": "It helps",
      "effort_hours": 2,
      "impact": "high",
      "files": ["src/test.cs"]
    }
  ],
  "priority_2_major": [
    {
      "title": "Example 2",
      "description": "Do something bigger",
      "benefit": "Big impact",
      "effort_hours": 8,
      "impact": "high",
      "files": []
    }
  ],
  "priority_3_minor": []
}
""",
    expected_output="Structured recommendations with priority tiers",
    agent=agent,
    output_pydantic=RecommendationsOutput,
)

crew = Crew(
    agents=[agent],
    tasks=[task],
    process=Process.sequential,
    verbose=True,
)

print("Running crew.kickoff()...\n")
result = crew.kickoff()

print("\n" + "="*60)
print("RESULT ANALYSIS")
print("="*60)

# Check what we got
print(f"\nResult type: {type(result)}")
print(f"Has .pydantic: {hasattr(result, 'pydantic')}")
print(f"Has .json_dict: {hasattr(result, 'json_dict')}")
print(f"Has .raw: {hasattr(result, 'raw')}")

if hasattr(result, 'pydantic') and result.pydantic:
    print("\n✅ SUCCESS: Got Pydantic output")
    print(f"Type: {type(result.pydantic)}")
    print(f"Priority 1: {len(result.pydantic.priority_1_quick_wins)} items")
    print(f"Priority 2: {len(result.pydantic.priority_2_major)} items")
    print(f"Priority 3: {len(result.pydantic.priority_3_minor)} items")
    
    if result.pydantic.priority_1_quick_wins:
        first = result.pydantic.priority_1_quick_wins[0]
        print(f"\nFirst recommendation:")
        print(f"  Title: {first.title}")
        print(f"  Description: {first.description}")
        print(f"  Benefit: {first.benefit}")
        print(f"  Effort: {first.effort_hours} hours")
        print(f"  Impact: {first.impact}")
        print(f"  Files: {first.files}")
else:
    print("\n❌ FAILED: No Pydantic output")
    if hasattr(result, 'raw'):
        print(f"\nRaw output (first 500 chars):")
        print(str(result.raw)[:500])
