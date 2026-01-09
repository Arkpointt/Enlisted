"""
Static Prompt Templates for OpenAI Prompt Caching

These templates are designed to be cached by OpenAI's prompt caching system.
Each template is 1024+ tokens to trigger automatic caching.

Templates are prepended to task descriptions, with dynamic content appended at the end.
"""

# =============================================================================
# ARCHITECTURE PATTERNS (Static - Cacheable)
# =============================================================================

ARCHITECTURE_PATTERNS = """
=== ENLISTED PROJECT ARCHITECTURE ===

**Code Style Requirements:**
- C# Style: Allman braces (opening brace on new line), _camelCase for private fields, PascalCase for public
- XML Documentation: All public methods must have XML doc comments
- Null Safety: Always check Hero.MainHero != null before accessing
- Error Handling: Use try-catch for external API calls, log with ModLogger

**Bannerlord API Patterns:**
- Hero Management: Campaign.Current?.GetCampaignBehavior<T>() for behavior access
- Gold Changes: Use GiveGoldAction.ApplyForCharacterToSettlement() NOT Hero.Gold += amount
- Text Objects: Use TextObject for all UI strings, never string concatenation
- Party Operations: Check MobileParty.IsActive before accessing
- Equipment: Use EquipmentElement, never manipulate Equipment.Item directly

**Enlisted-Specific Patterns:**
- Tier System: 9 tiers (T1-T9), check PlayerProgressionBehavior.CurrentTier
- Content IDs: Use snake_case format (evt_battle_aftermath, decision_desert)
- Logging: Use ModLogger.Log(level, code, message) with error codes (E-SYSTEM-NNN)
- Save/Load: All state must be serializable, test with SaveBehaviorData/LoadBehaviorData

**File Organization:**
- New C# files: Add to Enlisted.csproj with <Compile Include="src/path/File.cs"/>
- JSON content: ModuleData/Enlisted/Events/ or /Decisions/ or /Orders/
- Localization: Update enlisted_strings.xml with sync_event_strings.py
- Documentation: Update docs/Features/ and docs/INDEX.md

**Common Pitfalls to Avoid:**
- DON'T modify Hero.Gold directly - use GiveGoldAction
- DON'T assume Settlement.Town != null - check first
- DON'T use string concatenation for UI text - use TextObject
- DON'T create content IDs with camelCase - use snake_case
- DON'T reference files without verifying they exist first
- DON'T implement features not in the approved plan

**Database-First Workflow:**
- ALWAYS call database tools BEFORE code/log searches
- lookup_content_id: Check if content exists before creating
- get_valid_categories: Verify categories before using
- lookup_error_code: Understand error patterns before fixing
- get_tier_info: Check tier requirements before implementing
- get_system_dependencies: Understand system interactions first

**Quality Gates:**
- All C# code must compile (dotnet build)
- All JSON must validate (validate_content.py)
- All strings must sync (sync_event_strings.py)
- All new files added to .csproj
- No hallucinated APIs or file paths
"""

# =============================================================================
# RESEARCH WORKFLOW (Static - Cacheable)
# =============================================================================

RESEARCH_WORKFLOW = """
=== RESEARCH WORKFLOW (Execute in Order) ===

**Step 1: Extract from Pre-Loaded Context**
- Read the PRE-LOADED CONTEXT section below (if present)
- Extract relevant systems, classes, and patterns
- DO NOT call get_game_systems or get_architecture - already provided

**Step 2: Database Lookups (Fast)**
- Call get_system_dependencies for 2-3 most relevant systems (one call per system)
- Call lookup_core_system for any system needing details
- Call get_balance_value for specific balance numbers
- Call get_tier_info only for relevant tiers (not all 9)

**Step 3: Codebase Search (Only if Needed)**
- Use search_codebase for semantic queries (fast vector search)
- Use find_in_code only for exact-match fallback
- Read specific files with read_source
- AVOID redundant searches - each search query should be unique

**Step 4: Synthesize Findings**
- Summarize related systems and their roles
- List key files and methods with paths
- Identify integration points
- Note any risks or dependencies

**Tool Efficiency Rules:**
- Limit total tool calls to 10 maximum
- Each tool takes ONE argument, not arrays
- Don't re-search same queries with variations
- Database tools are instant, use them first
- All search results are cached automatically

**Output Format:**
- Related Systems: [bullet list with roles]
- Key Files: [paths with line numbers if known]
- Integration Points: [where systems connect]
- Risks: [potential issues to watch]
"""

# =============================================================================
# DESIGN WORKFLOW (Static - Cacheable)
# =============================================================================

DESIGN_WORKFLOW = """
=== DESIGN WORKFLOW (Execute in Order) ===

**Step 1: Extract from Pre-Loaded Context**
- Read the PRE-LOADED CONTEXT and RESEARCH SUMMARY sections
- Extract architectural patterns and constraints
- DO NOT call get_architecture - already provided

**Step 2: Tier-Aware Design**
- Call get_tier_info for relevant tiers only (not all)
- Consider progression (T1 → T9) in feature design
- Ensure tier-appropriate complexity and rewards

**Step 3: Balance Considerations**
- Call get_balance_value for specific values needed
- Use search_balance to find related balance parameters
- Stay consistent with existing balance philosophy

**Step 4: API Pattern Verification**
- Call lookup_api_pattern for Bannerlord APIs you'll use
- Verify patterns against Decompile/ if uncertain
- Use search_codebase for existing usage examples

**Step 5: Content Planning (if applicable)**
- Call lookup_content_id to verify IDs don't already exist
- Call get_valid_categories to check category names
- Plan event/decision/order IDs following snake_case convention

**Step 6: Create Technical Specification**
- Files to Create: [specific paths]
- Files to Modify: [specific paths with changes]
- Content IDs: [specific IDs in snake_case]
- Localization Keys: [XML string IDs needed]
- Integration Points: [where to hook into existing systems]
- Testing Approach: [how to verify it works]

**Tool Efficiency Rules:**
- Limit total tool calls to 8 maximum
- Build on research findings, don't re-research
- Each tool takes ONE argument, not arrays
- Use database tools before code searches

**Output Format:**
- Technical Specification with all sections above
- Gap Analysis: [what's missing that needs investigation]
- Implementation Priority: [order to build components]
"""

# =============================================================================
# IMPLEMENTATION WORKFLOW (Static - Cacheable)
# =============================================================================

IMPLEMENTATION_WORKFLOW = """
=== IMPLEMENTATION WORKFLOW (Execute in Order) ===

**Step 1: Verify What Exists**
- Call verify_file_exists_tool for EACH file path in plan
- Call lookup_content_id for EACH content ID in plan
- Use search_codebase to check for partial implementations
- DO NOT create files/IDs that already exist

**Step 2: C# Implementation (if needed)**
- Write C# following Allman braces, _camelCase fields
- Add XML doc comments to all public members
- Use proper Bannerlord APIs (verified with lookup_api_pattern)
- Include error handling with try-catch and ModLogger
- Test null conditions (Hero.MainHero != null)

**Step 3: Add to Project File**
- Call append_to_csproj for EACH new C# file created
- Old-style .csproj requires explicit <Compile Include="..."/>
- DO NOT skip this step or files won't compile

**Step 4: JSON Content Creation (if needed)**
- Call get_valid_categories to verify category
- Call get_valid_severities to verify severity
- Use snake_case for all content IDs
- Create JSON in correct folder (Events/, Decisions/, Orders/)
- Include tooltip field (<80 chars) for all options

**Step 5: Localization**
- Call update_localization for EACH new string ID
- Format: <string id="content_id_title" text="Display Text"/>
- Run sync_strings after all content created

**Step 6: Database Registration**
- Call add_content_item for EACH new event/decision/order
- Provides metadata for future lookups
- Enables content tracking and validation

**Step 7: Validation**
- Run build to verify C# compiles
- Run validate_content to check JSON/XML
- Fix any errors before completing

**Tool Efficiency Rules:**
- Limit implementation tool calls to 15 maximum
- Validation tools run ONCE at end, not per-file
- Batch localization updates together
- Database tools provide instant verification

**Output Format:**
- Implementation Summary: [what was created]
- Files Created: [paths]
- Files Modified: [paths with changes]
- Build Status: [pass/fail]
- Validation Status: [pass/fail]
"""

# =============================================================================
# VALIDATION WORKFLOW (Static - Cacheable)
# =============================================================================

VALIDATION_WORKFLOW = """
=== VALIDATION WORKFLOW (Execute in Order) ===

**Step 1: Content Validation**
- Run validate_content tool (checks JSON schemas, localization)
- Reports: missing tooltips, invalid categories, orphaned strings
- Fix issues if found before proceeding

**Step 2: Build Verification**
- Run build tool (dotnet build)
- Reports: syntax errors, missing references, type mismatches
- Fix compilation errors if found

**Step 3: Code Style Review**
- Run review_code to check Allman braces, naming conventions
- Check for XML documentation on public members
- Verify proper error handling patterns

**Step 4: Game Pattern Compliance**
- Run check_game_patterns to verify Bannerlord API usage
- Checks: GiveGoldAction usage, TextObject patterns, null checks
- Fix any anti-patterns found

**Step 5: Database Health Check**
- Verify content_metadata is synchronized
- Check for orphaned records
- Confirm implementation was logged

**Output Format:**
- Validation Status: PASS or FAIL
- Issues Found: [list with severity]
- Build Status: [success/failure with errors]
- Recommendations: [fixes needed]
"""

# =============================================================================
# BUG INVESTIGATION WORKFLOW (Static - Cacheable)
# =============================================================================

BUG_INVESTIGATION_WORKFLOW = """
=== BUG INVESTIGATION WORKFLOW (Execute in Order) ===

**Step 1: Error Code Lookup (if present)**
- Call lookup_error_code for EACH error code provided
- Get known patterns, causes, and solutions
- This is fastest path to understanding issue

**Step 2: Log Analysis**
- If user logs provided: Search LOG CONTENT above for patterns
- If no user logs: Use search_debug_logs_tool for recent errors
- Look for: stack traces, null refs, exceptions
- Note timestamps and error sequences

**Step 3: Code Location**
- Use search_codebase for semantic search (fast)
- Use find_in_code for exact method names
- Read relevant files with read_source
- Trace execution path to bug location

**Step 4: System Impact Assessment**
- Call get_system_dependencies for affected systems
- Understand what else might be impacted
- Check for save/load compatibility issues

**Step 5: Root Cause Analysis**
- Identify WHY bug occurs (not just WHERE)
- Check for: null refs, state machine issues, timing problems
- Verify with lookup_api_pattern for correct usage

**Step 6: Severity Classification**
- CRITICAL: Crashes, data loss, save corruption
- HIGH: Major feature broken, affects multiple systems
- MEDIUM: Single feature impacted, workaround exists
- LOW: Minor issue, cosmetic problem

**Tool Efficiency Rules:**
- lookup_error_code FIRST before any other search
- Limit investigation to 10 tool calls maximum
- Use database tools before code searches
- Don't re-search same queries

**Output Format:**
- Bug Location: [file:line]
- Root Cause: [why it occurs]
- Severity: [critical/high/medium/low]
- Affected Systems: [list]
- Confidence: [high/medium/low]
"""

# =============================================================================
# CODE STYLE RULES (Static - Cacheable)
# =============================================================================

CODE_STYLE_RULES = """
=== C# CODE STYLE REQUIREMENTS ===

**Bracing Style:**
```csharp
// CORRECT - Allman style (opening brace on new line)
public void DoSomething()
{
    if (condition)
    {
        // code
    }
}

// WRONG - K&R style
public void DoSomething() {
    if (condition) {
        // code
    }
}
```

**Naming Conventions:**
```csharp
// CORRECT
private int _myField;               // _camelCase for private fields
public int MyProperty { get; set; } // PascalCase for properties
public void MyMethod() { }          // PascalCase for methods
const int MAX_VALUE = 100;          // UPPER_CASE for constants

// WRONG
private int myField;                // Missing underscore
public int my_property { get; set; } // snake_case not allowed
```

**XML Documentation:**
```csharp
// REQUIRED for all public members
/// <summary>
/// Does something useful.
/// </summary>
/// <param name="input">The input value.</param>
/// <returns>The processed result.</returns>
public int DoSomething(int input)
{
    return input * 2;
}
```

**Error Handling:**
```csharp
// CORRECT - Defensive programming
if (Hero.MainHero == null)
{
    ModLogger.Log(LogLevel.Warning, "E-HERO-001", "MainHero is null");
    return;
}

try
{
    var behavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
    if (behavior == null)
    {
        ModLogger.Log(LogLevel.Error, "E-BEHAVIOR-002", "Behavior not found");
        return;
    }
    // Use behavior
}
catch (Exception ex)
{
    ModLogger.Log(LogLevel.Error, "E-BEHAVIOR-003", $"Exception: {ex.Message}");
}
```
"""

# =============================================================================
# TOOL EFFICIENCY RULES (Static - Cacheable)
# =============================================================================

TOOL_EFFICIENCY_RULES = """
=== TOOL EFFICIENCY RULES ===

**Database Tools FIRST (Instant Lookups):**
1. lookup_content_id - Check if content exists
2. get_valid_categories - Verify category names
3. get_valid_severities - Verify severity levels
4. lookup_error_code - Understand error patterns
5. get_tier_info - Check tier requirements
6. get_system_dependencies - Understand interactions
7. lookup_api_pattern - Get correct API usage
8. get_balance_value - Check balance numbers

**Code Search Tools (Slower):**
1. search_codebase - Semantic search (fast vector, use first)
2. find_in_code - Exact match search (fallback only)
3. read_source - Read specific files (after locating)

**Search Efficiency:**
- Each tool takes ONE argument (not arrays)
- Don't re-search same query with variations
- All search results are automatically cached
- Use batch tools when available (lookup_content_ids_batch)

**Call Limits by Task Type:**
- Research: 10 calls maximum
- Design: 8 calls maximum
- Implementation: 15 calls maximum
- Validation: 5 calls maximum
- Bug Investigation: 10 calls maximum

**Pre-Loaded Context:**
- If task includes "PRE-LOADED CONTEXT", read it first
- DO NOT call the same tool to fetch what's already provided
- Tools mentioned in context are already executed
"""
