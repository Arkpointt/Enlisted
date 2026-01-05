#!/usr/bin/env python
"""
Enlisted CrewAI CLI

Command-line interface for running Enlisted CrewAI workflows.

Usage:
    enlisted-crew validate                    # Run full validation
    enlisted-crew validate-file <path>        # Validate specific file
    enlisted-crew create-event                # Create new event interactively
    enlisted-crew style-review <path>         # Review style of event file
    enlisted-crew code-review <paths...>      # Review C# code changes
    enlisted-crew full-review <paths...>      # Full content review
    enlisted-crew hunt-bug --description "..."  # Investigate a bug (Flow-based)
"""

import argparse
import sys
from pathlib import Path

from dotenv import load_dotenv

from .crew import EnlistedCrew
from .tools import SearchCache


def main():
    """Main CLI entry point."""
    load_dotenv()
    
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
    
    # validate-file command
    validate_file_parser = subparsers.add_parser(
        "validate-file",
        help="Validate a specific event file",
    )
    validate_file_parser.add_argument(
        "file_path",
        help="Path to event JSON file",
    )
    
    # create-event command
    create_parser = subparsers.add_parser(
        "create-event",
        help="Create a new event",
    )
    create_parser.add_argument(
        "--type", "-t",
        dest="event_type",
        default="decision",
        help="Event type (decision, escalation, role, etc.)",
    )
    create_parser.add_argument(
        "--category", "-c",
        default="general",
        help="Event category",
    )
    create_parser.add_argument(
        "--theme",
        required=True,
        help="Event theme/topic description",
    )
    create_parser.add_argument(
        "--tier",
        default="1-9",
        help="Tier range (e.g., '1-3', '4-6', '1-9')",
    )
    
    # style-review command
    style_parser = subparsers.add_parser(
        "style-review",
        help="Review and improve event writing style",
    )
    style_parser.add_argument(
        "file_path",
        help="Path to event JSON file",
    )
    
    # code-review command
    code_parser = subparsers.add_parser(
        "code-review",
        help="Review C# code changes",
    )
    code_parser.add_argument(
        "file_paths",
        nargs="+",
        help="Paths to C# files to review",
    )
    
    # full-review command
    full_parser = subparsers.add_parser(
        "full-review",
        help="Run full content review",
    )
    full_parser.add_argument(
        "file_paths",
        nargs="+",
        help="Paths to files to review",
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
        elif args.command == "validate-file":
            result = run_file_validation(crew, args.file_path)
        elif args.command == "create-event":
            result = run_create_event(
                crew,
                event_type=args.event_type,
                category=args.category,
                theme=args.theme,
                tier_range=args.tier,
            )
        elif args.command == "style-review":
            result = run_style_review(crew, args.file_path)
        elif args.command == "code-review":
            result = run_code_review(crew, args.file_paths)
        elif args.command == "full-review":
            result = run_full_review(crew, args.file_paths)
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
    """Run full validation crew."""
    SearchCache.clear()  # Clear cache for fresh session
    print("Running full validation...")
    validation_crew = crew.validation_crew()
    return validation_crew.kickoff()


def run_file_validation(crew: EnlistedCrew, file_path: str):
    """Run validation on a specific file."""
    SearchCache.clear()  # Clear cache for fresh session
    print(f"Validating: {file_path}")
    
    # Create a one-off crew for file validation
    from crewai import Crew, Process, Task
    
    task = Task(
        description=f"""
        Validate the event file against the Enlisted schema:
        - Check required fields (id, category, title, setup)
        - Verify field ordering (fallback after ID)
        - Validate option count (0 or 2-6, never 1)
        - Check tooltip presence and length (<80 chars)
        - Verify skill names, roles, and contexts
        
        File to validate: {file_path}
        """,
        expected_output="Detailed validation report with issues and fixes",
        agent=crew.content_analyst(),
    )
    
    file_crew = Crew(
        agents=[crew.content_analyst()],
        tasks=[task],
        process=Process.sequential,
        verbose=True,
    )
    
    return file_crew.kickoff()


def run_create_event(
    crew: EnlistedCrew,
    event_type: str,
    category: str,
    theme: str,
    tier_range: str,
):
    """Run event creation crew."""
    SearchCache.clear()  # Clear cache for fresh session
    print(f"Creating {event_type} event: {theme}")
    
    from crewai import Crew, Process, Task
    
    task = Task(
        description=f"""
        Create a new event based on the following brief:
        
        Event Type: {event_type}
        Category: {category}
        Theme: {theme}
        Tier Range: {tier_range}
        
        Requirements:
        - Follow Enlisted writing style (sparse, concrete, implied emotion)
        - Create 2-4 options with clear tradeoffs
        - Include tooltips under 80 characters
        - Use proper field ordering (fallback after ID)
        - Match the specified tier and context
        """,
        expected_output="Complete, valid JSON event file",
        agent=crew.content_author(),
    )
    
    create_crew = Crew(
        agents=[crew.content_author(), crew.content_analyst()],
        tasks=[task],
        process=Process.sequential,
        verbose=True,
    )
    
    return create_crew.kickoff()


def run_style_review(crew: EnlistedCrew, file_path: str):
    """Run style review crew."""
    SearchCache.clear()  # Clear cache for fresh session
    print(f"Reviewing style: {file_path}")
    
    from crewai import Crew, Process, Task
    
    task = Task(
        description=f"""
        Review and improve the writing style of the event file:
        
        Event file: {file_path}
        
        Check for:
        - Forbidden words (steel yourself, amidst, the men, etc.)
        - Purple prose (too many adjectives, flowery language)
        - Weak verbs (is, was, had, seemed)
        - Missing concrete imagery
        - Tooltip length violations
        
        Suggest specific rewrites that maintain meaning while improving style.
        """,
        expected_output="Style review with specific rewrite suggestions",
        agent=crew.content_author(),
    )
    
    style_crew = Crew(
        agents=[crew.content_author()],
        tasks=[task],
        process=Process.sequential,
        verbose=True,
    )
    
    return style_crew.kickoff()


def run_code_review(crew: EnlistedCrew, file_paths: list):
    """Run code review crew."""
    SearchCache.clear()  # Clear cache for fresh session
    paths_str = ", ".join(file_paths)
    print(f"Reviewing code: {paths_str}")
    
    from crewai import Crew, Process, Task
    
    task = Task(
        description=f"""
        Review C# code changes for Enlisted-specific issues:
        
        Focus areas:
        - SaveableTypeDefiner: new saveable classes registered?
        - TextObject: using SetTextVariable, not string concat?
        - Hero safety: null checks before accessing Hero properties?
        - Equipment iteration: using ToList() on equipment collections?
        - Gold: using AddGoldAction, not direct assignment?
        
        Files to review: {paths_str}
        """,
        expected_output="Code review report with issues and recommendations",
        agent=crew.code_analyst(),
    )
    
    code_crew = Crew(
        agents=[crew.code_analyst(), crew.qa_agent()],
        tasks=[task],
        process=Process.sequential,
        verbose=True,
    )
    
    return code_crew.kickoff()


def run_full_review(crew: EnlistedCrew, file_paths: list):
    """Run full review crew."""
    SearchCache.clear()  # Clear cache for fresh session
    paths_str = ", ".join(file_paths)
    print(f"Running full review: {paths_str}")
    
    full_crew = crew.full_review_crew()
    return full_crew.kickoff(inputs={"file_paths": paths_str})


def run_hunt_bug(
    description: str,
    error_codes: str = "none",
    repro_steps: str = "unknown",
    context: str = None,
    output_file: str = None,
    user_logs: str = None,
):
    """
    Run bug hunting flow.
    
    Uses the Flow-based BugHuntingFlow for state-managed bug investigation.
    
    Args:
        user_logs: End-user provided logs. Can be:
            - Direct log content (pasted)
            - Path to a log file
            If not provided, flow will analyze code paths instead of searching logs.
    """
    from .flows import BugHuntingFlow
    from pathlib import Path
    
    # Handle user_logs - could be file path or direct content
    user_log_content = None
    if user_logs:
        log_path = Path(user_logs)
        if log_path.exists() and log_path.is_file():
            print(f"📂 Reading user log file: {user_logs}")
            user_log_content = log_path.read_text(encoding='utf-8', errors='replace')
        else:
            # Treat as direct log content
            user_log_content = user_logs
    
    print("\n" + "=" * 60)
    print("ENLISTED BUG HUNTING FLOW")
    print("=" * 60)
    print(f"\nDescription: {description[:100]}...")
    print(f"Error Codes: {error_codes}")
    print(f"Repro Steps: {repro_steps}")
    print(f"Context: {context or 'None'}")
    print(f"User Logs: {'Provided (' + str(len(user_log_content)) + ' chars)' if user_log_content else 'None - will analyze code paths'}")
    print("\nStarting investigation...\n")
    
    # Create and run the flow
    flow = BugHuntingFlow()
    
    result = flow.kickoff(inputs={
        "bug_report": {
            "description": description,
            "error_codes": error_codes,
            "repro_steps": repro_steps,
            "context": context,
            "user_log_content": user_log_content,
        }
    })
    
    # Get final state
    final_state = result
    
    # Output report
    if hasattr(final_state, 'final_report') and final_state.final_report:
        report = final_state.final_report
    else:
        report = str(result)
    
    if output_file:
        with open(output_file, 'w', encoding='utf-8') as f:
            f.write(report)
        print(f"\n📄 Report saved to: {output_file}")
    
    return report


if __name__ == "__main__":
    main()
