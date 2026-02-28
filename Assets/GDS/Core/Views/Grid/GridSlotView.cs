using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GDS.Core {

    public class GridSlotView : VisualElement {
        Label debugLabel = Dom.Label("debug-label", "");
        public GridSlotView() {
            this.Add("slot", Dom.Div("slot-overlay"), debugLabel);
        }

        public void Render(GridSlot slot) {
            debugLabel.text = $"{slot.Pos}\n{slot.Item}";
        }
    }
}
