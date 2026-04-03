using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Visual representation of a single dialogue node in the graph editor.
/// Contains an input port, speaker/text fields, and dynamic response output ports.
/// </summary>
public class DialogueNodeView : Node
{
    /// <summary>
    /// Holds a response output port and a getter for its editable text.
    /// </summary>
    public struct ResponsePortData
    {
        public Port Port;
        public Func<string> GetText;
    }

    private static readonly Color EntryNodeColor = new Color(0.18f, 0.46f, 0.2f, 0.9f);

    public Port InputPort { get; private set; }
    public bool IsEntryNode { get; private set; }
    public string SpeakerName => _speakerField.value;
    public string DialogueText => _textField.value;
    public IReadOnlyList<ResponsePortData> ResponsePorts => _responsePorts;

    private readonly List<ResponsePortData> _responsePorts = new List<ResponsePortData>();
    private readonly TextField _speakerField;
    private readonly TextField _textField;
    private readonly GraphView _graphView;

    public DialogueNodeView(GraphView graphView, Vector2 position, string speaker, string dialogueText, bool isEntry)
    {
        _graphView = graphView;
        IsEntryNode = isEntry;

        title = isEntry ? "START" : "Dialogue";
        SetPosition(new Rect(position, new Vector2(280, 200)));

        if (isEntry)
        {
            titleContainer.style.backgroundColor = EntryNodeColor;
            capabilities &= ~Capabilities.Deletable;
        }

        // --- Input port ---
        InputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
        InputPort.portName = "In";
        inputContainer.Add(InputPort);

        // --- Speaker name field ---
        _speakerField = new TextField("Speaker") { value = speaker };
        ApplyFieldMargins(_speakerField);
        StopGraphKeyCapture(_speakerField);
        extensionContainer.Add(_speakerField);

        // --- Dialogue text area ---
        _textField = new TextField("Text") { value = dialogueText, multiline = true };
        ApplyFieldMargins(_textField);
        StopGraphKeyCapture(_textField);
        var textInput = _textField.Q("unity-text-input");
        if (textInput != null)
        {
            textInput.style.minHeight = 48;
            textInput.style.whiteSpace = WhiteSpace.Normal;
        }
        extensionContainer.Add(_textField);

        // --- Add Response button ---
        var addBtn = new Button(() => AddResponsePort("New response")) { text = "+ Response" };
        addBtn.style.marginTop = 4;
        ApplyFieldMargins(addBtn);
        extensionContainer.Add(addBtn);

        RefreshExpandedState();
        RefreshPorts();
    }

    /// <summary>
    /// Marks or unmarks this node as the dialogue entry point.
    /// </summary>
    public void SetEntryNode(bool isEntry)
    {
        IsEntryNode = isEntry;
        title = isEntry ? "START" : "Dialogue";

        if (isEntry)
        {
            titleContainer.style.backgroundColor = EntryNodeColor;
            capabilities &= ~Capabilities.Deletable;
        }
        else
        {
            titleContainer.style.backgroundColor = StyleKeyword.Null;
            capabilities |= Capabilities.Deletable;
        }
    }

    /// <summary>
    /// Creates a new response output port with the given text and adds it to the node.
    /// </summary>
    public Port AddResponsePort(string responseText)
    {
        var port = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
        port.portName = "";

        // Hide the default empty port label
        var portLabel = port.Q("type");
        if (portLabel != null)
            portLabel.style.display = DisplayStyle.None;

        // Response text field
        var textField = new TextField { value = responseText };
        textField.style.minWidth = 100;
        textField.style.flexGrow = 1;
        StopGraphKeyCapture(textField);

        // Delete button
        var deleteBtn = new Button(() => RemoveResponsePort(port)) { text = "\u00D7" };
        deleteBtn.style.color = new Color(1f, 0.3f, 0.3f);
        deleteBtn.style.minWidth = 22;
        deleteBtn.style.maxWidth = 22;

        // Insert content before the connector (last child for output ports)
        port.Insert(0, textField);
        port.Insert(1, deleteBtn);

        var data = new ResponsePortData
        {
            Port = port,
            GetText = () => textField.value
        };
        _responsePorts.Add(data);

        outputContainer.Add(port);
        RefreshExpandedState();
        RefreshPorts();

        return port;
    }

    /// <summary>
    /// Disconnects and removes a response output port.
    /// </summary>
    private void RemoveResponsePort(Port port)
    {
        // Disconnect all edges from this port
        var connectedEdges = new List<Edge>(port.connections);
        _graphView.DeleteElements(connectedEdges);

        // Remove from tracking list
        for (int i = _responsePorts.Count - 1; i >= 0; i--)
        {
            if (_responsePorts[i].Port == port)
            {
                _responsePorts.RemoveAt(i);
                break;
            }
        }

        outputContainer.Remove(port);
        RefreshExpandedState();
        RefreshPorts();
    }

    /// <summary>
    /// Prevents the GraphView from intercepting keyboard events aimed at a text field.
    /// </summary>
    private static void StopGraphKeyCapture(VisualElement element)
    {
        element.RegisterCallback<KeyDownEvent>(evt => evt.StopPropagation());
    }

    /// <summary>
    /// Applies consistent side margins to a visual element.
    /// </summary>
    private static void ApplyFieldMargins(VisualElement element)
    {
        element.style.marginLeft = 4;
        element.style.marginRight = 4;
    }
}
