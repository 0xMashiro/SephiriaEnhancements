using UnityEngine;

namespace SephiriaEnhancements.MapEnhancements
{
    // Native component identities and localization keys belong at this game API boundary.
    internal static class NativeMapFacilities
    {
        internal static string Name(Interactable target)
        {
            string key = null;
            if (target.GetComponent<CostumeSelector>() != null) key = "UI_CostumePanel_Title";
            else if (target.GetComponent<AbilitySelector>() != null) key = "Talent";
            else if (target.GetComponent<StartingFountain>() != null) key = "UI_DimensionPocket";
            else if (target.GetComponent<FruitSkewerSelector>() != null) key = "TreeShopItem_FruitSkewer_Name";
            else if (target.GetComponent<PresetSelector>() != null) key = "UI_PresetPanel";
            else if (target is Sign sign)
            {
                // Only signs whose text identifies a place, not warnings or dialogue.
                switch (sign.message.key)
                {
                    case "Message_KiKiShop":
                    case "Message_KiKiShop_Sign":
                    case "Message_TrainingCenterSign":
                    case "Message_TheRabbittown": key = sign.message.key; break;
                }
            }
            if (key == null) return null;
            return KeywordDatabase.Convert(Loc._(key), useColor: false, useSprite: false);
        }
    }
}
