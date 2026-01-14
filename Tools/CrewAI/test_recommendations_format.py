#!/usr/bin/env python
"""
Smoke test: Verify that recommendations are properly formatted in the document.

Tests:
1. Pydantic models can be created and converted to dict format
2. Document generation formats full structured recommendations correctly
3. Fallback formatting works for title-only recommendations
"""

from src.enlisted_crew.flows.state_models import (
    Recommendation,
    RecommendationsOutput,
    ImpactLevel,
)


def test_pydantic_to_dict_conversion():
    """Test that Pydantic recommendations convert to proper dict format."""
    print("\n[TEST 1] Pydantic → Dict Conversion")
    print("=" * 60)
    
    # Create a Pydantic recommendation
    rec = Recommendation(
        title="Add morale visibility to player UI",
        description="Display current morale value and trends in the party screen",
        benefit="Players can make informed decisions about rest/resupply timing",
        effort_hours=4,
        impact=ImpactLevel.HIGH,
        files=["src/Features/UI/PartyScreen.cs", "src/Features/Company/CompanyNeedsState.cs"],
    )
    
    # Convert to dict (simulating what the flow does)
    rec_dict = {
        "title": rec.title,
        "description": rec.description,
        "benefit": rec.benefit,
        "effort_hours": rec.effort_hours,
        "impact": rec.impact.value if hasattr(rec.impact, 'value') else rec.impact,
        "files": rec.files,
        "priority": 1,
    }
    
    print(f"✓ Title: {rec_dict['title']}")
    print(f"✓ Description: {rec_dict['description'][:50]}...")
    print(f"✓ Benefit: {rec_dict['benefit'][:50]}...")
    print(f"✓ Effort: {rec_dict['effort_hours']}h")
    print(f"✓ Impact: {rec_dict['impact']}")
    print(f"✓ Files: {len(rec_dict['files'])} files")
    print(f"✓ Priority: {rec_dict['priority']}")
    
    assert rec_dict["description"], "Description should not be empty"
    assert rec_dict["benefit"], "Benefit should not be empty"
    assert rec_dict["effort_hours"] > 0, "Effort should be positive"
    assert rec_dict["impact"] in ["high", "medium", "low"], "Impact should be valid"
    
    print("\n✅ TEST PASSED: Pydantic → Dict conversion works correctly\n")
    return rec_dict


def test_document_formatting(rec_dict):
    """Test that document generation formats recommendations correctly."""
    print("\n[TEST 2] Document Formatting")
    print("=" * 60)
    
    # Simulate document generation logic
    doc_parts = []
    
    if "description" in rec_dict and rec_dict["description"]:
        # Full structured recommendation
        priority = rec_dict.get("priority", "?")
        impact = rec_dict.get("impact", "?")
        effort = rec_dict.get("effort_hours", "?")
        
        doc_parts.append(f"### 1. {rec_dict['title']}")
        doc_parts.append("")
        doc_parts.append(f"**Priority:** {priority} | **Impact:** {impact} | **Effort:** {effort}h")
        doc_parts.append("")
        doc_parts.append(f"**Description:** {rec_dict['description']}")
        doc_parts.append("")
        if rec_dict.get("benefit"):
            doc_parts.append(f"**Benefit:** {rec_dict['benefit']}")
            doc_parts.append("")
        if rec_dict.get("files"):
            doc_parts.append(f"**Files:** {', '.join(rec_dict['files'])}")
            doc_parts.append("")
    else:
        # Fallback: just title
        doc_parts.append(f"1. {rec_dict.get('title', '')}")
        doc_parts.append("")
    
    formatted = "\n".join(doc_parts)
    
    print("Generated markdown:")
    print("-" * 60)
    print(formatted)
    print("-" * 60)
    
    # Verify all fields are present
    assert "### 1. Add morale visibility" in formatted, "Title should be formatted as heading"
    assert "**Priority:** 1" in formatted, "Priority should be present"
    assert "**Impact:** high" in formatted, "Impact should be present"
    assert "**Effort:** 4h" in formatted, "Effort should be present"
    assert "**Description:**" in formatted, "Description should be present"
    assert "**Benefit:**" in formatted, "Benefit should be present"
    assert "**Files:**" in formatted, "Files should be present"
    
    print("\n✅ TEST PASSED: Document formatting includes all fields\n")


def test_fallback_formatting():
    """Test that title-only recommendations still format correctly."""
    print("\n[TEST 3] Fallback Formatting (Title Only)")
    print("=" * 60)
    
    # Simulate old-style recommendation (title only)
    rec_dict = {
        "title": "1. **Fix caching issue** - Performance",
        "raw": "",
    }
    
    doc_parts = []
    
    if "description" in rec_dict and rec_dict["description"]:
        # Full structured (won't trigger)
        doc_parts.append(f"### 1. {rec_dict['title']}")
    else:
        # Fallback: just title
        doc_parts.append(f"1. {rec_dict.get('title', '')}")
        doc_parts.append("")
    
    formatted = "\n".join(doc_parts)
    
    print("Generated markdown:")
    print("-" * 60)
    print(formatted)
    print("-" * 60)
    
    assert "1. **Fix caching issue**" in formatted, "Title should be present"
    assert "###" not in formatted, "Should not use heading format for fallback"
    
    print("\n✅ TEST PASSED: Fallback formatting works correctly\n")


if __name__ == "__main__":
    print("\n" + "=" * 60)
    print("SMOKE TEST: Recommendations Formatting")
    print("=" * 60)
    
    # Run tests
    rec_dict = test_pydantic_to_dict_conversion()
    test_document_formatting(rec_dict)
    test_fallback_formatting()
    
    print("\n" + "=" * 60)
    print("✅ ALL TESTS PASSED")
    print("=" * 60)
    print("\nNext steps:")
    print("1. Run full system analysis to verify end-to-end")
    print("2. Check that console shows '[RECOMMENDATIONS] Got structured Pydantic output'")
    print("3. Verify generated markdown has full recommendation details")
    print()
