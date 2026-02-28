using GDS.Core;
using GDS.Core.Events;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GDS.Demos.Arpg {

    public class StashScript : MonoBehaviour, IPointerClickHandler {

        [Required]
        public Store Store;

        [Space(12)]
        public Stash Bag;

        public void OnPointerClick(PointerEventData eventData) {
            Store.Bus.Publish(new OpenWindow(Bag));
        }
    }

}