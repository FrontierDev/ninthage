using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared.Data
{
    [Serializable]
    /// <summary>Defines one node in a dialogue.</summary>
    public class DialogueNode
    {
        [SerializeField] private int id;
        /// <summary>Gets or sets the node identifier.</summary>
        public int ID
        {
            get => id;
            set => id = value;
        }

        [SerializeField] private string title;
        /// <summary>Gets or sets the node title.</summary>
        public string Title
        {
            get => title;
            set => title = value;
        }

        [SerializeField] private string dialogueText;
        /// <summary>Gets or sets the node dialogue text.</summary>
        public string DialogueText
        {
            get => dialogueText;
            set => dialogueText = value;
        }

        [SerializeField] private List<DialogueChoice> choices = new();
        /// <summary>Gets the choices available at this node.</summary>
        public IReadOnlyList<DialogueChoice> Choices => choices;

        /// <summary>Adds a choice to the node.</summary>
        public void AddChoice(DialogueChoice choice)
        {
            choices.Add(choice);
        }

        /// <summary>Removes a choice at the specified index.</summary>
        public void RemoveChoiceAt(int index)
        {
            if (index >= 0 && index < choices.Count)
            {
                choices.RemoveAt(index);
            }
        }
    }
}
