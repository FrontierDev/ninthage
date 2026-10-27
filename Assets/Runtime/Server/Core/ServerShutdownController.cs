using Debug = Game.Shared.Utility.FormattedDebug;
using System;
using System.Collections;
using UnityEngine;

namespace Game.Server.Core
{
    /// <summary>Owns external shutdown requests and marshals graceful server exit onto the Unity main thread.</summary>
    internal sealed class ServerShutdownController : MonoBehaviour
    {
        internal const float NETWORK_SHUTDOWN_TIMEOUT_SECONDS = 10f;

        private static readonly object REQUEST_LOCK = new object();
        private static ServerShutdownController _active;
        private static bool _requestPending;
        private static bool _shutdownRequested;
        private static string _pendingReasonCode;
        private static string _pendingMessage;

        private bool _shutdownCoroutineStarted;
        private bool _exitRequested;

        internal static bool IsShutdownRequested
        {
            get
            {
                lock (REQUEST_LOCK)
                    return _shutdownRequested;
            }
        }

        internal static void Attach(MonoBehaviour host)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));

            ServerShutdownController controller = host.GetComponent<ServerShutdownController>();
            if (controller == null)
                controller = host.gameObject.AddComponent<ServerShutdownController>();

            lock (REQUEST_LOCK)
            {
                _active = controller;
                _requestPending = false;
                _shutdownRequested = false;
                _pendingReasonCode = null;
                _pendingMessage = null;
            }

            controller._shutdownCoroutineStarted = false;
            controller._exitRequested = false;
        }

        internal static void Detach()
        {
            lock (REQUEST_LOCK)
            {
                _active = null;
                _requestPending = false;
                _shutdownRequested = false;
                _pendingReasonCode = null;
                _pendingMessage = null;
            }
        }

        internal static void RequestShutdown(string reasonCode, string message)
        {
            if (string.IsNullOrWhiteSpace(reasonCode))
                throw new ArgumentException("A shutdown reason code is required.", nameof(reasonCode));
            if (reasonCode.Length > 128)
                throw new ArgumentException("The shutdown reason code exceeds its bounded length.", nameof(reasonCode));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("A shutdown message is required.", nameof(message));
            if (message.Length > 1024)
                throw new ArgumentException("The shutdown message exceeds its bounded length.", nameof(message));

            lock (REQUEST_LOCK)
            {
                if (_active == null
                    || _shutdownRequested
                    || ServerInitialization.IsFatalShutdownRequested)
                {
                    return;
                }

                _shutdownRequested = true;
                _requestPending = true;
                _pendingReasonCode = reasonCode;
                _pendingMessage = message;
            }
        }

        internal static void ClearPendingRequest()
        {
            lock (REQUEST_LOCK)
            {
                _requestPending = false;
                _pendingReasonCode = null;
                _pendingMessage = null;
                if (_active == null || !_active._shutdownCoroutineStarted)
                {
                    _shutdownRequested = false;
                }
            }
        }

        internal static void CompleteShutdown(int exitCode)
        {
            ServerShutdownController controller;
            lock (REQUEST_LOCK)
            {
                controller = _active;
                if (controller == null || controller._exitRequested)
                    return;

                controller._exitRequested = true;
                _active = null;
                _requestPending = false;
                _shutdownRequested = false;
                _pendingReasonCode = null;
                _pendingMessage = null;
            }

            Debug.Log($"Server shutdown teardown completed. Exit code: {exitCode}.");
            if (!Application.isEditor)
            {
                Debug.Log($"Server is closing now. Exit code: {exitCode}.");
                Application.Quit(exitCode);
            }
        }

        private void Update()
        {
            if (_shutdownCoroutineStarted || ServerInitialization.IsFatalShutdownRequested)
                return;

            if (!TryTakePendingRequest(out string reasonCode, out string message))
                return;

            _shutdownCoroutineStarted = true;
            Debug.Log($"Graceful server shutdown requested: {reasonCode}.");
            StartCoroutine(RunGracefulShutdown(reasonCode, message));
        }

        private IEnumerator RunGracefulShutdown(string reasonCode, string message)
        {
            yield return ServerInitialization.BeginGracefulShutdown(reasonCode, message);
            CompleteShutdown(ServerInitialization.ShutdownExitCode);
        }

        private bool TryTakePendingRequest(out string reasonCode, out string message)
        {
            lock (REQUEST_LOCK)
            {
                if (!_requestPending)
                {
                    reasonCode = null;
                    message = null;
                    return false;
                }

                _requestPending = false;
                reasonCode = _pendingReasonCode;
                message = _pendingMessage;
                _pendingReasonCode = null;
                _pendingMessage = null;
                return true;
            }
        }

        private void OnDestroy()
        {
            if (!ReferenceEquals(_active, this))
                return;

            Detach();
        }
    }
}
