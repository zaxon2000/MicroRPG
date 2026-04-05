using System.Linq;
using GDS.Core;
using GDS.Core.Events;

namespace GDS.Examples {

    public class Arc_ChestWindowView : WindowView {
        public Arc_ChestWindowView(Arc_Chest bag, Store store) {
            Init(bag.Name, () => store.Bus.Publish(new CloseWindow(bag)));

            var gridBagView = new GridBagView() { CreateItemView = () => new Arc_GridItemView() };
            gridBagView.Init(bag, store.Ghost);

            var identifyText = Dom.Label("", IdentifyLabelText(bag)).SetVisible(bag.HasUnidentifiedItems);
            Container.Add(identifyText, gridBagView);

            bag.IdentifyStarted += item => {
                gridBagView.ItemViewsDict.TryGetValue(item, out var itemView);
                // Trigger progress animation on the next frame (workaround for some sort of bug)
                itemView.schedule.Execute(() => itemView.AddToClassList("show-progress")).ExecuteLater(10);
            };

            bag.IdentifyCompleted += item => {
                if (!bag.HasUnidentifiedItems) identifyText.Hide();
                else identifyText.text = IdentifyLabelText(bag);

            };
        }

        string IdentifyLabelText(Arc_Chest bag) => $"Identifying items: {bag.UnidentifiedItems.Count()} left";
    }

}