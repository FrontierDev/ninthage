using Game.Client.UI.Skin;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.UI
{
    /// <summary>
    /// Provides the shared Editor-time operation for applying one <see cref="UI_Skin"/>.
    /// </summary>
    internal static class UI_SkinEditorUtility
    {
        /// <summary>
        /// Applies a skin while recording its affected text components for Undo.
        /// </summary>
        /// <param name="skin">The skin to apply.</param>
        /// <param name="undoName">The Undo operation name.</param>
        /// <param name="errorMessage">The expected authoring error, if application failed.</param>
        /// <returns><see langword="true"/> when the skin was applied successfully.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="skin"/> is null.</exception>
        internal static bool TryApplyWithUndo(UI_Skin skin, string undoName, out string errorMessage)
        {
            if (skin == null)
                throw new ArgumentNullException(nameof(skin));

            var affectedObjects = new List<UnityEngine.Object>();
            var seenObjects = new HashSet<UnityEngine.Object>();
            var textObjects = new List<TMP_Text>();
            if (skin.TextBindings != null)
            {
                foreach (var binding in skin.TextBindings)
                {
                    if (binding?.Text != null && seenObjects.Add(binding.Text))
                    {
                        textObjects.Add(binding.Text);
                        affectedObjects.Add(binding.Text);
                    }
                }
            }

            var rectTransforms = new List<RectTransform>();
            if (skin.RectTransformBindings != null)
            {
                foreach (var binding in skin.RectTransformBindings)
                {
                    if (binding?.Target != null && seenObjects.Add(binding.Target))
                    {
                        rectTransforms.Add(binding.Target);
                        affectedObjects.Add(binding.Target);
                    }
                }
            }

            Undo.RecordObjects(affectedObjects.ToArray(), undoName);
            try
            {
                skin.Apply();
            }
            catch (InvalidOperationException exception)
            {
                errorMessage = exception.Message;
                return false;
            }

            foreach (var text in textObjects)
            {
                EditorUtility.SetDirty(text);
                PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            }

            foreach (var rectTransform in rectTransforms)
            {
                EditorUtility.SetDirty(rectTransform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(rectTransform);
            }

            errorMessage = null;
            return true;
        }
    }
}
