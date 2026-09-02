#nullable disable

using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Runtime.Inventory
{
    internal enum InventoryItemKind
    {
        Other,
        Artifact,
        RestrictedArtifact,
        StoneTablet
    }

    internal enum NativeInventoryItemType
    {
        Unknown,
        Misc,
        ThrowingWeapon,
        Potion,
        Food,
        Scroll,
        Charm,
        StoneTablet,
        Identifiable
    }

    internal enum CriteriaEvaluationState
    {
        NotApplicable,
        Satisfied,
        Unsatisfied,
        Unknown
    }

    internal enum ArtifactActivationConditionKind
    {
        None,
        TopRow,
        BottomRow,
        SideEdge,
        Interior,
        Border,
        BothSidesEmpty,
        BothSidesArtifacts,
        AllNeighborsOccupied,
        AdjacentMagicArtifact,
        FullHealth,
        Unknown
    }

    internal sealed class InventoryCellSnapshot
    {
        internal InventoryCellSnapshot(int index, int x, int y, int level, int maxLevel,
            int temporaryLevel, int levelMultiplier, int disableCount,
            int ignoreCriteriaCount, bool mystic,
            InventoryCellSettlementSnapshot settlement = null)
        {
            Index = index;
            X = x;
            Y = y;
            Level = level;
            MaxLevel = maxLevel;
            TemporaryLevel = temporaryLevel;
            LevelMultiplier = levelMultiplier;
            DisableCount = disableCount;
            IgnoreCriteriaCount = ignoreCriteriaCount;
            Mystic = mystic;
            Settlement = settlement;
        }

        internal int Index { get; }
        internal int X { get; }
        internal int Y { get; }
        internal int Level { get; }
        internal int MaxLevel { get; }
        internal int TemporaryLevel { get; }
        internal int LevelMultiplier { get; }
        internal int DisableCount { get; }
        internal int IgnoreCriteriaCount { get; }
        internal bool Disabled => DisableCount > 0;
        internal bool IgnoresCriteria => IgnoreCriteriaCount > 0;
        internal bool Mystic { get; }
        internal InventoryCellSettlementSnapshot Settlement { get; }
    }

    internal sealed class CriteriaSnapshot
    {
        internal CriteriaSnapshot(ArtifactActivationConditionKind kind,
            CriteriaEvaluationState runtimeState,
            CriteriaEvaluationState positionProjectionState)
        {
            Kind = kind;
            RuntimeState = runtimeState;
            PositionProjectionState = positionProjectionState;
        }

        internal ArtifactActivationConditionKind Kind { get; }
        internal CriteriaEvaluationState RuntimeState { get; }
        internal CriteriaEvaluationState PositionProjectionState { get; }
    }

    internal sealed class MagicSnapshot
    {
        internal MagicSnapshot(int skillId, string nameKey, float cooldown,
            int maxMpCost, string tags, string[] magicClasses, bool bolt)
        {
            SkillId = skillId;
            NameKey = nameKey ?? string.Empty;
            Cooldown = cooldown;
            MaxMpCost = maxMpCost;
            Tags = tags ?? string.Empty;
            MagicClasses = Array.AsReadOnly(magicClasses == null
                ? Array.Empty<string>()
                : (string[])magicClasses.Clone());
            Bolt = bolt;
        }

        internal int SkillId { get; }
        internal string NameKey { get; }
        internal float Cooldown { get; }
        internal int MaxMpCost { get; }
        internal string Tags { get; }
        internal IReadOnlyList<string> MagicClasses { get; }
        internal bool Bolt { get; }
    }

    internal sealed class ArtifactSnapshot
    {
        internal ArtifactSnapshot(int displayedLevel, int maxLevel,
            int enchant, int effectEnabledLevel, int limitedEffectEnabledLevel,
            bool effectEnabled, bool penaltyEnabled, bool weaponRestricted,
            string requiredWeapon, bool weaponCompatible, bool uniqueEffect,
            bool uniqueEffectRegistered, string calculationOrder,
            CriteriaSnapshot criteria, string[] effectiveCategories,
            string[] possibleCategories, bool attackable,
            MagicSnapshot magic,
            ArtifactCategoryRuleSnapshot categoryRule = null)
        {
            DisplayedLevel = displayedLevel;
            MaxLevel = maxLevel;
            Enchant = enchant;
            EffectEnabledLevel = effectEnabledLevel;
            LimitedEffectEnabledLevel = limitedEffectEnabledLevel;
            EffectEnabled = effectEnabled;
            PenaltyEnabled = penaltyEnabled;
            WeaponRestricted = weaponRestricted;
            RequiredWeapon = requiredWeapon ?? string.Empty;
            WeaponCompatible = weaponCompatible;
            UniqueEffect = uniqueEffect;
            UniqueEffectRegistered = uniqueEffectRegistered;
            CalculationOrder = calculationOrder ?? string.Empty;
            Criteria = criteria;
            EffectiveCategories = Array.AsReadOnly(effectiveCategories == null
                ? Array.Empty<string>()
                : (string[])effectiveCategories.Clone());
            PossibleCategories = Array.AsReadOnly(possibleCategories == null
                ? Array.Empty<string>()
                : (string[])possibleCategories.Clone());
            Attackable = attackable;
            Magic = magic;
            CategoryRule = categoryRule ?? ArtifactCategoryRuleSnapshot.Static;
        }

        internal int DisplayedLevel { get; }
        internal int MaxLevel { get; }
        internal int Enchant { get; }
        internal int EffectEnabledLevel { get; }
        internal int LimitedEffectEnabledLevel { get; }
        internal bool EffectEnabled { get; }
        internal bool PenaltyEnabled { get; }
        internal bool WeaponRestricted { get; }
        internal string RequiredWeapon { get; }
        internal bool WeaponCompatible { get; }
        internal bool UniqueEffect { get; }
        internal bool UniqueEffectRegistered { get; }
        internal string CalculationOrder { get; }
        internal CriteriaSnapshot Criteria { get; }
        internal IReadOnlyList<string> EffectiveCategories { get; }
        internal IReadOnlyList<string> PossibleCategories { get; }
        internal bool Attackable { get; }
        internal MagicSnapshot Magic { get; }
        internal ArtifactCategoryRuleSnapshot CategoryRule { get; }
    }

    internal sealed class StoneTabletSnapshot
    {
        internal StoneTabletSnapshot(int rotation, bool rotatable, bool custom,
            bool applied, bool includesCriteriaInMinMaxGrid,
            string conditionQuery, string effectQuery,
            TabletRotationProjectionSnapshot[] rotationProjections = null,
            TabletPlacementProjectionSnapshot[] placementProjections = null)
        {
            Rotation = rotation;
            Rotatable = rotatable;
            Custom = custom;
            Applied = applied;
            IncludesCriteriaInMinMaxGrid = includesCriteriaInMinMaxGrid;
            ConditionQuery = conditionQuery ?? string.Empty;
            EffectQuery = effectQuery ?? string.Empty;
            RotationProjections = Array.AsReadOnly(rotationProjections == null
                ? Array.Empty<TabletRotationProjectionSnapshot>()
                : (TabletRotationProjectionSnapshot[])rotationProjections.Clone());
            PlacementProjections = Array.AsReadOnly(placementProjections == null
                ? Array.Empty<TabletPlacementProjectionSnapshot>()
                : (TabletPlacementProjectionSnapshot[])placementProjections.Clone());
        }

        internal int Rotation { get; }
        internal bool Rotatable { get; }
        internal bool Custom { get; }
        internal bool Applied { get; }
        internal bool IncludesCriteriaInMinMaxGrid { get; }
        internal string ConditionQuery { get; }
        internal string EffectQuery { get; }
        internal IReadOnlyList<TabletRotationProjectionSnapshot> RotationProjections
        { get; }
        internal IReadOnlyList<TabletPlacementProjectionSnapshot>
            PlacementProjections
        { get; }

        internal TabletRotationProjectionSnapshot FindProjection(int cellIndex,
            int rotation)
        {
            for (int index = 0; index < PlacementProjections.Count; index++)
            {
                TabletPlacementProjectionSnapshot placement =
                    PlacementProjections[index];
                if (placement.CellIndex == cellIndex)
                {
                    return placement.FindRotation(rotation);
                }
            }

            for (int index = 0; index < RotationProjections.Count; index++)
            {
                if (RotationProjections[index].Rotation == rotation)
                {
                    return RotationProjections[index];
                }
            }
            return null;
        }
    }

    internal sealed class InventoryItemSnapshot
    {
        internal InventoryItemSnapshot(int instanceId, int entityId, int quantity,
            int cellIndex, int x, int y, string name, string nameKey,
            string nativeItemTypeName, string rarity, string[] baseCategories,
            InventoryItemKind kind, ArtifactSnapshot artifact,
            StoneTabletSnapshot stoneTablet)
        {
            InstanceId = instanceId;
            EntityId = entityId;
            Quantity = quantity;
            CellIndex = cellIndex;
            X = x;
            Y = y;
            Name = name ?? string.Empty;
            NameKey = nameKey ?? string.Empty;
            NativeItemTypeName = nativeItemTypeName ?? string.Empty;
            NativeType = Enum.TryParse(NativeItemTypeName,
                    ignoreCase: false, out NativeInventoryItemType nativeType)
                ? nativeType
                : NativeInventoryItemType.Unknown;
            Rarity = rarity ?? string.Empty;
            BaseCategories = Array.AsReadOnly(baseCategories == null
                ? Array.Empty<string>()
                : (string[])baseCategories.Clone());
            Kind = kind;
            Artifact = artifact;
            StoneTablet = stoneTablet;
        }

        internal InventoryItemKey ItemKey => new(EntityId, InstanceId);
        internal int InstanceId { get; }
        internal int EntityId { get; }
        internal int Quantity { get; }
        internal int CellIndex { get; }
        internal int X { get; }
        internal int Y { get; }
        internal string Name { get; }
        internal string NameKey { get; }
        internal string NativeItemTypeName { get; }
        internal NativeInventoryItemType NativeType { get; }
        internal string Rarity { get; }
        internal IReadOnlyList<string> BaseCategories { get; }
        internal InventoryItemKind Kind { get; }
        internal ArtifactSnapshot Artifact { get; }
        internal StoneTabletSnapshot StoneTablet { get; }
    }

    internal sealed class NativePresetSnapshot
    {
        internal NativePresetSnapshot(int selectedSlot, bool enabled, string name,
            int startingWeaponId, string costumeId, int[] favoriteEntityIds,
            string[] favoriteCategories, string costumeSkinId = null,
            NativePresetPassiveSnapshot[] passives = null,
            NativePresetPocketItemSnapshot[] pocketItems = null,
            NativePresetFruitSnapshot[] fruits = null,
            int adaptiveItemDropBonus = 1)
        {
            SelectedSlot = selectedSlot;
            Enabled = enabled;
            Name = name ?? string.Empty;
            StartingWeaponId = startingWeaponId;
            CostumeId = costumeId ?? string.Empty;
            CostumeSkinId = costumeSkinId ?? string.Empty;
            FavoriteEntityIds = Array.AsReadOnly(favoriteEntityIds == null
                ? Array.Empty<int>()
                : (int[])favoriteEntityIds.Clone());
            FavoriteCategories = Array.AsReadOnly(favoriteCategories == null
                ? Array.Empty<string>()
                : (string[])favoriteCategories.Clone());
            Passives = Array.AsReadOnly(passives == null
                ? Array.Empty<NativePresetPassiveSnapshot>()
                : (NativePresetPassiveSnapshot[])passives.Clone());
            PocketItems = Array.AsReadOnly(pocketItems == null
                ? Array.Empty<NativePresetPocketItemSnapshot>()
                : (NativePresetPocketItemSnapshot[])pocketItems.Clone());
            Fruits = Array.AsReadOnly(fruits == null
                ? Array.Empty<NativePresetFruitSnapshot>()
                : (NativePresetFruitSnapshot[])fruits.Clone());
            AdaptiveItemDropBonus = adaptiveItemDropBonus;
        }

        internal int SelectedSlot { get; }
        internal bool Enabled { get; }
        internal string Name { get; }
        internal int StartingWeaponId { get; }
        internal string CostumeId { get; }
        internal string CostumeSkinId { get; }
        internal IReadOnlyList<int> FavoriteEntityIds { get; }
        internal IReadOnlyList<string> FavoriteCategories { get; }
        internal IReadOnlyList<NativePresetPassiveSnapshot> Passives { get; }
        internal IReadOnlyList<NativePresetPocketItemSnapshot> PocketItems { get; }
        internal IReadOnlyList<NativePresetFruitSnapshot> Fruits { get; }
        internal int AdaptiveItemDropBonus { get; }
        internal bool HasExplicitComboTargets => false;

        internal bool ContentEquals(NativePresetSnapshot other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other == null || SelectedSlot != other.SelectedSlot ||
                Enabled != other.Enabled || Name != other.Name ||
                StartingWeaponId != other.StartingWeaponId ||
                CostumeId != other.CostumeId || CostumeSkinId != other.CostumeSkinId ||
                AdaptiveItemDropBonus != other.AdaptiveItemDropBonus ||
                FavoriteEntityIds.Count != other.FavoriteEntityIds.Count ||
                FavoriteCategories.Count != other.FavoriteCategories.Count ||
                Passives.Count != other.Passives.Count ||
                PocketItems.Count != other.PocketItems.Count ||
                Fruits.Count != other.Fruits.Count)
            {
                return false;
            }

            for (int index = 0; index < FavoriteEntityIds.Count; index++)
            {
                if (FavoriteEntityIds[index] != other.FavoriteEntityIds[index])
                {
                    return false;
                }
            }

            for (int index = 0; index < FavoriteCategories.Count; index++)
            {
                if (FavoriteCategories[index] != other.FavoriteCategories[index])
                {
                    return false;
                }
            }

            for (int index = 0; index < Passives.Count; index++)
            {
                if (!Passives[index].ContentEquals(other.Passives[index]))
                {
                    return false;
                }
            }

            for (int index = 0; index < PocketItems.Count; index++)
            {
                if (!PocketItems[index].ContentEquals(other.PocketItems[index]))
                {
                    return false;
                }
            }

            for (int index = 0; index < Fruits.Count; index++)
            {
                if (!Fruits[index].ContentEquals(other.Fruits[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }

    internal sealed class NativePresetPassiveSnapshot
    {
        internal NativePresetPassiveSnapshot(ulong passiveId, int points)
        {
            PassiveId = passiveId;
            Points = points;
        }

        internal ulong PassiveId { get; }
        internal int Points { get; }
        internal bool ContentEquals(NativePresetPassiveSnapshot other) =>
            other != null && PassiveId == other.PassiveId && Points == other.Points;
    }

    internal sealed class NativePresetPocketItemSnapshot
    {
        internal NativePresetPocketItemSnapshot(int instanceId, int entityId,
            int quantity)
        {
            InstanceId = instanceId;
            EntityId = entityId;
            Quantity = quantity;
        }

        internal InventoryItemKey ItemKey => new(EntityId, InstanceId);
        internal int InstanceId { get; }
        internal int EntityId { get; }
        internal int Quantity { get; }
        internal bool ContentEquals(NativePresetPocketItemSnapshot other) =>
            other != null && ItemKey == other.ItemKey && Quantity == other.Quantity;
    }

    internal sealed class NativePresetFruitSnapshot
    {
        internal NativePresetFruitSnapshot(string categoryId, int value)
        {
            CategoryId = categoryId ?? string.Empty;
            Value = value;
        }

        internal string CategoryId { get; }
        internal int Value { get; }
        internal bool ContentEquals(NativePresetFruitSnapshot other) =>
            other != null && CategoryId == other.CategoryId && Value == other.Value;
    }

    internal sealed class ComboCategorySnapshot
    {
        internal ComboCategorySnapshot(string categoryId, int currentCount,
            int appliedCount, int artifactCategoryCount, int bonusCount,
            int inferredUniquePairCount, int[] setThresholds,
            int[] comboThresholds, bool nativePresetFavorite,
            int highestComboCount = 0, int highestReachedThreshold = 0,
            int unlimitedComboExtraCount = 0)
        {
            CategoryId = categoryId ?? string.Empty;
            CurrentCount = currentCount;
            AppliedCount = appliedCount;
            ArtifactCategoryCount = artifactCategoryCount;
            BonusCount = bonusCount;
            InferredUniquePairCount = inferredUniquePairCount;
            SetThresholds = Array.AsReadOnly(setThresholds == null
                ? Array.Empty<int>()
                : (int[])setThresholds.Clone());
            ComboThresholds = Array.AsReadOnly(comboThresholds == null
                ? Array.Empty<int>()
                : (int[])comboThresholds.Clone());
            NativePresetFavorite = nativePresetFavorite;
            HighestComboCount = highestComboCount;
            HighestReachedThreshold = highestReachedThreshold;
            UnlimitedComboExtraCount = unlimitedComboExtraCount;
        }

        internal string CategoryId { get; }
        internal int CurrentCount { get; }
        internal int AppliedCount { get; }
        internal int ArtifactCategoryCount { get; }
        internal int BonusCount { get; }
        internal int InferredUniquePairCount { get; }
        internal bool AccountingConsistent => InferredUniquePairCount >= 0;
        internal IReadOnlyList<int> SetThresholds { get; }
        internal IReadOnlyList<int> ComboThresholds { get; }
        internal bool NativePresetFavorite { get; }
        internal int HighestComboCount { get; }
        internal int HighestReachedThreshold { get; }
        internal int UnlimitedComboExtraCount { get; }
    }

    internal sealed class InventorySnapshot
    {
        private readonly InventoryCellSnapshot[] cells;
        private readonly InventoryItemSnapshot[] items;
        private readonly ComboCategorySnapshot[] comboCategories;
        private readonly FixedTabletSourceSnapshot[] fixedTabletSources;
        private readonly IReadOnlyList<InventoryCellSnapshot> readonlyCells;
        private readonly IReadOnlyList<InventoryItemSnapshot> readonlyItems;
        private readonly IReadOnlyList<ComboCategorySnapshot> readonlyComboCategories;
        private readonly IReadOnlyList<FixedTabletSourceSnapshot>
            readonlyFixedTabletSources;

        internal InventorySnapshot(int width, int storage,
            InventoryCellSnapshot[] cells, InventoryItemSnapshot[] items,
            bool artifactEffectsEnabled = true, int globalActiveValue = 1,
            NativePresetSnapshot nativePreset = null,
            ComboCategorySnapshot[] comboCategories = null,
            bool suppressDuplicateComboEntities = false,
            int uniquePairComboMode = 0, BuildIntentSnapshot buildIntent = null,
            int unlimitedComboStatValue = 0,
            InventoryEvaluationOrderSnapshot evaluationOrder = null,
            FixedTabletSourceSnapshot[] fixedTabletSources = null,
            bool arrangementBonusesEnabled = false,
            InventoryPositionEffectsSnapshot positionEffects = null)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (storage < 0 || cells == null || cells.Length != storage)
            {
                throw new ArgumentException("Cell count must match storage.", nameof(cells));
            }

            Width = width;
            Storage = storage;
            Height = storage == 0 ? 0 : (storage + width - 1) / width;
            ArtifactEffectsEnabled = artifactEffectsEnabled;
            GlobalActiveValue = globalActiveValue;
            NativePreset = nativePreset;
            BuildIntent = buildIntent ?? BuildIntentSnapshot.FromNativePreset(
                nativePreset);
            SuppressDuplicateComboEntities = suppressDuplicateComboEntities;
            UniquePairComboMode = uniquePairComboMode;
            UnlimitedComboStatValue = unlimitedComboStatValue;
            ArrangementBonusesEnabled = arrangementBonusesEnabled;
            PositionEffects = positionEffects ?? InventoryPositionEffectsSnapshot.Empty;
            EvaluationOrder = evaluationOrder;
            this.fixedTabletSources = fixedTabletSources == null
                ? Array.Empty<FixedTabletSourceSnapshot>()
                : (FixedTabletSourceSnapshot[])fixedTabletSources.Clone();
            this.cells = (InventoryCellSnapshot[])cells.Clone();
            this.items = items == null
                ? Array.Empty<InventoryItemSnapshot>()
                : (InventoryItemSnapshot[])items.Clone();
            this.comboCategories = comboCategories == null
                ? Array.Empty<ComboCategorySnapshot>()
                : (ComboCategorySnapshot[])comboCategories.Clone();
            readonlyCells = Array.AsReadOnly(this.cells);
            readonlyItems = Array.AsReadOnly(this.items);
            readonlyComboCategories = Array.AsReadOnly(this.comboCategories);
            readonlyFixedTabletSources = Array.AsReadOnly(this.fixedTabletSources);
            SettlementValidation = InventorySettlementValidator.Validate(this);
        }

        internal int Width { get; }
        internal int Height { get; }
        internal int Storage { get; }
        internal bool ArtifactEffectsEnabled { get; }
        internal int GlobalActiveValue { get; }
        internal NativePresetSnapshot NativePreset { get; }
        internal BuildIntentSnapshot BuildIntent { get; }
        internal bool SuppressDuplicateComboEntities { get; }
        internal int UniquePairComboMode { get; }
        internal int UnlimitedComboStatValue { get; }
        internal bool ArrangementBonusesEnabled { get; }
        internal InventoryPositionEffectsSnapshot PositionEffects { get; }
        internal InventoryEvaluationOrderSnapshot EvaluationOrder { get; }
        internal IReadOnlyList<FixedTabletSourceSnapshot> FixedTabletSources =>
            readonlyFixedTabletSources;
        internal InventorySettlementValidationSnapshot SettlementValidation
        { get; }
        internal IReadOnlyList<InventoryCellSnapshot> Cells => readonlyCells;
        internal IReadOnlyList<InventoryItemSnapshot> Items => readonlyItems;
        internal IReadOnlyList<ComboCategorySnapshot> ComboCategories =>
            readonlyComboCategories;

        internal bool TryGetCell(int x, int y, out InventoryCellSnapshot cell)
        {
            int index = y * Width + x;
            if (x < 0 || x >= Width || y < 0 || index < 0 || index >= cells.Length)
            {
                cell = null;
                return false;
            }

            cell = cells[index];
            return true;
        }
    }
}
