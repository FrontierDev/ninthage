using Debug = Game.Shared.Utility.FormattedDebug;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Game.Server.Core
{
    /// <summary>Owns the local World Server shutdown request boundary.</summary>
    internal sealed class ServerControlEndpoint
    {
        private const int MAX_REQUEST_BYTES = 8192;
        private const int SOCKET_TIMEOUT_MILLISECONDS = 2000;
        private const int THREAD_JOIN_TIMEOUT_MILLISECONDS = 1000;
        private const string SHUTDOWN_PATH = "/control/shutdown";

        private static readonly object STATE_LOCK = new object();
        private static ServerControlEndpoint _active;

        private readonly TcpListener _listener;
        private readonly Thread _listenerThread;
        private volatile bool _stopping;

        private ServerControlEndpoint(int controlPort)
        {
            _listener = new TcpListener(IPAddress.Loopback, controlPort);
            _listener.Start();
            try
            {
                _listenerThread = new Thread(ListenLoop)
                {
                    IsBackground = true,
                    Name = "NinthAge.WorldServerControl"
                };
            }
            catch
            {
                _listener.Stop();
                throw;
            }
        }

        internal static void Attach(MonoBehaviour host, int controlPort)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            if (controlPort < 1 || controlPort > 65535)
                throw new ArgumentOutOfRangeException(nameof(controlPort), "The control port must be in the range 1..65535.");

            ServerControlEndpoint endpoint;
            lock (STATE_LOCK)
            {
                if (_active != null)
                    throw new InvalidOperationException("A World Server control endpoint is already active.");

                endpoint = new ServerControlEndpoint(controlPort);
                _active = endpoint;
            }

            try
            {
                endpoint._listenerThread.Start();
            }
            catch
            {
                lock (STATE_LOCK)
                {
                    if (ReferenceEquals(_active, endpoint))
                        _active = null;
                }

                endpoint.StopAndJoin();
                throw;
            }

            Debug.Log($"World Server control endpoint listening at http://127.0.0.1:{controlPort}{SHUTDOWN_PATH}");
        }

        internal static void Detach()
        {
            ServerControlEndpoint endpoint;
            lock (STATE_LOCK)
            {
                endpoint = _active;
                if (endpoint == null)
                    return;

                endpoint._stopping = true;
                _active = null;
            }

            endpoint.StopAndJoin();
        }

        private void ListenLoop()
        {
            while (!_stopping)
            {
                TcpClient client;
                try
                {
                    client = _listener.AcceptTcpClient();
                }
                catch (SocketException)
                {
                    if (_stopping)
                        return;

                    continue;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }

                try
                {
                    ProcessClient(client);
                }
                catch
                {
                    TryWriteResponse(client, 500, "Internal Server Error", "{\"status\":\"internal_error\"}");
                }
                finally
                {
                    client.Close();
                }
            }
        }

        private void ProcessClient(TcpClient client)
        {
            client.ReceiveTimeout = SOCKET_TIMEOUT_MILLISECONDS;
            client.SendTimeout = SOCKET_TIMEOUT_MILLISECONDS;

            using (NetworkStream stream = client.GetStream())
            {
                if (_stopping)
                {
                    WriteResponse(stream, 503, "Service Unavailable", "{\"status\":\"stopping\"}");
                    return;
                }

                if (!TryReadHeaders(stream, out byte[] requestBytes, out int requestLength))
                {
                    WriteResponse(stream, 400, "Bad Request", "{\"status\":\"bad_request\"}");
                    return;
                }

                string request = Encoding.UTF8.GetString(requestBytes, 0, requestLength);
                int requestLineEnd = request.IndexOf("\r\n", StringComparison.Ordinal);
                if (requestLineEnd <= 0)
                {
                    WriteResponse(stream, 400, "Bad Request", "{\"status\":\"bad_request\"}");
                    return;
                }

                int headerEnd = request.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                if (!HasValidHeaders(request, requestLineEnd, headerEnd))
                {
                    WriteResponse(stream, 400, "Bad Request", "{\"status\":\"bad_request\"}");
                    return;
                }

                string[] requestParts = request
                    .Substring(0, requestLineEnd)
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (requestParts.Length != 3 || requestParts[2] != "HTTP/1.1")
                {
                    WriteResponse(stream, 400, "Bad Request", "{\"status\":\"bad_request\"}");
                    return;
                }

                if (requestParts[0] != "POST")
                {
                    WriteResponse(stream, 405, "Method Not Allowed", "{\"status\":\"method_not_allowed\"}");
                    return;
                }

                if (requestParts[1] != SHUTDOWN_PATH)
                {
                    WriteResponse(stream, 404, "Not Found", "{\"status\":\"not_found\"}");
                    return;
                }

                if (_stopping)
                {
                    WriteResponse(stream, 503, "Service Unavailable", "{\"status\":\"stopping\"}");
                    return;
                }

                ServerShutdownController.RequestShutdown(
                    "CONTROL_SHUTDOWN",
                    "Shutdown requested through the World Server control endpoint.");
                WriteResponse(stream, 202, "Accepted", "{\"status\":\"shutdown_requested\"}");
            }
        }

        private static bool TryReadHeaders(NetworkStream stream, out byte[] requestBytes, out int requestLength)
        {
            requestBytes = new byte[MAX_REQUEST_BYTES];
            requestLength = 0;
            while (requestLength < MAX_REQUEST_BYTES)
            {
                int bytesRead = stream.Read(requestBytes, requestLength, MAX_REQUEST_BYTES - requestLength);
                if (bytesRead <= 0)
                    return false;

                requestLength += bytesRead;
                if (requestLength >= 4 && ContainsHeaderTerminator(requestBytes, requestLength))
                    return true;
            }

            return false;
        }

        private static bool ContainsHeaderTerminator(byte[] requestBytes, int requestLength)
        {
            for (int i = 3; i < requestLength; i++)
            {
                if (requestBytes[i - 3] == '\r'
                    && requestBytes[i - 2] == '\n'
                    && requestBytes[i - 1] == '\r'
                    && requestBytes[i] == '\n')
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasValidHeaders(string request, int requestLineEnd, int headerEnd)
        {
            if (headerEnd < 0)
                return false;

            int headerStart = requestLineEnd + 2;
            while (headerStart < headerEnd)
            {
                int lineEnd = request.IndexOf("\r\n", headerStart, StringComparison.Ordinal);
                if (lineEnd < 0 || lineEnd > headerEnd)
                    return false;

                int separator = request.IndexOf(':', headerStart, lineEnd - headerStart);
                if (separator <= headerStart)
                    return false;

                headerStart = lineEnd + 2;
            }

            return headerStart == headerEnd;
        }

        private static void WriteResponse(NetworkStream stream, int statusCode, string reasonPhrase, string body)
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            string responseHeaders =
                $"HTTP/1.1 {statusCode} {reasonPhrase}\r\n"
                + "Content-Type: application/json; charset=utf-8\r\n"
                + $"Content-Length: {bodyBytes.Length}\r\n"
                + "Connection: close\r\n\r\n";
            byte[] headerBytes = Encoding.UTF8.GetBytes(responseHeaders);
            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Write(bodyBytes, 0, bodyBytes.Length);
        }

        private static void TryWriteResponse(TcpClient client, int statusCode, string reasonPhrase, string body)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                    WriteResponse(stream, statusCode, reasonPhrase, body);
            }
            catch
            {
                // The client may have disconnected before an error response could be sent.
            }
        }

        private void StopAndJoin()
        {
            try
            {
                _listener.Stop();
            }
            catch (SocketException exception)
            {
                Debug.Warning("Failed to stop the World Server control endpoint listener: " + exception.Message);
            }

            if (_listenerThread == null
                || Thread.CurrentThread == _listenerThread
                || _listenerThread.Join(THREAD_JOIN_TIMEOUT_MILLISECONDS))
            {
                return;
            }

            Debug.Warning("World Server control endpoint thread did not stop within one second; continuing shutdown.");
        }
    }
}
