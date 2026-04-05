using GDS.Core;
using UnityEngine.UIElements;

namespace GDS.Examples {

    public class QuickSlot_SlotView : SlotView {

        Label shortcutLabel = new Label().WithClass("shortcut-label");

        public QuickSlot_SlotView() {
            itemView = new QuickSlot_ItemView();
            Clear();
            this.Add("slot", itemView, shortcutLabel, overlay, debug);
        }

        override public void Render() {
            shortcutLabel.text = KeyText(slot);
            itemView.Item = Item;
            EnableInClassList("empty", slot.Empty());
        }


        string KeyText(Slot slot) => slot switch {
            ListSlot s => (s.Index + 1).ToString(),
            SetSlot s => s.Key,
            _ => ""
        };
    }

}