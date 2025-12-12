# Phase 4 Checklist — Corruption / Heat / Discipline

This checklist is the **Phase 4 acceptance gate** for the Enlisted “corruption” layer. It is derived from the research bundle under `docs/research/` and is intended to ensure we implement the full loop (temptation → meters → escalation → consequences → relief valves) without scope creep or save-risk.

## Scope (what Phase 4 must cover)
- **Heat (contraband/corruption attention)**: rises from corrupt choices; triggers scrutiny and shakedowns.
- **Discipline risk**: rises from shirking/insubordination/being caught; triggers duty punishment and temporary progression blocks.
- **Honor / Lance Reputation**: a lightweight internal meter that gates some options and modifies outcomes.

## Non-negotiables (before anything else)
- [ ] **Feature flags**: corruption systems can be disabled safely in config (no lingering state effects when disabled).
- [ ] **Safety gating**: no popups during battle/encounter/captivity or unstable menu transitions.
- [ ] **Rate limiting**: category cooldowns + global caps prevent “event spam.”
- [ ] **Always a safe option**: each event has at least one safe choice with bounded consequences.

## Inputs (what can increase meters)
### Heat inputs
- [ ] **Direct corruption choices**: bribery, theft, skimming, “looking the other way” on contraband.
- [ ] **Small social contraband**: “accept a drink”, “contraband run / sneak out” adds heat in small steps (not only big events).
- [ ] **Quartermaster interaction**: “clerk approaches with bribe”, “ledger game”, “unguarded goods” (player-initiated) are supported patterns.

### Discipline inputs
- [ ] **Shirking / misconduct**: oversleep, skip the line, sleeping on watch, disobedience.
- [ ] **Caught outcomes**: risky actions can fail and add discipline risk (caught sleeping, caught stealing, etc.).
- [ ] **Failed duty performance**: poor duty event outcomes can add discipline risk (e.g., false report).

## Escalation thresholds (must be explicit and predictable)
### Heat escalation (Low / Medium / High)
- [ ] **Low heat**: “watched closely” feedback (text cue / subtle consequence).
- [ ] **Medium heat**: **periodic shakedown events** (search/pressure) with survivable outcomes.
- [ ] **High heat**: **confiscation and/or targeted audit** events that are still survivable and do not hard-fail the campaign.

### Discipline escalation (Low / Medium / High)
- [ ] **Low discipline**: worse duties / minor penalties.
- [ ] **Medium discipline**: extra duty fatigue penalties / increased scrutiny.
- [ ] **High discipline**: **formal discipline hearing** event(s) and/or **duty removal/reassignment**.
- [ ] **Temporary promotion blocks**: allowed at very high discipline, but must have a clear recovery path (cooldown + “clean service”).

## Consequence event types (must exist in the content plan)
- [ ] **Shakedown** (heat-driven): search, intimidation, minor confiscation, or warning.
- [ ] **Audit / snitch** (heat-driven): someone reports you, clerk “remembers your name,” targeted scrutiny.
- [ ] **Loot discipline** (operation-driven): post-raid or post-siege-assault moral choices.
- [ ] **Discipline hearing** (discipline-driven): judgment, penalties, and a way to get back to normal.

## Quartermaster corruption loop (must be implemented as internal-only)
- [ ] Quartermaster mood/price consequences remain **internal to Enlisted** (no global economy rewrite).
- [ ] At least one corruption offer type exists and is tier-gated appropriately:
  - [ ] “Ledger skim” / “fix the books”
  - [ ] “Pay to ‘fix’ your back pay” (PayTension-driven temptation)
  - [ ] “Unguarded goods” (player-initiated opportunity)

## Pay/IOU interactions (camp-scale, no economy rewrite)
- [ ] **PayTension can amplify temptations** (bribes, IOU pressure).
- [ ] **Debt/IOU beats** (e.g., gambling IOU) can exist as social pressure and may touch discipline risk.
- [ ] Any “IOU” mechanism remains inside **Enlisted’s ledger** (no global gold generation loops).

## Relief valves (must exist so players can recover)
- [ ] **Clean choices reduce risk over time** (heat/discipline decay or “cooldown clearing”).
- [ ] **Opt-out path**: players can stop engaging in corrupt content without being trapped in a punishment spiral.
- [ ] **Relationship mitigation (limited)**: lance relationships can sometimes warn/cover for you, reducing heat/discipline impact in bounded ways (not immunity).

## QA / acceptance tests (practical checks)
- [ ] **No spam**: in a 14-day term, corruption events do not exceed configured weekly caps.
- [ ] **Heat progression**: intentionally picking corrupt choices 3–5 times triggers a medium heat consequence (shakedown) with clear feedback.
- [ ] **High heat**: continuing corrupt play triggers an audit/confiscation consequence, but player remains playable afterward.
- [ ] **Discipline progression**: repeated misconduct triggers a hearing/duty penalty; player can recover with clean service.
- [ ] **Promotion block**: if implemented, it is temporary, explained, and cleared by a defined recovery condition.
- [ ] **Internal-only**: disabling the feature flag stops new corruption events and suppresses corruption-based modifiers.

## References (where this checklist comes from)
- `docs/research/lance_life_events_master_doc_v2.md` (heat/discipline thresholds, corrupt option patterns)
- `docs/research/lance_life_ui_integration.md` (corruption/discpline event mappings, loot discipline, hearings)
- `docs/research/time_and_ai_state_system.md` (discipline/heat inputs from time-based events, safety gating)
- `docs/research/menu_system_update.md` (heat gained from small social actions; “camp followers” mention)
- `docs/research/lance_career_system.md` (relationship mitigation: warn/cover; discipline responsibilities)

