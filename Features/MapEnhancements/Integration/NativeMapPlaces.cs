using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.MapEnhancements.Core;
using UnityEngine;
using System.Collections.Generic;

namespace SephiriaEnhancements.MapEnhancements.Integration
{
    internal static class NativeMapPlaces
    {
        internal static IEnumerable<(Transform Target, MapPlaceKind Kind, Vector3 Position, NativeMapDestination Destination)>
            Collect(NativeMapGeometry geometry, PlayerAvatar player, IEnumerable<Interactable> interactions)
        {
            var facilities = new Dictionary<Transform, (MapPlaceKind Kind, Vector3 Position, int Count)>();
            foreach (Interactable interaction in interactions)
            {
                if (interaction == null || !interaction.isActiveAndEnabled || interaction.hideGuide ||
                    interaction.interactionType == Interactable.EInteractionType.NoAction ||
                    !geometry.Contains(interaction.transform.position) ||
                    interaction.GetComponentInParent<UnitAI_NewBasic>() != null) continue;
                FloorGenerator owner = interaction.GetComponentInParent<FloorGenerator>();
                if (owner != null && owner != geometry.Floor) continue;
                MapPlaceKind kind = FacilityKind(interaction);
                if (kind == MapPlaceKind.None || !interaction.IsInteractable(player.gameObject)) continue;
                Transform target = FacilityTarget(interaction);
                Vector3 position = interaction.transform.position;
                int count = 1;
                if (facilities.TryGetValue(target, out var existing))
                {
                    count += existing.Count;
                    position = (existing.Position * existing.Count + position) / count;
                }
                facilities[target] = (kind, position, count);
            }
            if (geometry.Designed != null)
                foreach (var connection in geometry.Designed.mapTeleportConnections)
                {
                    if (connection.icon == null || connection.point == null) continue;
                    var destination = new NativeMapDestination(connection);
                    if (destination.Kind == MapPlaceKind.None ||
                        !geometry.Contains(destination.Position)) continue;
                    // An authored entry represents one facility of the same kind;
                    // other facilities, even nearby ones, remain separate places.
                    Transform matched = null;
                    float distance = float.PositiveInfinity;
                    foreach (var facility in facilities)
                    {
                        if (facility.Value.Kind != destination.Kind) continue;
                        float candidateDistance = (facility.Value.Position - destination.Position).sqrMagnitude;
                        if (candidateDistance >= distance) continue;
                        matched = facility.Key;
                        distance = candidateDistance;
                    }
                    if (matched != null) facilities.Remove(matched);
                    // A hidden authored entry must not reappear through its facility.
                    if (destination.IsVisible)
                        yield return (destination.Target, destination.Kind, destination.Position, destination);
                }
            foreach (var facility in facilities)
                yield return (facility.Key, facility.Value.Kind, facility.Value.Position, null);
        }

        // Authored map object names are native asset identifiers, not player labels.
        internal static MapPlaceKind DestinationKind(string iconName) => iconName switch
        {
            "Home" => MapPlaceKind.Home,
            "Weapon" => MapPlaceKind.Weapons,
            "Downstair" => MapPlaceKind.Departure,
            "MultiDoor" => MapPlaceKind.MultiplayerGate,
            "Tree_Mini" or "Towntree" => MapPlaceKind.Tree,
            "Training" => MapPlaceKind.Training,
            "Costume" or "Costume_Mini" => MapPlaceKind.Clothing,
            "TimeControl" => MapPlaceKind.TimeOfDay,
            _ => MapPlaceKind.None
        };

        internal static MapPlaceKind FacilityKind(Interactable target)
        {
            if (target is MultiplayerRules.Integration.NativeLobbyRulesInteraction) return MapPlaceKind.TeamRules;
            if (target.GetComponent<OpenMultiplayerPanel>() != null) return MapPlaceKind.MultiplayerGate;
            if (target.GetComponent<NetConnectedPortal>() is NetConnectedPortal portal &&
                portal.direction == NetConnectedPortal.EDirection.GoToMyTown) return MapPlaceKind.TownReturnPortal;
            if (target.GetComponent<GoToNextStage_MultiZone>() != null) return MapPlaceKind.Departure;
            if (target.GetComponent<OpenHardModePanel>() != null) return MapPlaceKind.RootsRetreat;
            if (target.GetComponent<ClientTreeShopOpenObject>() != null) return MapPlaceKind.DestinyInscription;
            if (target is TownWeaponStand) return MapPlaceKind.Weapons;
            if (target.GetComponent<CostumeSelector>() != null) return MapPlaceKind.Clothing;
            if (target.GetComponent<AbilitySelector>() != null) return MapPlaceKind.Talents;
            if (target.GetComponent<StartingFountain>() != null) return MapPlaceKind.WishingFountain;
            if (target.GetComponent<FruitSkewerSelector>() != null) return MapPlaceKind.FruitSkewers;
            if (target.GetComponent<PresetSelector>() != null) return MapPlaceKind.Presets;
            if (target is Sign sign)
                return sign.message.key switch
                {
                    "Message_KiKiShop" or "Message_KiKiShop_Sign" => MapPlaceKind.Shop,
                    "Message_TrainingCenterSign" => MapPlaceKind.Training,
                    "Message_TheRabbittown" => MapPlaceKind.Town,
                    _ => MapPlaceKind.None
                };
            return MapPlaceKind.None;
        }

        internal static Transform FacilityTarget(Interactable target) =>
            target is TownWeaponStand && target.transform.parent != null
                ? target.transform.parent : target.transform;

        internal static string Name(MapPlaceKind kind)
        {
            string modKey = kind switch
            {
                MapPlaceKind.Home => MapNavigationLocalization.Home,
                MapPlaceKind.Weapons => MapNavigationLocalization.Weapons,
                MapPlaceKind.Departure => MapNavigationLocalization.Departure,
                MapPlaceKind.MultiplayerGate => MapNavigationLocalization.MultiplayerGate,
                MapPlaceKind.TownReturnPortal => MapNavigationLocalization.TownReturnPortal,
                MapPlaceKind.Tree => MapNavigationLocalization.Tree,
                MapPlaceKind.TeamRules => MultiplayerRules.Presentation.MultiplayerRulesLocalization.LobbyTitle,
                _ => null
            };
            if (modKey != null) return ModLocalization.Get(modKey);
            string nativeKey = kind switch
            {
                MapPlaceKind.Training => "Message_TrainingCenterSign",
                MapPlaceKind.Clothing => "UI_CostumePanel_Title",
                MapPlaceKind.TimeOfDay => "UI_TimeControlPanel",
                MapPlaceKind.Talents => "Talent",
                MapPlaceKind.WishingFountain => "UI_DimensionPocket",
                MapPlaceKind.FruitSkewers => "TreeShopItem_FruitSkewer_Name",
                MapPlaceKind.Presets => "UI_PresetPanel",
                MapPlaceKind.Shop => "Message_KiKiShop_Sign",
                MapPlaceKind.Town => "Message_TheRabbittown",
                MapPlaceKind.RootsRetreat => "UI_HardModePanel_Title",
                MapPlaceKind.DestinyInscription => "UI_TreeShopPanel_Title",
                _ => null
            };
            return nativeKey == null ? null : KeywordDatabase.Convert(Loc._(nativeKey),
                useColor: false, useSprite: false, useDungeonManager: true, disableLink: false);
        }
    }
}
