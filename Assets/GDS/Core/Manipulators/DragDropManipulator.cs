using System;
using UnityEngine;
using UnityEngine.UIElements;
using GDS.Core.Events;

namespace GDS.Core {
    public class DragDropManipulator : PointerManipulator {

        public int MinDragDistance = 32;

        protected EventBus bus;
        protected Observable<Item> ghost;
        protected VisualElement ghostView;

        Vector2 lastPos;
        IItemContext lastContext;
        Item lastContextItem;
        bool loggedGhostMove;


        public DragDropManipulator(Store store, ItemView itemView = null) {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Control });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Command });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Shift });

            bus = store.Bus;
            ghost = store.Ghost;
            itemView ??= new ItemView();

            // var square = Dom.Div("absolute, red-square");

            ghostView = Dom.Div("ghost-item absolute", itemView/*, square*/).PickIgnoreAll();
            ghostView.Observe(ghost, item => {
                if (item == null) { ghostView.Hide(); Debug.Log("[DragDrop] Ghost cleared"); return; }
                itemView.Item = item;
                ghostView.Show();
                Debug.Log($"[DragDrop] Ghost SET to: {item.Name} (type={item.GetType().Name})");
            });

        }

        protected override void RegisterCallbacksOnTarget() {
            target.Add(ghostView);

            target.RegisterCallback<PointerDownEvent>(TryGetValidDragTarget);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        }

        protected override void UnregisterCallbacksFromTarget() {
            target.UnregisterCallback<PointerDownEvent>(TryGetValidDragTarget);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        }

        void TryGetValidDragTarget(PointerDownEvent e) {
            if (!CanStartManipulation(e)) return;
            if (ghost.Value != null) return;

            IItemContext context = (e.target as VisualElement).GetFirstOfType<IItemContext>();

            if (context == null) return;
            if (context.Item == null) return;

            lastPos = e.position;
            lastContext = context;
            lastContextItem = context.Item;
        }

        void OnPointerUp(PointerUpEvent e) {
            if (!CanStartManipulation(e)) return;
            // TODO: add a temporal threshold between picking and placing items (compare mouse down and up times)
            //       to fix a rare case when you click on the edge of one cell and release immediately in another cell
            //       resulting in item moving instead of being picked
            lastContext = null;

            var targetVE = e.target as VisualElement;
            var context = targetVE.GetFirstOfType<IItemContext>();

            Debug.Log($"[DragDrop] OnPointerUp: target={targetVE?.name}({targetVE?.GetType().Name}), " +
                      $"context={context?.GetType().Name}, ghost={ghost.Value?.Name}, " +
                      $"contextSlot={context?.Slot}, contextItem={context?.Item?.Name}");

            if (context == null) return;

            if (ghost.Value == null && context.Item != null) {
                bus.Publish(new PickItem(context.Bag, context.Slot, context.Item, e));
                return;
            }
            if (ghost.Value != null && context.Slot != null) {
                bus.Publish(new PlaceItem(context.Bag, context.Slot, ghost.Value, e));
                return;
            }

            if (ghost.Value != null && context.Slot == null) {
                Debug.Log($"[DragDrop] PlaceItem skipped: ghost={ghost.Value.Name} but context.Slot is null " +
                          $"(context type={context.GetType().Name}, bag={context.Bag?.GetType().Name})");
            }
        }

        void OnPointerMove(PointerMoveEvent e) {
            ghostView.style.left = e.localPosition.x;
            ghostView.style.top = e.localPosition.y;

            if (ghost.Value != null) {
                if (!loggedGhostMove) {
                    Debug.Log($"[DragDrop] OnPointerMove with ghost: target={((VisualElement)e.target)?.name}({((VisualElement)e.target)?.GetType().Name}), pos={e.localPosition}");
                    loggedGhostMove = true;
                }
                return;
            }
            loggedGhostMove = false;
            if (lastContext == null) return;
            if (Math.Abs(lastPos.x - e.position.x) < MinDragDistance && Math.Abs(lastPos.y - e.position.y) < MinDragDistance) return;

            bus.Publish(new PickItem(lastContext.Bag, lastContext.Slot, lastContextItem, e));
            lastContext = null;
        }

    }
}