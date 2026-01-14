"""
System Analysis Flow - Automated technical system integration analysis.

Performs deep analysis of game systems to find integration gaps, efficiency issues,
and improvement opportunities. Outputs comprehensive analysis documents.

Architecture: Single-agent Crews + pure Python steps following CrewAI best practices.
Each step focuses on one analytical task with minimal tools (3-5 max).
"""

import os
from pathlib import Path
from datetime import datetime
from typing import List

from crewai import Agent, Crew, Process, Task, LLM
from crewai.flow.flow import Flow, listen, start

from .state_models import (
    SystemAnalysisState, AnalysisFocus, 
    RecommendationsOutput, Recommendation, ImpactLevel
)
# Note: memory=False for analysis workflows - large outputs cause contamination loops
# (see GitHub Issue #826, Medium "CrewAI Memory Best Practices")
# from ..monitoring import EnlistedExecutionMonitor  # TODO: Fix import hang
from ..hooks import (
    reset_tool_budget, set_tool_budget, get_tool_budget_status,
    create_pydantic_safe_task
)
from ..tools import (
    get_system_dependencies, get_game_systems, lookup_api_pattern,
    verify_file_exists_tool
)
from ..tools.database_tools import (
    search_content, get_balance_value, search_balance, get_tier_info,
    list_all_core_systems, lookup_core_system, get_all_tiers, get_balance_by_category
)
from ..rag.codebase_rag_tool import search_codebase
from ..tools.docs_tools import search_docs_semantic, read_source
from ..prompts import ARCHITECTURE_PATTERNS


def get_project_root() -> Path:
    """Get the Enlisted project root directory."""
    env_root = os.environ.get("ENLISTED_PROJECT_ROOT")
    if env_root:
        return Path(env_root)
    
    current = Path(__file__).resolve()
    for parent in current.parents:
        if (parent / "Enlisted.csproj").exists():
            return parent
    
    # Default fallback
    return Path.cwd()


def _normalize_gap_title(title: str) -> str:
    """Normalize a gap title for de-duplication comparison."""
    import re
    # Remove markdown formatting, emojis, numbers, punctuation
    normalized = re.sub(r'[\*\#\-\d\.:!?\[\]\(\)]', '', title)
    # Remove common emoji characters
    normalized = re.sub(r'[❌⚠️🔍👁️🚨🐌🔄⚡🔥]', '', normalized)
    # Lowercase and strip whitespace
    normalized = normalized.lower().strip()
    # Keep only first 60 chars for comparison
    return normalized[:60]


def _add_gap_if_unique(gaps: List[dict], new_gap: dict) -> bool:
    """
    Add a gap to the list only if it's not a near-duplicate.
    
    Returns True if added, False if duplicate.
    """
    new_normalized = _normalize_gap_title(new_gap.get("title", ""))
    if not new_normalized or len(new_normalized) < 10:
        return False  # Skip very short or empty titles
    
    for existing in gaps:
        existing_normalized = _normalize_gap_title(existing.get("title", ""))
        # Check for exact match or high similarity (one is prefix of other)
        if new_normalized == existing_normalized:
            return False
        if len(new_normalized) > 15 and len(existing_normalized) > 15:
            # Check if one contains the other (catches similar phrasings)
            if new_normalized in existing_normalized or existing_normalized in new_normalized:
                return False
    
    gaps.append(new_gap)
    return True


# LLM Configurations
GPT5_ANALYST = LLM(model="gpt-5.2", max_completion_tokens=12000, reasoning_effort="high")
GPT5_ARCHITECT = LLM(model="gpt-5.2", max_completion_tokens=12000, reasoning_effort="high")
GPT5_CODE_REVIEW = LLM(model="gpt-5.2", max_completion_tokens=8000, reasoning_effort="medium")


class SystemAnalysisFlow(Flow[SystemAnalysisState]):
    """
    Automated system integration analysis flow.
    
    Architecture:
    - 8 Flow steps: 5 single-agent Crews + 3 pure Python
    - Each Crew step focuses on one analytical task
    - Pure Python steps handle deterministic operations
    
    Output: Comprehensive analysis document at docs/CrewAI_Plans/{system-name}-analysis.md
    """
    
    initial_state = SystemAnalysisState
    
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        # self._monitor = EnlistedExecutionMonitor()  # TODO: Fix monitoring hang
        # print("[MONITORING] Execution monitoring enabled")
    
    @start()
    def load_systems(self):
        """Step 1: Load system information (pure Python)."""
        print("\n" + "="*60)
        print("SYSTEM ANALYSIS FLOW STARTED")
        print("="*60)
        
        # Note: Systems database syncs lazily on first database tool access
        # (see database_tools._ensure_systems_synced)
        
        if not self.state.system_names:
            print("[ERROR] No system names provided!")
            self.state.success = False
            self.state.final_report = "Error: No system names provided"
            return
        
        print(f"\nSystems to analyze: {', '.join(self.state.system_names)}")
        print(f"Focus: {self.state.focus.value}")
        print(f"Subsystem mode: {self.state.is_subsystem}")
        
        # Identify likely file paths for these systems
        project_root = get_project_root()
        src_dir = project_root / "src" / "Features"
        
        system_files = []
        for system_name in self.state.system_names:
            # Common patterns for system files
            patterns = [
                f"{system_name}Manager.cs",
                f"{system_name}Behavior.cs",
                f"{system_name}State.cs",
                f"{system_name}.cs"
            ]
            
            for pattern in patterns:
                for file_path in src_dir.rglob(pattern):
                    system_files.append(str(file_path.relative_to(project_root)))
        
        self.state.system_files = system_files
        print(f"[OK] Found {len(system_files)} system files")
        
        # Set output path
        if not self.state.output_path:
            system_slug = "-".join(s.lower() for s in self.state.system_names)
            output_filename = f"{system_slug}-analysis.md"
            self.state.output_path = str(project_root / "docs" / "CrewAI_Plans" / output_filename)
        
        print(f"[OK] Output will be saved to: {self.state.output_path}")
    
    @listen(load_systems)
    def analyze_architecture(self):
        """Step 2: Analyze current architecture (single-agent Crew)."""
        print("\n" + "-"*60)
        print("[STEP 2/8] Analyzing architecture...")
        print("-"*60)
        
        # Reset tool budget for this step
        set_tool_budget(max_calls=10)
        
        agent = Agent(
            role="Systems Analyst",
            goal="Map current system architecture and data flows",
            backstory="""You analyze complex software architectures efficiently.
            You trace data flows, identify components, and document integration points.
            CRITICAL: Always verify code references with search_codebase before reporting.
            IMPORTANT: You have a LIMITED tool budget. Do NOT exceed it. Synthesize from what you find.""",
            llm=GPT5_ANALYST,
            tools=[search_codebase, search_docs_semantic, read_source, get_game_systems, get_system_dependencies],
            max_iter=8,
            allow_delegation=False,
            verbose=True,
        )
        
        systems_list = ", ".join(self.state.system_names)
        
        task = Task(
            description=f"""{ARCHITECTURE_PATTERNS}

=== YOUR TASK ===
Analyze the architecture of: {systems_list}

**WORKFLOW (execute in order):**
1. Use search_codebase to find Manager/Behavior files for each system
2. Use read_source to examine key components
3. Use get_system_dependencies to map relationships
4. Document architecture overview with data flows

**TOOL EFFICIENCY RULES (STRICT - STOP AFTER BUDGET):**
- MAXIMUM 8 tool calls total. STOP and synthesize after 8.
- Use search_codebase for broad searches (2-3 calls)
- Use read_source for specific files (3-4 calls)
- Use get_system_dependencies once per system (1-2 calls)
- DO NOT re-search the same system. If you already searched it, use those results.
- After 8 calls, STOP searching and write your analysis from what you found.

**Expected Output:**
## Architecture Overview
- Brief description of each system
- Key components (Manager, Behavior, State classes)
- Data flow between systems

## Integration Points
- How systems connect to each other
- Dependencies and coupling
- Event/callback mechanisms

Verify all code references with file paths!
            """,
            expected_output="Architecture analysis with components, data flows, and integration points",
            agent=agent,
        )
        
        crew = Crew(
            agents=[agent],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
            memory=False,  # Disabled: analysis outputs cause memory contamination loops
            cache=True,    # Keep tool result caching
        )
        
        result = crew.kickoff()
        self.state.architecture_overview = str(result)
        
        budget = get_tool_budget_status()
        print(f"[ANALYSIS] Architecture documented ({len(self.state.architecture_overview)} chars)")
        print(f"[BUDGET] Step used {budget['current_calls']}/{budget['max_calls']} tool calls")
    
    @listen(analyze_architecture)
    def identify_gaps(self):
        """Step 3: Identify integration gaps (single-agent Crew)."""
        # Skip if focus is efficiency-only
        if self.state.focus == AnalysisFocus.EFFICIENCY:
            print("[SKIP] Gap analysis (focus=efficiency)")
            return
        
        print("\n" + "-"*60)
        print("[STEP 3/8] Identifying integration gaps...")
        print("-"*60)
        
        # Reset tool budget for this step
        set_tool_budget(max_calls=12)
        
        agent = Agent(
            role="Architecture Advisor",
            goal="Find missing integrations, invisible systems, and unused values in this Bannerlord mod",
            backstory="""You identify integration gaps in the Enlisted mod for Bannerlord.
            
            KEY MOD CONTEXT:
            - This is a soldier career mod where players enlist with lords
            - Systems include: Morale, Supply, Promotion, Reputation, Orchestrator, Orders
            - Content is JSON-driven (ModuleData/Enlisted/) with XML localization
            - Players see systems through: Camp Hub menu, Daily Brief, Tooltips, event text
            
            You spot where:
            - Systems track values but don't show them to players
            - JSON content uses effects that aren't implemented in C#
            - Related systems should interact but don't
            - Documented features aren't actually coded""",
            llm=GPT5_ARCHITECT,
            tools=[search_codebase, search_docs_semantic, lookup_api_pattern, get_system_dependencies,
                   search_content, get_balance_value, search_balance],
            max_iter=10,
            allow_delegation=False,
            verbose=True,
        )
        
        task = Task(
            description=f"""
Based on this architecture analysis:

{self.state.architecture_overview[:3000]}

=== YOUR TASK ===
Find integration gaps and unused opportunities.

**WORKFLOW (execute in order):**
1. Use search_content to find content metadata (events, decisions, orders)
2. Use get_balance_value/search_balance to check tier thresholds and requirements
3. Identify values that systems track but don't use fully
4. Find missing connections between related systems
5. Document each gap with evidence from code AND database

**TOOL EFFICIENCY RULES:**
- Limit to 8 tool calls total
- Use search_content for content metadata (1-2 calls)
- Use search_balance for tier/economy values (1-2 calls)
- Use search_codebase to verify gaps in code (3-4 calls)
- DO NOT re-search the same pattern

**Expected Output:**
## Missing Integrations
1. **[Gap Title]**
   - Issue: [description]
   - Current State: [what exists]
   - Missing: [what should exist]
   - Impact: [gameplay/technical impact]

## Unused System Values
1. **[Value Name]**
   - Tracked: [what's being tracked]
   - Not Used For: [missed opportunities]
   - Potential Uses: [suggestions]

Maximum 10 gaps total. Prioritize by impact.
            """,
            expected_output="List of integration gaps and unused system values with evidence",
            agent=agent,
        )
        
        crew = Crew(
            agents=[agent],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
            memory=False,  # Disabled: analysis outputs cause memory contamination loops
            cache=True,    # Keep tool result caching
        )
        
        result = crew.kickoff()
        
        budget = get_tool_budget_status()
        print(f"[BUDGET] Step used {budget['current_calls']}/{budget['max_calls']} tool calls")
        
        # Parse gaps into structured format with de-duplication
        result_str = str(result)
        gaps = []
        duplicates_skipped = 0
        
        for line in result_str.split('\n'):
            line_stripped = line.strip()
            if not line_stripped or len(line_stripped) < 15:
                continue
            
            # Skip headers and metadata lines
            if line_stripped.startswith('#') or line_stripped.startswith('==='):
                continue
            
            new_gap = None
            
            # Pattern 1: Emoji markers (❌, ⚠️, 🔍, 👁️)
            if any(line_stripped.startswith(e) for e in ('❌', '⚠️', '🔍', '👁️', '🚨')):
                new_gap = {"title": line_stripped, "raw": "", "source": "generator"}
            # Pattern 2: Numbered items with bold "1. **Title**"
            elif line_stripped[0].isdigit() and '**' in line_stripped:
                new_gap = {"title": line_stripped, "raw": "", "source": "generator"}
            # Pattern 3: Bullet with bold "- **Title**"
            elif line_stripped.startswith('- **') or line_stripped.startswith('* **'):
                new_gap = {"title": line_stripped, "raw": "", "source": "generator"}
            # Pattern 4: Bold standalone "**Gap Title**" or "**Issue:**"
            elif line_stripped.startswith('**') and line_stripped.endswith('**'):
                new_gap = {"title": line_stripped, "raw": "", "source": "generator"}
            
            # Add with de-duplication
            if new_gap:
                if not _add_gap_if_unique(gaps, new_gap):
                    duplicates_skipped += 1
        
        self.state.gaps_identified = gaps
        print(f"[GAPS] Identified {len(gaps)} integration gaps ({duplicates_skipped} duplicates skipped)")
        
        # Debug if none found
        if not gaps and result_str:
            print(f"[DEBUG] No gaps parsed. Looking for patterns in output...")
            # Try to extract anything that looks like a finding
            import re
            bold_items = re.findall(r'\*\*([^*]+)\*\*', result_str)
            if bold_items:
                print(f"[DEBUG] Found {len(bold_items)} bold items - might be gaps")
    
    @listen(identify_gaps)
    def critique_analysis(self):
        """Step 4: Critique gap analysis - find blind spots (Generator-Critic pattern)."""
        if self.state.focus == AnalysisFocus.EFFICIENCY:
            print("[SKIP] Critique step (focus=efficiency)")
            return
        
        print("\n" + "-"*60)
        print("[STEP 4/8] Systems Critic - finding blind spots...")
        print("-"*60)
        
        # Reset tool budget for this step (Critic gets more to verify)
        set_tool_budget(max_calls=15)
        
        critic = Agent(
            role="Systems Critic & Game Designer",
            goal="Find BOTH implementation gaps AND design/utilization gaps - what's broken AND what's underutilized",
            backstory="""You are BOTH a skeptical QA architect AND a game designer.
            
            You find TWO types of gaps:
            
            **TYPE A: IMPLEMENTATION GAPS (QA perspective)**
            - Systems that WORK but players CAN'T SEE (no UI, no tooltip, no feedback)
            - JSON effects that are PARSED but IGNORED (no handler code)
            - Documented features that aren't IMPLEMENTED
            - Requirements/thresholds that are HIDDEN from players
            - STALE DATA: Database values that don't match hardcoded values in code
            
            **TYPE B: DESIGN/UTILIZATION GAPS (Game Designer perspective)**
            - Systems that work correctly but are UNDERUTILIZED for gameplay
            - Values tracked but only used for BINARY decisions (yes/no) instead of GRADIENT influence
            - No NARRATIVE ARCS: pressure builds but no escalating content reflects it
            - Tracked values DON'T WEIGHT content selection (high value = same content as low value)
            - Player attributes DON'T INFLUENCE what content appears
            - Systems that COULD interact but DON'T
            
            YOUR APPROACH:
            1. DISCOVER what tracking systems exist (search for State classes, tracked values, enums)
            2. For EACH discovered system, ask: Is this value used as BINARY or GRADIENT?
            3. For EACH discovered system, ask: Does this affect WHAT content appears, or just gates?
            4. For EACH discovered system, ask: Can players SEE this value and their progress?
            5. Look for systems that COULD enhance each other but don't interact
            
            You VERIFY by searching code AND the knowledge database, not by assuming.""",
            llm=GPT5_ARCHITECT,
            tools=[
                # Discovery tools (use these FIRST)
                list_all_core_systems,  # Lists all 16 core systems from database
                get_all_tiers,          # Gets tier progression table
                get_balance_by_category, # Gets balance values by category
                lookup_core_system,     # Look up specific system details
                # Verification tools  
                search_codebase,        # Search code for implementation details
                read_source,            # Read specific files
                get_balance_value,      # Get specific balance values
            ],
            max_iter=12,
            allow_delegation=False,
            verbose=True,
        )
        
        systems_list = ", ".join(self.state.system_names)
        prev_gap_count = len(self.state.gaps_identified)
        
        task = Task(
            description=f"""
The previous analysis found {prev_gap_count} gaps for: {systems_list}

Architecture context:
{self.state.architecture_overview[:2000]}

=== YOUR TASK: FIND WHAT WAS MISSED ===

You must find BOTH implementation gaps AND design/utilization gaps.

---
**PHASE 1: DISCOVER TRACKING SYSTEMS (USE DATABASE FIRST!)**

Start by querying the database to discover what systems exist:

1. **Call `list_all_core_systems()`** - This returns ALL 16 core systems with:
   - Name, file path, description, key features
   - Use this to know what tracking systems exist BEFORE searching code

2. **Call `get_all_tiers()`** - Get the tier progression table showing:
   - All tiers (T1-T9), XP requirements, tracks, descriptions

3. **Call `get_balance_by_category("economy")`** - Get economy balance values
4. **Call `get_balance_by_category("xp")`** - Get XP balance values

For each system discovered from the database, note:
- What values does it track? (from key_features column)
- What is the value range? (0-100? -50 to +50? enum?)
- Where is the file? (from file_path column)

---
**PHASE 2: IMPLEMENTATION GAPS (for each discovered system)**

**Visibility Audit:**
- Search for UI code that DISPLAYS this value to players
- Can players see their current value? Their progress? Requirements?
- If tracked but NOT shown → VISIBILITY GAP

**Handler Audit:**
- If JSON content references effect types, search for C# handlers
- If content uses a field but no code processes it → SILENT FAILURE

**Consistency Audit:**
- Compare database values to hardcoded values in code
- If they don't match → STALE DATA

---
**PHASE 3: DESIGN/UTILIZATION GAPS (for each discovered system)**

For EACH tracked value you discovered, ask these questions:

**Q1: Binary or Gradient?**
- Search how this value is USED (not just tracked)
- Is it only checked as threshold (value < X)? Or used as continuous modifier?
- Does value=40 feel different than value=90? Or only crossing thresholds matters?
- If continuous value only used for binary checks → UNDERUTILIZATION

**Q2: Gates or Weights?**
- Does this value affect WHAT content appears? Or just unlock/block access?
- Is it used in any "fitness" or "scoring" or "weight" calculations?
- Does high value make MORE of something appear? Or same amount, just unlocked?
- If only gates but doesn't weight → DESIGN OPPORTUNITY

**Q3: Narrative Arcs?**
- Is there duration/days tracking for sustained states?
- Does prolonged low/high value create escalating content?
- Or does content just trigger once when threshold crossed?
- If no duration awareness → MISSING NARRATIVE ARC

**Q4: Cross-System Influence?**
- Could this system enhance another but doesn't?
- Does it affect tone/dialogue/options in other systems?
- Are systems siloed when they could interact?
- If isolated → INTEGRATION OPPORTUNITY

---
**Expected Output:**

## Discovered Tracking Systems
[List what you found: name, range, storage location]

## Implementation Gaps
[Code broken, handlers missing, data stale, invisible to players]

## Utilization Gaps (DESIGN)
[For each system: is it binary-only? gates-only? no arcs? isolated?]

## Integration Opportunities  
[Systems that could enhance each other but don't]

For each gap, include:
- The specific system/value involved
- What you searched for
- What you found (or didn't find)
- Why this matters for player experience
- Is this an IMPLEMENTATION fix or a DESIGN enhancement?

DISCOVER first, then ANALYZE. Don't assume - verify with tools.
            """,
            expected_output="Blind spots and gaps missed by the initial analysis",
            agent=critic,
        )
        
        crew = Crew(
            agents=[critic],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
            memory=False,  # Disabled: analysis outputs cause memory contamination loops
            cache=True,    # Keep tool result caching
        )
        
        result = crew.kickoff()
        
        budget = get_tool_budget_status()
        print(f"[BUDGET] Step used {budget['current_calls']}/{budget['max_calls']} tool calls")
        
        # Parse critic findings and merge with de-duplication
        result_str = str(result)
        added_count = 0
        duplicates_skipped = 0
        
        # Look for gap indicators in output
        for line in result_str.split('\n'):
            line_stripped = line.strip()
            # Match numbered items, emoji markers, or bold items
            if any([
                line_stripped.startswith(('1.', '2.', '3.', '4.', '5.')),
                line_stripped.startswith(('❌', '⚠️', '🔍', '👁️')),
                line_stripped.startswith('- **'),
                line_stripped.startswith('**') and 'gap' in line_stripped.lower(),
            ]):
                if len(line_stripped) > 10:  # Substantive content
                    new_gap = {
                        "title": line_stripped,
                        "raw": "",
                        "source": "critic"
                    }
                    # Use existing list for de-duplication (checks against generator gaps too)
                    if _add_gap_if_unique(self.state.gaps_identified, new_gap):
                        added_count += 1
                    else:
                        duplicates_skipped += 1
        
        print(f"[CRITIC] Added {added_count} unique gaps, skipped {duplicates_skipped} duplicates (total: {len(self.state.gaps_identified)})")
    
    @listen(critique_analysis)
    def analyze_efficiency(self):
        """Step 5: Analyze efficiency issues (single-agent Crew)."""
        # Skip if focus is integration-only
        if self.state.focus == AnalysisFocus.INTEGRATION:
            print("[SKIP] Efficiency analysis (focus=integration)")
            return
        
        print("\n" + "-"*60)
        print("[STEP 5/8] Analyzing efficiency...")
        print("-"*60)
        
        # Reset tool budget for this step
        set_tool_budget(max_calls=10)
        
        agent = Agent(
            role="Code Analyst",
            goal="Find performance bottlenecks and code smells",
            backstory="""You review code for efficiency and quality issues.
            You identify duplicate logic, unnecessary recalculations, and performance problems.
            You suggest practical refactoring approaches.""",
            llm=GPT5_CODE_REVIEW,
            tools=[search_codebase, read_source, verify_file_exists_tool],
            max_iter=10,
            allow_delegation=False,
            verbose=True,
        )
        
        systems_list = ", ".join(self.state.system_names)
        
        task = Task(
            description=f"""
Analyze efficiency for: {systems_list}

**WORKFLOW (execute in order):**
1. Use search_codebase to find system implementation files
2. Use read_source to examine hotspots (Update, Tick, Calculate methods)
3. Identify duplicate logic across files
4. Find unnecessary recalculations or allocations
5. Document each issue with file:line references

**TOOL EFFICIENCY RULES:**
- Limit to 8 tool calls total
- Use search_codebase for file discovery (2-3 calls)
- Use read_source for targeted analysis (5-6 calls)
- Focus on Update/Tick/Calculate methods (performance hotspots)
- DO NOT read entire files - use specific line ranges

**Expected Output:**
## Performance Issues
1. **[Issue Title]**
   - Problem: [description]
   - Location: [file:line]
   - Impact: [performance cost]
   - Fix Complexity: [low|medium|high]

## Code Smells
1. **[Smell Type]**
   - Pattern: [what's wrong]
   - Occurrences: [count and locations]
   - Refactor Approach: [suggestion]

Maximum 10 issues total. Include file:line for all!
            """,
            expected_output="List of performance issues and code smells with locations",
            agent=agent,
        )
        
        crew = Crew(
            agents=[agent],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
            memory=False,  # Disabled: analysis outputs cause memory contamination loops
            cache=True,    # Keep tool result caching
        )
        
        result = crew.kickoff()
        
        budget = get_tool_budget_status()
        print(f"[BUDGET] Step used {budget['current_calls']}/{budget['max_calls']} tool calls")
        
        # Parse issues into structured format - multiple patterns
        result_str = str(result)
        issues = []
        
        for line in result_str.split('\n'):
            line_stripped = line.strip()
            if not line_stripped or len(line_stripped) < 15:
                continue
            
            # Skip headers
            if line_stripped.startswith('#') or line_stripped.startswith('==='):
                continue
            
            # Pattern 1: Emoji markers (🐌, 🔄, ⚡, 🔥)
            if any(line_stripped.startswith(e) for e in ('🐌', '🔄', '⚡', '🔥', '⚠️')):
                issues.append({"title": line_stripped, "raw": ""})
            # Pattern 2: Numbered items with bold
            elif line_stripped[0].isdigit() and '**' in line_stripped:
                issues.append({"title": line_stripped, "raw": ""})
            # Pattern 3: Bullet with bold
            elif line_stripped.startswith('- **') or line_stripped.startswith('* **'):
                issues.append({"title": line_stripped, "raw": ""})
        
        self.state.efficiency_issues = issues
        print(f"[EFFICIENCY] Identified {len(issues)} efficiency issues")
        
        # Debug if none found
        if not issues and result_str:
            print(f"[DEBUG] No efficiency issues parsed. Sample output (first 300 chars):")
            print(result_str[:300])
    
    @listen(analyze_efficiency)
    def propose_improvements(self):
        """Step 6: Propose prioritized improvements (single-agent Crew)."""
        print("\n" + "-"*60)
        print("[STEP 6/8] Proposing improvements...")
        print("-"*60)
        
        # Reset tool budget for this step
        set_tool_budget(max_calls=8)
        
        agent = Agent(
            role="Feature Architect",
            goal="Generate prioritized recommendations with effort/impact estimates",
            backstory="""You synthesize analysis into actionable recommendations.
            You prioritize by impact vs effort matrix.
            You provide realistic effort estimates and implementation notes.""",
            llm=GPT5_ARCHITECT,
            tools=[search_docs_semantic, lookup_api_pattern, verify_file_exists_tool],
            max_iter=6,
            allow_delegation=False,
            verbose=True,
        )
        
        # Combine analysis results - include actual gap/issue details, not just counts
        gaps_text = "\n".join(
            f"- {g.get('title', 'Unknown gap')}" 
            for g in self.state.gaps_identified[:15]  # Limit to 15 most important
        ) if self.state.gaps_identified else "No gaps identified"
        
        issues_text = "\n".join(
            f"- {i.get('title', 'Unknown issue')}" 
            for i in self.state.efficiency_issues[:10]
        ) if self.state.efficiency_issues else "No efficiency issues identified"
        
        combined_analysis = f"""
ARCHITECTURE SUMMARY:
{self.state.architecture_overview[:2500]}

IDENTIFIED GAPS ({len(self.state.gaps_identified)} total):
{gaps_text}

EFFICIENCY ISSUES ({len(self.state.efficiency_issues)} total):
{issues_text}
"""
        
        # Use autonomous Pydantic fixer to ensure structured output
        task = create_pydantic_safe_task(
            description=f"""
Based on this analysis:

{combined_analysis}

=== YOUR TASK ===
Generate prioritized recommendations grouped by priority tier.

**WORKFLOW (execute in order):**
1. Review all identified issues (gaps + efficiency)
2. For each issue, estimate impact (high/medium/low) and effort (hours)
3. Prioritize by impact/effort ratio
4. Group into 3 priority buckets

**TOOL USAGE:**
- Limit to 5 tool calls total
- Use lookup_api_pattern to verify APIs (2-3 calls)
- Use search_docs_semantic for examples (2-3 calls)

Maximum 10 recommendations total across all priorities.
            """,
            expected_output="Structured recommendations grouped by priority tier with effort estimates",
            agent=agent,
            output_pydantic=RecommendationsOutput,
        )
        
        crew = Crew(
            agents=[agent],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
            memory=False,  # Disabled: analysis outputs cause memory contamination loops
            cache=True,    # Keep tool result caching
        )
        
        result = crew.kickoff()
        
        budget = get_tool_budget_status()
        print(f"[BUDGET] Step used {budget['current_calls']}/{budget['max_calls']} tool calls")
        
        # Try to get structured Pydantic output first
        recommendations = []
        pydantic_success = False
        
        try:
            if hasattr(result, 'pydantic') and result.pydantic is not None:
                # Got structured output - convert to state format
                pydantic_out = result.pydantic
                
                # Flatten all priority tiers into single list with tier info
                for rec in pydantic_out.priority_1_quick_wins:
                    recommendations.append({
                        "title": rec.title,
                        "description": rec.description,
                        "benefit": rec.benefit,
                        "effort_hours": rec.effort_hours,
                        "impact": rec.impact.value if hasattr(rec.impact, 'value') else rec.impact,
                        "files": rec.files,
                        "priority": 1,
                    })
                
                for rec in pydantic_out.priority_2_major:
                    recommendations.append({
                        "title": rec.title,
                        "description": rec.description,
                        "benefit": rec.benefit,
                        "effort_hours": rec.effort_hours,
                        "impact": rec.impact.value if hasattr(rec.impact, 'value') else rec.impact,
                        "files": rec.files,
                        "priority": 2,
                    })
                
                for rec in pydantic_out.priority_3_minor:
                    recommendations.append({
                        "title": rec.title,
                        "description": rec.description,
                        "benefit": rec.benefit,
                        "effort_hours": rec.effort_hours,
                        "impact": rec.impact.value if hasattr(rec.impact, 'value') else rec.impact,
                        "files": rec.files,
                        "priority": 3,
                    })
                
                pydantic_success = True
                print(f"[RECOMMENDATIONS] Got structured Pydantic output")
        except Exception as e:
            print(f"[WARN] Pydantic parsing failed: {e}")
        
        # Fallback: parse raw output if Pydantic failed
        if not pydantic_success:
            print(f"[RECOMMENDATIONS] Falling back to text parsing")
            result_str = str(result)
            
            for line in result_str.split('\n'):
                line_stripped = line.strip()
                if not line_stripped:
                    continue
                
                # Pattern 1: "1. **Title**" or "1) **Title**"
                if line_stripped[0].isdigit() and '**' in line_stripped:
                    recommendations.append({"title": line_stripped, "raw": ""})
                # Pattern 2: "- **Title**" (bullet with bold)
                elif line_stripped.startswith('- **') or line_stripped.startswith('* **'):
                    recommendations.append({"title": line_stripped, "raw": ""})
                # Pattern 3: "**1. Title**" or "**Title:**"
                elif line_stripped.startswith('**') and len(line_stripped) > 2 and (':' in line_stripped or line_stripped[2].isdigit()):
                    recommendations.append({"title": line_stripped, "raw": ""})
        
        self.state.recommendations = recommendations
        print(f"[RECOMMENDATIONS] Generated {len(recommendations)} prioritized recommendations")
        
        # Debug: if still 0, log a sample of what we got
        if not recommendations:
            result_str = str(result)
            print(f"[DEBUG] No recommendations parsed. Sample output (first 500 chars):")
            print(result_str[:500])
    
    @listen(propose_improvements)
    def validate_feasibility(self):
        """Step 6: Validate API compatibility (pure Python)."""
        print("\n" + "-"*60)
        print("[STEP 7/8] Validating feasibility...")
        print("-"*60)
        
        # Basic validation checks
        warnings = []
        
        # Check if recommendations reference known problematic patterns
        recommendations_text = "\n".join(r.get("title", "") for r in self.state.recommendations)
        
        if "breaking change" in recommendations_text.lower():
            warnings.append("⚠️ Some recommendations may require breaking changes")
        
        if "save migration" in recommendations_text.lower():
            warnings.append("⚠️ Save migration may be required")
        
        # Check Bannerlord version compatibility
        if "v1.3.13" not in recommendations_text and "1.3.13" not in recommendations_text:
            warnings.append("⚠️ Verify API compatibility with Bannerlord v1.3.13")
        
        self.state.compatibility_warnings = warnings
        self.state.api_compatible = len(warnings) == 0
        
        if warnings:
            print(f"[WARNINGS] {len(warnings)} compatibility warnings:")
            for warning in warnings:
                print(f"  {warning}")
        else:
            print("[OK] No compatibility concerns detected")
    
    @listen(validate_feasibility)
    def generate_analysis_doc(self):
        """Step 7: Generate final markdown document (pure Python)."""
        print("\n" + "-"*60)
        print("[STEP 8/8] Generating analysis document...")
        print("-"*60)
        
        # Build markdown document
        systems_str = ", ".join(self.state.system_names)
        date_str = datetime.now().strftime("%Y-%m-%d")
        
        doc_parts = [
            f"# {systems_str} System Analysis",
            f"",
            f"**Generated:** {date_str}",
            f"**Systems Analyzed:** {systems_str}",
            f"**Focus:** {self.state.focus.value}",
            f"**Subsystem Mode:** {'Yes' if self.state.is_subsystem else 'No'}",
            f"",
            "---",
            "",
            "## Executive Summary",
            "",
            f"- Analyzed {len(self.state.system_files)} system files",
            f"- Identified {len(self.state.gaps_identified)} integration gaps",
            f"- Found {len(self.state.efficiency_issues)} efficiency issues",
            f"- Generated {len(self.state.recommendations)} prioritized recommendations",
            "",
            "---",
            "",
            "## Current Architecture",
            "",
            self.state.architecture_overview,
            "",
            "---",
            "",
        ]
        
        # Add gap analysis section
        if self.state.focus != AnalysisFocus.EFFICIENCY:
            doc_parts.extend([
                "## Gap Analysis",
                "",
                f"Total gaps identified: {len(self.state.gaps_identified)}",
                "",
            ])
            for gap in self.state.gaps_identified[:10]:  # Limit to 10
                doc_parts.append(gap.get("title", ""))
                doc_parts.append("")
            doc_parts.extend(["---", ""])
        
        # Add efficiency analysis section
        if self.state.focus != AnalysisFocus.INTEGRATION:
            doc_parts.extend([
                "## Efficiency Analysis",
                "",
                f"Total issues identified: {len(self.state.efficiency_issues)}",
                "",
            ])
            for issue in self.state.efficiency_issues[:10]:  # Limit to 10
                doc_parts.append(issue.get("title", ""))
                doc_parts.append("")
            doc_parts.extend(["---", ""])
        
        # Add recommendations section
        doc_parts.extend([
            "## Recommendations",
            "",
            f"Total recommendations: {len(self.state.recommendations)}",
            "",
        ])
        for i, rec in enumerate(self.state.recommendations[:10], 1):  # Limit to 10
            # Format based on whether we have full structured data or just titles
            if "description" in rec and rec["description"]:
                # Full structured recommendation
                priority = rec.get("priority", "?")
                impact = rec.get("impact", "?")
                effort = rec.get("effort_hours", "?")
                
                doc_parts.append(f"### {i}. {rec['title']}")
                doc_parts.append("")
                doc_parts.append(f"**Priority:** {priority} | **Impact:** {impact} | **Effort:** {effort}h")
                doc_parts.append("")
                doc_parts.append(f"**Description:** {rec['description']}")
                doc_parts.append("")
                if rec.get("benefit"):
                    doc_parts.append(f"**Benefit:** {rec['benefit']}")
                    doc_parts.append("")
                if rec.get("files"):
                    doc_parts.append(f"**Files:** {', '.join(rec['files'])}")
                    doc_parts.append("")
            else:
                # Fallback: just title
                doc_parts.append(f"{i}. {rec.get('title', '')}")
                doc_parts.append("")
        
        # Add compatibility warnings
        if self.state.compatibility_warnings:
            doc_parts.extend([
                "---",
                "",
                "## Compatibility Warnings",
                "",
            ])
            for warning in self.state.compatibility_warnings:
                doc_parts.append(f"- {warning}")
                doc_parts.append("")
        
        # Add footer
        doc_parts.extend([
            "---",
            "",
            "## Next Steps",
            "",
            "1. Review recommendations and prioritize",
            "2. Use Warp Agent to create detailed implementation plans",
            "3. Execute with `enlisted-crew implement -p <plan-path>`",
            "",
            "---",
            "",
            f"*Generated by SystemAnalysisFlow v1.0 on {date_str}*",
        ])
        
        final_doc = "\n".join(doc_parts)
        self.state.final_report = final_doc
        
        # Write to file
        try:
            output_path = Path(self.state.output_path)
            output_path.parent.mkdir(parents=True, exist_ok=True)
            output_path.write_text(final_doc, encoding="utf-8")
            
            self.state.analysis_doc_path = str(output_path)
            self.state.success = True
            
            print(f"[SUCCESS] Analysis saved to: {output_path}")
            print(f"[SUCCESS] Document size: {len(final_doc)} chars")
        except Exception as e:
            print(f"[ERROR] Failed to write analysis document: {e}")
            self.state.success = False
            self.state.final_report = f"Error writing document: {e}\n\n{final_doc}"
        
        print("\n" + "="*60)
        print("SYSTEM ANALYSIS FLOW COMPLETE")
        print("="*60)
