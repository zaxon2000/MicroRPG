using UnityEngine;
using GDS.Core;
using GDS.Core.Events;

namespace GDS.Examples {

    [CreateAssetMenu(menuName = "SO/Examples/Arc/Arc_Store")]
    public class Arc_Store : Store {

        public Arc_Store() {
            Bus.On<PickItem>(OnPickItem);
            Bus.On<PlaceItem>(OnPlaceItem);

            Bus.On<OpenWindow>(OnOpenWindow);
            Bus.On<CloseWindow>(OnCloseWindow);

            Bus.On<ToggleUI>(OnToggleUI);
            Bus.On<CloseUI>(OnCloseUI);
        }

        public Observable<bool> UiOpen = new(false);
        public Observable<object> SideWindow = new(null);

        public override void Reset() {
            base.Reset();
            UiOpen.Reset();
            SideWindow.Reset();
        }

        void OnPickItem(PickItem e) {
            Result result = e.Bag.Remove(e.Item);
            UpdateGhost(result);
            Bus.Publish(result);
        }

        void OnPlaceItem(PlaceItem e) {
            var result = e.Bag.AddAt(e.Slot, e.Item);
            UpdateGhost(result);
            Bus.Publish(result);
        }

        void OnOpenWindow(OpenWindow e) {
            LogUtil.LogEvent(e);
            SideWindow.SetValue(e.Handle);
            UiOpen.SetValue(true);
            if (e.Handle is Arc_Chest bag) bag.Open();
        }

        void OnCloseWindow(CloseWindow e) {
            LogUtil.LogEvent(e);
            if (SideWindow.Value is Arc_Chest bag) bag.Close();
            SideWindow.SetValue(null);
        }

        void OnToggleUI(ToggleUI e) {
            UiOpen.Toggle();
            if (UiOpen.Value == false) {
                if (SideWindow.Value is Arc_Chest bag) bag.Close();
                SideWindow.SetValue(null);
            }
        }

        void OnCloseUI(CloseUI e) {
            UiOpen.SetValue(false);
            if (SideWindow.Value is Arc_Chest bag) bag.Close();
            SideWindow.SetValue(null);
        }


    }


}