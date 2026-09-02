using System;
using System.Collections.Generic;
using HarmonyLib;

namespace SephiriaEnhancements.Runtime.GameBridge
{
    internal static class NativeContractProbe
    {
        internal static void RequireMethod(Type type, string name,
            ICollection<string> missing, params Type[] arguments)
        {
            if (AccessTools.Method(type, name, arguments) == null)
                missing.Add(type.Name + "." + name);
        }

        internal static void RequireField(Type type, string name,
            ICollection<string> missing)
        {
            if (AccessTools.Field(type, name) == null)
                missing.Add(type.Name + "." + name);
        }
    }
}
