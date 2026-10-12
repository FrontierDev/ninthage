using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared.Data
{
    [Serializable]
    /// <summary>Defines one node in a dialogue.</summary>
    public class DialogueNode
    {
        [SerializeField, FormerlySerializedAs("id")] private int _id;
        /// <summary>Gets or sets the node identifier.</summary>
        public int ID
        {
            get => _id;
            set => _id = value;
        }

        [SerializeField, FormerlySerializedAs("title")] private string _title;
        /// <summary>Gets or sets the node title.</summary>
        public string Title
        {
            get => _title;
            set => _title = value;
        }

        [SerializeField, FormerlySerializedAs("dialogueText")] private string _dialogueText;
        /// <summary>Gets or sets the node dialogue text.</summary>
        public string DialogueText
        {
            get => _dialogueText;
            set => _dialogueText = value;
        }

        [SerializeField, FormerlySerializedAs("choices")] private List<DialogueChoice> _choices = new();
        /// <summary>Gets the choices available at this node.</summary>
        public IReadOnlyList<DialogueChoice> Choices => _choices;

        /// <summary>Adds a choice to the node.</summary>
        public void AddChoice(DialogueChoice choice)
        {
            _choices.Add(choice);
        }

        /// <summary>Removes a choice at the specified index.</summary>
        public void RemoveChoiceAt(int index)
        {
            if (index >= 0 && index < _choices.Count)
            {
                _choices.RemoveAt(index);
            }
        }
    }
}
