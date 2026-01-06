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
