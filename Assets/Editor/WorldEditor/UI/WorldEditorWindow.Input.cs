using UnityEditor;
using UnityEngine;

namespace Game.Editor.WorldEditor
{
    public partial class WorldEditorWindow : EditorWindow
    {
        private void HandlePanZoomInput()
        {
            Event currentEvent = Event.current;

            if (currentEvent.type == EventType.ScrollWheel)
            {
                float zoomDelta = -currentEvent.delta.y * ZOOM_SENSITIVITY;
                _zoomLevel = Mathf.Clamp(_zoomLevel + zoomDelta, MIN_ZOOM, MAX_ZOOM);
                currentEvent.Use();
                Repaint();
            }

            if (currentEvent.type == EventType.KeyDown)
            {
                Vector2 panDelta = Vector2.zero;

                if (currentEvent.keyCode == KeyCode.W)
                    panDelta.y += PAN_SENSITIVITY;
                else if (currentEvent.keyCode == KeyCode.S)
                    panDelta.y -= PAN_SENSITIVITY;
                else if (currentEvent.keyCode == KeyCode.A)
                    panDelta.x += PAN_SENSITIVITY;
                else if (currentEvent.keyCode == KeyCode.D)
                    panDelta.x -= PAN_SENSITIVITY;

                if (panDelta != Vector2.zero)
                {
                    _panOffset += panDelta;
                    currentEvent.Use();
                    Repaint();
                }
            }
        }

        private Vector2 WorldToScreenPoint(Vector2 worldPoint, Rect scaledRect, Rect mainAreaRect)
        {
            // Convert pixel coordinates to normalized coordinates using reference texture dimensions
            Texture2D referenceTexture = _generatedHeightmapTexture ?? _heightmapTexture;
            if (referenceTexture == null)
                return Vector2.zero;

            float normalizedX = worldPoint.x / (referenceTexture.width - 1);
            float normalizedY = worldPoint.y / (referenceTexture.height - 1);

            float screenX = scaledRect.x + normalizedX * scaledRect.width;
            float screenY = scaledRect.y + normalizedY * scaledRect.height;

            return new Vector2(screenX, screenY);
        }

        private Vector2 ScreenToWorldPoint(Vector2 screenPos, Rect scaledRect, Rect mainAreaRect)
        {
            // Calculate position relative to the texture display
            float relativeX = screenPos.x - scaledRect.x;
            float relativeY = screenPos.y - scaledRect.y;

            // Normalize position within the texture display
            float normalizedX = relativeX / scaledRect.width;
            float normalizedY = relativeY / scaledRect.height;

            // Convert to pixel coordinates using reference texture dimensions
            Texture2D referenceTexture = _generatedHeightmapTexture ?? _heightmapTexture;
            if (referenceTexture == null)
                return Vector2.zero;

            float pixelX = normalizedX * (referenceTexture.width - 1);
            float pixelY = normalizedY * (referenceTexture.height - 1);

            return new Vector2(pixelX, pixelY);
        }
    }
}
