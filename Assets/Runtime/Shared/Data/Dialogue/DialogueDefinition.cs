using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Shared.Data
{


    /// <summary>
    /// Defines a dialogue that actors (both PCs and NPCs) can engage in.
    /// </summary>
    [CreateAssetMenu(fileName = "DialogueDefinition_", menuName = "NinthAge/Definitions/Dialogue Definition")]
    public class DialogueDefinition : DataDefinition
    {
        [SerializeField] private List<DialogueNode> nodes = new();
        public IReadOnlyList<DialogueNode> Nodes => nodes;

        protected override string AssetPrefix => "Dialog";

        public DialogueNode GetNode(int nodeID)
        {
            return Nodes.FirstOrDefault(n => n.ID == nodeID);
        }
    }
}
