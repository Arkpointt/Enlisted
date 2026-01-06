# CrewAI Testing Guide

Quick reference for testing Enlisted CrewAI flows after implementation.

---

## Prerequisites

1. **Environment Setup:**
   ```bash
   cd C:\Dev\Enlisted\Enlisted\Tools\CrewAI
   .\.venv\Scripts\Activate.ps1
   ```

2. **API Key:**
   ```bash
   # Verify key is set
   $env:OPENAI_API_KEY
   ```

3. **Installation Check:**
   ```bash
   python -c "import crewai; print(crewai.__version__)"
   ```

---

## Quick Test (Automated)

Run all three flows with real scenarios:

```bash
.\test_flows.ps1
```

This script will:
- Activate virtual environment
- Verify installation and API key
- Test PlanningFlow with sample feature
- Test BugHuntingFlow with sample bug
- Provide next steps for validation

**Monitoring is automatic:** All test runs are tracked in the database. Check performance with:
```bash
enlisted-crew stats
```

---

## Individual Flow Testing

### Test PlanningFlow

Design a simple feature to validate the workflow:

```bash
enlisted-crew plan -f "Test Feature" -d "A simple test feature with minimal scope"
```

**Expected Output:**
- Plan file created in `docs/CrewAI_Plans/test-feature.md`
- Status: "Planning" or "Validated"
- Sections: Overview, Technical Approach, Implementation Steps, Acceptance Criteria
- No hallucinated file paths or content IDs (auto-fixed if found)

**Validation:**
- Read generated plan for quality
- Verify file references are accurate
- Check if database was queried correctly

---

### Test BugHuntingFlow

Investigate and propose fix for a known issue:

```bash
enlisted-crew hunt-bug -d "Supply pressure events not triggering during daily simulation" -e "E-CAMP-*"
```

**Expected Output:**
- Bug report with investigation findings
- Severity classification (critical/high/medium/low)
- Root cause analysis
- Proposed fix with affected files
- Validation results

**Validation:**
- Review investigation accuracy
- Check if systems analysis was thorough
- Verify proposed fix is minimal and focused
- Confirm validation checks passed

---

### Test ImplementationFlow

Build a feature from an approved plan:

```bash
enlisted-crew implement -p "docs/CrewAI_Plans/test-feature.md"
```

**Expected Output:**
- C# files written to `src/Features/`
- JSON content written to `ModuleData/Enlisted/`
- Localization strings added to `enlisted_strings.xml`
- New files added to `Enlisted.csproj`
- Plan status updated to "Implemented"
- Documentation synchronized

**Validation:**
- Run `git diff` to review all changes
- Build project: `dotnet build -c "Enlisted RETAIL" /p:Platform=x64`
- Validate content: `python Tools/Validation/validate_content.py`
- Check database: `sqlite3 enlisted_knowledge.db "SELECT * FROM implementation_history ORDER BY id DESC LIMIT 1;"`
- Review feature docs for updates

---

## Using Official `crewai test`

For performance benchmarking across multiple iterations:

```bash
# Basic test (2 iterations, default model)
crewai test

# Custom iterations and model
crewai test -n 3 -m gpt-5

# Short form
crewai test -n 5 --model gpt-5
```

**What It Measures:**
- Task completion quality (0-10 scale)
- Agent effectiveness
- Crew coordination
- Execution time per run
- Average performance across iterations

**Example Output:**
```
Tasks/Crew/Agents       Run 1    Run 2    Run 3    Avg. Total
------------------------------------------------------------
Task: research          8.5      9.0      8.8      8.77
Task: design            7.8      8.2      8.0      8.00
Agent: systems_analyst  8.9      9.1      8.7      8.90
Agent: feature_architect 8.0     8.5      8.3      8.27
Crew Overall            8.2      8.6      8.4      8.40
Execution Time (s)      126      145      138      136
```

---

## Performance Baselines

After initial testing, establish baselines for regression detection:

| Flow | Target Quality | Max Execution Time | Notes |
|------|---------------|--------------------|-------|
| PlanningFlow | 8.0+ | 180s | Research phase slowest |
| ImplementationFlow | 8.5+ | 300s | C# generation most complex |
| BugHuntingFlow | 8.0+ | 200s | Investigation variable |

**When to Re-baseline:**
- After major CrewAI version updates
- After changing LLM models or parameters
- After adding/removing agents or tools
- After knowledge base updates

---

## Post-Test Validation Checklist

After any flow completes:

### 1. File System Changes
- [ ] Run `git status` - verify expected files changed
- [ ] Run `git diff` - review all modifications
- [ ] Check no unintended files were modified

### 2. Code Quality
- [ ] Build succeeds: `dotnet build`
- [ ] No new ReSharper warnings
- [ ] C# follows project style (comments, naming)

### 3. Content Validation
- [ ] Run `python Tools/Validation/validate_content.py`
- [ ] All JSON schemas valid
- [ ] All content IDs unique and follow conventions
- [ ] No missing localization strings

### 4. Database Integrity
```bash
sqlite3 enlisted_knowledge.db
.tables                          # Verify all tables exist
SELECT COUNT(*) FROM content_metadata;  # Should match JSON file count
SELECT * FROM implementation_history ORDER BY id DESC LIMIT 3;  # Check logging
.exit
```

### 5. Documentation Sync
- [ ] Plan status updated (if implementing)
- [ ] Feature docs updated with new content
- [ ] `INDEX.md` references added
- [ ] Implementation summary added to plan file

---

## Troubleshooting

### "Module not found: crewai"
```bash
cd Tools/CrewAI
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
```

### "OPENAI_API_KEY not set"
```bash
# Set for current session
$env:OPENAI_API_KEY = "sk-..."

# Or add to .env file
echo "OPENAI_API_KEY=sk-..." >> .env
```

### "Flow failed mid-run"
**Good news:** Flows have `persist=True` - just re-run the same command. The flow will resume from the last successful step.

```bash
# Re-run the exact same command
enlisted-crew implement -p "docs/CrewAI_Plans/feature.md"
```

### "Hallucinated files/IDs in plan"
**Good news:** PlanningFlow has auto-fix enabled. If validation detects hallucinations, it will automatically correct them (up to 2 attempts).

If auto-fix fails, manually review the plan and correct any:
- Non-existent file paths
- Invalid content IDs
- Incorrect API references

### "Performance scores low"
Review these factors:
- **Prompt Quality:** More specific prompts yield better results
- **Model Selection:** Ensure using GPT-5 (not default gpt-4o-mini)
- **Knowledge Base:** May need updates if codebase changed significantly
- **Agent Tools:** Verify all tools are working correctly

---

## Next Steps After Testing

1. **Commit Changes:**
   ```bash
   git add -A
   git commit -m "feat: [description] via CrewAI [flow_name]"
   ```

2. **Run In-Game Test:**
   - Build and launch Bannerlord
   - Test the implemented feature
   - Check for runtime errors in `Modules/Enlisted/Debugging/`

3. **Iterate If Needed:**
   - If bugs found, use `enlisted-crew hunt-bug`
   - If feature incomplete, re-run `enlisted-crew implement` (smart routing!)
   - If design needs changes, update plan and re-implement

4. **Document Learnings:**
   - Update this guide with any new insights
   - Add edge cases to knowledge base if discovered
   - Record any API patterns for future reference

---

## Related Documentation

- [CREWAI.md](CREWAI.md) - Complete CrewAI documentation
- [../../WARP.md](../../WARP.md) - Project-wide conventions
- [../AGENT-WORKFLOW.md](../AGENT-WORKFLOW.md) - Multi-agent patterns
- [../../docs/BLUEPRINT.md](../../docs/BLUEPRINT.md) - System architecture
