"""
Documentation Tools for Enlisted CrewAI

Tools for reading and searching project documentation.
"""

import os
from pathlib import Path
from typing import Dict, List, Optional, Any
from crewai.tools import tool


# === Search Result Cache ===
# Prevents LLM from wasting tokens by re-searching the same queries
class SearchCache:
    """Module-level cache to deduplicate repeated searches within a session."""
    _cache: Dict[str, str] = {}
    _hit_count: Dict[str, int] = {}
    
    @classmethod
    def get(cls, cache_key: str) -> Optional[str]:
        """Get cached result, returning None if not found."""
        if cache_key in cls._cache:
            cls._hit_count[cache_key] = cls._hit_count.get(cache_key, 0) + 1
            hits = cls._hit_count[cache_key]
            return f"[CACHED - searched {hits}x before]\n{cls._cache[cache_key]}"
        return None
    
    @classmethod
    def set(cls, cache_key: str, result: str) -> str:
        """Store result in cache and return it."""
        cls._cache[cache_key] = result
        cls._hit_count[cache_key] = 0
        return result
    
    @classmethod
    def clear(cls) -> None:
        """Clear all cached results (call between flow runs if needed)."""
        cls._cache.clear()
        cls._hit_count.clear()
    
    @classmethod
    def stats(cls) -> str:
        """Return cache statistics."""
        total_queries = len(cls._cache)
        total_hits = sum(cls._hit_count.values())
        return f"SearchCache: {total_queries} unique queries, {total_hits} cache hits"


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

# Content and module data paths
MODULE_DATA_PATH = PROJECT_ROOT / "ModuleData"

# Runtime and development paths
BANNERLORD_INSTALL = Path(r"C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord")
MOD_DEBUGGING_PATH = BANNERLORD_INSTALL / "Modules" / "Enlisted" / "Debugging"
NATIVE_CRASH_LOGS_PATH = Path(r"C:\ProgramData\Mount and Blade II Bannerlord\logs")
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

NATIVE_DECOMPILE_PATH = _get_decompile_path()


@tool("Read Project Documentation")
def read_doc_tool(doc_path: str) -> str:
    """
    Read a project documentation file.
    
    Args:
        doc_path: Path to doc file, relative to project root or docs/ folder.
                  Examples: "BLUEPRINT.md", "docs/INDEX.md", "WARP.md",
                  "Features/Core/enlistment.md", "ANEWFEATURE/systems-integration-analysis.md"
    
    Returns:
        Contents of the documentation file.
    """
    # Normalize path separators
    doc_path = doc_path.replace("\\", "/")
    
    # Try various path resolutions (most specific to most general)
    possible_paths = [
        PROJECT_ROOT / doc_path,
        PROJECT_ROOT / "docs" / doc_path,
        PROJECT_ROOT / "docs" / "Features" / doc_path,
        PROJECT_ROOT / "docs" / "Features" / "Core" / doc_path,
        PROJECT_ROOT / "docs" / "Features" / "Content" / doc_path,
        PROJECT_ROOT / "docs" / "Features" / "Campaign" / doc_path,
        PROJECT_ROOT / "docs" / "ANEWFEATURE" / doc_path,
        PROJECT_ROOT / "docs" / "Reference" / doc_path,
        PROJECT_ROOT / "Tools" / doc_path,
    ]
    
    for path in possible_paths:
        if path.exists() and path.is_file():
            try:
                with open(path, 'r', encoding='utf-8') as f:
                    content = f.read()
                    # Truncate very long docs to avoid token explosion
                    if len(content) > 30000:
                        return f"[Truncated - showing first 30KB of {path.name}]\n\n" + content[:30000]
                    return content
            except Exception as e:
                return f"ERROR reading {path}: {e}"
    
    # Provide helpful suggestions
    error_msg = f"ERROR: Doc '{doc_path}' not found.\n\n"
    error_msg += "Available documentation folders:\n"
    error_msg += "  - docs/Features/Core/ (enlistment, promotion, orders, retinue)\n"
    error_msg += "  - docs/Features/Content/ (events, schemas, writing-style)\n"
    error_msg += "  - docs/Features/Campaign/ (battle, simulation)\n"
    error_msg += "  - docs/ANEWFEATURE/ (systems-integration, content-effects)\n"
    error_msg += "  - docs/Reference/ (native-apis)\n"
    error_msg += "\nUse find_in_docs to find docs by content, or list_docs_tool to browse.\n"
    error_msg += f"\nPaths tried:\n" + "\n".join(f"  - {p}" for p in possible_paths[:5])
    return error_msg


@tool("List Documentation Files")
def list_docs_tool(folder: str = "") -> str:
    """
    List documentation files in the project.
    
    Args:
        folder: Optional subfolder within docs/ to list.
                Examples: "", "Features/Content", "ANEWFEATURE", "Reference"
    
    Returns:
        List of documentation files with brief descriptions.
    """
    docs_dir = PROJECT_ROOT / "docs"
    if folder:
        docs_dir = docs_dir / folder
    
    if not docs_dir.exists():
        return f"ERROR: Folder not found: {docs_dir}"
    
    files = []
    
    # List .md files
    for md_file in sorted(docs_dir.glob("*.md")):
        # Read first line for description
        try:
            with open(md_file, 'r', encoding='utf-8') as f:
                first_line = f.readline().strip()
                # Extract title from markdown header
                title = first_line.lstrip('#').strip() if first_line.startswith('#') else md_file.stem
                files.append(f"  - {md_file.name}: {title}")
        except Exception:
            files.append(f"  - {md_file.name}")
    
    # List subdirectories
    subdirs = []
    for subdir in sorted(docs_dir.iterdir()):
        if subdir.is_dir() and not subdir.name.startswith('.'):
            md_count = len(list(subdir.glob("**/*.md")))
            if md_count > 0:
                subdirs.append(f"  {subdir.name}/ ({md_count} docs)")
    
    result = f"Documentation in {folder or 'docs/'}:\n\n"
    
    if files:
        result += "Files:\n" + "\n".join(files) + "\n\n"
    
    if subdirs:
        result += "Subfolders:\n" + "\n".join(subdirs)
    
    if not files and not subdirs:
        result += "No documentation files found."
    
    return result


@tool("Find in Docs")
def find_in_docs(query: str) -> str:
    """
    Search for text in project documentation.
    
    Args:
        query: Text to search for (case-insensitive).
    
    Returns:
        List of files containing the query with matching snippets.
    """
    # Check cache first to avoid duplicate searches
    cache_key = f"docs:{query.lower()}"
    cached = SearchCache.get(cache_key)
    if cached:
        return cached
    
    docs_dir = PROJECT_ROOT / "docs"
    results = []
    query_lower = query.lower()
    
    # Search in docs/
    for md_file in docs_dir.glob("**/*.md"):
        try:
            with open(md_file, 'r', encoding='utf-8') as f:
                content = f.read()
                if query_lower in content.lower():
                    # Find matching lines
                    lines = content.split('\n')
                    matches = []
                    for i, line in enumerate(lines):
                        if query_lower in line.lower():
                            matches.append(f"    L{i+1}: {line[:80]}...")
                            if len(matches) >= 3:
                                break
                    
                    rel_path = md_file.relative_to(PROJECT_ROOT)
                    results.append(f"FILE: {rel_path}:\n" + "\n".join(matches))
        except Exception:
            continue
    
    # Also search root .md files
    for md_file in PROJECT_ROOT.glob("*.md"):
        try:
            with open(md_file, 'r', encoding='utf-8') as f:
                content = f.read()
                if query_lower in content.lower():
                    lines = content.split('\n')
                    matches = []
                    for i, line in enumerate(lines):
                        if query_lower in line.lower():
                            matches.append(f"    L{i+1}: {line[:80]}...")
                            if len(matches) >= 3:
                                break
                    results.append(f"FILE: {md_file.name}:\n" + "\n".join(matches))
        except Exception:
            continue
    
    # Search in Tools/*.md
    for md_file in (PROJECT_ROOT / "Tools").glob("**/*.md"):
        try:
            with open(md_file, 'r', encoding='utf-8') as f:
                content = f.read()
                if query_lower in content.lower():
                    lines = content.split('\n')
                    matches = []
                    for i, line in enumerate(lines):
                        if query_lower in line.lower():
                            matches.append(f"    L{i+1}: {line[:80]}...")
                            if len(matches) >= 3:
                                break
                    rel_path = md_file.relative_to(PROJECT_ROOT)
                    results.append(f"FILE: {rel_path}:\n" + "\n".join(matches))
        except Exception:
            continue
    
    if not results:
        result = f"No documentation found containing '{query}'"
    else:
        result = f"Found '{query}' in {len(results)} files:\n\n" + "\n\n".join(results[:10])
    
    return SearchCache.set(cache_key, result)


@tool("Get Writing Guide")
def get_writing_guide() -> str:
    """
    Get essential context for content writing tasks.
    
    CALL THIS FIRST when writing events, decisions, or any narrative content.
    Returns a bundle of key references:
    - Writing style guide (voice, tone, vocabulary)
    - Event system schemas (JSON structure requirements)
    - Example events for reference
    
    Returns:
        Combined context from multiple sources of truth.
    """
    context_parts = []
    
    # Writing style guide
    style_path = PROJECT_ROOT / "docs" / "Features" / "Content" / "writing-style-guide.md"
    if style_path.exists():
        with open(style_path, 'r', encoding='utf-8') as f:
            content = f.read()
            # Extract key sections
            context_parts.append("=== WRITING STYLE GUIDE (Key Sections) ===")
            context_parts.append(content[:8000])  # First ~8k chars covers core rules
    
    # Event schemas - just the structure parts
    schema_path = PROJECT_ROOT / "docs" / "Features" / "Content" / "event-system-schemas.md"
    if schema_path.exists():
        with open(schema_path, 'r', encoding='utf-8') as f:
            content = f.read()
            context_parts.append("\n\n=== EVENT SYSTEM SCHEMAS (Structure) ===")
            context_parts.append(content[:6000])  # First ~6k chars covers main schemas
    
    # Content effects reference
    effects_path = PROJECT_ROOT / "docs" / "ANEWFEATURE" / "content-effects-reference.md"
    if effects_path.exists():
        with open(effects_path, 'r', encoding='utf-8') as f:
            content = f.read()
            context_parts.append("\n\n=== CONTENT EFFECTS REFERENCE ===")
            context_parts.append(content[:4000])
    
    context_parts.append("\n\n=== KEY REMINDERS ===")
    context_parts.append("""
- Second person, present tense for setup ("You see...")
- Past tense for results ("You took the gold.")
- NO exclamation marks in narration
- Medieval vocabulary (gold not money, wounded not injured)
- Show physical reactions, not named emotions
- Tooltips: max 80 chars, mechanical description only
- Options: 2-4 per event, never just 1
- Always include fallback option text
""")
    
    return "\n".join(context_parts)


@tool("Get Architecture")
def get_architecture() -> str:
    """
    Get essential context for feature development tasks.
    
    CALL THIS FIRST when designing or implementing new features.
    Returns:
    - Project architecture (BLUEPRINT.md)
    - Systems integration analysis
    - Coding standards and patterns
    
    Returns:
        Combined context for feature development.
    """
    context_parts = []
    
    # Blueprint - architecture and standards
    blueprint_path = PROJECT_ROOT / "BLUEPRINT.md"
    if blueprint_path.exists():
        with open(blueprint_path, 'r', encoding='utf-8') as f:
            content = f.read()
            context_parts.append("=== BLUEPRINT (Architecture & Standards) ===")
            context_parts.append(content[:10000])
    
    # Systems integration
    integration_path = PROJECT_ROOT / "docs" / "ANEWFEATURE" / "systems-integration-analysis.md"
    if integration_path.exists():
        with open(integration_path, 'r', encoding='utf-8') as f:
            content = f.read()
            context_parts.append("\n\n=== SYSTEMS INTEGRATION ANALYSIS ===")
            context_parts.append(content[:8000])
    
    # Content system architecture
    arch_path = PROJECT_ROOT / "docs" / "Features" / "Content" / "content-system-architecture.md"
    if arch_path.exists():
        with open(arch_path, 'r', encoding='utf-8') as f:
            content = f.read()
            context_parts.append("\n\n=== CONTENT SYSTEM ARCHITECTURE ===")
            context_parts.append(content[:6000])
    
    context_parts.append("\n\n=== KEY PATTERNS ===")
    context_parts.append("""
C# Style:
- Allman braces (opening brace on new line)
- _camelCase for private fields
- XML docs on all public methods
- .NET Framework 4.7.2 (no file-scoped namespaces, no records)

Bannerlord Patterns:
- Use GiveGoldAction for gold changes
- Use EscalationManager for reputation changes
- Use TextObject for all user-visible strings
- Check Hero.MainHero != null before access
- Use Campaign.Current?.GetCampaignBehavior<T>()

Files:
- Add new .cs files to Enlisted.csproj
- JSON content in Content/ folder
- Events use event-system-schemas.md structure
""")
    
    return "\n".join(context_parts)


@tool("Get Game Systems")
def get_game_systems() -> str:
    """
    Get comprehensive Enlisted mod domain knowledge.
    
    CALL THIS FIRST for any task involving game mechanics or systems.
    Returns key knowledge about:
    - 9-tier progression system (T1-T9)
    - Reputation tracks (Lord, Officer, Soldier)
    - Company Needs (Supply, Morale, Rest, Readiness)
    - Escalation tracks (Scrutiny, Discipline, Medical Risk)
    - Quartermaster system (equipment, quality, upgrades)
    - Order system (chain of command, phases)
    
    Returns:
        Domain knowledge essential for Enlisted development.
    """
    return """
=== ENLISTED MOD DOMAIN KNOWLEDGE ===

## Core Gameplay Loop
Player enlists with a lord -> follows army (party invisible) -> receives orders -> 
earns XP -> gets promoted (T1->T9) -> eventually discharges or becomes commander.

## 9-Tier Rank Progression (THREE CAREER TRACKS)

| Tier | XP | Title | Track | Key Unlock |
|------|-----|-------|-------|------------|
| T1 | 0 | Follower | Enlisted | Starting rank, basic duties |
| T2 | 800 | Recruit | Enlisted | Formation selection |
| T3 | 3,000 | Free Sword | Enlisted | Basic combat veteran |
| T4 | 6,000 | Veteran | Enlisted | Squad leadership, can train troops |
| T5 | 11,000 | Blade | Officer | NCO authority, specialist missions |
| T6 | 19,000 | Chosen | Officer | Senior NCO, mentoring |
| T7 | 30,000 | Captain | Commander | 20-troop retinue, strategic orders |
| T8 | 45,000 | Commander | Commander | 30-troop retinue |
| T9 | 65,000 | Marshal | Commander | 40-troop retinue (max) |

**Three Career Tracks (CRITICAL for tier-aware content):**
- **Enlisted (T1-T4)**: You're a grunt. Things happen TO you. Auto-assigned duties.
- **Officer (T5-T6)**: You're an NCO. You handle your squad. Mentor recruits, investigate issues.
- **Commander (T7-T9)**: You command troops. Strategic decisions, retinue management.

Promotions require: XP + Days in Rank + Battles + Reputation + Low Discipline

## Three Reputation Tracks
- **Soldier Rep (-50 to +50)**: Peer respect, can be negative (hated) or positive (bonded)
- **Officer Rep (0-100)**: NCO perception, starts neutral, only positive
- **Lord Rep (0-100)**: Lord's trust, starts neutral, only positive

## Escalation Tracks (0-10 scale)
- **Scrutiny**: How closely watched (triggers inspections at 7+)
- **Discipline**: Accumulated infractions (10 = discharge)
- **Medical Risk**: Health vulnerability (illness probability at 3+)

## Company Needs (0-100 scale)
- **Readiness**: Combat preparation
- **Morale**: Unit cohesion, spirits
- **Supplies**: Food, equipment
- **Rest**: Fatigue level

Thresholds: Critical(<20), Low(20-40), Normal(40-70), Good(70-90), Excellent(>90)

## Quartermaster System
- Equipment tiers match player tier (T3 player -> T1-T3 gear)
- Quality modifiers: Poor/Inferior/Common/Fine/Masterwork/Legendary
- Reputation affects: discounts (0-30%), quality distribution, Officers Armory access
- Supply <30% blocks equipment purchases

## Order System (Chain of Command)
- Orders arrive every 3-5 days
- Execute over multiple phases (4 phases/day)
- **T1-T3**: Basic tasks (guard, patrol, firewood) - AUTO-ASSIGNED
- **T4-T6**: Specialist missions (scouting, medical, leading patrols) - NCO AUTHORITY
- **T7-T9**: Strategic directives (command squads) - FULL COMMAND

**NCO Events (T4-T6 only):**
- `evt_nco_new_recruit` - "Your problem now"
- `evt_nco_squad_dispute` - Handle conflicts in YOUR squad
- `evt_nco_equipment_theft` - Investigate YOUR men
- Training chain - You can TRAIN lord's T1-T3 troops

## Content Delivery
- ContentOrchestrator schedules opportunities 24h ahead
- WorldStateAnalyzer determines context (Garrison, Campaign, Siege, etc.)
- Events fire based on day phase (Dawn, Midday, Dusk, Night)
- Camp opportunities: 36 contextual activities

## Discharge Bands
| Band | Trigger | Re-entry |
|------|---------|----------|
| Veteran | 200+ days, T4+ | Start T4, +1000 XP |
| Honorable | 100-199 days | Start T3, +500 XP |
| Washout | <100 days | Start T1, 30-day block |
| Dishonorable | Discipline=10 | Start T1, 90-day block |
| Deserter | Abandoned | Start T1, 90-day block, +crime |
| Grace | Lord died | Full restoration |

## Key Files
- EnlistmentBehavior.cs: Core service state, tier, XP
- PromotionBehavior.cs: Multi-factor requirements
- EscalationManager.cs: Reputation, discipline, scrutiny
- ContentOrchestrator.cs: Content scheduling
- QuartermasterManager.cs: Equipment, quality system
- CompanyNeedsManager.cs: Supply, morale, rest, readiness

## Critical Patterns
- Always use TextObject for user-visible strings
- Check Hero.MainHero != null before access
- Use GiveGoldAction for gold changes
- Use EscalationManager for reputation changes
- Tier gates are 1-indexed (T3 = tier value 3)
- XP thresholds are cumulative totals
"""


@tool("Get Dev Reference")
def get_dev_reference() -> str:
    """
    Get essential context for code analysis and review tasks.
    
    CALL THIS FIRST when analyzing or reviewing C# code.
    Returns patterns, anti-patterns, and Bannerlord API reference.
    
    Returns:
        Combined context for code analysis.
    """
    context_parts = []
    
    # Developer guide
    dev_path = PROJECT_ROOT / "docs" / "DEVELOPER-GUIDE.md"
    if dev_path.exists():
        with open(dev_path, 'r', encoding='utf-8') as f:
            content = f.read()
            context_parts.append("=== DEVELOPER GUIDE ===")
            context_parts.append(content[:8000])
    
    # Native APIs reference
    api_path = PROJECT_ROOT / "docs" / "Reference" / "native-apis.md"
    if api_path.exists():
        with open(api_path, 'r', encoding='utf-8') as f:
            content = f.read()
            context_parts.append("\n\n=== NATIVE BANNERLORD APIs ===")
            context_parts.append(content[:6000])
    
    context_parts.append("\n\n=== COMMON ISSUES TO CHECK ===")
    context_parts.append("""
Critical Patterns:
- SaveableTypeDefiner: All saved types must be registered
- Hero null safety: Always check Hero.MainHero != null
- TextObject: Never concatenate with + (breaks localization)
- Equipment iteration: Use proper slots, not raw indices
- Harmony patches: Always wrap in try-catch

Style Issues:
- K&R braces (wrong - use Allman)
- camelCase fields without underscore (wrong - use _camelCase)
- Missing XML docs on public methods
- C# 9+ features (file-scoped namespaces, records, init accessors)

Performance:
- Avoid LINQ in hot paths
- Cache Campaign.Current lookups
- Don't allocate in Update loops
""")
    
    return "\n".join(context_parts)


@tool("Read Source")
def read_source(file_path: str) -> str:
    """
    Read a C# source file from the project.
    
    Args:
        file_path: Path to C# file, relative to project root or src/ folder.
                   Examples: "src/Features/Content/ContentOrchestrator.cs",
                   "Features/Company/CompanyNeedsManager.cs"
    
    Returns:
        Contents of the C# file.
    """
    # Try various path resolutions
    possible_paths = [
        PROJECT_ROOT / file_path,
        PROJECT_ROOT / "src" / file_path,
        PROJECT_ROOT / "src" / "Features" / file_path,
    ]
    
    for path in possible_paths:
        if path.exists() and path.is_file():
            try:
                with open(path, 'r', encoding='utf-8') as f:
                    return f.read()
            except Exception as e:
                return f"ERROR reading {path}: {e}"
    
    return f"ERROR: File not found. Tried:\n" + "\n".join(str(p) for p in possible_paths)


@tool("Find in Code")
def find_in_code(query: str, max_results: int = 20) -> str:
    """
    Search the src/ C# codebase for a text pattern and return file hits with line snippets.

    Args:
        query: Text to search for (case-insensitive).
        max_results: Maximum number of files to return (default 20).

    Returns:
        Matches grouped by file with up to 3 line snippets per file.
    """
    # Check cache first to avoid duplicate searches
    cache_key = f"csharp:{query.lower()}:{max_results}"
    cached = SearchCache.get(cache_key)
    if cached:
        return cached
    
    src_dir = PROJECT_ROOT / "src"
    if not src_dir.exists():
        return f"ERROR: src folder not found at {src_dir}"

    results = []
    q = query.lower()
    for cs_file in src_dir.glob("**/*.cs"):
        try:
            with open(cs_file, 'r', encoding='utf-8', errors='replace') as f:
                content = f.read()
            if q not in content.lower():
                continue
            lines = content.split('\n')
            matches = []
            for i, line in enumerate(lines):
                if q in line.lower():
                    matches.append(f"  L{i+1}: {line[:100]}")
                    if len(matches) >= 3:
                        break
            rel = cs_file.relative_to(PROJECT_ROOT)
            results.append(f"FILE: {rel}:\n" + "\n".join(matches))
            if len(results) >= max_results:
                break
        except Exception:
            continue

    if not results:
        result = f"No matches for '{query}' in src/"
    else:
        result = f"Found '{query}' in {len(results)} files (showing up to {max_results}):\n\n" + "\n\n".join(results)
    
    return SearchCache.set(cache_key, result)


@tool("Read Source Section")
def read_source_section(
    file_path: str,
    pattern: str = "",
    max_snippets: int = 8,
    lines_before: int = 20,
    lines_after: int = 20,
    max_chars: int = 8000,
) -> str:
    """
    Read only small, relevant excerpts from a C# file.

    Constraints:
    - Returns at most `max_snippets` matched excerpts
    - Each excerpt includes `lines_before/after` context
    - Hard-capped to `max_chars` total output to protect LLM context

    Args:
        file_path: Path relative to project root or src/.
        pattern: Case-insensitive text to locate; if empty, returns file header only.
        max_snippets: Max excerpts to return (default 8).
        lines_before/after: Context lines around each match.
        max_chars: Hard output cap (default 8000 chars).
    """
    # Resolve file
    possible_paths = [
        PROJECT_ROOT / file_path,
        PROJECT_ROOT / "src" / file_path,
        PROJECT_ROOT / "src" / "Features" / file_path,
    ]
    path = None
    for p in possible_paths:
        if p.exists() and p.is_file():
            path = p
            break
    if path is None:
        return f"ERROR: File not found. Tried:\n" + "\n".join(str(p) for p in possible_paths)

    try:
        with open(path, 'r', encoding='utf-8', errors='replace') as f:
            content = f.read()
    except Exception as e:
        return f"ERROR reading {path}: {e}"

    # If no pattern, return only file header
    lines = content.split('\n')
    if not pattern:
        head = "\n".join(lines[: min(50, len(lines))])
        return f"FILE: {path.relative_to(PROJECT_ROOT)} (header only)\n" + head[: max_chars]

    q = pattern.lower()
    excerpts = []
    for i, line in enumerate(lines):
        if q in line.lower():
            start = max(0, i - lines_before)
            end = min(len(lines), i + lines_after + 1)
            block = "\n".join(lines[start:end])
            excerpts.append((i + 1, block))
            if len(excerpts) >= max_snippets:
                break

    if not excerpts:
        return f"No matches for '{pattern}' in {path.relative_to(PROJECT_ROOT)}"

    out_parts = [f"FILE: {path.relative_to(PROJECT_ROOT)}"]
    total = 0
    for ln, block in excerpts:
        header = f"\n--- SNIPPET around L{ln} ---\n"
        chunk = header + block
        if total + len(chunk) > max_chars:
            break
        out_parts.append(chunk)
        total += len(chunk)

    summary = f"\n--- SUMMARY ---\nSnippets: {len(out_parts)-1}, max_chars={max_chars}, pattern='{pattern}'"
    out = "\n".join(out_parts) + summary
    if len(out) > max_chars:
        out = out[:max_chars] + "\n... [truncated by snippet tool]"
    return out


@tool("Read Debug Logs")
def read_debug_logs_tool(session: str = "A") -> str:
    """
    Read Enlisted mod debug logs from the runtime Debugging folder.
    
    Logs are rotated: Session-A (newest), Session-B (previous), Session-C (oldest).
    
    Args:
        session: Which session log to read: "A" (newest), "B", or "C" (oldest).
                 Default is "A" for most recent.
    
    Returns:
        Contents of the debug log file, or list of available logs.
    """
    if not MOD_DEBUGGING_PATH.exists():
        return f"ERROR: Debugging folder not found at {MOD_DEBUGGING_PATH}"
    
    # Find session log files
    session_files = list(MOD_DEBUGGING_PATH.glob(f"Session-{session.upper()}_*.log"))
    
    if not session_files:
        # List what's available
        all_logs = list(MOD_DEBUGGING_PATH.glob("Session-*.log"))
        if all_logs:
            return "Available logs:\n" + "\n".join(f"  - {f.name}" for f in sorted(all_logs))
        return f"No session logs found in {MOD_DEBUGGING_PATH}"
    
    # Read the most recent matching file
    log_file = sorted(session_files)[-1]
    try:
        with open(log_file, 'r', encoding='utf-8', errors='replace') as f:
            content = f.read()
            # Truncate if too long
            if len(content) > 50000:
                return f"[Truncated - showing last 50KB of {log_file.name}]\n\n" + content[-50000:]
            return f"[{log_file.name}]\n\n" + content
    except Exception as e:
        return f"ERROR reading {log_file}: {e}"


@tool("Search Debug Logs")
def search_debug_logs_tool(query: str, error_codes_only: bool = False) -> str:
    """
    Search debug logs for specific text, error codes, or patterns.
    
    Args:
        query: Text to search for (e.g., "E-ENCOUNTER", "NullReference", "ContentOrchestrator")
        error_codes_only: If True, only search for error codes (E-*, W-*)
    
    Returns:
        Matching log lines with context.
    """
    # Check cache first to avoid duplicate searches
    cache_key = f"logs:{query.lower()}:{error_codes_only}"
    cached = SearchCache.get(cache_key)
    if cached:
        return cached
    
    if not MOD_DEBUGGING_PATH.exists():
        return f"ERROR: Debugging folder not found at {MOD_DEBUGGING_PATH}"
    
    results = []
    query_lower = query.lower()
    
    for log_file in sorted(MOD_DEBUGGING_PATH.glob("Session-*.log")):
        try:
            with open(log_file, 'r', encoding='utf-8', errors='replace') as f:
                lines = f.readlines()
                matches = []
                for i, line in enumerate(lines):
                    if error_codes_only:
                        if '[E-' in line or '[W-' in line:
                            matches.append(f"  L{i+1}: {line.strip()[:100]}")
                    elif query_lower in line.lower():
                        matches.append(f"  L{i+1}: {line.strip()[:100]}")
                    
                    if len(matches) >= 20:
                        matches.append("  ... (truncated)")
                        break
                
                if matches:
                    results.append(f"FILE: {log_file.name}:\n" + "\n".join(matches))
        except Exception:
            continue
    
    if not results:
        result = f"No matches for '{query}' in debug logs"
    else:
        result = f"Found '{query}' in logs:\n\n" + "\n\n".join(results)
    
    return SearchCache.set(cache_key, result)


@tool("Read Native Crash Logs")
def read_native_crash_logs_tool(recent_only: bool = True) -> str:
    """
    Read native Bannerlord crash logs from ProgramData.
    
    Use this for hard game crashes (not caught by mod error handling).
    These show native exceptions, access violations, and RGL crashes.
    
    Args:
        recent_only: If True, only read the 3 most recent crash logs.
                     If False, read all crash logs (can be large).
    
    Returns:
        Contents of crash logs with timestamps.
    
    Log Location: C:\\ProgramData\\Mount and Blade II Bannerlord\\logs\\
    """
    if not NATIVE_CRASH_LOGS_PATH.exists():
        return f"ERROR: Native crash logs not found at {NATIVE_CRASH_LOGS_PATH}"
    
    results = []
    
    # Read crashlist.txt first (summary of all crashes)
    crashlist = NATIVE_CRASH_LOGS_PATH / "crashlist.txt"
    if crashlist.exists():
        try:
            with open(crashlist, 'r', encoding='utf-8', errors='replace') as f:
                content = f.read()
                if content.strip():
                    results.append(f"CRASH LIST SUMMARY:\n{content[-5000:]}")
        except Exception as e:
            results.append(f"ERROR reading crashlist.txt: {e}")
    
    # Read individual crash uploader logs
    crash_files = sorted(
        NATIVE_CRASH_LOGS_PATH.glob("CrashUploader.*.txt"),
        key=lambda f: f.stat().st_mtime,
        reverse=True
    )
    
    if recent_only:
        crash_files = crash_files[:3]
    
    for crash_file in crash_files:
        try:
            with open(crash_file, 'r', encoding='utf-8', errors='replace') as f:
                content = f.read()
                # Truncate long files
                if len(content) > 10000:
                    content = content[:10000] + "\n... [truncated]"
                results.append(f"CRASH: {crash_file.name}:\n{content}")
        except Exception as e:
            results.append(f"ERROR reading {crash_file.name}: {e}")
    
    # Also check for rgl_log.txt in game directory
    rgl_log = BANNERLORD_INSTALL / "rgl_log.txt"
    if rgl_log.exists():
        try:
            with open(rgl_log, 'r', encoding='utf-8', errors='replace') as f:
                content = f.read()
                # Just get the last portion
                if len(content) > 5000:
                    content = "... [showing last 5KB]\n" + content[-5000:]
                results.append(f"RGL LOG (game engine):\n{content}")
        except Exception:
            pass
    
    if not results:
        return "No native crash logs found."
    
    return "\n\n" + "="*60 + "\n\n".join(results)


@tool("Find in Native API")
def find_in_native_api(query: str) -> str:
    """
    [DEPRECATED] Search the native Bannerlord decompiled source for API patterns.
    
    PREFER: Use the Bannerlord API MCP server tools instead:
    - get_class_definition(class_name)
    - search_api(query, filter_type)
    - get_method_signature(class_name, method_name)
    
    The MCP server is faster (indexed), provides full signatures, and has semantic understanding.
    This tool remains for backward compatibility only.
    
    Use this to verify how native APIs work before using them.
    The decompile is from Bannerlord v1.3.13 (the target version).
    
    Args:
        query: Class name, method name, or pattern to search for.
                Examples: "GiveGoldAction", "Hero.MainHero", "CampaignBehaviorBase"
    
    Returns:
        Matching code snippets from decompiled source.
    """
    if not NATIVE_DECOMPILE_PATH.exists():
        return f"ERROR: Native decompile not found at {NATIVE_DECOMPILE_PATH}"
    
    results = []
    query_lower = query.lower()
    
    # Search .cs files in decompile folder
    for cs_file in NATIVE_DECOMPILE_PATH.glob("**/*.cs"):
        try:
            with open(cs_file, 'r', encoding='utf-8', errors='replace') as f:
                content = f.read()
                if query_lower in content.lower():
                    # Find matching lines
                    lines = content.split('\n')
                    matches = []
                    for i, line in enumerate(lines):
                        if query_lower in line.lower():
                            # Get some context
                            start = max(0, i - 1)
                            end = min(len(lines), i + 3)
                            context = "\n".join(f"    {j+1}: {lines[j][:80]}" for j in range(start, end))
                            matches.append(context)
                            if len(matches) >= 3:
                                break
                    
                    if matches:
                        rel_path = cs_file.relative_to(NATIVE_DECOMPILE_PATH)
                        results.append(f"FILE: {rel_path}:\n" + "\n---\n".join(matches))
        except Exception:
            continue
        
        if len(results) >= 5:
            results.append("... (more results truncated)")
            break
    
    if not results:
        return f"No matches for '{query}' in native decompile"
    
    return f"Native API matches for '{query}':\n\n" + "\n\n".join(results)


@tool("Verify File Exists")
def verify_file_exists_tool(file_path: str) -> str:
    """
    Verify that a file path exists in the project.
    
    Use this to validate file references BEFORE including them in planning docs.
    Catches hallucinated file names early.
    
    Args:
        file_path: Relative path from project root, e.g. "src/Features/Content/EventEffectApplier.cs"
                   or "ModuleData/Enlisted/Events/some_file.json"
    
    Returns:
        Verification result with exists/not-found status and suggestions if not found.
    """
    full_path = PROJECT_ROOT / file_path
    
    if full_path.exists():
        return f"VERIFIED: VERIFIED: {file_path} exists"
    
    # File not found - try to suggest alternatives
    suggestions = []
    parent = full_path.parent
    if parent.exists():
        # List similar files in the same directory
        stem = full_path.stem.lower()
        for f in parent.iterdir():
            if f.is_file() and (stem in f.stem.lower() or f.stem.lower() in stem):
                suggestions.append(str(f.relative_to(PROJECT_ROOT)))
    
    result = f"NOT FOUND: NOT FOUND: {file_path}"
    if suggestions:
        result += "\n   Similar files found:\n" + "\n".join(f"   - {s}" for s in suggestions[:5])
    else:
        # Maybe the directory doesn't exist
        if not parent.exists():
            result += f"\n   Directory does not exist: {parent.relative_to(PROJECT_ROOT)}"
    
    return result


@tool("List Event IDs")
def list_event_ids(folder: str = "Decisions") -> str:
    """
    List all event/opportunity IDs from JSON content files.
    
    Use this to validate event ID references BEFORE including them in planning docs.
    Prevents hallucinating non-existent event IDs.
    
    Args:
        folder: Which JSON folder to search. One of:
                - "Decisions" - Camp opportunities, decisions (default)
                - "Events" - Event definitions
                - "Orders" - Order events (searches order_events/ subdir)
    
    Returns:
        List of all IDs found in the specified folder.
    """
    json_folder = MODULE_DATA_PATH / "Enlisted"
    
    if folder == "Orders":
        json_folder = json_folder / "Orders" / "order_events"
    else:
        json_folder = json_folder / folder
    
    if not json_folder.exists():
        return f"ERROR: Folder not found: {json_folder.relative_to(PROJECT_ROOT)}"
    
    results = []
    ids_found = []
    
    for json_file in sorted(json_folder.glob("*.json")):
        try:
            with open(json_file, 'r', encoding='utf-8') as f:
                content = f.read()
                # Extract IDs using regex - handles nested arrays
                import re
                ids = re.findall(r'"id"\s*:\s*"([^"]+)"', content)
                if ids:
                    ids_found.extend(ids)
                    results.append(f"FILE: {json_file.name}: {len(ids)} IDs")
        except Exception as e:
            results.append(f"WARNING: {json_file.name}: Error reading - {e}")
    
    summary = f"{folder}/ - {len(ids_found)} total IDs across {len(results)} files\n\n"
    summary += "Files:\n" + "\n".join(results)
    summary += "\n\nAll IDs:\n" + "\n".join(f"  - {id}" for id in sorted(set(ids_found)))
    
    return summary


@tool("List Feature Files")
def list_feature_files_tool(feature: str = "") -> str:
    """
    List C# files in a feature folder.
    
    Args:
        feature: Feature folder name (e.g., "Content", "Company", "Escalation").
                 Leave empty to list all feature folders.
    
    Returns:
        List of C# files in the feature folder.
    """
    features_dir = PROJECT_ROOT / "src" / "Features"
    
    if not feature:
        # List all feature folders
        folders = []
        for f in sorted(features_dir.iterdir()):
            if f.is_dir():
                cs_count = len(list(f.glob("*.cs")))
                folders.append(f"  {f.name}/ ({cs_count} files)")
        return "Feature folders:\n" + "\n".join(folders)
    
    feature_dir = features_dir / feature
    if not feature_dir.exists():
        return f"ERROR: Feature folder not found: {feature}"
    
    files = []
    for cs_file in sorted(feature_dir.glob("*.cs")):
        # Get first class/interface name
        try:
            with open(cs_file, 'r', encoding='utf-8') as f:
                for line in f:
                    if 'class ' in line or 'interface ' in line:
                        files.append(f"  - {cs_file.name}")
                        break
                else:
                    files.append(f"  - {cs_file.name}")
        except Exception:
            files.append(f"  - {cs_file.name}")
    
    return f"Files in {feature}:\n" + "\n".join(files)
