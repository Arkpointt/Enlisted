"""
Codebase indexer for semantic search.

Indexes src/ and Decompile/ C# files into ChromaDB for fast semantic search.
Run once to build the index, then search via codebase_rag_tool.py.

Usage:
    python -m enlisted_crew.rag.codebase_indexer --index-all
    python -m enlisted_crew.rag.codebase_indexer --stats
"""

import argparse
import os
import re
from pathlib import Path
from typing import List, Dict, Tuple

from dotenv import load_dotenv
from langchain_chroma import Chroma
from langchain_openai import OpenAIEmbeddings
from langchain_core.documents import Document

# Load .env file
env_path = Path(__file__).parent.parent.parent.parent / ".env"
load_dotenv(env_path)


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


def find_cs_files(base_path: Path) -> List[Path]:
    """Find all C# files in a directory recursively."""
    if not base_path.exists():
        print(f"Warning: Path does not exist: {base_path}")
        return []
    
    cs_files = []
    for cs_file in base_path.rglob("*.cs"):
        if cs_file.is_file():
            cs_files.append(cs_file)
    
    return cs_files


def chunk_csharp_file(file_path: Path, project_root: Path) -> List[Dict]:
    """
    Chunk a C# file at method/property boundaries.
    
    Returns list of chunks with metadata:
    - content: The code chunk
    - source: Relative file path
    - start_line: Starting line number
    - end_line: Ending line number
    - class_name: Containing class (if detectable)
    - method_name: Method/property name (if detectable)
    """
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            lines = f.readlines()
    except Exception as e:
        print(f"Error reading {file_path}: {e}")
        return []
    
    # Get relative path for source metadata
    try:
        relative_path = file_path.relative_to(project_root)
    except ValueError:
        relative_path = file_path
    
    chunks = []
    current_chunk = []
    current_start = 1
    current_class = None
    current_member = None
    brace_depth = 0
    in_method = False
    
    # Patterns for detecting C# constructs
    class_pattern = re.compile(r'\s*(public|internal|private|protected)?\s*(static|abstract|sealed)?\s*class\s+(\w+)')
    method_pattern = re.compile(r'\s*(public|internal|private|protected)?\s*(static|virtual|override|async)?\s*\w+\s+(\w+)\s*\(')
    property_pattern = re.compile(r'\s*(public|internal|private|protected)?\s*\w+\s+(\w+)\s*\{')
    
    for i, line in enumerate(lines, 1):
        # Track class names
        class_match = class_pattern.search(line)
        if class_match:
            current_class = class_match.group(3)
        
        # Track method/property names
        method_match = method_pattern.search(line)
        if method_match:
            current_member = method_match.group(3)
            in_method = True
        else:
            prop_match = property_pattern.search(line)
            if prop_match:
                current_member = prop_match.group(2)
                in_method = True
        
        # Track brace depth
        brace_depth += line.count('{') - line.count('}')
        
        current_chunk.append(line)
        
        # Chunk boundaries: end of method/property (brace depth returns to 0)
        if in_method and brace_depth == 0 and '{' in ''.join(current_chunk):
            # We've closed a method/property
            chunk_content = ''.join(current_chunk).strip()
            
            if chunk_content and len(chunk_content) > 50:  # Skip tiny chunks
                chunks.append({
                    'content': chunk_content,
                    'source': str(relative_path),
                    'start_line': current_start,
                    'end_line': i,
                    'class_name': current_class or 'Unknown',
                    'method_name': current_member or 'Unknown',
                })
            
            # Reset for next chunk
            current_chunk = []
            current_start = i + 1
            in_method = False
            current_member = None
        
        # Also chunk at class boundaries for large classes
        elif not in_method and brace_depth == 0 and len(current_chunk) > 100:
            chunk_content = ''.join(current_chunk).strip()
            
            if chunk_content and len(chunk_content) > 50:
                chunks.append({
                    'content': chunk_content,
                    'source': str(relative_path),
                    'start_line': current_start,
                    'end_line': i,
                    'class_name': current_class or 'Unknown',
                    'method_name': 'Class-level',
                })
            
            current_chunk = []
            current_start = i + 1
    
    # Add remaining content as final chunk if substantial
    if current_chunk:
        chunk_content = ''.join(current_chunk).strip()
        if chunk_content and len(chunk_content) > 50:
            chunks.append({
                'content': chunk_content,
                'source': str(relative_path),
                'start_line': current_start,
                'end_line': len(lines),
                'class_name': current_class or 'Unknown',
                'method_name': current_member or 'File-level',
            })
    
    return chunks


def index_codebase(
    src_dirs: List[Path],
    persist_dir: Path,
    batch_size: int = 100
) -> Tuple[int, int]:
    """
    Index C# files from specified directories.
    
    Args:
        src_dirs: List of directories to index (e.g., [src/, Decompile/])
        persist_dir: Directory to persist ChromaDB
        batch_size: Number of chunks to embed per batch
    
    Returns:
        (total_files, total_chunks) indexed
    """
    print("Starting codebase indexing...")
    print(f"Target directories: {[str(d) for d in src_dirs]}")
    print(f"Persist directory: {persist_dir}")
    
    # Find all C# files
    all_files = []
    for src_dir in src_dirs:
        files = find_cs_files(src_dir)
        print(f"Found {len(files)} C# files in {src_dir}")
        all_files.extend(files)
    
    if not all_files:
        print("No C# files found to index!")
        return 0, 0
    
    print(f"\nTotal C# files to index: {len(all_files)}")
    
    # Chunk all files
    project_root = _get_project_root()
    all_chunks = []
    
    for file_path in all_files:
        chunks = chunk_csharp_file(file_path, project_root)
        all_chunks.extend(chunks)
        
        if len(all_files) < 50 or len(all_chunks) % 500 == 0:
            print(f"  Processed {len(all_chunks)} chunks so far...")
    
    print(f"\nTotal chunks created: {len(all_chunks)}")
    
    if not all_chunks:
        print("No chunks created!")
        return len(all_files), 0
    
    # Estimate token count
    total_chars = sum(len(chunk['content']) for chunk in all_chunks)
    estimated_tokens = total_chars // 4  # Rough estimate: 4 chars per token
    estimated_cost = (estimated_tokens / 1_000_000) * 0.13  # text-embedding-3-large pricing
    
    print(f"Estimated tokens: {estimated_tokens:,}")
    print(f"Estimated cost: ${estimated_cost:.4f}")
    print("\nBuilding vector index...")
    
    # Create documents for ChromaDB
    documents = []
    for chunk in all_chunks:
        doc = Document(
            page_content=chunk['content'],
            metadata={
                'source': chunk['source'],
                'start_line': chunk['start_line'],
                'end_line': chunk['end_line'],
                'class_name': chunk['class_name'],
                'method_name': chunk['method_name'],
            }
        )
        documents.append(doc)
    
    # Initialize embeddings (same as memory_config.py)
    embeddings = OpenAIEmbeddings(model="text-embedding-3-large")
    
    # Create vector store with batching
    persist_dir.mkdir(parents=True, exist_ok=True)
    
    print("Embedding and indexing (this may take a few minutes)...")
    
    # Process in batches to show progress
    for i in range(0, len(documents), batch_size):
        batch = documents[i:i+batch_size]
        
        if i == 0:
            # Create new vectorstore with first batch
            vectorstore = Chroma.from_documents(
                documents=batch,
                embedding=embeddings,
                persist_directory=str(persist_dir)
            )
        else:
            # Add to existing vectorstore
            vectorstore.add_documents(batch)
        
        print(f"  Indexed {min(i + batch_size, len(documents))}/{len(documents)} chunks")
    
    print(f"\n✓ Indexing complete!")
    print(f"  Files indexed: {len(all_files)}")
    print(f"  Chunks indexed: {len(all_chunks)}")
    print(f"  Index location: {persist_dir}")
    
    return len(all_files), len(all_chunks)


def get_index_stats(persist_dir: Path) -> Dict:
    """Get statistics about the indexed codebase."""
    if not persist_dir.exists():
        return {'error': 'Index not found', 'path': str(persist_dir)}
    
    try:
        embeddings = OpenAIEmbeddings(model="text-embedding-3-large")
        vectorstore = Chroma(
            persist_directory=str(persist_dir),
            embedding_function=embeddings
        )
        
        # Get collection stats
        collection = vectorstore._collection
        count = collection.count()
        
        return {
            'total_chunks': count,
            'index_path': str(persist_dir),
            'status': 'ready'
        }
    except Exception as e:
        return {
            'error': str(e),
            'path': str(persist_dir)
        }


def main():
    parser = argparse.ArgumentParser(description='Index Enlisted codebase for semantic search')
    parser.add_argument(
        '--index-all',
        action='store_true',
        help='Index both src/ and Decompile/ directories'
    )
    parser.add_argument(
        '--index-src-only',
        action='store_true',
        help='Index only src/ directory'
    )
    parser.add_argument(
        '--stats',
        action='store_true',
        help='Show index statistics'
    )
    
    args = parser.parse_args()
    
    project_root = _get_project_root()
    persist_dir = project_root / "Tools" / "CrewAI" / "src" / "enlisted_crew" / "rag" / "vector_db"
    
    if args.stats:
        stats = get_index_stats(persist_dir)
        print("Index Statistics:")
        for key, value in stats.items():
            print(f"  {key}: {value}")
        return
    
    if args.index_all:
        # Index both src/ and Decompile/
        src_dirs = [
            project_root / "src",
            project_root.parent / "Decompile"  # Decompile is sibling to Enlisted
        ]
        index_codebase(src_dirs, persist_dir)
    
    elif args.index_src_only:
        # Index only src/
        src_dirs = [project_root / "src"]
        index_codebase(src_dirs, persist_dir)
    
    else:
        parser.print_help()


if __name__ == "__main__":
    main()
