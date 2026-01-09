"""
Enlisted CrewAI Custom Tools

Tools that extend CrewAI agent capabilities for Enlisted mod development.
Natural naming convention for better readability.
"""

from .validation_tools import (
    validate_content,
    sync_strings,
    build,
    analyze_issues,
)
from .style_tools import (
    get_style_guide,
)
from .schema_tools import (
    check_event_format,
    draft_event,
    read_event,
    list_events,
)
from .code_style_tools import (
    review_code,
    check_game_patterns,
    check_compatibility,
    review_source_file,
)
from .docs_tools import (
    SearchCache,
    read_doc_tool,
    list_docs_tool,
    search_docs_semantic,
    read_source,
    read_source_section,
    list_feature_files_tool,
    read_debug_logs_tool,
    search_debug_logs_tool,
    read_native_crash_logs_tool,
    get_game_systems,
    verify_file_exists_tool,
    list_event_ids,
)
from .planning_tools import (
    load_plan,
    parse_plan,
    get_plan_hash,
)
from .file_tools import (
    write_source,
    write_event,
    write_doc,
    update_localization,
    append_to_csproj,
)
from .database_tools import (
    # Query tools
    lookup_content_id,
    lookup_content_ids_batch,  # Batch version for efficiency
    search_content,
    get_balance_value,
    search_balance,
    lookup_error_code,
    get_tier_info,
    get_system_dependencies,
    lookup_api_pattern,
    get_valid_categories,
    get_valid_severities,
    lookup_core_system,
    get_all_tiers,
    get_balance_by_category,
    # Maintenance tools
    add_content_item,
    update_content_item,
    delete_content_item,
    update_balance_value,
    add_balance_value,
    add_error_code,
    add_system_dependency,
    record_implementation,
    sync_content_from_files,
    check_database_health,
)

__all__ = [
    # Validation
    "validate_content",
    "sync_strings",
    "build",
    "analyze_issues",
    
    # Style Review
    "get_style_guide",
    
    # Event Schema
    "check_event_format",
    "draft_event",
    "read_event",
    "list_events",
    
    # Code Review
    "review_code",
    "check_game_patterns",
    "check_compatibility",
    "review_source_file",
    
    # Documentation
    "read_doc_tool",
    "list_docs_tool",
    "search_docs_semantic",
    "SearchCache",
    
    # Source Code
    "read_source",
    "read_source_section",
    "list_feature_files_tool",
    
    # Debug & Native API
    "read_debug_logs_tool",
    "search_debug_logs_tool",
    "read_native_crash_logs_tool",
    
    # Context Loaders (kept: get_game_systems still used)
    "get_game_systems",
    
    # Verification
    "verify_file_exists_tool",
    "list_event_ids",
    
    # Planning
    "load_plan",
    "parse_plan",
    "get_plan_hash",
    
    # File Writing
    "write_source",
    "write_event",
    "write_doc",
    "update_localization",
    "append_to_csproj",
    
    # Database Query
    "lookup_content_id",
    "lookup_content_ids_batch",  # Batch version
    "search_content",
    "get_balance_value",
    "search_balance",
    "lookup_error_code",
    "get_tier_info",
    "get_system_dependencies",
    "lookup_api_pattern",
    "get_valid_categories",
    "get_valid_severities",
    "lookup_core_system",
    "get_all_tiers",
    "get_balance_by_category",
    
    # Database Maintenance
    "add_content_item",
    "update_content_item",
    "delete_content_item",
    "update_balance_value",
    "add_balance_value",
    "add_error_code",
    "add_system_dependency",
    "record_implementation",
    "sync_content_from_files",
    "check_database_health",
]
