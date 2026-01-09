"""Enlisted CrewAI Memory Configuration - Simplified for CrewAI 1.8.0+

Provides memory configuration that works reliably with modern CrewAI versions.

Previous Issues (Fixed):
- Custom ContextualRAGStorage was incompatible with CrewAI 1.8.0 RAGStorage API
- Complex hybrid search pipeline (BM25+Vector+Cohere reranking) caused hangs
- No timeouts on external API calls

Current Solution:
- Use CrewAI's built-in memory with simple embedder config
- Disable custom storage classes that are incompatible
- Keep chunking logic for reference but don't use custom storage

Usage:
    from enlisted_crew.memory_config import get_memory_config
    
    crew = Crew(
        agents=[...],
        tasks=[...],
        **get_memory_config(),  # Unpacks all memory settings
    )

References:
- Anthropic Contextual Retrieval: https://www.anthropic.com/news/contextual-retrieval
- FILCO (Filter Context): https://arxiv.org/abs/2311.08377
- Research: 49% reduction in failed retrievals (contextual + BM25), 67% with reranking
"""

import os
import re
import sqlite3
import uuid
from datetime import datetime, timedelta
from pathlib import Path
from typing import Any, Dict, List, Optional, Tuple

# Token limits and chunking config
EMBEDDING_TOKEN_LIMIT = 8192
CHUNK_SIZE = 1000  # Target tokens per chunk
CHUNK_OVERLAP = 150  # 15% overlap for context continuity
MAX_CHUNKS_PER_MEMORY = 20  # Reasonable limit

# Retrieval config
VECTOR_SEARCH_TOP_K = 20
BM25_SEARCH_TOP_K = 20
RRF_K = 60  # RRF constant (standard value, balances head vs tail)
FINAL_TOP_K = 10  # Final results after fusion

# Reranking config
RERAN_ENABLED = True  # Set to False to disable reranking (saves ~$0.01/session)
RERAN_MODEL = "rerank-v3.5"  # Cohere model: rerank-v3.5 or rerank-v4.0-fast/pro
RERAN_TOP_N = 10  # Final results after reranking
RERAN_MAX_CHUNKS = 1  # Max chunks per doc (keep low for speed)

# FILCO (Filter Context) config
FILCO_ENABLED = True  # Set to False to disable FILCO filtering
FILCO_RELEVANCE_THRESHOLD = 0.35  # Minimum relevance score to keep (tuned for reranker output)
FILCO_MIN_RESULTS = 3  # Always keep at least this many results
FILCO_NOISE_PATTERNS = [  # Patterns that indicate low-utility content
    r"^\s*$",  # Empty or whitespace-only
    r"^(Note:|TODO:|FIXME:|XXX:)",  # Meta-comments
    r"^\[?(DEBUG|INFO|WARN)\]?",  # Log-like prefixes
    r"^\.\.\.$",  # Truncation markers
]

# Contextualization config
CONTEXTUALIZATION_MODEL = "gpt-5.2"  # Same model as rest of system
CONTEXTUALIZATION_MAX_TOKENS = 200
CONTEXTUALIZATION_REASONING = "none"  # Fast, no reasoning needed for context generation


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


def _get_storage_path() -> Path:
    """Get the CrewAI storage path for Enlisted.
    
    Now uses workspace-relative path for cross-platform memory sharing.
    Memory is stored in Tools/CrewAI/memory/ and tracked in Git so the AI
    learns once and remembers across both Windows and Linux platforms.
    """
    storage_dir = os.environ.get("CREWAI_STORAGE_DIR")
    if storage_dir:
        return Path(storage_dir)
    
    # Default: Tools/CrewAI/memory/ (workspace-relative, syncs via Git)
    project_root = _get_project_root()
    return project_root / "Tools" / "CrewAI" / "memory"


def _get_db_path() -> Path:
    """Get the enlisted_knowledge.db path."""
    return _get_project_root() / "Tools" / "CrewAI" / "database" / "enlisted_knowledge.db"


def get_embedder_config() -> Dict[str, Any]:
    """Get the embedder configuration for memory.
    
    Uses text-embedding-3-large for best semantic quality.
    """
    return {
        "provider": "openai",
        "config": {
            "model": "text-embedding-3-large",  # 3,072 dimensions, best quality
        }
    }


def estimate_tokens(content: str) -> int:
    """Estimate token count for content using tiktoken."""
    try:
        import tiktoken
        encoding = tiktoken.get_encoding("cl100k_base")
        return len(encoding.encode(content))
    except ImportError:
        # Rough estimate: ~4 characters per token
        return len(content) // 4


def chunk_content(content: str, metadata: Optional[Dict] = None) -> List[Dict[str, Any]]:
    """Chunk content at semantic boundaries with overlap.
    
    Args:
        content: Text to chunk
        metadata: Optional metadata (flow_name, agent_name, task_description)
    
    Returns:
        List of chunks with metadata
    """
    try:
        import tiktoken
        encoding = tiktoken.get_encoding("cl100k_base")
    except ImportError:
        encoding = None
    
    # If content fits in one chunk, return as-is
    token_count = estimate_tokens(content)
    if token_count <= CHUNK_SIZE:
        return [{
            "content": content,
            "chunk_index": 0,
            "total_chunks": 1,
            "token_count": token_count,
            **(metadata or {})
        }]
    
    # Split at paragraph boundaries (double newline)
    paragraphs = re.split(r'\n\n+', content)
    
    chunks = []
    current_chunk = []
    current_tokens = 0
    
    for para in paragraphs:
        para_tokens = estimate_tokens(para)
        
        if current_tokens + para_tokens > CHUNK_SIZE and current_chunk:
            # Finalize current chunk
            chunk_text = "\n\n".join(current_chunk)
            chunks.append(chunk_text)
            
            # Start new chunk with overlap (last paragraph)
            overlap_text = current_chunk[-1] if current_chunk else ""
            current_chunk = [overlap_text, para] if overlap_text else [para]
            current_tokens = estimate_tokens("\n\n".join(current_chunk))
        else:
            current_chunk.append(para)
            current_tokens += para_tokens
    
    # Add remaining chunk
    if current_chunk:
        chunks.append("\n\n".join(current_chunk))
    
    # Build chunk dicts
    total = len(chunks)
    return [
        {
            "content": chunk,
            "chunk_index": i,
            "total_chunks": total,
            "token_count": estimate_tokens(chunk),
            **(metadata or {})
        }
        for i, chunk in enumerate(chunks)
    ]


def contextualize_chunk(chunk: Dict[str, Any], full_content: str) -> str:
    """Generate context prefix for a chunk using LLM.
    
    Args:
        chunk: Chunk dict with content and metadata
        full_content: Original full content (for LLM context)
    
    Returns:
        Context prefix string
    """
    # Check if we have OpenAI API key
    if not os.environ.get("OPENAI_API_KEY"):
        # Fallback: simple metadata-based context
        flow_name = chunk.get("flow_name", "unknown")
        agent_name = chunk.get("agent_name", "unknown")
        return f"This chunk is from the {flow_name} Flow, produced by the {agent_name} agent (chunk {chunk['chunk_index']+1} of {chunk['total_chunks']})."
    
    try:
        from openai import OpenAI
        
        client = OpenAI(api_key=os.environ.get("OPENAI_API_KEY"))
        
        flow_name = chunk.get("flow_name", "unknown")
        agent_name = chunk.get("agent_name", "unknown")
        task_desc = chunk.get("task_description", "")
        
        prompt = f"""You are helping improve search retrieval by providing context for document chunks.

Full document context (first 3000 chars):
{full_content[:3000]}

Chunk to contextualize:
{chunk["content"]}

Metadata:
- Flow: {flow_name}
- Agent: {agent_name}
- Task: {task_desc}

Provide a short, succinct context (2-3 sentences max) to situate this chunk within the overall document.
Focus on: What is this chunk about? What is its role in the larger context?

Answer only with the context and nothing else."""
        
        response = client.chat.completions.create(
            model=CONTEXTUALIZATION_MODEL,
            max_completion_tokens=CONTEXTUALIZATION_MAX_TOKENS,
            reasoning_effort=CONTEXTUALIZATION_REASONING,
            messages=[{"role": "user", "content": prompt}]
        )
        
        return response.choices[0].message.content.strip()
    
    except Exception as e:
        print(f"[MEMORY] Contextualization failed: {e}, using fallback")
        # Fallback: simple metadata-based context
        flow_name = chunk.get("flow_name", "unknown")
        agent_name = chunk.get("agent_name", "unknown")
        return f"This chunk is from the {flow_name} Flow, produced by the {agent_name} agent (chunk {chunk['chunk_index']+1} of {chunk['total_chunks']})."


def store_chunk_in_sql(chunk: Dict[str, Any], memory_id: str, memory_type: str, context_prefix: str):
    """Store chunk in contextual_memory SQL table."""
    db_path = _get_db_path()
    if not db_path.exists():
        print(f"[MEMORY] Warning: Database not found at {db_path}")
        return
    
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    contextualized = f"{context_prefix}\n\n{chunk['content']}"
    
    cursor.execute("""
        INSERT INTO contextual_memory (
            memory_id, memory_type, flow_name, agent_name, task_description,
            chunk_index, total_chunks, original_content, context_prefix,
            contextualized_content, token_count
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    """, (
        memory_id,
        memory_type,
        chunk.get("flow_name"),
        chunk.get("agent_name"),
        chunk.get("task_description"),
        chunk["chunk_index"],
        chunk["total_chunks"],
        chunk["content"],
        context_prefix,
        contextualized,
        chunk["token_count"]
    ))
    
    conn.commit()
    conn.close()


# =============================================================================
# BM25 KEYWORD INDEX
# =============================================================================

def simple_tokenize(text: str) -> List[str]:
    """Basic tokenization for BM25 - lowercase and split on non-alphanumeric."""
    import string
    text = text.lower()
    text = text.translate(str.maketrans('', '', string.punctuation))
    return text.split()


class BM25Index:
    """In-memory BM25 keyword index for hybrid search.
    
    Loads chunks from SQL and builds BM25 index on demand.
    Rebuilt when new chunks are added.
    """
    
    def __init__(self, memory_type: str = "short_term"):
        self.memory_type = memory_type
        self._index = None
        self._documents = []  # List of (id, contextualized_content)
        self._last_build_count = 0
    
    def _load_chunks_from_sql(self) -> List[Tuple[int, str]]:
        """Load all chunks for this memory type from SQL."""
        db_path = _get_db_path()
        if not db_path.exists():
            return []
        
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        try:
            cursor.execute("""
                SELECT id, contextualized_content 
                FROM contextual_memory 
                WHERE memory_type = ?
                ORDER BY created_at DESC
            """, (self.memory_type,))
            results = cursor.fetchall()
            return results
        except sqlite3.OperationalError:
            # Table might not exist yet
            return []
        finally:
            conn.close()
    
    def _rebuild_if_needed(self):
        """Rebuild index if new documents have been added."""
        documents = self._load_chunks_from_sql()
        
        if len(documents) == self._last_build_count and self._index is not None:
            return  # No change
        
        if not documents:
            self._index = None
            self._documents = []
            return
        
        try:
            from rank_bm25 import BM25Okapi
            
            self._documents = documents
            tokenized_corpus = [simple_tokenize(doc[1]) for doc in documents]
            self._index = BM25Okapi(tokenized_corpus)
            self._last_build_count = len(documents)
            print(f"[MEMORY] BM25 index rebuilt with {len(documents)} chunks")
        except ImportError:
            print("[MEMORY] rank-bm25 not installed, BM25 search disabled")
            self._index = None
    
    def search(self, query: str, top_k: int = BM25_SEARCH_TOP_K) -> List[Tuple[int, str, float]]:
        """Search the BM25 index.
        
        Args:
            query: Search query
            top_k: Number of results to return
        
        Returns:
            List of (doc_id, content, bm25_score) tuples, sorted by score descending
        """
        self._rebuild_if_needed()
        
        if self._index is None or not self._documents:
            return []
        
        tokenized_query = simple_tokenize(query)
        scores = self._index.get_scores(tokenized_query)
        
        # Get top-k indices by score
        import numpy as np
        top_indices = np.argsort(scores)[::-1][:top_k]
        
        results = []
        for idx in top_indices:
            if scores[idx] > 0:  # Only include if there's some relevance
                doc_id, content = self._documents[idx]
                results.append((doc_id, content, float(scores[idx])))
        
        return results


# Global BM25 indexes (one per memory type)
_bm25_indexes: Dict[str, BM25Index] = {}


def get_bm25_index(memory_type: str) -> BM25Index:
    """Get or create BM25 index for a memory type."""
    if memory_type not in _bm25_indexes:
        _bm25_indexes[memory_type] = BM25Index(memory_type)
    return _bm25_indexes[memory_type]


# =============================================================================
# RECIPROCAL RANK FUSION (RRF)
# =============================================================================

def reciprocal_rank_fusion(
    vector_results: List[Tuple[str, float]],  # (content, score)
    bm25_results: List[Tuple[int, str, float]],  # (id, content, score)
    k: int = RRF_K
) -> List[Tuple[str, float]]:
    """Combine vector and BM25 results using Reciprocal Rank Fusion.
    
    RRF is robust to different score scales and doesn't require tuning.
    Formula: RRF_score = sum(1 / (k + rank)) for each method where doc appears.
    
    Args:
        vector_results: Results from vector search (content, score)
        bm25_results: Results from BM25 search (id, content, score)
        k: RRF constant (typically 60)
    
    Returns:
        Fused results as (content, rrf_score) sorted by score descending
    """
    rrf_scores: Dict[str, float] = {}
    content_map: Dict[str, str] = {}  # For deduplication
    
    # Add vector results (ranked by their order, not raw score)
    for rank, (content, score) in enumerate(vector_results, start=1):
        # Use first 200 chars as key for deduplication
        content_key = content[:200]
        rrf_scores[content_key] = rrf_scores.get(content_key, 0) + (1.0 / (k + rank))
        content_map[content_key] = content
    
    # Add BM25 results
    for rank, (doc_id, content, score) in enumerate(bm25_results, start=1):
        content_key = content[:200]
        rrf_scores[content_key] = rrf_scores.get(content_key, 0) + (1.0 / (k + rank))
        content_map[content_key] = content
    
    # Sort by RRF score
    sorted_results = sorted(rrf_scores.items(), key=lambda x: x[1], reverse=True)
    
    return [(content_map[key], score) for key, score in sorted_results]


# =============================================================================
# COHERE RERANKER
# =============================================================================

# Timeout for external API calls (prevents hangs)
COHERE_TIMEOUT_SECONDS = 10.0


def rerank_results(
    query: str,
    results: List[Tuple[str, float]],  # (content, score)
    top_n: int = RERAN_TOP_N,
    timeout: float = COHERE_TIMEOUT_SECONDS
) -> List[Tuple[str, float]]:
    """Rerank results using Cohere's rerank API.
    
    Provides +18% improvement in retrieval quality on top of hybrid search.
    Falls back to original results if Cohere API unavailable or times out.
    
    Args:
        query: The search query
        results: List of (content, score) tuples from RRF fusion
        top_n: Number of results to return
        timeout: Max seconds to wait for Cohere API (default: 10s)
    
    Returns:
        Reranked results as (content, relevance_score) sorted by relevance
    """
    if not RERAN_ENABLED:
        return results[:top_n]
    
    if not results:
        return []
    
    # Check for Cohere API key
    cohere_key = os.environ.get("COHERE_API_KEY")
    if not cohere_key:
        print("[MEMORY] COHERE_API_KEY not set, skipping reranking")
        return results[:top_n]
    
    try:
        import cohere
        from httpx import Timeout
        
        # Create client with explicit timeout to prevent hangs
        client = cohere.ClientV2(
            api_key=cohere_key,
            timeout=Timeout(timeout, connect=5.0)
        )
        
        # Extract just the content strings for reranking
        # Filter out empty/whitespace-only documents to prevent API errors
        valid_results = [(content, score) for content, score in results if content and content.strip()]
        
        if not valid_results:
            print("[MEMORY] No valid documents to rerank, returning empty list")
            return []
        
        documents = [content for content, _ in valid_results]
        
        # Call Cohere rerank API
        response = client.rerank(
            model=RERAN_MODEL,
            query=query,
            documents=documents,
            top_n=min(top_n, len(documents)),
            max_tokens_per_doc=4096,  # Default context length
        )
        
        # Build reranked results
        reranked = []
        for result in response.results:
            idx = result.index
            relevance = result.relevance_score
            content = documents[idx]
            reranked.append((content, relevance))
        
        print(f"[MEMORY] Reranked {len(results)} → {len(reranked)} results (top score: {reranked[0][1]:.3f})")
        return reranked
    
    except ImportError:
        print("[MEMORY] cohere package not installed, skipping reranking")
        return results[:top_n]
    except TimeoutError:
        print(f"[MEMORY] Cohere API timed out after {timeout}s, using RRF results")
        return results[:top_n]
    except Exception as e:
        print(f"[MEMORY] Reranking failed: {e}, using RRF results")
        return results[:top_n]


# =============================================================================
# FILCO (FILTER CONTEXT) - POST-RETRIEVAL FILTERING
# =============================================================================

def filco_filter(
    query: str,
    results: List[Tuple[str, float]],  # (content, relevance_score)
) -> List[Tuple[str, float]]:
    """Filter low-utility content spans before passing to LLM.
    
    FILCO (Filter Context) enhances retrieval granularity by:
    1. Removing results below relevance threshold
    2. Filtering out noise patterns (empty, debug, meta-comments)
    3. Deduplicating near-identical content
    4. Ensuring minimum result count for robustness
    
    This improves faithfulness and reduces prompt noise without losing information.
    
    Args:
        query: Original search query (for logging)
        results: List of (content, score) tuples from reranking
    
    Returns:
        Filtered results as (content, score) tuples
    """
    if not FILCO_ENABLED:
        return results
    
    if not results:
        return []
    
    original_count = len(results)
    filtered = []
    seen_prefixes = set()  # For deduplication
    
    # Compile noise patterns
    noise_regex = [re.compile(p, re.IGNORECASE) for p in FILCO_NOISE_PATTERNS]
    
    for content, score in results:
        # Skip low relevance (but always keep minimum)
        if score < FILCO_RELEVANCE_THRESHOLD and len(filtered) >= FILCO_MIN_RESULTS:
            continue
        
        # Check noise patterns
        is_noise = False
        first_line = content.split('\n')[0].strip()[:100] if content else ""
        for pattern in noise_regex:
            if pattern.match(first_line):
                is_noise = True
                break
        
        if is_noise and len(filtered) >= FILCO_MIN_RESULTS:
            continue
        
        # Deduplicate by prefix (similar chunks from overlap)
        prefix = content[:150].strip() if content else ""
        if prefix in seen_prefixes:
            continue
        seen_prefixes.add(prefix)
        
        # Check for meaningful content length
        stripped = content.strip() if content else ""
        if len(stripped) < 50 and len(filtered) >= FILCO_MIN_RESULTS:
            continue
        
        filtered.append((content, score))
    
    # Always return at least FILCO_MIN_RESULTS if available
    if len(filtered) < FILCO_MIN_RESULTS and results:
        # Add back highest-scoring results we may have filtered
        for content, score in results:
            if (content, score) not in filtered:
                filtered.append((content, score))
                if len(filtered) >= FILCO_MIN_RESULTS:
                    break
        # Re-sort by score
        filtered.sort(key=lambda x: x[1], reverse=True)
    
    if len(filtered) < original_count:
        removed = original_count - len(filtered)
        print(f"[MEMORY] FILCO: filtered {removed} low-utility results ({original_count} → {len(filtered)})")
    
    return filtered


# =============================================================================
# CUSTOM STORAGE WITH CONTEXTUAL RETRIEVAL
# =============================================================================

def get_memory_config(use_advanced: bool = False) -> Dict[str, Any]:
    """Get memory configuration for Enlisted crews.
    
    Returns a dict that can be unpacked into Crew():
        crew = Crew(**get_memory_config(), agents=[...], tasks=[...])
    
    Args:
        use_advanced: If True, use custom contextual storage (experimental, may hang).
                      If False (default), use CrewAI's built-in memory.
    
    Configuration (default mode):
    - memory=True: Enable all memory types (short-term, long-term, entity)
    - embedder: text-embedding-3-large for best semantic search quality
    - CrewAI handles storage automatically (ChromaDB + SQLite)
    
    Note: Custom contextual retrieval (BM25+Cohere reranking) is disabled by default.
    The built-in CrewAI memory works reliably in v1.8.0. Set use_advanced=True only
    if you need hybrid search capabilities and are willing to debug potential issues.
    """
    config = {
        "memory": True,
        "embedder": get_embedder_config(),
    }
    
    # Advanced mode: Custom storage with contextual retrieval
    # Uses BM25 hybrid search + Cohere reranking + FILCO filtering
    # May cause hangs due to API incompatibilities - use with caution
    if use_advanced:
        print("[MEMORY] WARNING: Advanced memory mode enabled (experimental).")
        print("[MEMORY] Using custom ContextualRAGStorage with hybrid search.")
        short_term = get_contextual_short_term_memory()
        entity = get_contextual_entity_memory()
        
        if short_term:
            config["short_term_memory"] = short_term
        if entity:
            config["entity_memory"] = entity
    
    return config


def get_contextual_short_term_memory():
    """Get Short-Term Memory with contextual retrieval.
    
    Features:
    - Semantic chunking at paragraph boundaries
    - LLM-generated context prefixes
    - SQL storage for metadata
    - Hybrid search: vector + BM25 with RRF fusion
    
    Returns:
        ContextualShortTermMemory instance
    """
    try:
        from crewai.memory.short_term.short_term_memory import ShortTermMemory
        from crewai.memory.storage.rag_storage import RAGStorage
        
        class ContextualRAGStorage(RAGStorage):
            """RAGStorage with contextual retrieval: chunking, contextualization, hybrid search.
            
            CrewAI 1.8.0 compatibility:
            - Matches RAGStorage.__init__(type, allow_reset, embedder_config, crew, path)
            - Handles None crew gracefully (empty agents string)
            - BM25 index initialized lazily to avoid blocking
            """
            
            def __init__(self, type: str, allow_reset: bool = True, 
                         embedder_config=None, crew=None, path: str = None):
                # Call parent with explicit kwargs for CrewAI 1.8.0 compatibility
                super().__init__(
                    type=type,
                    allow_reset=allow_reset,
                    embedder_config=embedder_config,
                    crew=crew,
                    path=path
                )
                # Initialize BM25 index lazily (avoid blocking during init)
                self._bm25_index = None
                self._bm25_memory_type = "short_term"
            
            def _get_bm25_index(self):
                """Lazy initialization of BM25 index."""
                if self._bm25_index is None:
                    self._bm25_index = get_bm25_index(self._bm25_memory_type)
                return self._bm25_index
            
            def save(self, value, metadata=None):
                """Save with contextual retrieval pipeline.
                
                Note: chromadb 1.4.0+ removed 'agent' parameter from save().
                Agent info is extracted from metadata instead.
                """
                if not isinstance(value, str):
                    return super().save(value, metadata)
                
                token_count = estimate_tokens(value)
                
                # If under limit, use default behavior
                if token_count <= EMBEDDING_TOKEN_LIMIT - 500:
                    return super().save(value, metadata)
                
                print(f"[MEMORY] Content exceeds limit ({token_count} tokens), applying contextual chunking...")
                
                # Extract metadata from dict (agent parameter no longer passed in chromadb 1.4.0+)
                flow_name = metadata.get('flow_name') if metadata else None
                agent_name = metadata.get('agent', 'unknown') if metadata else 'unknown'
                task_desc = metadata.get('task', '') if metadata else ''
                
                chunk_metadata = {
                    "flow_name": flow_name,
                    "agent_name": agent_name,
                    "task_description": task_desc
                }
                
                # Chunk content
                chunks = chunk_content(value, chunk_metadata)
                memory_id = str(uuid.uuid4())
                
                print(f"[MEMORY] Split into {len(chunks)} chunks")
                
                # Process each chunk
                for chunk in chunks:
                    # Generate context prefix
                    context_prefix = contextualize_chunk(chunk, value)
                    
                    # Store in SQL (for BM25 and recovery)
                    store_chunk_in_sql(chunk, memory_id, "short_term", context_prefix)
                    
                    # Save contextualized version to vector DB
                    contextualized = f"{context_prefix}\n\n{chunk['content']}"
                    super().save(contextualized, metadata)
                
                print(f"[MEMORY] Contextual chunking complete: {len(chunks)} chunks stored")
                return memory_id
            
            def search(self, query: str, limit: int = FINAL_TOP_K, score_threshold: float = 0.0):
                """Hybrid search: vector + BM25 with RRF fusion + Cohere reranking + FILCO.
                
                Pipeline:
                1. Vector search (semantic) - top 20
                2. BM25 search (keyword) - top 20
                3. RRF fusion - combine results
                4. Cohere rerank - final refinement
                5. FILCO filter - remove low-utility spans
                6. Return filtered results
                """
                # Step 1: Vector search (use parent's search)
                try:
                    vector_results = super().search(query, limit=VECTOR_SEARCH_TOP_K, score_threshold=score_threshold)
                    # Convert to list of (content, score) tuples
                    if vector_results and hasattr(vector_results[0], 'context'):
                        vector_tuples = [(r.context, r.score if hasattr(r, 'score') else 1.0) for r in vector_results]
                    elif vector_results and isinstance(vector_results[0], dict):
                        vector_tuples = [(r.get('context', ''), r.get('score', 1.0)) for r in vector_results]
                    else:
                        vector_tuples = [(str(r), 1.0) for r in vector_results] if vector_results else []
                except Exception as e:
                    print(f"[MEMORY] Vector search failed: {e}")
                    vector_tuples = []
                
                # Step 2: BM25 search (keyword)
                bm25_results = self._get_bm25_index().search(query, top_k=BM25_SEARCH_TOP_K)
                
                # Step 3: RRF fusion
                if not vector_tuples and not bm25_results:
                    return []
                
                if not bm25_results:
                    # Fall back to vector-only
                    print("[MEMORY] BM25 returned no results, using vector-only")
                    fused = vector_tuples
                elif not vector_tuples:
                    # Fall back to BM25-only
                    print("[MEMORY] Vector returned no results, using BM25-only")
                    fused = [(content, score) for (_, content, score) in bm25_results]
                else:
                    # Full hybrid fusion
                    fused = reciprocal_rank_fusion(vector_tuples, bm25_results)
                    print(f"[MEMORY] Hybrid search: {len(vector_tuples)} vector + {len(bm25_results)} BM25 → {len(fused)} fused")
                
                # Step 4: Cohere reranking (optional, +18% improvement)
                reranked = rerank_results(query, fused, top_n=limit)
                
                # Step 5: FILCO filter (remove low-utility spans)
                filtered = filco_filter(query, reranked)
                
                return filtered
        
        # Return a custom ShortTermMemory subclass that creates storage with crew context
        class ContextualShortTermMemory(ShortTermMemory):
            """ShortTermMemory that uses ContextualRAGStorage for hybrid search."""
            
            def __init__(self, crew=None, embedder_config=None, storage=None, path=None):
                if storage is None:
                    storage = ContextualRAGStorage(
                        type="short_term",
                        allow_reset=True,
                        embedder_config=embedder_config or get_embedder_config(),
                        crew=crew,
                        path=path,
                    )
                super().__init__(crew=crew, embedder_config=embedder_config, storage=storage, path=path)
        
        # Return instance with None crew (will be set by CrewAI)
        return ContextualShortTermMemory(crew=None, embedder_config=get_embedder_config())
        return ContextualShortTermMemory
    except ImportError as e:
        print(f"[MEMORY] Warning: Could not create contextual storage: {e}")
        return None


def get_contextual_entity_memory():
    """Get Entity Memory with contextual retrieval and hybrid search.
    
    Returns:
        ContextualEntityMemory instance
    """
    try:
        from crewai.memory.entity.entity_memory import EntityMemory
        from crewai.memory.storage.rag_storage import RAGStorage
        
        class ContextualRAGStorage(RAGStorage):
            """RAGStorage with contextual retrieval for entities.
            
            CrewAI 1.8.0 compatibility:
            - Matches RAGStorage.__init__(type, allow_reset, embedder_config, crew, path)
            - BM25 index initialized lazily
            """
            
            def __init__(self, type: str, allow_reset: bool = True,
                         embedder_config=None, crew=None, path: str = None):
                super().__init__(
                    type=type,
                    allow_reset=allow_reset,
                    embedder_config=embedder_config,
                    crew=crew,
                    path=path
                )
                self._bm25_index = None
                self._bm25_memory_type = "entity"
            
            def _get_bm25_index(self):
                """Lazy initialization of BM25 index."""
                if self._bm25_index is None:
                    self._bm25_index = get_bm25_index(self._bm25_memory_type)
                return self._bm25_index
            
            def save(self, value, metadata=None):
                """Save with contextual retrieval pipeline.
                
                Note: chromadb 1.4.0+ removed 'agent' parameter from save().
                Agent info is extracted from metadata instead.
                """
                if not isinstance(value, str):
                    return super().save(value, metadata)
                
                token_count = estimate_tokens(value)
                
                if token_count <= EMBEDDING_TOKEN_LIMIT - 500:
                    return super().save(value, metadata)
                
                print(f"[MEMORY] Entity exceeds limit ({token_count} tokens), applying contextual chunking...")
                
                # Extract metadata from dict (agent parameter no longer passed in chromadb 1.4.0+)
                flow_name = metadata.get('flow_name') if metadata else None
                agent_name = metadata.get('agent', 'unknown') if metadata else 'unknown'
                task_desc = metadata.get('task', '') if metadata else ''
                
                chunk_metadata = {
                    "flow_name": flow_name,
                    "agent_name": agent_name,
                    "task_description": task_desc
                }
                
                chunks = chunk_content(value, chunk_metadata)
                memory_id = str(uuid.uuid4())
                
                for chunk in chunks:
                    context_prefix = contextualize_chunk(chunk, value)
                    store_chunk_in_sql(chunk, memory_id, "entity", context_prefix)
                    contextualized = f"{context_prefix}\n\n{chunk['content']}"
                    super().save(contextualized, metadata)
                
                print(f"[MEMORY] Entity chunking complete: {len(chunks)} chunks stored")
                return memory_id
            
            def search(self, query: str, limit: int = FINAL_TOP_K, score_threshold: float = 0.0):
                """Hybrid search: vector + BM25 with RRF fusion + Cohere reranking + FILCO."""
                # Step 1: Vector search
                try:
                    vector_results = super().search(query, limit=VECTOR_SEARCH_TOP_K, score_threshold=score_threshold)
                    if vector_results and hasattr(vector_results[0], 'context'):
                        vector_tuples = [(r.context, r.score if hasattr(r, 'score') else 1.0) for r in vector_results]
                    elif vector_results and isinstance(vector_results[0], dict):
                        vector_tuples = [(r.get('context', ''), r.get('score', 1.0)) for r in vector_results]
                    else:
                        vector_tuples = [(str(r), 1.0) for r in vector_results] if vector_results else []
                except Exception as e:
                    print(f"[MEMORY] Vector search failed: {e}")
                    vector_tuples = []
                
                # Step 2: BM25 search
                bm25_results = self._get_bm25_index().search(query, top_k=BM25_SEARCH_TOP_K)
                
                # Step 3: RRF fusion
                if not vector_tuples and not bm25_results:
                    return []
                
                if not bm25_results:
                    fused = vector_tuples
                elif not vector_tuples:
                    fused = [(content, score) for (_, content, score) in bm25_results]
                else:
                    fused = reciprocal_rank_fusion(vector_tuples, bm25_results)
                    print(f"[MEMORY] Entity hybrid search: {len(vector_tuples)} vector + {len(bm25_results)} BM25 → {len(fused)} fused")
                
                # Step 4: Cohere reranking
                reranked = rerank_results(query, fused, top_n=limit)
                
                # Step 5: FILCO filter (remove low-utility spans)
                filtered = filco_filter(query, reranked)
                
                return filtered
        
        # Return a custom EntityMemory subclass that creates storage with crew context
        class ContextualEntityMemory(EntityMemory):
            """EntityMemory that uses ContextualRAGStorage for hybrid search."""
            
            def __init__(self, crew=None, embedder_config=None, storage=None, path=None):
                if storage is None:
                    storage = ContextualRAGStorage(
                        type="entities",
                        allow_reset=True,
                        embedder_config=embedder_config or get_embedder_config(),
                        crew=crew,
                        path=path,
                    )
                super().__init__(crew=crew, embedder_config=embedder_config, storage=storage, path=path)
        
        # Return instance with None crew (will be set by CrewAI)
        return ContextualEntityMemory(crew=None, embedder_config=get_embedder_config())
    except ImportError as e:
        print(f"[MEMORY] Warning: Could not create contextual entity storage: {e}")
        return None


def clear_memory(memory_types: Optional[List[str]] = None) -> Dict[str, bool]:
    """Clear CrewAI memory storage including contextual_memory table.
    
    Args:
        memory_types: List of types to clear. Options:
            - 'short': Short-term memory (ChromaDB + SQL)
            - 'long': Long-term memory (SQLite)
            - 'entity': Entity memory (ChromaDB + SQL)
            - 'contextual': Contextual memory SQL table only
            - 'all': Clear everything
            If None, clears all.
    
    Returns:
        Dict of {memory_type: success_bool}
    """
    import shutil
    
    storage_path = _get_storage_path()
    
    if memory_types is None or 'all' in memory_types:
        memory_types = ['short', 'long', 'entity', 'contextual']
    
    type_to_folder = {
        'short': 'short_term_memory',
        'long': 'long_term_memory',
        'entity': 'entities',
    }
    
    results = {}
    
    for mem_type in memory_types:
        if mem_type == 'contextual':
            # Clear contextual_memory table
            try:
                db_path = _get_db_path()
                if db_path.exists():
                    conn = sqlite3.connect(db_path)
                    conn.execute("DELETE FROM contextual_memory")
                    conn.commit()
                    conn.close()
                    results['contextual'] = True
                    print("[MEMORY] Cleared contextual_memory table")
                else:
                    results['contextual'] = True
            except Exception as e:
                results['contextual'] = False
                print(f"[MEMORY] Failed to clear contextual_memory: {e}")
            continue
        
        folder_name = type_to_folder.get(mem_type)
        if not folder_name:
            continue
            
        folder_path = storage_path / folder_name
        try:
            if folder_path.exists():
                shutil.rmtree(folder_path)
                results[mem_type] = True
                print(f"[MEMORY] Cleared {mem_type} memory")
            else:
                results[mem_type] = True  # Nothing to clear
        except Exception as e:
            results[mem_type] = False
            print(f"[MEMORY] Failed to clear {mem_type}: {e}")
    
    # Also clear SQLite DB for long-term
    if 'long' in memory_types:
        db_path = storage_path / "long_term_memory_storage.db"
        try:
            if db_path.exists():
                db_path.unlink()
                print("[MEMORY] Cleared long-term memory database")
        except Exception as e:
            print(f"[MEMORY] Failed to clear LTM database: {e}")
    
    return results


# Export for convenience
__all__ = [
    'get_memory_config',
    'get_embedder_config',
    'get_contextual_short_term_memory',
    'get_contextual_entity_memory',
    'estimate_tokens',
    'chunk_content',
    'contextualize_chunk',
    'clear_memory',
    'get_bm25_index',
    'reciprocal_rank_fusion',
    'rerank_results',
    'filco_filter',
    'BM25Index',
    'CHUNK_SIZE',
    'CHUNK_OVERLAP',
    'EMBEDDING_TOKEN_LIMIT',
    'RERAN_ENABLED',
    'RERAN_MODEL',
    'FILCO_ENABLED',
    'FILCO_RELEVANCE_THRESHOLD',
    'FILCO_MIN_RESULTS',
    'VECTOR_SEARCH_TOP_K',
    'BM25_SEARCH_TOP_K',
    'RRF_K',
    'FINAL_TOP_K',
]
