### Minimal Grid Example

This example contains the minimal setup required to display a grid-based inventory.

It reuses the `SplitItemStack_Store` from a previous example which enabled stack splitting.

The **UXML Document** includes a GridBagView.

The **Controller** wires up the GridBag with the GridBagView.

In addition to the usual **Manipulators**, the **Controller** also attaches a `RotateGhostManipulator` to the root element. This **Manipulator** activates on right-click and rotates the dragged item 90° clockwise.

To properly display the dragged item as a grid item with rotation support, we pass a custom item renderer to the `DragDropManipulator` :

```cs
root.AddManipulator(new DragDropManipulator(Store, new GhostItemWithRotation() { CellSize = bag.CellSize }));
```

Similarly, to correctly render irregular item shapes in the grid, we assign a custom item view factory to the **GridBagView**:

```cs
gridBagView.CreateItemView = () => new IrregularGridItemView();
```

> Note: You don't need a custom item renderer if your items have regular shapes. See **ARPG Demo** for an example.

Finally, if we're using **Unity 2022**, we add an **EventSystem** to the scene hierarchy. It allows processing right-clicks while holding down left-click. This is **not required** in **Unity 6**.