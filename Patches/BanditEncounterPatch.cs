using System;
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
    /// Backup protection for encounters while enlisted. Primary protection is
    /// making MainParty untargetable by setting MainParty.IsActive = false in the behavior tick.
    /// These patches remain as a safety net only.
    /// </summary>
    [HarmonyPatch(typeof(EncounterManager), "StartPartyEncounter")]
    public static class BanditEncounterPatch
    {
        public static bool Prefix(PartyBase attackerParty, PartyBase defenderParty)
        {
            try
            {
                var enlistmentBehavior = EnlistmentBehavior.Instance;
                if (enlistmentBehavior == null || !enlistmentBehavior.IsEnlisted)
                {
                    return true;
                }

                bool isMainPartyInvolved = (attackerParty == PartyBase.MainParty || defenderParty == PartyBase.MainParty);
                if (!isMainPartyInvolved)
                {
                    return true;
                }

                if (defenderParty == PartyBase.MainParty &&
                    IsHostileToEnlistedPlayer(attackerParty, enlistmentBehavior.Commander))
                {
                    string attackerName = GetPartyName(attackerParty);
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"[Enlisted] {attackerName} avoids attacking you while you serve {enlistmentBehavior.Commander?.Name}."));
                    return false;
                }

                if (attackerParty == PartyBase.MainParty &&
                    IsHostileToEnlistedPlayer(defenderParty, enlistmentBehavior.Commander))
                {
                    string defenderName = GetPartyName(defenderParty);
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"[Enlisted] You cannot attack {defenderName} while serving {enlistmentBehavior.Commander?.Name}."));
                    return false;
                }

                return true;
            }
            catch (System.Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[Enlisted] Error in BanditEncounterPatch: {ex.Message}", Colors.Red));
                return true;
            }
        }

        public static string GetPartyName(PartyBase party)
        {
            try
            {
                if (party?.Name != null)
                {
                    return party.Name.ToString();
                }

                if (party?.MobileParty?.Name != null)
                {
                    return party.MobileParty.Name.ToString();
                }

                if (party?.MobileParty?.LeaderHero != null)
                {
                    return party.MobileParty.LeaderHero.Name?.ToString() ?? "Unknown Hero";
                }

                return "Unknown party";
            }
            catch
            {
                return "Unknown party";
            }
        }

        public static bool IsHostileToEnlistedPlayer(PartyBase party, Hero commander)
        {
            if (party?.MobileParty == null)
            {
                return false;
            }

            if (commander != null && party.MobileParty == commander.PartyBelongedTo)
            {
                return false;
            }

            if (commander?.MapFaction != null && party.MapFaction == commander.MapFaction)
            {
                return false;
            }

            var mobileParty = party.MobileParty;

            if (mobileParty.IsBandit || (party.MapFaction != null && party.MapFaction.IsBanditFaction))
            {
                return true;
            }

            var id = mobileParty.StringId;
            if (!string.IsNullOrEmpty(id) &&
                (id.IndexOf("looter", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 id.IndexOf("bandit", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 id.IndexOf("desert_bandit", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 id.IndexOf("steppe_bandit", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 id.IndexOf("sea_raider", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 id.IndexOf("forest_bandit", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 id.IndexOf("mountain_bandit", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return true;
            }

            if (commander?.MapFaction != null && party.MapFaction != null)
            {
                return FactionManager.IsAtWarAgainstFaction(party.MapFaction, commander.MapFaction);
            }

            return false;
        }
    }

    /// <summary>
    /// Legacy merge patch as another safety net.
    /// </summary>
    [HarmonyPatch(typeof(MergePartiesAction), "Apply")]
    public static class MergePartiesActionPatch
    {
        public static bool Prefix(PartyBase majorParty, PartyBase joinedParty)
        {
            try
            {
                var enlistmentBehavior = EnlistmentBehavior.Instance;
                if (enlistmentBehavior == null || !enlistmentBehavior.IsEnlisted)
                {
                    return true;
                }

                bool isMainPartyInvolved = (majorParty == PartyBase.MainParty || joinedParty == PartyBase.MainParty);
                if (!isMainPartyInvolved)
                {
                    return true;
                }

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
                    return false;
                }

                return true;
            }
            catch (System.Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[Enlisted] Error in MergePartiesActionPatch: {ex.Message}", Colors.Red));
                return true;
            }
        }
    }

    /// <summary>
    /// Legacy restart-encounter safety.
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
                {
                    return true;
                }

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
