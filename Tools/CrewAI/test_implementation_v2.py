#!/usr/bin/env python3
"""Test script for ImplementationFlowV2 single-agent refactor."""

from src.enlisted_crew.flows.implementation_flow_v2 import ImplementationFlowV2

def main():
    """Test the refactored single-agent implementation flow."""
    print("="*70)
    print("TESTING IMPLEMENTATION FLOW V2")
    print("="*70)
    
    flow = ImplementationFlowV2()
    
    # Test with a small plan
    test_plan_path = "docs/CrewAI_Plans/test-sequential.md"
    
    print(f"\nTesting with plan: {test_plan_path}")
    print("-"*70)
    
    try:
        result = flow.kickoff(inputs={"plan_path": test_plan_path})
        
        print("\n" + "="*70)
        print("TEST RESULTS")
        print("="*70)
        print(f"Success: {flow.state.success}")
        print(f"C# Status: {flow.state.csharp_status.value}")
        print(f"Content Status: {flow.state.content_status.value}")
        print(f"Skipped Steps: {', '.join(flow.state.skipped_steps) or 'None'}")
        print("\n" + "="*70)
        
        if flow.state.success:
            print("✓ Flow completed successfully!")
        else:
            print("✗ Flow failed")
            if flow.state.final_report:
                print(f"Report: {flow.state.final_report[:500]}")
        
        return 0 if flow.state.success else 1
        
    except Exception as e:
        print(f"\n✗ ERROR: {e}")
        import traceback
        traceback.print_exc()
        return 1


if __name__ == "__main__":
    exit(main())
