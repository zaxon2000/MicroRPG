using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDS.Core;
using GDS.Core.Events;
using UnityEngine;

namespace GDS.Examples {

    [Serializable]
    public class Arc_Chest : GridBag {

        public IEnumerable<Arc_Item> UnidentifiedItems => Items.Select(item => item as Arc_Item).Where(i => i != null && i.IsIdentified == false);
        public bool HasUnidentifiedItems => UnidentifiedItems.Count() > 0;

        public event Action<Arc_Item> IdentifyStarted;
        public event Action<Arc_Item> IdentifyCompleted;

        private CancellationTokenSource cts;
        private Task idTask;

        public int IdentifyDuration = 3000;

        public override bool Accepts(Item item) {
            return HasUnidentifiedItems == false;
        }

        public override Result Remove(Item item) {
            if (HasUnidentifiedItems) return Result.Fail;
            return base.Remove(item);
        }

        public void Open() {
            if (!HasUnidentifiedItems) return; // all identified
            if (idTask != null && !idTask.IsCompleted) return; // already running
            cts = new CancellationTokenSource();
            idTask = IdentifySequence(cts.Token);
        }

        public void Close() {
            cts.Cancel();
            Debug.Log($"identifying cancelled, {UnidentifiedItems.Count()} remaining...");
        }

        public void Dispose() {
            cts?.Cancel();
            cts?.Dispose();
            Debug.Log("identifying cancelled, disposed");
        }

        private async Task IdentifySequence(CancellationToken token) {
            foreach (var item in UnidentifiedItems) {
                await IdentifyOne(item, token);
            }
            Debug.Log($"all items identified!:".Green());

        }

        private async Task IdentifyOne(Arc_Item item, CancellationToken token) {
            token.ThrowIfCancellationRequested();
            Debug.Log($"started identifying: {item}");
            IdentifyStarted?.Invoke(item);
            await Task.Delay(IdentifyDuration, token);
            Debug.Log($"finished identifying: {item}");
            item.IsIdentified = true;
            IdentifyCompleted?.Invoke(item);
            NotifyChanged(item);
        }
    }

}