"""
File Writing Tools for Enlisted CrewAI

Wraps CrewAI's FileWriterTool with project-specific constraints.
Only allows writing to approved directories for safety.
"""

import os
from pathlib import Path
from crewai.tools import tool
from crewai_tools import FileWriterTool

# Initialize the base file writer
_file_writer = FileWriterTool()


def get_project_root() -> Path:
    """Get the Enlisted project root directory."""
    env_root = os.environ.get("ENLISTED_PROJECT_ROOT")
    if env_root:
        return Path(env_root)
    
    current = Path(__file__).resolve()
    for parent in current.parents:
        if (parent / "Enlisted.csproj").exists():
            return parent
    
    return Path(r"C:\Dev\Enlisted\Enlisted")


PROJECT_ROOT = get_project_root()

# Allowed directories for file writing (security constraint)
ALLOWED_WRITE_DIRS = [
    # Source code
    "src/Features",
    "src/Mod.Core",
    "src/Mod.GameAdapters",
    # JSON content
    "ModuleData/Enlisted/Events",
    "ModuleData/Enlisted/Decisions",
    "ModuleData/Enlisted/Orders",
    "ModuleData/Enlisted/Config",
    "ModuleData/Enlisted/Conditions",
    "ModuleData/Enlisted/Content",
    "ModuleData/Languages",
    # Documentation
    "docs/Features",
    "docs/CrewAI_Plans",
    "docs/ANEWFEATURE",
    "docs/Reference",
]


def _is_allowed_path(file_path: str) -> bool:
    """Check if the file path is within an allowed directory."""
    normalized = file_path.replace("\\", "/")
    for allowed in ALLOWED_WRITE_DIRS:
        if normalized.startswith(allowed) or f"/{allowed}" in normalized:
            return True
    return False


@tool("Write Source File")
def write_source(file_path: str, content: str) -> str:
    """
    Write a C# source file to the project.
    
    IMPORTANT: After creating a new .cs file, you must manually add it to
    Enlisted.csproj with <Compile Include="path/to/file.cs"/>
    
    Args:
        file_path: Path relative to project root (e.g., "src/Features/Content/MyNew.cs")
        content: Full C# source code content
    
    Returns:
        Success message or error.
    """
    if not file_path.endswith(".cs"):
        return f"ERROR: write_source only writes .cs files. Got: {file_path}"
    
    if not _is_allowed_path(file_path):
        return f"ERROR: Cannot write to {file_path}. Allowed directories: src/Features, src/Mod.Core, src/Mod.GameAdapters"
    
    full_path = PROJECT_ROOT / file_path
    directory = full_path.parent
    
    try:
        # Create directory if needed
        directory.mkdir(parents=True, exist_ok=True)
        
        # Write file
        full_path.write_text(content, encoding='utf-8')
        
        # Check if file is new (needs .csproj entry)
        csproj_reminder = ""
        if not (PROJECT_ROOT / "Enlisted.csproj").read_text().find(file_path.replace("/", "\\")) >= 0:
            csproj_reminder = f"\n\nREMINDER: Add to Enlisted.csproj:\n<Compile Include=\"{file_path.replace('/', '\\')}\"/>"
        
        return f"OK: Wrote C# file: {file_path} ({len(content)} bytes){csproj_reminder}"
    
    except Exception as e:
        return f"ERROR writing {file_path}: {e}"


@tool("Write Event File")
def write_event(file_path: str, content: str) -> str:
    """
    Write a JSON event file to ModuleData/Enlisted/.
    
    Args:
        file_path: Path relative to project root (e.g., "ModuleData/Enlisted/Events/my_events.json")
        content: Full JSON content (must be valid JSON)
    
    Returns:
        Success message or error.
    """
    if not file_path.endswith(".json"):
        return f"ERROR: write_event only writes .json files. Got: {file_path}"
    
    if not _is_allowed_path(file_path):
        return f"ERROR: Cannot write to {file_path}. Allowed: ModuleData/Enlisted/Events, Decisions, Orders, Config"
    
    full_path = PROJECT_ROOT / file_path
    directory = full_path.parent
    
    try:
        # Validate JSON before writing
        import json
        try:
            json.loads(content)
        except json.JSONDecodeError as e:
            return f"ERROR: Invalid JSON content: {e}"
        
        # Create directory if needed
        directory.mkdir(parents=True, exist_ok=True)
        
        # Write file with BOM for Windows compatibility
        full_path.write_text(content, encoding='utf-8-sig')
        
        return f"OK: Wrote JSON file: {file_path} ({len(content)} bytes)"
    
    except Exception as e:
        return f"ERROR writing {file_path}: {e}"


@tool("Update Localization")
def update_localization(string_id: str, text: str) -> str:
    """
    Add or update a localization string in enlisted_strings.xml.
    
    Args:
        string_id: The string ID (e.g., "evt_supply_crisis_title")
        text: The localized text content
    
    Returns:
        Success message or error.
    """
    xml_path = PROJECT_ROOT / "ModuleData" / "Languages" / "enlisted_strings.xml"
    
    if not xml_path.exists():
        return f"ERROR: Localization file not found at {xml_path}"
    
    try:
        content = xml_path.read_text(encoding='utf-8')
        
        # Check if string already exists
        if f'id="{string_id}"' in content:
            # Update existing string
            import re
            pattern = rf'<string id="{string_id}" text="[^"]*"\s*/>'
            replacement = f'<string id="{string_id}" text="{_escape_xml(text)}" />'
            new_content = re.sub(pattern, replacement, content)
            
            if new_content == content:
                return f"WARNING: String {string_id} exists but couldn't update (check format)"
            
            xml_path.write_text(new_content, encoding='utf-8')
            return f"OK: Updated localization: {string_id}"
        
        else:
            # Add new string before closing </strings> tag
            new_string = f'  <string id="{string_id}" text="{_escape_xml(text)}" />\n'
            new_content = content.replace("</strings>", f"{new_string}</strings>")
            
            xml_path.write_text(new_content, encoding='utf-8')
            return f"OK: Added localization: {string_id}"
    
    except Exception as e:
        return f"ERROR updating localization: {e}"


def _escape_xml(text: str) -> str:
    """Escape special XML characters."""
    return (text
            .replace("&", "&amp;")
            .replace("<", "&lt;")
            .replace(">", "&gt;")
            .replace('"', "&quot;")
            .replace("'", "&apos;")
            .replace("\n", "&#xA;"))


@tool("Write Documentation")
def write_doc(file_path: str, content: str) -> str:
    """
    Write or update a markdown documentation file anywhere in the project.
    
    Args:
        file_path: Path relative to project root (e.g., "docs/INDEX.md", "README.md")
        content: Full markdown content
    
    Returns:
        Success message or error.
    """
    if not file_path.endswith(".md"):
        return f"ERROR: write_doc only writes .md files. Got: {file_path}"
    
    full_path = PROJECT_ROOT / file_path
    
    # Safety: must be within project root
    try:
        full_path.resolve().relative_to(PROJECT_ROOT.resolve())
    except ValueError:
        return f"ERROR: Path {file_path} is outside project root"
    
    directory = full_path.parent
    
    try:
        directory.mkdir(parents=True, exist_ok=True)
        full_path.write_text(content, encoding='utf-8')
        return f"OK: Wrote documentation: {file_path} ({len(content)} bytes)"
    
    except Exception as e:
        return f"ERROR writing {file_path}: {e}"


@tool("Append to Csproj")
def append_to_csproj(file_path: str) -> str:
    """
    Add a new C# file to Enlisted.csproj compile list.
    
    Args:
        file_path: Path relative to project root (e.g., "src/Features/Content/MyNew.cs")
    
    Returns:
        Success message or error.
    """
    csproj_path = PROJECT_ROOT / "Enlisted.csproj"
    
    if not csproj_path.exists():
        return f"ERROR: Enlisted.csproj not found"
    
    # Normalize path for Windows
    windows_path = file_path.replace("/", "\\")
    
    try:
        content = csproj_path.read_text(encoding='utf-8')
        
        # Check if already included
        if windows_path in content:
            return f"WARNING: File already in csproj: {file_path}"
        
        # Find the last <Compile Include= entry and add after it
        import re
        last_compile = list(re.finditer(r'<Compile Include="[^"]+"\s*/>', content))
        
        if not last_compile:
            return "ERROR: Could not find existing Compile entries in csproj"
        
        last_match = last_compile[-1]
        insert_pos = last_match.end()
        
        new_entry = f'\n    <Compile Include="{windows_path}" />'
        new_content = content[:insert_pos] + new_entry + content[insert_pos:]
        
        csproj_path.write_text(new_content, encoding='utf-8')
        return f"OK: Added to csproj: {file_path}"
    
    except Exception as e:
        return f"ERROR updating csproj: {e}"
