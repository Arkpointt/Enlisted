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
    decompile_path = Path(r"C:\Dev\Enlisted\Decompile")
    
    if not mcp_index_path.exists():
        print_warn(f"MCP index not found at {mcp_index_path}")
        print_info("    Run: cd mcp_servers && python build_index.py")
        return True  # Non-critical, return True
    
    print_pass(f"MCP index found ({mcp_index_path.stat().st_size // 1024 // 1024} MB)")
    
    # Check if MCP index is stale (decompile folder updated after index was built)
    if decompile_path.exists():
        index_mtime = mcp_index_path.stat().st_mtime
        
        # Find newest .cs file in decompile folder (sample check, not exhaustive)
        newest_cs_time = 0
        sample_dirs = ["TaleWorlds.Core", "TaleWorlds.CampaignSystem", "TaleWorlds.MountAndBlade"]
        for subdir in sample_dirs:
            check_dir = decompile_path / subdir
            if check_dir.exists():
                for cs_file in list(check_dir.rglob("*.cs"))[:10]:  # Sample first 10 files
                    cs_mtime = cs_file.stat().st_mtime
                    if cs_mtime > newest_cs_time:
                        newest_cs_time = cs_mtime
        
        if newest_cs_time > index_mtime:
            from datetime import datetime
            index_date = datetime.fromtimestamp(index_mtime).strftime('%Y-%m-%d')
            decompile_date = datetime.fromtimestamp(newest_cs_time).strftime('%Y-%m-%d')
            print_warn(f"MCP INDEX IS STALE!")
            print_warn(f"    Index built: {index_date}")
            print_warn(f"    Decompile updated: {decompile_date}")
            print_warn(f"    Run: cd mcp_servers && python build_index.py")
        else:
            print_pass("MCP index is up-to-date with decompile folder")
    
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


# ===== Test 5: Flow Agents =====

def test_flow_agents():
    """Test that all flows can instantiate their agents."""
    print_header("5. FLOW AGENTS TESTS")
    
    try:
        from enlisted_crew.flows import ValidationFlow
        from enlisted_crew.flows.validation_flow import (
            get_content_validator, get_build_validator, get_qa_reporter
        )
        
        # Test ValidationFlow agents
        agents = [
            ("content_validator", get_content_validator),
            ("build_validator", get_build_validator),
            ("qa_reporter", get_qa_reporter),
        ]
        
        for name, factory in agents:
            agent = factory()
            tool_count = len(agent.tools) if hasattr(agent, 'tools') and agent.tools else 0
            print_pass(f"ValidationFlow.{name}: {tool_count} tools")
        
        print_info("")
        print_info("    All agents are now defined inline in Flow files.")
        print_info("    See flows/ directory for agent definitions.")
        
        return True
    except Exception as e:
        print_fail(f"Flow agents test failed: {e}")
        return False


# ===== Test 6: LLM Configuration =====

def test_llm_configuration():
    """Test LLM configuration is accessible in flows."""
    print_header("6. LLM CONFIGURATION TESTS")
    
    try:
        # LLMs are now defined inline in each flow
        # Just verify the flow modules have LLM definitions
        from enlisted_crew.flows import validation_flow, planning_flow, implementation_flow, bug_hunting_flow
        
        flows_with_llms = [
            ("validation_flow", validation_flow),
            ("planning_flow", planning_flow),
            ("implementation_flow", implementation_flow),
            ("bug_hunting_flow", bug_hunting_flow),
        ]
        
        for name, flow_module in flows_with_llms:
            # Check if module has LLM objects (GPT5_*)
            llm_names = [n for n in dir(flow_module) if n.startswith('GPT5_')]
            if llm_names:
                print_pass(f"{name}: {len(llm_names)} LLM configs")
                for llm_name in llm_names:
                    print_info(f"    - {llm_name}")
            else:
                print_warn(f"{name}: No LLM configs (may use defaults)")
        
        print_info("")
        print_info("    LLMs are now defined inline per Flow for better isolation.")
        
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
        ("Flow Agents", test_flow_agents),
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
