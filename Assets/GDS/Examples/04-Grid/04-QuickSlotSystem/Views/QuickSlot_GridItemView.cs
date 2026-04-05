using GDS.Core;
using UnityEngine.UIElements;

namespace GDS.Examples {

    public class QuickSlot_GridItemView : BaseGridItemView {

        public QuickSlot_GridItemView() {
            this.Add("item-view",
                Dom.Div("absolute item-shape"),
                image = new Image().WithClass("item-image"),
                quant = new Label().WithClass("item-quant")
            ).PickIgnoreAll();
        }

        override public void Render() {
            if (Item is not QuickSlot_Item shapeItem) return;

            this.SetSize(Item.Size(), CellSize);

            image.sprite = Item.Icon;
            image.SetSize(shapeItem.BaseSize, CellSize);
            image.Rotate((float)shapeItem.Direction);
            image.Translate(GridMath.AdjustPosForSizeAndDir(shapeItem.BaseSize, shapeItem.Direction), CellSize);

            var quantText = Item is QuickSlot_Weapon w ? w.Ammo : Item.StackSize;
            var quantTextVisible = Item is QuickSlot_Weapon || Item.Stackable;
            quant.text = quantText.ToString();
            quant.SetVisible(quantTextVisible);
        }
    }

}