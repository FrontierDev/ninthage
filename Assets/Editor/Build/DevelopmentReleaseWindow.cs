using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Game.Editor.Build
{
    /// <summary>
    /// Provides the one-click local Development Release preparation workflow.
    /// </summary>
    internal sealed class DevelopmentReleaseWindow : EditorWindow
    {
        private string _masterRepositoryPath;
        private DevelopmentReleasePreparationResult _lastResult;
        private bool _isPreparing;

        [MenuItem("Window/NinthAge/Development Release")]
        private static void Open()
        {
            var window = GetWindow<DevelopmentReleaseWindow>();
            window.titleContent = new GUIContent("Development Release");
            window.minSize = new Vector2(520f, 420f);
            window.Show();
        }

        private void OnEnable()
        {
            _masterRepositoryPath = EditorPrefs.GetString(
                DevelopmentReleaseBuilder.MASTER_REPOSITORY_EDITOR_PREFS_KEY,
                string.Empty);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Development Release", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            DrawReadOnlyStatus();
            EditorGUILayout.Space(8f);
            DrawMasterRepositorySelector();
            EditorGUILayout.Space(8f);

            bool unavailable = _isPreparing
                || EditorApplication.isPlaying
                || EditorApplication.isPlayingOrWillChangePlaymode
                || EditorApplication.isCompiling
                || BuildPipeline.isBuildingPlayer;
            using (new EditorGUI.DisabledScope(unavailable || string.IsNullOrWhiteSpace(_masterRepositoryPath)))
            {
                if (GUILayout.Button("Prepare Development Release", GUILayout.Height(30f)))
                    PrepareRelease();
            }

            if (unavailable && !_isPreparing)
            {
                EditorGUILayout.HelpBox(
                    "Preparation is disabled while Unity is playing, compiling or building.",
                    MessageType.Info);
            }

            EditorGUILayout.Space(8f);
            DrawResult();
        }

        private void DrawReadOnlyStatus()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            string profileName = "Unavailable";
            string builderName = "Unavailable";
            if (settings != null)
            {
                profileName = settings.profileSettings.GetProfileName(settings.activeProfileId);
                builderName = settings.ActivePlayerDataBuilder == null
                    ? "Unavailable"
                    : settings.ActivePlayerDataBuilder.GetType().Name;
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.LabelField("Build Target", EditorUserBuildSettings.activeBuildTarget.ToString());
            EditorGUILayout.LabelField("Subtarget", EditorUserBuildSettings.standaloneBuildSubtarget.ToString());
            EditorGUILayout.LabelField("Addressables Profile", profileName);
            EditorGUILayout.LabelField("Player Data Builder", builderName);
            EditorGUILayout.LabelField("Player Version", PlayerSettings.bundleVersion);
            EditorGUILayout.LabelField(
                "Unity Git SHA",
                _lastResult == null || string.IsNullOrWhiteSpace(_lastResult.UnityGitSHA)
                    ? "Detected during preparation"
                    : _lastResult.UnityGitSHA);
            EditorGUI.EndDisabledGroup();
        }

        private void DrawMasterRepositorySelector()
        {
            EditorGUILayout.LabelField("Master Repository", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(_masterRepositoryPath);
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("Choose Master Repository…", GUILayout.Width(190f)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel(
                    "Choose NinthAge Master Repository",
                    _masterRepositoryPath,
                    string.Empty);
                if (!string.IsNullOrWhiteSpace(selectedPath))
                {
                    _masterRepositoryPath = Path.GetFullPath(selectedPath);
                    EditorPrefs.SetString(
                        DevelopmentReleaseBuilder.MASTER_REPOSITORY_EDITOR_PREFS_KEY,
                        _masterRepositoryPath);
                    Repaint();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Manifest Output", ResolveManifestPath(_masterRepositoryPath));
            EditorGUILayout.LabelField(
                "Carried forward",
                "protocol, gameplay revision/hash and database schema are not independently verified");
        }

        private void PrepareRelease()
        {
            _isPreparing = true;
            _lastResult = null;
            try
            {
                _lastResult = DevelopmentReleaseBuilder.Prepare(_masterRepositoryPath);
            }
            finally
            {
                _isPreparing = false;
                Repaint();
            }
        }

        private void DrawResult()
        {
            if (_isPreparing)
            {
                EditorGUILayout.HelpBox("Preparing local Addressables content and Development manifest…", MessageType.Info);
                return;
            }

            if (_lastResult == null)
                return;

            EditorGUILayout.LabelField("Last Preparation Result", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(_lastResult.Message, _lastResult.Succeeded ? MessageType.Info : MessageType.Error);
            if (!_lastResult.Succeeded)
                return;

            EditorGUILayout.LabelField("ReleaseID", _lastResult.ReleaseID);
            EditorGUILayout.LabelField("CatalogRevision", _lastResult.CatalogRevision.ToString());
            EditorGUILayout.LabelField("CatalogHash", _lastResult.CatalogHash);
            EditorGUILayout.LabelField("ContentStateSHA256", _lastResult.ContentStateSHA256);
            EditorGUILayout.LabelField("Unity Git SHA", _lastResult.UnityGitSHA);
            EditorGUILayout.LabelField("Manifest Path", _lastResult.ManifestPath);
            EditorGUILayout.HelpBox(
                "Rebuild graphical Player and World Server; restart Master to activate this release.",
                MessageType.Warning);
        }

        private static string ResolveManifestPath(string masterRepositoryPath)
        {
            if (string.IsNullOrWhiteSpace(masterRepositoryPath))
                return "Not configured";

            try
            {
                return Path.Combine(
                    Path.GetFullPath(masterRepositoryPath),
                    "src",
                    "NinthAge.Master",
                    "release-manifests",
                    "development.json");
            }
            catch (Exception)
            {
                return "Invalid Master repository path";
            }
        }
    }
}
