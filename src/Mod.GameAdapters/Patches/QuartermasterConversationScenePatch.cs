using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Ensures quartermaster conversations use the correct scene (sea/land) based on
    /// the enlisted lord's party position. Without this, QM conversations default to
    /// land scenes even when the party is at sea.
    /// </summary>
    [HarmonyPatch(typeof(ConversationManager), "OpenMapConversation")]
    internal class QuartermasterConversationScenePatch
    {
        [HarmonyPrefix]
        public static void Prefix(
            ref ConversationCharacterData playerCharacterData,
            ref ConversationCharacterData conversationPartnerData)
        {
            try
            {
                // Check if this is a quartermaster conversation
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment == null || !enlistment.IsEnlisted)
                {
                    return;
                }
                
                var qmHero = enlistment.GetOrCreateQuartermaster();
                if (qmHero == null || conversationPartnerData.Character != qmHero.CharacterObject)
                {
                    return; // Not a QM conversation
                }
                
                // QM hero doesn't have its own party, so use the enlisted lord's party
                // This ensures the conversation scene (sea/land/terrain) matches the actual location
                var lordParty = enlistment.EnlistedLord?.PartyBelongedTo;
                if (lordParty != null)
                {
                    conversationPartnerData = new ConversationCharacterData(
                        character: conversationPartnerData.Character,
                        party: lordParty.Party, // Use lord's party instead of QM's (null) party
                        noHorse: conversationPartnerData.NoHorse,
                        noWeapon: conversationPartnerData.NoWeapon,
                        spawnAfterFight: conversationPartnerData.SpawnedAfterFight,
                        isCivilianEquipmentRequiredForLeader: false,
                        isCivilianEquipmentRequiredForBodyGuardCharacters: false,
                        noBodyguards: conversationPartnerData.NoBodyguards
                    );
                    
                    ModLogger.Debug("Interface", 
                        $"QM conversation scene fix: Using lord's party for scene determination");
                }
            }
            catch (System.Exception ex)
            {
                ModLogger.Error("Interface", "Error in QuartermasterConversationScenePatch", ex);
            }
        }
    }
}
