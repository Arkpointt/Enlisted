#!/usr/bin/env python
"""
Enlisted CrewAI CLI

Two core workflows for Enlisted mod development:

    enlisted-crew hunt-bug -d "bug" -e "E-XXX-*"        # Find & fix bugs
    enlisted-crew implement -p "plan.md"                # Build from plan

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
