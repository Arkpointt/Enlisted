# Multi-Agent Workflow for Enlisted Mod Development

A single-conversation workflow where Warp adopts specialist "agent" perspectives to analyze, implement, and validate changes. All agents operate within ONE chat window — no separate sessions needed.

---

## Quick Start

**Just describe your task.** Warp will automatically:
1. **Triage** → Classify the request
2. **Analyze** → Gather context with specialist perspective(s)
3. **Implement** → Make changes with domain expertise
4. **Validate** → Run checks before finishing

**Or invoke a specific phase:**
```
"[ANALYZE] Why does the quartermaster crash when supply is zero?"
"[IMPLEMENT] Add a new camp decision for weapon maintenance"
"[VALIDATE] Check my changes before I commit"
```

**For complex work, use CrewAI** (see [CrewAI Integration](#crewai-integration)):
```
"Use CrewAI bug_hunting_crew to investigate E-ENCOUNTER-042"
"Use CrewAI planning_crew to design the skill integration feature"
"Use CrewAI full_feature_crew to implement the approved spec"
```

---

## The Agent Pipeline

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        YOUR CONVERSATION                                 │
│                     (Single chat window)                                 │
└────────────────────────────────┬────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  PHASE 1: TRIAGE                                                         │
│  Warp classifies request → routes to appropriate analyst(s)             │
│  Output: "This is a [CONTENT/CODE/CRASH/MIXED] task"                    │
└────────────────────────────────┬────────────────────────────────────────┘
                                 │
        ┌────────────────────────┼────────────────────────────┐
        ▼                        ▼                            ▼
┌───────────────┐      ┌───────────────┐            ┌───────────────┐
│ C# ANALYST    │      │CONTENT ANALYST│            │ CRASH ANALYST │
│               │      │               │            │               │
│ • src/ code   │      │ • JSON events │            │ • Session logs│
│ • .csproj     │      │ • Decisions   │            │ • Error codes │
│ • Harmony     │      │ • Orders      │            │ • Stack traces│
│ • API safety  │      │ • Localization│            │ • Conflicts   │
└───────┬───────┘      └───────┬───────┘            └───────┬───────┘
        │                      │                            │
        └──────────────────────┼────────────────────────────┘
                               ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  PHASE 2: ANALYSIS REPORT                                                │
│  Warp synthesizes findings from all relevant analysts                   │
│  Output: Summary + recommended approach + any clarifying questions      │
└────────────────────────────────┬────────────────────────────────────────┘
                                 │
                                 ▼ (after your approval)
┌─────────────────────────────────────────────────────────────────────────┐
│  PHASE 3: IMPLEMENTATION                                                 │
│  Specialist implementers make changes                                   │
│                                                                         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                     │
│  │C# IMPLEMENT │  │CONTENT AUTH │  │PATCH SPECIAL│                     │
│  │             │  │             │  │             │                     │
│  │• Add to     │  │• Write JSON │  │• Harmony    │                     │
│  │  .csproj    │  │• Field order│  │• DLC guards │                     │
│  │• TextObject │  │• Tooltips   │  │• Crash fixes│                     │
│  │• Sea guards │  │• Sync XML   │  │• Edge cases │                     │
│  └─────────────┘  └─────────────┘  └─────────────┘                     │
└────────────────────────────────┬────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  PHASE 4: QA VALIDATION                                                  │
│  Warp runs validators and reports results                               │
│                                                                         │
│  • python Tools/Validation/validate_content.py                          │
│  • dotnet build -c "Enlisted RETAIL" /p:Platform=x64                    │
│  • python Tools/Validation/sync_event_strings.py --check                │
│                                                                         │
│  Output: Pass/fail + issues to address                                  │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Agent Specialist Perspectives

### Analyst Agents (Phase 2 — Read-Only)

| Agent | Trigger Keywords | What It Examines | Key Outputs |
|-------|-----------------|------------------|-------------|
| **C# Analyst** | code, behavior, crash, manager, patch | `src/`, `.csproj`, Harmony patches | Affected classes, API safety, patterns |
| **Content Analyst** | event, decision, order, dialogue, story | `ModuleData/Enlisted/`, JSON files | Schema compliance, pacing, localization |
| **Crash Analyst** | crash, exception, error, stuck, freeze | `Modules/Enlisted/Debugging/` logs | Error codes, stack traces, root cause |
| **Narrative Voice Agent** | writing, tone, style, voice, prose | Setup/option/result text in JSON | Style violations, token usage, length |
| **Balance Agent** | balance, effects, rewards, economy, XP | Effects objects in JSON | Risk/reward ratio, progression fit |

### Implementer Agents (Phase 3 — Makes Changes)

| Agent | Domain | Pre-Flight Checks | Post-Implementation |
|-------|--------|-------------------|---------------------|
| **C# Implementer** | `src/**/*.cs` | Add to .csproj, check API in decompile | Build, fix warnings |
| **Content Author** | `ModuleData/Enlisted/**/*.json` | Field ordering, tooltip presence | Validate, sync XML |
| **Patch Specialist** | Harmony patches, crash fixes | Settlement/siege guards, DLC checks | Test edge cases |

### QA Agent (Phase 4 — Validation)

Runs automatically after implementation:
```powershell
# Content validation
python Tools/Validation/validate_content.py

# Build check
dotnet build -c "Enlisted RETAIL" /p:Platform=x64

# Localization sync check
python Tools/Validation/sync_event_strings.py --check
```

---

## Warp Agent Profile Setup

Create these profiles in `Settings > AI > Agents > Profiles` to run specialist agents in parallel tabs.

### Profile: Analyst
**Purpose:** Read-only investigation (C# Analyst, Content Analyst, Crash Analyst, Narrative Voice, Balance)

| Setting | Value |
|---------|-------|
| **Name** | `Analyst` |
| **Base model** | `auto (cost-efficient)` |
| **Apply code diffs** | `Never` |
| **Read files** | `Always allow` |
| **Directory allowlist** | `C:\Dev\Enlisted\Enlisted` |
| **Execute commands** | `Always ask` |
| **Command allowlist** | `git.*`, `python.*validate.*`, `ls.*`, `cat.*` |
| **Interact with running commands** | `Never` |
| **Call MCP servers** | `Never` |
| **Call web tools** | Off |
| **Plan auto-sync** | Off |

**Starter prompt for this profile:**
```
You are an Analyst for the Enlisted mod. Your job is READ-ONLY investigation.
NEVER make changes. Only report findings and recommendations.
Refer to Tools/AGENT-WORKFLOW.md for your specialist checklists.
```

---

### Profile: Implementer
**Purpose:** Code and content creation (C# Implementer, Content Author, Patch Specialist)

| Setting | Value |
|---------|-------|
| **Name** | `Implementer` |
| **Base model** | `claude-4-opus` or best available |
| **Apply code diffs** | `Agent decides` |
| **Read files** | `Always allow` |
| **Directory allowlist** | `C:\Dev\Enlisted\Enlisted` |
| **Execute commands** | `Always ask` |
| **Command allowlist** | `dotnet build.*`, `git add.*`, `git status` |
| **Interact with running commands** | `Always ask` |
| **Call MCP servers** | `Agent decides` |
| **Call web tools** | Off |
| **Plan auto-sync** | On |

**Starter prompt for this profile:**
```
You are an Implementer for the Enlisted mod. You write C# code and JSON content.
Follow the rules in Tools/AGENT-WORKFLOW.md exactly:
- Add new .cs files to Enlisted.csproj
- Use correct JSON field ordering (fallback after ID)
- All options need tooltips under 100 chars
- Order events MUST grant skillXp
```

---

### Profile: QA
**Purpose:** Validation and testing (runs validators, checks builds)

| Setting | Value |
|---------|-------|
| **Name** | `QA` |
| **Base model** | `auto (cost-efficient)` |
| **Apply code diffs** | `Never` |
| **Read files** | `Always allow` |
| **Directory allowlist** | `C:\Dev\Enlisted\Enlisted` |
| **Execute commands** | `Agent decides` |
| **Command allowlist** | `python Tools/Validation/.*`, `dotnet build.*`, `git diff.*` |
| **Interact with running commands** | `Always ask` |
| **Call MCP servers** | `Never` |
| **Call web tools** | Off |
| **Plan auto-sync** | Off |

**Starter prompt for this profile:**
```
You are QA for the Enlisted mod. Your job is to VALIDATE, not fix.
Run these checks and report results:
1. python Tools/Validation/validate_content.py
2. dotnet build -c "Enlisted RETAIL" /p:Platform=x64
3. python Tools/Validation/sync_event_strings.py --check
Report pass/fail. Do NOT make changes.
```

---

### Profile: Narrative Voice
**Purpose:** Writing style review (specialized Analyst for prose quality)

| Setting | Value |
|---------|-------|
| **Name** | `Narrative Voice` |
| **Base model** | `claude-4-opus` or best available |
| **Apply code diffs** | `Never` |
| **Read files** | `Always allow` |
| **Directory allowlist** | `C:\Dev\Enlisted\Enlisted` |
| **Execute commands** | `Never` |
| **Call web tools** | Off |

**Starter prompt for this profile:**
```
You are the Narrative Voice reviewer for Enlisted. Check ALL text against:
- No exclamation marks
- No named emotions ("You feel X" → physical description)
- Sentences 8-12 words avg, max 20
- Options under 8 words, verb-first
- Tooltips under 80 chars, mechanical only
- No modern terms (stressed, trauma, facility)
- Use tokens: {SERGEANT} not "the sergeant"
Refer to docs/Features/Content/writing-style-guide.md
```

---

### Profile: Balance
**Purpose:** Effects and economy review (specialized Analyst for game balance)

| Setting | Value |
|---------|-------|
| **Name** | `Balance` |
| **Base model** | `auto (cost-efficient)` |
| **Apply code diffs** | `Never` |
| **Read files** | `Always allow` |
| **Directory allowlist** | `C:\Dev\Enlisted\Enlisted` |
| **Execute commands** | `Never` |
| **Call web tools** | Off |

**Starter prompt for this profile:**
```
You are the Balance reviewer for Enlisted. Check ALL effects against:
- Gold: T1-T3 max ~50, T4-T6 max ~100, T7+ max ~200
- Reputation: single event rarely exceeds ±10
- Escalation: +3 is significant, +5 is major
- SkillXp: training 15-25, exceptional 40-60
- Order events MUST grant XP
- Risk/reward ratio appropriate for tier
Refer to Tools/AGENT-WORKFLOW.md Balance Agent Checklist
```

---

### Multi-Agent Workflow (Using Profiles)

**Parallel analysis workflow:**
1. Open Tab 1 → Select `Analyst` profile → `[ANALYZE:CODE] <your task>`
2. Open Tab 2 → Select `Narrative Voice` profile → `Review the text in <file>`
3. Open Tab 3 → Select `Balance` profile → `Check effects in <file>`
4. Monitor all three in Agent Management Panel (top-right)
5. Collect findings from each tab
6. Open Tab 4 → Select `Implementer` profile → Paste findings + `[IMPLEMENT]`
7. Open Tab 5 → Select `QA` profile → `[VALIDATE]`

**Model recommendations:**
| Task Type | Recommended Model | Why |
|-----------|-------------------|-----|
| Analysis (general) | `auto (cost-efficient)` | Reading/reporting doesn't need heavy model |
| Narrative Voice | `claude-4-opus` | Nuanced prose evaluation needs best model |
| Implementation | `claude-4-opus` | Complex code generation benefits from best |
| Balance | `auto (cost-efficient)` | Numeric checks are straightforward |
| QA | `auto (cost-efficient)` | Running scripts, reporting results |

---

## Specialist Checklists

### C# Analyst Checklist
When examining code issues:
- [ ] Identify which `src/Features/` subsystem is involved
- [ ] Check if relevant manager/behavior exists
- [ ] Verify Bannerlord API usage against `C:\Dev\Enlisted\Decompile\`
- [ ] Look for existing Harmony patches that might conflict
- [ ] Check for DLC-specific code paths (Naval/WarSails)

### Content Analyst Checklist
When examining content issues:
- [ ] Identify content type (event, decision, order, order_event)
- [ ] Check tier and role requirements make sense
- [ ] Verify localization string references exist
- [ ] Check option count (must be 0 or 2-6, never 1)
- [ ] Verify tooltips present and <100 chars

### Crash Analyst Checklist
When examining crashes:
- [ ] Find latest `Session-A_*.log` in `Modules/Enlisted/Debugging/`
- [ ] Search for error codes (`E-*`, `W-*`)
- [ ] Extract full stack trace for exceptions
- [ ] Check `Conflicts-A_*.log` for mod conflicts
- [ ] Identify if crash is during: startup, campaign load, battle, menu

### Narrative Voice Agent Checklist
When reviewing content writing:
- [ ] **Tense:** Setup = present, Results = past action → present state
- [ ] **Perspective:** Second person ("you"), never third person
- [ ] **Sentence length:** Average 8-12 words, max 20
- [ ] **No exclamation marks** in narration
- [ ] **No named emotions:** "You feel guilty" → "Your stomach turns"
- [ ] **Physical over abstract:** Show body reactions, not psychology
- [ ] **Option text:** Under 8 words, starts with verb, no hedging
- [ ] **No modern terms:** stressed, trauma, PTSD, facility, unit
- [ ] **Token usage:** `{SERGEANT}` not "the sergeant", `{LORD_NAME}` not "your lord"
- [ ] **No praise/judgment:** "Good choice!" or "That was wrong" ❌
- [ ] **Tooltips:** Mechanical only, under 80 chars, not narrative
- [ ] **Hints:** Under 10 words, camp gossip tone, uses placeholders

**Voice Quick Test:**
```
❌ "You feel stressed about the upcoming inspection and decide to prepare."
✅ "Your hands won't stop shaking. The inspection is tomorrow."

❌ "Perhaps you should consider helping the wounded soldier."
✅ "Help with the wounded."

❌ "Great job! You made the right choice and earned respect."
✅ "The soldiers nod. Something shifts in how they look at you."
```

### Balance Agent Checklist
When reviewing effects and rewards:
- [ ] **Gold:** T1-T3 max ~50 per event, T4-T6 max ~100, T7+ max ~200
- [ ] **Reputation:** Single event rarely exceeds ±10 any track
- [ ] **Escalation:** scrutiny/discipline +3 is significant, +5 is major
- [ ] **SkillXp:** Training baseline ~15-25, exceptional ~40-60
- [ ] **Order events MUST grant XP** — validator will flag if missing
- [ ] **Fatigue:** Most actions cost 1-3, heavy labor 4-5
- [ ] **HP damage:** Minor 5-10%, moderate 15-25%, serious 30%+
- [ ] **Risk/reward ratio:** Higher risk = higher reward (proportional)
- [ ] **Tier gating:** T1 shouldn't access T5+ rewards
- [ ] **Tooltip accuracy:** Listed effects match actual effects object

**Balance Reference Values:**
```
Promotion requirements (soldier rep):
  T2: 5, T3: 10, T4: 20, T5: 30, T6: 40, T7: 50

Escalation danger zones:
  Scrutiny 7+ = officer attention
  Discipline 8+ = discharge risk
  Medical Risk 4+ = illness onset

Daily wages by tier:
  T1: 5, T2: 8, T3: 12, T4: 18, T5: 25, T6: 35, T7+: 50+
```

**Balance Quick Test:**
```
❌ T2 event gives 200 gold and +15 all reputations
✅ T2 event gives 25 gold and +5 soldier rep

❌ "Safe" option grants scrutiny +5
✅ "Risky" option grants scrutiny +3 on failure

❌ Order event option has no skillXp in effects
✅ Order event option: effects.skillXp: { "Tactics": 15 }
```

---

## C# Implementer Rules

### Adding New Files
```xml
<!-- ALWAYS add to Enlisted.csproj -->
<Compile Include="src\Features\MyFeature\MyNewClass.cs"/>
```

### TextObject Localization
```csharp
// CORRECT - uses localized string
new TextObject("{=my_string_id}Fallback text here")

// Add to enlisted_strings.xml:
<string id="my_string_id" text="Localized text here" />
```

### Sea Context Guards (Phase 8 Pattern)
```csharp
// REQUIRED when using IsCurrentlyAtSea for content filtering:
if (party.CurrentSettlement == null && 
    party.BesiegedSettlement == null && 
    party.IsCurrentlyAtSea)
{
    // Safe to use sea-specific logic
}
```

---

## Content Author Rules

### JSON Field Ordering (Critical)
Fallback fields MUST immediately follow their ID fields:
```json
{
  "titleId": "evt_gambling_title",
  "title": "A Game of Chance",        // ← immediately after titleId
  "setupId": "evt_gambling_setup",
  "setup": "Soldiers gather...",      // ← immediately after setupId
  "options": [
    {
      "textId": "evt_gambling_opt1_text",
      "text": "Join the game",        // ← immediately after textId
      "tooltip": "Risk gold for potential profit"  // REQUIRED, <100 chars
    }
  ]
}
```

### Option Count Rules
| Count | Valid? | Use Case |
|-------|--------|----------|
| 0 | ✅ | Dynamically generated options |
| 1 | ❌ | **NEVER** — validator error |
| 2-4 | ✅ | Standard choices |
| 5-6 | ⚠️ | Only for onboarding/abort events |
| 7+ | ❌ | Too many — validator error |

### Order Events MUST Grant XP
```json
{
  "options": [
    {
      "id": "complete",
      "effects": {
        "skillXp": { "Tactics": 15 }  // REQUIRED for order events
      }
    }
  ]
}
```

---

## Patch Specialist Rules

### Harmony Patch Safety
```csharp
// ALWAYS use try-catch in patches
[HarmonyPrefix]
static bool MyPatch(...)
{
    try
    {
        // patch logic
    }
    catch (Exception ex)
    {
        ModLogger.Error("E-PATCH-001", $"MyPatch failed: {ex.Message}", ex);
        return true; // Let original method run on failure
    }
}
```

### DLC Feature Gates
```csharp
// Check before using Naval/WarSails features
if (!ModSettings.Instance.HasNavalDLC)
{
    ModLogger.Info("W-DLC-001", "Naval DLC not detected, skipping sea features");
    return;
}
```

---

## QA Validation Checklist

Run before ANY commit:

```powershell
# 1. Validate all content
python Tools/Validation/validate_content.py

# 2. Build the mod
dotnet build -c "Enlisted RETAIL" /p:Platform=x64

# 3. Check localization sync
python Tools/Validation/sync_event_strings.py --check

# 4. Review warnings (acceptable baseline: ~299)
# See: Tools/Validation/VALIDATION_BASELINE.md
```

### Validation Phase Coverage

| Validator Phase | What It Catches |
|-----------------|-----------------|
| 1-4 | JSON structure, references, logic, consistency |
| 5 | Orphaned XML strings |
| 5.5 | Opportunity hints, deprecated fields |
| 6 | Config file errors |
| 7 | .csproj completeness |
| 8 | Code quality (sea context) |
| 9 | C# TextObject → XML refs |
| 9.5 | Camp schedule descriptions |

---

## Example Workflows

### Example 1: Add New Camp Decision

**You:** "Add a camp decision for soldiers to gamble"

**Warp (Triage):** This is a CONTENT task → Content Analyst + Content Author

**Warp (Analysis):**
- Decisions live in `ModuleData/Enlisted/Decisions/decisions.json`
- Need: id, category, tier requirements, options with tooltips
- Similar: `dec_dice_game`, `dec_card_game` for patterns

**Warp (Implementation):**
- Creates JSON with correct field ordering
- Adds 3 options (join/watch/decline)
- Each option has tooltip <100 chars
- Adds effects (gold change, reputation, fatigue)

**Warp (QA):**
- Runs validator → passes
- Runs sync check → 2 new strings needed
- Syncs XML strings
- Reports complete

---

### Example 2: Fix Crash on Settlement Entry

**You:** "Game crashes when entering town while enlisted"

**Warp (Triage):** This is a CRASH task → Crash Analyst + Patch Specialist

**Warp (Crash Analysis):**
- Checks `Session-A_*.log` for error codes
- Finds `E-ENCOUNTER-042: NullReferenceException in TownMenuBehavior`
- Stack trace points to `CurrentSettlement.Town` access
- Root cause: Code assumes settlement exists but encounter guard failed

**Warp (Implementation):**
- Adds null check: `if (settlement?.Town == null) return;`
- Updates Harmony patch with try-catch wrapper
- Adds error logging with code `E-TOWN-001`

**Warp (QA):**
- Builds successfully
- No new warnings
- Reports fix complete

---

### Example 3: Why Doesn't My Event Fire?

**You:** "[ANALYZE] Event evt_veteran_story never fires"

**Warp (Content Analysis):**
- Finds event in `events_camp.json`
- Requirements: `tier.min: 5`, `role: "NCO"`, `context: "Camp"`
- Triggers: `requires_flag: "has_veteran_in_unit"`
- Problem: Flag `has_veteran_in_unit` is never set by any other event

**Warp (Report):**
- Event requires flag that doesn't exist
- Options: 
  A) Add flag-setting event earlier in progression
  B) Remove flag requirement
  C) Change to different trigger condition

---

## Invoking Specific Agents

You can force a specific perspective:

| Command | Effect |
|---------|--------|
| `[ANALYZE]` | Skip to analysis phase, read-only |
| `[ANALYZE:CODE]` | Force C# Analyst perspective |
| `[ANALYZE:CONTENT]` | Force Content Analyst perspective |
| `[ANALYZE:CRASH]` | Force Crash Analyst perspective |
| `[ANALYZE:VOICE]` | Force Narrative Voice Agent perspective |
| `[ANALYZE:BALANCE]` | Force Balance Agent perspective |
| `[IMPLEMENT]` | Skip analysis, go straight to implementation |
| `[VALIDATE]` | Run QA validation only |
| `[FULL]` | Run complete pipeline with all phases |

---

## Key Reference Files

| What | Where |
|------|-------|
| Writing style | `docs/Features/Content/writing-style-guide.md` |
| JSON schemas | `docs/Features/Content/event-system-schemas.md` |
| Validation baseline | `Tools/Validation/VALIDATION_BASELINE.md` |
| Error codes | `README.md` → Logs + Troubleshooting section |
| API reference | `C:\Dev\Enlisted\Decompile\` (local) |
| Project blueprint | `docs/BLUEPRINT.md` |

---

## Tips for Best Results

1. **Be specific** — "Add gambling decision" beats "add some content"
2. **Include context** — "crashes when supply is zero" helps crash analysis
3. **Share logs** — Paste error codes or stack traces directly
4. **Use [ANALYZE]** — When you want investigation without changes
5. **Trust validation** — If validator passes, changes are structurally sound
6. **Iterate** — Complex features may need multiple passes

---

## Troubleshooting the Workflow

**"Warp made changes but validator fails"**
→ Check the specific error. Field ordering and tooltip issues are common.

**"Analysis seems incomplete"**
→ Use `[ANALYZE:CODE]` or `[ANALYZE:CONTENT]` to force deeper investigation.

**"I want to understand before any changes"**
→ Start with `[ANALYZE]` — this prevents any modifications.

**"Changes look wrong"**
→ Reject the edit. Warp won't force changes you don't approve.

---

## CrewAI Integration

For complex multi-agent workflows, this project has a **CrewAI integration** at `Tools/CrewAI/`.

### When to Use CrewAI vs Warp Directly

| Task | Use |
|------|-----|
| Quick fixes, single-file changes | Warp directly |
| Analysis questions | Warp directly |
| New content (1-3 events) | Warp directly |
| Bug investigation with error codes | CrewAI `bug_hunting_crew` |
| Complex multi-file features | CrewAI `full_feature_crew` |
| Planning docs for ANEWFEATURE/ | CrewAI `planning_crew` |
| Pre-release validation | CrewAI `validation_crew` |

### How to Invoke CrewAI

**Tell Warp what you want**, and Warp will write and run a CrewAI kickoff script:

```
User: "Use CrewAI to create a planning doc for consolidating the systems integration"

Warp creates and runs:
  from enlisted_crew import EnlistedCrew
  EnlistedCrew().planning_crew().kickoff(inputs={
      "feature_name": "Systems Integration Consolidation",
      "description": "...",
      "related_systems": "Supply, Morale, Reputation, Escalation"
  })
```

### Available CrewAI Crews

| Crew | Purpose | Agents |
|------|---------|--------|
| `bug_hunting_crew()` | Investigate crashes/bugs, trace to root cause, fix | code_analyst → systems_analyst → csharp_implementer → qa_agent |
| `planning_crew()` | Design docs only (ANEWFEATURE/) - NO code | systems_analyst → feature_architect → documentation_maintainer |
| `feature_design_crew()` | Design spec without implementation | systems_analyst → code_analyst → feature_architect → balance_analyst |
| `full_feature_crew()` | End-to-end: design → implement → docs | All 9 agents |
| `validation_crew()` | Pre-commit checks | content_analyst → qa_agent |
| `content_creation_crew()` | New JSON content | content_author → content_analyst → balance_analyst |
| `code_review_crew()` | C# code review | code_analyst → qa_agent |
| `documentation_crew()` | Sync docs after implementation | documentation_maintainer |

### CrewAI Model Tiers

| Agent | Model | Purpose |
|-------|-------|--------|
| systems_analyst, feature_architect | Opus 4.5 + 10k thinking | Complex architecture |
| code_analyst, documentation_maintainer | Sonnet 4.5 + 5k thinking | Analysis requiring reasoning |
| qa_agent | Sonnet 4.5 + 3k thinking | Final validation (never skimp) |
| csharp_implementer | Sonnet 4.5 (no thinking) | Execute from specs |
| content_analyst, content_author, balance_analyst | Haiku 4.5 | Fast validation/generation |

### Planning vs Implementation

**IMPORTANT:** CrewAI knows the difference:

- `planning_crew()` → Creates doc in `ANEWFEATURE/` with Status: 📋 Planning
  - Does NOT update INDEX.md, AI context, or validators
  
- `full_feature_crew()` → Implements code AND updates all docs
  - Updates INDEX.md, AI context (CrewAI, Warp), validators

### Example Prompts for CrewAI

```
"Use CrewAI bug_hunting_crew to investigate E-ENCOUNTER-042 crash"
→ Searches logs, traces root cause, proposes fix, validates it compiles

"Use CrewAI planning_crew to design the skill integration feature"
→ Creates planning doc, no code changes

"Use CrewAI full_feature_crew to implement the approved skill integration spec"
→ Implements C#, JSON, updates all docs

"Use CrewAI validation_crew to check all content before release"
→ Runs comprehensive validation

"Use CrewAI content_creation_crew to add 5 new camp opportunities"
→ Creates JSON content with style/schema compliance
```

### CrewAI Setup

See `Tools/CrewAI/README.md` for setup instructions. Requires:
- Python environment with `pip install -e .`
- `.env` file with `ANTHROPIC_API_KEY`
