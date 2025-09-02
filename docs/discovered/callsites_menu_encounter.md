# Menu/Encounter Callsites

Generated from "C:\Dev\Enlisted\DECOMPILE" on 2025-09-02 00:46:58 UTC

## GameMenu.SwitchToMenu Callsites

| Caller (Type::Method) | Callee | Arg (literal or var) | File:Line |
|---|---|---|---|
| HideoutCampaignBehavior::game_menu_hideout_after_wait_on_init | GameMenu.SwitchToMenu | "hideout_after_wait" | HideoutCampaignBehavior.cs:117 |
| EncounterGameMenuBehavior::encounter_attack_on_consequence | GameMenu.SwitchToMenu | "encounter" | EncounterGameMenuBehavior.cs:171 |
| EncounterGameMenuBehavior::encounter_leave_on_consequence | GameMenu.SwitchToMenu | "encounter" | EncounterGameMenuBehavior.cs:408 |
| PlayerTownVisitCampaignBehavior::SwitchToMenuIfThereIsAnInterrupt | GameMenu.SwitchToMenu | genericStateMenu (var) | PlayerTownVisitCampaignBehavior.cs:1753 |

## GameMenu.ActivateGameMenu Callsites

| Caller (Type::Method) | Callee | Arg (literal or var) | File:Line |
|---|---|---|---|
| PlayerEncounter::Finish | GameMenu.ActivateGameMenu | "continue_siege_after_attack" | PlayerEncounter.cs:1160 |
| EncounterGuard::TryLeaveEncounter | GameMenu.ActivateGameMenu | "party_wait" | EncounterGuard.cs:41 |

## GameMenu.ExitToLast Callsites

| Caller (Type::Method) | Callee | Arg (literal or var) | File:Line |
|---|---|---|---|
| PlayerEncounter::Finish | GameMenu.ExitToLast | (none) | PlayerEncounter.cs:1153 |
| GameMenuManager::ExitToLast | GameMenu.ExitToLast | (none) | GameMenuManager.cs:434 |
