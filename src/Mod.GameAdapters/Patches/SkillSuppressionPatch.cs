using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Suppresses tactics and leadership skill XP gain during battles for enlisted soldiers.
    /// As regular soldiers, enlisted players should not gain command/leadership experience from battle participation
    /// since they are not making tactical decisions or leading troops - that's the commander's role.
    /// </summary>
    // ReSharper disable once UnusedType.Global - Harmony patch class discovered via reflection
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Harmony patch class discovered via reflection")]
    [HarmonyPatch(typeof(HeroDeveloper), nameof(HeroDeveloper.AddSkillXp), typeof(SkillObject), typeof(float), typeof(bool), typeof(bool))]
    public class SkillSuppressionPatch
    {
        /// <summary>
        /// Prefix that runs before AddSkillXp. Returns false to prevent tactics and leadership skill XP when player is enlisted.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Called by Harmony via reflection")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Parameters required to match Harmony patch signature")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony convention: __instance is a special injected parameter")]
        private static bool Prefix(HeroDeveloper __instance, SkillObject skill, float rawXp, bool isAffectedByFocusFactor, bool shouldNotify)
        {
            try
            {
                if (!EnlistedActivation.EnsureActive())
                {
                    return true;
                }

                // Skip during character creation - use safe guard to prevent crashes
                if (!CampaignSafetyGuard.IsCampaignReady)
                {
                    return true; // Allow normal skill assignment
                }
                
                // Only suppress for the main hero when enlisted
                var mainHero = CampaignSafetyGuard.SafeMainHero;
                if (__instance?.Hero != mainHero || mainHero == null)
                {
                    return true; // Not the player - allow normal behavior
                }

                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    return true; // Not enlisted - allow normal behavior
                }

                // Suppress tactics and leadership skill XP for enlisted soldiers during battles
                // These are command/leadership skills - regular soldiers don't make tactical decisions or lead troops
                // However, allow XP from duties/professions (awarded during campaign layer, not in missions)
                if ((skill == DefaultSkills.Tactics || skill == DefaultSkills.Leadership) && rawXp > 0f)
                {
                    // Check if we're in a battle mission - if so, suppress
                    // If Mission.Current is null, we're in campaign layer (duties, etc.) - allow XP
                    if (Mission.Current != null)
                    {
                        // Suppress command skill XP during battles - return false to skip original method
                        ModLogger.LogOnce("skill_suppression_active", "XP", $"Suppressing {skill.Name} XP during battles - enlisted soldiers don't gain command skills from combat");
                        return false;
                    }
                    // Not in a mission - this is likely from duties/professions, allow it
                }

                // Allow all other skills to gain XP normally
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error("SkillSuppression", $"Error in skill suppression patch: {ex.Message}");
                return true; // Fail open - allow normal behavior on error
            }
        }
    }
}

