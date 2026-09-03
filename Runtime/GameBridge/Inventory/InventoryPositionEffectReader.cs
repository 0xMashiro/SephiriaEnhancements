#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using SephiriaEnhancements.Runtime.Inventory;
using static SephiriaEnhancements.Runtime.GameBridge.Inventory.NativeInventoryEffectAccess;

namespace SephiriaEnhancements.Runtime.GameBridge.Inventory
{
    internal static class InventoryPositionEffectReader
    {
        private static readonly HashSet<Type> PositionIndependentTypes = new();
        private static readonly Dictionary<string, InventoryPositionEffectKind> Kinds = new(StringComparer.Ordinal)
        {
            ["Charm_NearLevelDamage"] = InventoryPositionEffectKind.NeighborArtifactLevelDamage,
            ["Charm_PlanetModule"] = InventoryPositionEffectKind.AdjacentPlanetEnhancement,
            ["Charm_CompanionChaos"] = InventoryPositionEffectKind.SameRowCompanionMode,
            ["Charm_ReduceMPCost"] = InventoryPositionEffectKind.MagicCostReduction,
            ["Charm_RightSpellCooldownHelper"] = InventoryPositionEffectKind.MagicCooldownRecovery,
            ["Charm_WoodenBox"] = InventoryPositionEffectKind.FirstSlotsElementDamage,
            ["Charm_FireIce"] = InventoryPositionEffectKind.HalfBoardStats,
            ["Charm_FireIceWeapon"] = InventoryPositionEffectKind.HalfBoardWeaponMode,
            ["Charm_UpCharmDamage"] = InventoryPositionEffectKind.DependencyDamage
        };

        internal static InventoryPositionEffectsSnapshot Capture(GridInventory inventory)
        {
            var rules = new List<InventoryPositionEffectRule>();
            var traits = new List<InventoryPositionTargetTraits>();
            var observed = new List<InventoryPositionEffectValue>();
            var issues = new List<string>();
            try
            {
                var nativeItems = new List<NewItemOwnInstance>();
                for (int cell = 0; cell < inventory.CurrentInventoryStorage; cell++)
                {
                    var item = inventory.FindItem(inventory.IdxToPos(cell));
                    if (item?.Charm != null) nativeItems.Add(item);
                }
                var keys = nativeItems.ToDictionary(item => (object)item.Charm,
                    item => new InventoryItemKey(item.EntityID, item.InstanceID));
                foreach (var item in nativeItems)
                {
                    var source = keys[item.Charm];
                    object artifact = item.Charm;
                    Type type = artifact.GetType();
                    try
                    {
                        string[] names = Hierarchy(type).Select(owner => owner.Name).ToArray();
                        traits.Add(new InventoryPositionTargetTraits(source,
                            names.Contains("Charm_SummonGreenBat"),
                            type.GetInterfaces().Any(contract => contract.Name == "ICompanionCharm"),
                            item.Charm.netIdentity != null && item.Charm.netIdentity.netId != 0,
                            Convert.ToInt32(item.Entity.rarity), names.Contains("Charm_Magic")));
                        Type ruleType = Hierarchy(type).FirstOrDefault(owner => Kinds.ContainsKey(owner.Name));
                        if (ruleType == null)
                        {
                            ValidateUnmodeledType(type);
                            continue;
                        }
                        if (type != ruleType) throw new InvalidOperationException("Unmodeled effect subclass: " + type.Name);
                        // Native effect caches are updated by server callbacks and are not
                        // synchronized. A client cannot use their defaults as observations,
                        // even when the source effect is currently disabled.
                        if (!inventory.isServer)
                        {
                            if (!issues.Contains(InventoryPositionEffectsSnapshot.ObservationUnavailableOnClient))
                                issues.Add(InventoryPositionEffectsSnapshot.ObservationUnavailableOnClient);
                            continue;
                        }
                        var rule = CaptureRule(artifact, source, Kinds[ruleType.Name]);
                        rules.Add(rule);
                        CaptureObserved(item.Charm, rule, nativeItems, keys, observed);
                    }
                    catch (Exception error)
                    {
                        issues.Add("PositionEffectCaptureUnavailable:" + source + ":" + type.Name + ":" +
                            error.GetBaseException().GetType().Name + ":" + error.GetBaseException().Message);
                    }
                }
            }
            catch (Exception error)
            {
                issues.Add("PositionEffectCaptureUnavailable:" + error.GetBaseException().Message);
            }
            return new InventoryPositionEffectsSnapshot(rules.ToArray(), traits.ToArray(), observed.ToArray(), issues.ToArray());
        }

        private static InventoryPositionEffectRule CaptureRule(object artifact, InventoryItemKey source,
            InventoryPositionEffectKind kind)
        {
            Type type = artifact.GetType();
            switch (kind)
            {
                case InventoryPositionEffectKind.NeighborArtifactLevelDamage:
                    CheckMethod(type, "UpdateDamageBonus");
                    return new(source, kind, Curve(artifact, "allDamageBonusByLevel"), offsets: Directions(type));
                case InventoryPositionEffectKind.AdjacentPlanetEnhancement:
                    CheckMethod(type, "SearchPlanet");
                    return new(source, kind, offsets: Directions(type),
                        targetCategory: StringConstants(type, "SearchPlanet").Single());
                case InventoryPositionEffectKind.SameRowCompanionMode:
                    CheckMethod(type, "SearchCompanion");
                    return new(source, kind);
                case InventoryPositionEffectKind.MagicCostReduction:
                    CheckMethod(type, "SearchMagic");
                    return new(source, kind, Curve(artifact, "reducePercentByLevel"), offsets: TargetOffset(type, "SearchMagic"));
                case InventoryPositionEffectKind.MagicCooldownRecovery:
                    CheckMethod(type, "SearchMagic");
                    return new(source, kind, Curve(artifact, "cooldownRecoveryByLevel"), offsets: TargetOffset(type, "SearchMagic"));
                case InventoryPositionEffectKind.FirstSlotsElementDamage:
                    CheckMethod(type, "CheckQuickSlot");
                    CheckMethod(type, "ApplyEffect");
                    double[] values = Curve(artifact, "apPerQuickSlotCharmByLevel");
                    if (values.Length <= Integer(artifact, "maxLevel"))
                        throw new InvalidOperationException("Slot effect curve does not cover its maximum level");
                    return new(source, kind, values, boundary: SlotCount(type, "CheckQuickSlot"),
                        channels: StatChannels(type, "ApplyEffect"));
                case InventoryPositionEffectKind.HalfBoardStats:
                    CheckMethod(type, "AddStat");
                    return new(source, kind, Curve(artifact, "mainStat"), Curve(artifact, "oppositeStat"),
                        boundary: HalfBoardBoundary(type, "AddStat"),
                        channels: new[] { Read(artifact, "leftStatName").ToString(), Read(artifact, "rightStatName").ToString() });
                case InventoryPositionEffectKind.HalfBoardWeaponMode:
                    CheckMethod(type, "CheckPosition");
                    return new(source, kind, boundary: HalfBoardBoundary(type, "CheckPosition"),
                        channels: StatStringChannels(type, "CheckPosition"));
                case InventoryPositionEffectKind.DependencyDamage:
                    CheckMethod(type, "OnRequestCharmDamageBonus");
                    CheckMethod(type, "IsDependencyValid");
                    return new(source, kind, Curve(artifact, "damageBonusByLevel"), Curve(artifact, "dependencyDamageBonusByLevel"),
                        new[] { new InventoryOffsetSnapshot(Integer(artifact, "xOffset"), Integer(artifact, "yOffset")) },
                        conditionalDamage: Read<bool>(artifact, "hasDependencyCondition"),
                        maximumRarity: Integer(artifact, "maxRarity"));
                default: throw new InvalidOperationException("Unsupported effect kind");
            }
        }

        private static void CaptureObserved(Charm_Basic artifact, InventoryPositionEffectRule rule,
            IReadOnlyList<NewItemOwnInstance> nativeItems, Dictionary<object, InventoryItemKey> keys,
            List<InventoryPositionEffectValue> output)
        {
            switch (rule.Kind)
            {
                case InventoryPositionEffectKind.NeighborArtifactLevelDamage:
                    Add(Integer(artifact, "currentDamageBonus"));
                    break;
                case InventoryPositionEffectKind.AdjacentPlanetEnhancement:
                    foreach (object target in (IEnumerable)Read(artifact, "planets"))
                        Add(Integer(target, "isEnhanced") > 0 ? 1 : 0, keys[target]);
                    break;
                case InventoryPositionEffectKind.SameRowCompanionMode:
                    foreach (object target in (IEnumerable)Read(artifact, "companions"))
                        Add(Read<bool>(target, "chaoticMode") ? 1 : 0, keys[target]);
                    break;
                case InventoryPositionEffectKind.MagicCostReduction:
                    if (Read<bool>(artifact, "reduceActivated"))
                        Add(Integer(artifact, "reducedPercent"), keys[Read(artifact, "currentMagicCharm")]);
                    break;
                case InventoryPositionEffectKind.MagicCooldownRecovery:
                    if (Read<bool>(artifact, "helperActivated"))
                        Add(Integer(artifact, "helpPercent"), keys[Read(artifact, "currentMagicCharm")]);
                    break;
                case InventoryPositionEffectKind.FirstSlotsElementDamage:
                    int count = Integer(artifact, "applied");
                    int level = Integer(artifact, "appliedLevel");
                    if (count != 0 && (level < 0 || level >= rule.ValuesByLevel.Count))
                        throw new InvalidOperationException("Invalid applied slot effect level");
                    foreach (string channel in rule.Channels)
                        Add(count == 0 ? 0 : rule.ValuesByLevel[level] * count, channel: channel);
                    break;
                case InventoryPositionEffectKind.HalfBoardStats:
                    Add(Integer(artifact, "leftStatAdded"), channel: rule.Channels[0]);
                    Add(Integer(artifact, "rightStatAdded"), channel: rule.Channels[1]);
                    Add(Read<bool>(artifact, "added") ? (artifact.Item.XIdx <= rule.Boundary ? 0 : 1) : -1,
                        channel: "Mode", mode: true);
                    break;
                case InventoryPositionEffectKind.HalfBoardWeaponMode:
                    bool active = Read<bool>(artifact, "isEffectActivated");
                    bool left = Read<bool>(artifact, "isLeft");
                    Add(active && left ? 1 : 0, channel: rule.Channels[0]);
                    Add(active && !left ? 1 : 0, channel: rule.Channels[1]);
                    Add(active ? (left ? 0 : 1) : -1, channel: "Mode", mode: true);
                    break;
                case InventoryPositionEffectKind.DependencyDamage:
                    foreach (var root in nativeItems)
                    {
                        if (ReferenceEquals(root.Charm, artifact) || !NativeDependencyReaches(root.Charm, artifact)) continue;
                        // This native read-only request is independent of the projected graph and curves.
                        int bonus = Convert.ToInt32(Invoke(artifact, "OnRequestCharmDamageBonus", root.Charm));
                        if (bonus != 0) Add(bonus, keys[root.Charm]);
                    }
                    break;
            }

            void Add(double value, InventoryItemKey? target = null, string channel = "", bool mode = false) =>
                output.Add(new InventoryPositionEffectValue(new InventoryPositionEffectKey(
                    rule.Source, rule.Kind, target, channel), value, mode));
        }

        private static bool NativeDependencyReaches(Charm_Basic root, Charm_Basic source)
        {
            var visited = new HashSet<Charm_Basic>();
            var queue = new Queue<Charm_Basic>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var next = queue.Dequeue();
                if (!visited.Add(next) || next.netIdentity == null || next.netIdentity.netId == 0) continue;
                if (next == source) return true;
                if (next.GetType().GetInterfaces().Any(contract => contract.Name == "IDependencyConditionCharm") &&
                    !(bool)Invoke(next, "IsDependencyValid", root)) continue;
                if (next.Inventory == null) continue;
                foreach (var dependency in next.Inventory.GetCharmDependencies(new ItemPosition(next.xIdx, next.yIdx)))
                    queue.Enqueue(dependency);
            }
            return false;
        }

        private static InventoryOffsetSnapshot[] Directions(Type type) =>
            ((IEnumerable)Read(type, "directions")).Cast<object>()
                .Select(position => new InventoryOffsetSnapshot(Integer(position, "x"), Integer(position, "y"))).ToArray();

        private static InventoryOffsetSnapshot[] TargetOffset(Type type, string method) => new[]
        {
            new InventoryOffsetSnapshot(CoordinateOffset(type, method, "XIdx"), CoordinateOffset(type, method, "YIdx"))
        };

        private static void CheckMethod(Type type, string name)
        {
            var method = Method(type, name);
            if (Harmony.GetPatchInfo(method)?.Owners.Count > 0)
                throw new InvalidOperationException("Effect method has runtime patches: " + name);
            Instructions(method);
        }

        private static void ValidateUnmodeledType(Type type)
        {
            if (PositionIndependentTypes.Contains(type)) return;
            MethodInfo category = Method(type, "GetItemCategory");
            if (category.DeclaringType.Name != "Charm_Basic" &&
                category.DeclaringType.Name != "Charm_3Elemental_ByRow" && category.DeclaringType.Name != "Charm_WhitePaper")
                throw new InvalidOperationException("Unmodeled category calculation: " + category.DeclaringType.Name);
            foreach (Type owner in Hierarchy(type).TakeWhile(owner => owner.Name != "Charm_Basic"))
            {
                // These two inspected implementations scan all normal inventory
                // members without depending on their positions.
                if (owner.Name == "Charm_BoltMagicMultiShot" || owner.Name == "Charm_MagicCoolDownBonusByTag" ||
                    owner.Name == "Charm_3Elemental_ByRow" || owner.Name == "Charm_WhitePaper") continue;
                foreach (var method in owner.GetMethods(BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    // These callbacks replace/consume the source at its current cell.
                    // Their resulting inventory changes invalidate the active plan.
                    if (owner.Name == "Charm_SweepRange" && method.Name == "OnUpdate" ||
                        owner.Name == "Charm_Chintamani" && method.Name == "Avatar_OnDamagedServerside") continue;
                    if (method.IsSpecialName || method.Name.StartsWith("GetSubIcon", StringComparison.Ordinal) ||
                        method.Name == "GetCustomIcon" || method.Name == "GetConnectedCharmPositions" ||
                        method.Name == "SerializeSyncVars" || method.Name == "DeserializeSyncVars" || method.GetMethodBody() == null) continue;
                    if (Instructions(method).Any(instruction =>
                        instruction.Operand is MethodInfo called && called.DeclaringType?.Name == "GridInventory" && called.Name == "FindItem" ||
                        instruction.Operand is FieldInfo field && (field.Name == "XIdx" || field.Name == "YIdx" ||
                            field.Name == "xIdx" || field.Name == "yIdx") &&
                            (field.DeclaringType?.Name == "NewItemOwnInstance" || field.DeclaringType?.Name == "Charm_Basic")))
                        throw new InvalidOperationException("Unmodeled position-dependent method: " + owner.Name + "." + method.Name);
                }
            }
            PositionIndependentTypes.Add(type);
        }
    }
}
