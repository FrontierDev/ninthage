using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    public class UI_ScrollingFog : MonoBehaviour
    {
        public Vector2 scrollSpeed = new Vector2(0.01f, 0.02f);

        private Material mat;
        private Vector2 offset;

        void Start()
        {
            var rawImage = GetComponent<RawImage>();
            mat = Instantiate(rawImage.material);
            rawImage.material = mat;
        }

        void Update()
        {
            offset += scrollSpeed * Time.deltaTime;
            mat.SetTextureOffset("_MainTex", offset);
        }

        void OnDestroy()
        {
            if (mat) Destroy(mat);
        }
    }
}