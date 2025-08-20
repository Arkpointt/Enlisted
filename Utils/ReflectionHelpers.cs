using System;
using System.Reflection;
using TaleWorlds.CampaignSystem.Party;

namespace Enlisted.Utils
{
    /// <summary>
    /// Minimal reflection helpers for API compatibility.
    /// Contains only essential methods without fallbacks.
    /// </summary>
    public static class ReflectionHelpers
    {
        /// <summary>
        /// Gets a method from a type via reflection.
        /// </summary>
        public static MethodInfo GetMethod(Type type, string methodName)
        {
            return type?.GetMethod(methodName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        /// <summary>
        /// Gets a type from an assembly by name.
        /// </summary>
        public static Type GetType(Assembly assembly, string typeName)
        {
            return assembly?.GetType(typeName);
        }
    }
}