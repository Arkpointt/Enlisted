"""
Build/Rebuild Bannerlord API Index

Standalone script to build the SQLite index of decompiled Bannerlord source.
Run this after updating the decompiled source or when setting up for the first time.

Usage:
    python build_index.py [--rebuild]
    
Options:
    --rebuild: Delete existing index and rebuild from scratch
"""

import sys
import argparse
from pathlib import Path

# Import the indexer from the MCP server
from bannerlord_api_server import BannerlordAPIIndex, INDEX_DB_PATH, DECOMPILE_PATH


def build_index(rebuild: bool = False):
    """Build or rebuild the API index."""
    
    if rebuild and INDEX_DB_PATH.exists():
        print(f"Deleting existing index at {INDEX_DB_PATH}")
        INDEX_DB_PATH.unlink()
    
    if INDEX_DB_PATH.exists() and not rebuild:
        print(f"Index already exists at {INDEX_DB_PATH}")
        print("Use --rebuild to force rebuild")
        return
    
    if not DECOMPILE_PATH.exists():
        print(f"ERROR: Decompile path not found: {DECOMPILE_PATH}")
        print("Make sure C:\\Dev\\Enlisted\\Decompile exists and contains decompiled source")
        sys.exit(1)
    
    print(f"Building Bannerlord API index from {DECOMPILE_PATH}")
    print(f"This may take 2-5 minutes depending on your system...")
    print()
    
    index = BannerlordAPIIndex(INDEX_DB_PATH, DECOMPILE_PATH)
    index.connect()
    index.initialize_schema()
    index.index_all_files()
    
    # Print statistics
    conn = index.conn
    stats = {
        "namespaces": conn.execute("SELECT COUNT(*) FROM namespaces").fetchone()[0],
        "classes": conn.execute("SELECT COUNT(*) FROM classes").fetchone()[0],
        "methods": conn.execute("SELECT COUNT(*) FROM methods").fetchone()[0],
        "properties": conn.execute("SELECT COUNT(*) FROM properties").fetchone()[0],
        "interfaces": conn.execute("SELECT COUNT(DISTINCT interface_name) FROM interfaces_implemented").fetchone()[0],
    }
    
    print()
    print("=" * 60)
    print("INDEX STATISTICS")
    print("=" * 60)
    print(f"  Namespaces:  {stats['namespaces']:,}")
    print(f"  Classes:     {stats['classes']:,}")
    print(f"  Methods:     {stats['methods']:,}")
    print(f"  Properties:  {stats['properties']:,}")
    print(f"  Interfaces:  {stats['interfaces']:,}")
    print("=" * 60)
    print(f"[OK] Index saved to: {INDEX_DB_PATH}")
    print()
    print("You can now use the Bannerlord API MCP server with CrewAI!")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Build Bannerlord API index")
    parser.add_argument("--rebuild", action="store_true", help="Rebuild index from scratch")
    args = parser.parse_args()
    
    build_index(rebuild=args.rebuild)
