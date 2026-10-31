using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Server.Networking.Master
{
    /// <summary>Provides the authenticated World Server transport to the Master Server.</summary>
    internal sealed class MasterWorldClient : IDisposable
    {
        private const string MASTER_URL_ENVIRONMENT_VARIABLE = "NINTHAGE_MASTER_URL";
        private const string WORLD_SERVER_SECRET_ENVIRONMENT_VARIABLE = "NINTHAGE_WORLD_SERVER_SECRET";
        private const string DEVELOPMENT_MASTER_URL = "http://127.0.0.1:5080";
        private const string BEARER_SCHEME = "Bearer";
        private const string REGISTER_PATH = "world/v1/register";
        private const string HEARTBEAT_PATH = "world/v1/heartbeat";
        private const string UNREGISTER_PATH = "world/v1/unregister";

        private readonly HttpClient _httpClient;

        /// <summary>Creates an authenticated Master transport for one immutable World Server run identity.</summary>
        internal MasterWorldClient(
            string worldName,
            Guid worldServerRunID,
            WorldServerReleaseIdentity releaseIdentity)
        {
            if (string.IsNullOrWhiteSpace(worldName))
                throw new ArgumentException("A nonblank world name is required.", nameof(worldName));
            if (worldServerRunID == Guid.Empty)
                throw new ArgumentException("A nonempty World Server run ID is required.", nameof(worldServerRunID));
            if (releaseIdentity == null)
                throw new ArgumentNullException(nameof(releaseIdentity));

            bool development = Application.isEditor || UnityEngine.Debug.isDebugBuild;
            string masterURL = Environment.GetEnvironmentVariable(MASTER_URL_ENVIRONMENT_VARIABLE)?.Trim();
            Uri masterBaseAddress = ResolveMasterBaseAddress(masterURL, development);
            string worldServerSecret = Environment.GetEnvironmentVariable(WORLD_SERVER_SECRET_ENVIRONMENT_VARIABLE);
            ValidateWorldServerSecret(worldServerSecret);

            HttpClient httpClient = new HttpClient
            {
                BaseAddress = masterBaseAddress
            };
            try
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(BEARER_SCHEME, worldServerSecret);
            }
            catch (FormatException)
            {
                httpClient.Dispose();
                throw new InvalidOperationException("The World Server secret could not be configured as a bearer credential.");
            }

            WorldName = worldName;
            WorldServerRunID = worldServerRunID;
            ReleaseIdentity = releaseIdentity;
            _httpClient = httpClient;
        }

        /// <summary>Gets the immutable world name sent in registration.</summary>
        internal string WorldName { get; }

        /// <summary>Gets the immutable run ID sent in every request.</summary>
        internal Guid WorldServerRunID { get; }

        /// <summary>Gets the immutable release identity sent in registration.</summary>
        internal WorldServerReleaseIdentity ReleaseIdentity { get; }

        /// <summary>Registers this World Server and returns the validated Master response.</summary>
        internal async Task<MasterWorldProtocol.RegistrationResponse> RegisterAsync(
            CancellationToken cancellationToken)
        {
            var request = new MasterWorldProtocol.RegistrationRequest
            {
                WorldServerRunID = WorldServerRunID,
                WorldName = WorldName,
                ReleaseID = ReleaseIdentity.ReleaseID,
                ServerBuildID = ReleaseIdentity.ServerBuildID,
                ProtocolVersion = ReleaseIdentity.ProtocolVersion,
                GameplayContentRevision = ReleaseIdentity.GameplayContentRevision,
                GameplayContentManifestHash = ReleaseIdentity.GameplayContentManifestHash
            };
            (HttpStatusCode statusCode, string responseBody) = await PostJsonAsync(
                REGISTER_PATH,
                JsonConvert.SerializeObject(request),
                cancellationToken).ConfigureAwait(false);

            MasterWorldProtocol.RegistrationResponse response = DeserializeRegistrationResponse(responseBody);
            ValidateRegistrationStatus(statusCode, response.Result);
            ValidateRegistrationResponse(response);
            return response;
        }

        /// <summary>Renews the specified durable World Server lease.</summary>
        internal async Task<MasterWorldProtocol.LeaseResponse> HeartbeatAsync(
            Guid worldID,
            CancellationToken cancellationToken)
        {
            return await SendLeaseRequestAsync(HEARTBEAT_PATH, worldID, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Releases the specified durable World Server lease.</summary>
        internal async Task<MasterWorldProtocol.LeaseResponse> UnregisterAsync(
            Guid worldID,
            CancellationToken cancellationToken)
        {
            return await SendLeaseRequestAsync(UNREGISTER_PATH, worldID, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Renews a lease until cancellation or a rejected lease response occurs.</summary>
        internal async Task RunHeartbeatLoopAsync(
            Guid worldID,
            int heartbeatIntervalSeconds,
            CancellationToken cancellationToken)
        {
            if (worldID == Guid.Empty)
                throw new ArgumentException("A nonempty World ID is required.", nameof(worldID));
            if (heartbeatIntervalSeconds <= 0)
                throw new ArgumentException("The heartbeat interval must be positive.", nameof(heartbeatIntervalSeconds));

            while (true)
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(heartbeatIntervalSeconds),
                    cancellationToken).ConfigureAwait(false);
                MasterWorldProtocol.LeaseResponse response = await HeartbeatAsync(
                    worldID,
                    cancellationToken).ConfigureAwait(false);
                if (response.Result != MasterWorldProtocol.LeaseResult.Accepted)
                    throw new InvalidOperationException(response.Result.ToString());
            }
        }

        /// <summary>Disposes the one HTTP client owned by this transport.</summary>
        public void Dispose()
        {
            _httpClient.Dispose();
        }

        private async Task<MasterWorldProtocol.LeaseResponse> SendLeaseRequestAsync(
            string path,
            Guid worldID,
            CancellationToken cancellationToken)
        {
            if (worldID == Guid.Empty)
                throw new ArgumentException("A nonempty World ID is required.", nameof(worldID));

            var request = new MasterWorldProtocol.LeaseRequest
            {
                WorldID = worldID,
                WorldServerRunID = WorldServerRunID
            };
            (HttpStatusCode statusCode, string responseBody) = await PostJsonAsync(
                path,
                JsonConvert.SerializeObject(request),
                cancellationToken).ConfigureAwait(false);

            MasterWorldProtocol.LeaseResponse response = DeserializeLeaseResponse(responseBody);
            ValidateLeaseStatus(statusCode, response.Result);
            return response;
        }

        internal async Task<(HttpStatusCode StatusCode, string Body)> PostJsonAsync(
            string path,
            string requestBody,
            CancellationToken cancellationToken)
        {
            using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await _httpClient.PostAsync(
                path,
                content,
                cancellationToken).ConfigureAwait(false);
            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return (response.StatusCode, responseBody);
        }

        private static MasterWorldProtocol.RegistrationResponse DeserializeRegistrationResponse(string responseBody)
        {
            JObject responseObject = ParseResponseObject(responseBody);
            if (!responseObject.TryGetValue("result", out JToken resultToken) || resultToken.Type == JTokenType.Null)
                throw new InvalidOperationException("The Master registration response has no result.");

            try
            {
                MasterWorldProtocol.RegistrationResponse response =
                    JsonConvert.DeserializeObject<MasterWorldProtocol.RegistrationResponse>(responseBody);
                if (response == null)
                    throw new InvalidOperationException("The Master registration response was null.");
                if (!Enum.IsDefined(typeof(MasterWorldProtocol.RegistrationResult), response.Result))
                    throw new InvalidOperationException("The Master registration response has an unknown result.");
                return response;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception exception) when (exception is JsonException || exception is ArgumentException)
            {
                throw new InvalidOperationException("The Master registration response was malformed.", exception);
            }
        }

        private static MasterWorldProtocol.LeaseResponse DeserializeLeaseResponse(string responseBody)
        {
            JObject responseObject = ParseResponseObject(responseBody);
            if (!responseObject.TryGetValue("result", out JToken resultToken) || resultToken.Type == JTokenType.Null)
                throw new InvalidOperationException("The Master lease response has no result.");

            try
            {
                MasterWorldProtocol.LeaseResponse response =
                    JsonConvert.DeserializeObject<MasterWorldProtocol.LeaseResponse>(responseBody);
                if (response == null)
                    throw new InvalidOperationException("The Master lease response was null.");
                if (!Enum.IsDefined(typeof(MasterWorldProtocol.LeaseResult), response.Result))
                    throw new InvalidOperationException("The Master lease response has an unknown result.");
                return response;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception exception) when (exception is JsonException || exception is ArgumentException)
            {
                throw new InvalidOperationException("The Master lease response was malformed.", exception);
            }
        }

        private static JObject ParseResponseObject(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
                throw new InvalidOperationException("The Master response body was empty.");

            try
            {
                JObject responseObject = JsonConvert.DeserializeObject<JObject>(responseBody);
                if (responseObject == null)
                    throw new InvalidOperationException("The Master response was not a JSON object.");
                return responseObject;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException("The Master response was malformed JSON.", exception);
            }
        }

        private static void ValidateRegistrationResponse(MasterWorldProtocol.RegistrationResponse response)
        {
            bool hasWorldID = response.WorldID.HasValue && response.WorldID.Value != Guid.Empty;
            if (response.Result == MasterWorldProtocol.RegistrationResult.Accepted)
            {
                if (!hasWorldID
                    || !response.HeartbeatIntervalSeconds.HasValue
                    || response.HeartbeatIntervalSeconds.Value <= 0
                    || !response.LeaseTimeoutSeconds.HasValue
                    || response.LeaseTimeoutSeconds.Value <= response.HeartbeatIntervalSeconds.Value)
                {
                    throw new InvalidOperationException("The accepted Master registration response was contradictory.");
                }

                return;
            }

            if (response.WorldID.HasValue
                || response.HeartbeatIntervalSeconds.HasValue
                || response.LeaseTimeoutSeconds.HasValue)
            {
                throw new InvalidOperationException("The rejected Master registration response contained success fields.");
            }
        }

        private static void ValidateRegistrationStatus(
            HttpStatusCode statusCode,
            MasterWorldProtocol.RegistrationResult result)
        {
            HttpStatusCode expectedStatusCode = result switch
            {
                MasterWorldProtocol.RegistrationResult.Accepted => HttpStatusCode.OK,
                MasterWorldProtocol.RegistrationResult.Unauthorized => HttpStatusCode.Unauthorized,
                MasterWorldProtocol.RegistrationResult.WorldNotFound => HttpStatusCode.NotFound,
                MasterWorldProtocol.RegistrationResult.WorldAlreadyActive => HttpStatusCode.Conflict,
                MasterWorldProtocol.RegistrationResult.StaleWorldServerRun => HttpStatusCode.Conflict,
                MasterWorldProtocol.RegistrationResult.InvalidWorldServerRunID => HttpStatusCode.BadRequest,
                MasterWorldProtocol.RegistrationResult.InvalidWorldName => HttpStatusCode.BadRequest,
                MasterWorldProtocol.RegistrationResult.InvalidServerBuildID => HttpStatusCode.BadRequest,
                MasterWorldProtocol.RegistrationResult.InvalidReleaseID => HttpStatusCode.BadRequest,
                MasterWorldProtocol.RegistrationResult.InvalidProtocolVersion => HttpStatusCode.BadRequest,
                MasterWorldProtocol.RegistrationResult.InvalidGameplayContentRevision => HttpStatusCode.BadRequest,
                MasterWorldProtocol.RegistrationResult.InvalidGameplayContentManifestHash => HttpStatusCode.BadRequest,
                MasterWorldProtocol.RegistrationResult.ReleaseIDMismatch => HttpStatusCode.Conflict,
                MasterWorldProtocol.RegistrationResult.ServerBuildIDMismatch => HttpStatusCode.Conflict,
                MasterWorldProtocol.RegistrationResult.ProtocolVersionMismatch => HttpStatusCode.Conflict,
                MasterWorldProtocol.RegistrationResult.GameplayContentRevisionMismatch => HttpStatusCode.Conflict,
                MasterWorldProtocol.RegistrationResult.GameplayContentManifestHashMismatch => HttpStatusCode.Conflict,
                _ => throw new InvalidOperationException("The Master registration response has an unknown result.")
            };

            if (statusCode != expectedStatusCode)
                throw new InvalidOperationException("The Master registration status did not match its result.");
        }

        private static void ValidateLeaseStatus(HttpStatusCode statusCode, MasterWorldProtocol.LeaseResult result)
        {
            HttpStatusCode expectedStatusCode = result switch
            {
                MasterWorldProtocol.LeaseResult.Accepted => HttpStatusCode.OK,
                MasterWorldProtocol.LeaseResult.Unauthorized => HttpStatusCode.Unauthorized,
                MasterWorldProtocol.LeaseResult.WorldNotFound => HttpStatusCode.NotFound,
                MasterWorldProtocol.LeaseResult.StaleWorldServerRun => HttpStatusCode.Conflict,
                MasterWorldProtocol.LeaseResult.InvalidWorldID => HttpStatusCode.BadRequest,
                MasterWorldProtocol.LeaseResult.InvalidWorldServerRunID => HttpStatusCode.BadRequest,
                _ => throw new InvalidOperationException("The Master lease response has an unknown result.")
            };

            if (statusCode != expectedStatusCode)
                throw new InvalidOperationException("The Master lease status did not match its result.");
        }

        private static Uri ResolveMasterBaseAddress(string masterURL, bool development)
        {
            if (string.IsNullOrWhiteSpace(masterURL))
            {
                if (!development)
                    throw new InvalidOperationException(
                        "NINTHAGE_MASTER_URL is required outside Development.");

                masterURL = DEVELOPMENT_MASTER_URL;
            }

            Uri masterBaseAddress;
            try
            {
                masterBaseAddress = new Uri(masterURL, UriKind.Absolute);
            }
            catch (UriFormatException exception)
            {
                throw new InvalidOperationException("NINTHAGE_MASTER_URL must be an absolute URI.", exception);
            }

            bool isHttp = string.Equals(masterBaseAddress.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);
            bool isHttps = string.Equals(masterBaseAddress.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
            if (!isHttp && !isHttps)
            {
                throw new InvalidOperationException("NINTHAGE_MASTER_URL must use http or https.");
            }

            if (isHttp && !masterBaseAddress.IsLoopback)
            {
                throw new InvalidOperationException(
                    "NINTHAGE_MASTER_URL must use https unless it targets a loopback address.");
            }

            if (!masterBaseAddress.AbsolutePath.EndsWith("/", StringComparison.Ordinal))
            {
                UriBuilder uriBuilder = new UriBuilder(masterBaseAddress)
                {
                    Path = masterBaseAddress.AbsolutePath + "/"
                };
                masterBaseAddress = uriBuilder.Uri;
            }

            return masterBaseAddress;
        }

        private static void ValidateWorldServerSecret(string worldServerSecret)
        {
            if (string.IsNullOrWhiteSpace(worldServerSecret))
                throw new InvalidOperationException("NINTHAGE_WORLD_SERVER_SECRET is required.");

            byte[] decodedSecret;
            try
            {
                decodedSecret = Convert.FromBase64String(worldServerSecret);
            }
            catch (FormatException exception)
            {
                throw new InvalidOperationException(
                    "NINTHAGE_WORLD_SERVER_SECRET must be valid Base64.",
                    exception);
            }

            if (decodedSecret.Length != 32)
                throw new InvalidOperationException("NINTHAGE_WORLD_SERVER_SECRET must decode to exactly 32 bytes.");
        }
    }
}
