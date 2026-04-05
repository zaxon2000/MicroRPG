using GDS.Core;
using UnityEngine.UIElements;

namespace GDS.Examples {

    public class QuickSlot_ItemView : ItemView {

        public QuickSlot_ItemView() {
            Clear();
            ClearClassList();
            this.Add("custom-item-view",
                image = new Image().WithClass("item-image"),
                quant = new Label().WithClass("item-quant")
            ).PickIgnoreAll();
        }

        override public void Render() {
            if (Item == null) { this.Hide(); return; }

            this.Show();
            image.sprite = Item.Icon;
            var quantText = Item is QuickSlot_Weapon w ? w.Ammo : Item.StackSize;
            var quantTextVisible = Item is QuickSlot_Weapon || Item.Stackable;
            quant.text = quantText.ToString();
            quant.SetVisible(quantTextVisible);
        }
    }

}