#!/usr/bin/env python
"""
Enlisted CrewAI CLI

Core workflows for Enlisted mod development:

    enlisted-crew hunt-bug -d "bug" -e "E-XXX-*"        # Find & fix bugs
    enlisted-crew implement -p "plan.md"                # Build from plan
    enlisted-crew analyze-system "Supply,Morale"        # Analyze systems

Utility:
    enlisted-crew validate                              # Pre-commit check
    enlisted-crew stats [-c crew_name]                  # View execution metrics

Note: For planning/design tasks, use Warp Agent directly. It has full codebase
      access and is faster than multi-agent orchestration.
"""

import argparse
import sys
import os
from pathlib import Path

from dotenv import load_dotenv

from .crew import EnlistedCrew
from .tools import SearchCache
from .monitoring import enable_monitoring, print_execution_report, print_cost_report
from .hooks import reset_all_hooks, print_all_summaries


def main():
    """Main CLI entry point."""
    # Load .env from Tools/CrewAI directory
    env_path = Path(__file__).parent.parent.parent / ".env"
    load_dotenv(env_path)
    
    # Enable execution monitoring for all workflows
    enable_monitoring()
    
    # Note: Execution hooks are automatically registered on import
    
    parser = argparse.ArgumentParser(
        description="Enlisted CrewAI - Multi-agent workflows for Enlisted mod development",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    
    subparsers = parser.add_subparsers(dest="command", help="Available commands")
    
    # validate command
    validate_parser = subparsers.add_parser(
        "validate",
        help="Run full validation on all content",
    )
    
    # stats command - VIEW EXECUTION STATISTICS
    stats_parser = subparsers.add_parser(
        "stats",
        help="View execution statistics from monitoring",
    )
    stats_parser.add_argument(
        "--crew", "-c",
        default=None,
        help="Filter by crew name (e.g., 'PlanningFlow', 'ImplementationFlow', 'BugHuntingFlow')",
    )
    stats_parser.add_argument(
        "--costs", 
        action="store_true",
        help="Show LLM cost tracking statistics",
    )
    
    # implement command - BUILD FROM APPROVED PLAN
    implement_parser = subparsers.add_parser(
        "implement",
        help="Build a feature from an approved planning doc",
    )
    implement_parser.add_argument(
        "--plan", "-p",
        required=True,
        help="Path to approved planning doc (e.g., docs/CrewAI_Plans/feature.md)",
    )
    
    # hunt-bug command (Flow-based)
    hunt_parser = subparsers.add_parser(
        "hunt-bug",
        help="Investigate a bug using Flow-based workflow",
    )
    hunt_parser.add_argument(
        "--description", "-d",
        required=True,
        help="Description of the bug from user report",
    )
    hunt_parser.add_argument(
        "--error-codes", "-e",
        default="none",
        help="Any error codes (E-*, W-*) from logs",
    )
    hunt_parser.add_argument(
        "--repro-steps", "-r",
        default="unknown",
        help="Steps to reproduce the bug",
    )
    hunt_parser.add_argument(
        "--context", "-c",
        default=None,
        help="Additional context (game state, mods, etc.)",
    )
    hunt_parser.add_argument(
        "--output", "-o",
        default=None,
        help="Output file for the report (default: print to console)",
    )
    hunt_parser.add_argument(
        "--logs", "-l",
        default=None,
        help="User-provided log content OR path to log file from end user (not local dev logs)",
    )
    
    # analyze command - NATURAL LANGUAGE ANALYSIS (NEW)
    analyze_nl_parser = subparsers.add_parser(
        "analyze",
        help="Analyze gameplay/systems using natural language query",
    )
    analyze_nl_parser.add_argument(
        "query",
        nargs="?",
        default=None,
        help="Natural language query (e.g., 'analyze my gameplay flow', 'find gaps in player progression')",
    )
    analyze_nl_parser.add_argument(
        "--focus",
        choices=["integration", "efficiency", "both"],
        default="both",
        help="Analysis focus: integration gaps, efficiency issues, or both (default: both)",
    )
    analyze_nl_parser.add_argument(
        "--output", "-o",
        default=None,
        help="Custom output path (default: docs/CrewAI_Plans/{query-slug}-analysis.md)",
    )
    
    # analyze-system command - SYSTEM INTEGRATION ANALYSIS (explicit systems)
    analyze_parser = subparsers.add_parser(
        "analyze-system",
        help="Analyze specific systems by name (use 'analyze' for natural language)",
    )
    analyze_parser.add_argument(
        "systems",
        nargs="?",
        default=None,
        help="System names to analyze (comma-separated, e.g., 'Supply,Morale,Reputation')",
    )
    analyze_parser.add_argument(
        "--all",
        action="store_true",
        help="Analyze all major systems",
    )
    analyze_parser.add_argument(
        "--focus",
        choices=["integration", "efficiency", "both"],
        default="both",
        help="Analysis focus: integration gaps, efficiency issues, or both (default: both)",
    )
    analyze_parser.add_argument(
        "--subsystem",
        action="store_true",
        help="Treat as subsystem (narrower scope analysis)",
    )
    analyze_parser.add_argument(
        "--output", "-o",
        default=None,
        help="Custom output path (default: docs/CrewAI_Plans/{system-name}-analysis.md)",
    )
    
    args = parser.parse_args()
    
    if not args.command:
        parser.print_help()
        sys.exit(1)
    
    crew = EnlistedCrew()
    
    try:
        if args.command == "validate":
            result = run_validation(crew)
        elif args.command == "stats":
            print_execution_report(crew_name=args.crew)
            if args.costs:
                print_cost_report(crew_name=args.crew)
            return  # No result to print
        elif args.command == "implement":
            result = run_implement(crew, args.plan)
        elif args.command == "hunt-bug":
            result = run_hunt_bug(
                description=args.description,
                error_codes=args.error_codes,
                repro_steps=args.repro_steps,
                context=args.context,
                output_file=args.output,
                user_logs=args.logs,
            )
        elif args.command == "analyze":
            result = run_nl_analysis(
                query=args.query,
                focus=args.focus,
                output_path=args.output,
            )
        elif args.command == "analyze-system":
            result = run_analysis(
                systems=args.systems,
                analyze_all=args.all,
                focus=args.focus,
                subsystem=args.subsystem,
                output_path=args.output,
            )
        else:
            parser.print_help()
            sys.exit(1)
        
        print("\n" + "=" * 60)
        print("RESULT")
        print("=" * 60)
        print(result)
        
    except KeyboardInterrupt:
        print("\nAborted.")
        sys.exit(130)
    except Exception as e:
        print(f"\nError: {e}", file=sys.stderr)
        sys.exit(1)


def run_validation(crew: EnlistedCrew):
    """Run quick validation check using ValidationFlow."""
    from .flows import ValidationFlow
    
    SearchCache.clear()
    reset_all_hooks()  # Reset cost tracking and safety guards
    
    flow = ValidationFlow()
    result = flow.kickoff()
    
    # Print cost summary
    print_all_summaries()
    
    # Return the final report from the flow state
    if hasattr(result, 'final_report'):
        return result.final_report
    return str(result)


def run_implement(crew: EnlistedCrew, plan_path: str):
    """
    IMPLEMENTATION WORKFLOW - Build from an approved plan.
    
    Uses ImplementationFlow which:
    1. Verifies what's already implemented FIRST
    2. Routes around completed work (no duplicates)
    3. Only implements what's MISSING
    4. Always validates and updates docs
    """
    from .flows import ImplementationFlow
    
    SearchCache.clear()
    reset_all_hooks()  # Reset cost tracking and safety guards
    print("\n" + "=" * 60)
    print("IMPLEMENTATION WORKFLOW (Flow-based)")
    print("=" * 60)
    print(f"\nPlan: {plan_path}")
    print("This workflow will:")
    print("  1. Verify what's already implemented")
    print("  2. Skip completed work automatically")
    print("  3. Only implement missing parts")
    print("  4. Validate everything")
    print("  5. Update documentation\n")
    
    # Use the new Flow-based workflow
    flow = ImplementationFlow()
    result = flow.kickoff(inputs={
        "plan_path": plan_path,
    })
    
    # Print cost and safety summaries
    print_all_summaries()
    
    # Return the final report from the flow state
    if hasattr(result, 'final_report'):
        return result.final_report
    return str(result)


def run_hunt_bug(
    description: str,
    error_codes: str = "none",
    repro_steps: str = "unknown",
    context: str = None,
    output_file: str = None,
    user_logs: str = None,
):
    """
    BUG HUNTING WORKFLOW - Find and fix bugs completely.
    
    Chain: investigate -> analyze -> fix -> validate
    """
    from .crew import EnlistedCrew
    from pathlib import Path
    from datetime import datetime
    import re
    
    SearchCache.clear()
    reset_all_hooks()  # Reset cost tracking and safety guards
    
    # Handle user_logs - could be file path or direct content
    user_log_content = None
    if user_logs:
        log_path = Path(user_logs)
        if log_path.exists() and log_path.is_file():
            print(f"Reading user log file: {user_logs}")
            user_log_content = log_path.read_text(encoding='utf-8', errors='replace')
        else:
            user_log_content = user_logs
    
    print("\n" + "=" * 60)
    print("BUG HUNTING WORKFLOW")
    print("=" * 60)
    print(f"\nBug: {description[:100]}...")
    print(f"Error Codes: {error_codes}")
    print(f"Repro Steps: {repro_steps}")
    print(f"User Logs: {'Provided' if user_log_content else 'None'}")
    print("\nStarting bug investigation...\n")
    
    # Use BugHuntingFlow for state management and conditional routing
    from .flows import BugHuntingFlow
    
    flow = BugHuntingFlow()
    result = flow.kickoff(inputs={
        "bug_description": description,
        "error_codes": error_codes,
        "repro_steps": repro_steps,
        "context": context or "No additional context",
        "user_log_content": user_log_content or "No logs provided",
    })
    
    # Print cost and safety summaries
    print_all_summaries()
    
    # Extract report from flow state
    if hasattr(result, 'final_report'):
        report = result.final_report
    else:
        report = str(result)
    
    # Auto-save to docs/CrewAI_Plans/ if no output file specified
    if not output_file:
        # Generate filename from bug description
        safe_desc = re.sub(r'[^a-z0-9]+', '-', description.lower())[:50].strip('-')
        timestamp = datetime.now().strftime("%Y%m%d-%H%M%S")
        filename = f"bug-hunt-{safe_desc}-{timestamp}.md"
        
        # Get project root (3 levels up from src/enlisted_crew/main.py)
        project_root = Path(__file__).parent.parent.parent.parent
        output_file = project_root / "docs" / "CrewAI_Plans" / filename
    else:
        output_file = Path(output_file)
    
    # Ensure parent directory exists
    output_file.parent.mkdir(parents=True, exist_ok=True)
    
    # Write report to file
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write(report)
    print(f"\nReport saved to: {output_file}")
    
    return report


def run_nl_analysis(
    query: str = None,
    focus: str = "both",
    output_path: str = None,
):
    """
    NATURAL LANGUAGE ANALYSIS - Interpret query and run appropriate analysis.
    
    Uses an agent to understand the query and identify which systems to analyze.
    Example queries:
    - "analyze my gameplay flow"
    - "find gaps in player progression"
    - "what's wrong with the promotion system"
    - "audit player visibility"
    """
    from crewai import Agent, Task, Crew, Process, LLM
    from .flows import SystemAnalysisFlow, AnalysisFocus
    import re
    
    SearchCache.clear()
    reset_all_hooks()
    
    if not query:
        print("Error: Must provide a query")
        print('Example: enlisted-crew analyze "analyze my gameplay flow"')
        sys.exit(1)
    
    print("\n" + "=" * 60)
    print("NATURAL LANGUAGE ANALYSIS")
    print("=" * 60)
    print(f"\nQuery: {query}")
    print("\nInterpreting query...\n")
    
    # Import tools for exploration
    from .rag.codebase_rag_tool import search_codebase
    from .tools.docs_tools import search_docs_semantic
    from .tools import get_game_systems
    
    # Use an agent to interpret the query AND explore the codebase
    interpreter = Agent(
        role="Query Interpreter & System Explorer",
        goal="Understand the user's question, explore the codebase to find relevant systems, and identify what to analyze",
        backstory="""You are an expert at understanding the Enlisted Bannerlord mod and exploring its codebase.
        
        ENLISTED ARCHITECTURE:
        - src/Features/ contains all gameplay systems (one folder per system)
        - Each system typically has: Manager, Behavior, State classes
        - ModuleData/Enlisted/ has JSON content (events, decisions, orders)
        - docs/Features/ has documentation for each system
        
        KNOWN SYSTEMS (in src/Features/):
        - Enlistment: Joining/leaving service with a lord
        - Ranks: Tier progression (T1-T9), promotion requirements
        - Escalation: Lord/Officer/Soldier reputation tracking  
        - Company: Morale, Supply, Readiness (CompanyNeeds)
        - Orders: Mission directives from chain of command
        - Content: Events, Decisions, ContentOrchestrator, Daily Brief
        - Combat: Battle participation, formation assignment
        - Equipment: Quartermaster, gear management
        - Conditions: Injury/illness status
        - Identity: Role detection, traits
        - Interface: Camp Hub, menus, UI
        
        CONCEPTUAL MAPPINGS:
        - "gameplay flow" = how player progresses through the game
        - "player progression" = Ranks + Reputation + unlocks
        - "visibility" / "feedback" = what players can SEE (UI, tooltips, logs)
        - "integration" / "gaps" = how systems connect (or fail to)
        
        You can EXPLORE the codebase to find systems relevant to the query.
        Use search_codebase to find code, search_docs_semantic to find documentation.
        Use get_game_systems to list all registered systems.
        
        Think about what the user REALLY wants to know, not just literal keywords.""",
        llm=LLM(model="gpt-5.2", max_completion_tokens=4000, reasoning_effort="high"),
        tools=[search_codebase, search_docs_semantic, get_game_systems],
        max_iter=6,
        allow_delegation=False,
        verbose=True,
    )
    
    interpret_task = Task(
        description=f"""User query: "{query}"

=== YOUR TASK ===
Understand what the user wants to analyze and identify which systems are relevant.

**STEP 1: Interpret the Query**
What is the user REALLY asking about? Think beyond literal keywords:
- "gameplay flow" = the player's journey through the mod
- "progression" = how players advance (ranks, reputation, unlocks)
- "visibility" = what players can see and understand
- "gaps" = missing connections or features

**STEP 2: Explore if Needed**
If the query is vague or you're unsure which systems are relevant:
- Use get_game_systems to see all registered systems
- Use search_codebase to find code related to the query
- Use search_docs_semantic to find relevant documentation

**STEP 3: Identify Systems**
Based on your understanding and exploration, list the systems to analyze.
Think about:
- Which systems directly relate to the query?
- Which RELATED systems should also be included?
- What might the user NOT have mentioned but would want to know about?

**Output Format:**
After your reasoning, output ONLY the final answer as:
SYSTEMS: System1, System2, System3

Example: SYSTEMS: Ranks, Reputation, Escalation, Interface""",
        expected_output="List of system names prefixed with 'SYSTEMS:'",
        agent=interpreter,
    )
    
    crew = Crew(
        agents=[interpreter],
        tasks=[interpret_task],
        process=Process.sequential,
        verbose=False,
    )
    
    result = crew.kickoff()
    result_str = str(result).strip()
    
    # Extract systems from "SYSTEMS: x, y, z" format
    systems_str = ""
    for line in result_str.split('\n'):
        if 'SYSTEMS:' in line.upper():
            systems_str = line.split(':', 1)[-1].strip()
            break
    
    # Fallback: if no SYSTEMS: prefix, use last line
    if not systems_str:
        systems_str = result_str.split('\n')[-1].strip()
    
    # Clean up the output (remove markdown, quotes, etc)
    systems_str = re.sub(r'[*`"\']', '', systems_str)
    
    # Validate and parse system names
    valid_systems = {
        "enlistment", "promotion", "ranks", "reputation", "morale", 
        "supply", "orders", "orchestrator", "companyneeds", "combat",
        "equipment", "conditions", "escalation", "readiness", "rest",
        "identity", "retinue", "camp", "interface"
    }
    
    parsed_systems = []
    for s in systems_str.split(','):
        s = s.strip()
        if s.lower() in valid_systems:
            # Capitalize properly
            parsed_systems.append(s.capitalize())
        elif s:  # Non-empty but unknown
            print(f"  [WARN] Unknown system '{s}', skipping")
    
    if not parsed_systems:
        print("Could not identify systems from query. Using default gameplay flow.")
        parsed_systems = ["Enlistment", "Orders", "Promotion", "Reputation"]
    
    print(f"\nIdentified systems: {', '.join(parsed_systems)}")
    print("\nRunning analysis...\n")
    
    # Map focus string to enum
    focus_map = {
        "integration": AnalysisFocus.INTEGRATION,
        "efficiency": AnalysisFocus.EFFICIENCY,
        "both": AnalysisFocus.BOTH,
    }
    
    # Generate output path from query if not provided
    if not output_path:
        safe_query = re.sub(r'[^a-z0-9]+', '-', query.lower())[:40].strip('-')
        output_path = f"docs/CrewAI_Plans/{safe_query}-analysis.md"
    
    # Run SystemAnalysisFlow with identified systems
    flow = SystemAnalysisFlow()
    result = flow.kickoff(inputs={
        "system_names": parsed_systems,
        "focus": focus_map[focus],
        "is_subsystem": False,
        "output_path": output_path,
    })
    
    # Print cost and safety summaries
    print_all_summaries()
    
    # Extract report from flow state
    if hasattr(result, 'final_report'):
        report = result.final_report
    else:
        report = str(result)
    
    if hasattr(result, 'analysis_doc_path'):
        print(f"\n✅ Analysis complete: {result.analysis_doc_path}")
    
    return report


def run_analysis(
    systems: str = None,
    analyze_all: bool = False,
    focus: str = "both",
    subsystem: bool = False,
    output_path: str = None,
):
    """
    SYSTEM ANALYSIS WORKFLOW - Automated integration and efficiency analysis.
    
    Analyzes game systems to find:
    - Integration gaps (missing connections, unused values)
    - Efficiency issues (performance, code smells)
    - Prioritized improvement recommendations
    """
    from .flows import SystemAnalysisFlow, AnalysisFocus
    
    SearchCache.clear()
    reset_all_hooks()
    
    # Validate input
    if not systems and not analyze_all:
        print("Error: Must specify either system names or --all")
        print("Example: enlisted-crew analyze-system \"Supply,Morale\"")
        sys.exit(1)
    
    # Parse system names
    if analyze_all:
        system_names = [
            "Supply", "Morale", "Reputation", "Escalation",
            "Readiness", "Rest", "ContentOrchestrator", "CompanyNeeds"
        ]
    else:
        system_names = [s.strip() for s in systems.split(",")]
    
    print("\n" + "=" * 60)
    print("SYSTEM ANALYSIS WORKFLOW")
    print("=" * 60)
    print(f"\nSystems: {', '.join(system_names)}")
    print(f"Focus: {focus}")
    print(f"Subsystem: {subsystem}")
    print("\nAnalyzing systems...\n")
    
    # Map focus string to enum
    focus_map = {
        "integration": AnalysisFocus.INTEGRATION,
        "efficiency": AnalysisFocus.EFFICIENCY,
        "both": AnalysisFocus.BOTH,
    }
    
    # Run SystemAnalysisFlow
    flow = SystemAnalysisFlow()
    result = flow.kickoff(inputs={
        "system_names": system_names,
        "focus": focus_map[focus],
        "is_subsystem": subsystem,
        "output_path": output_path or "",
    })
    
    # Print cost and safety summaries
    print_all_summaries()
    
    # Extract report from flow state
    if hasattr(result, 'final_report'):
        report = result.final_report
    else:
        report = str(result)
    
    # Report is already saved by the flow, just return content
    if hasattr(result, 'analysis_doc_path'):
        print(f"\n✅ Analysis complete: {result.analysis_doc_path}")
    
    return report


if __name__ == "__main__":
    main()
