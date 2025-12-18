using System;
using System.Collections.Generic;
using System.IO;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using Enlisted.Features.Assignments.Core;
using EnlistedConfigMgr = Enlisted.Features.Assignments.Core.ConfigurationManager;

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
            SaveLoadDiagnostics.SafeSyncData(this, dataStore, () =>
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
                            var daysInService = 0;
                            var battlesParticipated = 0;
                            var xp = 0;

                            dataStore.SyncData($"lp_roster_{i}_m_{m}_slot", ref slot);
                            dataStore.SyncData($"lp_roster_{i}_m_{m}_pos", ref posInt);
                            dataStore.SyncData($"lp_roster_{i}_m_{m}_alive", ref alive);
                            dataStore.SyncData($"lp_roster_{i}_m_{m}_rankId", ref rankId);
                            dataStore.SyncData($"lp_roster_{i}_m_{m}_rankFallback", ref rankFallback);
                            dataStore.SyncData($"lp_roster_{i}_m_{m}_first", ref first);
                            dataStore.SyncData($"lp_roster_{i}_m_{m}_epithet", ref epithet);
                            dataStore.SyncData($"lp_roster_{i}_m_{m}_days", ref daysInService);
                            dataStore.SyncData($"lp_roster_{i}_m_{m}_battles", ref battlesParticipated);
                            dataStore.SyncData($"lp_roster_{i}_m_{m}_xp", ref xp);

                            roster.Members.Add(new LancePersonaMember
                            {
                                SlotIndex = slot,
                                Position = (LancePosition)posInt,
                                IsAlive = alive,
                                RankTitleId = rankId ?? string.Empty,
                                RankTitleFallback = rankFallback ?? string.Empty,
                                FirstName = first ?? string.Empty,
                                Epithet = epithet ?? string.Empty,
                                DaysInService = daysInService,
                                BattlesParticipated = battlesParticipated,
                                ExperiencePoints = xp
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
                            var daysInService = member.DaysInService;
                            var battlesParticipated = member.BattlesParticipated;
                            var xp = member.ExperiencePoints;

                            dataStore.SyncData($"lp_roster_{i}_m_{m}_slot", ref slot);
                            dataStore.SyncData($"lp_roster_{i}_m_{m}_pos", ref posInt);
                            dataStore.SyncData($"lp_roster_{i}_m_{m}_alive", ref alive);
                            dataStore.SyncData($"lp_roster_{i}_m_{m}_rankId", ref rankId);
                            dataStore.SyncData($"lp_roster_{i}_m_{m}_rankFallback", ref rankFallback);
                            dataStore.SyncData($"lp_roster_{i}_m_{m}_first", ref first);
                            dataStore.SyncData($"lp_roster_{i}_m_{m}_epithet", ref epithet);
                            dataStore.SyncData($"lp_roster_{i}_m_{m}_days", ref daysInService);
                            dataStore.SyncData($"lp_roster_{i}_m_{m}_battles", ref battlesParticipated);
                            dataStore.SyncData($"lp_roster_{i}_m_{m}_xp", ref xp);
                        }
                    }
                }
            });
        }

        public bool IsEnabled()
        {
            return EnlistedConfigMgr.LoadLancePersonasConfig()?.Enabled == true;
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

                var lord = enlistment.CurrentLord;
                if (lord == null)
                {
                    return;
                }

                // Simulate all lances in the lord's party (not just the player's current lance).
                // This keeps Camp UI "other lances" from being static placeholders.
                var partyLances = LanceRegistry.GetLordPartyLances(
                    lord,
                    playerCurrentLanceId: enlistment.CurrentLanceId,
                    maxLances: 3);

                if (partyLances == null || partyLances.Count == 0)
                {
                    // Fallback: at least ensure the player's current lance exists.
                    partyLances = new List<LanceAssignment>
                    {
                        LanceRegistry.ResolveLanceById(enlistment.CurrentLanceId)
                    };
                }

                var cultureId = lord.Culture?.StringId ?? "empire";
                
                // Calculate membercount distribution across lances based on actual party size
                var memberCountDistribution = CalculateMemberDistribution(lord, partyLances.Count);
                
                for (int i = 0; i < partyLances.Count; i++)
                {
                    var lance = partyLances[i];
                    var lanceId = lance?.Id ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(lanceId))
                    {
                        continue;
                    }

                    var targetMemberCount = i < memberCountDistribution.Count ? memberCountDistribution[i] : 10;
                    var roster = EnsureRosterFor(lord, lanceId, targetMemberCount);
                    if (roster?.Members == null)
                    {
                        continue;
                    }

                    // Increment days in service for all living members
                    foreach (var member in roster.Members)
                    {
                        if (member.IsAlive)
                        {
                            member.DaysInService++;
                        }
                    }

                    // Check for promotions once per day (one per roster/day)
                    ProcessPromotions(roster, cultureId);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Daily tick failed", ex);
            }
        }
        
        /// <summary>
        /// Process promotions for eligible lance members.
        /// Promotions happen when a member has served 30+ days and been in 3+ battles.
        /// Only one promotion per day to create story moments.
        /// </summary>
        private void ProcessPromotions(LancePersonaRoster roster, string cultureId)
        {
            if (roster?.Members == null) return;
            
            // Find the most eligible member for promotion (highest XP among eligible)
            LancePersonaMember promotionCandidate = null;
            foreach (var member in roster.Members)
            {
                if (!member.IsAlive) continue;
                if (!member.IsPromotionEligible) continue;
                
                if (promotionCandidate == null || member.ExperiencePoints > promotionCandidate.ExperiencePoints)
                {
                    promotionCandidate = member;
                }
            }
            
            if (promotionCandidate == null) return;
            
            // Try to promote to the next position if slot is available
            var nextPosition = GetNextPosition(promotionCandidate.Position);
            if (nextPosition == promotionCandidate.Position) return; // Already at max
            
            // Check if there's room (the position above them needs to be vacant or they outrank that person)
            var canPromote = CanPromoteToPosition(roster, promotionCandidate, nextPosition);
            if (!canPromote) return;
            
            // Perform the promotion
            var oldPosition = promotionCandidate.Position;
            promotionCandidate.Position = nextPosition;
            
            // Update rank title based on new position
            var pools = LoadNamePools();
            string newRankDisplay = GetDefaultRankName(nextPosition);
            if (pools != null && pools.Cultures.TryGetValue(cultureId, out var pool))
            {
                var rankKey = GetRankKeyForPosition(nextPosition);
                if (pool.RankTitles.TryGetValue(rankKey, out var rankTitle))
                {
                    promotionCandidate.RankTitleId = rankTitle.Id ?? string.Empty;
                    promotionCandidate.RankTitleFallback = rankTitle.Fallback ?? GetDefaultRankName(nextPosition);
                    newRankDisplay = rankTitle.Fallback ?? GetDefaultRankName(nextPosition);
                }
            }
            
            // Reset the counters for next promotion
            promotionCandidate.DaysInService = 0;
            promotionCandidate.BattlesParticipated = 0;
            
            // Show in-game notification to player
            var promotionMessage = $"{promotionCandidate.FirstName} has been promoted to {newRankDisplay}!";
            InformationManager.DisplayMessage(new InformationMessage(promotionMessage, Colors.Green));
            
            // Log the promotion event for diagnostics
            ModLogger.Info(LogCategory, $"PROMOTION: {promotionCandidate.FirstName} promoted from {oldPosition} to {nextPosition}");
        }
        
        /// <summary>
        /// Record a battle participation for all living lance members.
        /// Call this after a battle ends.
        /// </summary>
        public void RecordBattleParticipation(string lanceKey, int xpGain = 10)
        {
            if (!_rosters.TryGetValue(lanceKey, out var roster)) return;
            
            foreach (var member in roster.Members)
            {
                if (member.IsAlive)
                {
                    member.BattlesParticipated++;
                    member.ExperiencePoints += xpGain;
                }
            }
            
            ModLogger.Debug(LogCategory, $"Battle recorded for lance {lanceKey}: +{xpGain} XP to all living members");
        }
        
        /// <summary>
        /// Get the next position up the lance hierarchy.
        /// </summary>
        private LancePosition GetNextPosition(LancePosition current)
        {
            return current switch
            {
                LancePosition.Recruit => LancePosition.Soldier,
                LancePosition.Soldier => LancePosition.Veteran,
                LancePosition.Veteran => LancePosition.SeniorVeteran,
                LancePosition.SeniorVeteran => LancePosition.Second,
                LancePosition.Second => LancePosition.Leader,
                _ => current // Leader stays Leader
            };
        }
        
        /// <summary>
        /// Check if a member can be promoted to a position.
        /// Ensures we don't have too many people at senior ranks.
        /// </summary>
        private bool CanPromoteToPosition(LancePersonaRoster roster, LancePersonaMember candidate, LancePosition targetPosition)
        {
            // Count how many people are at or above the target position (excluding dead)
            int countAtOrAbove = 0;
            foreach (var m in roster.Members)
            {
                if (m.IsAlive && m != candidate && (int)m.Position <= (int)targetPosition)
                {
                    countAtOrAbove++;
                }
            }
            
            // Limits: 1 Leader, 1 Second, 2 Senior Veterans, 3 Veterans, rest are Soldiers/Recruits
            return targetPosition switch
            {
                LancePosition.Leader => countAtOrAbove == 0, // Only one leader
                LancePosition.Second => roster.Members.FindAll(m => m.IsAlive && m.Position == LancePosition.Second && m != candidate).Count == 0,
                LancePosition.SeniorVeteran => roster.Members.FindAll(m => m.IsAlive && m.Position == LancePosition.SeniorVeteran && m != candidate).Count < 2,
                LancePosition.Veteran => roster.Members.FindAll(m => m.IsAlive && m.Position == LancePosition.Veteran && m != candidate).Count < 3,
                _ => true // Soldier has no limit
            };
        }
        
        /// <summary>
        /// Map position to rank key for looking up titles.
        /// </summary>
        private string GetRankKeyForPosition(LancePosition position)
        {
            return position switch
            {
                LancePosition.Leader => "leader",
                LancePosition.Second => "second",
                LancePosition.SeniorVeteran => "senior_veteran",
                LancePosition.Veteran => "veteran",
                LancePosition.Soldier => "soldier",
                LancePosition.Recruit => "recruit",
                _ => "soldier"
            };
        }
        
        /// <summary>
        /// Get default English rank name for a position (fallback).
        /// </summary>
        private string GetDefaultRankName(LancePosition position)
        {
            return position switch
            {
                LancePosition.Leader => "Lance Leader",
                LancePosition.Second => "Lance Second",
                LancePosition.SeniorVeteran => "Senior Veteran",
                LancePosition.Veteran => "Veteran",
                LancePosition.Soldier => "Soldier",
                LancePosition.Recruit => "Recruit",
                _ => "Soldier"
            };
        }

        /// <summary>
        /// Ensure a roster exists with the specified member count.
        /// If the roster exists with a different member count, it will be regenerated.
        /// Called by UI to ensure rosters match current party composition.
        /// </summary>
        internal void EnsureRosterWithMemberCount(Hero lord, string lanceId, int memberCount)
        {
            if (!IsEnabled() || lord == null || string.IsNullOrWhiteSpace(lanceId))
            {
                return;
            }

            EnsureRosterFor(lord, lanceId, memberCount);
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

        private LancePersonaRoster EnsureRosterFor(Hero lord, string lanceId, int targetMemberCount = 10)
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

            // Check if roster exists and has the correct member count
            if (_rosters.TryGetValue(key, out var existing) && existing != null && existing.Members != null)
            {
                // If member count matches target, return existing roster
                if (existing.Members.Count == targetMemberCount)
                {
                    return existing;
                }
                
                // If member count is different, regenerate the roster
                // This handles party size changes (e.g., lord recruited more troops)
                ModLogger.Info(LogCategory, $"Roster {key} member count changed from {existing.Members.Count} to {targetMemberCount}, regenerating");
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

            var roster = GenerateRoster(key, lord.StringId ?? string.Empty, cultureId, seed, pool, targetMemberCount);
            _rosters[key] = roster;
            ModLogger.Info(LogCategory, $"Generated roster for {key} (culture={cultureId}, members={targetMemberCount})");
            return roster;
        }

        /// <summary>
        /// Calculate how to distribute party members across lances.
        /// Example: 87 members with 9 lances = [10,10,10,10,10,10,10,10,7]
        /// </summary>
        private List<int> CalculateMemberDistribution(Hero lord, int lanceCount)
        {
            var distribution = new List<int>();
            
            if (lord?.PartyBelongedTo == null || lanceCount <= 0)
            {
                // Fallback: standard 10-member lances
                for (int i = 0; i < Math.Max(1, lanceCount); i++)
                {
                    distribution.Add(10);
                }
                return distribution;
            }

            int totalMembers = lord.PartyBelongedTo.Party.NumberOfHealthyMembers;
            if (totalMembers <= 0)
            {
                // No members - give each lance 1 member (the leader placeholder)
                for (int i = 0; i < lanceCount; i++)
                {
                    distribution.Add(1);
                }
                return distribution;
            }

            // Distribute members evenly, with remainder going to earlier lances
            int baseCount = totalMembers / lanceCount;
            int remainder = totalMembers % lanceCount;

            for (int i = 0; i < lanceCount; i++)
            {
                int memberCount = baseCount + (i < remainder ? 1 : 0);
                // Ensure at least 1 member per lance, max 20 for sanity
                memberCount = Math.Max(1, Math.Min(20, memberCount));
                distribution.Add(memberCount);
            }

            ModLogger.Debug(LogCategory, $"Distributed {totalMembers} members across {lanceCount} lances: {string.Join(", ", distribution)}");
            return distribution;
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
            var cfg = EnlistedConfigMgr.LoadLancePersonasConfig() ?? new LancePersonasConfig();
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
                var path = EnlistedConfigMgr.GetModuleDataPathForConsumers(Path.Combine("LancePersonas", "name_pools.json"));
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
            LanceCulturePersonaPoolJson pool,
            int memberCount = 10)
        {
            var rng = new Random(seed);
            var cfg = EnlistedConfigMgr.LoadLancePersonasConfig() ?? new LancePersonasConfig();

            var roster = new LancePersonaRoster
            {
                LanceKey = lanceKey,
                LordId = lordId ?? string.Empty,
                CultureId = cultureId ?? string.Empty,
                Seed = seed,
                Members = new List<LancePersonaMember>()
            };

            // Track used first names to ensure uniqueness within the lance
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Ensure at least 1 member (the leader)
            memberCount = Math.Max(1, Math.Min(memberCount, 20)); // Cap at 20 for sanity

            // Dynamic position distribution based on member count
            // Standard full lance (10): 1 leader, 1 second, 2 veterans, 4 soldiers, 2 recruits
            // Smaller lances: prioritize higher ranks first
            
            int slotIndex = 1;
            
            // Always have a leader
            if (slotIndex <= memberCount)
            {
                roster.Members.Add(GenerateMember(pool, rng, cfg.FemaleLeaderChance, slotIndex++, 0, LancePosition.Leader, epithetChance: 0.9f, usedNames));
            }
            
            // Second (if 2+ members)
            if (slotIndex <= memberCount)
            {
                roster.Members.Add(GenerateMember(pool, rng, cfg.FemaleSecondChance, slotIndex++, 1, LancePosition.Second, epithetChance: 0.2f, usedNames));
            }
            
            // Veterans (up to 2 if 3+ members)
            int veteranCount = Math.Min(2, Math.Max(0, memberCount - slotIndex + 1));
            for (int i = 0; i < veteranCount && slotIndex <= memberCount; i++)
            {
                var position = i == 0 ? LancePosition.SeniorVeteran : LancePosition.Veteran;
                var epithet = i == 0 ? 0.7f : 0.6f;
                roster.Members.Add(GenerateMember(pool, rng, cfg.FemaleVeteranChance, slotIndex++, 2, position, epithetChance: epithet, usedNames));
            }
            
            // Soldiers (fill middle ranks)
            int soldierCount = Math.Max(0, memberCount - slotIndex - 2); // Leave room for 2 recruits if possible
            if (memberCount < 6) soldierCount = Math.Max(0, memberCount - slotIndex); // For small lances, use all remaining slots
            
            for (int i = 0; i < soldierCount && slotIndex <= memberCount; i++)
            {
                roster.Members.Add(GenerateMember(pool, rng, cfg.FemaleSoldierChance, slotIndex++, 4, LancePosition.Soldier, epithetChance: 0.2f, usedNames));
            }
            
            // Recruits (fill remaining slots)
            while (slotIndex <= memberCount)
            {
                roster.Members.Add(GenerateMember(pool, rng, cfg.FemaleRecruitChance, slotIndex++, 5, LancePosition.Recruit, epithetChance: 0.0f, usedNames));
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
            float epithetChance,
            HashSet<string> usedNames)
        {
            var isFemale = rng.NextDouble() < Clamp01(femaleChance);
            
            // Generate unique first name within this lance
            // Try up to 50 times to find a unique name before giving up
            var namePool = isFemale ? pool?.FemaleFirst : pool?.MaleFirst;
            string first = "Soldier";
            bool foundUniqueName = false;
            
            if (namePool != null && namePool.Count > 0)
            {
                const int maxAttempts = 50;
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    var candidateName = Pick(namePool, rng);
                    if (!usedNames.Contains(candidateName))
                    {
                        first = candidateName;
                        foundUniqueName = true;
                        break;
                    }
                }
                
                // If we couldn't find a unique name after 50 attempts,
                // append a Roman numeral to make it unique (e.g., "John II")
                if (!foundUniqueName)
                {
                    // Use the last candidate as base
                    var baseName = first;
                    int suffix = 2;
                    while (usedNames.Contains(first))
                    {
                        first = $"{baseName} {ToRomanNumeral(suffix)}";
                        suffix++;
                    }
                }
            }
            
            // Add the final unique name to the set
            usedNames.Add(first);

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

        /// <summary>
        /// Convert a number to Roman numerals for name suffixes (e.g., II, III, IV).
        /// Only supports numbers 2-10 since we shouldn't need more than that for lance names.
        /// </summary>
        private static string ToRomanNumeral(int number)
        {
            return number switch
            {
                2 => "II",
                3 => "III",
                4 => "IV",
                5 => "V",
                6 => "VI",
                7 => "VII",
                8 => "VIII",
                9 => "IX",
                10 => "X",
                _ => number.ToString() // Fallback to Arabic if out of range
            };
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


