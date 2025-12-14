## Camp / Menu Overhaul (Single Source of Truth)

### Goals
- **Main menu (`enlisted_status`)**: Army-wide + general service information.
- **Camp menu (`command_tent`)**: Personal area (player-centric stats + camp news).
- **Lance menu (`enlisted_lance`)**: Lance-centric interactions and (now) lance activities.

### Implemented changes
- **Seek Medical Attention moved into Camp**
  - Removed from `enlisted_status`.
  - Added to Camp menu as a managed option that opens `enlisted_medical`.

- **Removed Activity Log**
  - Deleted the Camp option and its popup.

- **Removed XP Breakdown**
  - Deleted the Camp option and its popup.

- **Service Records now covers the removed info**
  - `Review Service Records` now includes:
    - Current enlistment snapshot (tier, XP, days served, fatigue)
    - Next tier requirement
    - Term snapshot (battles, kills)
    - XP sources explanation

- **Request Discharge UX updated**
  - Removed `Cancel Pending Discharge` button.
  - `Request Discharge` now toggles:
    - If **not pending**: opens a **confirmation popup** describing that it resolves at **next pay muster** and previews expected consequences.
    - If **pending**: button becomes **Cancel Discharge Request**.

- **Camp menu now has more information**
  - Camp main text now includes:
    - Personal service stats (tier, XP, days served, fatigue, condition)
    - Pay snapshot (pending pay, owed backpay)
    - Term snapshot (battles, kills)
    - Camp bulletin (driven by `CampLifeBehavior` meters)

- **Army-wide stats added to main menu**
  - `enlisted_status` now shows whether the lord is in an army and the army party count (and attempts a best-effort man count).

- **Camp Activities tightened up**
  - Kept organized categories + tooltips.
  - **Removed the “Lance” category** from Camp Activities.

- **Lance Activities moved into the Lance menu**
  - Added `Lance Activities` to `enlisted_lance`.
  - New submenu `enlisted_lance_activities` lists activities from the JSON catalog where `category == "lance"`.

### Notes / Follow-ups
- Text IDs were added inline with English fallback (so they render even without localization updates). If desired, add proper entries to `ModuleData/Languages/enlisted_strings.xml`.

### Files changed
- `src/Features/Camp/CampMenuHandler.cs`
- `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`
- `src/Features/Lances/Behaviors/EnlistedLanceMenuBehavior.cs`
