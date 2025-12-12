using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Assignments.Core;
using EnlistedConfig = Enlisted.Features.Assignments.Core.ConfigurationManager;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace Enlisted.Features.Lances.Behaviors
{
    /// <summary>
    /// Text-based “Viking Conquest”-style lance activities.
    ///
    /// - Runs only while enlisted
    /// - Gated by config + player tier
    /// - Presents occasional story popups with choices (skill XP, fatigue, gold)
    /// - Designed to be easy to extend via JSON and easy to disable/remove
    /// </summary>
    public sealed class LanceStoryBehavior : CampaignBehaviorBase
    {
        private const string LogCategory = "LanceLife";

        // Persisted: per-story cooldown and per-week limiter
        private readonly Dictionary<string, CampaignTime> _storyLastFired = new Dictionary<string, CampaignTime>();
        private CampaignTime _nextGlobalEligibleTime = CampaignTime.Zero;
        private int _storiesFiredThisWeek;
        private int _storiesFiredWeekNumber;

        private LanceStoryPack _cachedPack;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, _ => RefreshCache());
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Persist global limiter
            dataStore.SyncData("ll_nextGlobalEligibleTime", ref _nextGlobalEligibleTime);
            dataStore.SyncData("ll_storiesFiredThisWeek", ref _storiesFiredThisWeek);
            dataStore.SyncData("ll_storiesFiredWeekNumber", ref _storiesFiredWeekNumber);

            // Persist per-story last-fired timestamps
            var keys = _storyLastFired.Keys.ToList();
            var count = keys.Count;
            dataStore.SyncData("ll_storyCount", ref count);

            if (dataStore.IsLoading)
            {
                _storyLastFired.Clear();
                for (var i = 0; i < count; i++)
                {
                    var id = string.Empty;
                    var time = CampaignTime.Zero;
                    dataStore.SyncData($"ll_story_{i}_id", ref id);
                    dataStore.SyncData($"ll_story_{i}_time", ref time);
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        _storyLastFired[id] = time;
                    }
                }
            }
            else
            {
                for (var i = 0; i < keys.Count; i++)
                {
                    var id = keys[i];
                    var time = _storyLastFired[id];
                    dataStore.SyncData($"ll_story_{i}_id", ref id);
                    dataStore.SyncData($"ll_story_{i}_time", ref time);
                }
            }
        }

        private void RefreshCache()
        {
            _cachedPack = TryLoadStoryPack();
        }

        private static bool IsEnabled()
        {
            // Lance Life is intentionally separated from lance assignment gating:
            // - lances_enabled governs whether the player has a lance identity at all
            // - lance_life.enabled governs whether stories should fire
            var lancesEnabled = EnlistedConfig.LoadLancesConfig()?.LancesEnabled == true;
            var enabled = EnlistedConfig.LoadLanceLifeConfig()?.Enabled == true;
            return lancesEnabled && enabled;
        }

        private void OnDailyTick()
        {
            try
            {
                if (!IsEnabled())
                {
                    return;
                }

                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    return;
                }

                var cfg = EnlistedConfig.LoadLanceLifeConfig() ?? new LanceLifeConfig();
                var tier = enlistment.EnlistmentTier;
                if (tier < Math.Max(1, cfg.MinTier))
                {
                    return;
                }

                if (cfg.RequireFinalLance && enlistment.IsLanceProvisional)
                {
                    return;
                }

                // Weekly limiter (simple, stable): use campaign day / 7
                var weekNumber = (int)(CampaignTime.Now.ToDays / 7f);
                if (weekNumber != _storiesFiredWeekNumber)
                {
                    _storiesFiredWeekNumber = weekNumber;
                    _storiesFiredThisWeek = 0;
                }

                if (_storiesFiredThisWeek >= Math.Max(0, cfg.MaxStoriesPerWeek))
                {
                    return;
                }

                if (_nextGlobalEligibleTime.IsFuture)
                {
                    return;
                }

                // Only fire when safe: not in battle, encounter, or captivity (same guard as deferred bag check)
                var main = MobileParty.MainParty;
                bool inBattle = main?.Party?.MapEvent != null;
                bool inEncounter = PlayerEncounter.Current != null;
                bool isPrisoner = Hero.MainHero?.IsPrisoner == true;
                if (inBattle || inEncounter || isPrisoner)
                {
                    return;
                }

                _cachedPack ??= TryLoadStoryPack();
                if (_cachedPack?.Stories == null || _cachedPack.Stories.Count == 0)
                {
                    return;
                }

                var candidates = _cachedPack.Stories
                    .Where(s => s != null && s.IsEligible(enlistment))
                    .Where(s => !IsOnCooldown(s.Id, s.CooldownDays))
                    .ToList();

                if (candidates.Count == 0)
                {
                    return;
                }

                // Keep it simple for now: pick 1 candidate at random.
                // (Later: weight by camp conditions, lance storyId, role hint, etc.)
                var picked = candidates[MBRandom.RandomInt(candidates.Count)];
                if (picked == null)
                {
                    return;
                }

                ShowStoryInquiry(picked, enlistment);

                // Update limiters immediately to prevent double-fires.
                _storiesFiredThisWeek++;
                _storyLastFired[picked.Id] = CampaignTime.Now;
                _nextGlobalEligibleTime = CampaignTime.Now + CampaignTime.Days(Math.Max(0, cfg.MinDaysBetweenStories));

                ModLogger.Info(LogCategory, $"Story fired: {picked.Id} (tier={tier}, lance={enlistment.CurrentLanceName})");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error during daily lance story evaluation", ex);
            }
        }

        private bool IsOnCooldown(string storyId, int cooldownDays)
        {
            if (string.IsNullOrWhiteSpace(storyId))
            {
                return true;
            }

            if (cooldownDays <= 0)
            {
                return false;
            }

            if (!_storyLastFired.TryGetValue(storyId, out var last))
            {
                return false;
            }

            var next = last + CampaignTime.Days(cooldownDays);
            return CampaignTime.Now < next;
        }

        private void ShowStoryInquiry(LanceStory story, EnlistmentBehavior enlistment)
        {
            // Viking Conquest-style: a short body and a few choices.
            // We use MultiSelectionInquiryData so each choice can have a tooltip/hint.
            try
            {
                // Localizable strings: each story/option can provide string IDs.
                // If the ID is missing or not found in language XML, Bannerlord falls back to the text we embed in the TextObject.
                var title = ResolveLocalizedString(story.TitleId, story.Title, "{=ll_default_title}Lance Activity", enlistment);
                var body = ResolveLocalizedString(story.BodyId, story.Body, string.Empty, enlistment);

                var options = new List<InquiryElement>();
                foreach (var option in story.Options ?? new List<LanceStoryOption>())
                {
                    if (option == null)
                    {
                        continue;
                    }

                    var enabled = option.IsEnabled(Hero.MainHero);
                    var optionText = ResolveLocalizedString(option.TextId, option.Text, "{=ll_default_continue}Continue", enlistment);
                    var hint = ResolveLocalizedString(option.HintId, option.Hint, string.Empty, enlistment);
                    options.Add(new InquiryElement(option, optionText, null, enabled, hint));
                }

                if (options.Count == 0)
                {
                    return;
                }

                var inquiry = new MultiSelectionInquiryData(
                    titleText: title,
                    descriptionText: body,
                    inquiryElements: options,
                    isExitShown: true,
                    minSelectableOptionCount: 1,
                    maxSelectableOptionCount: 1,
                    affirmativeText: new TextObject("{=ll_inquiry_choose}Choose").ToString(),
                    negativeText: new TextObject("{=ll_inquiry_leave}Leave").ToString(),
                    affirmativeAction: selected =>
                    {
                        try
                        {
                            if (selected?.FirstOrDefault()?.Identifier is LanceStoryOption chosen)
                            {
                                ApplyOption(chosen);
                            }
                        }
                        catch (Exception ex)
                        {
                            ModLogger.Error(LogCategory, "Error applying lance story option", ex);
                        }
                    },
                    negativeAction: _ => { },
                    soundEventPath: string.Empty);

                MBInformationManager.ShowMultiSelectionInquiry(inquiry);
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error showing lance story inquiry", ex);
            }
        }

        private static void ApplyOption(LanceStoryOption option)
        {
            if (option == null)
            {
                return;
            }

            var enlistment = EnlistmentBehavior.Instance;

            // Fatigue cost / restore
            if (option.FatigueCost > 0)
            {
                enlistment?.TryConsumeFatigue(option.FatigueCost, "lance_story");
            }
            if (option.FatigueRestore > 0)
            {
                enlistment?.RestoreFatigue(option.FatigueRestore, "lance_story");
            }

            // Gold cost (small, optional; kept internal and simple)
            if (option.GoldCost > 0)
            {
                if (Hero.MainHero.Gold >= option.GoldCost)
                {
                    GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, option.GoldCost);
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=ll_not_enough_coin}You don't have enough coin.").ToString(), Colors.Red));
                    return;
                }
            }

            // Skill XP
            if (option.SkillXp != null && option.SkillXp.Count > 0)
            {
                foreach (var kvp in option.SkillXp)
                {
                    var skill = ResolveSkill(kvp.Key);
                    var amount = kvp.Value;
                    if (skill != null && amount > 0)
                    {
                        Hero.MainHero.AddSkillXp(skill, amount);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(option.ResultText))
            {
                InformationManager.DisplayMessage(new InformationMessage(option.ResultText, Colors.White));
            }
        }

        private static string ResolveLocalizedString(string textId, string fallbackText, string defaultText, EnlistmentBehavior enlistment)
        {
            // Prefer a provided ID, otherwise just return fallback/default.
            if (string.IsNullOrWhiteSpace(textId))
            {
                return !string.IsNullOrWhiteSpace(fallbackText) ? fallbackText : (defaultText ?? string.Empty);
            }

            var embeddedFallback = !string.IsNullOrWhiteSpace(fallbackText) ? fallbackText : (defaultText ?? string.Empty);
            var t = new TextObject("{=" + textId + "}" + embeddedFallback);
            ApplyCommonStoryTextVariables(t, enlistment);
            return t.ToString();
        }

        private static void ApplyCommonStoryTextVariables(TextObject text, EnlistmentBehavior enlistment)
        {
            if (text == null)
            {
                return;
            }

            // Keep this minimal and safe: these variables are always available while enlisted.
            text.SetTextVariable("PLAYER_NAME", Hero.MainHero?.Name ?? TextObject.Empty);
            text.SetTextVariable("LORD_NAME", enlistment?.EnlistedLord?.Name ?? new TextObject("{=enlist_fallback_army}the army"));
            text.SetTextVariable("LANCE_NAME", !string.IsNullOrWhiteSpace(enlistment?.CurrentLanceName) ? new TextObject(enlistment.CurrentLanceName) : TextObject.Empty);
        }

        private static SkillObject ResolveSkill(string skillName)
        {
            if (string.IsNullOrWhiteSpace(skillName))
            {
                return null;
            }

            try
            {
                return MBObjectManager.Instance.GetObject<SkillObject>(skillName);
            }
            catch
            {
                ModLogger.Warn(LogCategory, $"Unknown skill in lance story pack: {skillName}");
                return null;
            }
        }

        private static LanceStoryPack TryLoadStoryPack()
        {
            try
            {
                var path = EnlistedConfig.GetModuleDataPathForConsumers("lance_stories.json");
                if (string.IsNullOrWhiteSpace(path))
                {
                    return null;
                }

                if (!System.IO.File.Exists(path))
                {
                    ModLogger.Warn(LogCategory, $"lance_stories.json not found at: {path}");
                    return null;
                }

                var json = System.IO.File.ReadAllText(path);
                var pack = JsonConvert.DeserializeObject<LanceStoryPack>(json);
                if (pack?.SchemaVersion != 1)
                {
                    ModLogger.Warn(LogCategory,
                        $"Unsupported lance story schema version: {pack?.SchemaVersion}. Expected: 1");
                }

                return pack;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to load lance story pack", ex);
                return null;
            }
        }

        // =============================================================================================
        // JSON models (intentionally minimal for text-based VC-style events)
        // =============================================================================================

        private sealed class LanceStoryPack
        {
            [JsonProperty("schemaVersion")] public int SchemaVersion { get; set; } = 1;
            [JsonProperty("stories")] public List<LanceStory> Stories { get; set; } = new List<LanceStory>();
        }

        private sealed class LanceStory
        {
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("title")] public string Title { get; set; }
            [JsonProperty("body")] public string Body { get; set; }
            [JsonProperty("titleId")] public string TitleId { get; set; }
            [JsonProperty("bodyId")] public string BodyId { get; set; }

            [JsonProperty("minTier")] public int MinTier { get; set; } = 1;
            [JsonProperty("requireFinalLance")] public bool RequireFinalLance { get; set; } = true;
            [JsonProperty("cooldownDays")] public int CooldownDays { get; set; } = 7;

            [JsonProperty("options")] public List<LanceStoryOption> Options { get; set; } = new List<LanceStoryOption>();

            public bool IsEligible(EnlistmentBehavior enlistment)
            {
                if (enlistment == null)
                {
                    return false;
                }

                if (enlistment.EnlistmentTier < Math.Max(1, MinTier))
                {
                    return false;
                }

                if (RequireFinalLance && enlistment.IsLanceProvisional)
                {
                    return false;
                }

                return true;
            }
        }

        private sealed class LanceStoryOption
        {
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("text")] public string Text { get; set; }
            [JsonProperty("hint")] public string Hint { get; set; }
            [JsonProperty("textId")] public string TextId { get; set; }
            [JsonProperty("hintId")] public string HintId { get; set; }

            [JsonProperty("fatigueCost")] public int FatigueCost { get; set; }
            [JsonProperty("fatigueRestore")] public int FatigueRestore { get; set; }
            [JsonProperty("goldCost")] public int GoldCost { get; set; }

            [JsonProperty("skillXp")] public Dictionary<string, int> SkillXp { get; set; } = new Dictionary<string, int>();

            // Optional: a simple outcome line (kept plain for now; can be localized later)
            [JsonProperty("resultText")] public string ResultText { get; set; }

            public bool IsEnabled(Hero hero)
            {
                if (hero == null)
                {
                    return false;
                }

                if (GoldCost > 0 && hero.Gold < GoldCost)
                {
                    return false;
                }

                // We don't hard-disable on fatigue here; fatigue spending is "soft" in current Enlisted design.
                return true;
            }
        }
    }
}


