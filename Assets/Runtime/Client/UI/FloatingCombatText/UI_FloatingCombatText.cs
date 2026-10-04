using UnityEngine;
using TMPro;

namespace Game.Client.UI
{
    public sealed class UI_FloatingCombatText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private float floatSpeed = 0.5f;
        [SerializeField] private float fadeDuration = 1f;
        [SerializeField] private float screenSize = 0.04f;

        private Vector3 floatDirection;
        private float elapsedTime;
        private float sizeMultiplier = 1f;
        private float durationMultiplier = 1f;

        public void Initialize(string content, Color color, Vector3 direction, float sizeMultiplier = 1f, float durationMultiplier = 1f)
        {
            text.text = content;
            text.color = color;
            floatDirection = direction.normalized;
            elapsedTime = 0f;
            this.sizeMultiplier = sizeMultiplier;
            this.durationMultiplier = durationMultiplier;
        }

        private void Update()
        {
            // Move the text upwards
            transform.position += floatDirection * floatSpeed * Time.deltaTime;

            // Face camera and scale to constant screen size
            Camera cam = Camera.main;
            if (cam != null)
            {
                transform.rotation = cam.transform.rotation;

                float dist = Vector3.Distance(cam.transform.position, transform.position);
                float frustumHeight = 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float scale = frustumHeight * screenSize * sizeMultiplier;
                transform.localScale = Vector3.one * scale;
            }

            // Fade out over time
            elapsedTime += Time.deltaTime;
            float totalDuration = fadeDuration * durationMultiplier;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / totalDuration);
            var c = text.color;
            c.a = alpha;
            text.color = c;

            if (elapsedTime >= totalDuration)
                Destroy(gameObject);
        }
    }
}