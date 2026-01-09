#!/usr/bin/env python
"""
Quick test to verify cache hit tracking is working.

This simulates the OpenAI response structure to validate that our hooks
correctly extract and log cache hit information.
"""

import sys
from pathlib import Path

# Add src to path
sys.path.insert(0, str(Path(__file__).parent / "src"))

from enlisted_crew.hooks import track_llm_costs, reset_run_costs, print_run_cost_summary


# Mock OpenAI response structure
class MockUsage:
    def __init__(self, prompt_tokens, completion_tokens, cached_tokens=0):
        self.prompt_tokens = prompt_tokens
        self.completion_tokens = completion_tokens
        self.prompt_tokens_details = MockPromptTokensDetails(cached_tokens)


class MockPromptTokensDetails:
    def __init__(self, cached_tokens):
        self.cached_tokens = cached_tokens


class MockContext:
    def __init__(self, model, prompt_tokens, completion_tokens, cached_tokens=0):
        self.model = model
        self.usage = MockUsage(prompt_tokens, completion_tokens, cached_tokens)


def test_cache_tracking():
    """Test that cache hit tracking works correctly."""
    print("=" * 70)
    print("CACHE TRACKING TEST")
    print("=" * 70 + "\n")
    
    # Reset tracking
    reset_run_costs()
    
    print("Simulating 5 LLM calls with varying cache hit rates:\n")
    
    # Call 1: No cache (cold start)
    print("Call 1: Cold start (no cache)")
    context1 = MockContext("gpt-5.2", prompt_tokens=2000, completion_tokens=500, cached_tokens=0)
    track_llm_costs(context1)
    print()
    
    # Call 2: 50% cache hit
    print("Call 2: 50% cache hit")
    context2 = MockContext("gpt-5.2", prompt_tokens=2000, completion_tokens=500, cached_tokens=1000)
    track_llm_costs(context2)
    print()
    
    # Call 3: 80% cache hit
    print("Call 3: 80% cache hit")
    context3 = MockContext("gpt-5.2", prompt_tokens=2500, completion_tokens=600, cached_tokens=2000)
    track_llm_costs(context3)
    print()
    
    # Call 4: 90% cache hit
    print("Call 4: 90% cache hit")
    context4 = MockContext("gpt-5.2", prompt_tokens=3000, completion_tokens=700, cached_tokens=2700)
    track_llm_costs(context4)
    print()
    
    # Call 5: 100% cache hit (fully cached)
    print("Call 5: 100% cache hit")
    context5 = MockContext("gpt-5.2", prompt_tokens=1500, completion_tokens=400, cached_tokens=1500)
    track_llm_costs(context5)
    print()
    
    # Print summary
    print_run_cost_summary()
    
    print("✅ Cache tracking test complete!")
    print("   - Check console output above for cache percentages")
    print("   - Check database: Tools/CrewAI/database/enlisted_knowledge.db")
    print("   - Run 'enlisted-crew stats --costs' to see aggregated cache metrics\n")


if __name__ == "__main__":
    test_cache_tracking()
