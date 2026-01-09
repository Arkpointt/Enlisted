"""
Semantic codebase search using RAG.

Provides fast vector-based search over src/ and Decompile/ codebases,
replacing slow grep-based searches with semantic retrieval.
"""

from .codebase_rag_tool import search_codebase
from .codebase_indexer import index_codebase, get_index_stats

__all__ = [
    'search_codebase',
    'index_codebase',
    'get_index_stats',
]
