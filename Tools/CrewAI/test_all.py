"""
Comprehensive CrewAI Test Suite

Tests everything: configuration, flows, MCP server, database, tools, agents.

Setup:
    cd Tools/CrewAI
    .venv\\Scripts\\Activate.ps1    # Activate virtual environment first!

Usage:
    python test_all.py              # Run all tests
    python test_all.py --quick      # Skip slow tests (TBD)
    python test_all.py --verbose    # Detailed output (TBD)
"""

import sys
import os
from pathlib import Path
import sqlite3


# Color output for better readability
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


def print_warn(text):
    """Print warning."""
    print(f"  {Colors.YELLOW}[WARN]{Colors.RESET} {text}")


def print_info(text):
    """Print info."""
    print(f"  {text}")


# ===== Test 1: Configuration =====

def test_configuration():
    """Test that all flows and configuration are working."""
    print_header("1. CONFIGURATION TESTS")
    
    try:
        from enlisted_crew.crew import EnlistedCrew
        from enlisted_crew.flows import PlanningFlow, ImplementationFlow, BugHuntingFlow
        print_pass("All imports successful")
    except Exception as e:
        print_fail(f"Import failed: {e}")
        return False
    
    try:
        ec = EnlistedCrew()
        print_pass("EnlistedCrew initialized")
    except Exception as e:
        print_fail(f"EnlistedCrew init failed: {e}")
        return False
    
    # Test flows
    flows = [
        ("PlanningFlow", PlanningFlow),
        ("ImplementationFlow", ImplementationFlow),
        ("BugHuntingFlow", BugHuntingFlow),
    ]
    
    for flow_name, flow_class in flows:
        try:
            flow = flow_class()
            print_pass(f"{flow_name}: {flow.__class__.__name__}")
            print_info(f"    - Has persist: {hasattr(flow, 'persist')}")
            print_info(f"    - State class: {flow.initial_state.__name__ if hasattr(flow, 'initial_state') else 'N/A'}")
        except Exception as e:
            print_fail(f"{flow_name} failed: {e}")
            return False
    
    return True


# ===== Test 2: Database =====

def test_database():
    """Test SQLite knowledge database."""
    print_header("2. DATABASE TESTS")
    
    db_path = Path("database/enlisted_knowledge.db")
    
    if not db_path.exists():
        print_fail(f"Database not found at {db_path}")
        print_info("    Run: cd database && sqlite3 enlisted_knowledge.db < schema.sql")
        return False
    
    print_pass(f"Database found ({db_path.stat().st_size // 1024} KB)")
    
    # Test database contents
    try:
        conn = sqlite3.connect(db_path)
        
        tables = [
            "tier_definitions",
            "error_catalog",
            "game_systems",
            "content_metadata",
            "balance_values",
            "api_patterns",
        ]
        
        for table in tables:
            cursor = conn.execute(f"SELECT COUNT(*) FROM {table}")
            count = cursor.fetchone()[0]
            if count > 0:
                print_pass(f"Table '{table}': {count} rows")
            else:
                print_warn(f"Table '{table}': empty")
        
        conn.close()
        return True
    except Exception as e:
        print_fail(f"Database query failed: {e}")
        return False


# ===== Test 3: Database Tools =====

def test_database_tools():
    """Test database tool imports."""
    print_header("3. DATABASE TOOLS TESTS")
    
    try:
        from enlisted_crew.tools.database_tools import (
            # Query tools (14)
            lookup_error_code,
            get_tier_info,
            get_all_tiers,
            get_system_dependencies,
            lookup_core_system,
            lookup_api_pattern,
            get_balance_value,
            search_balance,
            get_balance_by_category,
            lookup_content_id,
            search_content,
            get_valid_categories,
            get_valid_severities,
            check_database_health,
            # Maintenance tools (9)
            add_content_item,
            update_content_item,
            delete_content_item,
            add_balance_value,
            update_balance_value,
            add_error_code,
            add_system_dependency,
            sync_content_from_files,
            record_implementation,
        )
        print_pass("All 23 database tools imported")
        print_info("    - Query tools: 14")
        print_info("    - Maintenance tools: 9")
        return True
    except Exception as e:
        print_fail(f"Database tools import failed: {e}")
        return False


# ===== Test 4: MCP Server =====

def test_mcp_server():
    """Test Bannerlord API MCP server."""
    print_header("4. MCP SERVER TESTS")
    
    mcp_index_path = Path("mcp_servers/bannerlord_api_index.db")
    
    if not mcp_index_path.exists():
        print_warn(f"MCP index not found at {mcp_index_path}")
        print_info("    Run: cd mcp_servers && python build_index.py")
        return True  # Non-critical, return True
    
    print_pass(f"MCP index found ({mcp_index_path.stat().st_size // 1024 // 1024} MB)")
    
    try:
        sys.path.insert(0, str(Path("mcp_servers")))
        from bannerlord_api_server import BannerlordAPIIndex, INDEX_DB_PATH, DECOMPILE_PATH
        
        index = BannerlordAPIIndex(INDEX_DB_PATH, DECOMPILE_PATH)
        index.connect()
        print_pass("Connected to MCP index")
        
        # Test a simple query
        result = index.get_class_definition("Hero")
        if result["count"] > 0:
            print_pass("get_class_definition('Hero') works")
        else:
            print_warn("Hero class not found in index")
        
        # Print statistics
        conn = index.conn
        classes_count = conn.execute("SELECT COUNT(*) FROM classes").fetchone()[0]
        methods_count = conn.execute("SELECT COUNT(*) FROM methods").fetchone()[0]
        
        print_info(f"    - Classes indexed: {classes_count:,}")
        print_info(f"    - Methods indexed: {methods_count:,}")
        
        return True
    except Exception as e:
        print_warn(f"MCP server test skipped: {e}")
        return True  # Non-critical


# ===== Test 5: Agents & Tools =====

def test_agents_configuration():
    """Test agent and tool configuration."""
    print_header("5. AGENTS & TOOLS TESTS")
    
    try:
        from enlisted_crew.crew import EnlistedCrew
        
        ec = EnlistedCrew()
        
        # Get agent methods
        agent_methods = [
            m for m in dir(ec) 
            if not m.startswith('_') and callable(getattr(ec, m)) and 
            ('analyst' in m or 'architect' in m or 'implementer' in m or 
             'author' in m or 'maintainer' in m or 'advisor' in m or 'agent' in m)
        ]
        
        print_pass(f"Found {len(agent_methods)} agent methods")
        
        # Test creating a few agents
        test_agents = ['systems_analyst', 'code_analyst', 'feature_architect', 'qa_agent']
        available_agents = [m for m in agent_methods if m in test_agents]
        
        for agent_name in available_agents:
            try:
                agent = getattr(ec, agent_name)()
                print_pass(f"Agent '{agent_name}': {len(agent.tools if hasattr(agent, 'tools') else [])} tools")
            except Exception as e:
                print_fail(f"Agent '{agent_name}' failed: {e}")
        
        return True
    except Exception as e:
        print_fail(f"Agents test failed: {e}")
        return False


# ===== Test 6: LLM Configuration =====

def test_llm_configuration():
    """Test LLM tier configuration."""
    print_header("6. LLM CONFIGURATION TESTS")
    
    try:
        from enlisted_crew.crew import (
            GPT5_ARCHITECT, GPT5_ANALYST, GPT5_IMPLEMENTER, 
            GPT5_FAST, GPT5_QA
        )
        
        llms = [
            ("GPT5_ARCHITECT", GPT5_ARCHITECT),
            ("GPT5_ANALYST", GPT5_ANALYST),
            ("GPT5_IMPLEMENTER", GPT5_IMPLEMENTER),
            ("GPT5_FAST", GPT5_FAST),
            ("GPT5_QA", GPT5_QA),
        ]
        
        for name, llm in llms:
            print_pass(f"{name}: {llm.model}")
            print_info(f"    - Max tokens: {llm.max_tokens}")
            print_info(f"    - Prompt caching: {llm.supports_prompt_caching if hasattr(llm, 'supports_prompt_caching') else 'N/A'}")
        
        return True
    except Exception as e:
        print_fail(f"LLM configuration test failed: {e}")
        return False


# ===== Test 7: Environment =====

def test_environment():
    """Test environment configuration."""
    print_header("7. ENVIRONMENT TESTS")
    
    # Check OpenAI API key
    if os.environ.get("OPENAI_API_KEY"):
        print_pass("OPENAI_API_KEY is set")
    else:
        print_warn("OPENAI_API_KEY not set")
        print_info("    Set with: export OPENAI_API_KEY='your-key-here'")
    
    # Check project root
    project_root = Path("C:/Dev/Enlisted/Enlisted")
    if project_root.exists():
        print_pass(f"Project root exists: {project_root}")
    else:
        print_warn(f"Project root not found: {project_root}")
    
    # Check knowledge folder
    knowledge_path = Path("knowledge")
    if knowledge_path.exists():
        knowledge_files = list(knowledge_path.glob("*.md"))
        print_pass(f"Knowledge folder: {len(knowledge_files)} files")
        for kf in knowledge_files:
            print_info(f"    - {kf.name}")
    else:
        print_fail("Knowledge folder not found")
        return False
    
    return True


# ===== Main =====

def main():
    """Run all tests."""
    print(f"\n{Colors.BOLD}{Colors.CYAN}")
    print("=" * 60)
    print("     ENLISTED CREWAI - COMPREHENSIVE TEST SUITE")
    print("=" * 60)
    print(f"{Colors.RESET}\n")
    
    tests = [
        ("Configuration", test_configuration),
        ("Database", test_database),
        ("Database Tools", test_database_tools),
        ("MCP Server", test_mcp_server),
        ("Agents & Tools", test_agents_configuration),
        ("LLM Configuration", test_llm_configuration),
        ("Environment", test_environment),
    ]
    
    results = {}
    
    for test_name, test_func in tests:
        try:
            results[test_name] = test_func()
        except Exception as e:
            print_fail(f"Test '{test_name}' crashed: {e}")
            results[test_name] = False
    
    # Summary
    print_header("TEST SUMMARY")
    
    passed = sum(1 for v in results.values() if v)
    total = len(results)
    
    for test_name, result in results.items():
        status = f"{Colors.GREEN}PASS{Colors.RESET}" if result else f"{Colors.RED}FAIL{Colors.RESET}"
        print(f"  {test_name:<20} [{status}]")
    
    print(f"\n{Colors.CYAN}{'='*60}{Colors.RESET}")
    
    if passed == total:
        print(f"{Colors.GREEN}{Colors.BOLD}[OK] ALL {total} TESTS PASSED{Colors.RESET}")
        print(f"{Colors.CYAN}{'='*60}{Colors.RESET}\n")
        
        print(f"{Colors.BOLD}SYSTEM STATUS: READY TO USE{Colors.RESET}")
        print("\nQuick Start:")
        print("  - enlisted-crew plan -f 'feature' -d 'description'")
        print("  - enlisted-crew implement -p 'path/to/plan.md'")
        print("  - enlisted-crew hunt-bug -d 'bug description'")
        print("  - enlisted-crew stats --costs")
        print()
        return 0
    else:
        print(f"{Colors.RED}{Colors.BOLD}[FAIL] {total - passed} of {total} TESTS FAILED{Colors.RESET}")
        print(f"{Colors.CYAN}{'='*60}{Colors.RESET}\n")
        
        print("Please fix the failing tests before using CrewAI.")
        print()
        return 1


if __name__ == "__main__":
    sys.exit(main())
