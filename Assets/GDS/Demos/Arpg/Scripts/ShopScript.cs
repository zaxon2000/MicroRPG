using GDS.Core;
using GDS.Core.Events;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GDS.Demos.Arpg {

    public class ShopScript : MonoBehaviour, IPointerClickHandler {

        [Required]
        public Arpg_Store Store;

        [Space(12)]
        public Shop Bag;

        private void Awake() {
            Bag.PlayerGold = Store.PlayerGold;
        }

        public void OnPointerClick(PointerEventData eventData) {
            Store.Bus.Publish(new OpenWindow(Bag));
        }
    }

}