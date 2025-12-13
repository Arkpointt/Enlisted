using System;
using System.Collections.Generic;
using System.IO;
using Enlisted.Features.Assignments.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace Enlisted.Features.Lances.Personas
{
    /// <summary>
    /// Phase 5: Named lance role personas (text-only roster, not real troop individuation).
    ///
    /// - Enlisted-only: does nothing when not enlisted.
    /// - Feature-flagged: lance_personas.enabled.
    /// - Safe persistence: stores only primitives/strings.
    /// - Deterministic: per lord + lance id + seed_salt.
    /// </summary>
    public sealed class LancePersonaBehavior : CampaignBehaviorBase
    {
        private const string LogCategory = "LancePersonas";

        private readonly Dictionary<string, LancePersonaRoster> _rosters =
            new Dictionary<string, LancePersonaRoster>(StringComparer.OrdinalIgnoreCase);

        private LancePersonaNamePoolsJson _cachedPools;

        public static LancePersonaBehavior Instance { get; private set; }

        public LancePersonaBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            var rosterKeys = new List<string>(_rosters.Keys);
            rosterKeys.Sort(StringComparer.OrdinalIgnoreCase);
            var rosterCount = rosterKeys.Count;
            dataStore.SyncData("lp_rosterCount", ref rosterCount);

            if (dataStore.IsLoading)
            {
                _rosters.Clear();
                for (var i = 0; i < rosterCount; i++)
                {
                    var key = string.Empty;
                    var lordId = string.Empty;
                    var cultureId = string.Empty;
                    var seed = 0;
                    var memberCount = 0;
                    dataStore.SyncData($"lp_roster_{i}_key", ref key);
                    dataStore.SyncData($"lp_roster_{i}_lord", ref lordId);
                    dataStore.SyncData($"lp_roster_{i}_culture", ref cultureId);
                    dataStore.SyncData($"lp_roster_{i}_seed", ref seed);
                    dataStore.SyncData($"lp_roster_{i}_memberCount", ref memberCount);

                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    var roster = new LancePersonaRoster
                    {
                        LanceKey = key,
                        LordId = lordId ?? string.Empty,
                        CultureId = cultureId ?? string.Empty,
                        Seed = seed,
                        Members = new List<LancePersonaMember>()
                    };

                    for (var m = 0; m < memberCount; m++)
                    {
                        var slot = 0;
                        var posInt = 0;
                        var alive = true;
                        var rankId = string.Empty;
                        var rankFallback = string.Empty;
                        var first = string.Empty;
                        var epithet = string.Empty;

                        dataStore.SyncData($"lp_roster_{i}_m_{m}_slot", ref slot);
                        dataStore.SyncData($"lp_roster_{i}_m_{m}_pos", ref posInt);
                        dataStore.SyncData($"lp_roster_{i}_m_{m}_alive", ref alive);
                        dataStore.SyncData($"lp_roster_{i}_m_{m}_rankId", ref rankId);
                        dataStore.SyncData($"lp_roster_{i}_m_{m}_rankFallback", ref rankFallback);
                        dataStore.SyncData($"lp_roster_{i}_m_{m}_first", ref first);
                        dataStore.SyncData($"lp_roster_{i}_m_{m}_epithet", ref epithet);

                        roster.Members.Add(new LancePersonaMember
                        {
                            SlotIndex = slot,
                            Position = (LancePosition)posInt,
                            IsAlive = alive,
                            RankTitleId = rankId ?? string.Empty,
                            RankTitleFallback = rankFallback ?? string.Empty,
                            FirstName = first ?? string.Empty,
                            Epithet = epithet ?? string.Empty
                        });
                    }

                    _rosters[key] = roster;
                }
            }
            else
            {
                for (var i = 0; i < rosterKeys.Count; i++)
                {
                    var key = rosterKeys[i];
                    if (!_rosters.TryGetValue(key, out var roster) || roster == null)
                    {
                        continue;
                    }

                    var lordId = roster.LordId ?? string.Empty;
                    var cultureId = roster.CultureId ?? string.Empty;
                    var seed = roster.Seed;
                    var members = roster.Members ?? new List<LancePersonaMember>();
                    var memberCount = members.Count;

                    dataStore.SyncData($"lp_roster_{i}_key", ref key);
                    dataStore.SyncData($"lp_roster_{i}_lord", ref lordId);
                    dataStore.SyncData($"lp_roster_{i}_culture", ref cultureId);
                    dataStore.SyncData($"lp_roster_{i}_seed", ref seed);
                    dataStore.SyncData($"lp_roster_{i}_memberCount", ref memberCount);

                    for (var m = 0; m < memberCount; m++)
                    {
                        var member = members[m] ?? new LancePersonaMember();
                        var slot = member.SlotIndex;
                        var posInt = (int)member.Position;
                        var alive = member.IsAlive;
                        var rankId = member.RankTitleId ?? string.Empty;
                        var rankFallback = member.RankTitleFallback ?? string.Empty;
                        var first = member.FirstName ?? string.Empty;
                        var epithet = member.Epithet ?? string.Empty;

                        dataStore.SyncData($"lp_roster_{i}_m_{m}_slot", ref slot);
                        dataStore.SyncData($"lp_roster_{i}_m_{m}_pos", ref posInt);
                        dataStore.SyncData($"lp_roster_{i}_m_{m}_alive", ref alive);
                        dataStore.SyncData($"lp_roster_{i}_m_{m}_rankId", ref rankId);
                        dataStore.SyncData($"lp_roster_{i}_m_{m}_rankFallback", ref rankFallback);
                        dataStore.SyncData($"lp_roster_{i}_m_{m}_first", ref first);
                        dataStore.SyncData($"lp_roster_{i}_m_{m}_epithet", ref epithet);
                    }
                }
            }
        }

        public bool IsEnabled()
        {
            return ConfigurationManager.LoadLancePersonasConfig()?.Enabled == true;
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

                EnsureRosterFor(enlistment.CurrentLord, enlistment.CurrentLanceId);
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Daily tick failed", ex);
            }
        }

        internal LancePersonaRoster GetRosterFor(Hero lord, string lanceId)
        {
            if (!IsEnabled() || lord == null || string.IsNullOrWhiteSpace(lanceId))
            {
                return null;
            }

            var key = BuildLanceKey(lord, lanceId);
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            if (_rosters.TryGetValue(key, out var roster))
            {
                return roster;
            }

            return EnsureRosterFor(lord, lanceId);
        }

        private LancePersonaRoster EnsureRosterFor(Hero lord, string lanceId)
        {
            if (lord == null || string.IsNullOrWhiteSpace(lanceId))
            {
                return null;
            }

            var key = BuildLanceKey(lord, lanceId);
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            if (_rosters.TryGetValue(key, out var existing) && existing != null && existing.Members?.Count == 10)
            {
                return existing;
            }

            var cultureId = lord.Culture?.StringId ?? "empire";
            var seed = ComputeSeed(key, cultureId);

            var pools = LoadNamePools();
            if (pools == null)
            {
                return null;
            }

            if (!pools.Cultures.TryGetValue(cultureId, out var pool) || pool == null)
            {
                // Fallback to empire pool if missing
                pools.Cultures.TryGetValue("empire", out pool);
            }

            var roster = GenerateRoster(key, lord.StringId ?? string.Empty, cultureId, seed, pool);
            _rosters[key] = roster;
            ModLogger.Info(LogCategory, $"Generated roster for {key} (culture={cultureId})");
            return roster;
        }

        private static string BuildLanceKey(Hero lord, string lanceId)
        {
            var lordId = lord?.StringId;
            if (string.IsNullOrWhiteSpace(lordId) || string.IsNullOrWhiteSpace(lanceId))
            {
                return string.Empty;
            }

            return $"{lordId}:{lanceId}";
        }

        private int ComputeSeed(string lanceKey, string cultureId)
        {
            var cfg = ConfigurationManager.LoadLancePersonasConfig() ?? new LancePersonasConfig();
            var salt = cfg.SeedSalt ?? "enlisted";
            var input = $"{salt}|{cultureId}|{lanceKey}";
            return Fnv1a32(input);
        }

        private LancePersonaNamePoolsJson LoadNamePools()
        {
            if (_cachedPools != null)
            {
                return _cachedPools;
            }

            try
            {
                var path = ConfigurationManager.GetModuleDataPathForConsumers(Path.Combine("LancePersonas", "name_pools.json"));
                if (!File.Exists(path))
                {
                    ModLogger.Warn(LogCategory, $"Name pools file missing at: {path}");
                    return null;
                }

                var json = File.ReadAllText(path);
                var parsed = JsonConvert.DeserializeObject<LancePersonaNamePoolsJson>(json);
                if (parsed?.SchemaVersion != 1 || parsed.Cultures == null || parsed.Cultures.Count == 0)
                {
                    ModLogger.Warn(LogCategory, "Invalid name pools schema; expected schemaVersion=1 with cultures");
                    return null;
                }

                _cachedPools = parsed;
                return _cachedPools;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to load name pools", ex);
                return null;
            }
        }

        private LancePersonaRoster GenerateRoster(
            string lanceKey,
            string lordId,
            string cultureId,
            int seed,
            LanceCulturePersonaPoolJson pool)
        {
            var rng = new Random(seed);
            var cfg = ConfigurationManager.LoadLancePersonasConfig() ?? new LancePersonasConfig();

            var roster = new LancePersonaRoster
            {
                LanceKey = lanceKey,
                LordId = lordId ?? string.Empty,
                CultureId = cultureId ?? string.Empty,
                Seed = seed,
                Members = new List<LancePersonaMember>()
            };

            // Slot layout (doc): 1 leader, 1 second, 2 veterans, 4 soldiers, 2 recruits
            roster.Members.Add(GenerateMember(pool, rng, cfg.FemaleLeaderChance, 1, 0, LancePosition.Leader, epithetChance: 0.9f));
            roster.Members.Add(GenerateMember(pool, rng, cfg.FemaleSecondChance, 2, 1, LancePosition.Second, epithetChance: 0.2f));
            roster.Members.Add(GenerateMember(pool, rng, cfg.FemaleVeteranChance, 3, 2, LancePosition.SeniorVeteran, epithetChance: 0.7f));
            roster.Members.Add(GenerateMember(pool, rng, cfg.FemaleVeteranChance, 4, 3, LancePosition.Veteran, epithetChance: 0.6f));

            for (var i = 0; i < 4; i++)
            {
                roster.Members.Add(GenerateMember(pool, rng, cfg.FemaleSoldierChance, 5 + i, 4, LancePosition.Soldier, epithetChance: 0.2f));
            }

            for (var i = 0; i < 2; i++)
            {
                roster.Members.Add(GenerateMember(pool, rng, cfg.FemaleRecruitChance, 9 + i, 5, LancePosition.Recruit, epithetChance: 0.0f));
            }

            return roster;
        }

        private static LancePersonaMember GenerateMember(
            LanceCulturePersonaPoolJson pool,
            Random rng,
            float femaleChance,
            int slotIndex,
            int rankKeyIndex,
            LancePosition position,
            float epithetChance)
        {
            var isFemale = rng.NextDouble() < Clamp01(femaleChance);
            var first = Pick(isFemale ? pool?.FemaleFirst : pool?.MaleFirst, rng) ?? "Soldier";

            var epithet = string.Empty;
            if (epithetChance > 0f && rng.NextDouble() < epithetChance)
            {
                epithet = Pick(pool?.Epithets, rng) ?? string.Empty;
            }

            var (rankId, rankFallback) = ResolveRankTitle(pool, rankKeyIndex);

            return new LancePersonaMember
            {
                SlotIndex = slotIndex,
                Position = position,
                IsAlive = true,
                RankTitleId = rankId,
                RankTitleFallback = rankFallback,
                FirstName = first,
                Epithet = epithet
            };
        }

        private static (string id, string fallback) ResolveRankTitle(LanceCulturePersonaPoolJson pool, int rankKeyIndex)
        {
            // rankKeyIndex mapping:
            // 0 leader, 1 second, 2 senior_veteran, 3 veteran, 4 soldier, 5 recruit
            var key = rankKeyIndex switch
            {
                0 => "leader",
                1 => "second",
                2 => "senior_veteran",
                3 => "veteran",
                4 => "soldier",
                5 => "recruit",
                _ => "soldier"
            };

            if (pool?.RankTitles != null && pool.RankTitles.TryGetValue(key, out var t) && t != null)
            {
                return (t.Id ?? string.Empty, t.Fallback ?? string.Empty);
            }

            return (string.Empty, string.Empty);
        }

        internal static TextObject BuildRankTitleText(string id, string fallback)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return new TextObject(fallback ?? string.Empty);
            }

            var embeddedFallback = fallback ?? string.Empty;
            return new TextObject("{=" + id + "}" + embeddedFallback);
        }

        internal static TextObject BuildFullNameText(LancePersonaMember member)
        {
            if (member == null)
            {
                return new TextObject(string.Empty);
            }

            var rank = BuildRankTitleText(member.RankTitleId, member.RankTitleFallback).ToString().Trim();
            var first = (member.FirstName ?? string.Empty).Trim();
            var epithet = (member.Epithet ?? string.Empty).Trim();

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(rank))
            {
                parts.Add(rank);
            }
            if (!string.IsNullOrWhiteSpace(first))
            {
                parts.Add(first);
            }
            if (!string.IsNullOrWhiteSpace(epithet))
            {
                parts.Add(epithet);
            }

            return new TextObject(string.Join(" ", parts));
        }

        internal static TextObject BuildShortNameText(LancePersonaMember member)
        {
            return new TextObject(member?.FirstName ?? string.Empty);
        }

        private static string Pick(List<string> list, Random rng)
        {
            if (list == null || list.Count == 0 || rng == null)
            {
                return null;
            }

            return list[rng.Next(list.Count)];
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }

        private static int Fnv1a32(string input)
        {
            unchecked
            {
                const int fnvPrime = 16777619;
                var hash = (int)2166136261;
                if (!string.IsNullOrEmpty(input))
                {
                    for (var i = 0; i < input.Length; i++)
                    {
                        hash ^= input[i];
                        hash *= fnvPrime;
                    }
                }
                return hash;
            }
        }
    }
}


