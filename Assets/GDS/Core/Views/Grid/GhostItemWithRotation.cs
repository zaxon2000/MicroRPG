using UnityEngine;
using UnityEngine.UIElements;

namespace GDS.Core {
    public class GhostItemWithRotation : GridItemView {
        string lastItemId;
        Direction lastDirection;
        int lastAngle = 0;

        override public void Render() {
            if (item == null) return;

            if (item is not ShapeItem shapeItem) {
                image.RemoveFromClassList("item-image-transition");
                image.sprite = item.Icon;
                image.SetSize(Item.Size(), CellSize);
                image.Rotate(0);
                quant.SetVisible(item.Base.Stackable);
                quant.text = item.StackSize.ToString();
                quant.Translate(item.Size(), CellSize / 2);
                return;
            }

            if (lastItemId == item.Id && lastDirection != shapeItem.Direction) {
                var newAngle = lastAngle + 90;
                image.Rotate(newAngle);
                quant.Translate(shapeItem.Size, CellSize / 2);
                lastAngle = newAngle;
                lastDirection = shapeItem.Direction;
                return;
            }

            lastItemId = item.Id;
            lastAngle = (int)shapeItem.Direction;
            lastDirection = shapeItem.Direction;

            image.RemoveFromClassList("item-image-transition");
            image.sprite = item.Icon;
            image.SetSize(shapeItem.BaseSize, CellSize);
            image.Rotate(lastAngle);
            image.style.transformOrigin = new TransformOrigin(new Length(50, LengthUnit.Percent), new Length(50, LengthUnit.Percent));
            image.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));

            // Add transition on the next frame
            schedule.Execute(() => { image.WithClass("item-image-transition"); }).ExecuteLater(50);

            quant.SetVisible(item.Base.Stackable);
            quant.text = item.StackSize.ToString();
            quant.Translate(shapeItem.Size, CellSize / 2);
        }

    }
}