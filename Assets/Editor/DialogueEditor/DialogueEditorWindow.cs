using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Game.Shared.Data;
using System.Linq;

namespace Game.Editor.Dialogue
{
    public class DialogueEditorWindow : EditorWindow
    {
        private DialogueDefinition dialogue;
        private SerializedObject serializedDialogue;

        private Dictionary<int, Rect> nodePositions = new Dictionary<int, Rect>();

        private int draggingNodeId = -1;
        private Vector2 dragOffset;
        private int selectedNodeId = -1;
        private bool isDraggingConnection = false;
        private int dragFromNodeId = -1;
        private int dragFromChoiceIndex = -1;
        private Vector2 dragPosition;
        // Hovered link state (for highlighting)
        private int hoveredLinkSourceId = -1;
        private int hoveredLinkChoiceIndex = -1;
        private int hoveredLinkTargetId = -1;
        private GUIStyle nodeTextStyle;

        [MenuItem("Window/NinthAge/Dialogue Editor")]
        public static void ShowWindow()
        {
            var w = GetWindow<DialogueEditorWindow>("Dialogue Editor");
            w.minSize = new Vector2(600, 300);
        }

        public static void OpenWith(DialogueDefinition def)
        {
            ShowWindow();
            var w = GetWindow<DialogueEditorWindow>();
            w.SetDialogue(def);
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            nodeTextStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true, // Enable text wrapping
                normal = { textColor = Color.black } // Set text color to black for better contrast with white background
            };
        }

        private void SetDialogue(DialogueDefinition def)
        {
            dialogue = def;
            if (dialogue != null) serializedDialogue = new SerializedObject(dialogue);
            RebuildNodePositionsIfNeeded();
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (dialogue == null)
            {
                EditorGUILayout.HelpBox("Select a DialogueDefinition asset to edit.", MessageType.Info);
                return;
            }

            if (serializedDialogue == null || serializedDialogue.targetObject != dialogue)
            {
                serializedDialogue = new SerializedObject(dialogue);
            }
            serializedDialogue.UpdateIfRequiredOrScript();

            var nodes = dialogue.Nodes;

            // Layout: canvas on the left, inspector on the right
            float inspectorWidth = 300f;
            var canvasRect = new Rect(0, 20, Mathf.Max(0, position.width - inspectorWidth), position.height - 20);
            var inspectorRect = new Rect(position.width - inspectorWidth, 20, inspectorWidth, position.height - 20);

            GUI.Box(canvasRect, GUIContent.none);

            // Inspector background (slightly tinted) and border
            EditorGUI.DrawRect(inspectorRect, new Color(0.18f, 0.18f, 0.18f, 1f));
            Handles.BeginGUI();
            Handles.DrawSolidRectangleWithOutline(inspectorRect, Color.clear, new Color(0.18f, 0.18f, 0.2f, 1f));
            Handles.EndGUI();

            HandleEvents(canvasRect);

            // Ensure positions for all nodes and update sizes
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (!nodePositions.TryGetValue(node.ID, out var rect))
                {
                    rect = new Rect(20 + (i % 5) * 220, 40 + (i / 5) * 120, 200, 80);
                    nodePositions[node.ID] = rect;
                }
                // keep width consistent
                rect.width = 200f;
                nodePositions[node.ID] = rect;
            }

            UpdateNodeSizes(nodes);

            // Draw connections first (under nodes)
            DrawConnections();

            // Draw nodes
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                var rect = nodePositions[node.ID];
                DrawNode(rect, node, i);
            }

            // Inspector
            DrawInspectorArea(inspectorRect);

            if (GUI.changed)
            {
                MarkDirty();
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            var newDialogue = (DialogueDefinition)EditorGUILayout.ObjectField(dialogue, typeof(DialogueDefinition), false, GUILayout.Width(300));
            if (newDialogue != dialogue)
            {
                SetDialogue(newDialogue);
            }

            if (GUILayout.Button("Add Node", EditorStyles.toolbarButton))
            {
                CreateNode();
            }

            if (GUILayout.Button("Save", EditorStyles.toolbarButton))
            {
                Save();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // Updated node visuals to include white background and improved text wrapping
        private void DrawNode(Rect rect, DialogueNode node, int index)
        {
            // Draw white background
            EditorGUI.DrawRect(rect, Color.white);

            // Draw node border
            Color borderColor = Color.black;
            if (selectedNodeId >= 0 && dialogue != null)
            {
                var selectedNode = dialogue.Nodes.FirstOrDefault(n => n.ID == selectedNodeId);
                if (selectedNode != null)
                {
                    borderColor = new Color(0.2f, 0.6f, 1f, 1f);
                }
            }
            Handles.BeginGUI();
            Handles.DrawSolidRectangleWithOutline(rect, Color.clear, borderColor);
            Handles.EndGUI();

            // Title
            var titleRect = new Rect(rect.x + 4, rect.y + 4, rect.width - 8, 18);
            EditorGUI.LabelField(titleRect, node.Title ?? $"Node {node.ID}", EditorStyles.boldLabel);

            // Dialogue text (wrapped)
            float textAreaWidth = rect.width - 8f;
            float textHeight = nodeTextStyle.CalcHeight(new GUIContent(node.DialogueText ?? string.Empty), textAreaWidth);
            var textRect = new Rect(rect.x + 4, rect.y + 22, textAreaWidth, textHeight);
            GUI.Label(textRect, node.DialogueText ?? string.Empty, nodeTextStyle);

            // Draw ports for each choice on the right side
            var choices = node.Choices;
            for (int i = 0; i < choices.Count; i++)
            {
                var portRect = GetChoicePortRect(rect, node, choices.Count, i);
                // Visual
                Color portColor = Color.grey;
                if (hoveredLinkSourceId == node.ID && hoveredLinkChoiceIndex == i)
                    portColor = new Color(1f, 0.85f, 0.2f, 1f);
                EditorGUI.DrawRect(portRect, portColor);

                // Add hover effect
                if (portRect.Contains(Event.current.mousePosition))
                {
                    EditorGUI.DrawRect(portRect, new Color(1f, 1f, 1f, 0.2f));
                }

                // Handle mouse down on port to start connection drag
                var e = Event.current;
                if (e.type == EventType.MouseDown && e.button == 0 && portRect.Contains(e.mousePosition))
                {
                    isDraggingConnection = true;
                    dragFromNodeId = node.ID;
                    dragFromChoiceIndex = i;
                    dragPosition = e.mousePosition;
                    e.Use();
                }
            }

            // Handle node dragging when clicking inside node body (but not on ports)
            var evt = Event.current;
            if (evt.type == EventType.MouseDown && rect.Contains(evt.mousePosition) && evt.button == 0)
            {
                // if clicked on a port we already started a drag; avoid overriding selection
                bool clickedOnPort = false;
                for (int i = 0; i < node.Choices.Count; i++) if (GetChoicePortRect(rect, node, node.Choices.Count, i).Contains(evt.mousePosition)) { clickedOnPort = true; break; }
                if (!clickedOnPort)
                {
                    draggingNodeId = node.ID;
                    dragOffset = evt.mousePosition - rect.position;
                    selectedNodeId = node.ID;
                    evt.Use();
                }
            }

            if (draggingNodeId == node.ID && evt.type == EventType.MouseDrag)
            {
                var newPos = evt.mousePosition - dragOffset;
                nodePositions[node.ID] = new Rect(newPos.x, newPos.y, rect.width, rect.height);
                Repaint();
            }

            if (evt.type == EventType.MouseUp && draggingNodeId == node.ID)
            {
                draggingNodeId = -1;
                evt.Use();
            }

            // If a connection drag is active, update drag position and on mouse up finalize
            if (isDraggingConnection)
            {
                var e2 = Event.current;
                if (e2.type == EventType.MouseDrag)
                {
                    dragPosition = e2.mousePosition;
                    Repaint();
                }

                if (e2.type == EventType.MouseUp)
                {
                    // Find target node under mouse
                    int targetId = -1;
                    foreach (var kv in nodePositions)
                    {
                        if (kv.Value.Contains(e2.mousePosition)) { targetId = kv.Key; break; }
                    }
                    // Apply the link: set nextNodeId for the source choice
                    if (dragFromNodeId >= 0 && dragFromChoiceIndex >= 0)
                    {
                        int srcIndex = -1;
                        for (int ni = 0; ni < dialogue.Nodes.Count; ni++) if (dialogue.Nodes[ni].ID == dragFromNodeId) { srcIndex = ni; break; }
                        if (srcIndex >= 0)
                        {
                            serializedDialogue.Update();
                            var nodesProp = serializedDialogue.FindProperty("nodes");
                            var srcNodeProp = nodesProp.GetArrayElementAtIndex(srcIndex);
                            var choicesProp = srcNodeProp.FindPropertyRelative("choices");
                            if (dragFromChoiceIndex < choicesProp.arraySize)
                            {
                                var choiceProp = choicesProp.GetArrayElementAtIndex(dragFromChoiceIndex);
                                Undo.RecordObject(dialogue, "Connect Dialogue Nodes");
                                choiceProp.FindPropertyRelative("nextNodeId").intValue = targetId;
                                serializedDialogue.ApplyModifiedProperties();
                                EditorUtility.SetDirty(dialogue);
                                AssetDatabase.SaveAssets();
                            }
                        }
                    }

                    // Reset drag state
                    isDraggingConnection = false;
                    dragFromNodeId = -1;
                    dragFromChoiceIndex = -1;
                    evt.Use();
                }
            }
        }

        private Rect GetChoicePortRect(Rect nodeRect, DialogueNode node, int choiceCount, int choiceIndex)
        {
            float portSize = 12f;
            if (nodeTextStyle == null) nodeTextStyle = new GUIStyle(EditorStyles.label) { wordWrap = true };
            if (choiceCount <= 0) return new Rect(nodeRect.xMax - portSize, nodeRect.y + nodeRect.height * 0.5f - portSize * 0.5f, portSize, portSize);
            // place ports under the text area; start at y = nodeRect.y + 22 + textHeight
            float textAreaWidth = nodeRect.width - 8f;
            // estimate text height using the node's actual text
            float approxTextHeight = nodeTextStyle.CalcHeight(new GUIContent(node?.DialogueText ?? string.Empty), textAreaWidth);
            float startY = nodeRect.y + 22f + approxTextHeight + 6f;
            float available = Mathf.Max(10f, nodeRect.y + nodeRect.height - startY - 8f);
            float spacing = available / (choiceCount + 1);
            float y = startY + spacing * (choiceIndex + 1);
            float x = nodeRect.xMax - portSize - 4f;
            return new Rect(x, y - portSize * 0.5f, portSize, portSize);
        }

        // Helper used to provide node text for port placement estimation
        private string GetNodeTextByRect(Rect nodeRect)
        {
            // Find node by matching rect (best-effort). This is only used for estimating heights.
            foreach (var kv in nodePositions)
            {
                if (kv.Value == nodeRect)
                {
                    var id = kv.Key;
                    var nodes = dialogue?.Nodes;
                    if (nodes == null) return string.Empty;
                    foreach (var n in nodes) if (n.ID == id) return n.DialogueText ?? string.Empty;
                }
            }
            return string.Empty;
        }

        private void UpdateNodeSizes(IReadOnlyList<DialogueNode> nodes)
        {
            if (nodes == null) return;
            if (nodeTextStyle == null) nodeTextStyle = new GUIStyle(EditorStyles.label) { wordWrap = true };
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (!nodePositions.TryGetValue(node.ID, out var rect))
                {
                    rect = new Rect(20 + (i % 5) * 220, 40 + (i / 5) * 120, 200, 80);
                }

                float textAreaWidth = rect.width - 8f;
                float textHeight = nodeTextStyle.CalcHeight(new GUIContent(node.DialogueText ?? string.Empty), textAreaWidth);
                // choices area: approximate 20px per choice
                float choicesHeight = Mathf.Max(0f, node.Choices.Count * 18f);
                float desiredHeight = 22f + textHeight + 6f + choicesHeight + 12f; // title + text + spacing + choices + padding
                rect.height = Mathf.Max(60f, desiredHeight);
                nodePositions[node.ID] = rect;
            }
        }

        private void DrawConnections()
        {
            if (dialogue == null) return;

            Color linkColor = new Color(0.2f, 0.6f, 1f, 1f);
            Color dragColor = new Color(1f, 0.6f, 0.2f, 1f);
            Color highlightColor = new Color(1f, 0.85f, 0.2f, 1f);

            // Hover detection: find the closest connection to the mouse
            var e = Event.current;
            Vector2 mousePos = e.mousePosition;
            float bestDist = float.MaxValue;
            int bestSrc = -1;
            int bestChoiceIndex = -1;
            int bestTarget = -1;

            foreach (var node in dialogue.Nodes)
            {
                if (!nodePositions.TryGetValue(node.ID, out var srcRect)) continue;
                for (int i = 0; i < node.Choices.Count; i++)
                {
                    var choice = node.Choices[i];
                    var srcPort = GetChoicePortRect(srcRect, node, node.Choices.Count, i);
                    Vector3 start = new Vector3(srcPort.xMax, srcPort.y + srcPort.height * 0.5f, 0);

                    // If dragging a new connection from this port, skip hover testing for existing links
                    if (isDraggingConnection && dragFromNodeId == node.ID && dragFromChoiceIndex == i)
                        continue;

                    var targetId = choice.NextNodeID;
                    if (targetId == -1) continue;
                    if (!nodePositions.TryGetValue(targetId, out var dstRect)) continue;

                    Vector3 endPos = new Vector3(dstRect.xMin, dstRect.y + dstRect.height * 0.5f, 0);
                    Vector3 startTan2 = start + Vector3.right * 50;
                    Vector3 endTan2 = endPos + Vector3.left * 50;

                    // Use HandleUtility to compute the distance from the mouse to the bezier curve
                    float dist = HandleUtility.DistancePointBezier(mousePos, start, endPos, startTan2, endTan2);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestSrc = node.ID;
                        bestChoiceIndex = i;
                        bestTarget = targetId;
                    }
                }
            }

            // Threshold in pixels for hover
            float hoverThreshold = 10f;
            if (bestDist <= hoverThreshold)
            {
                hoveredLinkSourceId = bestSrc;
                hoveredLinkChoiceIndex = bestChoiceIndex;
                hoveredLinkTargetId = bestTarget;
            }
            else
            {
                hoveredLinkSourceId = -1;
                hoveredLinkChoiceIndex = -1;
                hoveredLinkTargetId = -1;
            }

            // Now draw the connections, highlighting hovered one
            foreach (var node in dialogue.Nodes)
            {
                if (!nodePositions.TryGetValue(node.ID, out var srcRect)) continue;
                for (int i = 0; i < node.Choices.Count; i++)
                {
                    var choice = node.Choices[i];
                    var srcPort = GetChoicePortRect(srcRect, node, node.Choices.Count, i);
                    Vector3 start = new Vector3(srcPort.xMax, srcPort.y + srcPort.height * 0.5f, 0);

                    if (isDraggingConnection && dragFromNodeId == node.ID && dragFromChoiceIndex == i)
                    {
                        Vector3 end = new Vector3(dragPosition.x, dragPosition.y, 0);
                        Vector3 startTan = start + Vector3.right * 50;
                        Vector3 endTan = end + Vector3.left * 50;
                        Handles.DrawBezier(start, end, startTan, endTan, dragColor, null, 3f);
                        continue;
                    }

                    var targetId = choice.NextNodeID;
                    if (targetId == -1) continue;
                    if (!nodePositions.TryGetValue(targetId, out var dstRect)) continue;

                    Vector3 endPos = new Vector3(dstRect.xMin, dstRect.y + dstRect.height * 0.5f, 0);
                    Vector3 startTan2 = start + Vector3.right * 50;
                    Vector3 endTan2 = endPos + Vector3.left * 50;

                    // If this connection is hovered, draw with highlight
                    bool isHovered = (hoveredLinkSourceId == node.ID && hoveredLinkChoiceIndex == i && hoveredLinkTargetId == targetId);
                    if (isHovered)
                        Handles.DrawBezier(start, endPos, startTan2, endTan2, highlightColor, null, 6f);
                    else
                        Handles.DrawBezier(start, endPos, startTan2, endTan2, linkColor, null, 3f);
                }
            }

            // Request repaint on mouse move to update hover visuals
            if (e.type == EventType.MouseMove)
                Repaint();
        }

        private void DrawInspectorArea(Rect inspectorRect)
        {
            GUILayout.BeginArea(inspectorRect);
            EditorGUILayout.LabelField("Inspector", EditorStyles.boldLabel);

            if (selectedNodeId >= 0 && dialogue != null)
            {
                var selectedNode = dialogue.Nodes.FirstOrDefault(n => n.ID == selectedNodeId);
                if (selectedNode != null)
                {
                    EditorGUILayout.LabelField("Title:");
                    EditorGUI.BeginChangeCheck();

                    string newTitle = EditorGUILayout.TextField(selectedNode.Title ?? string.Empty);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(dialogue, "Edit Node Title");
                        selectedNode.Title = newTitle;
                        EditorUtility.SetDirty(dialogue);
                    }

                    EditorGUILayout.LabelField("Text:");
                    EditorGUI.BeginChangeCheck();

                    string newText = EditorGUILayout.TextArea(selectedNode.DialogueText ?? string.Empty, new GUIStyle(EditorStyles.textArea) { wordWrap = true }, GUILayout.Height(60));

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(dialogue, "Edit Dialogue Text");
                        selectedNode.DialogueText = newText;
                        EditorUtility.SetDirty(dialogue);
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Choices", EditorStyles.boldLabel);

                    for (int i = 0; i < selectedNode.Choices.Count; i++)
                    {
                        var choice = selectedNode.Choices[i];
                        EditorGUILayout.BeginVertical("box");

                        EditorGUILayout.LabelField($"Choice {i + 1}", EditorStyles.boldLabel);

                        EditorGUI.BeginChangeCheck();
                        string newChoiceText = EditorGUILayout.TextField("Text", choice.ChoiceText);

                        // Dropdown for Next Node Title
                        var nodeTitles = dialogue.Nodes.Select(n => n.Title).ToList();
                        nodeTitles.Insert(0, "None"); // Add "None" option
                        int selectedIndex = choice.NextNodeID == -1
                            ? 0
                            : dialogue.Nodes.Select((n, idx) => new { n.ID, idx })
                                .FirstOrDefault(x => x.ID == choice.NextNodeID)?.idx + 1 ?? 0;
                        if (selectedIndex == -1) selectedIndex = 0; // Fallback to "None" if ID not found
                        int newSelectedIndex = EditorGUILayout.Popup("Next Node", selectedIndex, nodeTitles.ToArray());


                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(dialogue, "Edit Choice");
                            choice.ChoiceText = newChoiceText;
                            choice.NextNodeID = newSelectedIndex == 0 ? -1 : dialogue.Nodes[newSelectedIndex - 1].ID;
                            EditorUtility.SetDirty(dialogue);
                        }

                        // Conditions (use serialized properties to support SerializeReference)
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Conditions", EditorStyles.miniBoldLabel);

                        // find selected node index for serialized access
                        int selNodeIndex = -1;
                        for (int ni = 0; ni < dialogue.Nodes.Count; ni++) if (dialogue.Nodes[ni].ID == selectedNodeId) { selNodeIndex = ni; break; }
                        if (selNodeIndex >= 0)
                        {
                            serializedDialogue.UpdateIfRequiredOrScript();
                            var nodesProp = serializedDialogue.FindProperty("nodes");
                            var selNodeProp = nodesProp.GetArrayElementAtIndex(selNodeIndex);
                            var choicesProp = selNodeProp.FindPropertyRelative("choices");
                            var choiceProp = choicesProp.GetArrayElementAtIndex(i);
                            var conditionsProp = choiceProp.FindPropertyRelative("conditions");

                            for (int ci = 0; ci < conditionsProp.arraySize; ci++)
                            {
                                var condProp = conditionsProp.GetArrayElementAtIndex(ci);
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(condProp.FindPropertyRelative("condition"), GUIContent.none);
                                EditorGUILayout.PropertyField(condProp.FindPropertyRelative("invert"), GUIContent.none, GUILayout.Width(70));
                                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                                {
                                    conditionsProp.DeleteArrayElementAtIndex(ci);
                                    serializedDialogue.ApplyModifiedProperties();
                                    EditorUtility.SetDirty(dialogue);
                                    break;
                                }
                                EditorGUILayout.EndHorizontal();
                            }

                            if (GUILayout.Button("Add Condition", GUILayout.Width(120)))
                            {
                                conditionsProp.InsertArrayElementAtIndex(conditionsProp.arraySize);
                                var newCond = conditionsProp.GetArrayElementAtIndex(conditionsProp.arraySize - 1);
                                // clear fields
                                newCond.FindPropertyRelative("condition").managedReferenceValue = null;
                                newCond.FindPropertyRelative("invert").boolValue = false;
                                serializedDialogue.ApplyModifiedProperties();
                                EditorUtility.SetDirty(dialogue);
                            }
                        }

                        if (GUILayout.Button("Remove Choice", GUILayout.Width(120)))
                        {
                            Undo.RecordObject(dialogue, "Remove Choice");
                            selectedNode.RemoveChoiceAt(i);
                            EditorUtility.SetDirty(dialogue);
                            break;
                        }

                        EditorGUILayout.EndVertical();
                    }

                    if (GUILayout.Button("Add Choice", GUILayout.Width(120)))
                    {
                        Undo.RecordObject(dialogue, "Add Choice");
                        selectedNode.AddChoice(new DialogueChoice());
                        EditorUtility.SetDirty(dialogue);
                    }
                }
            }

            GUILayout.EndArea();
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            if (s.Length <= max) return s;
            return s.Substring(0, max - 3) + "...";
        }

        private void HandleEvents(Rect canvasRect)
        {
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 1 && canvasRect.Contains(e.mousePosition))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Create Node"), false, () => CreateNodeAt(e.mousePosition));
                menu.ShowAsContext();
                e.Use();
            }
        }

        private void CreateNode()
        {
            CreateNodeAt(new Vector2(40, 40));
        }

        private void CreateNodeAt(Vector2 pos)
        {
            if (dialogue == null) return;

            var so = new SerializedObject(dialogue);
            var nodesProp = so.FindProperty("nodes");
            int newIndex = nodesProp.arraySize;
            nodesProp.InsertArrayElementAtIndex(newIndex);
            var newElem = nodesProp.GetArrayElementAtIndex(newIndex);
            var idProp = newElem.FindPropertyRelative("id");
            var textProp = newElem.FindPropertyRelative("dialogueText");
            var choicesProp = newElem.FindPropertyRelative("choices");

            int newId = dialogue.Nodes.Count;
            idProp.intValue = newId;
            textProp.stringValue = "New Node";
            choicesProp.ClearArray();

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(dialogue);
            AssetDatabase.SaveAssets();

            nodePositions[newId] = new Rect(pos.x, pos.y, 200, 80);
        }

        private void Save()
        {
            if (dialogue == null) return;
            EditorUtility.SetDirty(dialogue);
            AssetDatabase.SaveAssets();
        }

        private void MarkDirty()
        {
            if (dialogue == null) return;
            Undo.RecordObject(dialogue, "Dialogue Edit");
            EditorUtility.SetDirty(dialogue);
        }

        private void RebuildNodePositionsIfNeeded()
        {
            if (dialogue == null) return;
            foreach (var n in dialogue.Nodes)
            {
                if (!nodePositions.ContainsKey(n.ID))
                {
                    nodePositions[n.ID] = new Rect(20 + nodePositions.Count * 10, 40 + nodePositions.Count * 10, 200, 80);
                }
            }
        }
    }
}
