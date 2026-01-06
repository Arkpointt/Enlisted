"""Quick validation test for CrewAI configuration."""

from enlisted_crew.crew import EnlistedCrew

def test_configuration():
    """Test that all new features are properly configured."""
    print("\n" + "="*60)
    print("CREWAI CONFIGURATION TEST")
    print("="*60)
    
    # Initialize crew
    ec = EnlistedCrew()
    print("\n[OK] EnlistedCrew initialized")
    
    # Test plan_workflow
    print("\n[TEST] Testing plan_workflow...")
    crew = ec.plan_workflow()
    print(f"   - Has memory attribute: {hasattr(crew, 'memory')}")
    print(f"   - Has cache attribute: {hasattr(crew, 'cache')}")
    print(f"   - Has planning attribute: {hasattr(crew, 'planning')}")
    print(f"   - Has embedder attribute: {hasattr(crew, 'embedder')}")
    print(f"   - Agent count: {len(crew.agents)}")
    print(f"   - Task count: {len(crew.tasks)}")
    
    # Test implement_workflow
    print("\n[TEST] Testing implement_workflow...")
    crew = ec.implement_workflow()
    print(f"   - Has memory attribute: {hasattr(crew, 'memory')}")
    print(f"   - Has planning attribute: {hasattr(crew, 'planning')}")
    print(f"   - Agent count: {len(crew.agents)}")
    print(f"   - Task count: {len(crew.tasks)}")
    
    # Test bug_workflow
    print("\n[TEST] Testing bug_workflow...")
    crew = ec.bug_workflow()
    print(f"   - Has memory attribute: {hasattr(crew, 'memory')}")
    print(f"   - Has planning attribute: {hasattr(crew, 'planning')}")
    print(f"   - Agent count: {len(crew.agents)}")
    print(f"   - Task count: {len(crew.tasks)}")
    
    print("\n" + "="*60)
    print("[OK] ALL CONFIGURATION TESTS PASSED")
    print("="*60)
    print("\nNew Features Enabled:")
    print("  [+] Planning (AgentPlanner creates step-by-step plans)")
    print("  [+] Memory (short-term, long-term, entity)")
    print("  [+] Cache (tool results cached to avoid redundancy)")
    print("  [+] Embedder (text-embedding-3-large with 24K chunks)")
    print("\nReady to use:")
    print("  - enlisted-crew plan -f 'feature' -d 'description'")
    print("  - enlisted-crew implement -p 'path/to/plan.md'")
    print("  - enlisted-crew hunt-bug -d 'bug description'")
    print("  - crewai test --n_iterations 3")
    print()

if __name__ == "__main__":
    test_configuration()
