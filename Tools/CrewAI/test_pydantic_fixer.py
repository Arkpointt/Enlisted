"""
Test the pydantic output fixer helper.
"""

from pydantic import BaseModel, Field
from typing import List
from enlisted_crew.hooks.pydantic_output_fixer import (
    _generate_schema_example,
    _enhance_task_description_with_schema,
)


class TestRecommendation(BaseModel):
    """Test recommendation model."""
    title: str = Field(description="Short title")
    description: str = Field(description="What to do")
    priority: int = Field(description="Priority level 1-3")
    files: List[str] = Field(description="Affected files")


class TestOutput(BaseModel):
    """Test output with nested model."""
    recommendations: List[TestRecommendation] = Field(description="List of recommendations")
    summary: str = Field(description="Summary text")


def test_schema_generation():
    """Test that we can generate schema examples."""
    print("Testing schema generation...")
    
    # Simple model
    example = _generate_schema_example(TestRecommendation)
    print("\nSimple model example:")
    print(example)
    assert "title" in example
    assert "description" in example
    assert "priority" in example
    assert "files" in example
    
    # Nested model
    example2 = _generate_schema_example(TestOutput)
    print("\nNested model example:")
    print(example2)
    assert "recommendations" in example2
    assert "summary" in example2
    
    print("\n✓ Schema generation works!")


def test_description_enhancement():
    """Test that we can enhance task descriptions."""
    print("\nTesting description enhancement...")
    
    original = """
Generate recommendations for the following systems.

**WORKFLOW (execute in order):**
1. Analyze the systems
2. Create recommendations
3. Output structured data

Maximum 5 recommendations.
"""
    
    enhanced = _enhance_task_description_with_schema(original, TestRecommendation)
    
    print("\nOriginal description length:", len(original))
    print("Enhanced description length:", len(enhanced))
    print("\nEnhancement added:")
    print(enhanced[len(original):len(original) + 500])
    
    assert "CRITICAL - OUTPUT FORMAT" in enhanced
    assert "```json" in enhanced
    assert "title" in enhanced
    assert "**WORKFLOW" in enhanced  # Should preserve workflow section
    
    print("\n✓ Description enhancement works!")


def test_duplicate_prevention():
    """Test that enhancement doesn't duplicate."""
    print("\nTesting duplicate prevention...")
    
    # Already enhanced description
    already_enhanced = """
Some task.

**OUTPUT MUST BE VALID JSON:**
```json
{"example": "data"}
```

**WORKFLOW:**
1. Do something
"""
    
    result = _enhance_task_description_with_schema(already_enhanced, TestRecommendation)
    
    # Should not add another enhancement
    assert result == already_enhanced
    print("✓ Duplicate prevention works!")


if __name__ == "__main__":
    test_schema_generation()
    test_description_enhancement()
    test_duplicate_prevention()
    
    print("\n" + "=" * 60)
    print("ALL TESTS PASSED ✓")
    print("=" * 60)
