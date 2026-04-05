using GDS.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GDS.Examples {

    public class Arc_GridItemView : BaseGridItemView {
        VisualElement shape;
        VisualElement UnidentifiedOverlay;
        VisualElement IdentifyProgress;

        public Arc_GridItemView() {
            this.Add("item-view",
                shape = Dom.Div("absolute item-shape"),
                image = new Image().WithClass("item-image"),
                quant = new Label().WithClass("item-quant"),
                UnidentifiedOverlay = Dom.Div("cover unidentified-item place-items-center", Dom.Label("", "searching...")),
                IdentifyProgress = Dom.Div("identify-progress")
            ).PickIgnoreAll();
        }

        override public void Render() {

            if (Item is not Arc_Item i) { Debug.LogWarning("Item needs to be Arc_Item"); return; }

            this.SetSize(Item.Size(), CellSize);

            if (i.IsIdentified) {
                UnidentifiedOverlay.Hide();
                IdentifyProgress.Hide();
            }

            image.sprite = i.Icon;
            image.SetSize(i.BaseSize, CellSize);
            image.Rotate((float)i.Direction);
            image.Translate(GridMath.AdjustPosForSizeAndDir(i.BaseSize, i.Direction), CellSize);

        }
    }

}