using System.Collections.Generic;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace Enlisted.Features.Lances.Behaviors
{
    /// <summary>
    /// Manages persistent unique banners for each lance under each lord.
    /// Banners are generated once and saved with the campaign, ensuring
    /// consistent visual identity for lances throughout the game.
    /// </summary>
    public sealed class LanceBannerManager : CampaignBehaviorBase
    {
        private const string LogCategory = "LanceBanners";

        // Store banner codes (serialized string representation) per lord+lance
        // Key: "{lordId}_{lanceId}", Value: Banner serialized code (synced via SyncData, not SaveableField)
        private Dictionary<string, string> _lanceBannerCodes;

        public static LanceBannerManager Instance { get; private set; }

        public LanceBannerManager()
        {
            _lanceBannerCodes = new Dictionary<string, string>();
            Instance = this;
        }

        public override void RegisterEvents()
        {
            // No events needed - banners are generated/retrieved on demand
        }

        public override void SyncData(IDataStore dataStore)
        {
            SaveLoadDiagnostics.SafeSyncData(this, dataStore, () =>
            {
                dataStore.SyncData("_lanceBannerCodes", ref _lanceBannerCodes);
                _lanceBannerCodes ??= new Dictionary<string, string>();

                if (dataStore.IsLoading)
                {
                    ModLogger.Info(LogCategory, $"Loaded {_lanceBannerCodes.Count} persistent lance banners");
                }
                else
                {
                    ModLogger.Debug(LogCategory, $"Saving {_lanceBannerCodes.Count} persistent lance banners");
                }
            });
        }

        /// <summary>
        /// Get or generate a banner for a specific lance under a specific lord.
        /// Uses player's banner if they are the lance leader (tier 6+).
        /// Otherwise generates and caches a unique random banner.
        /// </summary>
        /// <param name="lord">The lord who commands this lance</param>
        /// <param name="lanceId">The unique identifier for this lance</param>
        /// <param name="isPlayerLanceLeader">True if player is leading this lance (tier 6+)</param>
        /// <returns>A banner instance (either lord's, player's, or unique random)</returns>
        public Banner GetOrCreateLanceBanner(Hero lord, string lanceId, bool isPlayerLanceLeader)
        {
            if (lord == null || string.IsNullOrEmpty(lanceId))
            {
                ModLogger.Error(LogCategory, "Null lord or empty lanceId provided, returning empty banner");
                return new Banner();
            }

            ModLogger.Debug(LogCategory, $"GetOrCreateLanceBanner: lord={lord.Name}, lanceId={lanceId}, isLeader={isPlayerLanceLeader}");

            // If player is lance leader, use the lord's banner to show authority
            if (isPlayerLanceLeader)
            {
                ModLogger.Debug(LogCategory, $"Player is lance leader for {lanceId}, using lord's banner");
                return new Banner(lord.ClanBanner);
            }

            // Generate cache key
            string cacheKey = $"{lord.StringId}_{lanceId}";
            ModLogger.Debug(LogCategory, $"Banner cache key: {cacheKey}");

            // Check if we already have a banner for this lance
            if (_lanceBannerCodes.TryGetValue(cacheKey, out string bannerCode))
            {
                try
                {
                    var banner = new Banner(bannerCode);
                    ModLogger.Debug(LogCategory, $"Retrieved cached banner for {cacheKey}");
                    return banner;
                }
                catch
                {
                    ModLogger.Error(LogCategory, $"Failed to deserialize cached banner for {cacheKey}, regenerating");
                    // Fall through to regenerate
                }
            }

            // Generate new unique random banner and cache it
            // Each lance gets its own unique banner that persists across saves
            var newBanner = Banner.CreateRandomBanner();
            _lanceBannerCodes[cacheKey] = newBanner.Serialize();
            
            ModLogger.Info(LogCategory, $"Generated and cached new unique banner for {cacheKey}");
            return newBanner;
        }

        /// <summary>
        /// Clear all cached banners for a specific lord.
        /// Useful when a lord dies or when player leaves their service.
        /// </summary>
        /// <param name="lord">The lord whose lance banners should be cleared</param>
        public void ClearBannersForLord(Hero lord)
        {
            if (lord == null) return;

            var keysToRemove = new List<string>();
            string lordPrefix = $"{lord.StringId}_";

            foreach (var key in _lanceBannerCodes.Keys)
            {
                if (key.StartsWith(lordPrefix))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _lanceBannerCodes.Remove(key);
            }

            if (keysToRemove.Count > 0)
            {
                ModLogger.Info(LogCategory, $"Cleared {keysToRemove.Count} banners for lord {lord.Name}");
            }
        }

        /// <summary>
        /// Get total number of cached lance banners (for debugging).
        /// </summary>
        public int GetCachedBannerCount()
        {
            return _lanceBannerCodes?.Count ?? 0;
        }
    }
}

