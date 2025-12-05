using System;

namespace Enlisted.Mod.Core.Util
{
    /// <summary>
    /// Marks a method, property, or class as being used implicitly via reflection,
    /// data binding, or other mechanisms that static analysis cannot detect.
    /// 
    /// Primary use case: TaleWorlds Gauntlet UI command binding (e.g., Command.Click="ExecuteXxx")
    /// where methods are invoked through XML data binding at runtime.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Class | 
        AttributeTargets.Constructor | AttributeTargets.Field,
        AllowMultiple = false,
        Inherited = false)]
    public sealed class UsedImplicitlyAttribute : Attribute
    {
        /// <summary>
        /// Optional description of how the member is used implicitly.
        /// </summary>
        public string Reason { get; }

        public UsedImplicitlyAttribute()
        {
        }

        public UsedImplicitlyAttribute(string reason)
        {
            Reason = reason;
        }
    }
}

