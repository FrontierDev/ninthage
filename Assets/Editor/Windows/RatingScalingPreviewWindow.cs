using UnityEngine;
using UnityEditor;

namespace Game.Editor.Windows
{
    public class RatingScalingPreviewWindow : EditorWindow
    {
        private const int ReferenceLevel = 60;
        private const float ScalingExponent = 1.5f;

        private float baseRatingPerPercent;
        private Vector2 scrollPos;
        private int simLevel = 60;
        private float simDesiredPercent = 10f;

        public static void Show(float baseRatingPerPercent)
        {
            var window = GetWindow<RatingScalingPreviewWindow>("Rating Scaling Preview");
            window.baseRatingPerPercent = baseRatingPerPercent;
            window.minSize = new Vector2(300, 400);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField($"Base Rating Per Percent: {baseRatingPerPercent:F1}", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Level", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Rating / 1%", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            for (int level = 1; level <= 100; level += (level < 10 ? 9 : 10))
            {
                float ratingPerPercent = Mathf.Max(1f, baseRatingPerPercent
                    * Mathf.Pow((float)level / ReferenceLevel, ScalingExponent));

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(level.ToString(), GUILayout.Width(60));
                EditorGUILayout.LabelField(ratingPerPercent.ToString("F2"));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Simulator", EditorStyles.boldLabel);
            simLevel = EditorGUILayout.IntField("Level", simLevel);
            simDesiredPercent = EditorGUILayout.FloatField("Desired %", simDesiredPercent);

            float simRatingPerPercent = Mathf.Max(1f, baseRatingPerPercent
                * Mathf.Pow((float)simLevel / ReferenceLevel, ScalingExponent));
            float requiredRating = simDesiredPercent * simRatingPerPercent;

            EditorGUILayout.LabelField($"Rating needed: {requiredRating:F1}");
        }
    }
}
