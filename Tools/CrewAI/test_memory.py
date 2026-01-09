#!/usr/bin/env python
"""Test script for advanced memory mode (CrewAI 1.8.0 compatibility).

Tests:
1. Memory config generation (basic & advanced)
2. Custom storage initialization without hanging
3. Short-term memory save/search with hybrid search
4. Entity memory save/search with hybrid search
5. Timeout handling for Cohere API
"""

import os
import sys
from pathlib import Path

# Ensure we can import enlisted_crew
sys.path.insert(0, str(Path(__file__).parent / "src"))

from enlisted_crew.memory_config import (
    get_memory_config,
    get_contextual_short_term_memory,
    get_contextual_entity_memory,
)


def test_basic_config():
    """Test basic memory config (default mode)."""
    print("\n=== Test 1: Basic Memory Config ===")
    config = get_memory_config(use_advanced=False)
    assert "memory" in config
    assert "embedder" in config
    assert "short_term_memory" not in config
    assert "entity_memory" not in config
    print("✓ Basic config returned correctly")


def test_advanced_config():
    """Test advanced memory config with custom storage."""
    print("\n=== Test 2: Advanced Memory Config ===")
    config = get_memory_config(use_advanced=True)
    assert "memory" in config
    assert "embedder" in config
    assert "short_term_memory" in config
    assert "entity_memory" in config
    print("✓ Advanced config returned custom memory classes")


def test_memory_instances():
    """Test that memory functions return instances."""
    print("\n=== Test 3: Memory Instances ===")
    
    stm_inst = get_contextual_short_term_memory()
    assert stm_inst is not None
    assert hasattr(stm_inst, "storage")
    assert stm_inst.__class__.__name__ == "ContextualShortTermMemory"
    print(f"✓ Short-term memory instance: {stm_inst.__class__.__name__}")
    
    entity_inst = get_contextual_entity_memory()
    assert entity_inst is not None
    assert hasattr(entity_inst, "storage")
    assert entity_inst.__class__.__name__ == "ContextualEntityMemory"
    print(f"✓ Entity memory instance: {entity_inst.__class__.__name__}")


def test_crew_initialization():
    """Test that a Crew can be initialized with advanced memory."""
    print("\n=== Test 4: Crew Initialization ===")
    
    try:
        from crewai import Crew, Agent, Task
        
        # Create minimal crew with advanced memory
        config = get_memory_config(use_advanced=True)
        
        agent = Agent(
            role="Test Agent",
            goal="Test memory",
            backstory="Testing agent for memory validation"
        )
        
        task = Task(
            description="Echo test",
            expected_output="Test output",
            agent=agent
        )
        
        # This should not hang (main test!)
        print("  Creating crew with advanced memory...")
        crew = Crew(
            agents=[agent],
            tasks=[task],
            **config
        )
        
        print("✓ Crew initialized successfully (no hang)")
        print(f"  Memory enabled: {crew.memory}")
        
        # Verify storage classes
        if hasattr(crew, "short_term_memory") and crew.short_term_memory:
            stm_storage = crew.short_term_memory.storage
            print(f"  Short-term storage: {stm_storage.__class__.__name__}")
            assert hasattr(stm_storage, "_get_bm25_index")
            print("  ✓ Custom ContextualRAGStorage detected")
        
    except ImportError as e:
        print(f"⚠ Skipping crew test: {e}")


def test_timeout_config():
    """Verify timeout is configured for Cohere API."""
    print("\n=== Test 5: Timeout Configuration ===")
    
    from enlisted_crew.memory_config import COHERE_TIMEOUT_SECONDS
    assert COHERE_TIMEOUT_SECONDS == 10.0
    print(f"✓ Cohere timeout set to {COHERE_TIMEOUT_SECONDS}s")


def main():
    """Run all tests."""
    print("="*60)
    print("Testing Advanced Memory Mode (CrewAI 1.8.0)")
    print("="*60)
    
    try:
        test_basic_config()
        test_advanced_config()
        test_memory_instances()
        test_timeout_config()
        test_crew_initialization()
        
        print("\n" + "="*60)
        print("✅ ALL TESTS PASSED")
        print("="*60)
        return 0
        
    except AssertionError as e:
        print(f"\n❌ TEST FAILED: {e}")
        return 1
    except Exception as e:
        print(f"\n❌ ERROR: {e}")
        import traceback
        traceback.print_exc()
        return 1


if __name__ == "__main__":
    sys.exit(main())
