using System;
using System.Collections.Generic;
using System.Linq;
using GDS.Core.Events;
using UnityEngine;

namespace GDS.Core {
    [Serializable]
    public class GridBag : Bag {
        public event Action<Item> ItemChanged;
        public event Action<Item> ItemAdded;
        public event Action<Item> ItemRemoved;
        public event Action CollectionChanged;
        public event Action CollectionReset;

        public int CellSize = 64;
        public GridMath.FillDirection FillDirection = GridMath.FillDirection.Horizontal;
        public Size Size = new(10, 6);

        public GridSlot[,] Slots;
        public int[,] Occupancy;
        public List<GridItem> GridItems;
        [SerializeReference]
        public List<Item> InitialState = new();

        public bool Initialized => GridItems != null;

        public override IEnumerable<Item> Items => GridItems.Select(gi => gi.Item);



        protected void NotifyAdded(Item item) {
            ItemAdded?.Invoke(item);
            CollectionChanged?.Invoke();
        }

        protected void NotifyRemoved(Item item) {
            ItemRemoved?.Invoke(item);
            CollectionChanged?.Invoke();
        }

        protected void NotifyChanged(Item item) {
            ItemChanged?.Invoke(item);
            CollectionChanged?.Invoke();
        }

        public override void Init() {
            Slots = GridMath.CreateMatrix(Size, pos => new GridSlot() { Pos = pos });
            Occupancy = new int[Size.H, Size.W];
            GridItems = new();
            InitialState.ForEach(i => AddNoNotify(i));
        }

        public override void Reset() {
            if (Occupancy == null) return;
            GridItems.Clear();
            for (var i = 0; i < Size.H; i++)
                for (var j = 0; j < Size.W; j++) {
                    Slots[i, j].Clear();
                    Occupancy[i, j] = 0;
                }
            CollectionReset?.Invoke();
        }

        public override Result CanAdd(Item item) {
            if (!Accepts(item)) return Result.Fail;
            var pos = GridMath.TryFitItemIntoGrid(Occupancy, item.Shape(), FillDirection);
            if (pos == null) return new BagFull(this);
            return Result.Success;
        }

        Pos AddNoNotify(Item item) {
            if (item == null) { Debug.LogWarning($"Warning: Trying to add a null item to {this}!"); return null; }
            var pos = GridMath.TryFitItemIntoGrid(Occupancy, item.Shape(), FillDirection);
            if (pos == null) return null;
            GridItems.Add(new GridItem(item, pos));
            UpdateSlotsAndOccupancy(pos, item.Shape(), item);
            return pos;
        }

        public override Result Add(Item item) {
            if (item == null) { Debug.LogWarning($"Warning: Trying to add a null item to {this}!"); return Result.Fail; }
            Pos pos = AddNoNotify(item);
            if (pos == null) return new BagFull(this);
            NotifyAdded(item);
            return Result.Success;
        }

        public override Result AddAt(Slot slot, Item item) {
            if (slot is not GridSlot s) return Result.Fail;
            if (!Accepts(item)) return Result.Fail;

            var computedShape = GridMath.ComputedShape(Occupancy, item.Shape(), s.Pos);
            var overlapping = GetOverlappingItems(computedShape, s.Pos);
            var count = overlapping.Count();
            if (count >= 2) return Result.Fail;

            Item replaced = null;
            if (count == 1) replaced = overlapping.ElementAt(0);

            // Stacking
            if (replaced != null && AllowStacking() && ItemExt.CanStack(item, replaced)) {
                // Debug.Log($"should transfer all from {item} to {replaced}");
                return TransferAll(item, slot, replaced);
            }

            if (replaced != null) {
                var replacedGridItem = FindGridItem(replaced);
                GridItems.Remove(replacedGridItem);
                UpdateSlotsAndOccupancy(replacedGridItem.Pos, replacedGridItem.Item.Shape(), null);
                NotifyRemoved(replaced);
            }

            var gridItem = new GridItem(item, s.Pos);
            // Debug.Log($"should add grid item at {s.Pos}");
            GridItems.Add(gridItem);
            UpdateSlotsAndOccupancy(s.Pos, item.Shape(), item);
            NotifyAdded(gridItem.Item);

            return new PlaceItemSuccess(item, replaced);
        }

        // In a GridBag more than 1 item can have the same grid position (due to custom shapes),
        // therefore we cannot rely on slot position but instead must find the item in the items list
        // TODO: Replace with list with dict to improve lookup?
        public override Result Remove(Item item) {
            // Debug.Log($"should remove item {item} from grid bag");
            var index = GridItems.FindIndex(gi => gi.Item == item);
            // Debug.Log($"item index: {index}");
            if (index == -1) return Result.Fail;
            var gridItem = GridItems[index];
            GridItems.RemoveAt(index);
            UpdateSlotsAndOccupancy(gridItem.Pos, gridItem.Item.Shape(), null);

            NotifyRemoved(item);
            return new PickItemSuccess(gridItem.Item);
        }

        // TODO: validate pos
        public Slot GetSlotAt(Pos pos) => Slots[pos.Y, pos.X];
        public Item GetItemAt(Pos pos) => GetSlotAt(pos).Item;
        public GridItem FindGridItem(Item item) => GridItems.Find(gi => gi.Item == item);
        public override Slot FindSlot(Item item) {
            if (FindGridItem(item) is not GridItem gi) return null;
            return GetSlotAt(gi.Pos);

        }

        /// <summary>
        /// Updates the Slots and Occupancy matrix
        /// Used when removing an item (slots set to NoItem, passed as param, and occupancy values set to 0)
        /// or adding an item (slots set to Item, passed as param, and occupancy values set to 1)
        /// </summary>
        public void UpdateSlotsAndOccupancy(Pos pos, int[,] shape, Item item) {
            var size = GridMath.GetSize(shape);
            var slotValue = item is null ? 0 : 1;
            for (var i = 0; i < size.H; i++)
                for (var j = 0; j < size.W; j++) {
                    if (shape[i, j] == 0) continue;
                    Slots[i + pos.Y, j + pos.X].Item = item;
                    Occupancy[i + pos.Y, j + pos.X] = slotValue;
                }
        }

        public Pos CreateGridGhostPos(Vector3 localPos, Size itemSize) {
            var (W, H) = itemSize;
            if (W > Size.W || H > Size.H) return null;
            var (hw, hh) = (W * CellSize / 2, H * CellSize / 2);
            int x = (int)Math.Round((localPos.x - hw) / CellSize);
            int y = (int)Math.Round((localPos.y - hh) / CellSize);
            int xClamp = Math.Clamp(x, 0, Size.W - W);
            int yClamp = Math.Clamp(y, 0, Size.H - H);
            return new Pos(xClamp, yClamp);
        }

        public IEnumerable<Item> GetOverlappingItems(int[,] shape, Pos offset) {
            var (x, y) = offset;
            var (h, w) = shape.GetLength2D();
            var list = new HashSet<Item>();
            for (var i = 0; i < h; i++) {
                for (var j = 0; j < w; j++) {
                    if (shape[i, j] <= 1) continue;
                    // This is redundant, but who knows
                    if (Slots[i + y, j + x].Empty()) continue;
                    list.Add(Slots[i + y, j + x].Item);
                }
            }
            return list;
        }

        public override Result TransferAll(Item fromItem, Slot _, Item toItem) {
            if (!Accepts(fromItem)) return Result.Fail;
            if (!AllowStacking()) return Result.Fail;
            ItemExt.TransferAll(fromItem, toItem);
            NotifyChanged(toItem);
            return new PlaceItemSuccess(toItem, fromItem.StackSize > 0 ? fromItem : null);
        }

        public override Result TransferOne(Item fromItem, Slot toSlot, Item _) {
            if (!fromItem.Stackable) return Result.Fail;
            Debug.Log($"Should transfer one from {fromItem} to slot: {toSlot}");
            if (toSlot is not GridSlot s) return Result.Fail;
            if (!Accepts(fromItem)) return Result.Fail;
            if (!AllowStacking()) return Result.Fail;
            var computedShape = GridMath.ComputedShape(Occupancy, fromItem.Shape(), s.Pos);
            var overlapping = GetOverlappingItems(computedShape, s.Pos);
            var count = overlapping.Count();
            if (count >= 2) return Result.Fail;

            Item replaced = null;
            if (count == 1) replaced = overlapping.ElementAt(0);

            // Stacking
            if (replaced != null && ItemExt.CanStack(fromItem, replaced)) {
                // Debug.Log($"should transfer all from {item} to {replaced}");
                var (newFromItem, newToItem) = ItemExt.TransferOne(fromItem, replaced);
                NotifyChanged(replaced);
                return new PlaceItemSuccess(newToItem, newFromItem.StackSize > 0 ? newFromItem : null);
            }

            if (replaced == null) {
                var (newFromItem, newToItem) = ItemExt.TransferOne(fromItem, null);
                AddAt(toSlot, newToItem);
                return new PlaceItemSuccess(newToItem, newFromItem.StackSize > 0 ? newFromItem : null);
            }

            return Result.Fail;
        }

        public override Result SplitHalf(Item item) {
            if (!item.Stackable) return Result.Fail;
            var gridItem = FindGridItem(item);
            if (gridItem == null) return Result.Fail;
            if (!AllowStacking()) return Result.Fail;
            var (newFromItem, newToItem) = ItemExt.SplitHalf(item);
            gridItem.Item = newFromItem.StackSize > 0 ? newFromItem : null;
            NotifyChanged(gridItem.Item);
            return new PickItemSuccess(newToItem);
        }
    }
}