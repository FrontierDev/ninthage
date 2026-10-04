using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared.Data
{
    [Serializable]
    public class DialogueChoice
    {
        [SerializeField] private string id;
        public string ID => id;

        [SerializeField] private string choiceText;
        public string ChoiceText
        {
            get => choiceText;
            set => choiceText = value;
        }

        [SerializeField] private Sprite icon;
        public Sprite Icon => icon;

        [SerializeField] private List<DialogueCondition> conditions = new();
        public IReadOnlyList<DialogueCondition> Conditions => conditions;

        public void AddCondition(DialogueCondition condition)
        {
            conditions.Add(condition);
        }

        public void RemoveConditionAt(int index)
        {
            if (index >= 0 && index < conditions.Count)
                conditions.RemoveAt(index);
        }

        [SerializeField] private bool showAlways = false;
        public bool ShowAlways => showAlways;

        // Links are stored by id to avoid nested serialization cycles.
        // The previous object reference `nextNode` caused Unity to inline nested
        // DialogueNode objects which created recursion when nodes referenced
        // each other. We now store only `nextNodeId` and resolve at runtime when
        // a containing DialogueDefinition is available.
        [SerializeField] private int nextNodeId = -1;
        public int NextNodeID
        {
            get => nextNodeId;
            set => nextNodeId = value;
        }

        public DialogueNode ResolveNextNode(DialogueDefinition root)
        {
            if (root == null || nextNodeId == -1) return null;
            foreach (var n in root.Nodes)
            {
                if (n.ID == nextNodeId) return n;
            }
            return null;
        }
    }
}
