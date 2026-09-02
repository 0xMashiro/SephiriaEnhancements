using SephiriaEnhancements.Runtime.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using SephiriaEnhancements.Integration;

namespace SephiriaEnhancements.Runtime.GameBridge.Inventory
{
    internal static class InventorySnapshotReader
    {
        // Sephiria's native API calls player-facing artifacts "Charm". Keep
        // Charm_* symbols in this adapter and map them to Artifact snapshots.
        internal static bool TryCaptureLocal(out InventorySnapshot snapshot)
        {
            return TryCaptureLocal(null, out snapshot);
        }

        internal static bool TryCaptureLocal(InventoryCatalogSnapshot catalog,
            out InventorySnapshot snapshot)
        {
            PlayerAvatar player = CombatManager.Instance?.CurrentPlayer;
            if (player == null || !LocalPlayerResolver.IsLocal(player))
            {
                player = GameCamera.Instance?.Observer;
            }

            if (player == null || !LocalPlayerResolver.IsLocal(player))
            {
                snapshot = null;
                return false;
            }

            return TryCapture(player.Inventory, out snapshot,
                CaptureNativePreset(catalog), catalog);
        }

        internal static bool TryCapture(GridInventory inventory,
            out InventorySnapshot snapshot,
            NativePresetSnapshot nativePreset = null,
            InventoryCatalogSnapshot catalog = null,
            TabletProjectionReader tabletProjectionReader = null,
            BuildIntentSnapshot buildIntent = null)
        {
            if (inventory == null || inventory.Width <= 0 ||
                inventory.CurrentInventoryStorage < 0)
            {
                snapshot = null;
                return false;
            }

            int width = inventory.Width;
            int storage = inventory.CurrentInventoryStorage;
            var cells = new InventoryCellSnapshot[storage];
            var items = new List<InventoryItemSnapshot>(storage);

            for (int index = 0; index < storage; index++)
            {
                ItemPosition position = inventory.IdxToPos(index);
                NewItemOwnInstance item = inventory.FindItem(position);
                if (item != null)
                {
                    items.Add(CaptureItem(item, inventory, position, index,
                        tabletProjectionReader));
                }
            }

            InventoryItemSnapshot[] itemSnapshots = items.ToArray();
            InventoryEvaluationOrderTraceSignal.TryGet(inventory,
                out InventoryEvaluationOrderSnapshot evaluationOrder);
            InventoryKnownCellContributions[] contributions =
                CaptureContributions(inventory, itemSnapshots, width, storage);
            var artifactsByCell = itemSnapshots.Where(item =>
                    item.Artifact != null)
                .ToDictionary(item => item.CellIndex);
            for (int index = 0; index < storage; index++)
            {
                ItemPosition position = inventory.IdxToPos(index);
                int level = GetValue(inventory.levelMatrix, position);
                int maximumLevel = GetValue(inventory.maxLevelMatrix, position);
                int temporaryLevel = GetValue(inventory.dungeonTempLevels,
                    position);
                int levelMultiplier = GetValue(inventory.multiplyLevelMatrix,
                    position);
                int disableCount = GetValue(inventory.disableMatrix, position);
                int criteriaBypassCount = GetValue(
                    inventory.ignoreCriteriaMatrix, position);
                InventoryBaselineInference.TryInfer(level, maximumLevel,
                    temporaryLevel, levelMultiplier, disableCount,
                    criteriaBypassCount, inventory.enableCharmEffects,
                    artifactsByCell.ContainsKey(index), contributions[index],
                    out InventoryCellSettlementSnapshot settlement);
                cells[index] = new InventoryCellSnapshot(index, position.x,
                    position.y, level, maximumLevel, temporaryLevel,
                    levelMultiplier, disableCount, criteriaBypassCount,
                    inventory.mysticPositions.Contains(position), settlement);
            }

            bool suppressDuplicateComboEntities = false;
            if (DungeonManager.Instance != null &&
                DungeonManager.Instance.hardModeEnvironment.TryGetValue(
                    "OVERLAPITEMCOMBO", out int overlapItemCombo))
            {
                suppressDuplicateComboEntities = overlapItemCombo > 0;
            }

            int uniquePairComboMode = 0;
            int unlimitedComboStatValue = 0;
            try
            {
                uniquePairComboMode = KeywordDatabase.GetConstValue(
                    "allowUniquePairIncreaseCombo");
                unlimitedComboStatValue = inventory.UnitAvatar == null
                    ? 0
                    : inventory.UnitAvatar.GetCustomStatUnsafe("UNLIMITEDCOMBO");
            }
            catch (Exception)
            {
                // The keyword database may not be initialized in non-run scenes.
            }

            snapshot = new InventorySnapshot(width, storage, cells, itemSnapshots,
                inventory.enableCharmEffects, inventory.globalActiveValue, nativePreset,
                CaptureComboCategories(inventory, nativePreset, itemSnapshots,
                    suppressDuplicateComboEntities, catalog,
                    unlimitedComboStatValue),
                suppressDuplicateComboEntities, uniquePairComboMode,
                buildIntent,
                unlimitedComboStatValue: unlimitedComboStatValue,
                evaluationOrder: evaluationOrder,
                fixedTabletSources: CaptureFixedTabletSources(inventory,
                    tabletProjectionReader),
                arrangementBonusesEnabled: ReadArrangementBonusEnabled());
            return true;
        }

        private static bool ReadArrangementBonusEnabled()
        {
            try
            {
                return GridInventory.ArrangementBonusEnabled();
            }
            catch (Exception)
            {
                // Unknown is treated as enabled so candidate evaluation fails closed.
                return true;
            }
        }

        private static FixedTabletSourceSnapshot[] CaptureFixedTabletSources(
            GridInventory inventory, TabletProjectionReader projectionReader)
        {
            var result = new List<FixedTabletSourceSnapshot>(
                inventory.engravings.Count);
            foreach (StoneTablet tablet in inventory.engravings)
            {
                if (tablet == null)
                {
                    continue;
                }
                try
                {
                    string condition = tablet.GetConditionQuery(tablet.instanceID);
                    string effect = tablet.GetQuery(tablet.instanceID);
                    int cell = tablet.xIdx + tablet.yIdx * inventory.Width;
                    TabletRotationProjectionSnapshot projection =
                        projectionReader?.CaptureAllRotations(condition, effect,
                            inventory.Width, inventory.Height,
                            inventory.CurrentInventoryStorage, tablet.xIdx,
                            tablet.yIdx)
                        ?.FirstOrDefault(value => value.Rotation ==
                            tablet.rotation);
                    result.Add(new FixedTabletSourceSnapshot(tablet.instanceID,
                        tablet.entityID, cell, tablet.rotation, tablet.IsApplied,
                        projection));
                }
                catch (Exception)
                {
                    result.Add(new FixedTabletSourceSnapshot(tablet.instanceID,
                        tablet.entityID, -1, tablet.rotation, tablet.IsApplied,
                        null));
                }
            }
            return result.ToArray();
        }

        private static InventoryKnownCellContributions[] CaptureContributions(
            GridInventory inventory, InventoryItemSnapshot[] items, int width,
            int storage)
        {
            var mutable = new CellContributions[storage];
            foreach (InventoryItemSnapshot item in items)
            {
                if (item.Artifact != null && item.CellIndex >= 0 &&
                    item.CellIndex < mutable.Length)
                {
                    mutable[item.CellIndex].EnchantLevel += item.Artifact.Enchant;
                }
            }

            // FixedEngraving is server-only native state. Its cell-fixed effects
            // intentionally remain in the locally inferred baseline so clients
            // never depend on a host-side collection.

            foreach (StoneTablet tablet in inventory.stoneTablets.Values)
            {
                AddTablet(tablet, width, storage, mutable);
            }
            foreach (StoneTablet engraving in inventory.engravings)
            {
                AddTablet(engraving, width, storage, mutable);
            }
            var result = new InventoryKnownCellContributions[storage];
            for (int index = 0; index < storage; index++)
            {
                CellContributions value = mutable[index];
                result[index] = new InventoryKnownCellContributions(
                    value.EnchantLevel, 0, 0, 0, 0, value.TabletLevel,
                    value.TabletDisableCount,
                    value.TabletCriteriaBypassCount,
                    value.TabletLevelMultiplier);
            }
            return result;
        }

        private static void AddTablet(StoneTablet tablet, int width, int storage,
            CellContributions[] result)
        {
            if (tablet == null || !tablet.IsApplied)
            {
                return;
            }

            foreach (StoneTablet.AdditionEffectData effect in tablet.EffectRange)
            {
                int index = effect.position.x + effect.position.y * width;
                if (effect.position.x < 0 || effect.position.x >= width ||
                    effect.position.y < 0 || index < 0 || index >= storage)
                {
                    continue;
                }

                switch (effect.effectType)
                {
                    case StoneTablet.EffectType.IncreaseConstLevel:
                        result[index].TabletLevel += effect.levelParam;
                        break;
                    case StoneTablet.EffectType.Disable:
                        result[index].TabletDisableCount++;
                        break;
                    case StoneTablet.EffectType.IgnoreCriteria:
                        result[index].TabletCriteriaBypassCount++;
                        break;
                    case StoneTablet.EffectType.MultiplyConstLevel:
                        result[index].TabletLevelMultiplier += effect.levelParam;
                        break;
                }
            }
        }

        private struct CellContributions
        {
            internal int EnchantLevel;
            internal int TabletLevel;
            internal int TabletDisableCount;
            internal int TabletCriteriaBypassCount;
            internal int TabletLevelMultiplier;
        }

        private static InventoryItemSnapshot CaptureItem(NewItemOwnInstance item,
            GridInventory inventory, ItemPosition position, int cellIndex,
            TabletProjectionReader tabletProjectionReader)
        {
            ItemEntity entity = item.Entity;
            Charm_Basic charm = item.Charm;
            StoneTablet tablet = item.StoneTablet;
            InventoryItemKind kind = GetKind(charm, tablet);

            return new InventoryItemSnapshot(
                item.InstanceID,
                item.EntityID,
                item.Quantity,
                cellIndex,
                position.x,
                position.y,
                entity?.Name,
                entity?.aName?.key,
                entity?.type.ToString(),
                entity?.rarity.ToString(),
                ToArray(entity?.categories),
                kind,
                CaptureArtifact(item, inventory, position, charm, entity),
                CaptureStoneTablet(item, tablet, inventory, position,
                    tabletProjectionReader));
        }

        private static ArtifactSnapshot CaptureArtifact(NewItemOwnInstance item,
            GridInventory inventory, ItemPosition position, Charm_Basic charm,
            ItemEntity entity)
        {
            if (charm == null)
            {
                return null;
            }

            int enchant = 0;
            if (DungeonManager.Instance != null)
            {
                int.TryParse(DungeonManager.Instance.GetGlobalItemStatValue(
                    item.InstanceID, "Enchant"), out enchant);
            }

            bool weaponCompatible = !charm.isWeaponRelatedCharm ||
                charm.WeaponController?.currentWeapon?.weaponType == charm.relatedWeapon;
            bool attackable = TryIsAttackable(charm);
            MagicSnapshot magic = CaptureMagic(charm as Charm_Magic);

            return new ArtifactSnapshot(
                charm.DisplayedLevel,
                charm.maxLevel,
                enchant,
                charm.EffectEnabledLevel,
                charm.limitedEffectEnabledLevel,
                charm.IsEffectEnabled,
                charm.IsPenaltyEnabled,
                charm.isWeaponRelatedCharm,
                charm.isWeaponRelatedCharm ? charm.relatedWeapon.ToString() : string.Empty,
                weaponCompatible,
                charm.isUniqueEffect,
                charm.IsUniqueEffectRegistered,
                charm.Order.ToString(),
                CaptureCriteria(item, inventory, position, charm),
                TryCategories(() => charm.GetItemCategory()),
                TryCategories(() => charm.GetPossibleCategory(entity)),
                attackable,
                magic,
                CaptureCategoryRule(charm));
        }

        private static ArtifactCategoryRuleSnapshot CaptureCategoryRule(
            Charm_Basic charm)
        {
            if (charm is Charm_3Elemental_ByRow row)
            {
                return new ArtifactCategoryRuleSnapshot(
                    ArtifactCategoryRuleKind.RowModulo,
                    row.lineCategory?.ToArray());
            }
            if (charm is Charm_UpCharmDamage dependency)
            {
                return new ArtifactCategoryRuleSnapshot(
                    ArtifactCategoryRuleKind.DependencyTarget,
                    targetX: dependency.xOffset,
                    targetY: dependency.yOffset);
            }
            if (charm is Charm_WhitePaper paper)
            {
                ItemPosition origin = new ItemPosition(charm.xIdx, charm.yIdx);
                InventoryOffsetSnapshot[] offsets;
                try
                {
                    offsets = paper.AllPossiblePositions()
                        .Select(position => new InventoryOffsetSnapshot(
                            position.x - origin.x, position.y - origin.y))
                        .ToArray();
                }
                catch (Exception)
                {
                    offsets = Array.Empty<InventoryOffsetSnapshot>();
                }
                return new ArtifactCategoryRuleSnapshot(
                    ArtifactCategoryRuleKind.NeighborMatch,
                    neighborOffsets: offsets, match: paper.match);
            }
            return ArtifactCategoryRuleSnapshot.Static;
        }

        private static CriteriaSnapshot CaptureCriteria(NewItemOwnInstance item,
            GridInventory inventory, ItemPosition position, Charm_Basic charm)
        {
            CharmActivateCriteria criteria = charm.criteria;
            if (criteria == null)
            {
                return new CriteriaSnapshot(ArtifactActivationConditionKind.None,
                    CriteriaEvaluationState.NotApplicable,
                    CriteriaEvaluationState.NotApplicable);
            }

            return new CriteriaSnapshot(MapActivationCondition(criteria),
                Evaluate(() => criteria.GetCriteria(charm)),
                Evaluate(() => criteria.IsActivePosition(item, inventory, position)));
        }

        private static ArtifactActivationConditionKind MapActivationCondition(
            CharmActivateCriteria criteria)
        {
            // Native runtime class names are intentionally confined to this game API
            // boundary; core inventory code consumes the domain condition enum.
            switch (criteria.GetType().Name)
            {
                case "CharmActivateCriteria_TopInInventory":
                    return ArtifactActivationConditionKind.TopRow;
                case "CharmActivateCriteria_BottomInInventory":
                    return ArtifactActivationConditionKind.BottomRow;
                case "CharmActivateCriteria_SideEnd":
                    return ArtifactActivationConditionKind.SideEdge;
                case "CharmActivateCriteria_Inside":
                    return ArtifactActivationConditionKind.Interior;
                case "CharmActivateCriteria_Outlined":
                    return ArtifactActivationConditionKind.Border;
                case "CharmActivateCriteria_BothSidesAreEmpty":
                    return ArtifactActivationConditionKind.BothSidesEmpty;
                case "CharmActivateCriteria_BothSideCharm":
                    return ArtifactActivationConditionKind.BothSidesArtifacts;
                case "CharmActivateCriteria_NeighborsAreFull":
                    return ArtifactActivationConditionKind.AllNeighborsOccupied;
                case "CharmActivateCriteria_Near8MagicBook":
                    return ArtifactActivationConditionKind.AdjacentMagicArtifact;
                case "CharmActivateCriteria_FullHP":
                    return ArtifactActivationConditionKind.FullHealth;
                default:
                    return ArtifactActivationConditionKind.Unknown;
            }
        }

        private static MagicSnapshot CaptureMagic(Charm_Magic magic)
        {
            ActiveSkillEntity skill = magic?.ContainedMagic;
            if (skill == null)
            {
                return null;
            }

            int maxMpCost = skill.mpCostsByLevel == null ||
                skill.mpCostsByLevel.Length == 0
                ? 0
                : skill.mpCostsByLevel.Max();
            bool bolt = skill.magicPrefab != null &&
                skill.magicPrefab.GetComponent<ActiveSkill>() is ActiveSkill_Bolt;

            return new MagicSnapshot(skill.id, skill.aName?.key, skill.cooldownTime,
                maxMpCost, skill.tags,
                skill.magicClasses?.Select(value => value.ToString()).ToArray(), bolt);
        }

        private static StoneTabletSnapshot CaptureStoneTablet(
            NewItemOwnInstance item,
            StoneTablet tablet, GridInventory inventory, ItemPosition position,
            TabletProjectionReader tabletProjectionReader)
        {
            if (tablet == null)
            {
                return null;
            }

            string conditionQuery = string.Empty;
            string effectQuery = string.Empty;
            try
            {
                conditionQuery = tablet.GetConditionQuery(item.InstanceID);
                effectQuery = tablet.GetQuery(item.InstanceID);
            }
            catch (Exception)
            {
                // A custom tablet can exist before DungeonManager has its query data.
            }

            return new StoneTabletSnapshot(tablet.rotation, tablet.isRotatable,
                tablet.isCustomTablet, tablet.IsApplied,
                tablet.includeConditionCriteriaToMinMaxGrid,
                conditionQuery, effectQuery,
                tabletProjectionReader?.CaptureAllRotations(conditionQuery,
                    effectQuery, inventory.Width, inventory.Height,
                    inventory.CurrentInventoryStorage, position.x, position.y),
                tabletProjectionReader?.CaptureAllPlacements(conditionQuery,
                    effectQuery, inventory.Width, inventory.Height,
                    inventory.CurrentInventoryStorage));
        }

        internal static NativePresetSnapshot CaptureNativePreset(
            InventoryCatalogSnapshot catalog)
        {
            SaveData save = SaveManager.Current;
            if (save == null)
            {
                return null;
            }

            int slot = save.GetInt("Preset_SelectedSlot", 0);
            string prefix = $"Preset_{slot}_";
            bool enabled = save.HasKey(prefix + "PresetEnabled")
                ? save.GetInt(prefix + "PresetEnabled", 0) != 0
                : save.HasKey(prefix + "StartingWeaponID");
            var favoriteIds = new List<int>();
            var favoriteCategories = new HashSet<string>(StringComparer.Ordinal);

            if (catalog == null)
            {
                if (!InventoryCatalogReader.TryCapture(null, out catalog))
                {
                    return null;
                }
            }

            string nativeArtifactType = EItemType.Charm.ToString();
            foreach (InventoryCatalogItemSnapshot item in catalog.Items)
            {
                if (item.NativeItemTypeName != nativeArtifactType ||
                    !save.GetBool(prefix + "Item_Favorite_" + item.EntityId,
                        fallback: false))
                {
                    continue;
                }

                favoriteIds.Add(item.EntityId);
                foreach (string category in item.PossibleCategories)
                {
                    favoriteCategories.Add(category);
                }
            }

            favoriteIds.Sort();
            string[] categories = favoriteCategories.OrderBy(value => value,
                StringComparer.Ordinal).ToArray();
            string costumeId = save.GetString(prefix + "PlayerCostume",
                "PinkRabbit");
            var passives = new List<NativePresetPassiveSnapshot>();
            foreach (PassiveEntity passive in PassiveDatabase.GetAll())
            {
                int points = save.GetInt(prefix + "PassivePoint_" + passive.id, 0);
                if (points > 0)
                {
                    passives.Add(new NativePresetPassiveSnapshot(passive.id, points));
                }
            }
            passives.Sort((left, right) => left.PassiveId.CompareTo(
                right.PassiveId));

            int pocketCount = BoundedCount(save.GetInt(
                prefix + "DimensionPocketCount", 0));
            var pocketItems = new NativePresetPocketItemSnapshot[pocketCount];
            for (int index = 0; index < pocketCount; index++)
            {
                string itemPrefix = prefix + "DimensionPocket" + index + "_";
                pocketItems[index] = new NativePresetPocketItemSnapshot(
                    save.GetInt(itemPrefix + "InstanceID", -1),
                    save.GetInt(itemPrefix + "EntityID", -1),
                    save.GetInt(itemPrefix + "Quantity", 1));
            }

            int fruitCount = BoundedCount(save.GetInt(
                prefix + "FruitSkewer_FruitCount", 0));
            var fruits = new NativePresetFruitSnapshot[fruitCount];
            for (int index = 0; index < fruitCount; index++)
            {
                string fruitPrefix = prefix + "FruitSkewer_Fruit" + index + "_";
                fruits[index] = new NativePresetFruitSnapshot(
                    save.GetString(fruitPrefix + "Category", string.Empty),
                    save.GetInt(fruitPrefix + "Value", 0));
            }

            return new NativePresetSnapshot(slot, enabled,
                save.GetString(prefix + "PresetName", string.Empty),
                save.GetInt(prefix + "StartingWeaponID", 0),
                costumeId, favoriteIds.ToArray(), categories,
                save.GetString(prefix + "PlayerCostume_CurrentSkin_" + costumeId,
                    string.Empty), passives.ToArray(), pocketItems, fruits,
                save.GetInt(prefix + "FruitSkewer_AdaptiveItemDropBonus", 1));
        }

        private static ComboCategorySnapshot[] CaptureComboCategories(
            GridInventory inventory, NativePresetSnapshot nativePreset,
            InventoryItemSnapshot[] items, bool suppressDuplicateEntities,
            InventoryCatalogSnapshot catalog, int unlimitedComboStatValue)
        {
            var result = new List<ComboCategorySnapshot>();
            Dictionary<string, int> artifactCategoryCounts = CountArtifactCategories(items,
                suppressDuplicateEntities);
            var favoriteCategories = new HashSet<string>(
                nativePreset?.FavoriteCategories ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            if (catalog == null &&
                !InventoryCatalogReader.TryCapture(inventory.UnitAvatar, out catalog))
            {
                return Array.Empty<ComboCategorySnapshot>();
            }

            foreach (InventoryCategoryCatalogSnapshot category in catalog.Categories)
            {
                int currentCount = GetValue(inventory.currentSetEffectCount,
                    category.CategoryId);
                int artifactCategoryCount = artifactCategoryCounts.TryGetValue(
                    category.CategoryId,
                    out int value) ? value : 0;
                int bonusCount = GetValue(inventory.bonusComboCount,
                    category.CategoryId);
                int inferredUniquePairCount = currentCount - artifactCategoryCount -
                    bonusCount;
                int highestReachedThreshold = category.ComboThresholds
                    .Where(threshold => threshold <= currentCount)
                    .DefaultIfEmpty(0)
                    .Max();
                int unlimitedComboExtraCount = unlimitedComboStatValue > 0 &&
                    category.HighestComboCount > 0
                    ? Math.Max(0, currentCount - category.HighestComboCount)
                    : 0;

                result.Add(new ComboCategorySnapshot(category.CategoryId, currentCount,
                    GetValue(inventory.currentAppliedSetEffect, category.CategoryId),
                    artifactCategoryCount, bonusCount, inferredUniquePairCount,
                    category.SetThresholds.ToArray(),
                    category.ComboThresholds.ToArray(),
                    favoriteCategories.Contains(category.CategoryId),
                    category.HighestComboCount, highestReachedThreshold,
                    unlimitedComboExtraCount));
            }

            return result.ToArray();
        }

        private static Dictionary<string, int> CountArtifactCategories(
            IEnumerable<InventoryItemSnapshot> items, bool suppressDuplicateEntities)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            var seenEntities = new HashSet<int>();
            foreach (InventoryItemSnapshot item in items)
            {
                if (item.Artifact == null ||
                    (suppressDuplicateEntities && !seenEntities.Add(item.EntityId)))
                {
                    continue;
                }

                foreach (string category in item.Artifact.EffectiveCategories)
                {
                    result[category] = result.TryGetValue(category,
                        out int count) ? count + 1 : 1;
                }
            }

            return result;
        }

        private static InventoryItemKind GetKind(Charm_Basic charm,
            StoneTablet tablet)
        {
            if (charm != null)
            {
                return charm.criteria != null
                    ? InventoryItemKind.RestrictedArtifact
                    : InventoryItemKind.Artifact;
            }

            return tablet != null
                ? InventoryItemKind.StoneTablet
                : InventoryItemKind.Other;
        }

        private static CriteriaEvaluationState Evaluate(Func<bool> evaluate)
        {
            try
            {
                return evaluate()
                    ? CriteriaEvaluationState.Satisfied
                    : CriteriaEvaluationState.Unsatisfied;
            }
            catch (Exception)
            {
                return CriteriaEvaluationState.Unknown;
            }
        }

        private static bool TryIsAttackable(Charm_Basic charm)
        {
            try
            {
                return charm is IAttackableCharm attackable &&
                    attackable.IsAttackableCharm();
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string[] TryCategories(Func<IEnumerable<string>> read)
        {
            try
            {
                return ToArray(read());
            }
            catch (Exception)
            {
                return Array.Empty<string>();
            }
        }

        private static string[] ToArray(IEnumerable<string> values)
        {
            return values?.Where(value => !string.IsNullOrEmpty(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray() ?? Array.Empty<string>();
        }

        private static int GetValue(IDictionary<ItemPosition, int> matrix,
            ItemPosition position)
        {
            return matrix.TryGetValue(position, out int value) ? value : 0;
        }

        private static int GetValue(IDictionary<string, int> dictionary,
            string key)
        {
            return dictionary.TryGetValue(key, out int value) ? value : 0;
        }

        private static int BoundedCount(int value)
        {
            return Math.Max(0, Math.Min(value, 256));
        }
    }
}
