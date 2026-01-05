"""
Validation Tools for Enlisted CrewAI

Wrappers around existing validation scripts in Tools/Validation/
"""

import subprocess
import os
from pathlib import Path
from crewai.tools import tool


def get_project_root() -> Path:
    """Get the Enlisted project root directory."""
    # Check environment variable first
    env_root = os.environ.get("ENLISTED_PROJECT_ROOT")
    if env_root:
        return Path(env_root)
    
    # Auto-detect: walk up from this file to find Enlisted.csproj
    current = Path(__file__).resolve()
    for parent in current.parents:
        if (parent / "Enlisted.csproj").exists():
            return parent
    
    # Fallback to common location
    return Path(r"C:\Dev\Enlisted\Enlisted")


PROJECT_ROOT = get_project_root()


@tool("Validate Content")
def validate_content() -> str:
    """
    Run the Enlisted content validator (validate_content.py).
    
    Validates all JSON events, decisions, orders, and project structure.
    Returns a report with errors, warnings, and info messages.
    
    Use this to check if content follows schema rules before committing.
    """
    validator_path = PROJECT_ROOT / "Tools" / "Validation" / "validate_content.py"
    
    if not validator_path.exists():
        return f"ERROR: Validator not found at {validator_path}"
    
    try:
        result = subprocess.run(
            ["python", str(validator_path)],
            capture_output=True,
            text=True,
            cwd=str(PROJECT_ROOT),
            timeout=120  # 2 minute timeout
        )
        
        output = result.stdout
        if result.stderr:
            output += f"\n\nSTDERR:\n{result.stderr}"
        
        # Add exit code info
        if result.returncode != 0:
            output += f"\n\nValidator exited with code {result.returncode}"
        
        return output
        
    except subprocess.TimeoutExpired:
        return "ERROR: Validation timed out after 2 minutes"
    except Exception as e:
        return f"ERROR: Failed to run validator: {e}"


@tool("Sync Strings")
def sync_strings(check_only: bool = True) -> str:
    """
    Sync JSON string IDs to XML localization file.
    
    Args:
        check_only: If True, only check for missing strings without modifying files.
                   If False, add missing strings to enlisted_strings.xml.
    
    Returns a report of missing or added strings.
    """
    sync_script = PROJECT_ROOT / "Tools" / "Validation" / "sync_event_strings.py"
    
    if not sync_script.exists():
        return f"ERROR: Sync script not found at {sync_script}"
    
    try:
        args = ["python", str(sync_script)]
        if check_only:
            args.append("--check")
        
        result = subprocess.run(
            args,
            capture_output=True,
            text=True,
            cwd=str(PROJECT_ROOT),
            timeout=60
        )
        
        output = result.stdout
        if result.stderr:
            output += f"\n\nSTDERR:\n{result.stderr}"
        
        return output
        
    except subprocess.TimeoutExpired:
        return "ERROR: Sync timed out after 1 minute"
    except Exception as e:
        return f"ERROR: Failed to run sync: {e}"


@tool("Build")
def build() -> str:
    """
    Build the Enlisted mod DLL using dotnet.
    
    Runs: dotnet build -c "Enlisted RETAIL" /p:Platform=x64
    
    Returns build output with any errors or warnings.
    """
    try:
        result = subprocess.run(
            ["dotnet", "build", "-c", "Enlisted RETAIL", "/p:Platform=x64"],
            capture_output=True,
            text=True,
            cwd=str(PROJECT_ROOT),
            timeout=300  # 5 minute timeout for build
        )
        
        output = result.stdout
        if result.stderr:
            output += f"\n\nSTDERR:\n{result.stderr}"
        
        if result.returncode == 0:
            output = "BUILD SUCCEEDED\n\n" + output
        else:
            output = f"BUILD FAILED (exit code {result.returncode})\n\n" + output
        
        return output
        
    except subprocess.TimeoutExpired:
        return "ERROR: Build timed out after 5 minutes"
    except FileNotFoundError:
        return "ERROR: dotnet not found. Make sure .NET SDK is installed and in PATH."
    except Exception as e:
        return f"ERROR: Failed to run build: {e}"


@tool("Analyze Issues")
def analyze_issues() -> str:
    """
    Run the validation analyzer to get a prioritized summary of issues.
    
    This produces a categorized list of issues by priority:
    - CRITICAL: Must fix immediately
    - HIGH: Fix before committing
    - MEDIUM: Fix when convenient
    - LOW: Fix as content completes
    """
    analyzer_path = PROJECT_ROOT / "Tools" / "Validation" / "analyze_validation.py"
    report_path = PROJECT_ROOT / "Tools" / "Debugging" / "validation_report.txt"
    
    # First run validation to generate report
    validator_path = PROJECT_ROOT / "Tools" / "Validation" / "validate_content.py"
    
    try:
        # Generate fresh report
        with open(report_path, "w") as f:
            result = subprocess.run(
                ["python", str(validator_path)],
                stdout=f,
                stderr=subprocess.STDOUT,
                cwd=str(PROJECT_ROOT),
                timeout=120
            )
        
        # Run analyzer
        if analyzer_path.exists():
            result = subprocess.run(
                ["python", str(analyzer_path)],
                capture_output=True,
                text=True,
                cwd=str(PROJECT_ROOT),
                timeout=30
            )
            return result.stdout + (result.stderr if result.stderr else "")
        else:
            # If analyzer doesn't exist, return raw report summary
            with open(report_path) as f:
                content = f.read()
            # Extract just summary lines
            lines = content.split("\n")
            summary_lines = [l for l in lines if "[ERROR]" in l or "[WARNING]" in l][:50]
            return "\n".join(summary_lines)
            
    except Exception as e:
        return f"ERROR: Failed to analyze: {e}"
