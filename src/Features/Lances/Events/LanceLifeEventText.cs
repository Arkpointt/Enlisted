using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Lances.Text;
using Enlisted.Mod.Core.Util;
using TaleWorlds.Localization;

namespace Enlisted.Features.Lances.Events
{
    internal static class LanceLifeEventText
    {
        public static string Resolve(string textId, string fallbackText, string defaultText, EnlistmentBehavior enlistment)
        {
            // Always process through TextObject so common placeholders like {LANCE_LEADER_SHORT}
            // resolve even when we're using authoring fallbacks (no localization id yet).
            // The {=id} prefix is only added when a textId is provided.

            var raw = !string.IsNullOrWhiteSpace(fallbackText) ? fallbackText : (defaultText ?? string.Empty);
            TextObject t;

            if (!string.IsNullOrWhiteSpace(textId))
            {
                t = new TextObject("{=" + textId + "}" + raw);
            }
            else
            {
                t = new TextObject(raw);
            }

            LanceLifeTextVariables.ApplyCommon(t, enlistment);
            return UiTextSanitizer.Normalize(t.ToString());
        }
    }
}


