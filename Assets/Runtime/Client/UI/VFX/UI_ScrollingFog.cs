using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_ScrollingFog</c> implementation.
    /// </summary>
    public class UI_ScrollingFog : MonoBehaviour
    {
        /// <summary>
        /// Gets or sets the texture scroll speed.
        /// </summary>
        public Vector2 scrollSpeed = new Vector2(0.01f, 0.02f);

        private Material _mat;
        private Vector2 _offset;

        void Start()
        {
            var rawImage = GetComponent<RawImage>();
            _mat = Instantiate(rawImage.material);
            rawImage.material = _mat;
        }

        void Update()
        {
            _offset += scrollSpeed * Time.deltaTime;
            _mat.SetTextureOffset("_MainTex", _offset);
        }

        void OnDestroy()
        {
            if (_mat) Destroy(_mat);
        }
    }
}
