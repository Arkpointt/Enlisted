#!/usr/bin/env python
"""
Enlisted CrewAI CLI

Three core workflows for Enlisted mod development:

    enlisted-crew plan -f "feature" -d "description"    # Design a feature
    enlisted-crew hunt-bug -d "bug" -e "E-XXX-*"        # Find & fix bugs
    enlisted-crew implement -p "plan.md"                # Build from plan

Utility:
    enlisted-crew validate                              # Pre-commit check
    enlisted-crew stats [-c crew_name]                  # View execution metrics
"""

import argparse
import sys
from pathlib import Path

from dotenv import load_dotenv

from .crew import EnlistedCrew
from .tools import SearchCache
from .monitoring import enable_monitoring, print_execution_report, print_cost_report
from .hooks import reset_all_hooks, print_all_summaries
from .hooks import print_cost_report  # Import to register hooks


def main():
    """Main CLI entry point."""
    load_dotenv()
    
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
    
    # plan command - DESIGN A FEATURE
    plan_parser = subparsers.add_parser(
        "plan",
        help="Create a planning document for a proposed feature",
    )
    plan_parser.add_argument(
        "--feature", "-f",
        required=True,
        help="Feature name (kebab-case like 'reputation-integration')",
    )
    plan_parser.add_argument(
        "--description", "-d",
        required=True,
        help="Brief description of what the feature does",
    )
    plan_parser.add_argument(
        "--systems", "-s",
        default="",
        help="Related systems (e.g. 'EscalationManager, ContentOrchestrator')",
    )
    plan_parser.add_argument(
        "--docs", "-D",
        default="",
        help="Related documentation files",
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
        elif args.command == "plan":
            result = run_plan(
                crew,
                feature_name=args.feature,
                description=args.description,
                systems=args.systems,
                docs=args.docs,
            )
        elif args.command == "hunt-bug":
            result = run_hunt_bug(
                description=args.description,
                error_codes=args.error_codes,
                repro_steps=args.repro_steps,
                context=args.context,
                output_file=args.output,
                user_logs=args.logs,
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
    """Run quick validation check."""
    SearchCache.clear()
    print("\n" + "=" * 60)
    print("VALIDATION CHECK")
    print("=" * 60 + "\n")
    
    validation_crew = crew.validation_crew()
    return validation_crew.kickoff()


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


def run_code_review(crew: EnlistedCrew, file_paths: list):
    """Legacy - kept for backwards compatibility."""
    SearchCache.clear()
    paths_str = ", ".join(file_paths)
    print(f"Reviewing code: {paths_str}")
    
    from crewai import Crew, Process, Task
    
    task = Task(
        description=f"Review C# code files: {paths_str}",
        expected_output="Code review report",
        agent=crew.code_analyst(),
    )
    
    code_crew = Crew(
        agents=[crew.code_analyst(), crew.qa_agent()],
        tasks=[task],
        process=Process.sequential,
        verbose=True,
    )
    
    return code_crew.kickoff()


def run_plan(
    crew: EnlistedCrew,
    feature_name: str,
    description: str,
    systems: str = "",
    docs: str = "",
):
    """
    PLANNING WORKFLOW - Design a feature completely.
    
    Uses PlanningFlow which:
    1. Researches existing systems
    2. Gets architectural advice
    3. Designs technical specification
    4. Writes planning document
    5. Validates for hallucinations (with auto-fix)
    
    State persistence enabled - can resume on failure.
    """
    from .flows import PlanningFlow
    
    SearchCache.clear()
    reset_all_hooks()  # Reset cost tracking and safety guards
    print("\n" + "=" * 60)
    print("PLANNING WORKFLOW (Flow-based)")
    print("=" * 60)
    print(f"\nFeature: {feature_name}")
    print(f"Description: {description}")
    print("\nThis workflow will:")
    print("  1. Research existing systems")
    print("  2. Get architectural advice")
    print("  3. Design technical specification")
    print("  4. Write planning document")
    print("  5. Validate (auto-fix hallucinations)\n")
    
    flow = PlanningFlow()
    result = flow.kickoff(inputs={
        "feature_name": feature_name,
        "description": description,
        "related_systems": systems or "",
        "related_docs": docs or "",
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
    
    Chain: investigate → analyze → fix → validate
    """
    from .crew import EnlistedCrew
    from pathlib import Path
    
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
    
    if output_file:
        with open(output_file, 'w', encoding='utf-8') as f:
            f.write(report)
        print(f"\nReport saved to: {output_file}")
    
    return report


if __name__ == "__main__":
    main()
