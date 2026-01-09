# Innovation Flow Implementation - Creative Content Brainstorming System

> **Status:** 🔴 **Not Yet Implemented**  
> **Last Updated:** 2026-01-09  
> **Architecture:** Single-agent Crews (aligned with current v1.0 architecture)

## Problem Statement
The current CrewAI workflows (Planning, Implementation, BugHunting, Validation) are execution-focused. They lack a creative ideation phase for generating new content concepts that align with Enlisted's design principles ("emergent identity from choices, not menus").

When users have vague/exploratory feature ideas (e.g., "explore officer gameplay" or "add more camp content"), they currently jump straight to PlanningFlow, which works best with clear technical specifications.

## Current State
**Existing Workflows:**
* ImplementationFlow: Builds from approved plans (single-agent Crews)
* BugHuntingFlow: Finds and fixes issues (single-agent Crews)
* ValidationFlow: Pre-commit checks (single-agent Crews)

**Planning Approach:**
* Use Warp Agent directly for planning/design (full codebase context, faster than multi-agent)
* CrewAI flows focus on execution, not planning

**Existing Agents (10 total):**
* systems_analyst, architecture_advisor, feature_architect
* code_analyst, csharp_implementer, content_author
* content_analyst, qa_agent
* documentation_maintainer, balance_analyst

**Gap:** No agent specializes in creative ideation or identifying content gaps.

## Proposed Solution: InnovationFlow
Create a new standalone Flow that generates 5-10 content concepts through agent-driven brainstorming, aligned with project design principles.

### Architecture Pattern
**Agentic Systems Pattern (DocuSign/CrewAI best practice 2026):**
* Deterministic backbone (Flow) controls structure
* Single-agent Crews per step (not multi-agent Crews) - prevents excessive tool calls
* Intelligence (Agents) invoked at specific steps within clear boundaries
* High agency for experimental/creative work → solidifies into structured plans

**CRITICAL:** Each Flow step with intelligence uses a single-agent Crew with focused tool set (3-5 tools max). Multi-agent Crews cause 168 tool calls and timeouts.

### Flow Structure
```python
class InnovationFlow(Flow):
    @start()
    def analyze_inputs(self):
        # DETERMINISTIC: Check tier scope, load existing content (pure Python)
        
    @listen(analyze_inputs)
    def brainstorm_concepts(self):
        # SINGLE-AGENT CREW: innovation_designer generates concepts
        # Crew(agents=[agent], tasks=[task]) - NOT multi-agent
        
    @listen(brainstorm_concepts)
    def validate_alignment(self):
        # SINGLE-AGENT CREW: design_validator checks alignment
        # Crew(agents=[agent], tasks=[task]) - NOT multi-agent
        
    @listen(validate_alignment)
    def output_concept_doc(self):
        # DETERMINISTIC: Save to docs/CrewAI_Concepts/{name}.md (pure Python)
```

### New Agent: innovation_designer
**Role:** "Enlisted Content Innovation Designer"

**Goal:** Generate creative content concepts that create meaningful player identity through choices, aligned with Bannerlord's military simulation and emergent narrative principles

**Backstory:** Deep knowledge of military simulation games and narrative design. Understands Bannerlord systems and can identify gaps in existing 245 content pieces across rank progression (T1-T9).

**Tools (5 max per architecture guidelines):**
* search_codebase (semantic RAG search - find systems, search existing content in src/)
* search_docs_semantic (semantic RAG search - study design principles, find documentation)
* get_tier_info (understand rank progression from SQLite knowledge base)
* list_event_ids (avoid ID conflicts)
* lookup_content_id (check existing content from SQLite knowledge base)

Note: Uses ChromaDB vector indexes for semantic search. Both code and docs use RAG for fast retrieval.

**LLM:** GPT-5.2 with reasoning_effort="medium" (creative but grounded)

**Configuration:**
* max_iter=20
* reasoning=True
* max_reasoning_attempts=3
* allow_delegation=False

### New Agent: design_validator (Reusable)
**Role:** "Design Principles Validator"

**Goal:** Validate concepts against Enlisted design principles and technical feasibility

**Tools (4 - within architecture guidelines):**
* search_docs_semantic (check against design principles via semantic search)
* search_codebase (verify systems exist via semantic search)
* get_game_systems (load core game systems context)
* verify_file_exists_tool (ensure no hallucinated references)

**LLM:** GPT-5.2 with reasoning_effort="medium"

Note: Uses both semantic search tools for fast, accurate validation. Architecture recommends 3-5 tools max.

## CLI Integration
```bash
# New command
enlisted-crew innovate -f "officer-command-pressure" -t "T7-T9" -s "retinue,escalation"

# Arguments
-f, --feature: Feature name (required)
-t, --tier: Tier scope (default: "T1-T9")
-s, --systems: Related systems (optional)
```

## Output Format
**Location:** `docs/CrewAI_Concepts/{feature-name}.md`

**Structure:**
```markdown
# {Feature Name} - Content Concepts
Generated: {date}
Tier Scope: {tiers}
Systems: {systems}

## Concept 1: {Title}
**Player Choice:** {dilemma}
**Mechanical Hook:** {systems interaction}
**Identity Outcome:** {what this says about player}
**Tier Scope:** {T#-T#}
**Validation:** ✅ APPROVED / ⚠️ NEEDS_REVISION / ❌ REJECT

## Concept 2: ...
```

## Integration with Existing Workflows
**Current Architecture:** Single-agent Crews + Warp Agent for planning

CrewAI flows use single-agent Crews per step (not multi-agent) for optimal performance. Planning is handled by Warp Agent directly.

**Typical Workflow:**
1. User: `enlisted-crew innovate -f "officer-stress" -t "T7-T9"`
2. InnovationFlow generates 5-10 concepts → `docs/CrewAI_Concepts/officer-stress.md`
3. User reviews concepts in terminal
4. User picks "Concept 3: Fatigue Management System" and asks Warp Agent to create plan:
    * User: "Create implementation plan for Concept 3 from docs/CrewAI_Concepts/officer-stress.md"
    * Warp Agent reads concept doc and creates plan using full codebase context
5. User: `enlisted-crew implement -p docs/CrewAI_Plans/officer-stress.md`
6. ImplementationFlow executes with single-agent Crews per step

**Integration Points:**
* InnovationFlow outputs consumed by Warp Agent (planning)
* Warp Agent outputs consumed by ImplementationFlow (execution)
* All flows use SQLite knowledge base + ChromaDB semantic search

## Files to Create

### 1. Flow Implementation
**Path:** `src/enlisted_crew/flows/innovation_flow.py`  
**Size:** ~350 lines  
**Dependencies:** innovation_designer agent, design_validator agent

### 2. Agent Configuration
**Path:** `src/enlisted_crew/agents/innovation_designer.py`  
**Size:** ~80 lines  
**Configuration:** Role, goal, backstory, tools, LLM settings

**Path:** `src/enlisted_crew/agents/design_validator.py`  
**Size:** ~60 lines  
**Configuration:** Role, goal, backstory, tools, LLM settings

### 3. State Model
**Path:** `src/enlisted_crew/flows/state_models.py` (update existing)  
**Addition:** `InnovationState` Pydantic model (~30 lines)

### 4. CLI Command
**Path:** `src/enlisted_crew/main.py` (update existing)  
**Addition:** `innovate` subparser and `run_innovate()` function (~60 lines)

### 5. Flow Exports
**Path:** `src/enlisted_crew/flows/__init__.py` (update existing)  
**Addition:** Export `InnovationFlow`

### 6. Output Directory
**Path:** `docs/CrewAI_Concepts/` (create)  
**Purpose:** Store generated concept documents

### 7. Documentation
**Path:** `docs/CREWAI.md` (update existing)  
**Addition:** Section 4 "Innovate - Generate Content Concepts" (~40 lines)

### 8. Tests (Optional)
**Path:** `test_innovation_flow.py` (create)  
**Size:** ~150 lines  
**Coverage:** Flow execution, agent configuration, output format

## Technical Specifications

### Input Validation
* `feature_name`: Required, kebab-case recommended
* `tier`: Optional, default "T1-T9", format: "T#" or "T#-T#"
* `systems`: Optional, comma-separated list

### State Management (Pydantic)
```python
class InnovationState(BaseModel):
    feature_name: str
    tier_scope: str = "T1-T9"
    systems: list[str] = []
    existing_content_count: int = 0
    concepts: list[dict] = []
    validated_concepts: list[dict] = []
    final_report: str = ""
```

### Error Handling
* Invalid tier format → Error message with examples
* Empty concepts from agent → Retry with adjusted prompt
* File write failures → Log error, return concepts as string
* Agent timeout → Graceful degradation with partial results

### Tool Call Limits (Optimization - Architecture Pattern)
* brainstorm_concepts step (single-agent Crew): 5-10 tool calls max (not 10-15)
* validate_alignment step (single-agent Crew): 3-5 tool calls max (not 5-8)
* Total flow execution: 10-15 tool calls target (not 20-25)

**Architecture requirement:** Single-agent Crews per step drastically reduce calls. Multi-agent Crews cause 168 calls. Target per architecture doc is 5-10 calls per single-agent Crew step.

## Design Principles Alignment

### Project Principles (from BLUEPRINT.md)
1. ✅ **Emergent identity from choices, not menus** - Concepts focus on player dilemmas
2. ✅ **Native Bannerlord integration** - Uses existing systems (retinue, escalation, etc.)
3. ✅ **Data-driven content** - Outputs JSON-ready concept specifications
4. ✅ **Choice-driven narrative progression** - Each concept centers on meaningful choices

### CrewAI Best Practices (2026)
1. ✅ **Deterministic backbone + strategic intelligence** - Flow controls structure, agents provide creativity
2. ✅ **High agency for experimental work** - Innovation uses reasoning_effort="medium"
3. ✅ **Role-based specialization** - innovation_designer has clear, focused role
4. ✅ **Tool count guidelines** - 5 tools (within 3-5 max guideline)
5. ✅ **State persistence** - Pydantic models ensure type safety

## Success Criteria

### Functional Requirements
- [ ] Generate 5-10 distinct content concepts per execution
- [ ] Each concept includes: title, player choice, mechanical hook, identity outcome, tier scope
- [ ] Validation marks each concept: APPROVED / NEEDS_REVISION / REJECT
- [ ] Output saved to `docs/CrewAI_Concepts/{feature-name}.md`
- [ ] CLI command `enlisted-crew innovate` works with all flags

### Quality Requirements
- [ ] Concepts align with Enlisted design principles (emergent identity)
- [ ] No hallucinated systems/features (validated against codebase)
- [ ] Concepts span multiple tier ranges (not all T1 or all T9)
- [ ] Mechanical hooks reference actual game systems
- [ ] Execution completes in <3 minutes (GPT-5.2 medium reasoning)

### Integration Requirements
- [ ] No breaking changes to existing flows
- [ ] Concept docs usable as input to Warp Agent (not PlanningFlow - deprecated)
- [ ] Agent tools properly imported and configured
- [ ] Error messages helpful and actionable
- [ ] Documentation updated in CREWAI.md
- [ ] Each Flow step uses single-agent Crew pattern (not multi-agent)
- [ ] Tool counts respect 3-5 max per agent guideline

## Implementation Checklist

### Phase 1: Core Flow (~2 hours)
- [ ] Create `innovation_flow.py` with Flow structure
- [ ] Implement `InnovationState` Pydantic model
- [ ] Add `@start()` analyze_inputs method
- [ ] Add `@listen()` brainstorm_concepts method
- [ ] Add `@listen()` validate_alignment method
- [ ] Add `@listen()` output_concept_doc method
- [ ] Test flow execution end-to-end

### Phase 2: Agents (~1.5 hours)
- [ ] Create `innovation_designer.py` agent configuration
- [ ] Create `design_validator.py` agent configuration
- [ ] Configure tools for each agent
- [ ] Set LLM parameters (reasoning_effort, max_iter, etc.)
- [ ] Write Task descriptions with workflow steps
- [ ] Test agent task execution individually

### Phase 3: CLI Integration (~1 hour)
- [ ] Add `innovate` subparser to `main.py`
- [ ] Implement `run_innovate()` function
- [ ] Add argument parsing (-f, -t, -s flags)
- [ ] Connect to InnovationFlow.kickoff()
- [ ] Test CLI command with various inputs


### Phase 4: Documentation & Testing (~1 hour)
- [ ] Update `docs/CREWAI.md` with Innovate section
- [ ] Create example concept doc in `docs/CrewAI_Concepts/`
- [ ] Update `README.md` with new command
- [ ] Write basic flow execution test
- [ ] Verify all success criteria met

## Risks & Mitigations

### Risk 1: Agent Generates Off-Brand Concepts
**Likelihood:** Medium  
**Impact:** High  
**Mitigation:**
* Load design principles into agent backstory
* Use design_validator agent to catch misalignment
* Provide examples of good concepts in task description

### Risk 2: Concepts Reference Non-Existent Systems
**Likelihood:** Medium  
**Impact:** High  
**Mitigation:**
* Provide search_codebase tool to verify systems exist
* design_validator explicitly checks for hallucinations
* Validation marks hallucinated concepts as REJECT

### Risk 3: Too Generic Concepts ("Add more events")
**Likelihood:** Medium  
**Impact:** Medium  
**Mitigation:**
* Task description requires specific player dilemmas
* Require mechanical hook that names specific systems
* Set min word count for concept descriptions

### Risk 4: HITL @human_feedback Issues (CrewAI 1.8.0)
**Likelihood:** Low (not using native HITL)  
**Impact:** N/A  
**Mitigation:**
* Use simple CLI input() for user prompts
* Avoid undocumented @human_feedback decorator
* Can upgrade to native HITL when 1.8.0+ officially released

### Risk 5: Slow Execution (GPT-5.2 Reasoning)
**Likelihood:** Low  
**Impact:** Medium  
**Mitigation:**
* Use reasoning_effort="medium" (not "high")
* Set tool call limits in task descriptions
* Monitor execution time during testing
* Consider caching for repeated concepts

## Future Enhancements (Post-MVP)

### Enhancement 1: Concept Refinement Loop
Allow users to provide feedback on concepts and re-run brainstorming:
```bash
enlisted-crew innovate -f "officer-stress" --refine "focus more on T8-T9"
```

### Enhancement 2: Concept-to-Plan Direct Integration
Auto-generate plan from approved concept:
```bash
enlisted-crew plan-from-concept -c "docs/CrewAI_Concepts/officer-stress.md" -n 3
```

### Enhancement 3: Gap Analysis Mode
Automatically identify content gaps across all tiers:
```bash
enlisted-crew innovate --analyze-gaps
```

### Enhancement 4: Batch Concept Generation
Generate concepts for multiple feature areas:
```bash
enlisted-crew innovate --batch "officer-gameplay,camp-life,maritime-content"
```

### Enhancement 5: Native HITL Upgrade
When CrewAI 1.8.0+ officially released with stable HITL:
* Replace CLI input() with @human_feedback decorator
* Support webhook-based approval workflows
* Enable async concept review

## Estimated Effort
**Total:** ~4.5 hours (1 development session)

**Breakdown:**
* Phase 1 (Core Flow): 2 hours
* Phase 2 (Agents): 1.5 hours
* Phase 3 (CLI): 1 hour
* Phase 4 (Docs/Tests): 1 hour

## Validation Plan

### Manual Testing Scenarios
1. **Vague Feature:** `enlisted-crew innovate -f "officer-stuff" -t "T7-T9"`
    * Expected: 5-10 concepts, all focused on T7-T9 officers
2. **Specific System:** `enlisted-crew innovate -f "baggage-events" -s "baggage_train"`
    * Expected: Concepts that use baggage train mechanics
3. **Full Tier Range:** `enlisted-crew innovate -f "camp-decisions"`
    * Expected: Concepts spanning T1-T9
4. **Warp Agent Handoff:** User asks Warp to create plan from concept doc
    * Expected: Warp reads concept doc and creates implementation plan

### Automated Tests
1. Flow execution completes without errors
2. State transitions correctly between methods
3. Output file created at correct path
4. Concept validation produces expected format
5. CLI arguments parsed correctly

### Success Metrics
* Execution time <3 minutes
* 80%+ concepts marked APPROVED or NEEDS_REVISION (not REJECT)
* Zero hallucinated system references
* Concepts span at least 3 different tier ranges
* User can use concept doc as input to Warp Agent for planning
