using System.Collections.Generic;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    public sealed class UI_TalentTreePanel : UI_Panel
    {
        [SerializeField] private Image graphics;
        [SerializeField] private TMP_Text specNameText;
        [SerializeField] private TMP_Text spentPointsText;
        [SerializeField] private int specIndex;

        [SerializeField] private UI_TalentTreeNode talentNodePanelPrefab;
        [SerializeField] private Transform talentNodePanelContainer;
        [SerializeField] private float rowSpacing;

        /// <summary>
        /// Talent tree nodes, organised by row.
        /// </summary>
        private Dictionary<int, List<UI_TalentTreeNode>> talentNodes = new();

        // Internal private members.
        private ClassSpecialisation specialisation;

        protected override void Awake()
        {
            base.Awake();

            if (graphics.material != null)
                graphics.material = new Material(graphics.material);

            ActorService.onPlayerActorAssigned += OnPlayerActorAssigned;
        }

        private void OnPlayerActorAssigned(Actor actor)
        {
            var characterData = ClientAccountManager.ActiveCharacter;
            if (characterData != null)
            {
                var classID = characterData.ClassID;
                var classDef = ClassDefinitionLibrary.Instance.GetDefinition(classID);
                specialisation = classDef.Specialisations[specIndex - 1];
                UIShaderProperties.SetColor(graphics, "_AccentColor", specialisation.color);
                Initialize(specialisation);

            }
        }


        /// <summary>
        /// This only needs to be called once when the player's class is assgined.
        /// </summary>
        /// <param name="spec"></param>
        private void Initialize(ClassSpecialisation spec)
        {
            // Set the spec.
            specialisation = spec;
            specNameText.text = spec.name;
            spentPointsText.text = ClientAccountManager.PlayerActor.GetComponent<PlayerTalents>().GetSpentTalentPoints(spec).ToString();

            // Clear previous nodes in the container and internal state.
            if (talentNodePanelContainer != null)
            {
                for (int i = talentNodePanelContainer.childCount - 1; i >= 0; i--)
                {
                    var child = talentNodePanelContainer.GetChild(i).gameObject;
                    if (Application.isPlaying)
                        Destroy(child);
                    else
                        DestroyImmediate(child);
                }
            }
            talentNodes.Clear();

            // First pass: instantiate nodes and group them by row.
            foreach (TalentDefinition talent in spec.talents)
            {
                if (talent == null)
                    continue;

                var go = Instantiate(talentNodePanelPrefab, talentNodePanelContainer);
                go.Initialize(talent);

                var talentNode = go.GetComponent<UI_TalentTreeNode>();
                talentNode.Initialize(talent);
                var row = GetRowNumber(talent.MinLevel);

                if (!talentNodes.TryGetValue(row, out var nodesInRow))
                {
                    nodesInRow = new List<UI_TalentTreeNode>();
                    talentNodes[row] = nodesInRow;
                }

                nodesInRow.Add(talentNode);
            }

            // Second pass: position nodes now that we know counts per row.
            var rows = new List<int>(talentNodes.Keys);
            rows.Sort();
            foreach (var row in rows)
            {
                var nodesInRow = talentNodes[row];
                int count = nodesInRow.Count;
                for (int index = 0; index < count; index++)
                {
                    float xPos = (index - (count - 1) * 0.5f) * rowSpacing;
                    float yPos = -row * rowSpacing;
                    var rt = nodesInRow[index].GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(xPos, yPos);
                }
            }
        }

        private int GetRowNumber(int minLevel)
        {
            // Assuming talents are unlocked every 5 levels, starting at level 10.
            // e.g., 10, 15, 20...
            if (minLevel < 10)
                return 0;

            return (minLevel - 10) / 5;
        }
    }
}