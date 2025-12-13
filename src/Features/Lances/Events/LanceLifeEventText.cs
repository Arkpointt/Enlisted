using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Lances.Text;
using TaleWorlds.Localization;

namespace Enlisted.Features.Lances.Events
{
    internal static class LanceLifeEventText
    {
        public static string Resolve(string textId, string fallbackText, string defaultText, EnlistmentBehavior enlistment)
        {
            if (string.IsNullOrWhiteSpace(textId))
            {
                return !string.IsNullOrWhiteSpace(fallbackText) ? fallbackText : (defaultText ?? string.Empty);
            }

            var embeddedFallback = !string.IsNullOrWhiteSpace(fallbackText) ? fallbackText : (defaultText ?? string.Empty);
            var t = new TextObject("{=" + textId + "}" + embeddedFallback);
            LanceLifeTextVariables.ApplyCommon(t, enlistment);
            return t.ToString();
        }
    }
}


