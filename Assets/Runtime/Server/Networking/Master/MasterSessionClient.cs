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

        /// <summary>Closes a finalized gameplay session through the Master.</summary>
        internal async Task<MasterSessionProtocol.SessionResponse> CloseAsync(
            MasterSessionProtocol.CloseRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            (HttpStatusCode statusCode, string responseBody) = await _worldClient.PostJsonAsync(
                MasterSessionProtocol.CLOSE_PATH,
                JsonConvert.SerializeObject(request),
                cancellationToken).ConfigureAwait(false);

            MasterSessionProtocol.SessionResponse response = DeserializeSessionResponse(responseBody);
            ValidateStatus(statusCode, response.Result);
            return response;
        }

        /// <summary>Admits a character through its authenticated gameplay session.</summary>
        internal Task<MasterSessionProtocol.CharacterAdmissionResponse> AdmitCharacterAsync(
            MasterSessionProtocol.CharacterAdmissionRequest request,
            Guid expectedAccountID,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (expectedAccountID == Guid.Empty)
                throw new ArgumentException("A nonempty expected Account ID is required.", nameof(expectedAccountID));

            return PostSessionResultAsync<MasterSessionProtocol.CharacterAdmissionResponse>(
                MasterSessionProtocol.CHARACTER_ADMISSION_PATH,
                request,
                response => response.SessionResult,
                response => response.PersistenceResult,
                response => ValidateCharacterAdmissionResponse(response, request, expectedAccountID),
                "character admission",
                cancellationToken);
        }

        /// <summary>Leaves a character through its authenticated gameplay session.</summary>
        internal Task<MasterSessionProtocol.CharacterLeaveResponse> LeaveCharacterAsync(
            MasterSessionProtocol.CharacterLeaveRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return PostSessionResultAsync<MasterSessionProtocol.CharacterLeaveResponse>(
                MasterSessionProtocol.CHARACTER_LEAVE_PATH,
                request,
                response => response.SessionResult,
                response => response.PersistenceResult,
                ValidateCharacterLeaveResponse,
                "character leave",
                cancellationToken);
        }

        /// <summary>Loads an account's character roster through its gameplay session.</summary>
        internal Task<MasterSessionProtocol.CharacterRosterResponse> GetCharacterRosterAsync(
            MasterSessionProtocol.CharacterRosterRequest request,
            Guid expectedAccountID,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (expectedAccountID == Guid.Empty)
                throw new ArgumentException("A nonempty expected Account ID is required.", nameof(expectedAccountID));

            return PostSessionPersistenceAsync<MasterSessionProtocol.CharacterRosterResponse>(
                MasterSessionProtocol.CHARACTER_ROSTER_PATH,
                request,
                response => response.SessionResult,
                response => response.PersistenceResponse?.Result,
                response =>
                {
                    ValidateSessionPersistenceWrapper(response.SessionResult, response.PersistenceResponse);
                    if (response.SessionResult == MasterSessionProtocol.GameplaySessionResult.Accepted)
                        MasterPersistenceClient.ValidateCharacterRosterResponse(response.PersistenceResponse, expectedAccountID);
                },
                cancellationToken);
        }

        /// <summary>Creates a character through its gameplay session.</summary>
        internal Task<MasterSessionProtocol.CharacterMutationResponse> CreateCharacterAsync(
            MasterSessionProtocol.CharacterCreateRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return PostSessionPersistenceAsync<MasterSessionProtocol.CharacterMutationResponse>(
                MasterSessionProtocol.CHARACTER_CREATE_PATH,
                request,
                response => response.SessionResult,
                response => response.PersistenceResponse?.Result,
                response =>
                {
                    ValidateSessionPersistenceWrapper(response.SessionResult, response.PersistenceResponse);
                    if (response.SessionResult == MasterSessionProtocol.GameplaySessionResult.Accepted)
                        MasterPersistenceClient.ValidateCharacterMutationResponse(response.PersistenceResponse);
                },
                cancellationToken);
        }

        /// <summary>Soft-deletes a character through its gameplay session.</summary>
        internal Task<MasterSessionProtocol.CharacterMutationResponse> DeleteCharacterAsync(
            MasterSessionProtocol.CharacterDeleteRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return PostSessionPersistenceAsync<MasterSessionProtocol.CharacterMutationResponse>(
                MasterSessionProtocol.CHARACTER_DELETE_PATH,
                request,
                response => response.SessionResult,
                response => response.PersistenceResponse?.Result,
                response =>
                {
                    ValidateSessionPersistenceWrapper(response.SessionResult, response.PersistenceResponse);
                    if (response.SessionResult == MasterSessionProtocol.GameplaySessionResult.Accepted)
                    {
                        MasterPersistenceClient.ValidateExistingCharacterMutationResponse(
                            response.PersistenceResponse,
                            request.CharacterID);
                    }
                },
                cancellationToken);
        }

        /// <summary>Restores a character through its gameplay session.</summary>
        internal Task<MasterSessionProtocol.CharacterMutationResponse> RestoreCharacterAsync(
            MasterSessionProtocol.CharacterRestoreRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return PostSessionPersistenceAsync<MasterSessionProtocol.CharacterMutationResponse>(
                MasterSessionProtocol.CHARACTER_RESTORE_PATH,
                request,
                response => response.SessionResult,
                response => response.PersistenceResponse?.Result,
                response =>
                {
                    ValidateSessionPersistenceWrapper(response.SessionResult, response.PersistenceResponse);
                    if (response.SessionResult == MasterSessionProtocol.GameplaySessionResult.Accepted)
                    {
                        MasterPersistenceClient.ValidateExistingCharacterMutationResponse(
                            response.PersistenceResponse,
                            request.CharacterID);
                    }
                },
                cancellationToken);
        }

        private async Task<TResponse> PostSessionPersistenceAsync<TResponse>(
            string path,
            object request,
            Func<TResponse, MasterSessionProtocol.GameplaySessionResult> sessionResultSelector,
            Func<TResponse, MasterPersistenceProtocol.PersistenceResult?> persistenceResultSelector,
            Action<TResponse> validateResponse,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A nonblank session persistence path is required.", nameof(path));
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (sessionResultSelector == null)
                throw new ArgumentNullException(nameof(sessionResultSelector));
            if (persistenceResultSelector == null)
                throw new ArgumentNullException(nameof(persistenceResultSelector));
            if (validateResponse == null)
                throw new ArgumentNullException(nameof(validateResponse));

            (HttpStatusCode statusCode, string responseBody) = await _worldClient.PostJsonAsync(
                path,
                JsonConvert.SerializeObject(request),
                cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(responseBody))
                throw new InvalidOperationException("The Master session persistence response body was empty.");

            JObject responseObject;
            try
            {
                responseObject = JObject.Parse(responseBody);
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException("The Master session persistence response was malformed JSON.", exception);
            }

            if (!responseObject.TryGetValue("sessionResult", out JToken sessionResultToken)
                || sessionResultToken.Type == JTokenType.Null)
            {
                throw new InvalidOperationException("The Master session persistence response has no session result.");
            }

            TResponse response;
            try
            {
                response = JsonConvert.DeserializeObject<TResponse>(responseBody);
                if (response == null)
                    throw new InvalidOperationException("The Master session persistence response was null.");
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception exception) when (exception is JsonException || exception is ArgumentException)
            {
                throw new InvalidOperationException("The Master session persistence response was malformed.", exception);
            }

            MasterSessionProtocol.GameplaySessionResult sessionResult = sessionResultSelector(response);
            if (!Enum.IsDefined(typeof(MasterSessionProtocol.GameplaySessionResult), sessionResult))
                throw new InvalidOperationException("The Master session persistence response has an unknown session result.");

            MasterPersistenceProtocol.PersistenceResult? persistenceResult = persistenceResultSelector(response);
            if (sessionResult != MasterSessionProtocol.GameplaySessionResult.Accepted)
            {
                if (persistenceResult.HasValue)
                {
                    throw new InvalidOperationException(
                        "The rejected Master session persistence response contained a persistence result.");
                }

                ValidateStatus(statusCode, sessionResult);
            }
            else
            {
                if (!responseObject.TryGetValue("persistenceResponse", out JToken persistenceResponseToken)
                    || persistenceResponseToken.Type != JTokenType.Object)
                {
                    throw new InvalidOperationException(
                        "The accepted Master session persistence response has no persistence result field.");
                }

                JObject persistenceResponseObject = persistenceResponseToken as JObject;
                if (persistenceResponseObject == null
                    || !persistenceResponseObject.TryGetValue(
                        "result",
                        out JToken persistenceResultToken)
                    || persistenceResultToken.Type == JTokenType.Null)
                {
                    throw new InvalidOperationException(
                        "The accepted Master session persistence response has no persistence result field.");
                }

                if (!persistenceResult.HasValue
                    || !Enum.IsDefined(typeof(MasterPersistenceProtocol.PersistenceResult), persistenceResult.Value))
                {
                    throw new InvalidOperationException(
                        "The accepted Master session persistence response has no valid persistence result.");
                }

                MasterPersistenceClient.ValidatePersistenceStatus(statusCode, persistenceResult.Value);
            }

            validateResponse(response);
            return response;
        }

        private async Task<TResponse> PostSessionResultAsync<TResponse>(
            string path,
            object request,
            Func<TResponse, MasterSessionProtocol.GameplaySessionResult> sessionResultSelector,
            Func<TResponse, MasterPersistenceProtocol.PersistenceResult?> persistenceResultSelector,
            Action<TResponse> validateResponse,
            string operationDescription,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A nonblank session path is required.", nameof(path));
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (sessionResultSelector == null)
                throw new ArgumentNullException(nameof(sessionResultSelector));
            if (persistenceResultSelector == null)
                throw new ArgumentNullException(nameof(persistenceResultSelector));
            if (validateResponse == null)
                throw new ArgumentNullException(nameof(validateResponse));

            (HttpStatusCode statusCode, string responseBody) = await _worldClient.PostJsonAsync(
                path,
                JsonConvert.SerializeObject(request),
                cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(responseBody))
                throw new InvalidOperationException($"The Master {operationDescription} response body was empty.");

            JObject responseObject;
            try
            {
                responseObject = JObject.Parse(responseBody);
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException(
                    $"The Master {operationDescription} response was malformed JSON.",
                    exception);
            }

            if (!responseObject.TryGetValue("sessionResult", out JToken sessionResultToken)
                || sessionResultToken.Type == JTokenType.Null)
            {
                throw new InvalidOperationException(
                    $"The Master {operationDescription} response has no session result.");
            }

            TResponse response;
            try
            {
                response = JsonConvert.DeserializeObject<TResponse>(responseBody);
                if (response == null)
                    throw new InvalidOperationException($"The Master {operationDescription} response was null.");
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception exception) when (exception is JsonException || exception is ArgumentException)
            {
                throw new InvalidOperationException(
                    $"The Master {operationDescription} response was malformed.",
                    exception);
            }

            MasterSessionProtocol.GameplaySessionResult sessionResult = sessionResultSelector(response);
            if (!Enum.IsDefined(typeof(MasterSessionProtocol.GameplaySessionResult), sessionResult))
            {
                throw new InvalidOperationException(
                    $"The Master {operationDescription} response has an unknown session result.");
            }

            MasterPersistenceProtocol.PersistenceResult? persistenceResult = persistenceResultSelector(response);
            if (sessionResult != MasterSessionProtocol.GameplaySessionResult.Accepted)
            {
                if (persistenceResult.HasValue)
                {
                    throw new InvalidOperationException(
                        $"The rejected Master {operationDescription} response contained a persistence result.");
                }

                ValidateStatus(statusCode, sessionResult);
            }
            else
            {
                if (!persistenceResult.HasValue
                    || !Enum.IsDefined(typeof(MasterPersistenceProtocol.PersistenceResult), persistenceResult.Value))
                {
                    throw new InvalidOperationException(
                        $"The accepted Master {operationDescription} response has no valid persistence result.");
                }

                MasterPersistenceClient.ValidatePersistenceStatus(statusCode, persistenceResult.Value);
            }

            validateResponse(response);
            return response;
        }

        private static void ValidateCharacterAdmissionResponse(
            MasterSessionProtocol.CharacterAdmissionResponse response,
            MasterSessionProtocol.CharacterAdmissionRequest request,
            Guid expectedAccountID)
        {
            if (response.SessionResult != MasterSessionProtocol.GameplaySessionResult.Accepted)
            {
                if (response.PersistenceResult.HasValue
                    || response.CharacterPersistenceLeaseID.HasValue
                    || response.Revision.HasValue
                    || response.Character != null)
                {
                    throw new InvalidOperationException(
                        "The rejected Master character admission response contained success fields.");
                }

                return;
            }

            if (response.PersistenceResult.Value != MasterPersistenceProtocol.PersistenceResult.Accepted)
            {
                if (response.CharacterPersistenceLeaseID.HasValue
                    || response.Revision.HasValue
                    || response.Character != null)
                {
                    throw new InvalidOperationException(
                        "The rejected Master character admission response contained success fields.");
                }

                return;
            }

            if (!response.CharacterPersistenceLeaseID.HasValue
                || !response.Revision.HasValue
                || response.Character == null)
            {
                throw new InvalidOperationException(
                    "The accepted Master character admission response was missing success fields.");
            }

            MasterPersistenceClient.ValidateCharacterPersistenceState(
                response.Character,
                response.CharacterPersistenceLeaseID.Value,
                response.Revision.Value,
                expectedAccountID,
                request.CharacterID);
        }

        private static void ValidateCharacterLeaveResponse(
            MasterSessionProtocol.CharacterLeaveResponse response)
        {
            if (response == null)
                throw new InvalidOperationException("The Master character leave response was null.");
        }

        private static void ValidateSessionPersistenceWrapper<TResponse>(
            MasterSessionProtocol.GameplaySessionResult sessionResult,
            TResponse persistenceResponse)
            where TResponse : class
        {
            if (sessionResult == MasterSessionProtocol.GameplaySessionResult.Accepted)
            {
                if (persistenceResponse == null)
                    throw new InvalidOperationException(
                        "The accepted Master session persistence response has no persistence response.");
            }
            else if (persistenceResponse != null)
            {
                throw new InvalidOperationException(
                    "The rejected Master session persistence response contained a persistence response.");
            }
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
                MasterSessionProtocol.GameplaySessionResult.AuthenticationThrottled => HttpStatusCode.TooManyRequests,
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
