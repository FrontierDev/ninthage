using Game.Shared.Networking;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Shared.Test
{
    /// <summary>Moves a test actor between interest chunks using keyboard input.</summary>
    public sealed class Test_InterestController : MonoBehaviour
    {
        private static Test_InterestController _instance;
        /// <summary>Gets the active test interest controller.</summary>
        public static Test_InterestController Instance => _instance;

        private Action _onMove;

        [SerializeField] Vector2Int currentTile;
        [SerializeField] Vector2Int lastTile;

        private void Start()
        {
            _instance = this;
            _onMove += OnMove;
        }

        private void Update() { PollKeys(); }

        private void OnMove()
        {
            MovementEndpoint.onClientInterestRequest?.Invoke(currentTile);
            MovementEndpoint.Server_RequestChunkInterest(currentTile);
        }

        /// <summary>Gets the current test chunk.</summary>
        public Vector2Int GetCurrentTile() { return currentTile; }

        private void PollKeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.aKey.wasPressedThisFrame)
            {
                lastTile = currentTile;
                currentTile.x -= 1;
                _onMove?.Invoke();
            }
            else if (keyboard.dKey.wasPressedThisFrame)
            {
                lastTile = currentTile;
                currentTile.x += 1;
                _onMove?.Invoke();
            }
            else if (keyboard.wKey.wasPressedThisFrame)
            {
                lastTile = currentTile;
                currentTile.y += 1;
                _onMove?.Invoke();
            }
            else if (keyboard.sKey.wasPressedThisFrame)
            {
                lastTile = currentTile;
                currentTile.y -= 1;
                _onMove?.Invoke();
            }
        }
    }
}
