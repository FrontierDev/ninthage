using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Validation
{
    /// <summary>
    /// Assigns presentation authoring labels and validates native prefab LOD/culling state.
    /// </summary>
    internal sealed class LODCullingValidatorWindow : EditorWindow
    {
        private const string CategoryLabelPrefix = "NinthAge.Presentation.Category.";
        private const string LODGroupRequirementLabel = "NinthAge.Presentation.Requirement.LODGroup";
        private const string DistantRepresentationRequirementLabel =
            "NinthAge.Presentation.Requirement.DistantRepresentation";
        private const string DistantModeLabelPrefix = "NinthAge.Presentation.Distant.";
        private const string FinalLODLabel = "NinthAge.Presentation.Distant.FinalLOD";
        private const string ExternalLabel = "NinthAge.Presentation.Distant.External";

        private enum AssetCategory
        {
            Terrain,
            LargeArchitecture,
            TreesAndLargeVegetation,
            SmallPropsAndUndergrowth,
            CharactersAndNPCs,
            VFX,
            WorldSpaceUI
        }

        private enum ValidationSeverity
        {
            Info,
            Warning,
            Error
        }

        private enum DistantMode
        {
            FinalLOD,
            External
        }

        private sealed class Finding
        {
            public Finding(
                ValidationSeverity severity,
                string prefabPath,
                string category,
                string objectPath,
                string message)
            {
                Severity = severity;
                PrefabPath = prefabPath ?? string.Empty;
                Category = category ?? string.Empty;
                ObjectPath = objectPath ?? string.Empty;
                Message = message ?? string.Empty;
            }

            public ValidationSeverity Severity { get; }
            public string PrefabPath { get; }
            public string Category { get; }
            public string ObjectPath { get; }
            public string Message { get; }
        }

        private readonly List<Finding> _findings = new();
        private AssetCategory _selectedCategory;
        private bool _requiresLODGroup;
        private bool _requiresDistantRepresentation;
        private DistantMode _selectedDistantMode;
        private Vector2 _resultScrollPosition;
        private int _prefabsScanned;
        private int _errorCount;
        private int _warningCount;
        private int _passedPrefabCount;
        private int _skippedSelectionCount;

        [MenuItem("Tools/Validation/LOD and Culling Assets")]
        private static void ShowWindow()
        {
            var window = GetWindow<LODCullingValidatorWindow>();
            window.titleContent = new GUIContent("LOD and Culling");
            window.minSize = new Vector2(520f, 360f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Presentation authoring metadata", EditorStyles.boldLabel);
            _selectedCategory = (AssetCategory)EditorGUILayout.EnumPopup("Asset Category", _selectedCategory);
            _requiresLODGroup = EditorGUILayout.Toggle("Requires LODGroup", _requiresLODGroup);
            _requiresDistantRepresentation = EditorGUILayout.Toggle(
                "Requires Distant Representation",
                _requiresDistantRepresentation);

            if (_requiresDistantRepresentation)
                _selectedDistantMode = (DistantMode)EditorGUILayout.EnumPopup("Distant Mode", _selectedDistantMode);

            EditorGUILayout.Space();
            if (GUILayout.Button("Assign Authoring Metadata To Selected Prefabs"))
                AssignAuthoringMetadata();

            if (GUILayout.Button("Validate Selected Prefabs/Folders"))
                ValidateSelectedPrefabsAndFolders();

            EditorGUILayout.Space();
            DrawResults();
        }

        private void AssignAuthoringMetadata()
        {
            ResetResults();

            if (Selection.objects == null || Selection.objects.Length == 0)
            {
                AddFinding(ValidationSeverity.Info, string.Empty, string.Empty, string.Empty, "No selection; nothing to assign.");
                SortFindings();
                return;
            }

            var processedPaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var selectedObject in Selection.objects)
            {
                var assetPath = AssetDatabase.GetAssetPath(selectedObject);
                if (string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath))
                {
                    ReportSkippedSelection(assetPath, "Assignment only operates on directly selected prefab assets.");
                    continue;
                }

                if (IsProtectedPath(assetPath))
                {
                    ReportSkippedSelection(assetPath, "Protected package or third-party assets are not writable.");
                    continue;
                }

                if (!IsPrefabAssetPath(assetPath, selectedObject) || !processedPaths.Add(assetPath))
                {
                    ReportSkippedSelection(assetPath, "Selection is not a directly selected prefab asset.");
                    continue;
                }

                var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefabAsset == null)
                {
                    AddFinding(
                        ValidationSeverity.Error,
                        assetPath,
                        string.Empty,
                        string.Empty,
                        "Prefab asset could not be loaded as a GameObject; authoring metadata was not assigned.");
                    continue;
                }

                _prefabsScanned++;
                try
                {
                    var labels = new List<string>(AssetDatabase.GetLabels(prefabAsset));
                    RemoveManagedLabels(labels);
                    labels.Add(GetCategoryLabel(_selectedCategory));

                    if (_requiresLODGroup)
                        labels.Add(LODGroupRequirementLabel);

                    if (_requiresDistantRepresentation)
                    {
                        labels.Add(DistantRepresentationRequirementLabel);
                        labels.Add(GetDistantModeLabel(_selectedDistantMode));
                    }

                    AssetDatabase.SetLabels(prefabAsset, labels.ToArray());
                    _passedPrefabCount++;
                    AddFinding(
                        ValidationSeverity.Info,
                        assetPath,
                        _selectedCategory.ToString(),
                        string.Empty,
                        "Authoring metadata assigned.");
                }
                catch (Exception exception)
                {
                    AddFinding(
                        ValidationSeverity.Error,
                        assetPath,
                        string.Empty,
                        string.Empty,
                        $"Authoring metadata could not be assigned: {exception.Message}");
                }
            }

            SortFindings();
            Repaint();
        }

        private void ValidateSelectedPrefabsAndFolders()
        {
            ResetResults();
            var prefabPaths = ResolveValidationPrefabPaths();

            foreach (var prefabPath in prefabPaths)
                ValidatePrefab(prefabPath);

            SortFindings();
            Repaint();
        }

        private List<string> ResolveValidationPrefabPaths()
        {
            var prefabPaths = new HashSet<string>(StringComparer.Ordinal);

            if (Selection.objects == null || Selection.objects.Length == 0)
            {
                AddFinding(ValidationSeverity.Info, string.Empty, string.Empty, string.Empty, "No selection; nothing to validate.");
                return new List<string>();
            }

            foreach (var selectedObject in Selection.objects)
            {
                var assetPath = AssetDatabase.GetAssetPath(selectedObject);
                if (string.IsNullOrEmpty(assetPath))
                {
                    ReportSkippedSelection(assetPath, "Selection is not a project asset.");
                    continue;
                }

                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    if (IsProtectedPath(assetPath))
                    {
                        ReportSkippedSelection(assetPath, "Protected package or third-party folders are not inspected.");
                        continue;
                    }

                    var folderPrefabPaths = FindPrefabPaths(assetPath);
                    if (folderPrefabPaths.Count == 0)
                    {
                        AddFinding(
                            ValidationSeverity.Info,
                            assetPath,
                            string.Empty,
                            string.Empty,
                            "Selected folder contains no prefab assets; nothing to validate.");
                        _skippedSelectionCount++;
                        continue;
                    }

                    foreach (var folderPrefabPath in folderPrefabPaths)
                    {
                        if (!IsProtectedPath(folderPrefabPath))
                            prefabPaths.Add(folderPrefabPath);
                    }

                    continue;
                }

                if (IsProtectedPath(assetPath))
                {
                    ReportSkippedSelection(assetPath, "Protected package or third-party assets are not inspected.");
                    continue;
                }

                if (!IsPrefabAssetPath(assetPath, selectedObject))
                {
                    ReportSkippedSelection(assetPath, "Selection is not a prefab asset.");
                    continue;
                }

                prefabPaths.Add(assetPath);
            }

            var sortedPaths = new List<string>(prefabPaths);
            sortedPaths.Sort(StringComparer.Ordinal);
            return sortedPaths;
        }

        private static List<string> FindPrefabPaths(string folderPath)
        {
            var prefabPaths = new List<string>();
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
            foreach (var prefabGuid in prefabGuids)
            {
                var prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                if (!string.IsNullOrEmpty(prefabPath))
                    prefabPaths.Add(prefabPath);
            }

            prefabPaths.Sort(StringComparer.Ordinal);
            return prefabPaths;
        }

        private void ValidatePrefab(string prefabPath)
        {
            _prefabsScanned++;
            var errorsBeforeValidation = _errorCount;
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                AddFinding(
                    ValidationSeverity.Error,
                    prefabPath,
                    string.Empty,
                    string.Empty,
                    "Prefab could not be loaded as a GameObject.");
                return;
            }

            string[] labels;
            try
            {
                labels = AssetDatabase.GetLabels(prefabAsset);
            }
            catch (Exception exception)
            {
                AddFinding(
                    ValidationSeverity.Error,
                    prefabPath,
                    string.Empty,
                    string.Empty,
                    $"Presentation metadata could not be read: {exception.Message}");
                return;
            }

            var categoryLabels = new List<AssetCategory>();
            var hasUnknownCategoryLabel = false;
            foreach (var label in labels)
            {
                if (!label.StartsWith(CategoryLabelPrefix, StringComparison.Ordinal))
                    continue;

                var categoryName = label.Substring(CategoryLabelPrefix.Length);
                if (!Enum.TryParse(categoryName, false, out AssetCategory category) ||
                    !string.Equals(category.ToString(), categoryName, StringComparison.Ordinal))
                {
                    hasUnknownCategoryLabel = true;
                    AddFinding(
                        ValidationSeverity.Error,
                        prefabPath,
                        string.Empty,
                        string.Empty,
                        $"Unknown presentation category label '{label}'.");
                    continue;
                }

                categoryLabels.Add(category);
            }

            var hasResolvedCategory = categoryLabels.Count == 1 && !hasUnknownCategoryLabel;
            var resolvedCategory = hasResolvedCategory ? categoryLabels[0] : default;
            var categoryDisplayName = hasResolvedCategory ? resolvedCategory.ToString() : string.Empty;
            if (categoryLabels.Count == 0)
            {
                AddFinding(
                    ValidationSeverity.Error,
                    prefabPath,
                    string.Empty,
                    string.Empty,
                    "Missing presentation category metadata.");
            }
            else if (categoryLabels.Count > 1)
            {
                AddFinding(
                    ValidationSeverity.Error,
                    prefabPath,
                    string.Empty,
                    string.Empty,
                    "Ambiguous presentation category metadata; exactly one category is required.");
            }

            var hasLODGroupRequirement = ContainsLabel(labels, LODGroupRequirementLabel);
            var hasDistantRepresentationRequirement = ContainsLabel(labels, DistantRepresentationRequirementLabel);
            var distantModeLabels = new List<DistantMode>();
            var distantModeLabelCount = 0;
            var hasUnknownDistantModeLabel = false;
            foreach (var label in labels)
            {
                if (!label.StartsWith(DistantModeLabelPrefix, StringComparison.Ordinal))
                    continue;

                distantModeLabelCount++;
                var modeName = label.Substring(DistantModeLabelPrefix.Length);
                if (!Enum.TryParse(modeName, false, out DistantMode mode) ||
                    !string.Equals(mode.ToString(), modeName, StringComparison.Ordinal))
                {
                    hasUnknownDistantModeLabel = true;
                    AddFinding(
                        ValidationSeverity.Error,
                        prefabPath,
                        categoryDisplayName,
                        string.Empty,
                        $"Unknown distant representation mode label '{label}'.");
                    continue;
                }

                distantModeLabels.Add(mode);
            }

            if (hasDistantRepresentationRequirement)
            {
                if (distantModeLabelCount == 0)
                {
                    AddFinding(
                        ValidationSeverity.Error,
                        prefabPath,
                        categoryDisplayName,
                        string.Empty,
                        "Distant representation mode is missing.");
                }
                else if (distantModeLabelCount > 1)
                {
                    AddFinding(
                        ValidationSeverity.Error,
                        prefabPath,
                        categoryDisplayName,
                        string.Empty,
                        "Ambiguous distant representation mode; exactly one mode is required.");
                }
            }
            else if (distantModeLabelCount > 0)
            {
                AddFinding(
                    ValidationSeverity.Error,
                    prefabPath,
                    categoryDisplayName,
                    string.Empty,
                    "Distant representation mode is declared without a distant-representation requirement.");
            }

            var hasResolvedDistantMode = distantModeLabelCount == 1 &&
                distantModeLabels.Count == 1 &&
                !hasUnknownDistantModeLabel;

            GameObject prefabContents = null;
            try
            {
                prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
                if (prefabContents == null)
                {
                    AddFinding(
                        ValidationSeverity.Error,
                        prefabPath,
                        categoryDisplayName,
                        string.Empty,
                        "Prefab contents could not be opened for inspection.");
                    return;
                }

                var lodGroups = prefabContents.GetComponentsInChildren<LODGroup>(true);
                foreach (var lodGroup in lodGroups)
                    ValidateLODGroup(prefabPath, categoryDisplayName, prefabContents.transform, lodGroup);

                var terrainOwnsRequirements = hasResolvedCategory && resolvedCategory == AssetCategory.Terrain &&
                    (hasLODGroupRequirement ||
                        (hasDistantRepresentationRequirement && hasResolvedDistantMode &&
                            distantModeLabels[0] == DistantMode.FinalLOD));
                if (terrainOwnsRequirements)
                {
                    AddFinding(
                        ValidationSeverity.Error,
                        prefabPath,
                        categoryDisplayName,
                        string.Empty,
                        "Terrain LOD requirements are owned by the terrain pipeline, not the generic prefab rule.");
                }
                else
                {
                    if (hasLODGroupRequirement && lodGroups.Length == 0)
                    {
                        AddFinding(
                            ValidationSeverity.Error,
                            prefabPath,
                            categoryDisplayName,
                            string.Empty,
                            "LODGroup is explicitly required but no LODGroup exists.");
                    }

                    if (hasDistantRepresentationRequirement && hasResolvedDistantMode &&
                        distantModeLabels[0] == DistantMode.FinalLOD)
                    {
                        ValidateFinalLOD(prefabPath, categoryDisplayName, lodGroups);
                    }
                    else if (hasDistantRepresentationRequirement && hasResolvedDistantMode &&
                        distantModeLabels[0] == DistantMode.External)
                    {
                        AddFinding(
                            ValidationSeverity.Info,
                            prefabPath,
                            categoryDisplayName,
                            string.Empty,
                            "External distant representation requires integration/manual validation by its owning pipeline; it is not validated from this prefab.");
                    }
                }

                if (hasResolvedCategory)
                    ValidateCategorySpecificRules(
                        prefabPath,
                        categoryDisplayName,
                        resolvedCategory,
                        prefabContents.transform);
            }
            catch (Exception exception)
            {
                AddFinding(
                    ValidationSeverity.Error,
                    prefabPath,
                    categoryDisplayName,
                    string.Empty,
                    $"Prefab inspection failed: {exception.Message}");
            }
            finally
            {
                if (prefabContents != null)
                    PrefabUtility.UnloadPrefabContents(prefabContents);
            }

            if (_errorCount == errorsBeforeValidation)
                _passedPrefabCount++;
        }

        private void ValidateLODGroup(
            string prefabPath,
            string category,
            Transform prefabRoot,
            LODGroup lodGroup)
        {
            var componentPath = GetHierarchyPath(prefabRoot, lodGroup.transform) + "/LODGroup";
            LOD[] levels;
            try
            {
                levels = lodGroup.GetLODs();
            }
            catch (Exception exception)
            {
                AddFinding(
                    ValidationSeverity.Error,
                    prefabPath,
                    category,
                    componentPath,
                    $"LODGroup levels could not be read: {exception.Message}");
                return;
            }

            if (levels == null || levels.Length == 0)
            {
                AddFinding(
                    ValidationSeverity.Error,
                    prefabPath,
                    category,
                    componentPath,
                    "LODGroup must contain at least one LOD level.");
                return;
            }

            for (var levelIndex = 0; levelIndex < levels.Length; levelIndex++)
            {
                var level = levels[levelIndex];
                var transition = level.screenRelativeTransitionHeight;
                if (!IsFinite(transition))
                {
                    AddFinding(
                        ValidationSeverity.Error,
                        prefabPath,
                        category,
                        componentPath,
                        $"LOD{levelIndex} transition must be finite.");
                }
                else if (transition <= 0f || transition > 1f)
                {
                    AddFinding(
                        ValidationSeverity.Error,
                        prefabPath,
                        category,
                        componentPath,
                        $"LOD{levelIndex} transition must be greater than zero and at most one.");
                }

                if (levelIndex > 0 && levels[levelIndex - 1].screenRelativeTransitionHeight <= transition)
                {
                    AddFinding(
                        ValidationSeverity.Error,
                        prefabPath,
                        category,
                        componentPath,
                        $"LOD transitions must strictly descend from LOD0; LOD{levelIndex - 1} is not above LOD{levelIndex}.");
                }

                if (level.renderers == null || level.renderers.Length == 0)
                {
                    AddFinding(
                        ValidationSeverity.Error,
                        prefabPath,
                        category,
                        componentPath,
                        $"LOD{levelIndex} must contain at least one renderer reference.");
                    continue;
                }

                for (var rendererIndex = 0; rendererIndex < level.renderers.Length; rendererIndex++)
                {
                    if (level.renderers[rendererIndex] != null)
                        continue;

                    AddFinding(
                        ValidationSeverity.Error,
                        prefabPath,
                        category,
                        componentPath,
                        $"LOD{levelIndex} renderer reference {rendererIndex} is null.");
                }
            }
        }

        private void ValidateFinalLOD(string prefabPath, string category, LODGroup[] lodGroups)
        {
            if (lodGroups.Length == 0)
            {
                AddFinding(
                    ValidationSeverity.Error,
                    prefabPath,
                    category,
                    string.Empty,
                    "FinalLOD distant representation requires at least one LODGroup.");
                return;
            }

            var hasTwoLevelGroup = false;
            var hasFinalRendererSet = false;
            foreach (var lodGroup in lodGroups)
            {
                LOD[] levels;
                try
                {
                    levels = lodGroup.GetLODs();
                }
                catch (Exception)
                {
                    // ValidateLODGroup already records the read failure for this component.
                    continue;
                }

                if (levels == null || levels.Length < 2)
                    continue;

                hasTwoLevelGroup = true;
                var finalLevel = levels[levels.Length - 1];
                if (finalLevel.renderers == null)
                    continue;

                foreach (var renderer in finalLevel.renderers)
                {
                    if (renderer != null)
                    {
                        hasFinalRendererSet = true;
                        break;
                    }
                }

                if (hasFinalRendererSet)
                    break;
            }

            if (!hasTwoLevelGroup)
            {
                AddFinding(
                    ValidationSeverity.Error,
                    prefabPath,
                    category,
                    string.Empty,
                    "FinalLOD distant representation requires an LODGroup with at least two LOD levels.");
            }
            else if (!hasFinalRendererSet)
            {
                AddFinding(
                    ValidationSeverity.Error,
                    prefabPath,
                    category,
                    string.Empty,
                    "FinalLOD distant representation requires the final LOD of a qualifying group to contain at least one non-null renderer.");
            }
        }

        private void ValidateCategorySpecificRules(
            string prefabPath,
            string category,
            AssetCategory resolvedCategory,
            Transform prefabRoot)
        {
            if (resolvedCategory == AssetCategory.CharactersAndNPCs)
            {
                var animators = prefabRoot.GetComponentsInChildren<Animator>(true);
                foreach (var animator in animators)
                {
                    if (animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                        continue;

                    AddFinding(
                        ValidationSeverity.Warning,
                        prefabPath,
                        category,
                        GetHierarchyPath(prefabRoot, animator.transform) + "/Animator",
                        "Animator uses AlwaysAnimate; review whether gameplay requires animation while off-screen.");
                }
            }
            else if (resolvedCategory == AssetCategory.VFX)
            {
                var renderers = prefabRoot.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    var bounds = renderer.localBounds;
                    if (IsFinite(bounds.center.x) && IsFinite(bounds.center.y) && IsFinite(bounds.center.z) &&
                        IsFinite(bounds.size.x) && IsFinite(bounds.size.y) && IsFinite(bounds.size.z))
                    {
                        continue;
                    }

                    AddFinding(
                        ValidationSeverity.Error,
                        prefabPath,
                        category,
                        GetHierarchyPath(prefabRoot, renderer.transform) + "/Renderer",
                        "Renderer local bounds centre and size must contain only finite values.");
                }
            }
        }

        private static bool ContainsLabel(IEnumerable<string> labels, string expectedLabel)
        {
            foreach (var label in labels)
            {
                if (string.Equals(label, expectedLabel, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static string GetCategoryLabel(AssetCategory category)
        {
            return CategoryLabelPrefix + category;
        }

        private static string GetDistantModeLabel(DistantMode mode)
        {
            return mode == DistantMode.FinalLOD ? FinalLODLabel : ExternalLabel;
        }

        private static void RemoveManagedLabels(List<string> labels)
        {
            for (var index = labels.Count - 1; index >= 0; index--)
            {
                var label = labels[index];
                if (label.StartsWith(CategoryLabelPrefix, StringComparison.Ordinal) ||
                    string.Equals(label, LODGroupRequirementLabel, StringComparison.Ordinal) ||
                    string.Equals(label, DistantRepresentationRequirementLabel, StringComparison.Ordinal) ||
                    label.StartsWith(DistantModeLabelPrefix, StringComparison.Ordinal))
                {
                    labels.RemoveAt(index);
                }
            }
        }

        private static bool IsPrefabAssetPath(string assetPath, UnityEngine.Object selectedObject)
        {
            if (!assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                return false;

            if (selectedObject is GameObject selectedGameObject)
                return PrefabUtility.GetPrefabAssetType(selectedGameObject) != PrefabAssetType.NotAPrefab;

            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            return prefabAsset != null && PrefabUtility.GetPrefabAssetType(prefabAsset) != PrefabAssetType.NotAPrefab;
        }

        private static bool IsProtectedPath(string assetPath)
        {
            return IsPathOrChildOf(assetPath, "Packages") ||
                IsPathOrChildOf(assetPath, "Assets/ThirdParty") ||
                IsPathOrChildOf(assetPath, "Assets/TextMesh Pro");
        }

        private static bool IsPathOrChildOf(string assetPath, string rootPath)
        {
            return string.Equals(assetPath, rootPath, StringComparison.Ordinal) ||
                assetPath.StartsWith(rootPath + "/", StringComparison.Ordinal);
        }

        private void ReportSkippedSelection(string assetPath, string message)
        {
            _skippedSelectionCount++;
            AddFinding(ValidationSeverity.Info, assetPath, string.Empty, string.Empty, message);
        }

        private void ResetResults()
        {
            _findings.Clear();
            _prefabsScanned = 0;
            _errorCount = 0;
            _warningCount = 0;
            _passedPrefabCount = 0;
            _skippedSelectionCount = 0;
        }

        private void AddFinding(
            ValidationSeverity severity,
            string prefabPath,
            string category,
            string objectPath,
            string message)
        {
            _findings.Add(new Finding(severity, prefabPath, category, objectPath, message));
            if (severity == ValidationSeverity.Error)
                _errorCount++;
            else if (severity == ValidationSeverity.Warning)
                _warningCount++;
        }

        private void SortFindings()
        {
            _findings.Sort((left, right) =>
            {
                var comparison = StringComparer.Ordinal.Compare(left.PrefabPath, right.PrefabPath);
                if (comparison != 0)
                    return comparison;

                comparison = left.Severity.CompareTo(right.Severity);
                if (comparison != 0)
                    return comparison;

                comparison = StringComparer.Ordinal.Compare(left.ObjectPath, right.ObjectPath);
                if (comparison != 0)
                    return comparison;

                return StringComparer.Ordinal.Compare(left.Message, right.Message);
            });
        }

        private void DrawResults()
        {
            EditorGUILayout.LabelField(
                $"Prefabs scanned: {_prefabsScanned}    Errors: {_errorCount}    Warnings: {_warningCount}");
            EditorGUILayout.LabelField(
                $"Passed prefabs: {_passedPrefabCount}    Skipped selections: {_skippedSelectionCount}");

            _resultScrollPosition = EditorGUILayout.BeginScrollView(_resultScrollPosition, GUILayout.ExpandHeight(true));
            if (_findings.Count == 0)
            {
                EditorGUILayout.LabelField("No validation results yet.", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                foreach (var finding in _findings)
                    EditorGUILayout.LabelField(FormatFinding(finding), EditorStyles.wordWrappedLabel);
            }

            EditorGUILayout.EndScrollView();
        }

        private static string FormatFinding(Finding finding)
        {
            var category = string.IsNullOrEmpty(finding.Category) ? string.Empty : $" [{finding.Category}]";
            var location = string.IsNullOrEmpty(finding.ObjectPath) ? string.Empty : $" ({finding.ObjectPath})";
            var path = string.IsNullOrEmpty(finding.PrefabPath) ? string.Empty : $" {finding.PrefabPath}";
            return $"{finding.Severity.ToString().ToUpperInvariant()}{path}{category}{location}: {finding.Message}";
        }

        private static string GetHierarchyPath(Transform prefabRoot, Transform target)
        {
            var pathParts = new List<string>();
            var current = target;
            while (current != null)
            {
                pathParts.Insert(0, current.name);
                if (current == prefabRoot)
                    break;

                current = current.parent;
            }

            return string.Join("/", pathParts);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
