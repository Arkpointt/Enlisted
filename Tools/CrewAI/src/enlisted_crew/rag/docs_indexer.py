"""
Documentation indexer for semantic search.

Indexes docs/ markdown files into ChromaDB for fast semantic search.
Run once to build the index, then search via search_docs_semantic tool.

Usage:
    python -m enlisted_crew.rag.docs_indexer --index-all
    python -m enlisted_crew.rag.docs_indexer --stats
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


def find_md_files(base_path: Path) -> List[Path]:
    """Find all markdown files in a directory recursively."""
    if not base_path.exists():
        print(f"Warning: Path does not exist: {base_path}")
        return []
    
    md_files = []
    for md_file in base_path.rglob("*.md"):
        if md_file.is_file():
            # Skip certain directories
            if any(skip in str(md_file) for skip in [".venv", "node_modules", ".git"]):
                continue
            md_files.append(md_file)
    
    return md_files


def chunk_markdown_file(file_path: Path, project_root: Path) -> List[Dict]:
    """
    Chunk a markdown file at heading boundaries.
    
    Returns list of chunks with metadata:
    - content: The markdown section
    - source: Relative file path
    - start_line: Starting line number
    - end_line: Ending line number
    - section_title: H1/H2/H3 heading
    - section_level: Heading depth (1-6)
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
    current_title = file_path.stem  # Default to filename
    current_level = 0
    
    # Pattern for markdown headings
    heading_pattern = re.compile(r'^(#{1,6})\s+(.+)$')
    
    for i, line in enumerate(lines, 1):
        heading_match = heading_pattern.match(line.strip())
        
        if heading_match:
            # Save previous chunk if substantial
            if current_chunk and len(''.join(current_chunk).strip()) > 100:
                chunk_content = ''.join(current_chunk).strip()
                chunks.append({
                    'content': chunk_content,
                    'source': str(relative_path),
                    'start_line': current_start,
                    'end_line': i - 1,
                    'section_title': current_title,
                    'section_level': current_level,
                })
            
            # Start new chunk
            current_chunk = [line]
            current_start = i
            current_level = len(heading_match.group(1))
            current_title = heading_match.group(2).strip()
        else:
            current_chunk.append(line)
    
    # Add final chunk if substantial
    if current_chunk and len(''.join(current_chunk).strip()) > 100:
        chunk_content = ''.join(current_chunk).strip()
        chunks.append({
            'content': chunk_content,
            'source': str(relative_path),
            'start_line': current_start,
            'end_line': len(lines),
            'section_title': current_title,
            'section_level': current_level,
        })
    
    return chunks


def index_docs(
    docs_dir: Path,
    persist_dir: Path,
    batch_size: int = 100
) -> Tuple[int, int]:
    """
    Index markdown files from docs directory.
    
    Args:
        docs_dir: Directory to index (e.g., project_root/docs/)
        persist_dir: Directory to persist ChromaDB
        batch_size: Number of chunks to embed per batch
    
    Returns:
        (total_files, total_chunks) indexed
    """
    print("Starting documentation indexing...")
    print(f"Docs directory: {docs_dir}")
    print(f"Persist directory: {persist_dir}")
    
    # Find all markdown files
    all_files = find_md_files(docs_dir)
    
    if not all_files:
        print("No markdown files found to index!")
        return 0, 0
    
    print(f"\nTotal markdown files to index: {len(all_files)}")
    
    # Chunk all files
    project_root = _get_project_root()
    all_chunks = []
    
    for file_path in all_files:
        chunks = chunk_markdown_file(file_path, project_root)
        all_chunks.extend(chunks)
        
        if len(all_chunks) % 50 == 0:
            print(f"  Processed {len(all_chunks)} chunks so far...")
    
    print(f"\nTotal chunks created: {len(all_chunks)}")
    
    if not all_chunks:
        print("No chunks created!")
        return len(all_files), 0
    
    # Estimate token count and cost
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
                'section_title': chunk['section_title'],
                'section_level': chunk['section_level'],
            }
        )
        documents.append(doc)
    
    # Initialize embeddings (same as codebase indexer)
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
    """Get statistics about the indexed documentation."""
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
    parser = argparse.ArgumentParser(description='Index Enlisted documentation for semantic search')
    parser.add_argument(
        '--index-all',
        action='store_true',
        help='Index all documentation in docs/ directory'
    )
    parser.add_argument(
        '--stats',
        action='store_true',
        help='Show index statistics'
    )
    
    args = parser.parse_args()
    
    project_root = _get_project_root()
    persist_dir = project_root / "Tools" / "CrewAI" / "src" / "enlisted_crew" / "rag" / "docs_vector_db"
    
    if args.stats:
        stats = get_index_stats(persist_dir)
        print("Documentation Index Statistics:")
        for key, value in stats.items():
            print(f"  {key}: {value}")
        return
    
    if args.index_all:
        docs_dir = project_root / "docs"
        index_docs(docs_dir, persist_dir)
    else:
        parser.print_help()


if __name__ == "__main__":
    main()
