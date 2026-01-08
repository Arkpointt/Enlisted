r"""
Bannerlord API MCP Server

Provides semantic access to decompiled Bannerlord source code (Decompile/ in workspace root).
Indexes C# classes, methods, interfaces, and provides structured queries for API verification.

MCP Tools Exposed:
- get_class_definition: Get full class with members
- get_method_signature: Get method parameters and return type
- find_implementations: Find all classes implementing an interface
- find_subclasses: Get class inheritance hierarchy
- search_api: Enhanced search with semantic understanding
- get_namespace_contents: List all classes in a namespace
- validate_api_call: Check if method signature is correct

Usage in CrewAI:
    agent = Agent(
        role="C# Implementer",
        mcps=["bannerlord-api"],
        tools=[...],
    )
"""

import asyncio
import sqlite3
import re
from pathlib import Path
from typing import Any, Sequence
from mcp.server.models import InitializationOptions
from mcp.server import NotificationOptions, Server
from mcp.server.stdio import stdio_server
from mcp.types import Tool, TextContent, ImageContent, EmbeddedResource

def _get_project_root() -> Path:
    """Get the Enlisted project root directory."""
    import os
    env_root = os.environ.get("ENLISTED_PROJECT_ROOT")
    if env_root:
        return Path(env_root)
    
    current = Path(__file__).resolve()
    for parent in current.parents:
        if (parent / "Enlisted.csproj").exists():
            return parent
    
    return Path(r"C:\Dev\Enlisted\Enlisted")

def _get_decompile_path() -> Path:
    """Get the Decompile folder path. Checks multiple locations."""
    import os
    
    # 1. Environment variable override
    env_decompile = os.environ.get("BANNERLORD_DECOMPILE_PATH")
    if env_decompile:
        return Path(env_decompile)
    
    # 2. Sibling to project root (standard location)
    project_root = _get_project_root()
    sibling_decompile = project_root.parent / "Decompile"
    if sibling_decompile.exists():
        return sibling_decompile
    
    # 3. Inside workspace (if someone puts it there)
    workspace_decompile = project_root / "Decompile"
    if workspace_decompile.exists():
        return workspace_decompile
    
    # 4. Fallback default
    return Path(r"C:\Dev\Enlisted\Decompile")

# Path to decompiled Bannerlord source
DECOMPILE_PATH = _get_decompile_path()
# Path to index database (created on first run)
INDEX_DB_PATH = Path(__file__).parent / "bannerlord_api_index.db"

# MCP Server instance
server = Server("bannerlord-api")


class BannerlordAPIIndex:
    """
    Indexes and queries decompiled Bannerlord C# source code.
    
    Uses SQLite for fast lookups of classes, methods, properties, and interfaces.
    """
    
    def __init__(self, db_path: Path, decompile_path: Path):
        self.db_path = db_path
        self.decompile_path = decompile_path
        self.conn = None
        
    def connect(self):
        """Connect to SQLite index."""
        self.conn = sqlite3.connect(str(self.db_path))
        self.conn.row_factory = sqlite3.Row
        
    def initialize_schema(self):
        """Create database schema for API index."""
        self.conn.executescript("""
            CREATE TABLE IF NOT EXISTS namespaces (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL UNIQUE,
                file_path TEXT NOT NULL
            );
            
            CREATE TABLE IF NOT EXISTS classes (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                full_name TEXT NOT NULL UNIQUE,
                namespace_id INTEGER,
                file_path TEXT NOT NULL,
                line_number INTEGER,
                is_interface BOOLEAN,
                is_abstract BOOLEAN,
                is_static BOOLEAN,
                base_class TEXT,
                modifiers TEXT,
                definition TEXT,
                FOREIGN KEY (namespace_id) REFERENCES namespaces(id)
            );
            
            CREATE TABLE IF NOT EXISTS methods (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                class_id INTEGER,
                return_type TEXT,
                parameters TEXT,
                modifiers TEXT,
                is_virtual BOOLEAN,
                is_override BOOLEAN,
                is_abstract BOOLEAN,
                line_number INTEGER,
                signature TEXT,
                body_preview TEXT,
                FOREIGN KEY (class_id) REFERENCES classes(id)
            );
            
            CREATE TABLE IF NOT EXISTS properties (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                class_id INTEGER,
                property_type TEXT,
                has_getter BOOLEAN,
                has_setter BOOLEAN,
                modifiers TEXT,
                line_number INTEGER,
                FOREIGN KEY (class_id) REFERENCES classes(id)
            );
            
            CREATE TABLE IF NOT EXISTS interfaces_implemented (
                class_id INTEGER,
                interface_name TEXT,
                FOREIGN KEY (class_id) REFERENCES classes(id)
            );
            
            CREATE TABLE IF NOT EXISTS inheritance (
                child_id INTEGER,
                parent_name TEXT,
                FOREIGN KEY (child_id) REFERENCES classes(id)
            );
            
            CREATE INDEX IF NOT EXISTS idx_classes_name ON classes(name);
            CREATE INDEX IF NOT EXISTS idx_classes_fullname ON classes(full_name);
            CREATE INDEX IF NOT EXISTS idx_methods_name ON methods(name);
            CREATE INDEX IF NOT EXISTS idx_methods_signature ON methods(signature);
            CREATE INDEX IF NOT EXISTS idx_interfaces ON interfaces_implemented(interface_name);
            CREATE INDEX IF NOT EXISTS idx_inheritance ON inheritance(parent_name);
        """)
        self.conn.commit()
        
    def parse_csharp_file(self, file_path: Path) -> dict:
        """
        Parse a C# file to extract classes, methods, properties.
        
        Uses regex-based parsing (faster than Roslyn, good enough for signatures).
        """
        try:
            with open(file_path, 'r', encoding='utf-8', errors='replace') as f:
                content = f.read()
        except Exception as e:
            return {"error": str(e)}
        
        result = {
            "file_path": str(file_path.relative_to(self.decompile_path)),
            "namespace": None,
            "classes": [],
        }
        
        # Extract namespace
        ns_match = re.search(r'namespace\s+([\w\.]+)', content)
        if ns_match:
            result["namespace"] = ns_match.group(1)
        
        # Extract classes (including interfaces, structs, enums)
        class_pattern = r'^\s*(public|internal|private|protected)?\s*(static|abstract|sealed|partial)?\s*(class|interface|struct|enum)\s+(\w+)(?:<[^>]+>)?(?:\s*:\s*([^\{]+))?'
        
        lines = content.split('\n')
        for i, line in enumerate(lines, 1):
            class_match = re.match(class_pattern, line)
            if class_match:
                modifiers = f"{class_match.group(1) or ''} {class_match.group(2) or ''}".strip()
                kind = class_match.group(3)
                name = class_match.group(4)
                inheritance = class_match.group(5)
                
                class_info = {
                    "name": name,
                    "kind": kind,
                    "modifiers": modifiers,
                    "line_number": i,
                    "is_interface": kind == "interface",
                    "is_abstract": "abstract" in modifiers,
                    "is_static": "static" in modifiers,
                    "base_class": None,
                    "interfaces": [],
                }
                
                # Parse inheritance
                if inheritance:
                    parts = [p.strip() for p in inheritance.split(',')]
                    if kind == "interface":
                        class_info["interfaces"] = parts
                    else:
                        class_info["base_class"] = parts[0] if parts else None
                        class_info["interfaces"] = parts[1:] if len(parts) > 1 else []
                
                # Extract methods for this class (simplified - just signatures)
                class_info["methods"] = self._extract_methods(content, i, name)
                class_info["properties"] = self._extract_properties(content, i, name)
                
                result["classes"].append(class_info)
        
        return result
    
    def _extract_methods(self, content: str, start_line: int, class_name: str) -> list:
        """Extract method signatures from class content."""
        methods = []
        lines = content.split('\n')[start_line:]
        
        # Method pattern: modifiers return_type method_name(params)
        method_pattern = r'^\s*(public|private|protected|internal)?\s*(static|virtual|override|abstract|sealed)?\s*([\w<>\[\]]+)\s+(\w+)\s*\(([^\)]*)\)'
        
        brace_count = 0
        in_class = False
        
        for i, line in enumerate(lines, start_line + 1):
            if '{' in line:
                brace_count += line.count('{')
                in_class = True
            if '}' in line:
                brace_count -= line.count('}')
                if brace_count <= 0:
                    break
            
            if not in_class:
                continue
                
            method_match = re.match(method_pattern, line)
            if method_match and brace_count == 1:
                access = method_match.group(1) or "private"
                modifiers = f"{access} {method_match.group(2) or ''}".strip()
                return_type = method_match.group(3)
                name = method_match.group(4)
                params = method_match.group(5)
                
                # Skip constructors, property accessors, etc.
                if name in ['get', 'set', 'add', 'remove'] or name == class_name:
                    continue
                
                methods.append({
                    "name": name,
                    "return_type": return_type,
                    "parameters": params,
                    "modifiers": modifiers,
                    "is_virtual": "virtual" in modifiers,
                    "is_override": "override" in modifiers,
                    "is_abstract": "abstract" in modifiers,
                    "line_number": i,
                    "signature": f"{return_type} {name}({params})",
                })
        
        return methods
    
    def _extract_properties(self, content: str, start_line: int, class_name: str) -> list:
        """Extract property definitions from class content."""
        properties = []
        lines = content.split('\n')[start_line:]
        
        # Property pattern: type Name { get; set; }
        prop_pattern = r'^\s*(public|private|protected|internal)?\s*(static|virtual|override)?\s*([\w<>\[\]]+)\s+(\w+)\s*\{'
        
        brace_count = 0
        in_class = False
        
        for i, line in enumerate(lines, start_line + 1):
            if '{' in line:
                brace_count += line.count('{')
                in_class = True
            if '}' in line:
                brace_count -= line.count('}')
                if brace_count <= 0:
                    break
            
            if not in_class or brace_count != 1:
                continue
                
            prop_match = re.match(prop_pattern, line)
            if prop_match:
                access = prop_match.group(1) or "private"
                modifiers = f"{access} {prop_match.group(2) or ''}".strip()
                prop_type = prop_match.group(3)
                name = prop_match.group(4)
                
                has_getter = 'get' in line
                has_setter = 'set' in line
                
                properties.append({
                    "name": name,
                    "type": prop_type,
                    "modifiers": modifiers,
                    "has_getter": has_getter,
                    "has_setter": has_setter,
                    "line_number": i,
                })
        
        return properties
    
    def index_all_files(self):
        """Index all C# files in the decompile directory."""
        print(f"Indexing Bannerlord API from {self.decompile_path}...")
        
        cs_files = list(self.decompile_path.glob("**/*.cs"))
        total = len(cs_files)
        
        for idx, cs_file in enumerate(cs_files, 1):
            if idx % 100 == 0:
                print(f"  Indexed {idx}/{total} files...")
            
            parsed = self.parse_csharp_file(cs_file)
            if "error" in parsed:
                continue
            
            # Insert namespace
            if parsed["namespace"]:
                self.conn.execute("""
                    INSERT OR IGNORE INTO namespaces (name, file_path)
                    VALUES (?, ?)
                """, (parsed["namespace"], parsed["file_path"]))
                
                ns_id = self.conn.execute(
                    "SELECT id FROM namespaces WHERE name = ?",
                    (parsed["namespace"],)
                ).fetchone()[0]
            else:
                ns_id = None
            
            # Insert classes and their members
            for cls in parsed["classes"]:
                full_name = f"{parsed['namespace']}.{cls['name']}" if parsed["namespace"] else cls["name"]
                
                cursor = self.conn.execute("""
                    INSERT OR REPLACE INTO classes 
                    (name, full_name, namespace_id, file_path, line_number, is_interface, 
                     is_abstract, is_static, base_class, modifiers, definition)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                """, (
                    cls["name"], full_name, ns_id, parsed["file_path"],
                    cls["line_number"], cls["is_interface"], cls["is_abstract"],
                    cls["is_static"], cls["base_class"], cls["modifiers"],
                    f"{cls['kind']} {cls['name']}"
                ))
                
                class_id = cursor.lastrowid
                
                # Insert methods
                for method in cls["methods"]:
                    self.conn.execute("""
                        INSERT INTO methods 
                        (name, class_id, return_type, parameters, modifiers, 
                         is_virtual, is_override, is_abstract, line_number, signature)
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                    """, (
                        method["name"], class_id, method["return_type"],
                        method["parameters"], method["modifiers"],
                        method["is_virtual"], method["is_override"],
                        method["is_abstract"], method["line_number"],
                        method["signature"]
                    ))
                
                # Insert properties
                for prop in cls["properties"]:
                    self.conn.execute("""
                        INSERT INTO properties
                        (name, class_id, property_type, has_getter, has_setter, modifiers, line_number)
                        VALUES (?, ?, ?, ?, ?, ?, ?)
                    """, (
                        prop["name"], class_id, prop["type"],
                        prop["has_getter"], prop["has_setter"],
                        prop["modifiers"], prop["line_number"]
                    ))
                
                # Insert interfaces
                for interface in cls["interfaces"]:
                    self.conn.execute("""
                        INSERT INTO interfaces_implemented (class_id, interface_name)
                        VALUES (?, ?)
                    """, (class_id, interface.strip()))
                
                # Insert inheritance
                if cls["base_class"]:
                    self.conn.execute("""
                        INSERT INTO inheritance (child_id, parent_name)
                        VALUES (?, ?)
                    """, (class_id, cls["base_class"].strip()))
        
        self.conn.commit()
        print(f"[OK] Indexed {total} files")
    
    def get_class_definition(self, class_name: str) -> dict:
        """Get full class definition with methods and properties."""
        cursor = self.conn.execute("""
            SELECT c.*, n.name as namespace
            FROM classes c
            LEFT JOIN namespaces n ON c.namespace_id = n.id
            WHERE c.name = ? OR c.full_name = ?
            ORDER BY c.full_name
            LIMIT 10
        """, (class_name, class_name))
        
        classes = []
        for row in cursor.fetchall():
            class_info = dict(row)
            class_id = class_info["id"]
            
            # Get methods
            methods = self.conn.execute("""
                SELECT * FROM methods WHERE class_id = ?
            """, (class_id,)).fetchall()
            class_info["methods"] = [dict(m) for m in methods]
            
            # Get properties
            properties = self.conn.execute("""
                SELECT * FROM properties WHERE class_id = ?
            """, (class_id,)).fetchall()
            class_info["properties"] = [dict(p) for p in properties]
            
            # Get interfaces
            interfaces = self.conn.execute("""
                SELECT interface_name FROM interfaces_implemented WHERE class_id = ?
            """, (class_id,)).fetchall()
            class_info["interfaces"] = [i[0] for i in interfaces]
            
            classes.append(class_info)
        
        return {"classes": classes, "count": len(classes)}
    
    def search_api(self, query: str, filter_type: str = "all") -> dict:
        """
        Search API by name with optional filtering.
        
        Args:
            query: Search term
            filter_type: "class", "method", "property", "interface", or "all"
        """
        results = {"classes": [], "methods": [], "properties": []}
        query_pattern = f"%{query}%"
        
        if filter_type in ["class", "all"]:
            cursor = self.conn.execute("""
                SELECT c.name, c.full_name, c.modifiers, c.file_path, c.line_number, n.name as namespace
                FROM classes c
                LEFT JOIN namespaces n ON c.namespace_id = n.id
                WHERE c.name LIKE ? OR c.full_name LIKE ?
                ORDER BY c.name
                LIMIT 50
            """, (query_pattern, query_pattern))
            results["classes"] = [dict(row) for row in cursor.fetchall()]
        
        if filter_type in ["method", "all"]:
            cursor = self.conn.execute("""
                SELECT m.name, m.signature, m.modifiers, c.full_name as class_name, c.file_path
                FROM methods m
                JOIN classes c ON m.class_id = c.id
                WHERE m.name LIKE ? OR m.signature LIKE ?
                ORDER BY m.name
                LIMIT 50
            """, (query_pattern, query_pattern))
            results["methods"] = [dict(row) for row in cursor.fetchall()]
        
        if filter_type in ["property", "all"]:
            cursor = self.conn.execute("""
                SELECT p.name, p.property_type, c.full_name as class_name, c.file_path
                FROM properties p
                JOIN classes c ON p.class_id = c.id
                WHERE p.name LIKE ?
                ORDER BY p.name
                LIMIT 50
            """, (query_pattern,))
            results["properties"] = [dict(row) for row in cursor.fetchall()]
        
        return results
    
    def find_implementations(self, interface_name: str) -> list:
        """Find all classes implementing an interface."""
        cursor = self.conn.execute("""
            SELECT c.name, c.full_name, c.file_path, c.line_number
            FROM classes c
            JOIN interfaces_implemented i ON c.id = i.class_id
            WHERE i.interface_name LIKE ?
            ORDER BY c.full_name
        """, (f"%{interface_name}%",))
        
        return [dict(row) for row in cursor.fetchall()]
    
    def find_subclasses(self, parent_name: str) -> list:
        """Find all classes inheriting from a parent class."""
        cursor = self.conn.execute("""
            SELECT c.name, c.full_name, c.file_path, c.line_number, i.parent_name
            FROM classes c
            JOIN inheritance i ON c.id = i.child_id
            WHERE i.parent_name LIKE ?
            ORDER BY c.full_name
        """, (f"%{parent_name}%",))
        
        return [dict(row) for row in cursor.fetchall()]
    
    def get_namespace_contents(self, namespace_name: str) -> dict:
        """List all classes in a namespace."""
        cursor = self.conn.execute("""
            SELECT c.name, c.full_name, c.modifiers, c.is_interface, c.is_abstract
            FROM classes c
            JOIN namespaces n ON c.namespace_id = n.id
            WHERE n.name = ? OR n.name LIKE ?
            ORDER BY c.name
        """, (namespace_name, f"{namespace_name}.%"))
        
        return {"namespace": namespace_name, "classes": [dict(row) for row in cursor.fetchall()]}


# Global index instance
api_index = BannerlordAPIIndex(INDEX_DB_PATH, DECOMPILE_PATH)


@server.list_tools()
async def handle_list_tools() -> list[Tool]:
    """List all available MCP tools."""
    return [
        Tool(
            name="get_class_definition",
            description="Get full class definition including methods, properties, and interfaces. Returns up to 10 matches.",
            inputSchema={
                "type": "object",
                "properties": {
                    "class_name": {
                        "type": "string",
                        "description": "Class name (e.g., 'Hero', 'CampaignBehaviorBase', 'TaleWorlds.CampaignSystem.Hero')",
                    },
                },
                "required": ["class_name"],
            },
        ),
        Tool(
            name="read_source_code",
            description="Read actual C# source code from decompiled files with context around a specific class or method. Returns real implementation code, not just signatures.",
            inputSchema={
                "type": "object",
                "properties": {
                    "file_path": {
                        "type": "string",
                        "description": "Relative file path from decompile root (e.g., 'TaleWorlds.CampaignSystem/Hero.cs')",
                    },
                    "search_term": {
                        "type": "string",
                        "description": "Optional: Class or method name to find and show context around",
                    },
                    "context_lines": {
                        "type": "integer",
                        "description": "Number of context lines before/after match. Default: 10",
                        "default": 10,
                    },
                },
                "required": ["file_path"],
            },
        ),
        Tool(
            name="find_usage_examples",
            description="Find where a class or method is USED in native code (find method calls, not definitions). Shows real usage patterns.",
            inputSchema={
                "type": "object",
                "properties": {
                    "target": {
                        "type": "string",
                        "description": "What to find usage of (e.g., 'GiveGoldAction.ApplyForParty', 'Hero.AddSkillXp')",
                    },
                    "max_examples": {
                        "type": "integer",
                        "description": "Maximum examples to return. Default: 5",
                        "default": 5,
                    },
                },
                "required": ["target"],
            },
        ),
        Tool(
            name="search_api",
            description="Search Bannerlord API by name. Can filter by class, method, property, or search all. Returns up to 50 results per category.",
            inputSchema={
                "type": "object",
                "properties": {
                    "query": {
                        "type": "string",
                        "description": "Search term (e.g., 'GiveGold', 'MainHero', 'AddXP')",
                    },
                    "filter_type": {
                        "type": "string",
                        "enum": ["all", "class", "method", "property", "interface"],
                        "description": "Filter results by type. Default: 'all'",
                        "default": "all",
                    },
                },
                "required": ["query"],
            },
        ),
        Tool(
            name="find_implementations",
            description="Find all classes implementing an interface (e.g., 'ICampaignBehavior', 'ISaveableType').",
            inputSchema={
                "type": "object",
                "properties": {
                    "interface_name": {
                        "type": "string",
                        "description": "Interface name to search for",
                    },
                },
                "required": ["interface_name"],
            },
        ),
        Tool(
            name="find_subclasses",
            description="Find all classes inheriting from a parent class (e.g., 'CampaignBehaviorBase', 'GameMenuOption').",
            inputSchema={
                "type": "object",
                "properties": {
                    "parent_name": {
                        "type": "string",
                        "description": "Parent class name",
                    },
                },
                "required": ["parent_name"],
            },
        ),
        Tool(
            name="get_namespace_contents",
            description="List all classes in a namespace (e.g., 'TaleWorlds.CampaignSystem', 'TaleWorlds.Core').",
            inputSchema={
                "type": "object",
                "properties": {
                    "namespace_name": {
                        "type": "string",
                        "description": "Full namespace name",
                    },
                },
                "required": ["namespace_name"],
            },
        ),
        Tool(
            name="get_method_signature",
            description="Get detailed method signature including parameters, return type, and modifiers.",
            inputSchema={
                "type": "object",
                "properties": {
                    "class_name": {
                        "type": "string",
                        "description": "Class containing the method",
                    },
                    "method_name": {
                        "type": "string",
                        "description": "Method name to look up",
                    },
                },
                "required": ["class_name", "method_name"],
            },
        ),
    ]


@server.call_tool()
async def handle_call_tool(name: str, arguments: dict) -> Sequence[TextContent | ImageContent | EmbeddedResource]:
    """Handle tool execution."""
    
    try:
        if name == "get_class_definition":
            result = api_index.get_class_definition(arguments["class_name"])
            return [TextContent(type="text", text=format_class_definition(result))]
        
        elif name == "search_api":
            result = api_index.search_api(
                arguments["query"],
                arguments.get("filter_type", "all")
            )
            return [TextContent(type="text", text=format_search_results(result))]
        
        elif name == "find_implementations":
            result = api_index.find_implementations(arguments["interface_name"])
            return [TextContent(type="text", text=format_implementations(result, arguments["interface_name"]))]
        
        elif name == "find_subclasses":
            result = api_index.find_subclasses(arguments["parent_name"])
            return [TextContent(type="text", text=format_subclasses(result, arguments["parent_name"]))]
        
        elif name == "get_namespace_contents":
            result = api_index.get_namespace_contents(arguments["namespace_name"])
            return [TextContent(type="text", text=format_namespace(result))]
        
        elif name == "get_method_signature":
            class_def = api_index.get_class_definition(arguments["class_name"])
            method_name = arguments["method_name"]
            return [TextContent(type="text", text=format_method_signature(class_def, method_name))]
        
        elif name == "read_source_code":
            result = read_source_code(
                arguments["file_path"],
                arguments.get("search_term"),
                arguments.get("context_lines", 10)
            )
            return [TextContent(type="text", text=result)]
        
        elif name == "find_usage_examples":
            result = find_usage_examples(
                arguments["target"],
                arguments.get("max_examples", 5)
            )
            return [TextContent(type="text", text=result)]
        
        else:
            return [TextContent(type="text", text=f"Unknown tool: {name}")]
    
    except Exception as e:
        return [TextContent(type="text", text=f"Error executing {name}: {str(e)}")]


def format_class_definition(result: dict) -> str:
    """Format class definition for readable output."""
    if not result["classes"]:
        return "No classes found"
    
    output = []
    for cls in result["classes"]:
        output.append(f"## {cls['full_name']}")
        output.append(f"**File:** {cls['file_path']}:{cls['line_number']}")
        output.append(f"**Modifiers:** {cls['modifiers']}")
        
        if cls["base_class"]:
            output.append(f"**Inherits:** {cls['base_class']}")
        
        if cls["interfaces"]:
            output.append(f"**Implements:** {', '.join(cls['interfaces'])}")
        
        if cls["methods"]:
            output.append(f"\n**Methods ({len(cls['methods'])}):**")
            for method in cls["methods"][:20]:  # Show first 20
                output.append(f"  - `{method['signature']}` ({method['modifiers']})")
        
        if cls["properties"]:
            output.append(f"\n**Properties ({len(cls['properties'])}):**")
            for prop in cls["properties"][:20]:  # Show first 20
                accessors = []
                if prop["has_getter"]:
                    accessors.append("get")
                if prop["has_setter"]:
                    accessors.append("set")
                output.append(f"  - `{prop['property_type']} {prop['name']}` {{ {'; '.join(accessors)}; }}")
        
        output.append("")
    
    return "\n".join(output)


def format_search_results(result: dict) -> str:
    """Format search results."""
    output = []
    
    if result["classes"]:
        output.append(f"### Classes ({len(result['classes'])}):")
        for cls in result["classes"][:20]:
            output.append(f"  - **{cls['full_name']}** ({cls['modifiers']}) - {cls['file_path']}:{cls['line_number']}")
    
    if result["methods"]:
        output.append(f"\n### Methods ({len(result['methods'])}):")
        for method in result["methods"][:20]:
            output.append(f"  - **{method['class_name']}.{method['name']}**")
            output.append(f"    `{method['signature']}`")
            output.append(f"    {method['file_path']}")
    
    if result["properties"]:
        output.append(f"\n### Properties ({len(result['properties'])}):")
        for prop in result["properties"][:20]:
            output.append(f"  - **{prop['class_name']}.{prop['name']}** : {prop['property_type']}")
    
    if not output:
        return "No results found"
    
    return "\n".join(output)


def format_implementations(result: list, interface_name: str) -> str:
    """Format interface implementations."""
    if not result:
        return f"No implementations found for {interface_name}"
    
    output = [f"### Classes implementing {interface_name} ({len(result)}):"]
    for cls in result:
        output.append(f"  - **{cls['full_name']}** - {cls['file_path']}:{cls['line_number']}")
    
    return "\n".join(output)


def format_subclasses(result: list, parent_name: str) -> str:
    """Format subclass hierarchy."""
    if not result:
        return f"No subclasses found for {parent_name}"
    
    output = [f"### Classes inheriting from {parent_name} ({len(result)}):"]
    for cls in result:
        output.append(f"  - **{cls['full_name']}** (extends {cls['parent_name']}) - {cls['file_path']}:{cls['line_number']}")
    
    return "\n".join(output)


def format_namespace(result: dict) -> str:
    """Format namespace contents."""
    if not result["classes"]:
        return f"No classes found in namespace {result['namespace']}"
    
    output = [f"### {result['namespace']} ({len(result['classes'])} classes):"]
    for cls in result["classes"]:
        kind = "interface" if cls["is_interface"] else ("abstract" if cls["is_abstract"] else "class")
        output.append(f"  - **{cls['name']}** ({kind}) - {cls['modifiers']}")
    
    return "\n".join(output)


def format_method_signature(class_def: dict, method_name: str) -> str:
    """Format method signature details."""
    if not class_def["classes"]:
        return "Class not found"
    
    output = []
    for cls in class_def["classes"]:
        matching_methods = [m for m in cls["methods"] if m["name"] == method_name]
        if matching_methods:
            output.append(f"### {cls['full_name']}.{method_name}")
            for method in matching_methods:
                output.append(f"**Signature:** `{method['signature']}`")
                output.append(f"**Modifiers:** {method['modifiers']}")
                output.append(f"**Return Type:** {method['return_type']}")
                output.append(f"**Parameters:** {method['parameters']}")
                output.append(f"**Virtual:** {method['is_virtual']}")
                output.append(f"**Override:** {method['is_override']}")
                output.append(f"**Abstract:** {method['is_abstract']}")
                output.append(f"**Location:** {cls['file_path']}:{method['line_number']}")
                output.append("")
    
    if not output:
        return f"Method {method_name} not found in any matched class"
    
    return "\n".join(output)


def read_source_code(file_path: str, search_term: str = None, context_lines: int = 10) -> str:
    """
    Read actual C# source code from decompiled files.
    
    Args:
        file_path: Relative path from decompile root
        search_term: Optional class/method to find and show context around
        context_lines: Lines of context before/after match
    
    Returns:
        Source code with line numbers
    """
    full_path = DECOMPILE_PATH / file_path
    
    if not full_path.exists():
        # Try finding the file
        candidates = list(DECOMPILE_PATH.glob(f"**/{Path(file_path).name}"))
        if not candidates:
            return f"File not found: {file_path}"
        full_path = candidates[0]
    
    try:
        with open(full_path, 'r', encoding='utf-8', errors='replace') as f:
            lines = f.readlines()
    except Exception as e:
        return f"Error reading file: {e}"
    
    if not search_term:
        # Return whole file with line numbers (limit to 500 lines)
        output = [f"### {full_path.relative_to(DECOMPILE_PATH)}"]
        output.append(f"Total lines: {len(lines)}\n")
        for i, line in enumerate(lines[:500], 1):
            output.append(f"{i:4d} | {line.rstrip()}")
        if len(lines) > 500:
            output.append(f"\n... ({len(lines) - 500} more lines truncated)")
        return "\n".join(output)
    
    # Search for term and show context
    search_lower = search_term.lower()
    output = [f"### {full_path.relative_to(DECOMPILE_PATH)}"]
    output.append(f"Searching for: '{search_term}'\n")
    
    matches = []
    for i, line in enumerate(lines):
        if search_lower in line.lower():
            start = max(0, i - context_lines)
            end = min(len(lines), i + context_lines + 1)
            context = []
            for j in range(start, end):
                marker = ">>>" if j == i else "   "
                context.append(f"{marker} {j+1:4d} | {lines[j].rstrip()}")
            matches.append("\n".join(context))
            
            if len(matches) >= 5:
                break
    
    if not matches:
        return f"No matches found for '{search_term}' in {file_path}"
    
    output.append("\n\n---\n\n".join(matches))
    return "\n".join(output)


def find_usage_examples(target: str, max_examples: int = 5) -> str:
    """
    Find where a class or method is USED in native code.
    
    Args:
        target: What to find usage of (e.g., 'GiveGoldAction.ApplyForParty')
        max_examples: Maximum examples to return
    
    Returns:
        Usage examples with context
    """
    target_lower = target.lower()
    results = []
    
    # Search for usage in decompile
    for cs_file in DECOMPILE_PATH.glob("**/*.cs"):
        if len(results) >= max_examples:
            break
        
        try:
            with open(cs_file, 'r', encoding='utf-8', errors='replace') as f:
                lines = f.readlines()
            
            for i, line in enumerate(lines):
                if target_lower in line.lower() and not line.strip().startswith("//"):
                    # Skip definitions (class/method declarations)
                    if re.match(r'^\s*(public|private|protected|internal)?\s*(static|virtual|override|abstract)?\s*\w+\s+' + re.escape(target.split('.')[-1]), line):
                        continue
                    
                    # Show context around usage
                    start = max(0, i - 3)
                    end = min(len(lines), i + 4)
                    context = []
                    for j in range(start, end):
                        marker = ">>>" if j == i else "   "
                        context.append(f"{marker} {j+1:4d} | {lines[j].rstrip()}")
                    
                    rel_path = cs_file.relative_to(DECOMPILE_PATH)
                    results.append(f"**{rel_path}:{i+1}**\n" + "\n".join(context))
                    
                    if len(results) >= max_examples:
                        break
        except Exception:
            continue
    
    if not results:
        return f"No usage examples found for '{target}'"
    
    output = [f"### Usage Examples for '{target}' ({len(results)} found)\n"]
    output.append("\n\n---\n\n".join(results))
    return "\n".join(output)


async def main():
    """Start the MCP server."""
    # Initialize index
    api_index.connect()
    
    if not INDEX_DB_PATH.exists():
        print("Index not found. Building index (this may take a few minutes)...")
        api_index.initialize_schema()
        api_index.index_all_files()
        print("✓ Index built successfully")
    else:
        print(f"Using existing index at {INDEX_DB_PATH}")
    
    # Start server
    async with stdio_server() as (read_stream, write_stream):
        await server.run(
            read_stream,
            write_stream,
            InitializationOptions(
                server_name="bannerlord-api",
                server_version="1.0.0",
                capabilities=server.get_capabilities(
                    notification_options=NotificationOptions(),
                    experimental_capabilities={},
                ),
            ),
        )


if __name__ == "__main__":
    asyncio.run(main())
