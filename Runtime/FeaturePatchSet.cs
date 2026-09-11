#nullable disable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace SephiriaEnhancements.Runtime
{
    internal sealed class FeaturePatchSet
    {
        private readonly string ownerPrefix;
        private readonly Dictionary<FeatureId, Harmony> owners = new();

        internal FeaturePatchSet(string ownerPrefix) => this.ownerPrefix = ownerPrefix;

        internal bool Install(FeatureId feature, Type patchType)
        {
            if (!FeatureFailure.IsAvailable(feature)) return false;
            if (!owners.TryGetValue(feature, out Harmony owner))
                owners.Add(feature, owner = new Harmony(ownerPrefix + "." + feature));
            try
            {
                RuntimeHelpers.RunClassConstructor(patchType.TypeHandle);
                owner.CreateClassProcessor(patchType).Patch();
                return true;
            }
            catch (Exception exception)
            {
                FeatureFailure.Disable(feature, exception);
                // Includes patches installed before this call and a partly installed class.
                Remove(feature);
                throw;
            }
        }

        internal void Remove(FeatureId feature)
        {
            if (!owners.TryGetValue(feature, out Harmony owner)) return;
            owner.UnpatchAll(owner.Id);
            owners.Remove(feature);
        }
    }
}
