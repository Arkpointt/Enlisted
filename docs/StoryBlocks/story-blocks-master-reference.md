# Story Blocks Master Reference

The **Story Blocks Master Reference** is the single source of truth for all narrative and social content in Enlisted v0.9.0. It defines the rules for how events and orders emerge during your military career.

## Index

- [Event Firing Rules](#event-firing-rules)
- [Static & Guaranteed Events](#static-events)
- [Event Mechanics](#event-mechanics)
- [Story Content Categories](#story-content-categories)
- [Design & Writing Guidelines](#design--writing-guidelines)

---

## Event Firing Rules

In v0.9.0, the event system is **Role-Based** and **Context-Aware**, moving away from a rigid schedule.

### Role-Based Events
-   **Trigger**: Hourly Tick (approx. 5% chance per hour).
-   **Frequency**: 1-2 events per day on average.
-   **Routing**: The system checks your primary role (Scout, Medic, Officer, Soldier) and current campaign context (War, Siege, Peace, Camp).
-   **Priority**: Crisis events (High Heat/Discipline) take precedence over role-specific and general events.

### Map Incidents
-   **Trigger**: Specific map actions (Leaving Battle, Entering Settlement, During Siege).
-   **Frequency**: Global cooldown of 8-24 hours between incidents.
-   **Restrictions**: Must be enlisted and safe (not in conversation or battle).

### Escalation Thresholds
-   **Trigger**: Reaching specific levels of Heat, Discipline, or Reputation.
-   **Chance**: 100% when a threshold is crossed for the first time.

---

## Static Events

Guaranteed events that provide structure to the career loop.

-   **Onboarding**: The Oath Ceremony and Initial Inspection (Bag Check).
-   **Milestones**: Proving Events for each rank promotion (T1-T9).
-   **Pay Muster**: The periodic distribution of wages and handling of Pay Tension.
-   **Orders**: Explicit directives issued by the chain of command every ~3 days.

---

## Event Mechanics

### Skill & Trait Checks
Outcomes often depend on your character's capabilities:
-   **Formula**: `Player Skill + Random(0-10) >= Difficulty`.
-   **Difficulty Bands**: Easy (15-20), Moderate (25-30), Hard (35-40), Elite (45+).

### Reputation Tracks
Identity is defined across three tracks (-100 to +100):
-   **Lord Reputation**: Loyalty and strategic success.
-   **Officer Reputation**: Competence and order completion.
-   **Soldier Reputation**: Popularity and unit-wide activities.

### Company Needs Impact
Choices frequently impact the state of the unit:
-   **Readiness**: Unit's combat effectiveness.
-   **Morale**: Men's will to serve.
-   **Supplies**: Food and basic necessities.
-   **Equipment**: Gear maintenance.
-   **Rest**: Recovery from fatigue.

---

## Story Content Categories

### 1. Role-Specific Stories
-   **Scout**: Reconnaissance, tracking, and intelligence.
-   **Officer**: Leadership, discipline, and tactical command.
-   **Medic**: Triage, treatment, and camp health.
-   **Operative**: Covert actions, black markets, and risks.

### 2. Unit Life (Soldier)
-   Daily interactions, gambling, letters home, and unit rivalries.

### 3. Crisis Moments
-   Mutiny plots, supply failures, and major disciplinary hearings.

---

## Design & Writing Guidelines

### Voice & Tone
-   **Military Realism**: Gritty, practical, and focused on the human element of service.
-   **Vocabulary**: Use terms like "muster", "requisition", and "formation" naturally.
-   **Brevity**: Keep setup text to 2-3 concise paragraphs.

### Placeholder Variables
-   `{PLAYER_NAME}`, `{PLAYER_RANK}`, `{PLAYER_ROLE}`
-   `{LORD_NAME}`, `{LORD_TITLE}`
-   `{COMPANY_NAME}`, `{SERGEANT_NAME}`
-   `{ORDER_ISSUER}`

### Choice Structure
-   Every choice should have a trade-off (e.g., gain gold but lose reputation).
-   Outcomes should be multi-dimensional (e.g., succeed in the task but gain fatigue).
-   Avoid "fake" choices where one option is clearly superior in all aspects.
