"""
Execution hooks for Enlisted CrewAI workflows.

Provides cost tracking, safety guards, and validation for LLM and tool calls.
"""

import sqlite3
from datetime import datetime
from pathlib import Path
from typing import Optional

from crewai.hooks import (
    before_llm_call,
    after_llm_call,
    before_tool_call,
    after_tool_call,
)


# =============================================================================
# COST TRACKING HOOKS
# =============================================================================

# Token cost estimates (per 1M tokens) - GPT-5 pricing
TOKEN_COSTS = {
    "gpt-5.2": {"input": 2.50, "output": 10.00},
    "gpt-5": {"input": 2.00, "output": 8.00},
    "gpt-5.2": {"input": 2.50, "output": 10.00},  # Instant mode ~$0.50/$2.00, Thinking mode $2.50/$10.00
    "gpt-5-mini": {"input": 0.10, "output": 0.40},  # Legacy reference
    "gpt-5-nano": {"input": 0.05, "output": 0.20},  # Legacy reference
    "gpt-4o-mini": {"input": 0.15, "output": 0.60},  # Fallback/default
}

# Global cost tracking
_CURRENT_RUN_COSTS = {
    "total_input_tokens": 0,
    "total_output_tokens": 0,
    "total_cost_usd": 0.0,
    "llm_calls": 0,
}


def _get_db_path() -> str:
    """Get path to monitoring database."""
    project_root = Path(__file__).parent.parent.parent.parent.parent
    return str(project_root / "Tools" / "CrewAI" / "enlisted_knowledge.db")


def _estimate_cost(model: str, input_tokens: int, output_tokens: int) -> float:
    """
    Estimate cost for an LLM call.
    
    Args:
        model: Model name (e.g., "gpt-5.2" with auto mode-switching)
        input_tokens: Number of input tokens
        output_tokens: Number of output tokens
    
    Returns:
        Cost in USD
    """
    # Find matching cost table
    costs = None
    for model_key, model_costs in TOKEN_COSTS.items():
        if model_key in model.lower():
            costs = model_costs
            break
    
    if costs is None:
        # Default to gpt-4o-mini pricing as conservative estimate
        costs = TOKEN_COSTS["gpt-4o-mini"]
    
    input_cost = (input_tokens / 1_000_000) * costs["input"]
    output_cost = (output_tokens / 1_000_000) * costs["output"]
    
    return input_cost + output_cost


@after_llm_call
def track_llm_costs(context):
    """
    Track token usage and costs for every LLM call.
    
    Logs to database and updates running total for current execution.
    """
    # Extract token usage if available
    usage = getattr(context, "usage", None)
    if not usage:
        return None  # No modification to response
    
    input_tokens = getattr(usage, "prompt_tokens", 0)
    output_tokens = getattr(usage, "completion_tokens", 0)
    
    if input_tokens == 0 and output_tokens == 0:
        return None
    
    # Get model name
    model = getattr(context, "model", "unknown")
    
    # Calculate cost
    cost = _estimate_cost(model, input_tokens, output_tokens)
    
    # Update running totals
    _CURRENT_RUN_COSTS["total_input_tokens"] += input_tokens
    _CURRENT_RUN_COSTS["total_output_tokens"] += output_tokens
    _CURRENT_RUN_COSTS["total_cost_usd"] += cost
    _CURRENT_RUN_COSTS["llm_calls"] += 1
    
    # Log to console
    print(f"      [COST] {model}: {input_tokens} in + {output_tokens} out = ${cost:.4f}")
    
    # Log to database
    try:
        db_path = _get_db_path()
        with sqlite3.connect(db_path) as conn:
            cursor = conn.cursor()
            
            # Ensure table exists
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS llm_costs (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    timestamp TEXT NOT NULL,
                    model TEXT NOT NULL,
                    input_tokens INTEGER NOT NULL,
                    output_tokens INTEGER NOT NULL,
                    cost_usd REAL NOT NULL
                )
            """)
            
            cursor.execute("""
                INSERT INTO llm_costs (timestamp, model, input_tokens, output_tokens, cost_usd)
                VALUES (?, ?, ?, ?, ?)
            """, (datetime.now().isoformat(), model, input_tokens, output_tokens, cost))
            
            conn.commit()
    except Exception as e:
        # Don't fail the execution if logging fails
        print(f"      [WARNING] Failed to log cost to database: {e}")
    
    return None  # Don't modify the response


def get_current_run_costs() -> dict:
    """
    Get cost statistics for the current run.
    
    Returns:
        Dictionary with total_input_tokens, total_output_tokens, total_cost_usd, llm_calls
    """
    return _CURRENT_RUN_COSTS.copy()


def reset_run_costs():
    """Reset cost tracking for a new run."""
    _CURRENT_RUN_COSTS["total_input_tokens"] = 0
    _CURRENT_RUN_COSTS["total_output_tokens"] = 0
    _CURRENT_RUN_COSTS["total_cost_usd"] = 0.0
    _CURRENT_RUN_COSTS["llm_calls"] = 0


def print_run_cost_summary():
    """Print a formatted summary of costs for the current run."""
    print("\n" + "=" * 70)
    print("RUN COST SUMMARY")
    print("=" * 70)
    print(f"Total LLM Calls: {_CURRENT_RUN_COSTS['llm_calls']}")
    print(f"Input Tokens: {_CURRENT_RUN_COSTS['total_input_tokens']:,}")
    print(f"Output Tokens: {_CURRENT_RUN_COSTS['total_output_tokens']:,}")
    print(f"Total Cost: ${_CURRENT_RUN_COSTS['total_cost_usd']:.2f}")
    print("=" * 70 + "\n")


# =============================================================================
# SAFETY GUARD HOOKS
# =============================================================================

# Dangerous operations that require validation
DANGEROUS_TOOLS = {
    "write_source",
    "write_event",
    "write_doc",
    "update_localization",
    "append_to_csproj",
    "delete_content_item",
    "add_content_item",
    "update_content_item",
}

# Track denied operations
_DENIED_OPERATIONS = []


@before_tool_call
def validate_tool_safety(context):
    """
    Validate tool calls before execution.
    
    Checks:
    1. File write operations target valid project directories
    2. Database operations have valid parameters
    3. No attempts to write outside project boundaries
    
    Returns False to block execution, None to allow.
    """
    tool_name = getattr(context, "tool_name", "")
    
    # Only validate dangerous tools
    if tool_name not in DANGEROUS_TOOLS:
        return None  # Allow execution
    
    # Get tool arguments
    args = getattr(context, "arguments", {})
    
    # Validate file write operations
    if tool_name in ["write_source", "write_event", "write_doc", "update_localization", "append_to_csproj"]:
        file_path = args.get("file_path") or args.get("path") or args.get("doc_path")
        
        if file_path:
            path = Path(file_path)
            
            # Block absolute paths outside project
            if path.is_absolute():
                project_root = Path(__file__).parent.parent.parent.parent.parent
                try:
                    path.relative_to(project_root)
                except ValueError:
                    print(f"      [BLOCKED] {tool_name} - Path outside project: {file_path}")
                    _DENIED_OPERATIONS.append({
                        "tool": tool_name,
                        "reason": "Path outside project boundaries",
                        "path": str(file_path),
                        "timestamp": datetime.now().isoformat(),
                    })
                    return False  # Block execution
            
            # Block suspicious patterns
            suspicious = ["../../../", "C:\\Windows", "C:\\Program Files", "/etc/", "/usr/", "~/.ssh"]
            if any(pattern in str(file_path) for pattern in suspicious):
                print(f"      [BLOCKED] {tool_name} - Suspicious path pattern: {file_path}")
                _DENIED_OPERATIONS.append({
                    "tool": tool_name,
                    "reason": "Suspicious path pattern",
                    "path": str(file_path),
                    "timestamp": datetime.now().isoformat(),
                })
                return False  # Block execution
    
    # Validate database operations
    if tool_name in ["delete_content_item", "add_content_item", "update_content_item"]:
        content_id = args.get("content_id")
        
        if not content_id:
            print(f"      [BLOCKED] {tool_name} - Missing content_id")
            _DENIED_OPERATIONS.append({
                "tool": tool_name,
                "reason": "Missing required parameter: content_id",
                "timestamp": datetime.now().isoformat(),
            })
            return False  # Block execution
        
        # Block suspicious content IDs
        if any(char in content_id for char in ["'", '"', ";", "--", "DROP", "DELETE"]):
            print(f"      [BLOCKED] {tool_name} - Suspicious content_id: {content_id}")
            _DENIED_OPERATIONS.append({
                "tool": tool_name,
                "reason": "Suspicious content_id (potential SQL injection)",
                "content_id": content_id,
                "timestamp": datetime.now().isoformat(),
            })
            return False  # Block execution
    
    return None  # Allow execution


@after_tool_call
def log_tool_execution(context):
    """
    Log tool execution results.
    
    Tracks success/failure and logs errors for debugging.
    """
    tool_name = getattr(context, "tool_name", "")
    success = not hasattr(context, "error")
    
    if not success:
        error = getattr(context, "error", "Unknown error")
        print(f"      [TOOL ERROR] {tool_name}: {error}")
    
    return None  # Don't modify the result


def get_denied_operations() -> list:
    """
    Get list of operations that were blocked by safety guards.
    
    Returns:
        List of dictionaries with tool, reason, timestamp, and other details
    """
    return _DENIED_OPERATIONS.copy()


def reset_denied_operations():
    """Reset the list of denied operations."""
    _DENIED_OPERATIONS.clear()


def print_safety_summary():
    """Print a summary of safety guard actions."""
    if not _DENIED_OPERATIONS:
        return
    
    print("\n" + "=" * 70)
    print("SAFETY GUARD SUMMARY")
    print("=" * 70)
    print(f"Blocked Operations: {len(_DENIED_OPERATIONS)}")
    for op in _DENIED_OPERATIONS:
        print(f"  - {op['tool']}: {op['reason']}")
    print("=" * 70 + "\n")


# =============================================================================
# INITIALIZATION
# =============================================================================

def reset_all_hooks():
    """Reset all hook tracking (call at start of each workflow run)."""
    reset_run_costs()
    reset_denied_operations()


def print_all_summaries():
    """Print all hook summaries (call at end of each workflow run)."""
    print_run_cost_summary()
    print_safety_summary()
