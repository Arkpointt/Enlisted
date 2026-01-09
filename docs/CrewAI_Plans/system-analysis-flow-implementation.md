# System Analysis Flow Implementation - Technical System Integration Analysis

> **Status:** 🔴 **Not Yet Implemented**  
> **Last Updated:** 2026-01-09  
> **Architecture:** Single-agent Crews (aligned with current v1.0 architecture)

## Implementation Context

**Project Location:** `Tools/CrewAI/` in Enlisted workspace

**Reference Implementations:**
- **ImplementationFlow:** `src/enlisted_crew/flows/implementation_flow.py` (366 lines) - Example of single-agent Crew pattern
- **BugHuntingFlow:** `src/enlisted_crew/flows/bug_hunting_flow.py` (614 lines) - Example of conditional routing
- **ValidationFlow:** `src/enlisted_crew/flows/validation_flow.py` (557 lines) - Example of pure Python steps

**Key Files to Reference:**
- **Agent definitions:** `src/enlisted_crew/agents/` - All 10 agents already defined
- **State models:** `src/enlisted_crew/flows/state_models.py` - Add SystemAnalysisState here
- **Conditions:** `src/enlisted_crew/flows/conditions.py` - Reusable condition functions
- **Tools:** `src/enlisted_crew/tools/` - All tools already exist (search_codebase, search_docs_semantic, etc.)
- **CLI:** `src/enlisted_crew/main.py` - Add analyze-system subcommand
- **Tests:** `test_all.py` - Comprehensive test suite pattern

**Development Rules:**
- Read `Tools/CrewAI/README.md` for project overview
- Read `Tools/CrewAI/docs/CREWAI.md` for architecture details
- Follow single-agent Crew pattern (NOT multi-agent)
- Use 3-5 tools max per agent
- Each Flow step either single-agent Crew OR pure Python (never both)

**Target Systems for Analysis:**
- **Core Systems:** Supply, Morale, Reputation, Escalation, Readiness, Rest
- **Managers:** CompanyNeedsManager, EscalationManager, ContentOrchestrator
- **Integration Points:** How systems connect to content delivery
- **Code Locations:** `src/Features/` in main Enlisted project
- **Docs:** `docs/Features/`, `docs/CrewAI_Plans/` (output location)

**Project Constraints:**
- **Bannerlord Version:** v1.3.13 (verify APIs against `Decompile/` not online docs)
- **C# Project:** Old-style `.csproj` with explicit file includes
- **Python:** 3.10-3.13 required
- **LLM:** GPT-5.2 with optimized reasoning levels

**Testing Approach:**
- Add test to `test_all.py` following existing pattern
- Verify state transitions
- Validate output format
- Check CLI argument parsing
- Run: `python test_all.py`

---

## Problem Statement

Enlisted has complex interconnected systems (Supply, Morale, Reputation, Escalation, etc.) that need tight integration for emergent gameplay. However:

**Current Challenges:**
- Systems may track values without using them effectively
- Integration gaps between related systems (e.g., Supply affects events, but not NPC behavior)
- Inefficient code patterns (duplicate checks, unnecessary recalculations)
- Missing opportunities for cross-system mechanics
- Hard to manually analyze 245+ content pieces and 50+ C# systems

**Example:** Supply system tracks 0-100 value but only uses binary thresholds (<20% = crisis). ContentOrchestrator doesn't weight opportunities based on supply level. Supply doesn't affect QM/officer NPC behavior beyond basic gating.

**Need:** Automated deep-dive analysis that finds gaps, inefficiencies, and improvement opportunities.

## Current State

**Existing Workflows:**
- ImplementationFlow: Builds from approved plans (single-agent Crews)
- BugHuntingFlow: Finds and fixes issues (single-agent Crews)
- ValidationFlow: Pre-commit checks (single-agent Crews)

**Planning Approach:**
- Use Warp Agent directly for planning/design (full codebase context, faster than multi-agent)
- CrewAI flows focus on execution, not planning

**Existing Agents (10 total):**
- systems_analyst, architecture_advisor, feature_architect
- code_analyst, csharp_implementer, content_author
- content_analyst, qa_agent
- documentation_maintainer, balance_analyst

**Gap:** No workflow for comprehensive system analysis that finds integration gaps and efficiency issues.

## Proposed Solution: SystemAnalysisFlow

Create a new standalone Flow that performs deep technical analysis of game systems, producing actionable recommendations for integration and optimization.

### Architecture Pattern

**Agentic Systems Pattern (DocuSign/CrewAI best practice 2026):**
- Deterministic backbone (Flow) controls structure
- Single-agent Crews per step (not multi-agent Crews) - prevents excessive tool calls
- Intelligence (Agents) invoked at specific steps within clear boundaries
- High agency for analytical work → produces structured analysis documents

**CRITICAL:** Each Flow step with intelligence uses a single-agent Crew with focused tool set (3-5 tools max). Multi-agent Crews cause 168 tool calls and timeouts.

### Flow Structure

```python
class SystemAnalysisFlow(Flow):
    @start()
    def load_systems(self):
        # DETERMINISTIC: Parse input, load system documentation, identify C# files
        # Pure Python - no LLM needed
        
    @listen(load_systems)
    def analyze_architecture(self):
        # SINGLE-AGENT CREW: systems_analyst examines current state
        # Output: Architecture overview, component list, data flows
        
    @listen(analyze_architecture)
    def identify_gaps(self):
        # SINGLE-AGENT CREW: architecture_advisor finds integration gaps
        # Output: Missing connections, unused data, integration opportunities
        
    @listen(identify_gaps)
    def analyze_efficiency(self):
        # SINGLE-AGENT CREW: code_analyst finds performance/design issues
        # Output: Bottlenecks, code smells, refactoring opportunities
        
    @listen(analyze_efficiency)
    def propose_improvements(self):
        # SINGLE-AGENT CREW: feature_architect generates prioritized recommendations
        # Output: Actionable improvements with effort/impact estimates
        
    @listen(propose_improvements)
    def validate_feasibility(self):
        # DETERMINISTIC: Check API compatibility, verify patterns exist
        # Pure Python - pattern matching against knowledge base
        
    @listen(validate_feasibility)
    def generate_analysis_doc(self):
        # DETERMINISTIC: Create markdown document
        # Pure Python - template-based generation
```

### Agent Usage (Reuse Existing Agents)

**1. systems_analyst** (analyze_architecture step)
- **Tools:** search_codebase, search_docs_semantic, read_source, get_system_dependencies, get_game_systems
- **Task:** Map current architecture, identify components, trace data flows
- **LLM:** GPT-5.2 with reasoning_effort="high" (complex analysis)

**2. architecture_advisor** (identify_gaps step)
- **Tools:** search_codebase, search_docs_semantic, lookup_api_pattern, get_system_dependencies
- **Task:** Find missing integrations, unused system values, coupling issues
- **LLM:** GPT-5.2 with reasoning_effort="high" (strategic thinking)

**3. code_analyst** (analyze_efficiency step)
- **Tools:** search_codebase, read_source, check_game_patterns, review_source_file
- **Task:** Identify performance bottlenecks, code duplication, inefficient patterns
- **LLM:** GPT-5.2 with reasoning_effort="medium" (code review)

**4. feature_architect** (propose_improvements step)
- **Tools:** search_docs_semantic, lookup_api_pattern, get_game_systems, verify_file_exists_tool
- **Task:** Generate prioritized recommendations with effort/impact estimates
- **LLM:** GPT-5.2 with reasoning_effort="high" (architectural decisions)

**Note:** All agents already exist - no new agents needed! Just reuse them in a new flow.

## CLI Integration

```bash
# Analyze a specific system
enlisted-crew analyze-system "Supply"

# Analyze multiple related systems
enlisted-crew analyze-system "Supply,Morale,Reputation"

# Analyze how a system integrates with content
enlisted-crew analyze-system "ContentOrchestrator" --focus integration

# Full codebase health check
enlisted-crew analyze-system --all

# Target specific subsystem
enlisted-crew analyze-system "CompanyNeedsManager" --subsystem
```

### Arguments
- `system_names`: System name(s) to analyze (required, comma-separated)
- `--all`: Analyze all major systems
- `--focus`: Analysis focus (integration|efficiency|both) - default: both
- `--subsystem`: Treat as subsystem (narrower scope)
- `--output`: Custom output path (default: `docs/CrewAI_Plans/`)

## Output Format

**Location:** `docs/CrewAI_Plans/{system-name}-analysis.md`

**Structure:**
```markdown
# {System Name} Analysis
Generated: {date}
Systems Analyzed: {list}
Focus: {integration|efficiency|both}

## Executive Summary
- {Key finding 1}
- {Key finding 2}
- {Key finding 3}

## Current Architecture
### Components
- {Component list with responsibilities}

### Data Flows
- {How data moves between systems}

### Integration Points
- {Where system connects to others}

## Gap Analysis

### Missing Integrations
1. ❌ **{Gap Title}**
   - **Issue:** {description}
   - **Impact:** {gameplay/technical impact}
   - **Current State:** {what exists now}
   - **Missing:** {what should exist}
   - **Affected Systems:** {list}

### Unused System Values
1. ⚠️ **{Unused Value}**
   - **Tracked:** {what's being tracked}
   - **Not Used For:** {missed opportunities}
   - **Potential Uses:** {suggestions}

## Efficiency Analysis

### Performance Issues
1. 🐌 **{Issue Title}**
   - **Problem:** {description}
   - **Location:** {file:line}
   - **Impact:** {performance cost}
   - **Fix Complexity:** {low|medium|high}

### Code Smells
1. 🔄 **{Smell Type}**
   - **Pattern:** {what's wrong}
   - **Occurrences:** {count and locations}
   - **Refactor Approach:** {suggestion}

## Recommendations

### Priority 1: High Impact, Low Effort
1. **{Recommendation Title}**
   - **Description:** {what to do}
   - **Benefit:** {why it matters}
   - **Effort:** {hours estimate}
   - **Impact:** {High|Medium|Low}
   - **Affected Files:** {list}
   - **Implementation Notes:** {key considerations}

### Priority 2: High Impact, Medium Effort
{...}

### Priority 3: Medium Impact, Low Effort
{...}

### Future Enhancements
{Lower priority improvements}

## Implementation Roadmap
1. **Phase 1:** {Quick wins - Priority 1 items}
2. **Phase 2:** {Core improvements - Priority 2 items}
3. **Phase 3:** {Strategic enhancements - Priority 3 items}

## Technical Details
### API Compatibility
- ✅ {Compatible API pattern}
- ⚠️ {Compatibility concern}

### Dependencies
- {System dependencies and coupling analysis}

### Testing Strategy
- {How to validate changes}
```

## Integration with Existing Workflows

**Current Architecture:** Single-agent Crews + Warp Agent for planning

**Typical Workflow:**
1. User: `enlisted-crew analyze-system "Supply,Morale"`
2. SystemAnalysisFlow runs for 3-5 minutes (unattended)
3. Outputs: `docs/CrewAI_Plans/supply-morale-analysis.md`
4. User reviews analysis document
5. User picks "Recommendation 2: Add supply-aware opportunity weighting"
6. User asks Warp Agent to create implementation plan:
   - User: "Create plan for Recommendation 2 from docs/CrewAI_Plans/supply-morale-analysis.md"
   - Warp Agent reads analysis and creates detailed plan at `docs/CrewAI_Plans/supply-opportunity-weighting.md`
7. User: `enlisted-crew implement -p docs/CrewAI_Plans/supply-opportunity-weighting.md`
8. ImplementationFlow executes with single-agent Crews per step

**Integration Points:**
- SystemAnalysisFlow outputs consumed by Warp Agent (for planning)
- Warp Agent outputs consumed by ImplementationFlow (for execution)
- All flows use SQLite knowledge base + ChromaDB semantic search

**Value Add:** SystemAnalysisFlow provides the deep technical analysis that's hard to do manually, feeding into your existing planning and implementation workflows.

## Files to Create

### 1. Flow Implementation
**Path:** `src/enlisted_crew/flows/system_analysis_flow.py`  
**Size:** ~450 lines  
**Dependencies:** Reuses existing agents (systems_analyst, architecture_advisor, code_analyst, feature_architect)

### 2. State Model
**Path:** `src/enlisted_crew/flows/state_models.py` (update existing)  
**Addition:** `SystemAnalysisState` Pydantic model (~40 lines)

### 3. CLI Command
**Path:** `src/enlisted_crew/main.py` (update existing)  
**Addition:** `analyze-system` subparser and `run_analysis()` function (~70 lines)

### 4. Flow Exports
**Path:** `src/enlisted_crew/flows/__init__.py` (update existing)  
**Addition:** Export `SystemAnalysisFlow`, `SystemAnalysisState`

### 5. Documentation
**Path:** `Tools/CrewAI/docs/CREWAI.md` (update existing)  
**Addition:** Section 4 "Analyze System - Technical Integration Analysis" (~50 lines)

### 6. Tests
**Path:** `test_system_analysis.py` (create)  
**Size:** ~180 lines  
**Coverage:** Flow execution, state transitions, output format validation

## Technical Specifications

### Input Validation
- `system_names`: Required, validates against known systems in codebase
- `--all`: Mutual exclusive with system_names
- `--focus`: Must be one of: integration, efficiency, both
- `--subsystem`: Boolean flag

### State Management (Pydantic)
```python
class AnalysisFocus(str, Enum):
    INTEGRATION = "integration"
    EFFICIENCY = "efficiency"
    BOTH = "both"

class SystemAnalysisState(BaseModel):
    # Flow ID
    id: str = ""
    
    # Input
    system_names: List[str] = Field(default_factory=list)
    focus: AnalysisFocus = AnalysisFocus.BOTH
    is_subsystem: bool = False
    output_path: str = ""
    
    # Loaded system data
    system_files: List[str] = Field(default_factory=list)
    doc_files: List[str] = Field(default_factory=list)
    related_systems: List[str] = Field(default_factory=list)
    
    # Analysis outputs
    architecture_overview: str = ""
    gaps_identified: List[dict] = Field(default_factory=list)
    efficiency_issues: List[dict] = Field(default_factory=list)
    recommendations: List[dict] = Field(default_factory=list)
    
    # Feasibility check
    api_compatible: bool = True
    compatibility_warnings: List[str] = Field(default_factory=list)
    
    # Final output
    analysis_doc_path: str = ""
    success: bool = False
    final_report: str = ""
```

### Error Handling
- Unknown system name → Error with list of valid systems
- No files found → Warning, proceed with docs-only analysis
- Analysis timeout → Graceful degradation with partial results
- File write failure → Log error, return markdown as string

### Tool Call Limits (Optimization)
- analyze_architecture step: 8-12 tool calls (complex system mapping)
- identify_gaps step: 5-8 tool calls (targeted gap finding)
- analyze_efficiency step: 6-10 tool calls (code review)
- propose_improvements step: 4-6 tool calls (synthesis)
- **Total flow execution:** 25-35 tool calls target

**Architecture requirement:** Single-agent Crews per step drastically reduce calls. Each step is focused on one analytical task.

## Design Principles Alignment

### Project Principles (from BLUEPRINT.md)
1. ✅ **Emergent identity from choices** - Analysis finds opportunities for player choice integration
2. ✅ **Native Bannerlord integration** - Verifies API compatibility, suggests native patterns
3. ✅ **Data-driven content** - Identifies how systems can drive content selection
4. ✅ **Choice-driven narrative** - Maps system states to narrative opportunities

### CrewAI Best Practices (2026)
1. ✅ **Deterministic backbone + strategic intelligence** - Flow controls structure, agents provide analysis
2. ✅ **High agency for analytical work** - All agents use reasoning_effort="medium" or "high"
3. ✅ **Role-based specialization** - Each agent has clear analytical focus
4. ✅ **Tool count guidelines** - 4-5 tools per agent (within 3-5 max guideline)
5. ✅ **State persistence** - Pydantic models ensure type safety and recovery

## Success Criteria

### Functional Requirements
- [ ] Analyze 1-5 systems in single execution
- [ ] Generate comprehensive analysis document (gap + efficiency sections)
- [ ] Provide prioritized recommendations with effort estimates
- [ ] Output saved to `docs/CrewAI_Plans/{system-name}-analysis.md`
- [ ] CLI command `enlisted-crew analyze-system` works with all flags
- [ ] Execution completes in 3-5 minutes

### Quality Requirements
- [ ] No hallucinated systems/APIs (validated against codebase)
- [ ] Gap analysis identifies real integration opportunities
- [ ] Efficiency issues reference actual code locations (file:line)
- [ ] Recommendations include implementation complexity estimates
- [ ] Analysis uses actual codebase data (not assumptions)

### Integration Requirements
- [ ] No breaking changes to existing flows
- [ ] Analysis docs usable as input to Warp Agent planning
- [ ] Agent tools properly configured and imported
- [ ] Error messages helpful and actionable
- [ ] Documentation updated in CREWAI.md
- [ ] Each Flow step uses single-agent Crew pattern
- [ ] Tool counts respect 3-5 max per agent guideline

## Implementation Checklist

### Phase 1: Core Flow (~3 hours)
- [ ] Create `system_analysis_flow.py` with Flow structure
- [ ] Implement `SystemAnalysisState` Pydantic model
- [ ] Add `@start()` load_systems method (pure Python)
- [ ] Add `@listen()` analyze_architecture method (single-agent Crew)
- [ ] Add `@listen()` identify_gaps method (single-agent Crew)
- [ ] Add `@listen()` analyze_efficiency method (single-agent Crew)
- [ ] Add `@listen()` propose_improvements method (single-agent Crew)
- [ ] Add `@listen()` validate_feasibility method (pure Python)
- [ ] Add `@listen()` generate_analysis_doc method (pure Python)
- [ ] Test flow execution end-to-end

### Phase 2: CLI Integration (~1 hour)
- [ ] Add `analyze-system` subparser to `main.py`
- [ ] Implement `run_analysis()` function
- [ ] Add argument parsing (system_names, --all, --focus, --subsystem, --output)
- [ ] Connect to SystemAnalysisFlow.kickoff()
- [ ] Test CLI command with various inputs

### Phase 3: Agent Task Configuration (~2 hours)
- [ ] Write Task description for systems_analyst (architecture mapping)
- [ ] Write Task description for architecture_advisor (gap finding)
- [ ] Write Task description for code_analyst (efficiency analysis)
- [ ] Write Task description for feature_architect (recommendation synthesis)
- [ ] Configure tool sets for each agent (verify 3-5 tools max)
- [ ] Test each agent task individually

### Phase 4: Documentation & Testing (~1.5 hours)
- [ ] Update `Tools/CrewAI/docs/CREWAI.md` with System Analysis section
- [ ] Create example analysis doc in `docs/CrewAI_Plans/`
- [ ] Write basic flow execution test
- [ ] Write state transition test
- [ ] Write output format validation test
- [ ] Verify all success criteria met

## Risks & Mitigations

### Risk 1: Analysis Generates Generic Recommendations
**Likelihood:** Medium  
**Impact:** High  
**Mitigation:**
- Require code references (file:line) for all findings
- Use search_codebase to verify systems exist
- Validate recommendations against actual API patterns
- Provide specific data flow examples, not abstract descriptions

### Risk 2: Analysis References Non-Existent Code
**Likelihood:** Medium  
**Impact:** High  
**Mitigation:**
- Use search_codebase and read_source to verify all code references
- Feasibility validation step checks API compatibility
- Verify file paths with verify_file_exists_tool
- Mark any unverified references with ⚠️ WARNING

### Risk 3: Too Many Recommendations (Analysis Paralysis)
**Likelihood:** Medium  
**Impact:** Medium  
**Mitigation:**
- Limit to top 10 recommendations maximum
- Prioritize by impact/effort matrix
- Group related recommendations
- Focus on actionable items, not theoretical improvements

### Risk 4: Long Execution Time (5+ minutes)
**Likelihood:** Medium  
**Impact:** Medium  
**Mitigation:**
- Use reasoning_effort="high" only for complex analytical steps
- Set tool call limits in task descriptions
- Optimize search queries to be specific
- Monitor execution time during testing, adjust if needed

### Risk 5: Analysis Conflicts with User Knowledge
**Likelihood:** Low  
**Impact:** Low  
**Mitigation:**
- Present findings as observations, not mandates
- Include "Implementation Notes" with caveats
- User reviews analysis before creating plan
- Analysis informs planning, doesn't replace user judgment

## Future Enhancements (Post-MVP)

### Enhancement 1: Comparative Analysis Mode
Compare two system implementations to find best patterns:
```bash
enlisted-crew analyze-system "Supply,Morale" --compare-patterns
```

### Enhancement 2: Historical Trend Analysis
Analyze how system integration has evolved over time:
```bash
enlisted-crew analyze-system "Reputation" --historical --since "3-months-ago"
```

### Enhancement 3: Content Coverage Analysis
Find which systems are under-represented in content:
```bash
enlisted-crew analyze-system --all --content-coverage
```

### Enhancement 4: Batch Multi-System Analysis
Analyze all systems and generate integration matrix:
```bash
enlisted-crew analyze-system --all --matrix
```

### Enhancement 5: Integration Opportunity Scoring
Rank system pairs by integration potential:
```bash
enlisted-crew analyze-system --all --rank-opportunities
```

## Estimated Effort
**Total:** ~7.5 hours (1-2 development sessions)

**Breakdown:**
* Phase 1 (Core Flow): 3 hours
* Phase 2 (CLI): 1 hour
* Phase 3 (Agent Tasks): 2 hours
* Phase 4 (Docs/Tests): 1.5 hours

## Validation Plan

### Manual Testing Scenarios
1. **Single System:** `enlisted-crew analyze-system "Supply"`
    * Expected: Architecture + gaps + efficiency + recommendations
2. **Multiple Systems:** `enlisted-crew analyze-system "Supply,Morale,Reputation"`
    * Expected: Integration analysis between all three
3. **Integration Focus:** `enlisted-crew analyze-system "ContentOrchestrator" --focus integration`
    * Expected: Only integration gaps, skip efficiency analysis
4. **Subsystem:** `enlisted-crew analyze-system "CompanyNeedsManager" --subsystem`
    * Expected: Narrower scope analysis
5. **Warp Agent Handoff:** User asks Warp to create plan from analysis doc
    * Expected: Warp reads analysis and creates implementation plan

### Automated Tests
1. Flow execution completes without errors
2. State transitions correctly between methods
3. Output file created at correct path
4. Analysis doc contains all required sections
5. CLI arguments parsed correctly
6. Recommendations include effort estimates

### Success Metrics
* Execution time 3-5 minutes
* Analysis produces 5-10 actionable recommendations
* Zero hallucinated system references (all verified)
* User can create implementation plan from recommendations
* 80%+ of recommendations rated "accurate" by user
