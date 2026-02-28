using UnityEngine.UIElements;
using GDS.Core.Events;

namespace GDS.Core {
    public class RotateGhostManipulator : PointerManipulator {

        protected EventBus bus;
        protected Observable<Item> ghost;

        CustomEvent rotateEvent = new RotateItem();

        public RotateGhostManipulator(Store store) {
            bus = store.Bus;
            ghost = store.Ghost;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse });
        }

        protected override void RegisterCallbacksOnTarget() {
            target.RegisterCallback<PointerUpEvent>(Rotate);
        }

        protected override void UnregisterCallbacksFromTarget() {
            target.UnregisterCallback<PointerUpEvent>(Rotate);
        }

        void Rotate(PointerUpEvent e) {
            if (e.button != 1) return;
            if (ghost.Value is not ShapeItem i) return;
            i.Direction = i.Direction.Rotate();
            ghost.Notify();
            bus.Publish(rotateEvent);
        }

    }
}