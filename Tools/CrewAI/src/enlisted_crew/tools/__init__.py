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
    review_prose,
    review_tooltip,
    suggest_edits,
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
    find_in_docs,
    read_source,
    find_in_code,
    read_source_section,
    list_feature_files_tool,
    read_debug_logs_tool,
    search_debug_logs_tool,
    read_native_crash_logs_tool,
    find_in_native_api,
    get_writing_guide,
    get_architecture,
    get_dev_reference,
    get_game_systems,
    verify_file_exists_tool,
    list_event_ids,
)
from .planning_tools import (
    save_plan,
    load_plan,
)
from .file_tools import (
    write_source,
    write_event,
    update_localization,
    append_to_csproj,
)

__all__ = [
    # Validation
    "validate_content",
    "sync_strings",
    "build",
    "analyze_issues",
    
    # Style Review
    "review_prose",
    "review_tooltip",
    "suggest_edits",
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
    "find_in_docs",
    "SearchCache",
    
    # Source Code
    "read_source",
    "find_in_code",
    "read_source_section",
    "list_feature_files_tool",
    
    # Debug & Native API
    "read_debug_logs_tool",
    "search_debug_logs_tool",
    "read_native_crash_logs_tool",
    "find_in_native_api",
    
    # Context Loaders
    "get_writing_guide",
    "get_architecture",
    "get_dev_reference",
    "get_game_systems",
    
    # Verification
    "verify_file_exists_tool",
    "list_event_ids",
    
    # Planning
    "save_plan",
    "load_plan",
    
    # File Writing
    "write_source",
    "write_event",
    "update_localization",
    "append_to_csproj",
]
