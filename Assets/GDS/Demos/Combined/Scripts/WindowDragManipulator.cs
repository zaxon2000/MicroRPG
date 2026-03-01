using UnityEngine;
using UnityEngine.UIElements;

namespace GDS.Demos.Combined {

    /// <summary>
    /// Allows an absolutely-positioned UI window to be freely dragged by a designated header element.
    /// Attach to the header VisualElement; pass the window root to be repositioned.
    /// </summary>
    public class WindowDragManipulator : PointerManipulator {

        readonly VisualElement _window;
        Vector2 _dragStartPointer;
        Vector2 _windowStartPos;
        bool _isDragging;

        /// <param name="header">The element the user grabs to initiate drag (title bar).</param>
        /// <param name="window">The absolutely-positioned element to reposition.</param>
        public WindowDragManipulator(VisualElement header, VisualElement window) {
            target  = header;
            _window = window;
        }

        protected override void RegisterCallbacksOnTarget() {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        protected override void UnregisterCallbacksFromTarget() {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        void OnPointerDown(PointerDownEvent e) {
            if (e.button != 0) return;
            _isDragging      = true;
            _dragStartPointer = e.position;
            _windowStartPos  = new Vector2(_window.resolvedStyle.left, _window.resolvedStyle.top);
            target.CapturePointer(e.pointerId);
            e.StopPropagation();
        }

        void OnPointerMove(PointerMoveEvent e) {
            if (!_isDragging || !target.HasPointerCapture(e.pointerId)) return;
            var delta = (Vector2)e.position - _dragStartPointer;
            _window.style.left = _windowStartPos.x + delta.x;
            _window.style.top  = _windowStartPos.y + delta.y;
        }

        void OnPointerUp(PointerUpEvent e) {
            if (!_isDragging) return;
            _isDragging = false;
            target.ReleasePointer(e.pointerId);
            e.StopPropagation();
        }

        void OnPointerCaptureOut(PointerCaptureOutEvent e) => _isDragging = false;
    }
}
