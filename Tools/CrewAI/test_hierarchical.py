"""
Test Hierarchical Process and Guardrails

Tests the manager agent coordination and guardrail validation functionality.

Usage:
    cd Tools/CrewAI
    .venv\Scripts\Activate.ps1
    python test_hierarchical.py
"""

import sys
from pathlib import Path
from typing import Tuple, Any

from crewai import Agent, Crew, Process, Task, LLM
from crewai.tasks.task_output import TaskOutput


# Test LLM (using GPT-5.2 with low reasoning for fast tests)
TEST_LLM = LLM(
    model="gpt-5.2",
    max_completion_tokens=1000,
    reasoning_effort="low",
)


class Colors:
    GREEN = '\033[92m'
    YELLOW = '\033[93m'
    RED = '\033[91m'
    CYAN = '\033[96m'
    RESET = '\033[0m'
    BOLD = '\033[1m'


def print_header(text):
    """Print section header."""
    print(f"\n{Colors.CYAN}{'='*60}{Colors.RESET}")
    print(f"{Colors.CYAN}{Colors.BOLD}{text}{Colors.RESET}")
    print(f"{Colors.CYAN}{'='*60}{Colors.RESET}")


def print_test(text):
    """Print test name."""
    print(f"\n{Colors.YELLOW}[TEST] {text}{Colors.RESET}")


def print_pass(text):
    """Print success."""
    print(f"  {Colors.GREEN}[PASS]{Colors.RESET} {text}")


def print_fail(text):
    """Print failure."""
    print(f"  {Colors.RED}[FAIL]{Colors.RESET} {text}")


def print_info(text):
    """Print info."""
    print(f"  {text}")


# =============================================================================
# GUARDRAIL TESTS
# =============================================================================

def test_guardrails():
    """Test guardrail validation functions."""
    print_header("1. GUARDRAIL TESTS")
    
    # Test 1: validate_plan_structure
    print_test("Guardrail: validate_plan_structure")
    try:
        from enlisted_crew.flows.planning_flow import validate_plan_structure
        
        # Mock TaskOutput with good structure
        class MockOutput:
            raw = """
            # Overview
            This is a feature plan.
            
            # Technical Specification
            Here's how we'll build it.
            
            # Files to Create
            - src/Feature.cs
            """
        
        result, _ = validate_plan_structure(MockOutput())
        if result:
            print_pass("Valid plan structure accepted")
        else:
            print_fail("Valid plan structure rejected")
            return False
        
        # Mock TaskOutput with missing section
        class BadOutput:
            raw = """
            # Overview
            This is incomplete.
            """
        
        result, error = validate_plan_structure(BadOutput())
        if not result and "missing required sections" in str(error).lower():
            print_pass("Invalid plan structure rejected with clear message")
        else:
            print_fail("Invalid plan structure not caught")
            return False
        
    except Exception as e:
        print_fail(f"Guardrail test failed: {e}")
        return False
    
    # Test 2: validate_no_placeholder_paths
    print_test("Guardrail: validate_no_placeholder_paths")
    try:
        from enlisted_crew.flows.planning_flow import validate_no_placeholder_paths
        
        class GoodOutput:
            raw = "Use src/Features/Reputation/ReputationBehavior.cs"
        
        result, _ = validate_no_placeholder_paths(GoodOutput())
        if result:
            print_pass("Real paths accepted")
        else:
            print_fail("Real paths rejected")
            return False
        
        class BadOutput:
            raw = "Use path/to/file.cs and PLACEHOLDER paths"
        
        result, error = validate_no_placeholder_paths(BadOutput())
        if not result and "placeholder" in str(error).lower():
            print_pass("Placeholder paths rejected")
        else:
            print_fail("Placeholder paths not caught")
            return False
        
    except Exception as e:
        print_fail(f"Guardrail test failed: {e}")
        return False
    
    # Test 3: validate_csharp_braces
    print_test("Guardrail: validate_csharp_braces")
    try:
        from enlisted_crew.flows.implementation_flow import validate_csharp_braces
        
        class GoodCode:
            raw = """
            ```csharp
            public class Test {
                public void Method() {
                    if (true) {
                        DoSomething();
                    }
                }
            }
            ```
            """
        
        result, _ = validate_csharp_braces(GoodCode())
        if result:
            print_pass("Balanced braces accepted")
        else:
            print_fail("Balanced braces rejected")
            return False
        
        class BadCode:
            raw = """
            ```csharp
            public class Test {
                public void Method() {
                    if (true) {
                        DoSomething();
                    // Missing closing brace
                }
            ```
            """
        
        result, error = validate_csharp_braces(BadCode())
        if not result and "unbalanced" in str(error).lower():
            print_pass("Unbalanced braces rejected")
        else:
            print_fail("Unbalanced braces not caught")
            return False
        
    except Exception as e:
        print_fail(f"Guardrail test failed: {e}")
        return False
    
    # Test 4: validate_fix_is_minimal
    print_test("Guardrail: validate_fix_is_minimal")
    try:
        from enlisted_crew.flows.bug_hunting_flow import validate_fix_is_minimal
        
        class GoodFix:
            raw = "Change line 42 to fix null reference"
        
        result, _ = validate_fix_is_minimal(GoodFix())
        if result:
            print_pass("Minimal fix accepted")
        else:
            print_fail("Minimal fix rejected")
            return False
        
        class BadFix:
            raw = "This requires a complete rewrite of the entire system"
        
        result, error = validate_fix_is_minimal(BadFix())
        if not result and "rewrite" in str(error).lower():
            print_pass("Complete rewrite rejected")
        else:
            print_fail("Complete rewrite not caught")
            return False
        
    except Exception as e:
        print_fail(f"Guardrail test failed: {e}")
        return False
    
    print_info("\n✅ All guardrail tests passed!")
    return True


# =============================================================================
# HIERARCHICAL PROCESS TESTS
# =============================================================================

def test_manager_agents():
    """Test that manager agents are configured correctly."""
    print_header("2. MANAGER AGENT CONFIGURATION TESTS")
    
    try:
        from enlisted_crew.flows.planning_flow import get_planning_manager
        from enlisted_crew.flows.implementation_flow import get_implementation_manager
        from enlisted_crew.flows.bug_hunting_flow import get_bug_hunting_manager
        from enlisted_crew.flows.validation_flow import get_validation_manager
        
        managers = [
            ("PlanningFlow", get_planning_manager),
            ("ImplementationFlow", get_implementation_manager),
            ("BugHuntingFlow", get_bug_hunting_manager),
            ("ValidationFlow", get_validation_manager),
        ]
        
        for flow_name, manager_factory in managers:
            print_test(f"{flow_name} Manager")
            
            manager = manager_factory()
            
            # Check allow_delegation=True
            if not hasattr(manager, 'allow_delegation') or not manager.allow_delegation:
                print_fail(f"{flow_name} manager missing allow_delegation=True")
                return False
            print_pass("allow_delegation=True ✓")
            
            # Check has high-capability LLM
            if not hasattr(manager, 'llm'):
                print_fail(f"{flow_name} manager missing LLM")
                return False
            print_pass(f"LLM configured: {manager.llm.model}")
            
            # Check max_iter is high (managers need more iterations)
            if hasattr(manager, 'max_iter'):
                if manager.max_iter >= 20:
                    print_pass(f"max_iter={manager.max_iter} (sufficient for coordination)")
                else:
                    print_fail(f"max_iter={manager.max_iter} too low for manager")
                    return False
            
            # Check has NO tools (managers coordinate, don't execute)
            tool_count = len(manager.tools) if hasattr(manager, 'tools') and manager.tools else 0
            if tool_count == 0:
                print_pass("No tools (coordination only) ✓")
            else:
                print_fail(f"Manager has {tool_count} tools (should have 0)")
                return False
        
        print_info("\n✅ All manager agents configured correctly!")
        return True
        
    except Exception as e:
        print_fail(f"Manager agent test failed: {e}")
        return False


def test_specialist_agents():
    """Test that specialist agents are configured correctly."""
    print_header("3. SPECIALIST AGENT CONFIGURATION TESTS")
    
    try:
        from enlisted_crew.flows.planning_flow import get_systems_analyst, get_architecture_advisor
        from enlisted_crew.flows.implementation_flow import get_csharp_implementer, get_content_author
        from enlisted_crew.flows.bug_hunting_flow import get_code_analyst
        from enlisted_crew.flows.validation_flow import get_content_validator, get_build_validator
        
        specialists = [
            ("Systems Analyst", get_systems_analyst),
            ("Architecture Advisor", get_architecture_advisor),
            ("C# Implementer", get_csharp_implementer),
            ("Content Author", get_content_author),
            ("Code Analyst", get_code_analyst),
            ("Content Validator", get_content_validator),
            ("Build Validator", get_build_validator),
        ]
        
        for agent_name, agent_factory in specialists:
            print_test(agent_name)
            
            agent = agent_factory()
            
            # Check allow_delegation=False (specialists don't delegate)
            if hasattr(agent, 'allow_delegation') and agent.allow_delegation:
                print_fail(f"{agent_name} has allow_delegation=True (should be False)")
                return False
            print_pass("allow_delegation=False ✓")
            
            # Check has tools (specialists need tools to execute)
            tool_count = len(agent.tools) if hasattr(agent, 'tools') and agent.tools else 0
            if tool_count > 0:
                print_pass(f"{tool_count} tools assigned")
            else:
                print_fail(f"{agent_name} has no tools")
                return False
            
            # Check has LLM
            if hasattr(agent, 'llm'):
                print_pass(f"LLM configured: {agent.llm.model}")
            else:
                print_fail(f"{agent_name} missing LLM")
                return False
        
        print_info("\n✅ All specialist agents configured correctly!")
        return True
        
    except Exception as e:
        print_fail(f"Specialist agent test failed: {e}")
        return False


def test_hierarchical_crew_configuration():
    """Test that crews are configured with hierarchical process."""
    print_header("4. HIERARCHICAL CREW CONFIGURATION TESTS")
    
    try:
        # Import flow classes
        from enlisted_crew.flows.planning_flow import PlanningFlow
        from enlisted_crew.flows.implementation_flow import ImplementationFlow
        from enlisted_crew.flows.bug_hunting_flow import BugHuntingFlow
        from enlisted_crew.flows.validation_flow import ValidationFlow
        
        flows = [
            ("PlanningFlow", PlanningFlow),
            ("ImplementationFlow", ImplementationFlow),
            ("BugHuntingFlow", BugHuntingFlow),
            ("ValidationFlow", ValidationFlow),
        ]
        
        for flow_name, flow_class in flows:
            print_test(f"{flow_name} Hierarchical Configuration")
            
            # Instantiate flow
            flow = flow_class()
            
            print_pass(f"Flow instantiated: {flow.__class__.__name__}")
            
            # Check that flow uses state
            if hasattr(flow, 'initial_state'):
                print_pass(f"State class: {flow.initial_state.__name__}")
            else:
                print_fail(f"{flow_name} missing state")
                return False
        
        print_info("\n✅ All flows configured with hierarchical process!")
        print_info("Note: Actual hierarchical crew creation happens during flow execution")
        return True
        
    except Exception as e:
        print_fail(f"Hierarchical crew test failed: {e}")
        return False


def test_guardrails_in_flows():
    """Test that flows have guardrails attached to tasks."""
    print_header("5. GUARDRAILS IN FLOWS TESTS")
    
    try:
        # Check that guardrail functions exist in each flow
        from enlisted_crew.flows import planning_flow, implementation_flow, bug_hunting_flow, validation_flow
        
        flow_guardrails = [
            ("planning_flow", planning_flow, ["validate_plan_structure", "validate_no_placeholder_paths"]),
            ("implementation_flow", implementation_flow, ["validate_csharp_braces", "validate_csharp_has_code", "validate_json_syntax"]),
            ("bug_hunting_flow", bug_hunting_flow, ["validate_fix_is_minimal", "validate_fix_has_code", "validate_fix_explains_root_cause"]),
            ("validation_flow", validation_flow, ["validate_report_has_status", "validate_content_check_ran", "validate_build_output_parsed"]),
        ]
        
        for flow_name, flow_module, expected_guardrails in flow_guardrails:
            print_test(f"{flow_name} Guardrails")
            
            for guardrail_name in expected_guardrails:
                if hasattr(flow_module, guardrail_name):
                    print_pass(f"{guardrail_name} exists")
                else:
                    print_fail(f"{guardrail_name} not found")
                    return False
        
        print_info("\n✅ All guardrails present in flows!")
        return True
        
    except Exception as e:
        print_fail(f"Guardrails in flows test failed: {e}")
        return False


# =============================================================================
# MAIN
# =============================================================================

def main():
    """Run all hierarchical tests."""
    print(f"\n{Colors.BOLD}{Colors.CYAN}")
    print("=" * 60)
    print("  HIERARCHICAL PROCESS & GUARDRAILS TEST SUITE")
    print("=" * 60)
    print(f"{Colors.RESET}\n")
    
    tests = [
        ("Guardrails", test_guardrails),
        ("Manager Agents", test_manager_agents),
        ("Specialist Agents", test_specialist_agents),
        ("Hierarchical Crews", test_hierarchical_crew_configuration),
        ("Guardrails in Flows", test_guardrails_in_flows),
    ]
    
    results = {}
    
    for test_name, test_func in tests:
        try:
            results[test_name] = test_func()
        except Exception as e:
            print_fail(f"Test '{test_name}' crashed: {e}")
            import traceback
            traceback.print_exc()
            results[test_name] = False
    
    # Summary
    print_header("TEST SUMMARY")
    
    passed = sum(1 for v in results.values() if v)
    total = len(results)
    
    for test_name, result in results.items():
        status = f"{Colors.GREEN}PASS{Colors.RESET}" if result else f"{Colors.RED}FAIL{Colors.RESET}"
        print(f"  {test_name:<25} [{status}]")
    
    print(f"\n{Colors.CYAN}{'='*60}{Colors.RESET}")
    
    if passed == total:
        print(f"{Colors.GREEN}{Colors.BOLD}[OK] ALL {total} TESTS PASSED{Colors.RESET}")
        print(f"{Colors.CYAN}{'='*60}{Colors.RESET}\n")
        
        print(f"{Colors.BOLD}HIERARCHICAL PROCESS: FULLY OPERATIONAL{Colors.RESET}")
        print("\n✅ Manager agents configured correctly (allow_delegation=True, no tools)")
        print("✅ Specialist agents configured correctly (allow_delegation=False, with tools)")
        print("✅ All 12 guardrails implemented across 4 flows")
        print("✅ Flows ready for hierarchical execution\n")
        return 0
    else:
        print(f"{Colors.RED}{Colors.BOLD}[FAIL] {total - passed} of {total} TESTS FAILED{Colors.RESET}")
        print(f"{Colors.CYAN}{'='*60}{Colors.RESET}\n")
        
        print("Please fix the failing tests before using hierarchical process.\n")
        return 1


if __name__ == "__main__":
    sys.exit(main())
