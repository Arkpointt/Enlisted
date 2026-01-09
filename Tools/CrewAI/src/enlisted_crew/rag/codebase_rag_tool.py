"""
Semantic codebase search tool for CrewAI agents.

Provides fast vector-based search over indexed C# codebase (src/ + Decompile/).
"""

import os
from pathlib import Path
from typing import Optional

from crewai.tools import tool
from langchain_chroma import Chroma
from langchain_openai import OpenAIEmbeddings


def _get_project_root() -> Path:
    """Get the Enlisted project root directory."""
    env_root = os.environ.get("ENLISTED_PROJECT_ROOT")
    if env_root:
        return Path(env_root)
    
    current = Path(__file__).resolve()
    for parent in current.parents:
        if (parent / "Enlisted.csproj").exists():
            return parent
    
    return Path(r"C:\Dev\Enlisted\Enlisted")


def _get_vectorstore() -> Chroma:
    """Get or create the codebase vector store."""
    project_root = _get_project_root()
    persist_dir = project_root / "Tools" / "CrewAI" / "src" / "enlisted_crew" / "rag" / "vector_db"
    
    if not persist_dir.exists():
        raise FileNotFoundError(
            f"Codebase index not found at {persist_dir}. "
            "Run: python -m enlisted_crew.rag.codebase_indexer --index-all"
        )
    
    # Reuse embeddings from memory_config.py
    embeddings = OpenAIEmbeddings(model="text-embedding-3-large")
    
    return Chroma(
        persist_directory=str(persist_dir),
        embedding_function=embeddings
    )


@tool("search_codebase")
def search_codebase(query: str, filter_path: str = "") -> str:
    """Semantic search of src/ and Decompile/ for relevant C# code.
    
    This tool performs fast vector-based semantic search over the indexed codebase,
    replacing slow grep searches with intelligent retrieval.
    
    Args:
        query: Natural language or code pattern to search for.
               Examples: "morale calculation", "hero null check pattern", "campaign behavior registration"
        filter_path: Optional path filter to narrow search scope.
                    Examples: "src/Features/", "Decompile/TaleWorlds.CampaignSystem/"
    
    Returns:
        Top 5 relevant code examples with file paths and line numbers.
        Each result includes the source file, line range, and relevant code snippet.
    
    Example:
        >>> search_codebase("morale calculation")
        File: src/Features/Morale/MoraleCalculator.cs
        Lines 45-67:
        public float CalculateMorale(Hero hero)
        {
            // Morale calculation implementation
            ...
        }
    """
    try:
        vectorstore = _get_vectorstore()
        
        # Perform similarity search
        # If filtering by path, retrieve more results to ensure we get 5 matches after filtering
        k = 50 if filter_path else 5
        retriever = vectorstore.as_retriever(
            search_type="similarity",
            search_kwargs={"k": k}
        )
        
        docs = retriever.invoke(query)
        
        # Filter by path after retrieval if specified
        # This is necessary because ChromaDB doesn't support substring matching on metadata
        if filter_path and docs:
            docs = [doc for doc in docs if filter_path in doc.metadata.get('source', '')]
            # Limit to top 5 after filtering
            docs = docs[:5]
        
        if not docs:
            return f"No results found for query: '{query}'"
        
        # Format results
        results = []
        for i, doc in enumerate(docs, 1):
            metadata = doc.metadata
            source = metadata.get('source', 'Unknown')
            start_line = metadata.get('start_line', '?')
            end_line = metadata.get('end_line', '?')
            
            results.append(f"[{i}] File: {source}")
            results.append(f"    Lines {start_line}-{end_line}:")
            results.append(f"    {doc.page_content[:500]}...")  # First 500 chars
            results.append("")
        
        return "\n".join(results)
    
    except FileNotFoundError as e:
        return f"Error: {e}\n\nPlease run the indexer first:\npython -m enlisted_crew.rag.codebase_indexer --index-all"
    except Exception as e:
        return f"Error searching codebase: {str(e)}"
