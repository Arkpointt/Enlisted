# Escalation Threshold Events — Story Pack

This document contains all story events triggered by escalation thresholds for **Tier 1-6 (Enlisted and Officer)** players. T7-9 Commander escalation events are not yet implemented.

---

## Table of Contents

1. [Overview](#overview)
2. [Heat Track Events](#heat-track-events)
3. [Discipline Track Events](#discipline-track-events)
4. [Lance Reputation Track Events](#lance-reputation-track-events)
5. [Medical Risk Track Events](#medical-risk-track-events)
6. [Adding New Escalation Events](#adding-new-escalation-events)

---

## Overview

### Delivery Metadata — All Escalation Events

| Field | Value |
|-------|-------|
| **Delivery** | Automatic |
| **Menu** | None |
| **Triggered By** | Track reaches threshold value |
| **Presentation** | Inquiry Popup |
| **Priority** | High (fires before regular events) |
| **Rate Limit** | Max 1 threshold event per day |
| **Cooldown** | 7 days per specific event |

**Implementation:** After any event modifies an escalation track, `ThresholdEventManager.CheckThresholds()` runs. If a track meets or exceeds a threshold and the event isn't on cooldown, it queues. Events fire at next safe moment as inquiry popups.

### Track Summary

| Track | Range | Thresholds | Events |
|-------|-------|------------|--------|
| Heat | 0–10 | 3, 5, 7, 10 | 4 |
| Discipline | 0–10 | 2, 3, 5, 7, 10 | 5 |
| Lance Reputation | −50 to +50 | −40, −20, +20, +40 | 4 |
| Medical Risk | 0–5 | 3, 4, 5 | 3 |

### Event ID Convention

```
{track}_{threshold_name}

Examples:
- heat_warning
- discipline_hearing
- lance_trusted
- medical_emergency
```

---

## Heat Track Events

> **Track:** Heat (Corruption Attention)
> **Range:** 0–10
> **Triggers:** Skimming supplies, bribes, contraband, falsifying records
> **Decay:** −1 per 7 days with no corrupt choices

### HEAT-01: The Warning

**Event ID:** `heat_warning`
**Track:** Heat
**Threshold:** 3 (Watched)
**Cooldown:** 7 days

#### Metadata

```json
{
  "id": "heat_warning",
  "category": "escalation",
  "track": "heat",
  "threshold": 3,
  "threshold_name": "watched",
  "delivery": "automatic",
  "cooldown_days": 7,
  "priority": "high"
}
```

#### Setup

{LANCE_LEADER_SHORT} pulls you aside after evening meal. Their voice is low, eyes checking that no one's close enough to hear.

"I'm going to say this once, {PLAYER_NAME}. Whatever you're doing — the extra coins, the supply discrepancies, the 'gifts' from merchants — people are starting to notice."

They lean closer.

"I don't care what you do on your own time. But if it comes back on {LANCE_NAME}, we're going to have a problem. Understand?"

#### Options

| Option | Text | Risk | Outcome |
|--------|------|------|---------|
| deny | "I don't know what you're talking about, {LANCE_LEADER_RANK}." | Safe | {LANCE_LEADER_SHORT} stares at you for a long moment. "Right. You don't." They walk away. The warning's been given. What you do with it is your business. |
| acknowledge | "I hear you. I'll be more careful." | Safe | A curt nod. "See that you are." {LANCE_LEADER_SHORT} leaves. You've been warned — by someone who could've reported you instead. That's worth something. |
| deflect | "Everyone's doing it. I'm just not as subtle." | Risky | "Everyone's NOT doing it," {LANCE_LEADER_SHORT} snaps. "And the ones who are? They're better at it than you." The words sting because they're true. |
| ask_help | "I'm in deeper than I meant to be. Any advice?" | Safe | {LANCE_LEADER_SHORT} sighs. "Keep your head down for a while. Volunteer for the shit jobs. Be visible doing honest work." They pause. "And stop. Whatever it is, just stop." |

#### Effects

| Option | Effects |
|--------|---------|
| deny | None — warning delivered, no change |
| acknowledge | −1 Heat (showed you're listening) |
| deflect | +1 Heat (attitude noticed) |
| ask_help | −2 Heat, +5 Lance Leader relation |

---

### HEAT-02: The Shakedown

**Event ID:** `heat_shakedown`
**Track:** Heat
**Threshold:** 5 (Shakedown)
**Cooldown:** 7 days

#### Metadata

```json
{
  "id": "heat_shakedown",
  "category": "escalation",
  "track": "heat",
  "threshold": 5,
  "threshold_name": "shakedown",
  "delivery": "automatic",
  "cooldown_days": 7,
  "priority": "high"
}
```

#### Setup

"Kit inspection! Everyone out, gear on the ground, NOW!"

{SERGEANT_NAME} — not your lance's sergeant, one of the captain's dogs — is tearing through the camp with two soldiers. Random inspection, they're calling it. But the way they looked at you when they announced it... this isn't random.

Your kit is laid out. There's nothing obviously wrong with it. But if they look closely at the coin pouch, or check the false bottom in your pack...

#### Options

| Option | Text | Risk | Outcome |
|--------|------|------|---------|
| comply | Stand at attention, let them search. Nothing to hide. | Risky (50%) | **Success:** They search. Find nothing incriminating — you got lucky, or they weren't looking hard enough. {SERGEANT_NAME} grunts, moves on. **Failure:** {SERGEANT_NAME}'s hand closes on the coin pouch. "What's this then?" The extra weight gives you away. "Skimming, are we?" |
| bribe | Quietly offer {SERGEANT_NAME} a cut to look the other way. | Risky (40%) | **Success:** A slight nod. The inspection of your kit is perfunctory at best. {SERGEANT_NAME} moves on. You've bought time — and a new problem. **Failure:** "Are you trying to BRIBE me, soldier?" {SERGEANT_NAME}'s voice carries. Everyone's looking now. This just got much worse. |
| distraction | "Sergeant! {SOLDIER_NAME}'s kit — I saw them hiding something earlier!" | Corrupt | {SERGEANT_NAME}'s attention shifts. They descend on {SOLDIER_NAME}'s kit like wolves. You've bought yourself time by throwing someone else to the dogs. {SOLDIER_NAME} will figure out who talked. Eventually. |
| dump | While attention's elsewhere, quietly dump the evidence. | Risky (60%) | **Success:** The incriminating items vanish into the latrine trench. When they search your kit, there's nothing to find. Clean — for now. **Failure:** {VETERAN_1_NAME} sees you. Their eyes meet yours. They don't say anything. But they saw. |

#### Effects

| Option | Effects |
|--------|---------|
| comply (success) | −1 Heat |
| comply (failure) | +2 Heat, +2 Discipline, lose contraband/extra coin |
| bribe (success) | −2 Heat, +1 Heat (new leverage against you) |
| bribe (failure) | +3 Heat, +3 Discipline |
| distraction | −2 Heat, −10 Lance Rep, −15 relation {SOLDIER_NAME} |
| dump (success) | −3 Heat, lose contraband/extra coin |
| dump (failure) | −1 Heat, {VETERAN_1_NAME} knows (future leverage) |

---

### HEAT-03: The Audit

**Event ID:** `heat_audit`
**Track:** Heat
**Threshold:** 7 (Audit)
**Cooldown:** 7 days

#### Metadata

```json
{
  "id": "heat_audit",
  "category": "escalation",
  "track": "heat",
  "threshold": 7,
  "threshold_name": "audit",
  "delivery": "automatic",
  "cooldown_days": 7,
  "priority": "high"
}
```

#### Setup

The captain's clerk finds you after morning muster. His smile doesn't reach his eyes.

"The captain's ordered a review of supply records. Your name came up several times — requisitions, distributions, inventory counts." He produces a ledger. "Some discrepancies need explaining."

This is it. The numbers don't add up because you made sure they wouldn't. The question is whether you can talk your way out, or if the noose is already tightening.

"The captain will see you at midday. I suggest you have answers ready."

#### Options

| Option | Text | Risk | Outcome |
|--------|------|------|---------|
| honest | Come clean. Admit what you did, throw yourself on mercy. | Safe | The captain listens in cold silence. When you finish: "At least you're not a liar on top of everything else." Punishment is coming, but it won't be the worst. They respect honesty, barely. |
| excuse | Blame clerical errors. The records were a mess before you touched them. | Risky (40%) | **Success:** The captain frowns, reviews the ledger. "This is a shambles. Someone needs to sort this properly." You've dodged — for now. **Failure:** "Funny how the errors all favor you," the captain observes. "I wasn't born yesterday, soldier." |
| implicate | There's a larger scheme. You were just following orders from above. | Corrupt | The captain's eyes narrow. "Names." You give them — true or not, it doesn't matter. The investigation widens. You're a witness now, not a suspect. But you've made powerful enemies. |
| records | You've been keeping your own records. Produce them — doctored to tell a better story. | Risky (50%) | **Success:** The captain compares ledgers. Your version is cleaner, more plausible. "This contradicts the official count." They choose to believe yours. **Failure:** "These don't match anything." The captain's voice is flat. "You've made this worse, soldier. Much worse." |

#### Effects

| Option | Effects |
|--------|---------|
| honest | −3 Heat, +3 Discipline, +5 Lord relation (honesty valued) |
| excuse (success) | −2 Heat |
| excuse (failure) | +2 Heat, +2 Discipline |
| implicate | −5 Heat, +2 Discipline, creates enemy (named party), −20 faction reputation |
| records (success) | −4 Heat, +20 Roguery XP |
| records (failure) | +3 Heat, +4 Discipline |

---

### HEAT-04: Exposed

**Event ID:** `heat_exposed`
**Track:** Heat
**Threshold:** 10 (Exposed)
**Cooldown:** 14 days

#### Metadata

```json
{
  "id": "heat_exposed",
  "category": "escalation",
  "track": "heat",
  "threshold": 10,
  "threshold_name": "exposed",
  "delivery": "automatic",
  "cooldown_days": 14,
  "priority": "critical"
}
```

#### Setup

Guards. Four of them, armed, surrounding your tent at dawn.

"On your feet, soldier. Captain's orders. You're under arrest for theft of army supplies, falsification of records, and conduct unbecoming."

No warnings this time. No audits. They've built their case and now they're collecting.

{LANCE_LEADER_SHORT} watches from a distance. Their expression is unreadable. They can't help you. Maybe they wouldn't if they could.

"Come quietly, or don't. Makes no difference to us."

#### Options

| Option | Text | Risk | Outcome |
|--------|------|------|---------|
| surrender | Go quietly. Face the charges. | Safe | You're escorted to the stockade. A hearing will follow. The evidence is damning, but you'll have a chance to speak. The army has procedures. Cold comfort, but it's something. |
| beg | "Please — I'll return everything, I'll work it off, anything—" | Risky (30%) | **Success:** One guard exchanges glances with another. "Double shifts. Three months. Every coin returned, with interest. One more incident and we won't be talking." A lifeline, barely. **Failure:** "Should've thought of that before." They're not interested in deals. The stockade awaits. |
| fight | Not like this. Make a break for it. | Risky (20%) | **Success:** You burst through — luck, desperation, they weren't expecting it. You're running now. A deserter and a thief. But you're free. **Failure:** They're ready for it. A club to the back of the head. You wake in chains, charges now including resisting arrest. |
| leverage | "I know things. About supply chains. About officers. Things the captain might want to know." | Risky (40%) | **Success:** A pause. Whispered consultation. "The captain will hear you. But this better be good." You've bought an audience, maybe your freedom — at the cost of becoming an informant. **Failure:** "Save it for the hearing." They don't care what you know. Into the stockade you go. |

#### Effects

| Option | Effects |
|--------|---------|
| surrender | Reset Heat to 0, +5 Discipline, lose all contraband/excess gold, confined until hearing |
| beg (success) | Reset Heat to 0, +3 Discipline, lose all contraband/excess gold, owe debt |
| beg (failure) | Reset Heat to 0, +5 Discipline, lose all contraband/excess gold, confined |
| fight (success) | Desertion flag, −100 faction reputation, player leaves army |
| fight (failure) | Reset Heat to 0, +7 Discipline, confined, injury (moderate) |
| leverage (success) | Reset Heat to 0, +2 Discipline, informant flag, −30 Lance Rep |
| leverage (failure) | Reset Heat to 0, +5 Discipline, confined |

---

## Discipline Track Events

> **Track:** Discipline
> **Range:** 0–10
> **Triggers:** Rule-breaking, insubordination, duty failures, fighting
> **Decay:** −1 per 14 days with no infractions

### DISC-01: On Notice

**Event ID:** `discipline_warning`
**Track:** Discipline
**Threshold:** 2 (On Notice)
**Cooldown:** 7 days

#### Metadata

```json
{
  "id": "discipline_warning",
  "category": "escalation",
  "track": "discipline",
  "threshold": 2,
  "threshold_name": "on_notice",
  "delivery": "automatic",
  "cooldown_days": 7,
  "priority": "high"
}
```

#### Setup

{LANCE_LEADER_SHORT} doesn't look happy.

"You're on notice, {PLAYER_NAME}. The sergeant's been keeping track — late to muster, sloppy kit, that business with the—" they wave a hand. "Doesn't matter. Point is, you're building a reputation, and not the good kind."

They lower their voice.

"I can only cover for you so many times. Get it together."

#### Options

| Option | Text | Risk | Outcome |
|--------|------|------|---------|
| apologize | "You're right. I've been slipping. It won't happen again." | Safe | {LANCE_LEADER_SHORT} nods slowly. "See that it doesn't." The warning's been delivered. Ball's in your court now. |
| excuse | "It's not my fault — there were circumstances—" | Risky | {LANCE_LEADER_SHORT}'s expression hardens. "There are always circumstances. The question is whether you rise above them." They walk away. Not convinced. |
| resentful | "Maybe if the duties weren't so—" | Safe | "I don't want to hear it." {LANCE_LEADER_SHORT} cuts you off. "Everyone's got complaints. Not everyone makes them the camp's problem." The message is clear: shut up and soldier. |
| grateful | "Thanks for the heads up. I mean it." | Safe | A slight softening. "Just... be better. That's all I'm asking." They've stuck their neck out for you. Don't make them regret it. |

#### Effects

| Option | Effects |
|--------|---------|
| apologize | −1 Discipline, +3 Lance Leader relation |
| excuse | +1 Discipline |
| resentful | No change (warning delivered either way) |
| grateful | −1 Discipline, +5 Lance Leader relation |

---

### DISC-02: Extra Duty

**Event ID:** `discipline_extra_duty`
**Track:** Discipline
**Threshold:** 3 (Extra Duty)
**Cooldown:** 7 days

#### Metadata

```json
{
  "id": "discipline_extra_duty",
  "category": "escalation",
  "track": "discipline",
  "threshold": 3,
  "threshold_name": "extra_duty",
  "delivery": "automatic",
  "cooldown_days": 7,
  "priority": "high"
}
```

#### Setup

The duty roster goes up. Your name's on it. Again. And again. And twice more after that.

Extra duty — the army's favorite corrective measure. Latrine digging, night watch, hauling water, whatever's worst. Your infractions have been noticed, and this is the price.

{LANCE_LEADER_SHORT} shrugs when you look at them. "Out of my hands. Sergeant's orders."

A week of this, minimum.

#### Options

| Option | Text | Risk | Outcome |
|--------|------|------|---------|
| accept | Accept the punishment. Do the work. | Safe | It's brutal. Every muscle aches. But you don't complain, don't slack. By week's end, the sergeant gives you a curt nod. "Maybe you're learning after all." |
| complain | Protest the unfairness of the punishment. | Risky | "Unfair?" The sergeant's voice carries across the camp. "You want to talk about UNFAIR?" The lecture that follows is worse than the extra duty. And you've still got to do the duty. |
| excel | Not just do it — do it better than anyone. Make a point. | Safe | You throw yourself into every task. Best-dug latrines in the camp. Not a drop of water spilled. The sergeant notices. Grudging respect, maybe. At least you're not a whiner. |
| shirk | Do the minimum. What are they going to do, punish you more? | Corrupt | Turns out, yes. They can. Extended duty. Double shifts. Someone's always watching now. You've made this worse. |

#### Effects

| Option | Effects |
|--------|---------|
| accept | −2 Discipline, +2 Fatigue |
| complain | +1 Discipline, +2 Fatigue |
| excel | −3 Discipline, +3 Fatigue, +15 Athletics XP, +10 Steward XP |
| shirk | +2 Discipline, +3 Fatigue (extended punishment) |

---

### DISC-03: The Hearing

**Event ID:** `discipline_hearing`
**Track:** Discipline
**Threshold:** 5 (Hearing)
**Cooldown:** 14 days

#### Metadata

```json
{
  "id": "discipline_hearing",
  "category": "escalation",
  "track": "discipline",
  "threshold": 5,
  "threshold_name": "hearing",
  "delivery": "automatic",
  "cooldown_days": 14,
  "priority": "high"
}
```

#### Setup

A formal hearing. The captain presides, {LORD_NAME}'s banner hanging behind them. You stand before a table of officers.

"Soldier {PLAYER_NAME}. The charges: repeated failure to maintain discipline standards, disregard for orders, and conduct prejudicial to good order."

They read from a list. Every infraction, every slip-up, documented and damning.

"What do you have to say for yourself?"

#### Options

| Option | Text | Risk | Outcome |
|--------|------|------|---------|
| accountable | "No excuses, sir. I failed to meet standards. I accept whatever punishment you decide." | Safe | The captain studies you. "Accountability. That's something, at least." The punishment is firm but fair. You've shown character, if not competence. |
| explain | Lay out the context. The pressures, the circumstances, the reasons behind each incident. | Risky (50%) | **Success:** The captain listens. Nods, occasionally. "Context noted. Punishment will reflect... mitigating factors." You've bought some leniency. **Failure:** "Excuses. I hear excuses." The captain's voice is cold. "Next." |
| blame | Point fingers. Others share responsibility — maybe more than you. | Corrupt | "Interesting." The captain makes notes. "We'll be speaking to these individuals." You've deflected heat. But someone's going to know you named names. |
| challenge | "Some of these charges are exaggerated. I demand to face my accusers." | Risky (40%) | **Success:** The captain raises an eyebrow. "Bold. Very well." The confrontation that follows is ugly, but you poke holes in the case. Charges reduced. **Failure:** "You're in no position to demand anything." The captain's patience is gone. "Charges stand. Punishment enhanced for attitude." |

#### Effects

| Option | Effects |
|--------|---------|
| accountable | −2 Discipline, +5 Captain relation, +10 Lance Rep (showed spine) |
| explain (success) | −3 Discipline |
| explain (failure) | +1 Discipline |
| blame | −3 Discipline, −15 Lance Rep, create enemy (blamed party) |
| challenge (success) | −4 Discipline, +15 Lance Rep |
| challenge (failure) | +2 Discipline |

---

### DISC-04: Promotion Blocked

**Event ID:** `discipline_blocked`
**Track:** Discipline
**Threshold:** 7 (Blocked)
**Cooldown:** 14 days

#### Metadata

```json
{
  "id": "discipline_blocked",
  "category": "escalation",
  "track": "discipline",
  "threshold": 7,
  "threshold_name": "blocked",
  "delivery": "automatic",
  "cooldown_days": 14,
  "priority": "high"
}
```

#### Setup

The promotion list goes up. Your name should be on it — the experience, the time served, the battles. But it's not there.

{LANCE_LEADER_SHORT} finds you staring at the roster.

"Your record, {PLAYER_NAME}. Too many marks. The captain won't sign off on promoting someone with your... history." They actually look sympathetic. "Clean it up. Prove them wrong. Maybe next time."

Next time. If there is one.

#### Options

| Option | Text | Risk | Outcome |
|--------|------|------|---------|
| accept | "I understand. I'll earn it properly." | Safe | {LANCE_LEADER_SHORT} nods. "That's the spirit. Keep your head down, do good work. They'll notice." A setback, not a defeat. The path forward is clear, if harder. |
| bitter | "Politics. It's always politics." | Safe | "Maybe." {LANCE_LEADER_SHORT} doesn't argue. "But politics or not, you gave them ammunition. Stop doing that." Cold comfort. True, though. |
| appeal | "I want to speak to the captain. Make my case directly." | Risky (30%) | **Success:** The captain hears you out. "Convince me you've changed." You do. Or try to. "Probationary advancement. One incident, and you're back where you started." A chance. **Failure:** "The decision stands. Prove yourself through action, not words." Appeal denied. You've wasted political capital. |
| despair | "What's the point? They'll never promote someone like me." | Safe | {LANCE_LEADER_SHORT} grabs your shoulder. "None of that. You're a soldier, act like one." Harsh. But sometimes harsh is what you need. |

#### Effects

| Option | Effects |
|--------|---------|
| accept | −1 Discipline (attitude noted positively) |
| bitter | No change |
| appeal (success) | −2 Discipline, promotion unlocked (probationary) |
| appeal (failure) | +1 Discipline (pushed too hard) |
| despair | +1 Discipline (attitude problem noted) |

---

### DISC-05: Facing Discharge

**Event ID:** `discipline_discharge`
**Track:** Discipline
**Threshold:** 10 (Discharge)
**Cooldown:** 14 days

#### Metadata

```json
{
  "id": "discipline_discharge",
  "category": "escalation",
  "track": "discipline",
  "threshold": 10,
  "threshold_name": "discharge",
  "delivery": "automatic",
  "cooldown_days": 14,
  "priority": "critical"
}
```

#### Setup

"Dishonorable discharge."

The words hang in the air. The captain doesn't look pleased to say them — or maybe they do. Hard to tell.

"Your record speaks for itself. Consistent failure to meet standards. Repeated infractions despite correction. You've left us no choice."

{LANCE_NAME} watches from the edge of the assembly. {LANCE_LEADER_SHORT}'s face is stone.

"You have one opportunity to speak before the sentence is carried out. Make it count."

#### Options

| Option | Text | Risk | Outcome |
|--------|------|------|---------|
| accept | "I have nothing to say. I accept the judgment." | Safe | The captain nods. "Then it's done. Turn in your equipment. You're no longer part of this army." It's over. Not with a bang, but with paperwork. |
| beg | "Please — one more chance. I'll be different. I swear it." | Risky (25%) | **Success:** A long silence. "Against my better judgment... penal detail. Six months. Any incident and this offer disappears." A thread of hope. Thin as spider silk. **Failure:** "We've given you chances. Too many." The captain shakes their head. "Sentence stands." |
| defiant | "Discharge me then. This army never valued me anyway." | Safe | "Noted." The captain's voice is ice. "Your attitude explains much." The discharge proceeds. No pension. No reference. Nothing. |
| service | "My service record — the battles, the duties, the blood I've shed for {LORD_NAME}—" | Risky (40%) | **Success:** The captain reviews documents. Consults with officers. "Your combat record... is not nothing. Reduction in rank. Final warning. Don't make us regret this." **Failure:** "Service doesn't excuse conduct. Many have served without becoming a disciplinary nightmare." No mercy. |

#### Effects

| Option | Effects |
|--------|---------|
| accept | Discharge — player leaves army, no pension, neutral reputation |
| beg (success) | Reset Discipline to 5, penal detail flag, −20 Lance Rep |
| beg (failure) | Discharge — player leaves army, no pension, −20 reputation |
| defiant | Discharge — player leaves army, no pension, −30 reputation |
| service (success) | Reset Discipline to 3, −1 Tier (rank reduction), final warning flag |
| service (failure) | Discharge — player leaves army, no pension |

---

## Lance Reputation Track Events

> **Track:** Lance Reputation
> **Range:** −50 to +50
> **Triggers:** Helping/betraying lance mates, sharing/hoarding, combat choices
> **Decay:** Trends toward 0 (±1 per 14 days)

### LANCE-01: Trusted

**Event ID:** `lance_trusted`
**Track:** Lance Reputation
**Threshold:** +20 (Trusted)
**Cooldown:** 14 days

#### Metadata

```json
{
  "id": "lance_trusted",
  "category": "escalation",
  "track": "lance_reputation",
  "threshold": 20,
  "threshold_direction": "positive",
  "threshold_name": "trusted",
  "delivery": "automatic",
  "cooldown_days": 14,
  "priority": "high"
}
```

#### Setup

{VETERAN_1_NAME} corners you after the evening meal. They look around, making sure no one's listening.

"Got a problem, {PLAYER_NAME}. Need help from someone I can trust." They pause. "That's you."

They explain: a debt from before the army. Collector's coming. If {VETERAN_1_NAME} doesn't have the coin, things get ugly — for them, maybe for their family back home.

"I'm not asking for money. Just... watch my back when the collector shows. Make sure things stay... civil."

#### Options

| Option | Text | Risk | Outcome |
|--------|------|------|---------|
| help | "I've got your back. When and where?" | Safe | Relief floods {VETERAN_1_NAME}'s face. "Tomorrow, by the supply wagons. Just... be there." You show up. The collector sees they're not alone. The conversation stays civil. A debt renegotiated, not collected by force. |
| money | "How much do you owe? Maybe I can help directly." | Safe | {VETERAN_1_NAME} hesitates. "Twenty denars. I know it's a lot—" You hand it over. They stare at the coins like they're made of gold. "I'll pay you back. Every copper, I swear." |
| decline | "Sorry, can't get involved. Too risky." | Safe | {VETERAN_1_NAME}'s face falls, then hardens. "Right. Forget I asked." They walk away. You had reasons. Good ones, probably. But something's broken now. |
| leverage | "I'll help. But you'll owe me a favor. A real one." | Risky | A long pause. "What kind of favor?" You don't specify. They agree anyway. Desperate people do desperate things. You've got leverage now. Whether that's worth more than simple friendship... time will tell. |

#### Effects

| Option | Effects |
|--------|---------|
| help | +10 Lance Rep, +15 {VETERAN_1_NAME} relation, +15 Charm XP |
| money | +15 Lance Rep, +25 {VETERAN_1_NAME} relation, −20 gold |
| decline | −10 Lance Rep, −20 {VETERAN_1_NAME} relation |
| leverage | +5 Lance Rep, +10 {VETERAN_1_NAME} relation, gain leverage flag |

---

### LANCE-02: Bonded

**Event ID:** `lance_bonded`
**Track:** Lance Reputation
**Threshold:** +40 (Bonded)
**Cooldown:** 14 days

#### Metadata

```json
{
  "id": "lance_bonded",
  "category": "escalation",
  "track": "lance_reputation",
  "threshold": 40,
  "threshold_direction": "positive",
  "threshold_name": "bonded",
  "delivery": "automatic",
  "cooldown_days": 14,
  "priority": "high"
}
```

#### Setup

{LANCE_LEADER_SHORT} pulls you aside after weapons inspection. Their voice is barely above a whisper.

"The sergeant's planning a kit inspection tomorrow. The captain's hound — looking for contraband, extra rations, anything they can nail someone for."

They meet your eyes.

"Thought you should know. In case you've got anything that needs... relocating."

This is a warning. The kind you don't give to just anyone. The kind that could get {LANCE_LEADER_SHORT} in serious trouble if anyone found out.

You're family now. {LANCE_NAME} protects its own.

#### Options

| Option | Text | Risk | Outcome |
|--------|------|------|---------|
| grateful | "I appreciate this. More than you know." | Safe | {LANCE_LEADER_SHORT} nods. "Just don't make me regret it." You won't. This is what a lance is supposed to be — watching each other's backs. Even when the enemy wears the same uniform. |
| clean | "I'm clean. But thanks for the warning." | Safe | "Good." {LANCE_LEADER_SHORT} looks relieved. "Keep it that way." They didn't have to warn you. They did anyway. That means something. |
| share | "Anyone else in the lance need to know?" | Safe | {LANCE_LEADER_SHORT} considers. "I'll handle it. Quietly." They move off to warn the others. You're not the only one they're protecting. But you were first. That means something too. |
| warn_others | "Let me tell the others. Less risk for you." | Safe | "You sure?" You nod. Over the next hour, quiet words are exchanged. By morning, the lance is clean. The inspection finds nothing. {LANCE_LEADER_SHORT} never gets connected to the warning. |

#### Effects

| Option | Effects |
|--------|---------|
| grateful | +5 Lance Rep, +10 Lance Leader relation, −3 Heat (if Heat > 0) |
| clean | +3 Lance Rep, +5 Lance Leader relation |
| share | +10 Lance Rep (whole lance benefits) |
| warn_others | +15 Lance Rep, +15 Lance Leader relation, +20 Leadership XP |

---

### LANCE-03: Isolated

**Event ID:** `lance_isolated`
**Track:** Lance Reputation
**Threshold:** −20 (Disliked)
**Cooldown:** 14 days

#### Metadata

```json
{
  "id": "lance_isolated",
  "category": "escalation",
  "track": "lance_reputation",
  "threshold": -20,
  "threshold_direction": "negative",
  "threshold_name": "disliked",
  "delivery": "automatic",
  "cooldown_days": 14,
  "priority": "high"
}
```

#### Setup

The fire circle again. But tonight, the gaps around you are wider. Conversations die when you approach. Laughter stops.

{SOLDIER_NAME} doesn't even look up when you sit down. {VETERAN_1_NAME} finds somewhere else to be. {RECRUIT_NAME} follows them.

{LANCE_LEADER_SHORT} watches from across the fire. Measuring. Not intervening.

You're still part of {LANCE_NAME}. Technically. But the warmth is gone. You're a stranger wearing a familiar uniform.

This is what isolation feels like.

#### Options

| Option | Text | Risk | Outcome |
|--------|------|------|---------|
| approach | Try to join a conversation. Break through the ice. | Risky (40%) | **Success:** {SOLDIER_NAME} doesn't welcome you. But they don't reject you either. A few words. A shared complaint about the food. It's not much. It's something. **Failure:** Silence. They wait for you to leave. When you do, the talking resumes. You weren't part of it. You aren't now. |
| alone | Take your food and eat alone. They've made their choice. | Safe | The darkness beyond the firelight is cold. But at least it's honest. You know where you stand now. Sometimes that's worth more than false warmth. |
| confront | "Someone want to tell me what's going on?" | Risky (30%) | **Success:** {VETERAN_1_NAME} speaks up. "You know what you did." An argument follows. Ugly. But at least it's honest. Grievances aired. Not forgiven, but acknowledged. **Failure:** "Nothing's going on." Flat. Final. They're not even willing to fight with you. That's worse. |
| leader | Find {LANCE_LEADER_SHORT}. Ask what happened. | Safe | They don't sugarcoat it. "You've lost their trust. The way you handled—" they list examples. Each one a small betrayal you barely remember. "You want back in? Start making different choices." A path forward. Hard, but real. |

#### Effects

| Option | Effects |
|--------|---------|
| approach (success) | +3 Lance Rep |
| approach (failure) | −2 Lance Rep |
| alone | No change (situation acknowledged) |
| confront (success) | +5 Lance Rep, grievances cleared |
| confront (failure) | −5 Lance Rep |
| leader | +2 Lance Rep, +5 Lance Leader relation, clear guidance |

---

### LANCE-04: Sabotage

**Event ID:** `lance_sabotage`
**Track:** Lance Reputation
**Threshold:** −40 (Hostile)
**Cooldown:** 14 days

#### Metadata

```json
{
  "id": "lance_sabotage",
  "category": "escalation",
  "track": "lance_reputation",
  "threshold": -40,
  "threshold_direction": "negative",
  "threshold_name": "hostile",
  "delivery": "automatic",
  "cooldown_days": 14,
  "priority": "critical"
}
```

#### Setup

Your kit is wrong.

Nothing obvious — nothing that would fail inspection. But your straps are loosened. Your blade is dull. Your waterskin has a slow leak you didn't notice until half your water was gone.

Someone did this. Someone in {LANCE_NAME}.

{LANCE_LEADER_SHORT} isn't going to help. They're part of this, or they're looking the other way. Same result.

You're on your own. In a unit that wants you gone — or worse.

#### Options

| Option | Text | Risk | Outcome |
|--------|------|------|---------|
| endure | Fix it silently. Don't give them the satisfaction of reacting. | Safe | Hours of work restoring your kit. They wanted you angry. They wanted a confrontation they could win. You deny them both. Cold. Professional. They'll get bored eventually. Or they won't. Either way, you're still standing. |
| confront | Find out who did it. Make them answer. | Risky (40%) | **Success:** You catch {SOLDIER_NAME} smirking. You get in their face. Words are exchanged. Maybe fists. But the message is clear: you're not a victim. **Failure:** No one admits anything. No one saw anything. You're shouting at walls. You look paranoid. Dangerous. They wanted that. |
| report | Take it to the sergeant. This is beyond lance politics. | Safe | The sergeant listens. Investigates, maybe. "I'll handle it." They probably won't. But it's on record now. If something happens to you, there's a trail. |
| transfer | Request reassignment. This lance is done with you. | Safe | {LANCE_LEADER_SHORT} doesn't argue. The paperwork goes through. New lance, new people, fresh start. You're leaving enemies behind. Whether you're also leaving the problem behind... time will tell. |

#### Effects

| Option | Effects |
|--------|---------|
| endure | +2 Discipline (self-control noted), +10 Steward XP |
| confront (success) | +5 Lance Rep (showed strength), +1 Discipline |
| confront (failure) | −5 Lance Rep, +2 Discipline |
| report | +2 Discipline, investigation flag (may reduce sabotage) |
| transfer | Reset Lance Rep to 0, new lance assignment, lose all lance relations |

---

## Medical Risk Track Events

> **Track:** Medical Risk
> **Range:** 0–5
> **Triggers:** Untreated injuries/illness, training while injured
> **Decay:** Resets on treatment

### MED-01: Worsening

**Event ID:** `medical_worsening`
**Track:** Medical Risk
**Threshold:** 3 (Worsening)
**Cooldown:** 3 days

#### Metadata

```json
{
  "id": "medical_worsening",
  "category": "escalation",
  "track": "medical_risk",
  "threshold": 3,
  "threshold_name": "worsening",
  "delivery": "automatic",
  "cooldown_days": 3,
  "priority": "high"
}
```

#### Setup

It's worse today.

What started as a manageable {CONDITION_TYPE} has become something harder to ignore. The {SYMPTOM_1} is constant now. {SYMPTOM_2} makes sleep difficult.

You've been pushing through. The army doesn't stop for one soldier's discomfort. But your body is sending messages you can't keep ignoring.

{LANCE_MATE_NAME} notices you wincing. "You should see the surgeon."

Maybe they're right.

#### Options

| Option | Text | Risk | Outcome |
|--------|------|------|---------|
| surgeon | Finally give in. See the surgeon. | Safe | The surgeon takes one look and scowls. "Why didn't you come sooner?" Treatment begins. It's not pleasant, but it's working. A few days rest and you'll be functional again. Should've done this earlier. |
| push | Keep pushing. It's not that bad. | Risky (60%) | **Success:** You power through. The symptoms peak, then start to fade. Natural recovery, or maybe just stubbornness. Either way, you're still standing. **Failure:** Your body disagrees. The {SYMPTOM_1} spikes. You stagger. {LANCE_MATE_NAME} catches you. "Get the surgeon. Now." |
| rest | Take it easy. Rest without formal treatment. | Safe | You scale back. Light duties only. Sleep when you can. It's not the surgeon's tent, but it's not reckless either. Slow recovery begins. |
| herbs | Find some herbs, treat yourself. | Risky (50%) | **Success:** {REMEDY_NAME} does the trick. The symptoms ease. Not cured, but manageable. And you didn't have to explain anything to the surgeon. **Failure:** The remedy does nothing. Or makes it worse. You've wasted time you didn't have. |

#### Effects

| Option | Effects |
|--------|---------|
| surgeon | Reset Medical Risk to 0, +2 Fatigue (treatment), condition severity reduced, +20 Medicine XP (observation) |
| push (success) | −1 Medical Risk, +10 Athletics XP |
| push (failure) | +2 Medical Risk, condition severity increases, forced treatment |
| rest | −2 Medical Risk, +2 Fatigue (lost time), condition unchanged |
| herbs (success) | −2 Medical Risk, +15 Medicine XP |
| herbs (failure) | +1 Medical Risk, condition severity increases |

---

### MED-02: Complication

**Event ID:** `medical_complication`
**Track:** Medical Risk
**Threshold:** 4 (Complication)
**Cooldown:** 3 days

#### Metadata

```json
{
  "id": "medical_complication",
  "category": "escalation",
  "track": "medical_risk",
  "threshold": 4,
  "threshold_name": "complication",
  "delivery": "automatic",
  "cooldown_days": 3,
  "priority": "high"
}
```

#### Setup

Fever hits in the night.

The {CONDITION_TYPE} has become something else — something worse. {COMPLICATION_NAME}, the surgeon would probably call it. Infection, inflammation, your body turning against itself.

You wake drenched in sweat, shaking. {LANCE_MATE_NAME} is there, looking worried.

"You're burning up. Can you walk?"

Walking sounds impossible. The surgeon's tent sounds very far away.

#### Options

| Option | Text | Risk | Outcome |
|--------|------|------|---------|
| help | "Get me to the surgeon." | Safe | {LANCE_MATE_NAME} half-carries you across camp. The surgeon takes one look and starts working. Treatment is aggressive — and necessary. You're going to be out for a while. But you're going to live. |
| refuse | "I'm fine. Just need water." | Risky (30%) | **Success:** Somehow, impossibly, you ride it out. The fever breaks by morning. You're weak as a kitten, but alive. The surgeon will call it a miracle. **Failure:** You're not fine. The fever spikes. You lose consciousness. When you wake, you're in the surgeon's tent anyway — in worse shape than before. |
| medicine | "There's medicine in my kit. Help me take it." | Risky (50%) | **Success:** Whatever you had stashed works. The fever eases. Not cured, but stable. {LANCE_MATE_NAME} watches through the night. By dawn, you're coherent again. **Failure:** Wrong medicine. Or not enough. The fever climbs. You don't remember {LANCE_MATE_NAME} carrying you to the surgeon. You're told about it later. |
| lance_leader | "Get {LANCE_LEADER_SHORT}. They'll know what to do." | Safe | {LANCE_LEADER_SHORT} arrives, assesses, takes charge. "Surgeon. Now. No arguments." They organize a carry. The surgeon isn't gentle, but they're effective. {LANCE_LEADER_SHORT} checks on you twice. That means something. |

#### Effects

| Option | Effects |
|--------|---------|
| help | Reset Medical Risk to 0, bed rest (3 days), complication treated, +5 {LANCE_MATE_NAME} relation |
| refuse (success) | −2 Medical Risk, condition severity unchanged |
| refuse (failure) | +1 Medical Risk, bed rest (4 days), condition severity increases |
| medicine (success) | −2 Medical Risk, condition severity reduced |
| medicine (failure) | +1 Medical Risk, bed rest (4 days) |
| lance_leader | Reset Medical Risk to 0, bed rest (3 days), complication treated, +10 Lance Leader relation |

---

### MED-03: Emergency

**Event ID:** `medical_emergency`
**Track:** Medical Risk
**Threshold:** 5 (Emergency)
**Cooldown:** 7 days

#### Metadata

```json
{
  "id": "medical_emergency",
  "category": "escalation",
  "track": "medical_risk",
  "threshold": 5,
  "threshold_name": "emergency",
  "delivery": "automatic",
  "cooldown_days": 7,
  "priority": "critical"
}
```

#### Setup

You collapse.

One moment you're standing, the next you're on the ground. Voices around you, distant and echoey. Someone's shouting for the surgeon. Someone else is pressing something against the {CONDITION_LOCATION}.

The sky is very bright. Or very dark. Hard to tell.

{LANCE_LEADER_SHORT}'s face swims into view. Their mouth moves. The words don't reach you.

Everything goes sideways.

#### Options

*Note: This event has reduced player agency — it's an emergency. Options represent fragments of consciousness.*

| Option | Text | Risk | Outcome |
|--------|------|------|---------|
| fight | Fight to stay conscious. You need to... something. | Risky (40%) | **Success:** Sheer will keeps you present. You hear the surgeon's instructions. Feel them working. Pain means alive. You cling to the pain. **Failure:** Darkness takes you anyway. When you wake, it's over. You don't know how long you were out. |
| surrender | Let go. Trust them to fix this. | Safe | You stop fighting. The darkness is almost peaceful. When you wake, it's to the surgeon's tent, the smell of herbs, and {LANCE_MATE_NAME} dozing in a chair beside you. Still here. |
| speak | Try to tell them something. Important. | Risky (30%) | **Success:** Words come out. Maybe coherent. {LANCE_LEADER_SHORT} nods. "We know. Rest now." You said what needed saying. **Failure:** Just sounds. No words. Then nothing. |

#### Effects

All outcomes lead to the same medical result — the variance is in the experience:

| Option | Effects |
|--------|---------|
| fight (success) | Reset Medical Risk to 0, bed rest (5-7 days), +15 Athletics XP (willpower), condition severity reduced, +5 Lance Rep (showed grit) |
| fight (failure) | Reset Medical Risk to 0, bed rest (7 days), condition severity reduced |
| surrender | Reset Medical Risk to 0, bed rest (5 days), condition severity reduced, faster mental recovery |
| speak (success) | Reset Medical Risk to 0, bed rest (6 days), condition severity reduced, +10 Lance Leader relation |
| speak (failure) | Reset Medical Risk to 0, bed rest (7 days), condition severity reduced |

---

## Adding New Escalation Events

### Event Template

When adding new escalation events, use this structure:

```markdown
### {TRACK}-{NUMBER}: {Event Title}

**Event ID:** `{track}_{event_name}`
**Track:** {Track Name}
**Threshold:** {Number} ({Threshold Name})
**Cooldown:** {Number} days

#### Metadata

\```json
{
  "id": "{track}_{event_name}",
  "category": "escalation",
  "track": "{track}",
  "threshold": {number},
  "threshold_name": "{name}",
  "threshold_direction": "positive|negative",  // Only for Lance Rep
  "delivery": "automatic",
  "cooldown_days": {number},
  "priority": "high|critical"
}
\```

#### Setup

{Setup text — 2-4 paragraphs describing the situation}

#### Options

| Option | Text | Risk | Outcome |
|--------|------|------|---------|
| {id} | "{Option text}" | Safe/Risky/Corrupt | {Outcome text} |

#### Effects

| Option | Effects |
|--------|---------|
| {id} | {Track changes, XP, relations, flags} |
```

### Guidelines for New Events

1. **Match the threshold** — Events should feel appropriate to the severity level
2. **Provide agency** — Even in bad situations, player should have meaningful choices
3. **Include recovery paths** — At least one option should reduce the track
4. **Consider cross-track effects** — Heat events might affect Discipline, etc.
5. **Use existing placeholders** — Reference lance mates, leaders, etc. for continuity
6. **Scale consequences** — Higher thresholds = bigger effects

### Linking to Regular Events

Regular duty/training/general events can reference escalation thresholds:

```json
{
  "id": "scout_bribe_offer",
  "triggers": {
    "all": ["has_duty:scout", "heat >= 3"]
  },
  "setup": "A merchant notices your reputation...",
  "notes": "Only fires if player already has some Heat — NPCs react to your known behavior"
}
```

This creates a web of interconnected content where your reputation (good or bad) shapes what events you see.

---

## Summary

### Event Count by Track

| Track | Events | Thresholds Covered |
|-------|--------|-------------------|
| Heat | 4 | 3, 5, 7, 10 |
| Discipline | 5 | 2, 3, 5, 7, 10 |
| Lance Reputation | 4 | −40, −20, +20, +40 |
| Medical Risk | 3 | 3, 4, 5 |
| **Total** | **16** | |

### Quick Reference

| Event ID | Track | Threshold | Summary |
|----------|-------|-----------|---------|
| heat_warning | Heat | 3 | Lance leader warns you privately |
| heat_shakedown | Heat | 5 | Kit inspection, hide evidence or get caught |
| heat_audit | Heat | 7 | Formal ledger review, explain discrepancies |
| heat_exposed | Heat | 10 | Arrested, face charges |
| discipline_warning | Discipline | 2 | Put on notice |
| discipline_extra_duty | Discipline | 3 | Assigned punishment duties |
| discipline_hearing | Discipline | 5 | Formal hearing before captain |
| discipline_blocked | Discipline | 7 | Promotion denied |
| discipline_discharge | Discipline | 10 | Facing dishonorable discharge |
| lance_trusted | Lance Rep | +20 | Lance mate asks for help |
| lance_bonded | Lance Rep | +40 | Warned about upcoming inspection |
| lance_isolated | Lance Rep | −20 | Excluded from fire circle |
| lance_sabotage | Lance Rep | −40 | Equipment sabotaged |
| medical_worsening | Medical | 3 | Condition getting worse |
| medical_complication | Medical | 4 | New complication (fever, infection) |
| medical_emergency | Medical | 5 | Collapse, emergency treatment |

---

*Document version: 1.0*
*Part of: Lance Life System*
*Integrates with: escalation_system.md, phase4_escalation_implementation_guide.md*
