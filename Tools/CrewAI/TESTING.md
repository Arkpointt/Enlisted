# CrewAI Testing Guide

Comprehensive guide for testing and validating CrewAI workflows in the Enlisted mod project.

## Quick Start

### Basic Test Command

```bash
cd Tools/CrewAI
crewai test
```

This runs your crew multiple times (default: 2 iterations) and provides performance metrics.

### Advanced Test Command

```bash
crewai test --n_iterations 5 --model gpt-5-mini
```

Parameters:
- `--n_iterations N` - Number of test runs (default: 2)
- `--model MODEL` - Override LLM model for testing (default: gpt-4o-mini)

Note: Only OpenAI models are supported for testing as of CrewAI v0.90+.

## Test Workflows

### 1. Testing the Planning Workflow

```bash
# From Tools/CrewAI directory
enlisted-crew plan "Add quartermaster dialogue for equipment quality inspection"
```

Expected outputs:
- Planning document in `docs/CrewAI_Plans/`
- Document should include:
  - Feature overview
  - Technical requirements
  - Implementation steps
  - Acceptance criteria
  - Effort estimates

### 2. Testing the Implementation Workflow

```bash
# Requires an existing plan file
enlisted-crew implement docs/CrewAI_Plans/supply-pressure-arc-test.md
```

Expected outputs:
- C# code files written to `src/Features/`
- JSON content files written to `ModuleData/Enlisted/Events/` or `/Orders/` or `/Decisions/`
- XML localization strings in `ModuleData/Languages/enlisted_strings.xml`
- Updated plan file with "Implemented" status and summary

Check with `git diff` after completion.

### 3. Testing the Bug Hunting Workflow

```bash
enlisted-crew hunt-bug --description "Player can't access town after battle" --error-codes "none" --repro "Win battle, try to enter town menu"
```

Expected outputs:
- Investigation report with severity assessment
- Proposed fix (C# code)
- Validation report (build status, style checks)
- Final comprehensive bug report

## Memory Testing

CrewAI now uses **all three memory types** automatically when `memory=True`:

### Short-Term Memory
- Temporarily stores recent interactions during current execution
- Uses RAG for context retrieval within the current workflow run

### Long-Term Memory
- Preserves insights from past executions
- Agents learn patterns like "tier-aware events need variant IDs"
- Stored in: `~/Library/Application Support/CrewAI/{project_name}/long_term_memory/`

### Entity Memory
- Tracks entities (systems, characters, factions, concepts)
- Builds relationship maps for better context understanding
- Example entities: `EnlistmentBehavior`, `ContentOrchestrator`, `Hero.MainHero`, `Tier System`
- Stored in: `~/Library/Application Support/CrewAI/{project_name}/entities/`

### Memory Storage Location (Windows)

```
C:\Users\{username}\AppData\Local\CrewAI\enlisted_crew\
├── knowledge/                    # ChromaDB embeddings for knowledge sources
├── short_term_memory/            # Current execution context
├── long_term_memory/             # Learnings across runs
└── entities/                     # Entity relationships
```

To inspect or clear memory:
```bash
# View memory contents
ls ~/AppData/Local/CrewAI/enlisted_crew/

# Clear memory (fresh start)
rm -rf ~/AppData/Local/CrewAI/enlisted_crew/long_term_memory/
rm -rf ~/AppData/Local/CrewAI/enlisted_crew/entities/
```

## Planning Feature Testing

All workflows now have `planning=True` enabled, which means:
1. Before each crew iteration, an `AgentPlanner` creates a step-by-step execution plan
2. This plan is added to each task description
3. Agents understand the full workflow before executing

### What to Look For
- Console output showing "Planning..." before agent execution
- More coherent agent outputs (agents understand their place in the workflow)
- Better task decomposition
- Fewer repeated searches or redundant tool calls

## Performance Metrics

When using `crewai test`, watch for:

### Success Rate
- Should be 90%+ for stable workflows
- Lower rates indicate prompt issues or API failures

### Token Usage
- Track average tokens per run
- Optimize if consistently hitting limits
- Current limits:
  - GPT-5.2: 128K context, 16K output
  - GPT-5 mini: 128K context, 8K output
  - GPT-5 nano: 128K context, 4K output

### Execution Time
- Planning workflow: ~2-5 minutes (depends on feature complexity)
- Implementation workflow: ~5-15 minutes (writes actual files)
- Bug hunting workflow: ~3-10 minutes (depends on bug severity)

### Common Issues

**Issue**: "Planning not supported"
- Make sure you're on CrewAI v0.90 or later
- Check: `pip show crewai`

**Issue**: "Test command not found"
- Only works with OpenAI models
- Update `.env` to use GPT models for testing

**Issue**: Memory not persisting
- Check file permissions in `AppData/Local/CrewAI/`
- Verify embedder config is correct (text-embedding-3-large)

## Manual Validation Checklist

After running a workflow, validate:

### Planning Workflow
- [ ] Plan file created in `docs/CrewAI_Plans/`
- [ ] All required sections present (Overview, Requirements, Steps, etc.)
- [ ] Effort estimates are reasonable
- [ ] Acceptance criteria are measurable
- [ ] Tier-aware design principles applied (if relevant)

### Implementation Workflow
- [ ] C# code files compile (`dotnet build`)
- [ ] Code follows Enlisted style (Allman braces, _camelCase)
- [ ] JSON content validates (`python Tools/Validation/validate_content.py`)
- [ ] XML strings synchronized (`python Tools/Validation/sync_event_strings.py`)
- [ ] Plan file updated with implementation status
- [ ] `git diff` shows only expected changes

### Bug Hunting Workflow
- [ ] Investigation identifies correct root cause
- [ ] Proposed fix is minimal and targeted
- [ ] Code compiles and passes style checks
- [ ] Fix addresses the symptoms described in bug report
- [ ] No regressions introduced

## Automated Test Suite (Future)

Planned additions:
- Unit tests for custom tools
- Integration tests for workflows
- Regression tests for memory system
- Performance benchmarks

## Debugging Failed Runs

### Enable Verbose Logging
All crews already have `verbose=True`, but you can also:

```bash
# Set environment variable for more detailed output
export CREWAI_LOG_LEVEL=DEBUG
enlisted-crew plan "test feature"
```

### Check Memory State
```python
# Inspect what entities the crew has learned
from crewai import Crew
crew = Crew(...)  # Your crew
# Memory is stored in platform-specific AppData directory
```

### Clear Cache
If behavior seems stuck or incorrect:
```bash
# Clear knowledge embeddings
rm -rf ~/AppData/Local/CrewAI/enlisted_crew/knowledge/

# Crews will rebuild embeddings on next run
```

## Best Practices

1. **Start Small**: Test with simple features before complex multi-system changes
2. **Validate Often**: Run `dotnet build` and `validate_content.py` after implementation
3. **Check Diffs**: Always `git diff` to verify exactly what was changed
4. **Monitor Memory**: Watch for agents learning incorrect patterns (e.g., hallucinations)
5. **Iterate**: If a workflow fails, check the outputs and adjust prompts/knowledge
6. **Use Entity Memory**: Complex systems benefit from entity tracking (e.g., `EnlistmentBehavior` relationships)

## Troubleshooting

### Workflow Hangs
- Check console for API errors (rate limits, token limits)
- Reduce chunk_size if ChromaDB errors occur
- Verify OpenAI API key is valid

### Incorrect Outputs
- Check if agent has correct tools
- Verify knowledge sources are loaded
- Review backstories and task descriptions
- Clear long-term memory if agents learned bad patterns

### Memory Issues
- Entity memory may grow large over time
- Periodically review and prune if needed
- Monitor AppData directory size

## Next Steps

After validating workflows:
1. Run performance benchmarks (`crewai test --n_iterations 10`)
2. Document any patterns or issues discovered
3. Refine agent backstories based on learnings
4. Expand knowledge base with new insights
5. Consider adding custom evaluation metrics

---

For more details, see:
- `CREWAI.md` - Configuration and architecture
- `Tools/AGENT-WORKFLOW.md` - Detailed workflow specs
- Official CrewAI docs: https://docs.crewai.com/concepts/testing
