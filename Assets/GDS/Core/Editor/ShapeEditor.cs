using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GDS.Core {

    [CustomEditor(typeof(Shape), true)]
    public class ShapeEditor : Editor {

        public override VisualElement CreateInspectorGUI() {
            var root = Dom.Div();
            EditorUtil.AddDefaultEditorStylesheet(root);
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            var dataField = root.Q<PropertyField>(name = "PropertyField:Data");
            dataField.SetEnabled(false);

            var shapeText = new Label { text = "Shape preview: " + "(click a cell to toggle)".Gray(), style = { marginTop = 24 } };
            root.Add(shapeText);

            var sh = target as Shape;
            var shapeContainer = new VisualElement { style = { marginLeft = 12, marginTop = 12 } };
            root.Add(shapeContainer);

            shapeContainer.RegisterCallback<PointerDownEvent>(e => {
                if (e.target is not ShapeCell cell) return;
                sh.Toggle(cell.x, cell.y);
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            });

            var hashField = root.Q<PropertyField>(name = "PropertyField:hash");
            hashField.SetEnabled(false);
            hashField.RegisterValueChangeCallback(_ => Redraw());


            void Redraw() {
                // Debug.Log("should redraw");
                var shapeEl = DrawShape(sh);
                shapeContainer.Clear();
                shapeContainer.Add(shapeEl);
            }

            return root;
        }

        VisualElement DrawShape(Shape shape) {
            var cellSize = 32;
            var gap = 4;
            var el = new VisualElement { style = { width = shape.Width * cellSize + (shape.Width - 1) * gap, height = shape.Height * cellSize + (shape.Height - 1) * gap } };
            for (var i = 0; i < shape.Height; i++) {
                for (var j = 0; j < shape.Width; j++) {
                    var cell = new ShapeCell {
                        x = j,
                        y = i,
                        style = { width = cellSize, height = cellSize, translate = new Translate(j * cellSize + (j - 1) * gap, i * cellSize + (i - 1) * gap) }
                    };
                    cell.AddToClassList("shape-cell");
                    cell.EnableInClassList("full", shape.Data[i * shape.Width + j] == 1);
                    el.Add(cell);
                }
            }

            return el;
        }

        class ShapeCell : VisualElement { public int x, y; }

    }
}