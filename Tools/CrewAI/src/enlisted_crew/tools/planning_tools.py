"""
Planning document tools for CrewAI.

Handles creating and versioning planning documents in docs/CrewAI_Plans/
"""
import os
import re
from pathlib import Path
from datetime import datetime
from crewai.tools import tool


@tool("Write Planning Document")
def write_planning_doc_tool(feature_name: str, content: str) -> str:
    """
    Write a planning document to docs/CrewAI_Plans/ with intelligent versioning.
    
    Versioning logic:
    - First time: feature-name.md
    - Updates: Checks if content is substantially different
      - If minor changes (< 30% different): Updates existing file with timestamp note
      - If major changes (> 30% different): Creates feature-name-v2.md, v3.md, etc.
    
    Args:
        feature_name: Kebab-case name like "reputation-morale-integration"
        content: Full markdown content of the planning document
    
    Returns:
        Success message with file path
    """
    try:
        # Find project root
        project_root = Path(os.environ.get("ENLISTED_PROJECT_ROOT", r"C:\Dev\Enlisted\Enlisted"))
        plans_dir = project_root / "docs" / "CrewAI_Plans"
        plans_dir.mkdir(parents=True, exist_ok=True)
        
        # Clean feature name
        clean_name = re.sub(r'[^a-z0-9-]', '-', feature_name.lower())
        base_path = plans_dir / f"{clean_name}.md"
        
        # Check if file exists
        if not base_path.exists():
            # First time - create new file
            _write_file(base_path, content, is_new=True)
            rel_path = base_path.relative_to(project_root)
            return f"✅ Created new planning document:\n   Path: {base_path}\n   Relative: {rel_path}"
        
        # File exists - check similarity
        existing_content = base_path.read_text(encoding='utf-8')
        similarity = _calculate_similarity(existing_content, content)
        
        if similarity > 0.70:  # > 70% similar = minor update
            # Update existing file with revision note
            timestamp = datetime.now().strftime("%Y-%m-%d %H:%M")
            updated_content = _add_revision_note(content, timestamp)
            _write_file(base_path, updated_content, is_new=False)
            rel_path = base_path.relative_to(project_root)
            return f"✅ Updated planning document (minor changes):\n   Path: {base_path}\n   Relative: {rel_path}"
        
        else:  # < 70% similar = major rewrite, create new version
            version = _find_next_version(plans_dir, clean_name)
            versioned_path = plans_dir / f"{clean_name}-v{version}.md"
            _write_file(versioned_path, content, is_new=True)
            rel_path = versioned_path.relative_to(project_root)
            return f"✅ Created new version (major changes):\n   Path: {versioned_path}\n   Relative: {rel_path}\n   Previous version preserved as {base_path.name}"
    
    except Exception as e:
        return f"❌ Error writing planning document: {str(e)}"


def _write_file(path: Path, content: str, is_new: bool):
    """Write content to file with UTF-8 encoding."""
    # Ensure content ends with newline
    if not content.endswith('\n'):
        content += '\n'
    
    path.write_text(content, encoding='utf-8')


def _add_revision_note(content: str, timestamp: str) -> str:
    """Add revision timestamp to existing content."""
    # Look for "Last Updated" or similar header
    if "**Last Updated:**" in content or "Last Updated:" in content:
        # Update existing timestamp
        content = re.sub(
            r'\*\*Last Updated:\*\*[^\n]*',
            f'**Last Updated:** {timestamp} (revised)',
            content
        )
    else:
        # Add after first heading or at top
        lines = content.split('\n')
        for i, line in enumerate(lines):
            if line.startswith('# '):
                lines.insert(i + 1, f'\n**Last Updated:** {timestamp} (revised)\n')
                break
        content = '\n'.join(lines)
    
    return content


def _calculate_similarity(text1: str, text2: str) -> float:
    """
    Calculate simple text similarity (0.0 to 1.0).
    Uses word overlap as a basic heuristic.
    """
    # Normalize: lowercase, remove special chars, split into words
    words1 = set(re.findall(r'\w+', text1.lower()))
    words2 = set(re.findall(r'\w+', text2.lower()))
    
    if not words1 or not words2:
        return 0.0
    
    # Jaccard similarity
    intersection = len(words1 & words2)
    union = len(words1 | words2)
    
    return intersection / union if union > 0 else 0.0


@tool("Read Planning Document")
def read_planning_doc_tool(feature_name: str) -> str:
    """
    Read a planning document from docs/CrewAI_Plans/.
    
    Args:
        feature_name: Kebab-case name like "reputation-morale-integration"
    
    Returns:
        The full content of the planning document, or error if not found.
    """
    try:
        project_root = Path(os.environ.get("ENLISTED_PROJECT_ROOT", r"C:\Dev\Enlisted\Enlisted"))
        plans_dir = project_root / "docs" / "CrewAI_Plans"
        
        # Clean feature name
        clean_name = re.sub(r'[^a-z0-9-]', '-', feature_name.lower())
        doc_path = plans_dir / f"{clean_name}.md"
        
        if not doc_path.exists():
            # Check for versioned files
            versioned = sorted(plans_dir.glob(f"{clean_name}-v*.md"), reverse=True)
            if versioned:
                doc_path = versioned[0]  # Use latest version
            else:
                return f"❌ Planning document not found: {clean_name}.md"
        
        content = doc_path.read_text(encoding='utf-8')
        return f"📄 {doc_path.name}:\n\n{content}"
    
    except Exception as e:
        return f"❌ Error reading planning document: {str(e)}"


def _find_next_version(directory: Path, base_name: str) -> int:
    """Find the next available version number (v2, v3, etc.)."""
    existing = list(directory.glob(f"{base_name}-v*.md"))
    
    if not existing:
        return 2  # First versioned file is v2 (v1 is the base file)
    
    # Extract version numbers
    versions = []
    for path in existing:
        match = re.search(r'-v(\d+)\.md$', path.name)
        if match:
            versions.append(int(match.group(1)))
    
    return max(versions) + 1 if versions else 2
