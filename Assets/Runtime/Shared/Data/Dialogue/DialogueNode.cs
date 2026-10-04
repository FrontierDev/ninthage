using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared.Data
{
    [Serializable]
    public class DialogueNode
    {
        [SerializeField] private int id;
        public int ID
        {
            get => id;
            set => id = value;
        }

        [SerializeField] private string title;
        public string Title
        {
            get => title;
            set => title = value;
        }

        [SerializeField] private string dialogueText;
        public string DialogueText
        {
            get => dialogueText;
            set => dialogueText = value;
        }

        [SerializeField] private List<DialogueChoice> choices = new();
        public IReadOnlyList<DialogueChoice> Choices => choices;

        public void AddChoice(DialogueChoice choice)
        {
            choices.Add(choice);
        }

        public void RemoveChoiceAt(int index)
        {
            if (index >= 0 && index < choices.Count)
            {
                choices.RemoveAt(index);
            }
        }
    }
}
