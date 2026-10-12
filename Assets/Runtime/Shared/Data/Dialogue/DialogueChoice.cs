using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared.Data.Dialogue
{
    [Serializable]
    /// <summary>Defines one selectable dialogue choice.</summary>
    public class DialogueChoice
    {
        [SerializeField, FormerlySerializedAs("id")] private string _id;
        /// <summary>Gets the choice identifier.</summary>
        public string ID => _id;

        [SerializeField, FormerlySerializedAs("choiceText")] private string _choiceText;
        /// <summary>Gets or sets the choice text.</summary>
        public string ChoiceText
        {
            get => _choiceText;
            set => _choiceText = value;
        }

        [SerializeField, FormerlySerializedAs("icon")] private Sprite _icon;
        /// <summary>Gets the choice icon.</summary>
        public Sprite Icon => _icon;

        [SerializeField, FormerlySerializedAs("conditions")] private List<DialogueCondition> _conditions = new();
        /// <summary>Gets the conditions for showing the choice.</summary>
        public IReadOnlyList<DialogueCondition> Conditions => _conditions;

        /// <summary>Adds a condition to the choice.</summary>
        public void AddCondition(DialogueCondition condition)
        {
            _conditions.Add(condition);
        }

        /// <summary>Removes a condition at the specified index.</summary>
        public void RemoveConditionAt(int index)
        {
            if (index >= 0 && index < _conditions.Count)
                _conditions.RemoveAt(index);
        }

        [SerializeField, FormerlySerializedAs("showAlways")] private bool _showAlways = false;
        /// <summary>Gets whether the choice is shown regardless of conditions.</summary>
        public bool ShowAlways => _showAlways;

        // Links are stored by id to avoid nested serialization cycles.
        // The previous object reference `nextNode` caused Unity to inline nested
        // DialogueNode objects which created recursion when nodes referenced
        // each other. We now store only `nextNodeId` and resolve at runtime when
        // a containing DialogueDefinition is available.
        [SerializeField, FormerlySerializedAs("nextNodeId")] private int _nextNodeID = -1;
        /// <summary>Gets or sets the next dialogue node identifier.</summary>
        public int NextNodeID
        {
            get => _nextNodeID;
            set => _nextNodeID = value;
        }

        /// <summary>Resolves the next node from the containing dialogue definition.</summary>
        public DialogueNode ResolveNextNode(DialogueDefinition root)
        {
            if (root == null || _nextNodeID == -1) return null;
            foreach (var n in root.Nodes)
            {
                if (n.ID == _nextNodeID) return n;
            }
            return null;
        }
    }
}
