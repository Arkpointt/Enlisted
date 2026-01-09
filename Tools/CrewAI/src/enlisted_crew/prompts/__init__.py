"""
Prompt Templates for CrewAI Workflows

This module contains static prompt templates optimized for OpenAI prompt caching.

Key Principle: Static-first, dynamic-last
- Static content (architecture rules, patterns, workflows) goes FIRST
- Dynamic content (feature names, descriptions) goes LAST
- Templates are 1024+ tokens for automatic caching

All templates are designed to be cached for 24 hours with prompt_cache_retention='24h'.
"""

from .templates import (
    ARCHITECTURE_PATTERNS,
    RESEARCH_WORKFLOW,
    DESIGN_WORKFLOW,
    IMPLEMENTATION_WORKFLOW,
    VALIDATION_WORKFLOW,
    BUG_INVESTIGATION_WORKFLOW,
    CODE_STYLE_RULES,
    TOOL_EFFICIENCY_RULES,
)

__all__ = [
    "ARCHITECTURE_PATTERNS",
    "RESEARCH_WORKFLOW",
    "DESIGN_WORKFLOW",
    "IMPLEMENTATION_WORKFLOW",
    "VALIDATION_WORKFLOW",
    "BUG_INVESTIGATION_WORKFLOW",
    "CODE_STYLE_RULES",
    "TOOL_EFFICIENCY_RULES",
]
