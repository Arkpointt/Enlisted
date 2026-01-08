# Enlisted Knowledge Database

Centralized SQLite database for CrewAI agent knowledge.

## Quick Start

```powershell
# Create/recreate database
cd Tools/CrewAI/database
.\setup.ps1
```

## What's Here

- **`schema.sql`** - Database structure with all tables and seed data
- **`enlisted_knowledge.db`** - SQLite database file (160 KB, tracked in Git for cross-platform use)
- **`setup.ps1`** - Setup script to rebuild the database from schema if needed
- **`.gitignore`** - (All files tracked for cross-platform sync)

## Database Structure

### 12 Tables (360+ total rows)

1. **`tier_definitions`** (9 rows) - Player progression tiers 1-9
2. **`error_catalog`** (40 rows) - Game error codes and fixes
3. **`content_items`** (empty) - Legacy content registry
4. **`valid_categories`** (13 rows) - Event/decision categories
5. **`valid_severities`** (7 rows) - Severity levels
6. **`balance_values`** (30 rows) - Core game balance values
7. **`core_systems`** (7 rows) - Key Bannerlord systems
8. **`system_dependencies`** (12 rows) - System relationships
9. **`api_patterns`** (8 rows) - Common API usage patterns
10. **`implementation_history`** (empty) - Feature implementation tracking
11. **`game_systems`** (empty) - Decompiled game system registry
12. **`content_metadata`** (empty) - Lightweight content index

## Best Practice

The database lives **within the project** and is **tracked in Git** for:
- ✅ Version control (schema AND data synced across platforms)
- ✅ Portability (self-contained project, no setup required)
- ✅ Clear ownership (no global dependencies)
- ✅ Multiple project support (no conflicts)
- ✅ Cross-platform development (Windows/Linux developers get same data)

## Maintenance Tools

All 23 database tools are available through CrewAI agents:

**Query Tools (14):**
- `lookup_error_code` - Find error by code
- `lookup_content_id` - Get content item details
- `search_content` - Find content by type/category
- `get_valid_categories` - List all categories
- `get_valid_severities` - List all severities
- `get_balance_value` - Look up balance value
- `search_balance` - Find balance values by pattern
- `get_balance_by_category` - Get all values in category
- `get_tier_info` - Get tier definition
- `get_all_tiers` - List all 9 tiers
- `lookup_core_system` - Find core system by name
- `get_system_dependencies` - Get system dependencies
- `lookup_api_pattern` - Find API usage pattern
- `check_game_patterns` - Search for code patterns

**Maintenance Tools (9):**
- `add_content_item` - Register new content
- `update_content_item` - Modify existing content
- `delete_content_item` - Remove content (soft delete)
- `add_error_code` - Add new error mapping
- `add_balance_value` - Add new balance value
- `update_balance_value` - Modify balance value
- `add_implementation_record` - Track feature implementation
- `check_database_health` - Verify database integrity
- `sync_content_from_files` - Scan ModuleData for changes

## Auto-sync

The database can auto-sync with your ModuleData files:

```python
from enlisted_crew.tools.database_tools import sync_content_from_files
result = sync_content_from_files()
print(result)  # "Sync complete: 5 added, 2 updated, 1 marked deleted"
```

This keeps `content_metadata` in sync with actual JSON event/decision/order files.
