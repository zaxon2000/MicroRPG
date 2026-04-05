using System;
using System.Collections.Generic;
using System.Linq;
using GDS.Core.Events;
using UnityEngine;

namespace GDS.Core {
    [Serializable]
    public class GridBag : Bag {
        /// <summary>
        /// Event trigerred when an item has changed.
        /// </summary>
        public event Action<Item> ItemChanged;

        /// <summary>
        /// Event trigerred when an item was added.
        /// </summary>
        public event Action<Item> ItemAdded;

        /// <summary>
        /// Event trigerred when an item was removed.
        /// </summary>
        public event Action<Item> ItemRemoved;

        /// <summary>
        /// Event trigerred when collection has been changed.
        /// </summary>
        public event Action CollectionChanged;

        /// <summary>
        /// Event trigerred when the collection has changed substantially. Typically requires a full redraw.
        /// </summary>
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

        // TODO: validate pos
        public Slot GetSlotAt(Pos pos) => Slots[pos.Y, pos.X];
        public Item GetItemAt(Pos pos) => GetSlotAt(pos).Item;
        public GridItem FindGridItem(Item item) => GridItems.Find(gi => gi.Item == item);
        public override Slot FindSlot(Item item) {
            if (FindGridItem(item) is not GridItem gi) return null;
            return GetSlotAt(gi.Pos);
        }

        public void NotifyAdded(Item item) {
            ItemAdded?.Invoke(item);
            NotifyChanged();
        }

        public void NotifyRemoved(Item item) {
            ItemRemoved?.Invoke(item);
            NotifyChanged();
        }

        public void NotifyChanged(Item item) {
            ItemChanged?.Invoke(item);
            NotifyChanged();
        }

        public void NotifyChanged() {
            CollectionChanged?.Invoke();
        }

        public void NotifyReset() {
            CollectionReset?.Invoke();
        }

        public void Init() {
            Slots = GridMath.CreateMatrix(Size, pos => new GridSlot() { Pos = pos });
            Occupancy = new int[Size.H, Size.W];
            GridItems = new();
            InitialState.ForEach(i => AddNoNotify(i));
        }

        /// <summary>
        /// Clears the bag.
        /// </summary>
        public override void Clear() {
            if (Occupancy == null) return;
            GridItems.Clear();
            for (var i = 0; i < Size.H; i++)
                for (var j = 0; j < Size.W; j++) {
                    Slots[i, j].Clear();
                    Occupancy[i, j] = 0;
                }
            NotifyReset();
        }

        /// <summary>
        /// Checks whether the bag can accept the item and has available capacity.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>ItemNotAccepted if bag does not accept the item. ItemCannotFit if the bag doesn't have enough capacity. Success if the item can be added.</returns>
        public override Result CanAdd(Item item) {
            if (!Accepts(item)) return Result.ItemNotAccepted;
            var pos = GridMath.TryFitItemIntoGrid(Occupancy, item.Shape(), FillDirection);
            if (pos == null) return Result.ItemCannotFit;
            return Result.Success;
        }

        /// <summary>
        /// Adds an item to the bag (without notifying subscribers)
        /// </summary>
        /// <param name="item">The item to add</param>
        /// <returns>Item position in the grid if it was added; null otherwise.</returns>
        Pos AddNoNotify(Item item) {
            if (item == null) { Debug.LogWarning($"Warning: Trying to add a null item to {this}!"); return null; }
            var pos = GridMath.TryFitItemIntoGrid(Occupancy, item.Shape(), FillDirection);
            if (pos == null) return null;
            GridItems.Add(new GridItem(item, pos));
            UpdateSlotsAndOccupancy(pos, item.Shape(), item);
            return pos;
        }

        /// <summary>
        /// Adds an item to the bag
        /// </summary>
        /// <param name="item">The item to add</param>
        /// <returns>ItemNotAccepted if bag does not accept the item. ItemCannotFit if the bag doesn't have enough capacity. PlaceItemSuccess if the item was added.</returns>
        public override Result Add(Item item) {
            if (item == null) { Debug.LogWarning($"Warning: Trying to add a null item to {this}!"); return Result.Fail; }
            if (!Accepts(item)) return Result.ItemNotAccepted;
            Pos pos = AddNoNotify(item);
            if (pos == null) return Result.ItemCannotFit;
            NotifyAdded(item);
            return new PlaceItemSuccess(item, null);
        }

        /// <summary>
        /// Adds an item to the specified slot. Can replace an existing item or move a stack to target slot.
        /// </summary>
        /// <param name="slot">Target slot</param>
        /// <param name="item">Item to add</param>
        /// <returns>PlaceItemSuccess or Fail</returns>
        public override Result AddAt(Slot slot, Item item) {
            if (slot is not GridSlot s) return Result.WrongSlotType;
            if (!Accepts(item)) return Result.ItemNotAccepted;

            var computedShape = GridMath.ComputedShape(Occupancy, item.Shape(), s.Pos);
            var overlapping = GetOverlappingItems(computedShape, s.Pos);
            var count = overlapping.Count();
            if (count >= 2) return Result.Fail;

            Item replaced = null;
            if (count == 1) replaced = overlapping.ElementAt(0);

            // Stacking
            if (replaced != null && AllowStacking() && item.CanStack(replaced)) {
                return TransferAll(item, slot, replaced);
            }

            if (replaced != null) {
                var replacedGridItem = FindGridItem(replaced);
                GridItems.Remove(replacedGridItem);
                UpdateSlotsAndOccupancy(replacedGridItem.Pos, replacedGridItem.Item.Shape(), null);
                NotifyRemoved(replaced);
            }

            var gridItem = new GridItem(item, s.Pos);
            GridItems.Add(gridItem);
            UpdateSlotsAndOccupancy(s.Pos, item.Shape(), item);
            NotifyAdded(gridItem.Item);

            return new PlaceItemSuccess(item, replaced);
        }


        /// <summary>
        /// Removes the item from the bag
        /// </summary>
        /// <param name="item">Item to remove</param>
        /// <returns>PickItemSuccess or Fail</returns>
        public override Result Remove(Item item) {
            // In a GridBag more than 1 item can have the same grid position (due to custom shapes and rotation),
            // therefore we cannot rely on slot position but instead must find the item in the items list
            // TODO: Replace with list with dict to improve lookup?
            var index = GridItems.FindIndex(gi => gi.Item == item);
            if (index == -1) return Result.Fail;
            var gridItem = GridItems[index];
            GridItems.RemoveAt(index);
            UpdateSlotsAndOccupancy(gridItem.Pos, gridItem.Item.Shape(), null);
            NotifyRemoved(item);
            return new PickItemSuccess(gridItem.Item);
        }

        /// <summary>
        /// Transfers the whole stack from source item to target item (up to max stack) 
        /// </summary>
        /// <returns>PlaceItemSuccess or Fail</returns>
        public override Result TransferAll(Item fromItem, Slot _, Item toItem) {
            if (!Accepts(fromItem)) return Result.ItemNotAccepted;
            if (!AllowStacking()) return Result.StackingNotAllowed;
            var (newFromItem, newToItem) = fromItem.TransferAll(toItem);
            NotifyChanged(newToItem);
            return new PlaceItemSuccess(newToItem, newFromItem);
        }

        /// <summary>
        /// Transfers one from a source item to target slot
        /// </summary>
        /// <returns>PlaceItemSuccess or Fail</returns>
        public override Result TransferOne(Item fromItem, Slot toSlot, Item _) {
            if (!fromItem.Stackable) return Result.Fail;
            if (toSlot is not GridSlot s) return Result.WrongSlotType;
            if (!Accepts(fromItem)) return Result.ItemNotAccepted;
            if (!AllowStacking()) return Result.StackingNotAllowed;
            var computedShape = GridMath.ComputedShape(Occupancy, fromItem.Shape(), s.Pos);
            var overlapping = GetOverlappingItems(computedShape, s.Pos);

            var count = overlapping.Count();
            if (count >= 2) return Result.Fail;

            Item replaced = null;
            if (count == 1) replaced = overlapping.ElementAt(0);

            // Stacking
            if (replaced != null && fromItem.CanStack(replaced)) {
                var (newFromItem, newToItem) = fromItem.TransferOne(replaced);
                NotifyChanged(replaced);
                return new PlaceItemSuccess(newToItem, newFromItem);
            }

            // Nothing to replace
            if (replaced == null) {
                var (newFromItem, newToItem) = fromItem.TransferOne(null);
                if (newFromItem is ShapeItem nf && newToItem is ShapeItem nt) { nt.Direction = nf.Direction; }
                AddAt(toSlot, newToItem);
                return new PlaceItemSuccess(newToItem, newFromItem);
            }

            return Result.Fail;
        }

        /// <summary>
        /// Splits a stack of items in half
        /// </summary>
        /// <param name="item">The item to split</param>
        /// <returns>PickItemSuccess or Fail</returns>
        public override Result SplitHalf(Item item) {
            if (!item.Stackable) return Result.Fail;
            var gridItem = FindGridItem(item);
            if (gridItem == null) return Result.Fail;
            if (!AllowStacking()) return Result.StackingNotAllowed;
            if (item.StackSize == 1) return Remove(item);

            var (newFromItem, newToItem) = item.SplitHalf();
            gridItem.Item = newFromItem;
            NotifyChanged(gridItem.Item);
            return new PickItemSuccess(newToItem);

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

        /// <summary>
        /// Computes grid ghost position based on ghost item size and local pointer position.
        /// </summary>
        /// <param name="localPos">Local Pointer position</param>
        /// <param name="itemSize">Ghost item size</param>
        /// <returns>Pos if the item fits; null otherwise</returns>
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

        /// <summary>
        /// Computes the overlap between bag items and ghost item
        /// </summary>
        /// <param name="shape">Ghost item shape</param>
        /// <param name="offset">Ghost item position</param>
        /// <returns>A list of items</returns>
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

    }
}