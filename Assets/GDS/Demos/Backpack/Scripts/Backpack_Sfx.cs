using UnityEngine;
using GDS.Core;
using GDS.Core.Events;

namespace GDS.Demos.Backpack {
    [RequireComponent(typeof(AudioSource))]
    public class Backpack_Sfx : MonoBehaviour {
        [System.Serializable]
        public class SoundList {
            public AudioClip Fail;
            public AudioClip Pick;
            public AudioClip Place;
            public AudioClip Rotate;
            public AudioClip Buy;
            public AudioClip Sell;
        }

        [Required]
        public Store store;
        [Space(12)]
        public SoundList Sounds;

        AudioSource audioSource;

        void Awake() {
            audioSource = GetComponent<AudioSource>();
        }
        void OnEnable() {
            store.Bus.OnAny<Result>(PlaySound);
        }
        void OnDisable() {
            store.Bus.OffAny<Result>(PlaySound);
        }

        void PlaySound(Result result) {
            var clip = GetClip(result);
            if (clip == null) return;
            audioSource.pitch = Random.Range(0.85f, 1.05f);
            audioSource.PlayOneShot(clip);
        }

        AudioClip GetClip(Result result) => result switch {
            Fail => Sounds.Fail,
            BuyItemSuccess => Sounds.Buy,
            SellItemSuccess => Sounds.Sell,
            PickItemSuccess => Sounds.Pick,
            PlaceItemSuccess => Sounds.Place,
            RotateItem => Sounds.Rotate,
            _ => null
        };

    }
}

