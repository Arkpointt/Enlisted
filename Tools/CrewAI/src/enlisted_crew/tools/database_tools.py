"""
Database query tools for CrewAI agents.

Provides structured access to project knowledge stored in SQLite.
All data verified against actual codebase (January 2026).
"""

import sqlite3
from pathlib import Path
from typing import Optional, List
from crewai.tools import tool


# Database location
DB_PATH = Path(r"C:\Dev\SQLite3\enlisted_knowledge.db")


def _get_connection() -> sqlite3.Connection:
    """Get database connection with row factory."""
    if not DB_PATH.exists():
        raise FileNotFoundError(
            f"Database not found at {DB_PATH}. "
            "Run: cd Tools/CrewAI && powershell ./setup_database.ps1"
        )
    conn = sqlite3.connect(DB_PATH)
    conn.row_factory = sqlite3.Row
    return conn


def _format_results(rows: List[sqlite3.Row]) -> str:
    """Format query results as readable text."""
    if not rows:
        return "No results found."
    
    # Convert to list of dicts
    results = [dict(row) for row in rows]
    
    # Format as table-like text
    output = []
    for i, row in enumerate(results, 1):
        output.append(f"\n[{i}]")
        for key, value in row.items():
            if value is not None:
                output.append(f"  {key}: {value}")
    
    return "\n".join(output)


@tool("lookup_content_id")
def lookup_content_id(content_id: str) -> str:
    """
    Look up a content item (event, decision, or order) by ID.
    
    Args:
        content_id: The content ID to look up (e.g., "equipment_quality_inspection")
    
    Returns:
        Details about the content item or "Not found" if it doesn't exist.
    
    Example:
        result = lookup_content_id("equipment_quality_inspection")
    """
    try:
        conn = _get_connection()
        cursor = conn.cursor()
        
        cursor.execute("""
            SELECT * FROM content_items 
            WHERE id = ? OR id LIKE ?
        """, (content_id, f"%{content_id}%"))
        
        rows = cursor.fetchall()
        conn.close()
        
        if not rows:
            return f"Content ID '{content_id}' not found in database."
        
        return _format_results(rows)
    except Exception as e:
        return f"Error querying database: {str(e)}"


@tool("search_content")
def search_content(
    content_type: Optional[str] = None,
    category: Optional[str] = None,
    status: str = "active"
) -> str:
    """
    Search for content items by type and category.
    
    Args:
        content_type: Filter by type ("event", "decision", "order") - optional
        category: Filter by category ("camp_life", "crisis", etc.) - optional
        status: Filter by status (default: "active")
    
    Returns:
        List of matching content items.
    
    Example:
        result = search_content(content_type="event", category="camp_life")
    """
    try:
        conn = _get_connection()
        cursor = conn.cursor()
        
        query = "SELECT id, type, category, title, tier_variant FROM content_items WHERE status = ?"
        params = [status]
        
        if content_type:
            query += " AND type = ?"
            params.append(content_type)
        
        if category:
            query += " AND category = ?"
            params.append(category)
        
        query += " ORDER BY type, category, id LIMIT 50"
        
        cursor.execute(query, params)
        rows = cursor.fetchall()
        conn.close()
        
        return _format_results(rows)
    except Exception as e:
        return f"Error querying database: {str(e)}"


@tool("get_balance_value")
def get_balance_value(key: str) -> str:
    """
    Get a game balance value by key.
    
    Args:
        key: The balance value key (e.g., "tier_5_xp_threshold")
    
    Returns:
        The balance value with units and description.
    
    Example:
        result = get_balance_value("tier_5_xp_threshold")
    """
    try:
        conn = _get_connection()
        cursor = conn.cursor()
        
        cursor.execute("""
            SELECT * FROM balance_values 
            WHERE key = ? OR key LIKE ?
        """, (key, f"%{key}%"))
        
        rows = cursor.fetchall()
        conn.close()
        
        if not rows:
            return f"Balance value '{key}' not found in database."
        
        return _format_results(rows)
    except Exception as e:
        return f"Error querying database: {str(e)}"


@tool("search_balance")
def search_balance(category: Optional[str] = None) -> str:
    """
    Search for balance values by category.
    
    Args:
        category: Filter by category ("tier", "economy", "morale", "supply") - optional
    
    Returns:
        List of balance values in the category.
    
    Example:
        result = search_balance(category="tier")
    """
    try:
        conn = _get_connection()
        cursor = conn.cursor()
        
        if category:
            cursor.execute("""
                SELECT key, value, unit, description FROM balance_values 
                WHERE category = ?
                ORDER BY key
            """, (category,))
        else:
            cursor.execute("""
                SELECT key, value, unit, category, description FROM balance_values 
                ORDER BY category, key
                LIMIT 50
            """)
        
        rows = cursor.fetchall()
        conn.close()
        
        return _format_results(rows)
    except Exception as e:
        return f"Error querying database: {str(e)}"


@tool("lookup_error_code")
def lookup_error_code(error_code: str) -> str:
    """
    Look up an error code to find its meaning and solution.
    
    Args:
        error_code: The error code (e.g., "E-CAMPUI-042") or prefix (e.g., "E-CAMPUI")
    
    Returns:
        Error details including common causes and solutions.
    
    Example:
        result = lookup_error_code("E-CAMPUI-042")
    """
    try:
        conn = _get_connection()
        cursor = conn.cursor()
        
        cursor.execute("""
            SELECT * FROM error_catalog 
            WHERE error_code = ? OR error_code LIKE ?
        """, (error_code, f"{error_code}%"))
        
        rows = cursor.fetchall()
        conn.close()
        
        if not rows:
            return f"Error code '{error_code}' not found in database."
        
        return _format_results(rows)
    except Exception as e:
        return f"Error querying database: {str(e)}"


@tool("get_tier_info")
def get_tier_info(tier: int) -> str:
    """
    Get information about a specific player progression tier.
    
    Args:
        tier: The tier number (1-10)
    
    Returns:
        Tier details including XP range, rank name, and features.
    
    Example:
        result = get_tier_info(5)
    """
    try:
        conn = _get_connection()
        cursor = conn.cursor()
        
        cursor.execute("SELECT * FROM tier_definitions WHERE tier = ?", (tier,))
        rows = cursor.fetchall()
        conn.close()
        
        if not rows:
            return f"Tier {tier} not found in database."
        
        return _format_results(rows)
    except Exception as e:
        return f"Error querying database: {str(e)}"


@tool("get_system_dependencies")
def get_system_dependencies(system_name: str) -> str:
    """
    Get dependencies for a system (what it depends on or what depends on it).
    
    Args:
        system_name: The system name (e.g., "EnlistmentBehavior")
    
    Returns:
        List of dependencies and dependents.
    
    Example:
        result = get_system_dependencies("EnlistmentBehavior")
    """
    try:
        conn = _get_connection()
        cursor = conn.cursor()
        
        # What this system depends on
        cursor.execute("""
            SELECT 'depends_on' as direction, depends_on as related_system, dependency_type, description
            FROM system_dependencies 
            WHERE system_name LIKE ?
        """, (f"%{system_name}%",))
        depends_on = cursor.fetchall()
        
        # What depends on this system
        cursor.execute("""
            SELECT 'depended_by' as direction, system_name as related_system, dependency_type, description
            FROM system_dependencies 
            WHERE depends_on LIKE ?
        """, (f"%{system_name}%",))
        depended_by = cursor.fetchall()
        
        conn.close()
        
        results = []
        if depends_on:
            results.append(f"\n{system_name} DEPENDS ON:")
            results.append(_format_results(depends_on))
        if depended_by:
            results.append(f"\n\nSYSTEMS THAT DEPEND ON {system_name}:")
            results.append(_format_results(depended_by))
        
        if not results:
            return f"No dependency information found for '{system_name}'."
        
        return "\n".join(results)
    except Exception as e:
        return f"Error querying database: {str(e)}"


@tool("lookup_api_pattern")
def lookup_api_pattern(api_name: str) -> str:
    """
    Look up a Bannerlord API usage pattern.
    
    Args:
        api_name: The API name (e.g., "GiveGoldAction" or "TextObject")
    
    Returns:
        Usage pattern, description, and common mistakes.
    
    Example:
        result = lookup_api_pattern("GiveGoldAction")
    """
    try:
        conn = _get_connection()
        cursor = conn.cursor()
        
        cursor.execute("""
            SELECT * FROM api_patterns 
            WHERE api_name LIKE ?
            ORDER BY api_name
        """, (f"%{api_name}%",))
        
        rows = cursor.fetchall()
        conn.close()
        
        if not rows:
            return f"No API patterns found for '{api_name}'."
        
        return _format_results(rows)
    except Exception as e:
        return f"Error querying database: {str(e)}"


@tool("record_implementation")
def record_implementation(
    feature_name: str,
    plan_file: Optional[str] = None,
    files_added: Optional[str] = None,
    files_modified: Optional[str] = None,
    content_ids: Optional[str] = None,
    notes: Optional[str] = None
) -> str:
    """
    Record a new implementation in the history.
    
    Args:
        feature_name: Name of the feature implemented
        plan_file: Path to planning document (optional)
        files_added: Comma-separated list of new files (optional)
        files_modified: Comma-separated list of modified files (optional)
        content_ids: Comma-separated list of event/decision IDs (optional)
        notes: Any important notes (optional)
    
    Returns:
        Confirmation message.
    
    Example:
        result = record_implementation(
            feature_name="Supply Pressure Arc",
            plan_file="docs/CrewAI_Plans/supply-pressure-arc.md",
            content_ids="supply_low_t1,supply_low_t2,supply_low_t3"
        )
    """
    try:
        from datetime import datetime
        
        conn = _get_connection()
        cursor = conn.cursor()
        
        cursor.execute("""
            INSERT INTO implementation_history 
            (feature_name, plan_file, date_implemented, implemented_by, 
             files_added, files_modified, content_ids, notes)
            VALUES (?, ?, ?, 'crewai', ?, ?, ?, ?)
        """, (
            feature_name,
            plan_file,
            datetime.now().isoformat(),
            files_added,
            files_modified,
            content_ids,
            notes
        ))
        
        conn.commit()
        conn.close()
        
        return f"Implementation recorded: {feature_name}"
    except Exception as e:
        return f"Error recording implementation: {str(e)}"


@tool("get_valid_categories")
def get_valid_categories() -> str:
    """
    Get all valid content categories for events, decisions, and orders.
    
    Returns:
        List of valid categories with descriptions.
    
    Example:
        result = get_valid_categories()
    """
    try:
        conn = _get_connection()
        cursor = conn.cursor()
        
        cursor.execute("""
            SELECT category, description, used_in 
            FROM valid_categories 
            ORDER BY category
        """)
        
        rows = cursor.fetchall()
        conn.close()
        
        return _format_results(rows)
    except Exception as e:
        return f"Error querying database: {str(e)}"


@tool("get_valid_severities")
def get_valid_severities() -> str:
    """
    Get all valid severity levels for events and content.
    
    Returns:
        List of valid severities with descriptions.
    
    Example:
        result = get_valid_severities()
    """
    try:
        conn = _get_connection()
        cursor = conn.cursor()
        
        cursor.execute("""
            SELECT severity, description, used_in 
            FROM valid_severities 
            ORDER BY severity
        """)
        
        rows = cursor.fetchall()
        conn.close()
        
        return _format_results(rows)
    except Exception as e:
        return f"Error querying database: {str(e)}"


@tool("lookup_core_system")
def lookup_core_system(system_name: str) -> str:
    """
    Look up a core system's details and responsibilities.
    
    Args:
        system_name: The system name (e.g., "EnlistmentBehavior", "ContentOrchestrator")
    
    Returns:
        System details including file path, type, and key responsibilities.
    
    Example:
        result = lookup_core_system("EnlistmentBehavior")
    """
    try:
        conn = _get_connection()
        cursor = conn.cursor()
        
        cursor.execute("""
            SELECT * FROM core_systems 
            WHERE name LIKE ? OR full_name LIKE ?
            ORDER BY name
        """, (f"%{system_name}%", f"%{system_name}%"))
        
        rows = cursor.fetchall()
        conn.close()
        
        if not rows:
            return f"No core system found matching '{system_name}'."
        
        return _format_results(rows)
    except Exception as e:
        return f"Error querying database: {str(e)}"


@tool("get_all_tiers")
def get_all_tiers() -> str:
    """
    Get the complete tier progression table showing all tiers, XP requirements, and tracks.
    
    Returns:
        Complete tier progression table.
    
    Example:
        result = get_all_tiers()
    """
    try:
        conn = _get_connection()
        cursor = conn.cursor()
        
        cursor.execute("""
            SELECT tier, rank_name, xp_required, track, description 
            FROM tier_definitions 
            ORDER BY tier
        """)
        
        rows = cursor.fetchall()
        conn.close()
        
        return _format_results(rows)
    except Exception as e:
        return f"Error querying database: {str(e)}"


@tool("get_balance_by_category")
def get_balance_by_category(category: str) -> str:
    """
    Get all balance values for a specific category.
    
    Args:
        category: The category (e.g., "economy", "tier", "xp", "progression")
    
    Returns:
        All balance values in that category.
    
    Example:
        result = get_balance_by_category("economy")
    """
    try:
        conn = _get_connection()
        cursor = conn.cursor()
        
        cursor.execute("""
            SELECT key, value, unit, description 
            FROM balance_values 
            WHERE category = ?
            ORDER BY key
        """, (category,))
        
        rows = cursor.fetchall()
        conn.close()
        
        if not rows:
            return f"No balance values found for category '{category}'."
        
        return _format_results(rows)
    except Exception as e:
        return f"Error querying database: {str(e)}"


# =============================================================================
# DATABASE MAINTENANCE TOOLS
# These tools allow the implementation crew to keep the database in sync
# with actual project files when content is added, modified, or deleted.
# =============================================================================


@tool("add_content_item")
def add_content_item(
    content_id: str,
    content_type: str,
    category: str,
    file_path: str,
    title: str,
    severity: str = "normal",
    tier_variant: bool = False,
    status: str = "active"
) -> str:
    """
    Add a new content item to the database.
    
    Args:
        content_id: Unique ID (e.g., "supply_pressure_t1")
        content_type: Type ("event", "decision", "order")
        category: Category ("camp_life", "crisis", "opportunity", etc.)
        file_path: Path to JSON file (e.g., "ModuleData/Enlisted/Events/supply.json")
        title: Human-readable title
        severity: Severity level (default: "normal")
        tier_variant: Whether this has T1/T2/T3 variants (default: False)
        status: Status (default: "active")
    
    Returns:
        Confirmation message.
    
    Example:
        add_content_item(
            content_id="supply_pressure_t1",
            content_type="event",
            category="crisis",
            file_path="ModuleData/Enlisted/Events/pressure_arc.json",
            title="Supply Pressure Stage 1",
            tier_variant=True
        )
    """
    try:
        from datetime import datetime
        
        conn = _get_connection()
        cursor = conn.cursor()
        
        # Check if already exists
        cursor.execute("SELECT id FROM content_metadata WHERE content_id = ?", (content_id,))
        if cursor.fetchone():
            conn.close()
            return f"Content '{content_id}' already exists. Use update_content_item to modify."
        
        cursor.execute("""
            INSERT INTO content_metadata 
            (content_id, type, category, severity, file_path, description, 
             tier_min, tier_max, localization_key)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, (
            content_id,
            content_type,
            category,
            severity,
            file_path,
            title,
            1 if tier_variant else None,
            9 if tier_variant else None,
            content_id
        ))
        
        conn.commit()
        conn.close()
        
        return f"Added content: {content_id} ({content_type}/{category})"
    except Exception as e:
        return f"Error adding content: {str(e)}"


@tool("update_content_item")
def update_content_item(
    content_id: str,
    category: Optional[str] = None,
    severity: Optional[str] = None,
    file_path: Optional[str] = None,
    title: Optional[str] = None,
    status: Optional[str] = None
) -> str:
    """
    Update an existing content item in the database.
    
    Args:
        content_id: The ID of the content to update
        category: New category (optional)
        severity: New severity (optional)
        file_path: New file path (optional)
        title: New title (optional)
        status: New status ("active", "deprecated", "planned") (optional)
    
    Returns:
        Confirmation message.
    
    Example:
        update_content_item(
            content_id="equipment_inspection",
            category="camp_life",
            status="deprecated"
        )
    """
    try:
        conn = _get_connection()
        cursor = conn.cursor()
        
        # Build dynamic update
        updates = []
        params = []
        
        if category:
            updates.append("category = ?")
            params.append(category)
        if severity:
            updates.append("severity = ?")
            params.append(severity)
        if file_path:
            updates.append("file_path = ?")
            params.append(file_path)
        if title:
            updates.append("description = ?")
            params.append(title)
        if status:
            updates.append("status = ?")
            params.append(status)
        
        if not updates:
            return "No fields to update. Provide at least one field."
        
        params.append(content_id)
        
        cursor.execute(f"""
            UPDATE content_metadata 
            SET {", ".join(updates)}
            WHERE content_id = ?
        """, params)
        
        if cursor.rowcount == 0:
            conn.close()
            return f"Content '{content_id}' not found."
        
        conn.commit()
        conn.close()
        
        return f"Updated content: {content_id}"
    except Exception as e:
        return f"Error updating content: {str(e)}"


@tool("delete_content_item")
def delete_content_item(content_id: str) -> str:
    """
    Remove a content item from the database (marks as deleted, doesn't hard delete).
    
    Args:
        content_id: The ID of the content to remove
    
    Returns:
        Confirmation message.
    
    Example:
        delete_content_item("old_event_id")
    """
    try:
        conn = _get_connection()
        cursor = conn.cursor()
        
        # Soft delete - mark as deprecated rather than hard delete
        cursor.execute("""
            UPDATE content_metadata 
            SET status = 'deleted'
            WHERE content_id = ?
        """, (content_id,))
        
        if cursor.rowcount == 0:
            conn.close()
            return f"Content '{content_id}' not found."
        
        conn.commit()
        conn.close()
        
        return f"Deleted content: {content_id}"
    except Exception as e:
        return f"Error deleting content: {str(e)}"


@tool("update_balance_value")
def update_balance_value(
    key: str,
    value: float,
    description: Optional[str] = None
) -> str:
    """
    Update a game balance value.
    
    Args:
        key: The balance key (e.g., "tier_5_xp_threshold")
        value: The new numeric value
        description: Updated description (optional)
    
    Returns:
        Confirmation message.
    
    Example:
        update_balance_value("morale_low_threshold", 25.0)
    """
    try:
        conn = _get_connection()
        cursor = conn.cursor()
        
        if description:
            cursor.execute("""
                UPDATE balance_values 
                SET value = ?, description = ?
                WHERE key = ?
            """, (value, description, key))
        else:
            cursor.execute("""
                UPDATE balance_values 
                SET value = ?
                WHERE key = ?
            """, (value, key))
        
        if cursor.rowcount == 0:
            conn.close()
            return f"Balance key '{key}' not found."
        
        conn.commit()
        conn.close()
        
        return f"Updated balance: {key} = {value}"
    except Exception as e:
        return f"Error updating balance: {str(e)}"


@tool("add_balance_value")
def add_balance_value(
    key: str,
    value: float,
    unit: str,
    category: str,
    description: str
) -> str:
    """
    Add a new balance value to the database.
    
    Args:
        key: Unique key (e.g., "new_threshold")
        value: Numeric value
        unit: Unit type ("xp", "gold", "days", "%", "count")
        category: Category ("tier", "economy", "morale", "supply", "progression")
        description: What this value controls
    
    Returns:
        Confirmation message.
    
    Example:
        add_balance_value(
            key="reputation_gain_major",
            value=15.0,
            unit="points",
            category="reputation",
            description="Rep gained from major positive actions"
        )
    """
    try:
        conn = _get_connection()
        cursor = conn.cursor()
        
        cursor.execute("SELECT key FROM balance_values WHERE key = ?", (key,))
        if cursor.fetchone():
            conn.close()
            return f"Balance key '{key}' already exists. Use update_balance_value."
        
        cursor.execute("""
            INSERT INTO balance_values (key, value, unit, category, description)
            VALUES (?, ?, ?, ?, ?)
        """, (key, value, unit, category, description))
        
        conn.commit()
        conn.close()
        
        return f"Added balance: {key} = {value} {unit}"
    except Exception as e:
        return f"Error adding balance: {str(e)}"


@tool("add_error_code")
def add_error_code(
    error_code: str,
    category: str,
    description: str,
    common_causes: str,
    suggested_solutions: str,
    related_systems: Optional[str] = None
) -> str:
    """
    Add a new error code to the catalog.
    
    Args:
        error_code: The error code (e.g., "E-SUPPLY-001")
        category: Error category (e.g., "SUPPLY", "CAMPUI", "SAVELOAD")
        description: What this error means
        common_causes: Common reasons this error occurs
        suggested_solutions: How to fix this error
        related_systems: Comma-separated list of affected systems (optional)
    
    Returns:
        Confirmation message.
    
    Example:
        add_error_code(
            error_code="E-SUPPLY-001",
            category="SUPPLY",
            description="Supply calculation overflow",
            common_causes="Party too large, negative supply values",
            suggested_solutions="Check party size limits, validate supply before calculations"
        )
    """
    try:
        conn = _get_connection()
        cursor = conn.cursor()
        
        cursor.execute("SELECT error_code FROM error_catalog WHERE error_code = ?", (error_code,))
        if cursor.fetchone():
            conn.close()
            return f"Error code '{error_code}' already exists."
        
        cursor.execute("""
            INSERT INTO error_catalog 
            (error_code, category, description, common_causes, suggested_solutions, related_systems)
            VALUES (?, ?, ?, ?, ?, ?)
        """, (error_code, category, description, common_causes, suggested_solutions, related_systems))
        
        conn.commit()
        conn.close()
        
        return f"Added error code: {error_code}"
    except Exception as e:
        return f"Error adding error code: {str(e)}"


@tool("add_system_dependency")
def add_system_dependency(
    system_name: str,
    depends_on: str,
    dependency_type: str,
    description: str
) -> str:
    """
    Record a dependency between two systems.
    
    Args:
        system_name: The system that has the dependency (e.g., "SupplyManager")
        depends_on: What it depends on (e.g., "EnlistmentBehavior")
        dependency_type: Type ("direct_call", "event_subscription", "config_read")
        description: How/why it depends
    
    Returns:
        Confirmation message.
    
    Example:
        add_system_dependency(
            system_name="SupplyManager",
            depends_on="EnlistmentBehavior",
            dependency_type="direct_call",
            description="Gets company party for supply calculations"
        )
    """
    try:
        conn = _get_connection()
        cursor = conn.cursor()
        
        cursor.execute("""
            INSERT INTO system_dependencies 
            (system_name, depends_on, dependency_type, description)
            VALUES (?, ?, ?, ?)
        """, (system_name, depends_on, dependency_type, description))
        
        conn.commit()
        conn.close()
        
        return f"Added dependency: {system_name} -> {depends_on}"
    except Exception as e:
        return f"Error adding dependency: {str(e)}"


@tool("sync_content_from_files")
def sync_content_from_files() -> str:
    """
    Scan JSON content files and sync to database.
    Adds new content, marks missing content as deleted.
    
    Returns:
        Summary of changes made.
    
    Example:
        sync_content_from_files()
    """
    import json
    from pathlib import Path
    
    try:
        project_root = Path(r"C:\Dev\Enlisted\Enlisted")
        content_dirs = [
            project_root / "ModuleData" / "Enlisted" / "Events",
            project_root / "ModuleData" / "Enlisted" / "Decisions",
            project_root / "ModuleData" / "Enlisted" / "Orders",
        ]
        
        found_ids = set()
        added = 0
        updated = 0
        
        conn = _get_connection()
        cursor = conn.cursor()
        
        for content_dir in content_dirs:
            if not content_dir.exists():
                continue
            
            content_type = content_dir.name.lower().rstrip("s")  # Events -> event
            
            for json_file in content_dir.glob("*.json"):
                try:
                    with open(json_file, "r", encoding="utf-8") as f:
                        data = json.load(f)
                    
                    # Handle both single items and arrays
                    items = data if isinstance(data, list) else [data]
                    
                    for item in items:
                        if "id" not in item:
                            continue
                        
                        content_id = item["id"]
                        found_ids.add(content_id)
                        
                        # Check if exists
                        cursor.execute(
                            "SELECT content_id FROM content_metadata WHERE content_id = ?",
                            (content_id,)
                        )
                        
                        if cursor.fetchone():
                            # Update existing
                            cursor.execute("""
                                UPDATE content_metadata 
                                SET category = ?, severity = ?, file_path = ?
                                WHERE content_id = ?
                            """, (
                                item.get("category", "unknown"),
                                item.get("severity", "normal"),
                                str(json_file.relative_to(project_root)),
                                content_id
                            ))
                            updated += 1
                        else:
                            # Add new
                            cursor.execute("""
                                INSERT INTO content_metadata 
                                (content_id, type, category, severity, file_path, description)
                                VALUES (?, ?, ?, ?, ?, ?)
                            """, (
                                content_id,
                                content_type,
                                item.get("category", "unknown"),
                                item.get("severity", "normal"),
                                str(json_file.relative_to(project_root)),
                                item.get("title", content_id)
                            ))
                            added += 1
                
                except (json.JSONDecodeError, KeyError):
                    continue
        
        # Mark missing content as deleted
        cursor.execute("SELECT content_id FROM content_metadata WHERE status = 'active'")
        existing_ids = {row[0] for row in cursor.fetchall()}
        missing_ids = existing_ids - found_ids
        
        deleted = 0
        for missing_id in missing_ids:
            cursor.execute(
                "UPDATE content_metadata SET status = 'deleted' WHERE content_id = ?",
                (missing_id,)
            )
            deleted += 1
        
        conn.commit()
        conn.close()
        
        return f"Sync complete: {added} added, {updated} updated, {deleted} marked deleted"
    except Exception as e:
        return f"Error syncing content: {str(e)}"
