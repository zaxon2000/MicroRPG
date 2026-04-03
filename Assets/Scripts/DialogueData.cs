using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds a simple branching dialogue tree as a list of nodes referenced by index.
/// </summary>
[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/DialogueData")]
public class DialogueData : ScriptableObject
{
    [Tooltip("Ordered list of dialogue nodes. The first node (index 0) is the entry point.")]
    public List<DialogueNode> nodes = new List<DialogueNode>();
}

/// <summary>
/// A single dialogue node: what the NPC says and the player's possible responses.
/// </summary>
[Serializable]
public class DialogueNode
{
    [Tooltip("Name displayed as the speaker.")]
    public string speakerName = "NPC";

    [TextArea(2, 4)]
    [Tooltip("The dialogue line the NPC speaks.")]
    public string text = "";

    [Tooltip("Player response choices. Leave empty for a terminal node (click to dismiss).")]
    public List<DialogueResponse> responses = new List<DialogueResponse>();

    [HideInInspector]
    [Tooltip("Position of this node in the dialogue editor graph.")]
    public Vector2 editorPosition;
}

/// <summary>
/// A single player response option that links to the next node.
/// </summary>
[Serializable]
public class DialogueResponse
{
    [Tooltip("Text shown on the response button.")]
    public string responseText = "";

    [Tooltip("Index of the next DialogueNode to show (-1 = end dialogue).")]
    public int nextNodeIndex = -1;
}
