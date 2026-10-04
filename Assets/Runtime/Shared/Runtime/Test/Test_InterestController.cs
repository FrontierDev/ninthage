using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Shared.Networking;

namespace Game.Shared.Test
{
    public sealed class Test_InterestController : MonoBehaviour
    {
        private static Test_InterestController _instance;
        public static Test_InterestController Instance => _instance;

        private Action onMove;

        [SerializeField] Vector2Int currentTile;
        [SerializeField] Vector2Int lastTile;

        private void Start()
        {
            _instance = this;
            onMove += OnMove;
        }

        private void Update() { PollKeys(); }

        private void OnMove()
        {
            MovementService.onClientInterestRequest?.Invoke(currentTile);
            MovementService.Server_RequestChunkInterest(currentTile);
        }

        public Vector2Int GetCurrentTile() { return currentTile; }

        private void PollKeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.aKey.wasPressedThisFrame)
            {
                lastTile = currentTile;
                currentTile.x -= 1;
                onMove?.Invoke();
            }
            else if (keyboard.dKey.wasPressedThisFrame)
            {
                lastTile = currentTile;
                currentTile.x += 1;
                onMove?.Invoke();
            }
            else if (keyboard.wKey.wasPressedThisFrame)
            {
                lastTile = currentTile;
                currentTile.y += 1;
                onMove?.Invoke();
            }
            else if (keyboard.sKey.wasPressedThisFrame)
            {
                lastTile = currentTile;
                currentTile.y -= 1;
                onMove?.Invoke();
            }
        }
    }
}