using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// GraphView that visualizes and edits a DialogueData asset as a node tree.
/// Supports creating/deleting nodes, connecting response ports, and serializing
/// the graph back to the ScriptableObject.
/// </summary>
public class DialogueGraphView : GraphView
{
    private const float DefaultNewNodeOffsetX = 50f;
    private const float DefaultNewNodeOffsetY = 50f;
    private const float DefaultNodeSpacingX = 320f;

    public DialogueGraphView()
    {
        // Standard manipulators for pan, zoom, selection
        SetupZoom(0.1f, 3f);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        // Grid background
        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        // Minimap
        var minimap = new MiniMap { anchored = true };
        minimap.SetPosition(new Rect(10, 30, 200, 140));
        Add(minimap);

        style.flexGrow = 1;
    }

    // ───────────────────── Port Compatibility ─────────────────────

    /// <summary>
    /// Returns all ports that the given start port can connect to.
    /// Output connects to Input on a different node; same-type restriction.
    /// </summary>
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        return ports.ToList().Where(p =>
            p.direction != startPort.direction &&
            p.node != startPort.node
        ).ToList();
    }

    // ───────────────────── Context Menu ─────────────────────

    /// <summary>
    /// Adds right-click menu options: create node, set entry node.
    /// </summary>
    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        evt.menu.AppendAction("Add Dialogue Node", action =>
        {
            var graphPos = contentViewContainer.WorldToLocal(
                this.LocalToWorld(action.eventInfo.localMousePosition));

            CreateNode(graphPos, "NPC", "", false);
        });

        // Allow setting a selected node as the entry point
        var selectedNodes = selection.OfType<DialogueNodeView>().ToList();
        if (selectedNodes.Count == 1 && !selectedNodes[0].IsEntryNode)
        {
            evt.menu.AppendAction("Set as Entry Node", _ =>
            {
                SetEntryNode(selectedNodes[0]);
            });
        }
    }

    // ───────────────────── Node Management ─────────────────────

    /// <summary>
    /// Creates a new DialogueNodeView at the given graph position.
    /// </summary>
    public DialogueNodeView CreateNode(Vector2 graphPosition, string speaker, string text, bool isEntry)
    {
        var node = new DialogueNodeView(this, graphPosition, speaker, text, isEntry);
        AddElement(node);
        return node;
    }

    /// <summary>
    /// Designates the given node as the single entry point, clearing entry status from others.
    /// </summary>
    public void SetEntryNode(DialogueNodeView newEntry)
    {
        foreach (var n in nodes.ToList().OfType<DialogueNodeView>())
        {
            n.SetEntryNode(n == newEntry);
        }
    }

    // ───────────────────── Load / Save ─────────────────────

    /// <summary>
    /// Rebuilds the entire graph from a DialogueData asset.
    /// </summary>
    public void LoadFromData(DialogueData data)
    {
        // Clear all existing elements
        DeleteElements(graphElements.ToList());

        if (data == null) return;

        if (data.nodes.Count == 0)
        {
            // Provide a default entry node so the user doesn't start with an empty canvas
            CreateNode(new Vector2(100, 200), "NPC", "", true);
            return;
        }

        // Auto-layout: if all positions are at origin, spread nodes out so they don't overlap
        bool allAtOrigin = data.nodes.TrueForAll(n => n.editorPosition == Vector2.zero);
        if (allAtOrigin && data.nodes.Count > 1)
        {
            for (int i = 0; i < data.nodes.Count; i++)
            {
                data.nodes[i].editorPosition = new Vector2(i * DefaultNodeSpacingX, 0f);
            }
        }

        // Phase 1: create node views
        var nodeViews = new List<DialogueNodeView>(data.nodes.Count);
        for (int i = 0; i < data.nodes.Count; i++)
        {
            DialogueNode node = data.nodes[i];
            var nodeView = CreateNode(node.editorPosition, node.speakerName, node.text, i == 0);

            foreach (DialogueResponse response in node.responses)
            {
                nodeView.AddResponsePort(response.responseText);
            }

            nodeViews.Add(nodeView);
        }

        // Phase 2: create edges based on response nextNodeIndex
        for (int i = 0; i < data.nodes.Count; i++)
        {
            DialogueNode node = data.nodes[i];
            DialogueNodeView nodeView = nodeViews[i];

            for (int r = 0; r < node.responses.Count; r++)
            {
                int nextIndex = node.responses[r].nextNodeIndex;
                if (nextIndex >= 0 && nextIndex < nodeViews.Count)
                {
                    Port outputPort = nodeView.ResponsePorts[r].Port;
                    Port inputPort = nodeViews[nextIndex].InputPort;

                    Edge edge = outputPort.ConnectTo(inputPort);
                    AddElement(edge);
                }
            }
        }
    }

    /// <summary>
    /// Serializes the current graph back into the given DialogueData asset.
    /// The entry node is always saved at index 0.
    /// </summary>
    public void SaveToData(DialogueData data)
    {
        if (data == null)
        {
            Debug.LogError("[DialogueGraphView] Cannot save: DialogueData is null.");
            return;
        }

        Undo.RecordObject(data, "Save Dialogue Graph");

        var nodeViews = nodes.ToList().OfType<DialogueNodeView>().ToList();

        // Guarantee the entry node occupies index 0
        var entryNode = nodeViews.FirstOrDefault(n => n.IsEntryNode);
        if (entryNode != null)
        {
            nodeViews.Remove(entryNode);
            nodeViews.Insert(0, entryNode);
        }

        // Build a lookup from node view to serialized index
        var indexMap = new Dictionary<DialogueNodeView, int>(nodeViews.Count);
        for (int i = 0; i < nodeViews.Count; i++)
        {
            indexMap[nodeViews[i]] = i;
        }

        // Serialize each node
        data.nodes.Clear();
        foreach (DialogueNodeView nodeView in nodeViews)
        {
            Rect rect = nodeView.GetPosition();
            var dialogueNode = new DialogueNode
            {
                speakerName = nodeView.SpeakerName,
                text = nodeView.DialogueText,
                editorPosition = new Vector2(rect.x, rect.y),
                responses = new List<DialogueResponse>()
            };

            foreach (DialogueNodeView.ResponsePortData rpd in nodeView.ResponsePorts)
            {
                var response = new DialogueResponse
                {
                    responseText = rpd.GetText(),
                    nextNodeIndex = -1
                };

                // Resolve the connected target node index
                foreach (Edge edge in rpd.Port.connections)
                {
                    if (edge.input?.node is DialogueNodeView targetNode &&
                        indexMap.TryGetValue(targetNode, out int targetIndex))
                    {
                        response.nextNodeIndex = targetIndex;
                    }
                }

                dialogueNode.responses.Add(response);
            }

            data.nodes.Add(dialogueNode);
        }

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();

        Debug.Log($"[DialogueEditor] Saved {nodeViews.Count} node(s) to '{data.name}'.");
    }
}
