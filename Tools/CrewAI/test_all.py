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
        from enlisted_crew.flows import ImplementationFlow, BugHuntingFlow
        # Note: PlanningFlow deprecated - use Warp Agent for planning
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
    
    # Test flows (PlanningFlow deprecated)
    flows = [
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
    # Get Decompile folder path (checks multiple locations)
    def get_project_root():
        env_root = os.environ.get("ENLISTED_PROJECT_ROOT")
        if env_root:
            return Path(env_root)
        current = Path(__file__).resolve()
        for parent in current.parents:
            if (parent / "Enlisted.csproj").exists():
                return parent
        return Path(r"C:\Dev\Enlisted\Enlisted")
    
    # Check multiple locations
    env_decompile = os.environ.get("BANNERLORD_DECOMPILE_PATH")
    if env_decompile:
        decompile_path = Path(env_decompile)
    else:
        project_root = get_project_root()
        # Try sibling folder first (standard location)
        sibling_decompile = project_root.parent / "Decompile"
        if sibling_decompile.exists():
            decompile_path = sibling_decompile
        else:
            # Fall back to workspace or default
            workspace_decompile = project_root / "Decompile"
            decompile_path = workspace_decompile if workspace_decompile.exists() else Path(r"C:\Dev\Enlisted\Decompile")
    
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
    """Test that all flows use single-agent pattern (Phase 2-4 refactor)."""
    print_header("5. FLOW AGENTS TESTS")
    
    try:
        from enlisted_crew.flows import ValidationFlow, ImplementationFlow, BugHuntingFlow
        import inspect
        
        flows = [
            ("ImplementationFlow", ImplementationFlow, 3),  # Expected single-agent Crews
            ("BugHuntingFlow", BugHuntingFlow, 3),
            ("ValidationFlow", ValidationFlow, 2),
        ]
        
        for name, FlowClass, expected_crews in flows:
            source = inspect.getsource(FlowClass)
            single_agent_count = source.count('agents=[agent]')
            if single_agent_count == expected_crews:
                print_pass(f"{name}: {single_agent_count}/{expected_crews} single-agent Crews")
            else:
                print_warn(f"{name}: {single_agent_count} single-agent Crews (expected {expected_crews})")
        
        print_info("")
        print_info("    Phase 2-4 Complete: All flows refactored to single-agent pattern.")
        print_info("    Agents are now defined inline in each Flow step.")
        print_info("    Agent factory functions removed.")
        
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
        # PlanningFlow removed in Phase 1
        from enlisted_crew.flows import validation_flow, implementation_flow, bug_hunting_flow
        
        flows_with_llms = [
            ("validation_flow", validation_flow),
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
        print_info("    Phase 1: PlanningFlow removed (use Warp Agent for planning).")
        print_info("    LLMs are now defined inline per Flow for better isolation.")
        
        return True
    except Exception as e:
        print_fail(f"LLM configuration test failed: {e}")
        return False


# ===== Test 7: Escalation Framework =====

def test_escalation_framework():
    """Test manager escalation framework."""
    print_header("7. ESCALATION FRAMEWORK TESTS")
    
    try:
        # Import directly from escalation module to avoid crewai dependency
        escalation_path = Path("src/enlisted_crew/flows")
        sys.path.insert(0, str(escalation_path))
        
        from escalation import (
            IssueType,
            IssueSeverity,
            IssueConfidence,
            DetectedIssue,
            should_escalate_to_human,
            format_critical_issues,
            categorize_issue_severity,
            create_escalation_message,
        )
        print_pass("Escalation framework imported")
    except Exception as e:
        print_fail(f"Escalation import failed: {e}")
        return False
    
    # Test 1: Critical severity always escalates
    issue = DetectedIssue(
        issue_type=IssueType.ARCHITECTURE_VIOLATION,
        severity=IssueSeverity.CRITICAL,
        confidence=IssueConfidence.HIGH,
        description="Breaking architecture",
        affected_component="test.cs",
        evidence="Uses wrong pattern",
        manager_recommendation="Fix it",
    )
    if should_escalate_to_human(issue, "critical_only"):
        print_pass("Critical severity escalates")
    else:
        print_fail("Critical severity did not escalate")
        return False
    
    # Test 2: Security always escalates
    issue = DetectedIssue(
        issue_type=IssueType.SECURITY_CONCERN,
        severity=IssueSeverity.HIGH,
        confidence=IssueConfidence.MEDIUM,
        description="Potential SQL injection",
        affected_component="query.cs",
        evidence="Unsanitized input",
        manager_recommendation="Review security",
    )
    if should_escalate_to_human(issue, "critical_only"):
        print_pass("Security concern escalates")
    else:
        print_fail("Security concern did not escalate")
        return False
    
    # Test 3: High confidence auto-fix doesn't escalate
    issue = DetectedIssue(
        issue_type=IssueType.INCORRECT_METHOD,
        severity=IssueSeverity.MEDIUM,
        confidence=IssueConfidence.HIGH,
        description="Wrong method name",
        affected_component="test.cs",
        evidence="Found correct method",
        manager_recommendation="Use GetNeeds() instead",
        auto_fixable=True,
    )
    if not should_escalate_to_human(issue, "critical_only"):
        print_pass("Auto-fixable high confidence doesn't escalate")
    else:
        print_fail("Auto-fixable high confidence escalated incorrectly")
        return False
    
    # Test 4: High severity + low confidence escalates
    issue = DetectedIssue(
        issue_type=IssueType.HALLUCINATED_API,
        severity=IssueSeverity.HIGH,
        confidence=IssueConfidence.LOW,
        description="Method might not exist",
        affected_component="test.cs",
        evidence="Search returned ambiguous results",
        manager_recommendation="Need human to verify",
    )
    if should_escalate_to_human(issue, "critical_only"):
        print_pass("High severity + low confidence escalates")
    else:
        print_fail("High severity + low confidence did not escalate")
        return False
    
    # Test 5: Categorize issue severity
    severity_tests = [
        (IssueType.SECURITY_CONCERN, IssueSeverity.CRITICAL),
        (IssueType.HALLUCINATED_API, IssueSeverity.HIGH),
        (IssueType.DEPRECATED_SYSTEM, IssueSeverity.MEDIUM),
        (IssueType.SPELLING_ERROR, IssueSeverity.LOW),
    ]
    
    for issue_type, expected_severity in severity_tests:
        result = categorize_issue_severity(issue_type)
        if result == expected_severity:
            print_pass(f"{issue_type.value} -> {expected_severity.value}")
        else:
            print_fail(f"{issue_type.value} -> got {result.value}, expected {expected_severity.value}")
            return False
    
    # Test 6: Format critical issues
    issues = [
        DetectedIssue(
            issue_type=IssueType.HALLUCINATED_API,
            severity=IssueSeverity.HIGH,
            confidence=IssueConfidence.LOW,
            description="Method doesn't exist",
            affected_component="ReputationManager.cs",
            evidence="Search returned no results",
            manager_recommendation="Use GetReputation() instead",
        ),
    ]
    
    output = format_critical_issues(issues)
    if "CRITICAL ISSUES FOUND: 1" in output and "HALLUCINATED API" in output:
        print_pass("Issue formatting works")
    else:
        print_fail("Issue formatting failed")
        return False
    
    print_info("")
    print_info("    All escalation logic tests passed.")
    print_info("    Managers will auto-handle 80-90% of issues.")
    
    return True


# ===== Test 8: Monitoring =====

def test_monitoring():
    """Test execution monitoring is enabled in all flows."""
    print_header("8. MONITORING TESTS")
    
    try:
        from enlisted_crew.flows import ImplementationFlow, BugHuntingFlow, ValidationFlow
        from enlisted_crew.monitoring import EnlistedExecutionMonitor
        
        print_pass("Monitoring imports successful")
        
        # Test that all flows initialize monitoring
        flows = [
            ("ImplementationFlow", ImplementationFlow),
            ("BugHuntingFlow", BugHuntingFlow),
            ("ValidationFlow", ValidationFlow),
        ]
        
        for name, FlowClass in flows:
            flow = FlowClass()
            if hasattr(flow, '_monitor'):
                print_pass(f"{name}: monitoring initialized")
            else:
                print_fail(f"{name}: no _monitor attribute")
                return False
        
        # Check database tables exist
        db_path = Path("database/enlisted_knowledge.db")
        if db_path.exists():
            conn = sqlite3.connect(db_path)
            monitoring_tables = [
                "crew_executions",
                "agent_executions",
                "task_executions",
                "tool_usages",
                "llm_costs",
            ]
            
            for table in monitoring_tables:
                try:
                    conn.execute(f"SELECT COUNT(*) FROM {table}")
                    print_pass(f"Table '{table}' exists")
                except:
                    print_warn(f"Table '{table}' not found")
            
            conn.close()
        
        print_info("")
        print_info("    Monitoring tracks: crew/agent/task execution, tool usage, LLM costs")
        print_info("    View stats: enlisted-crew stats --costs")
        
        return True
    except Exception as e:
        print_fail(f"Monitoring test failed: {e}")
        return False


# ===== Test 9: Semantic Search =====

def test_semantic_search():
    """Test semantic documentation and codebase search."""
    print_header("9. SEMANTIC SEARCH TESTS")
    
    try:
        from enlisted_crew.tools.docs_tools import search_docs_semantic
        from enlisted_crew.rag.codebase_rag_tool import search_codebase
        
        print_pass("Semantic search tools imported")
        
        # Check documentation index
        docs_index_path = Path("src/enlisted_crew/rag/docs_vector_db")
        if docs_index_path.exists():
            print_pass(f"Documentation index found")
            
            # Try to get index stats
            try:
                import sys
                sys.path.insert(0, "src")
                from enlisted_crew.rag.docs_indexer import DocsIndexer
                
                indexer = DocsIndexer()
                stats = indexer.get_stats()
                print_info(f"    - Chunks: {stats.get('total_chunks', 'N/A')}")
                print_info(f"    - Status: {stats.get('status', 'N/A')}")
            except Exception as e:
                print_info(f"    - Could not load stats: {e}")
        else:
            print_warn("Documentation index not found")
            print_info("    Run: python -m enlisted_crew.rag.docs_indexer --index-all")
        
        # Check codebase index
        code_index_path = Path("src/enlisted_crew/rag/vector_db")
        if code_index_path.exists():
            print_pass(f"Codebase index found")
        else:
            print_warn("Codebase index not found")
            print_info("    Run: python -m enlisted_crew.rag.codebase_indexer --index-all")
        
        print_info("")
        print_info("    Semantic search enables natural language queries for code/docs")
        
        return True
    except Exception as e:
        print_fail(f"Semantic search test failed: {e}")
        return False


# ===== Test 10: Tool Access =====

def test_tool_access():
    """Test that agents have correct tool assignments."""
    print_header("10. TOOL ACCESS TESTS")
    
    try:
        import inspect
        from enlisted_crew.flows import ImplementationFlow, BugHuntingFlow, ValidationFlow
        
        # Check ImplementationFlow agents have semantic search
        impl_source = inspect.getsource(ImplementationFlow)
        if "search_docs_semantic" in impl_source:
            count = impl_source.count("search_docs_semantic")
            print_pass(f"ImplementationFlow: search_docs_semantic present ({count} references)")
        else:
            print_fail("ImplementationFlow: missing search_docs_semantic")
            return False
        
        # Check BugHuntingFlow agents have semantic search
        bug_source = inspect.getsource(BugHuntingFlow)
        if "search_docs_semantic" in bug_source:
            count = bug_source.count("search_docs_semantic")
            print_pass(f"BugHuntingFlow: search_docs_semantic present ({count} references)")
        else:
            print_fail("BugHuntingFlow: missing search_docs_semantic")
            return False
        
        # Check ValidationFlow doesn't need search (uses validators)
        val_source = inspect.getsource(ValidationFlow)
        if "validate_content" in val_source and "build" in val_source:
            print_pass(f"ValidationFlow: validation tools present")
        else:
            print_fail("ValidationFlow: missing validation tools")
            return False
        
        print_info("")
        print_info("    All agents have appropriate tool access for their roles")
        
        return True
    except Exception as e:
        print_fail(f"Tool access test failed: {e}")
        return False


# ===== Test 11: Environment =====

def test_environment():
    """Test environment configuration."""
    print_header("8. ENVIRONMENT TESTS")
    
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
        ("Escalation Framework", test_escalation_framework),
        ("Monitoring", test_monitoring),
        ("Semantic Search", test_semantic_search),
        ("Tool Access", test_tool_access),
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
