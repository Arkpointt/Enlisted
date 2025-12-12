# External AI Brainstorm Prompt (Enlisted — Lance Life / Camp Events)

Copy/paste this entire prompt into your external writing AI when you want it to **brainstorm new Lance Life stories**.

## Prompt (copy/paste)

You are brainstorming **camp events** for a Mount & Blade II: Bannerlord mod called **Enlisted**. These are "Viking Conquest style" popups: a short situation, 2–4 choices, and immediate consequences.

### Player fantasy
The player is an enlisted soldier navigating camp life — drills, shortages, petty corruption, morale problems. Every choice should feel like a small gamble or opportunity that builds the character mechanically.

### Hard constraints
- No code, no implementation details.
- No new scenes, NPCs, quests, or items.
- At least one safe option per event.
- Keep it grounded: camp-scale problems, not kingdom politics.

### Event template (use this for each event)

1) **Title** (2–4 words)
2) **Setup** (2–4 sentences — just frame the choice)
3) **Tier gate** (minimum enlisted tier 1–6; provisional or final lance)
4) **Trigger** (after battle / long march / low morale / supply shortage / pay late / etc.)
5) **Options (2–4)** — each with:
   - Option text (what the player clicks)
   - Risk level (safe / risky / corrupt)
   - Cost (fatigue, gold, reputation, heat)
   - Reward (XP, gold, fatigue relief, reputation, temporary edge)
   - Skills trained (1–2 from: Athletics, Riding, Polearm, OneHanded, TwoHanded, Bow, Crossbow, Throwing, Scouting, Steward, Medicine, Engineering, Trade, Charm, Leadership, Roguery)
6) **Escalation hook** (1 line — what happens if the player keeps picking risky/corrupt options)

### Reward/cost guidelines
- Gold: 25–200
- Fatigue: 1–4
- Skill XP: 10–50 per skill, 1–2 skills per option
- Heat/discipline: small internal accumulation, not instant punishment

### Generate now
Create **5 events** with this mix:
- 2× drills/training (combat or athletic skills)
- 1× logistics/scrounging (steward/scouting/engineering)
- 1× morale/revelry (charm/roguery/leadership)
- 1× quartermaster/corruption (roguery/charm/trade)

Spread across tiers 2–5. At least one event must force a choice between **discipline and looking the other way**.

Return the 5 events using the template above.

## Notes for the human (you)
- If the external AI suggests extra mechanics (e.g., “heat”, “IOUs”, “stockouts”), that’s fine—bring it back here and we’ll implement the mechanics and wire them into Enlisted properly.
- If you want lance-specific stories tied to a particular lance style/name, ask the AI to mention it in the **Setup** and/or **Trigger vibe**; implementation will be handled later.


