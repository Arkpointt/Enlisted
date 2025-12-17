using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Enlisted.Features.Activities;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Features.Escalation;
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
        private const string LanceActivitiesMenuId = "enlisted_lance_activities";

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
                AddLanceActivitiesMenu(starter);
                ModLogger.Info(LogCategory, "Lance menu registered");
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-LANCE-001", "Failed to register lance menu", ex);
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

            // ═══════════════════════════════════════════════════════════════════
            // SECTION: ROSTER & RELATIONSHIPS
            // ═══════════════════════════════════════════════════════════════════
            
            // View full roster with relationships
            starter.AddGameMenuOption(
                LanceMenuId,
                "lance_view_roster",
                "{=lance_view_roster}View Full Roster",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.TroopSelection;
                    args.Tooltip = new TextObject("{=lance_roster_tooltip}See all members of your lance with relationship indicators.");
                    return true;
                },
                args => ShowLanceRosterPopup(),
                false, 1);

            // ═══════════════════════════════════════════════════════════════════
            // SECTION: INTERACTIONS
            // ═══════════════════════════════════════════════════════════════════
            
            // Talk to Lance Leader
            starter.AddGameMenuOption(
                LanceMenuId,
                "lance_talk_leader",
                "{=lance_talk_leader}Talk to Lance Leader",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
                    var leaderName = GetLanceLeaderName();
                    var leaderRel = GetLeaderRelationshipIndicator();
                    args.Tooltip = new TextObject("{=lance_talk_leader_hint}Speak with {LEADER}. {REL_DESC}\n\nRelationship: {REL}");
                    args.Tooltip.SetTextVariable("LEADER", leaderName);
                    args.Tooltip.SetTextVariable("REL", leaderRel);
                    args.Tooltip.SetTextVariable("REL_DESC", GetLeaderRelationshipDescription());
                    return HasLanceLeader();
                },
                args => TalkToLanceLeader(),
                false, 10);

            // Talk to Second
            starter.AddGameMenuOption(
                LanceMenuId,
                "lance_talk_second",
                "{=lance_talk_second}Talk to Second",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
                    var secondName = GetLanceSecondName();
                    args.Tooltip = new TextObject("{=lance_talk_second_hint}Speak with {SECOND}, the second-in-command.");
                    args.Tooltip.SetTextVariable("SECOND", secondName);
                    return HasLanceSecond();
                },
                args => TalkToLanceSecond(),
                false, 11);

            // Lance Activities (moved out of Camp Activities)
            starter.AddGameMenuOption(
                LanceMenuId,
                "lance_activities",
                "{=lance_activities}Lance Activities",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    var count = GetAvailableLanceActivitiesCount();
                    args.Tooltip = count > 0
                        ? new TextObject("{=lance_activities_tooltip}Spend time with your lance. ({COUNT} available)").SetTextVariable("COUNT", count)
                        : new TextObject("{=lance_activities_tooltip_none}No lance activities available at this time.");
                    return true;
                },
                _ => GameMenu.SwitchToMenu(LanceActivitiesMenuId),
                false, 12);

            // ═══════════════════════════════════════════════════════════════════
            // SECTION: WELFARE
            // ═══════════════════════════════════════════════════════════════════
            
            // Check on wounded
            starter.AddGameMenuOption(
                LanceMenuId,
                "lance_check_wounded",
                "{=lance_check_wounded}Check on the Wounded",
                args =>
                {
                    var woundedCount = GetWoundedCount();
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                    
                    if (woundedCount > 0)
                    {
                        args.Tooltip = new TextObject("{=lance_wounded_tooltip_active}{COUNT} lance mate(s) are recovering from wounds.\n\n+1 Lance Rep for showing concern.");
                        args.Tooltip.SetTextVariable("COUNT", woundedCount);
                        return true;
                    }
                    
                    args.Tooltip = new TextObject("{=lance_wounded_tooltip_none}No wounded lance mates at this time.");
                    return false;
                },
                args => CheckOnWounded(),
                false, 20);

            // Honor the fallen
            starter.AddGameMenuOption(
                LanceMenuId,
                "lance_honor_fallen",
                "{=lance_honor_fallen}Honor the Fallen",
                args =>
                {
                    var fallenCount = GetFallenCount();
                    args.optionLeaveType = GameMenuOption.LeaveType.Mission;
                    
                    if (fallenCount > 0)
                    {
                        args.Tooltip = new TextObject("{=lance_honor_fallen_hint}{COUNT} comrade(s) have fallen in battle.\n\nTake a moment to remember them.\n+2 Lance Rep");
                        args.Tooltip.SetTextVariable("COUNT", fallenCount);
                        return true;
                    }
                    
                    args.Tooltip = new TextObject("{=lance_no_fallen}No fallen to honor. May fortune continue to favor your lance.");
                    return false;
                },
                args => HonorTheFallen(),
                false, 21);

            // ═══════════════════════════════════════════════════════════════════
            // SECTION: STATUS & INFORMATION
            // ═══════════════════════════════════════════════════════════════════
            
            // View lance stats
            starter.AddGameMenuOption(
                LanceMenuId,
                "lance_view_stats",
                "{=lance_view_stats}View Lance History",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    args.Tooltip = new TextObject("{=lance_stats_tooltip}Review your lance's battle record, reputation, and achievements.");
                    return true;
                },
                args => ShowLanceStats(),
                false, 30);

            // Back to enlisted status
            starter.AddGameMenuOption(
                LanceMenuId,
                "lance_back",
                "{=lance_back}Return to Camp",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    args.Tooltip = new TextObject("{=lance_back_hint}Return to the main camp menu.");
                    return true;
                },
                args => GameMenu.SwitchToMenu("enlisted_status"),
                true, 99);
        }

        /// <summary>
        /// Creates a Lance Activities submenu (activities.json category "lance").
        /// </summary>
        private void AddLanceActivitiesMenu(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu(
                LanceActivitiesMenuId,
                "{=lance_act_intro}You find your lance-mates and fall into the small rituals of unit life. What will you do?",
                OnLanceActivitiesMenuInit,
                args => true,
                null,
                (args, dt) => { },
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            AddLanceActivitiesMenuOptions(starter);
        }

        private void OnLanceActivitiesMenuInit(MenuCallbackArgs args)
        {
            try
            {
                // Start wait for time controls
                args.MenuContext.GameMenu.StartWait();
                Campaign.Current.SetTimeControlModeLock(false);

                var captured = QuartermasterManager.CapturedTimeMode ?? Campaign.Current.TimeControlMode;
                var normalized = QuartermasterManager.NormalizeToStoppable(captured);
                Campaign.Current.TimeControlMode = normalized;
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-LANCE-002", "Error initializing lance activities menu", ex);
            }
        }

        private void AddLanceActivitiesMenuOptions(CampaignGameStarter starter)
        {
            var activitiesBehavior = CampActivitiesBehavior.Instance;
            if (activitiesBehavior == null)
            {
                return;
            }

            var priority = 0;
            var lanceActivities = activitiesBehavior.GetAllActivities()
                .Where(a => string.Equals(a.Category, "lance", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (lanceActivities.Count == 0)
            {
                starter.AddGameMenuOption(
                    LanceActivitiesMenuId,
                    "lance_act_none",
                    "{=lance_act_none}(No lance activities configured.)",
                    args =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                        args.IsEnabled = false;
                        return true;
                    },
                    _ => { },
                    false,
                    priority++);
            }
            else
            {
                foreach (var activity in lanceActivities)
                {
                    var activityId = activity.Id;
                    var currentPriority = priority++;
                    starter.AddGameMenuOption(
                        LanceActivitiesMenuId,
                        $"lance_act_{activityId.Replace(".", "_")}",
                        ResolveActivityText(activity.TextId, activity.TextFallback ?? activity.Id),
                        args => IsLanceActivityOptionAvailable(args, activityId),
                        _ => OnLanceActivitySelected(activityId),
                        false,
                        currentPriority);
                }
            }

            // Back to My Lance
            starter.AddGameMenuOption(
                LanceActivitiesMenuId,
                "lance_act_back",
                "{=lance_act_back}Back to My Lance",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    args.Tooltip = new TextObject("{=lance_act_back_hint}Return to the lance menu.");
                    return true;
                },
                _ => GameMenu.SwitchToMenu(LanceMenuId),
                true,
                100);
        }

        private bool IsLanceActivityOptionAvailable(MenuCallbackArgs args, string activityId)
        {
            var activitiesBehavior = CampActivitiesBehavior.Instance;
            var enlistment = EnlistmentBehavior.Instance;

            if (activitiesBehavior == null || enlistment?.IsEnlisted != true)
            {
                return false;
            }

            var activity = activitiesBehavior.GetAllActivities()
                .FirstOrDefault(a => string.Equals(a.Id, activityId, StringComparison.OrdinalIgnoreCase));

            if (activity == null)
            {
                return false;
            }

            args.optionLeaveType = GameMenuOption.LeaveType.TroopSelection;

            var dayPart = Mod.Core.Triggers.CampaignTriggerTrackerBehavior.Instance?.GetDayPart();
            var dayPartToken = dayPart?.ToString().ToLowerInvariant() ?? "day";
            var formation = EnlistedDutiesBehavior.Instance?.GetPlayerFormationType()?.ToLowerInvariant() ?? "infantry";
            var currentDay = (int)CampaignTime.Now.ToDays;

            var disableReasons = new List<string>();
            var statusParts = new List<string>();

            // Tier check
            if (enlistment.EnlistmentTier < activity.MinTier)
            {
                disableReasons.Add($"Requires Tier {activity.MinTier}");
            }

            // Formation check
            if (activity.Formations != null && activity.Formations.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(formation) ||
                    !activity.Formations.Any(f => string.Equals(f, formation, StringComparison.OrdinalIgnoreCase)))
                {
                    var formationList = string.Join("/", activity.Formations.Select(f =>
                        CultureInfo.CurrentCulture.TextInfo.ToTitleCase(f)));
                    disableReasons.Add($"{formationList} only");
                }
            }

            // Day part check
            if (activity.DayParts != null && activity.DayParts.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(dayPartToken) ||
                    !activity.DayParts.Any(dp => string.Equals(dp, dayPartToken, StringComparison.OrdinalIgnoreCase)))
                {
                    var dayPartList = string.Join("/", activity.DayParts.Select(dp =>
                        CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dp)));
                    disableReasons.Add($"Available: {dayPartList}");
                }
            }

            // Cooldown check
            if (activitiesBehavior.TryGetCooldownDaysRemaining(activity, currentDay, out var daysRemaining))
            {
                disableReasons.Add($"Cooldown: {daysRemaining} day{(daysRemaining > 1 ? "s" : "")}");
            }

            // Fatigue check
            if (activity.FatigueCost > 0 && enlistment.FatigueCurrent < activity.FatigueCost)
            {
                disableReasons.Add("Too fatigued");
            }

            // Status lines for tooltip
            if (activity.FatigueCost > 0) statusParts.Add($"Fatigue: -{activity.FatigueCost}");
            if (activity.FatigueRelief > 0) statusParts.Add($"Rest: +{activity.FatigueRelief}");
            if (activity.SkillXp != null && activity.SkillXp.Count > 0)
            {
                var xpList = activity.SkillXp.Select(kvp => $"{kvp.Key} +{kvp.Value}");
                statusParts.Add(string.Join(", ", xpList));
            }

            var hint = ResolveActivityText(activity.HintId, activity.HintFallback ?? "");
            if (statusParts.Count > 0)
            {
                hint += (string.IsNullOrEmpty(hint) ? "" : "\n") + string.Join(" | ", statusParts);
            }
            if (disableReasons.Count > 0)
            {
                hint += (string.IsNullOrEmpty(hint) ? "" : "\n") + "[" + string.Join(", ", disableReasons) + "]";
            }

            args.Tooltip = new TextObject(hint);
            args.IsEnabled = disableReasons.Count == 0;
            return true;
        }

        private void OnLanceActivitySelected(string activityId)
        {
            var activitiesBehavior = CampActivitiesBehavior.Instance;
            if (activitiesBehavior == null)
            {
                return;
            }

            var activity = activitiesBehavior.GetAllActivities()
                .FirstOrDefault(a => string.Equals(a.Id, activityId, StringComparison.OrdinalIgnoreCase));

            if (activity == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=act_not_found}Activity not found.").ToString(), Colors.Red));
                return;
            }

            if (activitiesBehavior.TryExecuteActivity(activity, out var failReason))
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=lance_act_done}You spend the time well.").ToString(), Colors.Green));
                GameMenu.SwitchToMenu(LanceActivitiesMenuId);
            }
            else
            {
                var failMsg = !string.IsNullOrEmpty(failReason)
                    ? new TextObject("{=act_failed}Could not complete activity: {REASON}").SetTextVariable("REASON", failReason).ToString()
                    : new TextObject("{=act_failed_generic}Could not complete the activity.").ToString();
                InformationManager.DisplayMessage(new InformationMessage(failMsg, Colors.Red));
            }
        }

        private static int GetAvailableLanceActivitiesCount()
        {
            try
            {
                var activitiesBehavior = CampActivitiesBehavior.Instance;
                var enlistment = EnlistmentBehavior.Instance;
                if (activitiesBehavior?.IsEnabled() != true || enlistment?.IsEnlisted != true)
                {
                    return 0;
                }

                var dayPart = Mod.Core.Triggers.CampaignTriggerTrackerBehavior.Instance?.GetDayPart();
                var dayPartToken = dayPart?.ToString().ToLowerInvariant() ?? "day";
                var formation = EnlistedDutiesBehavior.Instance?.GetPlayerFormationType()?.ToLowerInvariant() ?? "infantry";
                var currentDay = (int)CampaignTime.Now.ToDays;

                var count = 0;
                foreach (var def in activitiesBehavior.GetAllActivities()
                             .Where(a => string.Equals(a.Category, "lance", StringComparison.OrdinalIgnoreCase)))
                {
                    if (!CampActivitiesBehavior.IsActivityVisibleFor(def, enlistment, formation, dayPartToken))
                    {
                        continue;
                    }

                    // Enabled if not on cooldown and not blocked by fatigue; other checks handled in UI.
                    if (activitiesBehavior.TryGetCooldownDaysRemaining(def, currentDay, out _))
                    {
                        continue;
                    }

                    if (def.FatigueCost > 0 && enlistment.FatigueCurrent < def.FatigueCost)
                    {
                        continue;
                    }

                    count++;
                }

                return count;
            }
            catch
            {
                return 0;
            }
        }

        private static string ResolveActivityText(string textId, string fallback)
        {
            if (string.IsNullOrEmpty(textId))
            {
                return fallback ?? string.Empty;
            }

            try
            {
                return new TextObject("{=" + textId + "}" + (fallback ?? string.Empty)).ToString();
            }
            catch
            {
                return fallback ?? string.Empty;
            }
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
                ModLogger.ErrorCode(LogCategory, "E-LANCE-003", "Error initializing lance menu", ex);
            }
        }

        private void OnLanceMenuTick(MenuCallbackArgs args, CampaignTime dt)
        {
            // No per-tick updates needed
        }

        private void RefreshLanceMenuText()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var escalation = EscalationManager.Instance;

            var sb = new StringBuilder();
            sb.AppendLine();

            // ═══════════════════════════════════════════════════════════════════
            // LANCE HEADER
            // ═══════════════════════════════════════════════════════════════════
            var lanceName = enlistment?.CurrentLanceName ?? "Your Lance";
            sb.AppendLine($"══════ {lanceName} ══════");
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

            // ═══════════════════════════════════════════════════════════════════
            // ESCALATION TRACKS
            // ═══════════════════════════════════════════════════════════════════
            if (escalation?.IsEnabled() == true)
            {
                var state = escalation.State;
                
                // Lance Reputation with visual indicator
                var repValue = state.LanceReputation;
                var repStatus = escalation.GetLanceReputationStatus();
                var repBar = BuildReputationBar(repValue);
                sb.AppendLine($"Lance Rep: {repBar} {(repValue >= 0 ? "+" : "")}{repValue} ({repStatus})");
                
                // Heat indicator (only show if > 0)
                if (state.Heat > 0)
                {
                    var heatBar = BuildTrackBar(state.Heat, 10);
                    var heatWarning = GetHeatWarning(state.Heat);
                    sb.AppendLine($"Heat: {heatBar} {state.Heat}/10 {(string.IsNullOrEmpty(heatWarning) ? "" : $"[{heatWarning}]")}");
                }
                
                // Discipline indicator (only show if > 0)
                if (state.Discipline > 0)
                {
                    var discBar = BuildTrackBar(state.Discipline, 10);
                    var discWarning = GetDisciplineWarning(state.Discipline);
                    sb.AppendLine($"Discipline: {discBar} {state.Discipline}/10 {(string.IsNullOrEmpty(discWarning) ? "" : $"[{discWarning}]")}");
                }
                
                sb.AppendLine();
            }

            // ═══════════════════════════════════════════════════════════════════
            // ROSTER SUMMARY (condensed)
            // ═══════════════════════════════════════════════════════════════════
            sb.AppendLine("— ROSTER SUMMARY —");
            BuildCondensedRoster(sb, enlistment);

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
            sb.AppendLine(new TextObject("{=lance_roster_full_header}Full Lance Roster with Relationships").ToString());
            sb.AppendLine();
            sb.AppendLine("Relationship Key: [+++] Bonded  [++] Trusted  [+] Friendly  [ ] Neutral  [-] Wary  [--] Hostile");
            sb.AppendLine();

            var tier = enlistment?.EnlistmentTier ?? 1;
            var playerSlot = Math.Min(10, Math.Max(3, 12 - tier));
            var playerRank = GetPlayerRankTitle(tier);

            // Get the persona roster for named members
            var personaRoster = TryGetPersonaRoster(enlistment);

            // Build full roster with all 10 slots and relationships
            for (int slot = 1; slot <= 10; slot++)
            {
                if (slot == playerSlot)
                {
                    sb.AppendLine($"[{slot}] YOU — {playerRank}");
                }
                else
                {
                    var memberName = GetMemberDisplayName(personaRoster, slot);
                    var relationship = GetMemberRelationship(personaRoster, slot);
                    var status = GetMemberStatus(personaRoster, slot);
                    sb.AppendLine($"[{slot}] {relationship} {memberName}{status}");
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

        // ═══════════════════════════════════════════════════════════════════
        // CONDENSED ROSTER FOR MENU HEADER
        // ═══════════════════════════════════════════════════════════════════

        private void BuildCondensedRoster(StringBuilder sb, EnlistmentBehavior enlistment)
        {
            var tier = enlistment?.EnlistmentTier ?? 1;
            var playerSlot = Math.Min(10, Math.Max(3, 12 - tier));
            var personaRoster = TryGetPersonaRoster(enlistment);

            // Show Leader, Second, and Player only in condensed view
            var leaderName = GetMemberDisplayName(personaRoster, 1);
            var leaderRel = GetMemberRelationship(personaRoster, 1);
            sb.AppendLine($"Leader: {leaderRel} {leaderName}");

            var secondName = GetMemberDisplayName(personaRoster, 2);
            var secondRel = GetMemberRelationship(personaRoster, 2);
            sb.AppendLine($"Second: {secondRel} {secondName}");

            var playerRank = GetPlayerRankTitle(tier);
            sb.AppendLine($"You: [{playerSlot}] {playerRank}");

            // Show wounded/fallen summary if any
            var woundedCount = GetWoundedCount();
            var fallenCount = GetFallenCount();
            if (woundedCount > 0 || fallenCount > 0)
            {
                sb.AppendLine();
                if (woundedCount > 0)
                    sb.AppendLine($"Wounded: {woundedCount}");
                if (fallenCount > 0)
                    sb.AppendLine($"Fallen: {fallenCount}");
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // RELATIONSHIP HELPERS
        // ═══════════════════════════════════════════════════════════════════

        private static string GetMemberRelationship(LancePersonaRoster roster, int slot)
        {
            // Try to get relationship from persona member
            if (roster?.Members != null)
            {
                var member = roster.Members.Find(m => m != null && m.SlotIndex == slot && m.IsAlive);
                if (member != null)
                {
                    // Use member's relationship value if we have one
                    // For now, generate based on escalation state
                    return GetRelationshipIndicator(GetSimulatedRelationship(member));
                }
            }

            // Default to neutral for unknown members
            return "[ ]";
        }

        private static int GetSimulatedRelationship(LancePersonaMember member)
        {
            // In a full implementation, this would read from saved relationship data
            // For now, derive from lance reputation and member slot
            var escalation = EscalationManager.Instance;
            var baseRep = escalation?.State.LanceReputation ?? 0;

            // Leaders and seniors have slightly higher expectations
            var slotModifier = member.SlotIndex switch
            {
                1 => -5, // Leader is harder to impress
                2 => -3, // Second too
                _ => 0
            };

            return baseRep + slotModifier;
        }

        private static string GetRelationshipIndicator(int relationship)
        {
            return relationship switch
            {
                >= 40 => "[+++]", // Bonded
                >= 20 => "[++]",  // Trusted
                >= 5 => "[+]",    // Friendly
                >= -5 => "[ ]",   // Neutral
                >= -20 => "[-]",  // Wary
                _ => "[--]"       // Hostile
            };
        }

        private static string GetMemberStatus(LancePersonaRoster roster, int slot)
        {
            if (roster?.Members == null) return "";

            var member = roster.Members.Find(m => m != null && m.SlotIndex == slot);
            if (member == null) return "";

            if (!member.IsAlive)
                return " FALLEN";

            // Could add wounded status here when implemented
            return "";
        }

        // ═══════════════════════════════════════════════════════════════════
        // LEADER/SECOND INTERACTION HELPERS
        // ═══════════════════════════════════════════════════════════════════

        private static string GetLanceLeaderName()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var roster = TryGetPersonaRosterStatic(enlistment);

            if (roster?.Members != null)
            {
                var leader = roster.Members.Find(m => m != null && m.SlotIndex == 1 && m.IsAlive);
                if (leader != null)
                {
                    return LancePersonaBehavior.BuildFullNameText(leader)?.ToString() ?? "the Lance Corporal";
                }
            }

            return "the Lance Corporal";
        }

        private static string GetLanceSecondName()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var roster = TryGetPersonaRosterStatic(enlistment);

            if (roster?.Members != null)
            {
                var second = roster.Members.Find(m => m != null && m.SlotIndex == 2 && m.IsAlive);
                if (second != null)
                {
                    return LancePersonaBehavior.BuildFullNameText(second)?.ToString() ?? "the Second";
                }
            }

            return "the Second";
        }

        private static string GetLeaderRelationshipIndicator()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var roster = TryGetPersonaRosterStatic(enlistment);
            return GetMemberRelationship(roster, 1);
        }

        private static string GetLeaderRelationshipDescription()
        {
            var rel = GetSimulatedLeaderRelationship();
            return rel switch
            {
                >= 40 => "They consider you a trusted brother-in-arms.",
                >= 20 => "They respect your service and abilities.",
                >= 5 => "They view you favorably.",
                >= -5 => "They treat you like any other soldier.",
                >= -20 => "They seem wary of you.",
                _ => "There is clear tension between you."
            };
        }

        private static int GetSimulatedLeaderRelationship()
        {
            var escalation = EscalationManager.Instance;
            return (escalation?.State.LanceReputation ?? 0) - 5; // Leader slightly harder to impress
        }

        private static bool HasLanceLeader()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var roster = TryGetPersonaRosterStatic(enlistment);
            if (roster?.Members == null) return true; // Always have a leader

            var leader = roster.Members.Find(m => m != null && m.SlotIndex == 1);
            return leader?.IsAlive ?? true;
        }

        private static bool HasLanceSecond()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var roster = TryGetPersonaRosterStatic(enlistment);
            if (roster?.Members == null) return true; // Always have a second

            var second = roster.Members.Find(m => m != null && m.SlotIndex == 2);
            return second?.IsAlive ?? true;
        }

        private static LancePersonaRoster TryGetPersonaRosterStatic(EnlistmentBehavior enlistment)
        {
            var personaBehavior = LancePersonaBehavior.Instance;
            if (personaBehavior == null || !personaBehavior.IsEnabled())
                return null;

            if (enlistment?.CurrentLord == null || string.IsNullOrWhiteSpace(enlistment.CurrentLanceId))
                return null;

            return personaBehavior.GetRosterFor(enlistment.CurrentLord, enlistment.CurrentLanceId);
        }

        private void TalkToLanceLeader()
        {
            var leaderName = GetLanceLeaderName();
            var rel = GetLeaderRelationshipIndicator();

            // Show a flavor message for now (full conversation system would be more complex)
            var title = new TextObject("{=lance_talk_leader_title}Speaking with {LEADER}");
            title.SetTextVariable("LEADER", leaderName);

            var body = new TextObject("{=lance_talk_leader_body}You approach {LEADER} respectfully.\n\nRelationship: {REL}\n\n\"{MESSAGE}\"");
            body.SetTextVariable("LEADER", leaderName);
            body.SetTextVariable("REL", rel);
            body.SetTextVariable("MESSAGE", GetLeaderDialogue());

            InformationManager.ShowInquiry(
                new InquiryData(
                    title.ToString(),
                    body.ToString(),
                    true, false,
                    new TextObject("{=lance_talk_close}Take your leave").ToString(),
                    string.Empty,
                    () => { },
                    null),
                false);
        }

        private static string GetLeaderDialogue()
        {
            var rel = GetSimulatedLeaderRelationship();
            return rel switch
            {
                >= 40 => "Good to see you. You've proven yourself time and again. I'm proud to have you in this lance.",
                >= 20 => "You're doing well. Keep it up and you'll go far in this army.",
                >= 5 => "Everything in order? Good. Stay sharp out there.",
                >= -5 => "Soldier. Is there something you need?",
                >= -20 => "What do you want? Make it quick.",
                _ => "I've got my eye on you. Watch yourself."
            };
        }

        private void TalkToLanceSecond()
        {
            var secondName = GetLanceSecondName();

            var title = new TextObject("{=lance_talk_second_title}Speaking with {SECOND}");
            title.SetTextVariable("SECOND", secondName);

            var body = new TextObject("{=lance_talk_second_body}You find {SECOND} near the lance's kit.\n\n\"{MESSAGE}\"");
            body.SetTextVariable("SECOND", secondName);
            body.SetTextVariable("MESSAGE", "The lance is in good shape. We look out for each other here. You do the same, and we'll have no trouble.");

            InformationManager.ShowInquiry(
                new InquiryData(
                    title.ToString(),
                    body.ToString(),
                    true, false,
                    new TextObject("{=lance_talk_close}Take your leave").ToString(),
                    string.Empty,
                    () => { },
                    null),
                false);
        }

        // ═══════════════════════════════════════════════════════════════════
        // WELFARE HELPERS
        // ═══════════════════════════════════════════════════════════════════

        private static int GetWoundedCount()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var roster = TryGetPersonaRosterStatic(enlistment);
            if (roster?.Members == null) return 0;

            // Count wounded members (for now, simulated based on recent battles)
            // Full implementation would track actual wounded state
            return roster.Members.Count(m => m != null && m.IsAlive && IsWounded(m));
        }

        private static bool IsWounded(LancePersonaMember member)
        {
            // Placeholder - would check actual wounded tracking
            return false;
        }

        private static int GetFallenCount()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var roster = TryGetPersonaRosterStatic(enlistment);
            if (roster?.Members == null) return 0;

            return roster.Members.Count(m => m != null && !m.IsAlive);
        }

        private void CheckOnWounded()
        {
            var woundedCount = GetWoundedCount();

            var title = new TextObject("{=lance_wounded_title}Checking on the Wounded");
            var body = woundedCount > 0
                ? new TextObject("{=lance_wounded_body_active}You visit the wounded lance mates and offer words of encouragement. They appreciate your concern.\n\n+1 Lance Reputation")
                : new TextObject("{=lance_wounded_body_none}No wounded to check on. The lance is in fighting shape.");

            // Grant small reputation bonus for caring
            if (woundedCount > 0)
            {
                EscalationManager.Instance?.ModifyLanceReputation(1, "Checked on wounded");
            }

            InformationManager.ShowInquiry(
                new InquiryData(
                    title.ToString(),
                    body.ToString(),
                    true, false,
                    new TextObject("{=lance_close}Continue").ToString(),
                    string.Empty,
                    () => { },
                    null),
                false);
        }

        private void HonorTheFallen()
        {
            var fallenCount = GetFallenCount();
            var enlistment = EnlistmentBehavior.Instance;
            var roster = TryGetPersonaRosterStatic(enlistment);

            var sb = new StringBuilder();
            sb.AppendLine("You take a moment to remember those who have fallen:\n");

            if (roster?.Members != null)
            {
                foreach (var member in roster.Members.Where(m => m != null && !m.IsAlive))
                {
                    var name = LancePersonaBehavior.BuildFullNameText(member)?.ToString() ?? "Unknown Soldier";
                    sb.AppendLine($"• {name}");
                }
            }

            sb.AppendLine("\nTheir sacrifice will not be forgotten.\n\n+2 Lance Reputation");

            // Grant reputation for honoring the dead
            EscalationManager.Instance?.ModifyLanceReputation(2, "Honored the fallen");

            InformationManager.ShowInquiry(
                new InquiryData(
                    new TextObject("{=lance_honor_title}Honoring the Fallen").ToString(),
                    sb.ToString(),
                    true, false,
                    new TextObject("{=lance_close}Continue").ToString(),
                    string.Empty,
                    () => { },
                    null),
                false);
        }

        // ═══════════════════════════════════════════════════════════════════
        // STATS / HISTORY
        // ═══════════════════════════════════════════════════════════════════

        private void ShowLanceStats()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var escalation = EscalationManager.Instance;

            var sb = new StringBuilder();
            var lanceName = enlistment?.CurrentLanceName ?? "Your Lance";
            sb.AppendLine($"═══ {lanceName} History ═══\n");

            // Service stats
            var daysServed = (int)(enlistment?.DaysServed ?? 0);
            sb.AppendLine($"Days with Lance: {daysServed}");
            sb.AppendLine($"Your Tier: {enlistment?.EnlistmentTier ?? 1}");
            sb.AppendLine();

            // Escalation stats
            if (escalation?.IsEnabled() == true)
            {
                var state = escalation.State;
                sb.AppendLine("— STANDING —");
                sb.AppendLine($"Lance Reputation: {(state.LanceReputation >= 0 ? "+" : "")}{state.LanceReputation}");
                sb.AppendLine($"Status: {escalation.GetLanceReputationStatus()}");
                sb.AppendLine();

                if (state.Heat > 0 || state.Discipline > 0)
                {
                    sb.AppendLine("— ISSUES —");
                    if (state.Heat > 0)
                        sb.AppendLine($"Heat: {state.Heat}/10 {GetHeatWarning(state.Heat)}");
                    if (state.Discipline > 0)
                        sb.AppendLine($"Discipline: {state.Discipline}/10 {GetDisciplineWarning(state.Discipline)}");
                    sb.AppendLine();
                }
            }

            // Roster stats
            var roster = TryGetPersonaRoster(enlistment);
            var aliveCount = roster?.Members?.Count(m => m != null && m.IsAlive) ?? 10;
            var fallenCount = GetFallenCount();

            sb.AppendLine("— STRENGTH —");
            sb.AppendLine($"Active Members: {aliveCount}/10");
            if (fallenCount > 0)
                sb.AppendLine($"Fallen: {fallenCount}");

            InformationManager.ShowInquiry(
                new InquiryData(
                    new TextObject("{=lance_stats_title}Lance History").ToString(),
                    sb.ToString(),
                    true, false,
                    new TextObject("{=lance_close}Continue").ToString(),
                    string.Empty,
                    () => { },
                    null),
                false);
        }

        // ═══════════════════════════════════════════════════════════════════
        // TRACK BAR HELPERS (shared with main menu)
        // ═══════════════════════════════════════════════════════════════════

        private static string BuildTrackBar(int current, int max)
        {
            const int barLength = 10;
            var filled = Math.Min(barLength, Math.Max(0, current));
            var empty = barLength - filled;
            return new string('▓', filled) + new string('░', empty);
        }

        private static string BuildReputationBar(int reputation)
        {
            // Rep goes from -50 to +50, center is 0
            // Show a centered bar with + and - sides
            var normalized = Math.Max(-50, Math.Min(50, reputation));
            var position = (normalized + 50) / 10; // 0-10 scale

            var leftEmpty = position;
            var rightEmpty = 10 - position;

            if (normalized < 0)
                return new string('░', leftEmpty) + "│" + new string('░', rightEmpty);
            else if (normalized > 0)
                return new string('░', leftEmpty) + "│" + new string('▓', Math.Min(rightEmpty, normalized / 5));
            else
                return new string('░', 5) + "│" + new string('░', 5);
        }

        private static string GetHeatWarning(int heat)
        {
            return heat switch
            {
                >= 10 => "[EXPOSED]",
                >= 7 => "[Audit]",
                >= 5 => "[Shakedown]",
                >= 3 => "[Watched]",
                _ => ""
            };
        }

        private static string GetDisciplineWarning(int discipline)
        {
            return discipline switch
            {
                >= 10 => "[DISCHARGE]",
                >= 7 => "[Blocked]",
                >= 5 => "[Hearing]",
                >= 3 => "[Extra Duty]",
                _ => ""
            };
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
