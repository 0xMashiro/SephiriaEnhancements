using System;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.MultiplayerRules.Presentation;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal sealed class NativeLobbyRulesPoint : IDisposable
    {
        private GameObject root;
        private FloorGenerator floor;

        internal void Update()
        {
            if (!MultiplayerRulesContext.IsInLobby)
            {
                Dispose();
                return;
            }
            var player = LocalPlayerResolver.Resolve();
            var currentFloor = FloorGenerator.FindByGuid(player.currentFloorGuid);
            if (currentFloor == null) return;
            if (root != null && floor == currentFloor) return;
            Dispose();
            var source = currentFloor.GetComponentInChildren<OpenHardModePanel>();
            if (source == null) return;
            var sourceSprite = source.GetComponent<SpriteRenderer>();
            var sourceCollider = source.GetComponent<BoxCollider2D>();
            if (sourceSprite == null || sourceCollider == null) return;

            // A local UI interaction point. Never clone the native gameplay scripts,
            // network identity, persistent events, or save-writing behavior.
            root = new GameObject("Team Rules");
            root.SetActive(false);
            floor = currentFloor;
            root.layer = source.gameObject.layer;
            root.transform.SetParent(floor.transform, false);
            root.transform.position = source.transform.position - new Vector3(sourceSprite.bounds.size.x + .35f, 0f, 0f);
            root.transform.localScale = source.transform.localScale;
            foreach (var original in source.GetComponentsInChildren<SpriteRenderer>())
            {
                var visual = new GameObject("Stone Visual");
                visual.layer = original.gameObject.layer;
                visual.transform.SetParent(root.transform, false);
                visual.transform.position = original.transform.position + root.transform.position - source.transform.position;
                visual.transform.rotation = original.transform.rotation;
                visual.transform.localScale = new Vector3(
                    original.transform.lossyScale.x / root.transform.lossyScale.x,
                    original.transform.lossyScale.y / root.transform.lossyScale.y, 1f);
                var sprite = visual.AddComponent<SpriteRenderer>();
                sprite.sprite = original.sprite;
                sprite.sharedMaterial = original.sharedMaterial;
                sprite.sortingLayerID = original.sortingLayerID;
                sprite.sortingOrder = original.sortingOrder;
                sprite.color = original.color;
                sprite.flipX = original.flipX;
                sprite.flipY = original.flipY;
            }
            var collider = root.AddComponent<BoxCollider2D>();
            collider.size = sourceCollider.size;
            collider.offset = sourceCollider.offset;
            collider.isTrigger = true;
            var interaction = root.AddComponent<NativeLobbyRulesInteraction>();
            interaction.interactionDescription = new LocalizedString(MultiplayerRulesLocalization.PanelTitle);
            interaction.applyCustomHeight = true;
            interaction.customHeight = source.GetComponent<Interactable>().Height;
            root.SetActive(true);
        }

        public void Dispose()
        {
            if (root != null)
            {
                root.SetActive(false);
                UnityEngine.Object.Destroy(root);
            }
            root = null;
            floor = null;
        }
    }

    internal sealed class NativeLobbyRulesInteraction : Interactable
    {
        public override bool IsInteractable(GameObject actor) =>
            base.IsInteractable(actor) && MultiplayerRulesContext.IsInLobby &&
            LocalPlayerResolver.Resolve()?.gameObject == actor;

        public override void Interactive(GameObject actor)
        {
            if (IsInteractable(actor) && UIManager.Instance?.CurrentControlStack == null)
                NativeMultiplayerRulesPanel.Show();
        }
    }
}
