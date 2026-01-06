# Migrate CrewAI Flows to Hierarchical Process

## Problem Statement
Current flows create **separate single-agent crews per step** with `Process.sequential`. Each step (research, design, write, validate) creates its own isolated Crew:

```python
# Current pattern in planning_flow.py (lines 405-419)
crew = Crew(
    agents=[get_systems_analyst()],  # Only ONE agent
    tasks=[task],
    process=Process.sequential,
    # ...
)
```

**What's missing:**
* Manager coordination and strategic task delegation
* Cross-agent validation of outputs  
* Efficient token usage through shared context
* Agent collaboration within same crew
* Corporate hierarchy emulation for complex workflows

**Current agent configuration issues:**
* `systems_analyst`, `architecture_advisor`, `feature_architect` have `allow_delegation=True` (lines 171, 212, 262 in planning_flow.py)
* These are NOT managers but have delegation enabled
* Should be `allow_delegation=False` for specialists

## Solution Overview
Refactor each flow to use a **single Crew with `Process.hierarchical`** and a **custom manager agent** that coordinates all specialist agents.

## Key Requirements (from CrewAI Official Docs)

### Hierarchical Process Requirements
* Must set `process=Process.hierarchical`
* Must provide either `manager_llm` OR `manager_agent` (we use custom manager)
* Manager coordinates workflow, delegates tasks, validates outcomes
* Tools assigned at agent level facilitate delegation
* Custom manager gives more control over behavior
* Despite being hierarchical, tasks follow logical order via manager's oversight

### Manager Role in Hierarchical (CRITICAL - from docs)
The manager agent **oversees task execution** including:
1. **Planning** - Determines execution strategy
2. **Delegation** - Allocates tasks to agents based on their capabilities
3. **Validation** - Reviews outputs and assesses task completion

**Key Behavior:** Tasks are **NOT pre-assigned** in hierarchical mode. The manager:
* Allocates tasks to agents as needed (based on capabilities)
* Reviews outputs from each agent
* Assesses whether tasks are complete
* Can request re-work if output doesn't meet standards

This is the core value of hierarchical: **Manager checks work quality before proceeding.**

### Custom Manager Agent Requirements
* `allow_delegation=True` (REQUIRED - enables delegation to crew members)
* High-capability LLM for strategic decision-making
* Clear backstory about coordination responsibilities
* Goal focused on quality and efficient task completion
* Manager oversees entire process, ensuring tasks completed efficiently

### Agent Customization (from docs)
* `allow_delegation=False` is now the DEFAULT for regular agents
* `max_iter=20` default (not 25 - corrected from docs)
* `max_retry_limit=2` default
* `respect_context_window=True` default - summarizes to keep under context window
* `verbose=False` default (we set True for debugging)
* `step_callback` - function called after each agent step
* `function_calling_llm` - separate LLM for tool calls (overrides crew's)
* Agent capabilities: perform tasks, make decisions, use tools, communicate, maintain memory, **delegate tasks when allowed**

## Agent Collaboration (from Official Docs)

### Automatic Collaboration Tools
When `allow_delegation=True`, CrewAI automatically provides agents with TWO tools:

**1. Delegate Work Tool**
```python
# Agent automatically gets this tool:
Delegate work to coworker(task: str, context: str, coworker: str)
```
* Allows agents to assign tasks to teammates with specific expertise
* Manager uses this to delegate to specialists

**2. Ask Question Tool**
```python
# Agent automatically gets this tool:
Ask question to coworker(question: str, context: str, coworker: str)
```
* Enables agents to ask specific questions to gather information
* Specialists can ask each other for clarification

### Collaboration Strategy for Hierarchical
**Manager Agent**: `allow_delegation=True`
* Uses Delegate Work Tool to assign tasks to specialists
* Uses Ask Question Tool to clarify requirements
* Validates outputs before proceeding

**Specialist Agents**: `allow_delegation=False` (default)
* Focus on their domain expertise
* Cannot delegate to others (prevents confusion)
* Execute assigned tasks directly

**Exception - Cross-Specialist Questions**:
For complex workflows, specialists MAY need `allow_delegation=True` to ask questions:
```python
# Feature Architect may need to ask Systems Analyst
feature_architect = Agent(
    role="Feature Architect",
    allow_delegation=True,  # Can ASK questions (but shouldn't delegate)
    ...
)
```

## Conditional Tasks (from Official Docs)

### ConditionalTask for Dynamic Workflows
`ConditionalTask` enables tasks to execute ONLY when conditions are met:
```python
from crewai.tasks.conditional_task import ConditionalTask
from crewai.tasks.task_output import TaskOutput

def needs_deep_validation(output: TaskOutput) -> bool:
    """Condition: Only run if complex systems mentioned."""
    content = str(output.raw).lower()
    return "contentorchestrator" in content or "state machine" in content

deep_validation = ConditionalTask(
    description="Deep system integration validation...",
    expected_output="Deep validation report",
    condition=needs_deep_validation,
    agent=get_architecture_advisor(),
)
```

### Use Cases in Our Flows
**1. PlanningFlow - Deep Validation (already implemented)**
* Trigger deep validation only for complex system integrations
* Saves time on simple features

**2. ImplementationFlow - Conditional C#/Content**
```python
def needs_csharp_implementation(output: TaskOutput) -> bool:
    """Only implement C# if plan includes new classes."""
    return "class" in str(output.raw).lower() or ".cs" in str(output.raw)

csharp_task = ConditionalTask(
    description="Implement C# code...",
    condition=needs_csharp_implementation,
    agent=get_csharp_implementer(),
)
```

**3. BugHuntingFlow - Severity-Based Analysis**
```python
def needs_systems_analysis(output: TaskOutput) -> bool:
    """Only analyze systems for critical/high severity bugs."""
    content = str(output.raw).lower()
    return "critical" in content or "high" in content

systems_analysis = ConditionalTask(
    description="Analyze related systems for impact...",
    condition=needs_systems_analysis,
    agent=get_systems_analyst(),
)
```

### ConditionalTask + Hierarchical Process
In hierarchical mode, the manager still coordinates, but conditional tasks:
* Are evaluated BEFORE manager delegates
* Skip automatically if condition returns False
* Reduce unnecessary work and token usage

## Human-in-the-Loop Integration

### Two HITL Patterns to Preserve
**1. Task-Level HITL (`human_input=True`)**
* Agent can ask clarifying questions DURING task execution
* Used on design task for major uncertainties
* Pauses agent, prompts human, continues with answer

**2. Flow-Level HITL (`@human_feedback` decorator)**
* Pauses BETWEEN flow steps for human review
* Parameters:
    * `message`: What human sees (make it actionable)
    * `emit`: List of outcomes for routing (natural language)
    * `llm`: LLM to classify free-text feedback into outcomes
    * `default_outcome`: Fallback if user presses Enter
* Returns `HumanFeedbackResult` with `.output`, `.feedback`, `.outcome`, `.timestamp`
* Separate `@listen` handlers for each outcome

### Current HITL in PlanningFlow (preserve this)
```python
@human_feedback(
    message="Review UNCLEAR gaps...",
    emit=["deprecated", "implement", "test_code", "continue"],
    llm="gpt-5.2",
    default_outcome="continue",
)
def review_unclear_gaps(self, state): ...

@listen("deprecated")
def handle_deprecated_gap(self, result): ...
```

## Detailed Implementation Plan

### Phase 1: Create Manager Agent Factory AND Fix Specialist Delegation
**Files**: All flow files (`planning_flow.py`, `implementation_flow.py`, `bug_hunting_flow.py`, `validation_flow.py`)

**CRITICAL: Fix Current Agents First**

Currently in `planning_flow.py`, these agents incorrectly have `allow_delegation=True`:
* Line 171: `systems_analyst` (should be False)
* Line 212: `architecture_advisor` (should be False)  
* Line 262: `feature_architect` (should be False)

**Change all specialists to:**
```python
allow_delegation=False,  # Specialists don't delegate (only managers do)
```

**Manager Agent Design Pattern**:
```python
def get_planning_manager() -> Agent:
    return Agent(
        role="Planning Coordinator",
        goal="Efficiently coordinate research, design, and documentation to produce high-quality feature plans",
        backstory="""You are an experienced project manager coordinating a feature planning team.
        Your role is to:
        1. Delegate research tasks to Systems Analyst
        2. Request architectural advice from Architecture Advisor
        3. Coordinate design work with Feature Architect
        4. Ensure Documentation Maintainer captures all decisions
        5. Validate outputs meet quality standards before proceeding
        
        You ensure each specialist contributes their expertise while maintaining
        overall coherence and quality of the planning document.""",
        llm=GPT5_ARCHITECT,  # High reasoning for coordination
        allow_delegation=True,  # REQUIRED for manager
        verbose=True,
        max_iter=30,  # Higher for coordination work
        max_retry_limit=3,
    )
```

**Create 4 Manager Agents**:
1. `get_planning_manager()` - Coordinates research → design → documentation
2. `get_implementation_manager()` - Coordinates C# → content → QA
3. `get_bug_hunting_manager()` - Coordinates investigation → fix → validate
4. `get_validation_manager()` - Coordinates content → build → report

### Phase 2: Refactor PlanningFlow
**Current Architecture** (5 separate crews):
```
receive_request → research_systems (Crew1) → suggest_improvements (Crew2)
→ design_feature (Crew3) → [human_review] → write_plan (Crew4)
→ validate_plan (Crew5) → [fix loop] → generate_report
```

**New Architecture** (1 hierarchical crew + Flow orchestration):
```
receive_request → run_planning_crew (single hierarchical Crew)
→ [human_review if UNCLEAR] → validate_and_fix_loop → generate_report
```

**Key Changes**:
1. Consolidate research, suggest, design, write into ONE hierarchical crew
2. Manager coordinates task delegation based on agent capabilities
3. Keep validation as separate step (needs different agent set)
4. Preserve Flow-level HITL for UNCLEAR gaps
5. Preserve task-level HITL (`human_input=True`) on design task

**New Flow Structure**:
```python
@start()
def receive_request(self) -> PlanningState: ...

@listen(receive_request)
def run_planning_crew(self, state: PlanningState) -> PlanningState:
    """Single hierarchical crew for all planning work."""
    # Define all tasks with context dependencies
    research_task = Task(
        description="Research existing systems...",
        expected_output="Research summary",
        agent=get_systems_analyst(),
    )
    
    advise_task = Task(
        description="Suggest architectural improvements...",
        expected_output="Recommendations",
        agent=get_architecture_advisor(),
        context=[research_task],  # Depends on research
    )
    
    design_task = Task(
        description="Create technical specification with gap analysis...",
        expected_output="Technical spec",
        agent=get_feature_architect(),
        context=[research_task, advise_task],
        human_input=True,  # PRESERVE task-level HITL
    )
    
    write_task = Task(
        description="Write planning document...",
        expected_output="Plan saved to disk",
        agent=get_documentation_maintainer(),
        context=[design_task],
    )
    
    # Single hierarchical crew
    crew = Crew(
        agents=[
            get_systems_analyst(),
            get_architecture_advisor(),
            get_feature_architect(),
            get_documentation_maintainer(),
        ],
        tasks=[research_task, advise_task, design_task, write_task],
        manager_agent=get_planning_manager(),
        process=Process.hierarchical,
        verbose=True,
        memory=True,
        cache=True,
        planning=True,
        planning_llm=GPT5_PLANNING,
    )
    
    result = crew.kickoff()
    # ... update state
    return state

@listen(run_planning_crew)
@router
def route_after_planning(self, state) -> str:
    if "unclear" in state.design_output.lower():
        return "human_review"
    return "validate"

# PRESERVE existing HITL handlers
@listen("human_review")
@human_feedback(...)
def review_unclear_gaps(self, state): ...

@listen("deprecated")
def handle_deprecated_gap(self, result): ...
# etc.

@listen(or_("validate", "deprecated", "implement", "test_code", "continue"))
def validate_plan(self, state): ...
```

### Phase 3: Refactor ImplementationFlow
**Current**: Separate crews for verify, implement_csharp, implement_content, validate, document

**New**: Single hierarchical crew with Implementation Manager + ConditionalTasks

**Agents**:
* Systems Analyst (verify existing)
* C# Implementer (write code)
* Content Author (write JSON)
* QA Agent (validate)
* Documentation Maintainer (update docs)

**Implementation Manager Design**:
```python
def get_implementation_manager() -> Agent:
    return Agent(
        role="Implementation Lead",
        goal="Coordinate efficient implementation of approved plans with quality validation",
        backstory="""You lead an implementation team that builds features from approved plans.
        Your responsibilities:
        1. Verify what's already implemented (delegate to Systems Analyst)
        2. Coordinate C# implementation ONLY if needed (conditional)
        3. Coordinate content creation ONLY if needed (conditional)
        4. Ensure QA validates all changes
        5. Update documentation to reflect completed work
        
        You skip unnecessary work and ensure quality at each step.""",
        llm=GPT5_ARCHITECT,
        allow_delegation=True,
        verbose=True,
        max_iter=35,
        max_retry_limit=3,
    )
```

**ConditionalTask Integration**:
```python
def needs_csharp_work(output: TaskOutput) -> bool:
    """Check if C# implementation needed based on verification output."""
    content = str(output.raw).lower()
    return "not_started" in content or "partial" in content

def needs_content_work(output: TaskOutput) -> bool:
    """Check if JSON content creation needed."""
    content = str(output.raw).lower()
    return "not_started" in content or "partial" in content

# In the hierarchical crew:
verify_task = Task(
    description="Verify existing implementation...",
    agent=get_systems_analyst(),
)

csharp_task = ConditionalTask(
    description="Implement C# code for missing components...",
    condition=needs_csharp_work,
    agent=get_csharp_implementer(),
    context=[verify_task],
)

content_task = ConditionalTask(
    description="Create JSON content for missing events/decisions...",
    condition=needs_content_work,
    agent=get_content_author(),
    context=[verify_task],
)
```

**Manager Coordination**:
* Manager delegates verification to Systems Analyst first
* ConditionalTasks auto-skip if work already complete
* Manager validates each output before proceeding
* Ensures QA validates the complete implementation

**Preserve**:
* Flow routing based on `csharp_status` and `content_status`
* State persistence for resumability

### Phase 4: Refactor BugHuntingFlow
**Current**: Separate crews for investigate, analyze, fix, validate

**New**: Single hierarchical crew with Bug Hunting Manager + ConditionalTasks

**Agents**:
* Code Analyst (find bug location)
* Systems Analyst (analyze impact) - CONDITIONAL
* C# Implementer (create fix)
* QA Agent (validate fix)

**Bug Hunting Manager Design**:
```python
def get_bug_hunting_manager() -> Agent:
    return Agent(
        role="Bug Triage Manager",
        goal="Efficiently investigate, fix, and validate bug reports with appropriate depth",
        backstory="""You lead a bug hunting team that triages and resolves issues.
        Your responsibilities:
        1. Delegate investigation to Code Analyst
        2. Assess severity from investigation results
        3. For CRITICAL/HIGH: delegate systems analysis (conditional)
        4. Coordinate fix implementation
        5. Ensure QA validates the fix thoroughly
        
        You adapt investigation depth based on bug severity.""",
        llm=GPT5_ARCHITECT,
        allow_delegation=True,
        verbose=True,
        max_iter=30,
        max_retry_limit=3,
    )
```

**ConditionalTask Integration**:
```python
def needs_systems_analysis(output: TaskOutput) -> bool:
    """Only analyze systems for critical/high severity bugs."""
    content = str(output.raw).lower()
    return "critical" in content or "high" in content or "multi-system" in content

def needs_deep_fix(output: TaskOutput) -> bool:
    """Check if fix requires architectural changes."""
    content = str(output.raw).lower()
    return "state machine" in content or "behavior" in content

# In the hierarchical crew:
investigate_task = Task(
    description="Investigate bug location and severity...",
    agent=get_code_analyst(),
)

systems_analysis_task = ConditionalTask(
    description="Analyze related systems for impact...",
    condition=needs_systems_analysis,
    agent=get_systems_analyst(),
    context=[investigate_task],
)

fix_task = Task(
    description="Implement minimal fix for the bug...",
    agent=get_csharp_implementer(),
    context=[investigate_task, systems_analysis_task],  # Gets both if available
)

validate_task = Task(
    description="Validate fix builds and passes tests...",
    agent=get_qa_agent(),
    context=[fix_task],
)
```

**Manager Coordination**:
* Manager delegates investigation to Code Analyst first
* ConditionalTask auto-skips systems analysis for simple bugs
* Manager reviews fix proposal before QA validation
* Validates fix quality before completion

### Phase 5: Refactor ValidationFlow
**Current**: Separate crews for content validation, build validation, reporting

**New**: Single hierarchical crew with Validation Manager

**Agents**:
* Content Validator
* Build Validator
* QA Reporter

**Validation Manager Design**:
```python
def get_validation_manager() -> Agent:
    return Agent(
        role="QA Coordinator",
        goal="Coordinate comprehensive validation to ensure quality before release",
        backstory="""You lead a QA team that validates all changes before release.
        Your responsibilities:
        1. Delegate content validation (JSON schemas, strings)
        2. Delegate build validation (compilation, no warnings)
        3. Coordinate final QA report
        4. Ensure no issues are missed
        
        You ensure all validation passes before approving.""",
        llm=GPT5_ANALYST,  # Medium reasoning sufficient
        allow_delegation=True,
        verbose=True,
        max_iter=20,
        max_retry_limit=3,
    )
```

**Simpler Flow**: Validation is more straightforward, manager ensures all checks pass.

### Phase 6: Update CREWAI.md Documentation
Add new section:
```markdown
## Hierarchical Process Architecture

All flows use `Process.hierarchical` with custom manager agents:

### Manager Agents

| Flow | Manager | Coordinates |
|------|---------|-------------|
| PlanningFlow | Planning Coordinator | Research → Design → Documentation |
| ImplementationFlow | Implementation Lead | C# → Content → QA |
| BugHuntingFlow | Bug Triage Manager | Investigation → Fix → Validation |
| ValidationFlow | QA Coordinator | Content → Build → Report |

### Why Hierarchical?

1. **Strategic Delegation** - Manager assigns tasks based on agent capabilities
2. **Quality Validation** - Manager validates outputs before proceeding
3. **Efficient Token Usage** - Shared context across agents
4. **Clear Accountability** - Manager responsible for overall quality

### HITL Patterns Preserved

- Task-level: `human_input=True` on design tasks
- Flow-level: `@human_feedback` for UNCLEAR gap review
```

## Technical Details (from Official Docs)

### Production Architecture: Flow-First Mindset
From CrewAI docs: "When building production AI applications with CrewAI, we recommend starting with a Flow."

**Why Flows for production:**
* **State Management**: Built-in way to manage state across steps
* **Control**: Define precise execution paths, loops, conditionals, branching
* **Observability**: Clear structure for tracing, debugging, monitoring

**Our architecture follows this pattern:**
```
Flow (PlanningFlow/ImplementationFlow/etc.)
  └── Crew (single hierarchical crew)
        └── Tasks (with context dependencies)
              └── Agents (specialists + manager)
```

### Task Configuration (from docs)
**Key Task Attributes:**
```python
Task(
    description="...",
    expected_output="...",
    agent=agent,
    context=[previous_task],  # Tasks whose outputs become context
    tools=[specific_tools],   # Override agent tools for this task
    human_input=True,         # Enable HITL during execution
    async_execution=False,    # Set True for parallel execution
    output_file="output.md",  # Auto-save output to file
    markdown=True,            # Enable markdown formatting
    guardrails=[validate_fn], # Validate output before proceeding (NEW!)
    guardrail_max_retries=3,  # Retries on guardrail failure
)
```

**Guardrails for Validation** (NEW - from docs):
```python
def validate_no_hallucinations(output: TaskOutput) -> Tuple[bool, Any]:
    """Guardrail: Check output doesn't reference non-existent files."""
    content = str(output.raw)
    # Check for suspicious patterns
    if "DOES_NOT_EXIST" in content or "hallucinated" in content.lower():
        return (False, "Output contains hallucinated references")
    return (True, output)

def validate_has_required_sections(output: TaskOutput) -> Tuple[bool, Any]:
    """Guardrail: Check plan has required sections."""
    content = str(output.raw).lower()
    required = ["overview", "technical specification", "implementation"]
    missing = [s for s in required if s not in content]
    if missing:
        return (False, f"Missing sections: {missing}")
    return (True, output)

# Usage in task:
write_task = Task(
    description="Write planning document...",
    agent=get_documentation_maintainer(),
    guardrails=[validate_has_required_sections],
    guardrail_max_retries=2,
)
```

### Crew Configuration (from docs)
**Key Crew Attributes:**
```python
Crew(
    agents=[...],
    tasks=[...],
    process=Process.hierarchical,
    manager_agent=get_xxx_manager(),
    verbose=True,
    memory=True,
    cache=True,
    planning=True,
    planning_llm=GPT5_PLANNING,
    # NEW options from docs:
    function_calling_llm=GPT5_FAST,  # Crew-wide LLM for tool calls
    output_log_file="crew_output.log",  # Log all outputs
    task_callback=on_task_complete,  # Called after each task
    step_callback=on_agent_step,     # Called after each agent step
)
```

### Specialist Agent Configuration (unchanged)
```python
Agent(
    role="...",
    goal="...",
    backstory="...",
    llm=GPT5_...,
    tools=[...],
    allow_delegation=False,  # Specialists don't delegate
    max_iter=15-25,
    max_retry_limit=3,
    verbose=True,
    respect_context_window=True,
)
```

### Manager Agent Configuration (new)
```python
Agent(
    role="[Flow] Coordinator",
    goal="Efficiently coordinate team to produce high-quality output",
    backstory="Experienced coordinator who delegates and validates...",
    llm=GPT5_ARCHITECT,  # High reasoning for coordination
    allow_delegation=True,  # REQUIRED - gives Delegate Work + Ask Question tools
    max_iter=30,  # Higher for coordination
    max_retry_limit=3,
    verbose=True,
)
```

### Flow State Management (from docs)
**Structured State with Pydantic (recommended):**
```python
from pydantic import BaseModel

class PlanningState(BaseModel):
    # Note: 'id' field is automatically added by CrewAI
    feature_name: str = ""
    description: str = ""
    research_output: str = ""
    design_output: str = ""
    validation_status: ValidationStatus = ValidationStatus.PENDING
    # ... etc

class PlanningFlow(Flow[PlanningState]):
    @start()
    def receive_request(self) -> PlanningState:
        print(f"Flow State ID: {self.state.id}")  # Auto-generated UUID
        # ...
```

**Flow Decorators:**
* `@start()` - Entry point, can have multiple (execute in parallel when Flow begins)
* `@start("label")` - Gated start, runs when label is emitted
* `@start(condition=callable)` - Conditional start, only fires when callable returns True
* `@listen(method)` - Runs when method completes
* `@listen("label")` - Runs when label is emitted
* `@router(method)` - Returns string label to route execution
* `@human_feedback(...)` - Pauses for human input, routes based on response
* `@persist` - Save state after method execution (class-level or method-level)

**Logical Operators for Complex Triggering:**
```python
from crewai.flow.flow import or_, and_

# or_ - Trigger when ANY method completes
@listen(or_(method_a, method_b))
def handler(self, result): ...  # Runs when EITHER completes

# and_ - Trigger when ALL methods complete
@listen(and_(method_a, method_b))
def handler(self, result): ...  # Runs when BOTH complete
```

**Use Cases:**
* `or_`: Consolidate multiple paths (e.g., all HITL handlers → single next step)
* `and_`: Wait for parallel work to finish before proceeding

### Task Context Dependencies
In hierarchical process, use `context` parameter to specify dependencies:
```python
task_b = Task(
    description="...",
    agent=agent_b,
    context=[task_a],  # task_b receives task_a output
)
```

### Structured Outputs (from Production Architecture docs)
Always use structured outputs when passing data between tasks:
```python
from pydantic import BaseModel

class ResearchResult(BaseModel):
    summary: str
    sources: List[str]
    related_systems: List[str]

research_task = Task(
    description="Research existing systems...",
    expected_output="Research summary with sources",
    agent=get_systems_analyst(),
    output_pydantic=ResearchResult,  # Structured output!
)
```

**Benefits:**
* Prevents parsing errors
* Ensures type safety
* Clear contract between tasks
* Manager can validate structure before proceeding

### Knowledge Integration (from docs)
Knowledge can be at **Crew level** OR **Agent level**:

**Agent-Specific Knowledge:**
```python
from crewai.knowledge.source.string_knowledge_source import StringKnowledgeSource

specialist_knowledge = StringKnowledgeSource(
    content="Technical specifications only the specialist needs"
)

specialist = Agent(
    role="Technical Specialist",
    goal="Provide technical expertise",
    backstory="Technical expert",
    knowledge_sources=[specialist_knowledge],  # Agent-specific
)
```

**Crew-Wide Knowledge:**
```python
crew_knowledge = StringKnowledgeSource(
    content="Information all agents need"
)

crew = Crew(
    agents=[...],
    tasks=[...],
    knowledge_sources=[crew_knowledge],  # Crew-wide
)
```

**Our Usage:**
* Crew-wide: Core systems, balance values, error codes
* Agent-specific: Specialized domain knowledge per agent

## Guardrails Integration (NEW from docs)

### Why Guardrails?
Guardrails validate task output BEFORE proceeding to next task. This catches errors early:
* Hallucinated file paths
* Missing required sections
* Invalid content IDs
* Malformed JSON

### Guardrail Function Signature
```python
def guardrail_fn(output: TaskOutput) -> Tuple[bool, Any]:
    """Returns (passed: bool, result_or_error: Any)"""
    if validation_fails:
        return (False, "Error message")  # Retries up to guardrail_max_retries
    return (True, output)  # Proceeds to next task
```

### Guardrails for Each Flow
**PlanningFlow Guardrails:**
```python
def validate_plan_structure(output: TaskOutput) -> Tuple[bool, Any]:
    """Ensure plan has required sections."""
    content = str(output.raw).lower()
    required = ["overview", "technical specification", "files to create"]
    missing = [s for s in required if s not in content]
    if missing:
        return (False, f"Missing required sections: {missing}")
    return (True, output)

def validate_no_hallucinated_paths(output: TaskOutput) -> Tuple[bool, Any]:
    """Check file paths look valid."""
    content = str(output.raw)
    # Check for obviously wrong patterns
    suspicious = ["src/NonExistent", "PLACEHOLDER", "TODO_PATH"]
    found = [s for s in suspicious if s in content]
    if found:
        return (False, f"Suspicious paths found: {found}")
    return (True, output)

# Apply to write_task:
write_task = Task(
    description="Write planning document...",
    agent=get_documentation_maintainer(),
    guardrails=[validate_plan_structure, validate_no_hallucinated_paths],
    guardrail_max_retries=2,
)
```

**ImplementationFlow Guardrails:**
```python
def validate_csharp_syntax(output: TaskOutput) -> Tuple[bool, Any]:
    """Basic C# syntax checks."""
    content = str(output.raw)
    # Check for unclosed braces, missing semicolons in obvious places
    if content.count('{') != content.count('}'):
        return (False, "Unbalanced braces in C# code")
    return (True, output)

def validate_json_syntax(output: TaskOutput) -> Tuple[bool, Any]:
    """Check JSON is valid."""
    import json
    try:
        # Extract JSON blocks and validate
        # ... implementation
        return (True, output)
    except json.JSONDecodeError as e:
        return (False, f"Invalid JSON: {e}")
```

**BugHuntingFlow Guardrails:**
```python
def validate_fix_is_minimal(output: TaskOutput) -> Tuple[bool, Any]:
    """Ensure fix doesn't change too much."""
    content = str(output.raw)
    # Count lines changed indicators
    if "complete rewrite" in content.lower():
        return (False, "Fix should be minimal, not a complete rewrite")
    return (True, output)
```

## Migration Checklist

### Per Flow:
- [ ] Create manager agent factory function
- [ ] Consolidate separate crews into single hierarchical crew
- [ ] Define all tasks with proper context dependencies
- [ ] Set `process=Process.hierarchical`
- [ ] Set `manager_agent=get_xxx_manager()`
- [ ] Add guardrails to key tasks (write, implement, fix)
- [ ] Preserve HITL patterns (both task-level and flow-level)
- [ ] Update state management to work with consolidated output
- [ ] Test flow end-to-end

### Documentation:
- [ ] Update CREWAI.md with hierarchical process section
- [ ] Document manager agent responsibilities
- [ ] Document guardrails and their purpose
- [ ] Update agent configuration patterns

## Testing Strategy

### Baseline Metrics (Before Migration)
Run current flows with CrewAI's built-in testing:
```bash
cd Tools/CrewAI
.venv\Scripts\Activate.ps1

# Test each flow 10 times to establish baseline
crewai test --n_iterations 10 --model gpt-4o
```

**Record baseline metrics:**
- Task scores (1-10 scale per task)
- Crew overall score
- Execution time (average, min, max)
- Token usage (via observability tools)
- Success rate

**Example output:**
```
Tasks/Crew/Agents │ Run 1 │ Run 2 │ ... │ Avg. Total
Task 1 (Research) │  9.0  │  9.5  │ ... │    9.2
Task 2 (Design)   │  9.0  │ 10.0 │ ... │    9.5
Crew              │  9.00 │  9.38 │ ... │    9.2
Execution Time    │  126s │  145s │ ... │   135s
```

### Hierarchical-Specific Tests

**1. Manager Delegation Validation**
Verify manager delegates correctly (doesn't take over all tasks):
```python
# Add to test suite
def test_manager_delegates_correctly():
    """Ensure manager delegates tasks to specialists, not executes them."""
    task_executions = {}  # Track which agent executed which task
    
    def track_task(output):
        agent_role = output.agent.role if output.agent else "Unknown"
        task_executions[output.description[:50]] = agent_role
    
    crew = Crew(
        agents=[get_systems_analyst(), get_feature_architect(), ...],
        tasks=[research_task, design_task, ...],
        manager_agent=get_planning_manager(),
        process=Process.hierarchical,
        task_callback=track_task,
    )
    
    result = crew.kickoff()
    
    # Assertions
    specialist_tasks = [t for t, agent in task_executions.items() 
                       if "Manager" not in agent]
    assert len(specialist_tasks) > 0, "Manager executed all tasks!"
    assert "Systems Analyst" in task_executions.values()
    assert "Feature Architect" in task_executions.values()
```

**2. HITL Compatibility Test**
```python
def test_hitl_with_hierarchical():
    """Verify both HITL patterns work with hierarchical process."""
    # Task-level HITL
    design_task = Task(
        description="Design feature...",
        agent=get_feature_architect(),
        human_input=True,  # Should still trigger
    )
    
    crew = Crew(
        agents=[...],
        tasks=[research_task, design_task],
        manager_agent=get_planning_manager(),
        process=Process.hierarchical,
    )
    
    # Test with mock human input
    # Verify agent pauses for input correctly
    
    # Flow-level HITL (@human_feedback) is tested at Flow layer
    # Should work independently of Crew process type
```

**3. Infinite Loop Prevention Test**
```python
def test_manager_respects_max_iter():
    """Ensure manager stops after max_iter, doesn't loop forever."""
    manager = Agent(
        role="Test Manager",
        allow_delegation=True,
        max_iter=5,  # Low limit for testing
        llm=GPT5_ARCHITECT,
    )
    
    crew = Crew(
        agents=[...],
        tasks=[...],
        manager_agent=manager,
        process=Process.hierarchical,
    )
    
    start_time = time.time()
    result = crew.kickoff()
    duration = time.time() - start_time
    
    # Should complete, not timeout
    assert duration < 300, "Crew exceeded reasonable timeout"
```

### Pilot Testing Procedure

**ValidationFlow Pilot (Simplest Flow First):**
1. Implement hierarchical ValidationFlow
2. Add monitoring (callbacks, logging)
3. Run `crewai test --n_iterations 10`
4. Compare to baseline metrics
5. Manual testing with various inputs

**Success Criteria for Pilot:**
```python
success_criteria = {
    "task_scores": lambda new, baseline: new >= baseline * 0.95,  # Allow 5% variance
    "execution_time": lambda new, baseline: new <= baseline * 1.2,  # Max 20% slower
    "manager_delegates": lambda logs: "delegate" in logs.lower(),  # Manager uses delegation
    "no_infinite_loops": lambda logs: logs.count("Agent Action") < 100,  # Reasonable action count
    "hitl_works": True,  # Manual verification
}
```

**If pilot fails any criterion → STOP, investigate, potentially abort migration.**

### Regression Testing

**After each flow migration:**
1. Run full test suite: `crewai test --n_iterations 20`
2. Compare metrics to baseline
3. Manual smoke tests (run actual use cases)
4. Check logs for warnings/errors
5. Git commit with metrics in commit message

## Monitoring and Observability

### Real-Time Monitoring with Callbacks

**Task Callbacks** - Monitor each task completion:
```python
def monitor_task_completion(output: TaskOutput):
    """Log task execution details."""
    print(f"✓ Task: {output.description[:50]}")
    print(f"  Agent: {output.agent.role if output.agent else 'Unknown'}")
    print(f"  Duration: {getattr(output, 'duration', 'N/A')}")
    print(f"  Output length: {len(str(output.raw))}")
    
    # Log to file for analysis
    with open("logs/task_execution.log", "a") as f:
        f.write(f"{datetime.now()} | {output.agent.role} | {output.description[:50]}\n")

crew = Crew(
    agents=[...],
    tasks=[...],
    manager_agent=get_planning_manager(),
    process=Process.hierarchical,
    task_callback=monitor_task_completion,  # Called after EACH task
    output_log_file="logs/crew_output.log",  # Persistent logging
)
```

**Step Callbacks** - Monitor individual agent steps:
```python
def monitor_agent_steps(step):
    """Track agent actions in real-time."""
    print(f"Agent step: {step}")
    
    # Detect potential issues
    if "delegate" not in str(step).lower() and "Manager" in str(step):
        print("⚠️  WARNING: Manager may be executing tasks directly!")

crew = Crew(
    agents=[...],
    tasks=[...],
    manager_agent=get_planning_manager(),
    process=Process.hierarchical,
    step_callback=monitor_agent_steps,  # Called after EACH agent action
)
```

### Production Observability (Optional)

Integrate external observability tools for production monitoring:

**AgentOps Integration:**
```python
import agentops

agentops.init(api_key=os.environ["AGENTOPS_API_KEY"])

crew = Crew(
    agents=[...],
    tasks=[...],
    manager_agent=get_planning_manager(),
    process=Process.hierarchical,
)

result = crew.kickoff()

# AgentOps automatically tracks:
# - Token usage and costs
# - Latency per task
# - Agent interactions
# - Session replays for debugging
```

**Langtrace Integration:**
```python
from langtrace_python_sdk import langtrace

langtrace.init(api_key=os.environ["LANGTRACE_API_KEY"])

# Now all CrewAI executions are traced:
# - Token usage per agent
# - Execution flow visualization
# - Bottleneck identification
# - Cost tracking
```

**Usage Metrics (Built-in):**
```python
crew = Crew(
    agents=[...],
    tasks=[...],
    manager_agent=get_planning_manager(),
    process=Process.hierarchical,
)

result = crew.kickoff()

# Access metrics after execution
metrics = crew.usage_metrics
print(f"Total tokens: {metrics.get('total_tokens')}")
print(f"Prompt tokens: {metrics.get('prompt_tokens')}")
print(f"Completion tokens: {metrics.get('completion_tokens')}")
```

## Risk Mitigation

### Preventing Infinite Loops

**Manager Agent Configuration:**
```python
manager = Agent(
    role="Planning Manager",
    goal="Coordinate planning efficiently",
    backstory="...",
    llm=GPT5_ARCHITECT,
    allow_delegation=True,
    max_iter=30,  # CRITICAL: Hard stop after 30 iterations
    max_retry_limit=3,  # Retry failed operations max 3 times
    respect_context_window=True,  # Auto-summarize to prevent token overflow
    verbose=True,  # Enable to monitor behavior
)
```

**Crew-Level Safeguards:**
```python
crew = Crew(
    agents=[...],
    tasks=[...],
    manager_agent=manager,
    process=Process.hierarchical,
    max_rpm=60,  # Rate limiting: max 60 requests per minute
    cache=True,  # Prevent redundant work
)
```

### Detecting Manager Takeover

**Use callbacks to detect if manager executes tasks directly:**
```python
manager_task_count = 0
specialist_task_count = 0

def detect_takeover(output: TaskOutput):
    global manager_task_count, specialist_task_count
    
    agent_role = output.agent.role if output.agent else "Unknown"
    
    if "Manager" in agent_role or "Coordinator" in agent_role:
        manager_task_count += 1
    else:
        specialist_task_count += 1
    
    # Alert if manager doing too much work
    if manager_task_count > specialist_task_count:
        print("🚨 ALERT: Manager executing more tasks than specialists!")
        print(f"   Manager: {manager_task_count}, Specialists: {specialist_task_count}")

crew = Crew(
    agents=[...],
    tasks=[...],
    manager_agent=get_planning_manager(),
    process=Process.hierarchical,
    task_callback=detect_takeover,
)
```

### Automatic Abort on Issues

**Implement safety checks in callbacks:**
```python
class SafetyMonitor:
    def __init__(self):
        self.agent_actions = []
        self.max_actions = 100  # Abort if exceeded
    
    def step_callback(self, step):
        self.agent_actions.append(step)
        
        if len(self.agent_actions) > self.max_actions:
            raise RuntimeError(
                f"Crew exceeded {self.max_actions} agent actions. "
                "Possible infinite loop detected. Aborting."
            )

monitor = SafetyMonitor()

crew = Crew(
    agents=[...],
    tasks=[...],
    manager_agent=get_planning_manager(),
    process=Process.hierarchical,
    step_callback=monitor.step_callback,
)

try:
    result = crew.kickoff()
except RuntimeError as e:
    print(f"Safety abort triggered: {e}")
    # Log incident, alert team, rollback if needed
```

## Additional Flow Patterns (from Official Docs)

### State Persistence with @persist
The `@persist` decorator enables state recovery after failures:
```python
from crewai.flow.flow import Flow, start, listen, persist

# Class-level: Saves state after EVERY method
@persist
class PlanningFlow(Flow[PlanningState]):
    @start()
    def receive_request(self): ...

# Method-level: Saves state after specific methods only
class PlanningFlow(Flow[PlanningState]):
    @start()
    @persist
    def critical_step(self): ...  # State saved after this method
```

**Benefits:**
* Resume from last successful step after crash/restart
* SQLite backend by default (`SQLiteFlowPersistence`)
* Can provide custom `FlowPersistence` implementation

### Flow Visualization with plot()
Generate interactive HTML visualization of flow structure:
```python
flow = PlanningFlow()
flow.plot("planning_flow_diagram")  # Creates planning_flow_diagram.html
result = flow.kickoff()
```

**CLI Alternative:**
```bash
crewai flow plot  # Generates flow diagram for project
```

**Use:** Debug flow structure, documentation, onboarding.

### Using Agents Directly in Flows (Lightweight)
For simpler tasks, use agents directly without full Crew overhead:
```python
from crewai import Agent

@listen(previous_step)
def simple_analysis(self, state):
    agent = Agent(
        role="Quick Analyzer",
        goal="Perform simple analysis",
        llm=GPT5_FAST,
    )
    # Direct agent execution (no Crew wrapper)
    result = agent.execute_task(
        task="Analyze this: " + state.data,
        context="Additional context here",
    )
    state.analysis = result
    return state
```

**When to use:**
* Single-agent tasks
* No inter-agent coordination needed
* Faster execution, less overhead

### Accessing Human Feedback History
After `@human_feedback` steps, access collected feedback:
```python
@listen("continue")
def after_human_review(self, result):
    # Most recent feedback
    last = self.last_human_feedback
    print(f"Feedback: {last.feedback}")
    print(f"Outcome: {last.outcome}")
    print(f"Timestamp: {last.timestamp}")
    
    # All feedback collected during flow
    for fb in self.human_feedback_history:
        print(f"{fb.timestamp}: {fb.outcome} - {fb.feedback}")
```

### Flow Tracing for Observability
Enable tracing for debugging and monitoring:
```python
class PlanningFlow(Flow[PlanningState]):
    def __init__(self):
        super().__init__(tracing=True)  # Enable tracing

# Or at instantiation:
flow = PlanningFlow(tracing=True)
```

**Benefits:** Real-time monitoring via CrewAI AMP dashboard.

### Batch Execution with kickoff_for_each()
Process multiple inputs efficiently:
```python
flow = PlanningFlow()
inputs_list = [
    {"feature_name": "feature-a", "description": "..."},
    {"feature_name": "feature-b", "description": "..."},
    {"feature_name": "feature-c", "description": "..."},
]

# Synchronous batch
results = flow.kickoff_for_each(inputs=inputs_list)

# Async batch
results = await flow.kickoff_for_each_async(inputs=inputs_list)
```

### Human Feedback Without Routing
Collect feedback without emitting routing outcomes:
```python
@start()
@human_feedback(message="Any comments on this output?")
def get_optional_feedback(self):
    return "Output for review"

@listen(get_optional_feedback)
def next_step(self, result):
    # result.feedback contains human's text (if any)
    # result.output contains the original output
    if result.feedback:
        self.state.notes = result.feedback
    return self.state
```

## Rollback Plan
If hierarchical causes issues:
* Keep old sequential crews in commented code
* Can revert by changing `process=Process.sequential` and removing `manager_agent`
* State models unchanged, so state persistence still works
