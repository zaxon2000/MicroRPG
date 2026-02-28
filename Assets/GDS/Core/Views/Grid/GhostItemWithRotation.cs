using UnityEngine;
using UnityEngine.UIElements;

namespace GDS.Core {
    public class GhostItemWithRotation : ItemView {

        public int CellSize = 64;

        string lastItemId;
        Direction lastDirection;
        int lastAngle = 0;

        override public void Render() {
            if (Item is not ShapeItem item) {
                image.RemoveFromClassList("item-image-transition");
                image.sprite = Item.Icon;
                image.SetSize(Item.Size(), CellSize);
                image.Rotate(0);
                quant.SetVisible(Item.Base.Stackable);
                quant.text = Item.StackSize.ToString();
                quant.Translate(Item.Size(), CellSize / 2);
                return;
            }

            if (lastItemId == item.Id && lastDirection != item.Direction) {
                var newAngle = lastAngle + 90;
                image.Rotate(newAngle);
                quant.Translate(item.Size, CellSize / 2);
                lastAngle = newAngle;
                lastDirection = item.Direction;
                return;
            }

            lastItemId = item.Id;
            lastAngle = (int)item.Direction;
            lastDirection = item.Direction;

            image.RemoveFromClassList("item-image-transition");
            image.sprite = item.Icon;
            image.SetSize(item.BaseSize, CellSize);
            image.Rotate(lastAngle);
            image.style.transformOrigin = new TransformOrigin(new Length(50, LengthUnit.Percent), new Length(50, LengthUnit.Percent));
            image.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));

            // Add transition on the next frame
            schedule.Execute(() => { image.WithClass("item-image-transition"); }).ExecuteLater(50);

            quant.SetVisible(item.Base.Stackable);
            quant.text = item.StackSize.ToString();
            quant.Translate(item.Size, CellSize / 2);
        }

    }
}