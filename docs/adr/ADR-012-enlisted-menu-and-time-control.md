## ADR-012: In-Game Enlistment Menu and Time-Control Behavior

- Status: Accepted
- Date: 2025-08-29
- Owners: Enlisted Team

### Context

We implemented an in-game enlistment experience inspired by Freelancer/SAS where a persistent soldier menu remains open as an overlay while the campaign continues to run. Early iterations paused the game or required clicking "Return to campaign" to regain control. We needed a design that:

- Opens a status/report panel immediately after enlisting
- Keeps the campaign time controls (spacebar/arrow ribbon) responsive while the panel is expanded
- Avoids hidden encounter contexts that trap inputs
- Aligns with the official Bannerlord 1.2.12 API

### Decision

Adopt a persistent enlisted menu that preserves campaign time controls by:

1) Finishing any active encounter before opening our menu
2) Draining stray menus to a clean state
3) Opening the enlisted menu and explicitly enabling time controls
4) Marking the menu as a wait-context so inputs remain responsive

This mirrors patterns observed in Freelancer/SAS decompiles while remaining within the 1.2.12 API.

### Implementation (authoritative pattern)

- Registration
  - Use `CampaignGameStarter.AddGameMenu` (or `AddWaitGameMenu` where appropriate)
  - Flags: `GameMenu.MenuFlags.None`
  - Overlay: `GameOverlays.MenuOverlayType.None`

- On enlist (before any escort/camera state):
```csharp
// A) Close encounter and drain menus
if (PlayerEncounter.Current != null)
{
    PlayerEncounter.Finish(true);
}
for (int i = 0; i < 5 && Campaign.Current?.CurrentMenuContext != null; i++)
{
    GameMenu.ExitToLast();
}

// B) Ensure ribbon time controls are available
Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay;

// C) Open status menu (choose based on context)
if (Campaign.Current?.CurrentMenuContext != null)
{
    GameMenu.SwitchToMenu("enlisted_soldier_status");
}
else
{
    GameMenu.ActivateGameMenu("enlisted_soldier_status");
}
```

- Menu init (OnInit delegate):
```csharp
// Keep ribbon time controls and mark as wait so inputs work while panel is open
Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay;
args.MenuContext.GameMenu.StartWait();
Campaign.Current.GameMenuManager.RefreshMenuOptions(Campaign.Current.CurrentMenuContext);
```

- While our menu is active (optional safety in tick):
```csharp
// If an encounter resurfaces, finish and re-open the status menu
if (PlayerEncounter.Current != null)
{
    PlayerEncounter.Finish(true);
    int safety = 3;
    while (Campaign.Current?.CurrentMenuContext != null && safety-- > 0)
        GameMenu.ExitToLast();
    GameMenu.ActivateGameMenu("enlisted_soldier_status");
    Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay;
}
```

### Rationale

- The combination of finishing encounters, draining menus, and `StartWait()` with `StoppablePlay` preserves input focus and matches the user experience of Freelancer/SAS where the menu panel can stay open/collapsible without pausing the game.
- Using `MenuFlags.None` and `MenuOverlayType.None` avoids overlays that could interfere with time controls.

### Alternatives Considered

- Close the enlisted panel immediately after enlistment: Simplifies inputs but deviates from the desired UX.
- Force pause while menu is open: Conflicts with the target design and breaks fluid play.
- Auto-join player clan to lord's kingdom on enlist: Rejected for parity with Freelancer; we remain in our own kingdom by default.

### Consequences

- Menu code must consistently call the pattern above to avoid regressions (especially encounter finishing and `StartWait()`).
- If other systems open native menus/encounters, we must finish/drain before switching back to our panel.

### Rollout & Testing

- Manual steps:
  - Enlist via conversation; verify status panel opens and ribbon time controls (spacebar/arrows) respond immediately
  - Collapse/expand the panel via chevron; verify time continues
  - Enter/exit settlements with the commander; verify wait/status menus appear without trapping inputs

### References

- Bannerlord 1.2.12 API: `GameMenu`, `MenuCallbackArgs`, `CampaignGameStarter`, `PlayerEncounter`, `CampaignTimeControlMode` ([official API site](https://apidoc.bannerlord.com/v/1.2.12/))
- Observed patterns in Freelancer/SAS decompiled behavior (usage of `StoppablePlay`, `StartWait()`, and menu switching)


