using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.KeyboardUiNavigation;
using SephiriaEnhancements.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SephiriaEnhancements.ItemCommunication.Integration
{
    internal sealed class NativeItemCommunication : MonoBehaviour
    {
        internal static NativeItemCommunication Current { get; private set; }
        private readonly List<RaycastResult> hits = new();
        private ItemCommunicationThrottle throttle = new();
        private NativeItemCommunicationView view;
        private NativeItemCommunicationTarget target;
        private PlayerAvatar player;
        private int sentFrame = -1;

        private void Awake() => Current = this;
        private void LateUpdate() => FeatureFailure.Run(FeatureId.ItemCommunication, Tick);

        private void Tick()
        {
            var local = LocalPlayerResolver.Resolve();
            if (local != player) { Clear(); player = local; throttle = new(); }
            if (!EnhancementsSettings.Enabled || !NativeItemCommunicationTarget.Ready(player)) { Clear(); return; }
            UIBase panel = UIManager.Instance.GetElement<UI_ShopPanel>();
            if (!KeyboardUiSelection.IsPanelReady(panel)) panel = UIManager.Instance.GetElement<UI_SephiriteRewardPanel>();
            if (!KeyboardUiSelection.IsPanelReady(panel)) { Clear(); return; }
            if (view == null || view.Panel != panel)
            {
                Clear();
                view = new NativeItemCommunicationView(panel, Send);
            }
            InputAction action = NativeInputActions.FindShortcut(PlayerInputController.Instance?.playerInput?.actions,
                ModShortcuts.ContextAction);
            GameObject selected = BrowsedObject(action?.WasPressedThisFrame() == true ? action : null);
            var browsed = NativeItemCommunicationTarget.Read(selected);
            if (browsed != null && browsed.Panel == panel) target = browsed;
            else if (!view.Owns(selected) && selected?.GetComponentInParent<Selectable>() != null) target = null;
            else if (target != null && !target.IsCurrent()) target = null;
            view.Refresh(target, Binding(action));
            if (action?.WasPressedThisFrame() != true) return;
            // A remembered item only applies to an explicitly focused communication button.
            if (browsed != null && browsed.Panel == panel)
                Send(browsed.Shop == null ? ItemCommunicationIntent.OfferReward : ItemCommunicationIntent.RequestPurchase);
            else if (view.Owns(selected))
                Send(view.IntentFor(selected));
        }

        private GameObject BrowsedObject(InputAction pressed)
        {
            bool mouse = pressed != null ? pressed.activeControl?.device is Mouse :
                ControlsChangeHandler.Current?.IsUsingKeyboardAndMouse == true && !KeyboardUiPointer.OwnsFocus;
            if (!mouse) return EventSystem.current?.currentSelectedGameObject;
            if (Mouse.current == null || EventSystem.current == null) return null;
            hits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current)
                { position = Mouse.current.position.ReadValue() }, hits);
            return hits.Count == 0 ? null : hits[0].gameObject;
        }

        private static string Binding(InputAction action) => action?.GetBindingDisplayString(
            group: PlayerInputController.Instance?.playerInput?.currentControlScheme) ?? "";

        private void Send(ItemCommunicationIntent intent)
        {
            if (sentFrame == Time.frameCount) return;
            if (!EnhancementsSettings.Enabled || target == null || !target.IsCurrent() ||
                (target.Shop == null) != (intent == ItemCommunicationIntent.OfferReward))
            { NativeModNotifications.Short(ItemCommunicationLocalization.Unavailable); return; }
            string key = intent == ItemCommunicationIntent.OfferReward ? ItemCommunicationLocalization.RewardMessage :
                intent == ItemCommunicationIntent.RequestPurchase ? ItemCommunicationLocalization.BuyMessage : ItemCommunicationLocalization.ShareMessage;
            string message = ItemCommunicationMessage.Compose(ModLocalization.Get(key), Clean(target.Item.Name),
                target.Shop == null ? "" : Clean(target.Quote()), target.Shop == null ? "" :
                    target.Shop.BuyerCharacter.GetCustomStat(ECustomStat.Negotiation).ToString(CultureInfo.InvariantCulture),
                Clean(ItemDatabase.GetItemRarityName(target.Item.rarity).ToString()), Clean(target.Categories));
            if (message == null) { NativeModNotifications.Short(ItemCommunicationLocalization.TooLong); return; }
            if (!throttle.CanSend(message, Time.unscaledTime))
            { NativeModNotifications.Short(ItemCommunicationLocalization.Cooldown); return; }
            var local = LocalPlayerResolver.Resolve();
            if (local == null || local != player || DungeonManager.Instance == null) return;
            // Native chat owns server relay and the sender's eventual local echo.
            DungeonManager.Instance.Chat(local, local.Name, message);
            throttle.Record(message, Time.unscaledTime);
            sentFrame = Time.frameCount;
        }

        private static string Clean(string text) => ChatTextSanitizer.Sanitize(text, ItemCommunicationMessage.MaximumLength);

        internal Selectable FindEntry(GameObject source, Selectable next)
        {
            if (view == null || !EnhancementsSettings.Enabled) return next;
            var item = NativeItemCommunicationTarget.Read(source);
            if (item == null || item.Panel != view.Panel) return next;
            if (view.Owns(next?.gameObject)) return next;
            var destination = next == null ? null : NativeItemCommunicationTarget.Read(next.gameObject);
            if (destination != null && destination.Panel == item.Panel) return next;
            target = item;
            view.FollowingControl = next;
            view.Refresh(target, Binding(NativeInputActions.FindShortcut(
                PlayerInputController.Instance?.playerInput?.actions, ModShortcuts.ContextAction)));
            return view.Entry;
        }

        private void Clear()
        {
            view?.Dispose(); view = null; target = null;
        }

        private void OnDestroy()
        {
            Clear();
            if (Current == this) Current = null;
        }
    }

    [HarmonyPatch(typeof(Selectable), nameof(Selectable.OnMove))]
    internal static class ItemCommunicationNavigationPatch
    {
        private static bool Prefix(Selectable __instance, AxisEventData eventData)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.ItemCommunication)) return true;
            try { return PrefixCore(__instance, eventData); }
            catch (System.Exception exception) { FeatureFailure.Disable(FeatureId.ItemCommunication, exception); return true; }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore(Selectable __instance, AxisEventData eventData)
        {
            if (eventData.moveDir != MoveDirection.Down || NativeItemCommunication.Current == null) return true;
            var next = __instance.FindSelectableOnDown();
            var entry = NativeItemCommunication.Current.FindEntry(__instance.gameObject, next);
            if (entry == next || entry == null) return true;
            eventData.selectedObject = entry.gameObject;
            return false;
        }
    }
}
