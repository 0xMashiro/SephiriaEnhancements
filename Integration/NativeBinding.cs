using System;
using System.Reflection;
using HarmonyLib;

namespace SephiriaEnhancements.Integration
{
    internal static class NativeBinding
    {
        internal static T Method<T>(Type owner, string name) where T : Delegate
        {
            try
            {
                var method = owner.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method == null) throw new MissingMethodException();
                return (T)method.CreateDelegate(typeof(T));
            }
            catch (Exception exception) { throw new NativeBindingException(owner, name, exception); }
        }

        internal static AccessTools.FieldRef<T, F> Field<T, F>(string name) where T : class
        {
            try { return AccessTools.FieldRefAccess<T, F>(name); }
            catch (Exception exception) { throw new NativeBindingException(typeof(T), name, exception); }
        }
    }

    internal sealed class NativeBindingException : Exception
    {
        // Supplied only by binding call sites, never derived from exception messages.
        internal Type Owner { get; }
        internal string MemberName { get; }

        internal NativeBindingException(Type owner, string memberName, Exception cause)
            : base("Native member binding failed.", cause)
        {
            Owner = owner;
            MemberName = memberName;
        }
    }
}
