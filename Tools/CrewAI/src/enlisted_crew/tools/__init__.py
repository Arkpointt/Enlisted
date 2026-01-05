"""
Enlisted CrewAI Custom Tools

Tools that extend CrewAI agent capabilities for Enlisted mod development.
"""

from .validation_tools import (
    validate_content_tool,
    sync_localization_tool,
    run_build_tool,
    analyze_validation_report_tool,
)
from .style_tools import (
    check_writing_style_tool,
    check_tooltip_style_tool,
    suggest_style_improvements_tool,
    read_writing_style_guide_tool,
)
from .schema_tools import (
    validate_event_schema_tool,
    create_event_json_tool,
    read_event_file_tool,
    list_event_files_tool,
)
from .code_style_tools import (
    check_code_style_tool,
    check_bannerlord_patterns_tool,
    check_framework_compatibility_tool,
    check_csharp_file_tool,
)
from .docs_tools import (
    SearchCache,
    read_doc_tool,
    list_docs_tool,
    search_docs_tool,
    read_csharp_tool,
    search_csharp_tool,
    read_csharp_snippet_tool,
    list_feature_files_tool,
    read_debug_logs_tool,
    search_debug_logs_tool,
    read_native_crash_logs_tool,
    search_native_api_tool,
    load_content_context_tool,
    load_feature_context_tool,
    load_code_context_tool,
    load_domain_context_tool,
    verify_file_exists_tool,
    list_json_event_ids_tool,
)
from .planning_tools import (
    write_planning_doc_tool,
    read_planning_doc_tool,
)

__all__ = [
    # Validation tools
    "validate_content_tool",
    "sync_localization_tool", 
    "run_build_tool",
    "analyze_validation_report_tool",
    # Style tools
    "check_writing_style_tool",
    "check_tooltip_style_tool",
    "suggest_style_improvements_tool",
    "read_writing_style_guide_tool",
    # Schema tools
    "validate_event_schema_tool",
    "create_event_json_tool",
    "read_event_file_tool",
    "list_event_files_tool",
    # Code style tools
    "check_code_style_tool",
    "check_bannerlord_patterns_tool",
    "check_framework_compatibility_tool",
    "check_csharp_file_tool",
    # Documentation tools
    "read_doc_tool",
    "list_docs_tool",
    "search_docs_tool",
    "SearchCache",
    "read_csharp_tool",
    "search_csharp_tool",
    "read_csharp_snippet_tool",
    "list_feature_files_tool",
    # Debug and native API tools
    "read_debug_logs_tool",
    "search_debug_logs_tool",
    "read_native_crash_logs_tool",
    "search_native_api_tool",
    # Context loader tools
    "load_content_context_tool",
    "load_feature_context_tool",
    "load_code_context_tool",
    "load_domain_context_tool",
    # Verification tools
    "verify_file_exists_tool",
    "list_json_event_ids_tool",
    # Planning tools
    "write_planning_doc_tool",
    "read_planning_doc_tool",
]
