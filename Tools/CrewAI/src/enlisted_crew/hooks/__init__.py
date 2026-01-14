"""
Hooks and utilities for CrewAI workflows.
"""

from .pydantic_output_fixer import (
    with_pydantic_retry,
    create_pydantic_safe_task,
    detect_pydantic_failure,
)

# Legacy hooks from hooks_legacy.py
from ..hooks_legacy import (
    reset_all_hooks,
    print_all_summaries,
    get_current_run_costs,
    reset_run_costs,
    print_run_cost_summary,
    set_tool_budget,
    reset_tool_budget,
    disable_tool_budget,
    get_tool_budget_status,
    get_denied_operations,
    reset_denied_operations,
    print_safety_summary,
)

__all__ = [
    # Pydantic output fixer
    "with_pydantic_retry",
    "create_pydantic_safe_task", 
    "detect_pydantic_failure",
    # Legacy hooks
    "reset_all_hooks",
    "print_all_summaries",
    "get_current_run_costs",
    "reset_run_costs",
    "print_run_cost_summary",
    "set_tool_budget",
    "reset_tool_budget",
    "disable_tool_budget",
    "get_tool_budget_status",
    "get_denied_operations",
    "reset_denied_operations",
    "print_safety_summary",
]
