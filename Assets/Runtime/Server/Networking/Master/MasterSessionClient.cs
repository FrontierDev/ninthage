using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Server.Networking.Master
{
    /// <summary>Sends authenticated gameplay-session requests through the shared Master transport.</summary>
    internal sealed class MasterSessionClient
    {
        private readonly MasterWorldClient _worldClient;

        /// <summary>Creates a gameplay-session client over the existing authenticated World Server transport.</summary>
        internal MasterSessionClient(MasterWorldClient worldClient)
        {
            _worldClient = worldClient ?? throw new ArgumentNullException(nameof(worldClient));
        }

        /// <summary>Authenticates an account and creates or reclaims its gameplay session.</summary>
        internal async Task<MasterSessionProtocol.AuthenticateResponse> AuthenticateAsync(
            MasterSessionProtocol.AuthenticateRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            (HttpStatusCode statusCode, string responseBody) = await _worldClient.PostJsonAsync(
                MasterSessionProtocol.AUTHENTICATE_PATH,
                JsonConvert.SerializeObject(request),
                cancellationToken).ConfigureAwait(false);

            MasterSessionProtocol.AuthenticateResponse response =
                DeserializeAuthenticateResponse(responseBody);
            ValidateStatus(statusCode, response.Result);
            ValidateAuthenticateResponse(response);
            return response;
        }

        /// <summary>Disconnects an authenticated gameplay session.</summary>
        internal async Task<MasterSessionProtocol.SessionResponse> DisconnectAsync(
            MasterSessionProtocol.DisconnectRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            (HttpStatusCode statusCode, string responseBody) = await _worldClient.PostJsonAsync(
                MasterSessionProtocol.DISCONNECT_PATH,
                JsonConvert.SerializeObject(request),
                cancellationToken).ConfigureAwait(false);

            MasterSessionProtocol.SessionResponse response = DeserializeSessionResponse(responseBody);
            ValidateStatus(statusCode, response.Result);
            return response;
        }

        private static MasterSessionProtocol.AuthenticateResponse DeserializeAuthenticateResponse(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
                throw new InvalidOperationException("The Master gameplay-session authentication response body was empty.");

            ValidateResultField(responseBody, "gameplay-session authentication");
            try
            {
                MasterSessionProtocol.AuthenticateResponse response =
                    JsonConvert.DeserializeObject<MasterSessionProtocol.AuthenticateResponse>(responseBody);
                if (response == null)
                    throw new InvalidOperationException("The Master gameplay-session authentication response was null.");
                if (!Enum.IsDefined(typeof(MasterSessionProtocol.GameplaySessionResult), response.Result))
                    throw new InvalidOperationException("The Master gameplay-session authentication response has an unknown result.");
                return response;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception exception) when (exception is JsonException || exception is ArgumentException)
            {
                throw new InvalidOperationException(
                    "The Master gameplay-session authentication response was malformed.",
                    exception);
            }
        }

        private static MasterSessionProtocol.SessionResponse DeserializeSessionResponse(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
                throw new InvalidOperationException("The Master gameplay-session response body was empty.");

            ValidateResultField(responseBody, "gameplay-session");
            try
            {
                MasterSessionProtocol.SessionResponse response =
                    JsonConvert.DeserializeObject<MasterSessionProtocol.SessionResponse>(responseBody);
                if (response == null)
                    throw new InvalidOperationException("The Master gameplay-session response was null.");
                if (!Enum.IsDefined(typeof(MasterSessionProtocol.GameplaySessionResult), response.Result))
                    throw new InvalidOperationException("The Master gameplay-session response has an unknown result.");
                return response;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception exception) when (exception is JsonException || exception is ArgumentException)
            {
                throw new InvalidOperationException("The Master gameplay-session response was malformed.", exception);
            }
        }

        private static void ValidateResultField(string responseBody, string operationDescription)
        {
            JObject responseObject;
            try
            {
                responseObject = JObject.Parse(responseBody);
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException(
                    $"The Master {operationDescription} response was malformed.",
                    exception);
            }

            if (!responseObject.TryGetValue("result", out JToken resultToken)
                || resultToken.Type == JTokenType.Null)
            {
                throw new InvalidOperationException(
                    $"The Master {operationDescription} response has no result.");
            }
        }

        private static void ValidateAuthenticateResponse(MasterSessionProtocol.AuthenticateResponse response)
        {
            if (response.Result == MasterSessionProtocol.GameplaySessionResult.Accepted)
            {
                if (!response.GameplaySessionID.HasValue
                    || response.GameplaySessionID.Value == Guid.Empty
                    || !response.AccountID.HasValue
                    || response.AccountID.Value == Guid.Empty
                    || !response.HomeWorldID.HasValue
                    || response.HomeWorldID.Value == Guid.Empty
                    || !response.Revision.HasValue
                    || response.Revision.Value < 1
                    || !response.Disposition.HasValue
                    || !Enum.IsDefined(
                        typeof(MasterSessionProtocol.GameplaySessionDisposition),
                        response.Disposition.Value)
                    || (response.SelectedCharacterID.HasValue
                        && response.SelectedCharacterID.Value == Guid.Empty))
                {
                    throw new InvalidOperationException(
                        "The accepted Master gameplay-session authentication response was contradictory.");
                }

                return;
            }

            if (response.GameplaySessionID.HasValue
                || response.AccountID.HasValue
                || response.HomeWorldID.HasValue
                || response.Revision.HasValue
                || response.Disposition.HasValue
                || response.SelectedCharacterID.HasValue)
            {
                throw new InvalidOperationException(
                    "The rejected Master gameplay-session authentication response contained success fields.");
            }
        }

        private static void ValidateStatus(
            HttpStatusCode statusCode,
            MasterSessionProtocol.GameplaySessionResult result)
        {
            HttpStatusCode expectedStatusCode = result switch
            {
                MasterSessionProtocol.GameplaySessionResult.Accepted => HttpStatusCode.OK,
                MasterSessionProtocol.GameplaySessionResult.InvalidRequest => HttpStatusCode.BadRequest,
                MasterSessionProtocol.GameplaySessionResult.Unauthorized => HttpStatusCode.Unauthorized,
                MasterSessionProtocol.GameplaySessionResult.InvalidCredentials => HttpStatusCode.Forbidden,
                MasterSessionProtocol.GameplaySessionResult.WorldNotFound => HttpStatusCode.NotFound,
                MasterSessionProtocol.GameplaySessionResult.WorldNotActive => HttpStatusCode.ServiceUnavailable,
                MasterSessionProtocol.GameplaySessionResult.PersistenceUnavailable => HttpStatusCode.ServiceUnavailable,
                MasterSessionProtocol.GameplaySessionResult.StaleWorldServerRun => HttpStatusCode.Conflict,
                MasterSessionProtocol.GameplaySessionResult.HomeWorldMismatch => HttpStatusCode.Conflict,
                MasterSessionProtocol.GameplaySessionResult.SessionAlreadyActive => HttpStatusCode.Conflict,
                MasterSessionProtocol.GameplaySessionResult.StaleSession => HttpStatusCode.Conflict,
                _ => throw new InvalidOperationException("The Master gameplay-session response has an unknown result.")
            };

            if (statusCode != expectedStatusCode)
                throw new InvalidOperationException("The Master gameplay-session status did not match its result.");
        }
    }
}
