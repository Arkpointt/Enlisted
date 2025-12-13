using System;
using System.Text;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Features.Lances.Personas;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Enlisted.Features.Lances.Behaviors
{
    /// <summary>
    /// Handles the My Lance menu - roster view, NPC relationships, and lance status.
    /// This menu is accessed from the main enlisted status menu and shows the player's
    /// position within their lance, relationships with lance mates, and wounded/fallen tracking.
    /// </summary>
    public sealed class EnlistedLanceMenuBehavior : CampaignBehaviorBase
    {
        private const string LogCategory = "LanceMenu";
        private const string LanceMenuId = "enlisted_lance";

        public static EnlistedLanceMenuBehavior Instance { get; private set; }

        public EnlistedLanceMenuBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // No persistent state - lance data comes from EnlistmentBehavior
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                AddLanceMenu(starter);
                ModLogger.Info(LogCategory, "Lance menu registered");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Failed to register lance menu: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates the My Lance menu with roster and interaction options.
        /// </summary>
        private void AddLanceMenu(CampaignGameStarter starter)
        {
            // Wait menu with hidden progress for time controls
            starter.AddWaitGameMenu(
                LanceMenuId,
                "{LANCE_MENU_TEXT}",
                OnLanceMenuInit,
                args => true,
                null,
                OnLanceMenuTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            // View full roster
            starter.AddGameMenuOption(
                LanceMenuId,
                "lance_view_roster",
                "{=lance_view_roster}View Full Roster",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    args.Tooltip = new TextObject("{=lance_roster_tooltip}See all members of your lance.");
                    return true;
                },
                args => ShowLanceRosterPopup(),
                false, 1);

            // Check on wounded (conditional - placeholder for future implementation)
            starter.AddGameMenuOption(
                LanceMenuId,
                "lance_check_wounded",
                "{=lance_check_wounded}Check on the Wounded",
                args =>
                {
                    // Will be enabled when casualty tracking is implemented
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                    args.Tooltip = new TextObject("{=lance_wounded_tooltip}Check on wounded lance mates.");
                    return false; // Disabled for now
                },
                args => ShowWoundedStatus(),
                false, 2);

            // Back to enlisted status
            starter.AddGameMenuOption(
                LanceMenuId,
                "lance_back",
                "{=lance_back}Back",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                args => GameMenu.SwitchToMenu("enlisted_status"),
                true, 99);
        }

        /// <summary>
        /// Menu background initialization for lance menu.
        /// </summary>
        [GameMenuInitializationHandler(LanceMenuId)]
        public static void OnLanceMenuBackgroundInit(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            var backgroundMesh = "encounter_looter";

            if (enlistment?.CurrentLord?.Clan?.Kingdom?.Culture?.EncounterBackgroundMesh != null)
            {
                backgroundMesh = enlistment.CurrentLord.Clan.Kingdom.Culture.EncounterBackgroundMesh;
            }
            else if (enlistment?.CurrentLord?.Culture?.EncounterBackgroundMesh != null)
            {
                backgroundMesh = enlistment.CurrentLord.Culture.EncounterBackgroundMesh;
            }

            args.MenuContext.SetBackgroundMeshName(backgroundMesh);
            args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/camp_army");
        }

        private void OnLanceMenuInit(MenuCallbackArgs args)
        {
            try
            {
                RefreshLanceMenuText();

                // Start wait for time controls
                args.MenuContext.GameMenu.StartWait();
                Campaign.Current.SetTimeControlModeLock(false);

                var captured = QuartermasterManager.CapturedTimeMode ?? Campaign.Current.TimeControlMode;
                var normalized = QuartermasterManager.NormalizeToStoppable(captured);
                Campaign.Current.TimeControlMode = normalized;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error initializing lance menu: {ex.Message}");
            }
        }

        private void OnLanceMenuTick(MenuCallbackArgs args, CampaignTime dt)
        {
            // No per-tick updates needed
        }

        private void RefreshLanceMenuText()
        {
            var enlistment = EnlistmentBehavior.Instance;

            var sb = new StringBuilder();
            sb.AppendLine();

            // Lance name header
            var lanceName = enlistment?.CurrentLanceName ?? "Your Lance";
            sb.AppendLine($"— {lanceName} —");
            sb.AppendLine();

            // Player position
            var playerRank = GetPlayerRankTitle(enlistment?.EnlistmentTier ?? 1);
            var posLine = new TextObject("{=lance_position}Your Position: {RANK}");
            posLine.SetTextVariable("RANK", playerRank);
            sb.AppendLine(posLine.ToString());

            // Days served
            var daysServed = (int)(enlistment?.DaysServed ?? 0);
            var statsLine = new TextObject("{=lance_stats}Days with Lance: {DAYS}");
            statsLine.SetTextVariable("DAYS", daysServed);
            sb.AppendLine(statsLine.ToString());
            sb.AppendLine();

            // Roster summary
            sb.AppendLine(new TextObject("{=lance_roster_header}— ROSTER —").ToString());
            BuildRosterSummary(sb, enlistment);

            MBTextManager.SetTextVariable("LANCE_MENU_TEXT", sb.ToString());
        }

        private void BuildRosterSummary(StringBuilder sb, EnlistmentBehavior enlistment)
        {
            var tier = enlistment?.EnlistmentTier ?? 1;
            var playerSlot = Math.Min(10, Math.Max(3, 12 - tier)); // Higher tier = earlier slot
            var playerRank = GetPlayerRankTitle(tier);

            // Try to get the named persona roster for this lance
            var personaRoster = TryGetPersonaRoster(enlistment);
            
            // Build roster with named personas (or fallback to generic slots)
            for (int slot = 1; slot <= 10; slot++)
            {
                if (slot == playerSlot)
                {
                    sb.AppendLine($"[{slot}] YOU — {playerRank}");
                }
                else
                {
                    var memberName = GetMemberDisplayName(personaRoster, slot);
                    sb.AppendLine($"[{slot}] {memberName}");
                }
            }
        }

        /// <summary>
        /// Attempts to get the persona roster for the current lance.
        /// Returns null if personas are disabled or unavailable.
        /// </summary>
        private LancePersonaRoster TryGetPersonaRoster(EnlistmentBehavior enlistment)
        {
            var personaBehavior = LancePersonaBehavior.Instance;
            if (personaBehavior == null || !personaBehavior.IsEnabled())
            {
                return null;
            }

            if (enlistment?.CurrentLord == null || string.IsNullOrWhiteSpace(enlistment.CurrentLanceId))
            {
                return null;
            }

            return personaBehavior.GetRosterFor(enlistment.CurrentLord, enlistment.CurrentLanceId);
        }

        /// <summary>
        /// Gets the display name for a member at the given slot.
        /// Uses persona names if available, otherwise falls back to generic position titles.
        /// </summary>
        private string GetMemberDisplayName(LancePersonaRoster roster, int slot)
        {
            // Try to find a persona member at this slot
            if (roster?.Members != null)
            {
                var member = roster.Members.Find(m => m != null && m.SlotIndex == slot && m.IsAlive);
                if (member != null)
                {
                    var fullName = LancePersonaBehavior.BuildFullNameText(member)?.ToString();
                    if (!string.IsNullOrWhiteSpace(fullName))
                    {
                        return fullName;
                    }
                }
            }

            // Fallback to generic position name if no persona available
            return GetFallbackSlotName(slot);
        }

        /// <summary>
        /// Returns a generic position name for a slot when personas aren't available.
        /// </summary>
        private static string GetFallbackSlotName(int slot)
        {
            return slot switch
            {
                1 => "Lance Corporal (Leader)",
                2 => "Senior Soldier (Second)",
                3 or 4 => "Veteran",
                5 or 6 or 7 or 8 => "Soldier",
                9 or 10 => "Recruit",
                _ => "Soldier"
            };
        }

        private void ShowLanceRosterPopup()
        {
            var enlistment = EnlistmentBehavior.Instance;

            var sb = new StringBuilder();
            sb.AppendLine(new TextObject("{=lance_roster_full_header}Full Lance Roster").ToString());
            sb.AppendLine();

            var tier = enlistment?.EnlistmentTier ?? 1;
            var playerSlot = Math.Min(10, Math.Max(3, 12 - tier));
            var playerRank = GetPlayerRankTitle(tier);

            // Get the persona roster for named members
            var personaRoster = TryGetPersonaRoster(enlistment);

            // Build full roster with all 10 slots
            for (int slot = 1; slot <= 10; slot++)
            {
                if (slot == playerSlot)
                {
                    sb.AppendLine($"[{slot}] YOU — {playerRank}");
                }
                else
                {
                    var memberName = GetMemberDisplayName(personaRoster, slot);
                    sb.AppendLine($"[{slot}] {memberName}");
                }
            }

            InformationManager.ShowInquiry(
                new InquiryData(
                    new TextObject("{=lance_roster_title}Lance Roster").ToString(),
                    sb.ToString(),
                    true,
                    false,
                    new TextObject("{=lance_roster_close}Close").ToString(),
                    string.Empty,
                    () => { },
                    null),
                false);
        }

        private void ShowWoundedStatus()
        {
            var title = new TextObject("{=lance_wounded_title}Wounded Lance Mates");
            var body = new TextObject("{=lance_wounded_body}No wounded to report at this time.");

            InformationManager.ShowInquiry(
                new InquiryData(
                    title.ToString(),
                    body.ToString(),
                    true,
                    false,
                    new TextObject("{=lance_wounded_close}Close").ToString(),
                    string.Empty,
                    () => { },
                    null),
                false);
        }

        private static string GetPlayerRankTitle(int tier)
        {
            // Use culture-specific rank titles from progression config
            var enlistment = EnlistmentBehavior.Instance;
            var cultureId = enlistment?.CurrentLord?.Culture?.StringId ??
                           enlistment?.CurrentLord?.Clan?.Kingdom?.Culture?.StringId ??
                           "mercenary";
            return Enlisted.Features.Assignments.Core.ConfigurationManager.GetCultureRankTitle(tier, cultureId);
        }
    }
}
