using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_TalentTreePanel</c> implementation.
    /// </summary>
    public sealed class UI_TalentTreePanel : UI_Panel
    {
        [FormerlySerializedAs("graphics")]
        [SerializeField] private Image _graphics;
        [FormerlySerializedAs("specNameText")]
        [SerializeField] private TMP_Text _specNameText;
        [FormerlySerializedAs("spentPointsText")]
        [SerializeField] private TMP_Text _spentPointsText;
        [FormerlySerializedAs("specIndex")]
        [SerializeField] private int _specIndex;

        [FormerlySerializedAs("talentNodePanelPrefab")]
        [SerializeField] private UI_TalentTreeNode _talentNodePanelPrefab;
        [FormerlySerializedAs("talentNodePanelContainer")]
        [SerializeField] private Transform _talentNodePanelContainer;
        [FormerlySerializedAs("rowSpacing")]
        [SerializeField] private float _rowSpacing;

        /// <summary>
        /// Talent tree nodes, organised by row.
        /// </summary>
        private Dictionary<int, List<UI_TalentTreeNode>> _talentNodes = new();

        // Internal private members.
        private ClassSpecialisation _specialisation;

        protected override void Awake()
        {
            base.Awake();

            if (_graphics.material != null)
                _graphics.material = new Material(_graphics.material);

            ActorEndpoint.onPlayerActorAssigned += OnPlayerActorAssigned;
        }

        private void OnPlayerActorAssigned(Actor actor)
        {
            var characterData = ClientAccountManager.ActiveCharacter;
            if (characterData != null)
            {
                var classID = characterData.ClassID;
                var classDef = ClassDefinitionLibrary.Instance.GetDefinition(classID);
                _specialisation = classDef.Specialisations[_specIndex - 1];
                UIShaderProperties.SetColor(_graphics, "_AccentColor", _specialisation.color);
                Initialize(_specialisation);

            }
        }


        /// <summary>
        /// This only needs to be called once when the player's class is assgined.
        /// </summary>
        /// <param name="spec"></param>
        private void Initialize(ClassSpecialisation spec)
        {
            // Set the spec.
            _specialisation = spec;
            _specNameText.text = spec.name;
            _spentPointsText.text = ClientAccountManager.PlayerActor.GetComponent<PlayerTalents>().GetSpentTalentPoints(spec).ToString();

            // Clear previous nodes in the container and internal state.
            if (_talentNodePanelContainer != null)
            {
                for (int i = _talentNodePanelContainer.childCount - 1; i >= 0; i--)
                {
                    var child = _talentNodePanelContainer.GetChild(i).gameObject;
                    if (Application.isPlaying)
                        Destroy(child);
                    else
                        DestroyImmediate(child);
                }
            }
            _talentNodes.Clear();

            // First pass: instantiate nodes and group them by row.
            foreach (TalentDefinition talent in spec.talents)
            {
                if (talent == null)
                    continue;

                var go = Instantiate(_talentNodePanelPrefab, _talentNodePanelContainer);
                go.Initialize(talent);

                var talentNode = go.GetComponent<UI_TalentTreeNode>();
                talentNode.Initialize(talent);
                var row = GetRowNumber(talent.MinLevel);

                if (!_talentNodes.TryGetValue(row, out var nodesInRow))
                {
                    nodesInRow = new();
                    _talentNodes[row] = nodesInRow;
                }

                nodesInRow.Add(talentNode);
            }

            // Second pass: position nodes now that we know counts per row.
            var rows = new List<int>(_talentNodes.Keys);
            rows.Sort();
            foreach (var row in rows)
            {
                var nodesInRow = _talentNodes[row];
                int count = nodesInRow.Count;
                for (int index = 0; index < count; index++)
                {
                    float xPos = (index - (count - 1) * 0.5f) * _rowSpacing;
                    float yPos = -row * _rowSpacing;
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
