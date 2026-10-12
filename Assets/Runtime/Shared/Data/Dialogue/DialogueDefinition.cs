using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared.Data.Dialogue
{


    /// <summary>
    /// Defines a dialogue that actors (both PCs and NPCs) can engage in.
    /// </summary>
    [CreateAssetMenu(fileName = "DialogueDefinition_", menuName = "NinthAge/Definitions/Dialogue Definition")]
    public class DialogueDefinition : DataDefinition
    {
        [SerializeField, FormerlySerializedAs("nodes")] private List<DialogueNode> _nodes = new();
        /// <summary>Gets the dialogue nodes.</summary>
        public IReadOnlyList<DialogueNode> Nodes => _nodes;

        protected override string AssetPrefix => "Dialog";

        /// <summary>Gets a dialogue node by identifier.</summary>
        public DialogueNode GetNode(int nodeID)
        {
            return Nodes.FirstOrDefault(n => n.ID == nodeID);
        }
    }
}
