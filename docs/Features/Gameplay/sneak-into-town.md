# Feature Spec: Sneak Into Town (Enlisted)

## Overview
Replace the enlisted "Visit Town" action with a "Sneak Into Town" action that uses the native disguise/sneak flow, adds bespoke enlisted penalties when caught, and enforces a 14-day cooldown between attempts.

## Purpose
Let enlisted players infiltrate a town while keeping parity with Bannerlord's built-in sneak mechanics and providing clear consequences when the attempt fails.

## Inputs/Outputs
**Inputs**
- Enlisted status and current lord (only while the lord is in a town)
- Town being visited and its map faction
- Current cooldown timestamp for sneak attempts
- Player gold and relations with the lord and faction
- Native disguise detection chance from `Campaign.Current.Models.DisguiseDetectionModel`

**Outputs**
- Starts the vanilla sneak attempt flow (success → `menu_sneak_into_town_succeeded`, failure → `menu_sneak_into_town_caught`)
- On failure: relation penalties (-10 lord, -2 faction), 100 gold fine; suppress vanilla crime rating gain
- Cooldown start time set to 14 days after any attempt (success or failure)
- Debug log entry under `Modules/Enlisted/Debugging/` for the attempt result

## Behavior
- Availability
  - Button text changes to "Sneak Into Town" when the lord is in a town; keep existing castle text/behavior unchanged.
  - Disabled with tooltip when a sneak attempt is on cooldown; tooltip shows remaining days (rounded up).
  - Hidden whenever the enlisted menu would have hidden the old Visit action (not enlisted, no settlement, in native town menus, battle/siege/encounter guard rails remain).
- Activation flow
  - Reuse the existing synthetic outside-encounter creation in `OnVisitTownSelected` to reach `town_outside`.
  - After the outside menu is established, immediately run the vanilla sneak attempt logic (mirror `game_menu_town_disguise_yourself_on_consequence`): calculate disguise detection probability, roll success/failure with `MBRandom.RandomFloat`, set `Campaign.Current.IsMainHeroDisguised = true`, and push either `menu_sneak_into_town_succeeded` or `menu_sneak_into_town_caught`.
  - Stamp the cooldown timer as soon as the attempt starts to prevent retry spamming in the same visit.
- Success path
  - Preserve existing enlisted return handling; player enters town disguised via the native success menu and keeps the synthetic encounter flag so cleanup works when leaving.
- Failure path
  - Prevent crime rating increases for these enlisted sneak attempts by neutralizing the native caught init effect (see Implementation notes).
  - Add enlisted-specific penalties once per caught menu open:
    - `ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, currentLord, -10);`
    - `ChangeRelationAction.ApplyPlayerRelation(lord.MapFaction.Leader, -2, true, true);`
    - Fine of 100 gold: if the hero has less than 100, take what is available; route gold to the town owner (or just deduct if no owner).
  - Respect existing captivity flow (`menu_captivity_castle_taken_prisoner`) and avoid double-applying penalties via a one-shot guard flag tied to the current attempt.
- Cooldown gating
  - Track next allowed attempt time on the enlisted state (persisted with save data). Cooldown starts on attempt initiation; reset only after 14 in-game days elapse.
- Logging
  - Emit a single-line log per attempt into `Modules/Enlisted/Debugging/` session log: settlement, success/failure, roll %, penalties applied, remaining gold.

## Edge Cases
- Do not offer sneaking in villages or while any army/siege/battle encounter is active.
- If the player is already inside a native town/castle menu, suppress the button to avoid recursion.
- If gold is insufficient, cap the fine at available gold but still apply relation penalties and cooldown.
- Guard against multiple caught-menu openings (e.g., load screens) by tagging the attempt and applying penalties once.
- Ensure `_syntheticOutsideEncounter` cleanup still runs when leaving the settlement after success/failure to restore the enlisted hidden-party state.

## Acceptance Criteria
- Button shows as "Sneak Into Town" when the lord is in a town; castle flow remains unchanged.
- Button is disabled with a clear tooltip while the 14-day cooldown is active; re-enables after the timer expires.
- Clicking the button triggers the native sneak chance roll and routes to the native success or caught menus without assertion errors.
- On failure, relations change by -10 (lord) and -2 (faction), the player loses up to 100 gold, and vanilla crime rating gain still occurs.
- Cooldown starts after any attempt and blocks further attempts for 14 days.
- Debug log records each attempt with outcome and penalties.

## Reference Notes
- The enlisted menu currently exposes "Visit Town/Castle" via `OnVisitTownSelected` and dynamic text setting, which we will retarget for sneaking.
- Native sneak flow is defined in `EncounterGameMenuBehavior`: the "Disguise yourself and sneak through the gate." option leads to either `menu_sneak_into_town_succeeded` or `menu_sneak_into_town_caught`. For enlisted sneaks we must override/neutralize the caught crime rating so no criminal rating is applied.

