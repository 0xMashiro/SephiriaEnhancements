using SephiriaEnhancements.Runtime;
using System;
using System.Collections.Generic;
using HarmonyLib;
using SephiriaEnhancements.Runtime.Execution;
using UnityEngine;
using Mirror;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    // EProceduralMerchantType member names are native API contracts. Domain and
    // player-facing names use Wandering Merchant and Merchant Guild terminology.
    internal static class MerchantGenerationContext
    {
        internal static IDisposable Enter(EProceduralMerchantType merchant)
        {
            return AmbientExecutionContext<Frame>.Enter(new Frame(merchant));
        }

        internal static bool TryGet(out EProceduralMerchantType merchant)
        {
            Frame current = AmbientExecutionContext<Frame>.Current;
            merchant = current?.Merchant ?? EProceduralMerchantType.None;
            return current != null;
        }

        internal static bool TryBeginGeneratedInventoryAdjustment(
            out EProceduralMerchantType merchant)
        {
            Frame current = AmbientExecutionContext<Frame>.Current;
            merchant = current?.Merchant ?? EProceduralMerchantType.None;
            if (current == null || current.InventoryAdjusted) return false;
            current.InventoryAdjusted = true;
            return true;
        }

        private sealed class Frame
        {
            internal Frame(EProceduralMerchantType merchant) => Merchant = merchant;
            internal EProceduralMerchantType Merchant { get; }
            internal bool InventoryAdjusted { get; set; }
        }
    }

    internal static class MerchantRuleActivation
    {
        internal static bool HasOverride(EProceduralMerchantType merchant,
            int participantCount)
        {
            bool stocksParticipantScaledRestorativePotion =
                merchant == EProceduralMerchantType.Vendor ||
                merchant == EProceduralMerchantType.SmallVendor ||
                merchant == EProceduralMerchantType.PotionVendor;
            if (stocksParticipantScaledRestorativePotion &&
                MultiplayerRulesController.TryGetActiveOverride(
                    MultiplayerRuleId.RestorativePotionQuantity, participantCount,
                    out _)) return true;
            if (merchant == EProceduralMerchantType.PotionVendor &&
                MultiplayerRulesController.TryGetActiveOverride(
                    MultiplayerRuleId.RegenerationSamplePotionQuantity,
                    participantCount, out _)) return true;
            if (merchant == EProceduralMerchantType.Vendor)
                return MultiplayerRulesController.TryGetActiveOverride(
                        MultiplayerRuleId.WanderingMerchantArtifactCandidateBonus,
                        participantCount, out _) ||
                    MultiplayerRulesController.TryGetActiveOverride(
                        MultiplayerRuleId.WanderingMerchantTabletCandidateCount,
                        participantCount, out _);
            if (merchant == EProceduralMerchantType.MerchantUnionVendor)
                return MultiplayerRulesController.TryGetActiveOverride(
                        MultiplayerRuleId.MerchantGuildArtifactCandidateBonus,
                        participantCount, out _) ||
                    MultiplayerRulesController.TryGetActiveOverride(
                        MultiplayerRuleId.MerchantGuildTabletCandidateCount,
                        participantCount, out _);
            return false;
        }
    }

    [HarmonyPatch(typeof(UnitAI_NewBasic), nameof(UnitAI_NewBasic.SetSocialID))]
    internal static class MerchantGenerationRuleContextPatch
    {
        private static void Prefix(EProceduralMerchantType merchant, out IDisposable __state)
        {
            __state = default;
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerRules))
            {
                return;
            }

            try
            {
                PrefixCore(merchant, out __state);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerRules, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(EProceduralMerchantType merchant, out IDisposable __state)
        {
            __state = merchant != EProceduralMerchantType.None && MerchantRuleActivation.HasOverride(merchant, ServerParticipantCountReader.Read()) ? MerchantGenerationContext.Enter(merchant) : null;
        }

        private static Exception Finalizer(Exception __exception, IDisposable __state)
        {
            try
            {
                return FinalizerCore(__exception, __state);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerRules, exception);
                return __exception;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Exception FinalizerCore(Exception __exception, IDisposable __state)
        {
            __state?.Dispose();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(UnitAI_NewBasic), "AddTradingItemsToList")]
    internal static class MerchantCandidateRulePatch
    {
        private static void Prefix(ref int charms, ref int stoneTablets)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerRules))
            {
                return;
            }

            var original_charms = charms;
            var original_stoneTablets = stoneTablets;
            try
            {
                PrefixCore(ref charms, ref stoneTablets);
            }
            catch (System.Exception exception)
            {
                charms = original_charms;
                stoneTablets = original_stoneTablets;
                FeatureFailure.Disable(FeatureId.MultiplayerRules, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(ref int charms, ref int stoneTablets)
        {
            if (!MerchantGenerationContext.TryGet(out EProceduralMerchantType merchant))
                return;
            int participantCount = ServerParticipantCountReader.Read();
            // Native SetSocialID derives this bonus from raw server connections.
            int nativeParticipantBonus = NetworkServer.connections.Count - 1;
            if (merchant == EProceduralMerchantType.Vendor)
            {
                if (MultiplayerRulesController.TryGetActiveOverride(MultiplayerRuleId.WanderingMerchantArtifactCandidateBonus, participantCount, out float charmBonus))
                    charms += Mathf.RoundToInt(charmBonus) - nativeParticipantBonus;
                if (MultiplayerRulesController.TryGetActiveOverride(MultiplayerRuleId.WanderingMerchantTabletCandidateCount, participantCount, out float tabletCount))
                    stoneTablets = Mathf.RoundToInt(tabletCount);
            }
            else if (merchant == EProceduralMerchantType.MerchantUnionVendor)
            {
                if (MultiplayerRulesController.TryGetActiveOverride(MultiplayerRuleId.MerchantGuildArtifactCandidateBonus, participantCount, out float charmBonus))
                    charms += Mathf.RoundToInt(charmBonus) - nativeParticipantBonus;
                if (MultiplayerRulesController.TryGetActiveOverride(MultiplayerRuleId.MerchantGuildTabletCandidateCount, participantCount, out float tabletCount))
                    stoneTablets = Mathf.RoundToInt(tabletCount);
            }
        }
    }

    internal static class MerchantInventoryRuleApplier
    {
        // These numeric IDs are native ItemDatabase integration contracts.
        private const int NativeRestorativePotionItemId = 0;
        private const int NativeRegenerationSampleItemId = 37;

        internal static void Apply(ref ItemMetadata[] items)
        {
            if (items == null ||
                !MerchantGenerationContext.TryBeginGeneratedInventoryAdjustment(
                    out EProceduralMerchantType merchant)) return;
            int participantCount = ServerParticipantCountReader.Read();
            var adjustedItems = new List<ItemMetadata>(items.Length);
            for (int index = 0; index < items.Length; index++)
            {
                ItemMetadata item = items[index];
                bool stocksParticipantScaledRestorativePotion =
                    merchant == EProceduralMerchantType.Vendor ||
                    merchant == EProceduralMerchantType.SmallVendor ||
                    merchant == EProceduralMerchantType.PotionVendor;
                if (stocksParticipantScaledRestorativePotion &&
                    item.entityID == NativeRestorativePotionItemId &&
                    MultiplayerRulesController.TryGetActiveOverride(
                        MultiplayerRuleId.RestorativePotionQuantity,
                        participantCount,
                        out float restorativePotionQuantity))
                {
                    item.quantity = (sbyte)Mathf.RoundToInt(
                        restorativePotionQuantity);
                }
                else if (merchant == EProceduralMerchantType.PotionVendor &&
                    item.entityID == NativeRegenerationSampleItemId &&
                    MultiplayerRulesController.TryGetActiveOverride(
                        MultiplayerRuleId.RegenerationSamplePotionQuantity,
                        participantCount,
                        out float regenerationSampleQuantity))
                {
                    item.quantity = (sbyte)Mathf.RoundToInt(
                        regenerationSampleQuantity);
                }
                if (item.quantity > 0) adjustedItems.Add(item);
            }
            items = adjustedItems.ToArray();
        }
    }

    [HarmonyPatch(typeof(Safe), nameof(Safe.GenerateItemInInventory))]
    internal static class SafeMerchantInventoryRulePatch
    {
        private static void Prefix(ref ItemMetadata[] data)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerRules))
            {
                return;
            }

            var original_data = data;
            try
            {
                PrefixCore(ref data);
            }
            catch (System.Exception exception)
            {
                data = original_data;
                FeatureFailure.Disable(FeatureId.MultiplayerRules, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(ref ItemMetadata[] data) => MerchantInventoryRuleApplier.Apply(ref data);
    }

    [HarmonyPatch(typeof(GridInventory), nameof(GridInventory.AddItems),
        new[] { typeof(ItemMetadata[]), typeof(bool), typeof(bool) })]
    internal static class DirectMerchantInventoryRulePatch
    {
        private static void Prefix(ref ItemMetadata[] items)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerRules))
            {
                return;
            }

            var original_items = items;
            try
            {
                PrefixCore(ref items);
            }
            catch (System.Exception exception)
            {
                items = original_items;
                FeatureFailure.Disable(FeatureId.MultiplayerRules, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(ref ItemMetadata[] items) => MerchantInventoryRuleApplier.Apply(ref items);
    }
}
