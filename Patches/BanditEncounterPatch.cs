using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using Enlisted.Behaviors;

namespace Enlisted.Patches
{
    /// <summary>
    /// LEGACY: These encounter patches are kept for compatibility but are no longer the primary 
    /// protection mechanism. The main protection now comes from setting MainParty.IsActive = false
    /// in the OnTick method, which makes the party completely untargetable by encounters.
    /// 
    /// This approach is based on the original "Serve as a Soldier" mod and is much more reliable
    /// than trying to patch every possible encounter entry point.
    /// </summary>
    [HarmonyPatch(typeof(EncounterManager), "StartPartyEncounter")]
    public static class BanditEncounterPatch
    {
        /// <summary>
        /// Legacy prefix patch - now mostly redundant due to IsActive = false approach.
        /// </summary>
        public static bool Prefix(PartyBase attackerParty, PartyBase defenderParty)
        {
            try
            {
                // Get enlistment status first
                var enlistmentBehavior = EnlistmentBehavior.Instance;
                if (enlistmentBehavior == null || !enlistmentBehavior.IsEnlisted) 
                    return true; // Not enlisted, allow all encounters

                // Only apply this patch for encounters involving the main party
                bool isMainPartyInvolved = (attackerParty == PartyBase.MainParty || defenderParty == PartyBase.MainParty);
                if (!isMainPartyInvolved) 
                    return true; // Main party not involved, allow encounter

                // Block hostile encounters - this is backup protection
                if (defenderParty == PartyBase.MainParty)
                {
                    if (IsHostileToEnlistedPlayer(attackerParty, enlistmentBehavior.Commander))
                    {
                        string attackerName = GetPartyName(attackerParty);
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"[Enlisted] {attackerName} avoids attacking you while you serve {enlistmentBehavior.Commander?.Name}."));
                        return false; // Cancel the encounter
                    }
                }

                if (attackerParty == PartyBase.MainParty)
                {
                    if (IsHostileToEnlistedPlayer(defenderParty, enlistmentBehavior.Commander))
                    {
                        string defenderName = GetPartyName(defenderParty);
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"[Enlisted] You cannot attack {defenderName} while serving {enlistmentBehavior.Commander?.Name}."));
                        return false; // Cancel the encounter
                    }
                }

                // Allow the encounter to proceed (friendly or allied encounters)
                return true;
            }
            catch (System.Exception ex)
            {
                // If anything goes wrong, allow the encounter to proceed to avoid breaking the game
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[Enlisted] Error in BanditEncounterPatch: {ex.Message}", Colors.Red));
                return true;
            }
        }

        /// <summary>
        /// Helper method to get a safe party name for display
        /// </summary>
        public static string GetPartyName(PartyBase party)
        {
            try
            {
                if (party?.Name != null)
                    return party.Name.ToString();
                if (party?.MobileParty?.Name != null)
                    return party.MobileParty.Name.ToString();
                if (party?.MobileParty?.LeaderHero != null)
                    return party.MobileParty.LeaderHero.Name?.ToString() ?? "Unknown Hero";
                return "Unknown party";
            }
            catch
            {
                return "Unknown party";
            }
        }

        /// <summary>
        /// Comprehensive check for hostile parties.
        /// </summary>
        public static bool IsHostileToEnlistedPlayer(PartyBase party, Hero commander)
        {
            if (party?.MobileParty == null) return false;

            // Allow encounters with our own commander (join battles, etc.)
            if (commander != null && party.MobileParty == commander.PartyBelongedTo)
                return false;

            // Allow encounters with parties of our commander's faction
            if (commander?.MapFaction != null && party.MapFaction == commander.MapFaction)
                return false;

            var mobileParty = party.MobileParty;

            // 1. BANDITS - Always hostile when enlisted
            if (mobileParty.IsBandit || (party.MapFaction != null && party.MapFaction.IsBanditFaction))
                return true;

            // 2. LOOTERS - Special case, often not properly flagged as bandits
            if (mobileParty.StringId != null && 
                (mobileParty.StringId.Contains("looter") || 
                 mobileParty.StringId.Contains("bandit") ||
                 mobileParty.StringId.ToLower().Contains("desert_bandit") ||
                 mobileParty.StringId.ToLower().Contains("steppe_bandit") ||
                 mobileParty.StringId.ToLower().Contains("sea_raider") ||
                 mobileParty.StringId.ToLower().Contains("forest_bandit") ||
                 mobileParty.StringId.ToLower().Contains("mountain_bandit")))
                return true;

            // 3. ENEMY FACTIONS - At war with our commander's faction
            if (commander?.MapFaction != null && party.MapFaction != null)
            {
                return FactionManager.IsAtWarAgainstFaction(party.MapFaction, commander.MapFaction);
            }

            // 4. DEFAULT - If we can't determine faction relationships, be safe
            return false;
        }
    }

    /// <summary>
    /// LEGACY: This patch is now mostly redundant due to the IsActive = false approach.
    /// Kept for compatibility and as additional backup protection.
    /// </summary>
    [HarmonyPatch(typeof(MergePartiesAction), "Apply")]
    public static class MergePartiesActionPatch
    {
        /// <summary>
        /// Legacy prefix patch - now backup protection only.
        /// </summary>
        public static bool Prefix(PartyBase majorParty, PartyBase joinedParty)
        {
            try
            {
                // Get enlistment status first
                var enlistmentBehavior = EnlistmentBehavior.Instance;
                if (enlistmentBehavior == null || !enlistmentBehavior.IsEnlisted) 
                    return true; // Not enlisted, allow all merges

                // Only care about merges involving the main party
                bool isMainPartyInvolved = (majorParty == PartyBase.MainParty || joinedParty == PartyBase.MainParty);
                if (!isMainPartyInvolved) 
                    return true; // Main party not involved, allow merge

                // Determine who is the "attacker" in this merge scenario
                PartyBase hostileParty = null;
                if (majorParty == PartyBase.MainParty)
                {
                    hostileParty = joinedParty;
                }
                else if (joinedParty == PartyBase.MainParty)
                {
                    hostileParty = majorParty;
                }

                if (hostileParty != null && BanditEncounterPatch.IsHostileToEnlistedPlayer(hostileParty, enlistmentBehavior.Commander))
                {
                    string hostileName = BanditEncounterPatch.GetPartyName(hostileParty);
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"[Enlisted] {hostileName} avoids confronting you while you serve with {enlistmentBehavior.Commander?.Name}'s army."));
                    return false; // Cancel the merge (which would have led to combat)
                }

                // Allow the merge to proceed (friendly or allied parties)
                return true;
            }
            catch (System.Exception ex)
            {
                // If anything goes wrong, allow the merge to proceed to avoid breaking the game
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[Enlisted] Error in MergePartiesActionPatch: {ex.Message}", Colors.Red));
                return true;
            }
        }
    }

    /// <summary>
    /// LEGACY: Secondary protection layer - now backup only.
    /// </summary>
    [HarmonyPatch(typeof(EncounterManager), "RestartPlayerEncounter")]
    public static class RestartEncounterPatch
    {
        public static bool Prefix(PartyBase attackerParty, PartyBase defenderParty)
        {
            try
            {
                var enlistmentBehavior = EnlistmentBehavior.Instance;
                if (enlistmentBehavior == null || !enlistmentBehavior.IsEnlisted) 
                    return true;

                // Use the same logic as the main patch
                if (defenderParty == PartyBase.MainParty && BanditEncounterPatch.IsHostileToEnlistedPlayer(attackerParty, enlistmentBehavior.Commander))
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"[Enlisted] Encounter restart blocked while serving {enlistmentBehavior.Commander?.Name}."));
                    return false;
                }

                return true;
            }
            catch (System.Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[Enlisted] Error in RestartEncounterPatch: {ex.Message}", Colors.Red));
                return true;
            }
        }
    }
}