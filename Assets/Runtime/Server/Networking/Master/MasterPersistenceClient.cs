using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Server.Networking.Master
{
    /// <summary>Provides single-attempt persistence requests over the authenticated Master transport.</summary>
    internal sealed partial class MasterPersistenceClient
    {
        private readonly MasterWorldClient _worldClient;

        /// <summary>Creates a persistence client that reuses the existing World Server transport.</summary>
        internal MasterPersistenceClient(MasterWorldClient worldClient)
        {
            _worldClient = worldClient ?? throw new ArgumentNullException(nameof(worldClient));
        }

        /// <summary>Releases the character's persistence lease through the Master Server.</summary>
        internal Task<MasterPersistenceProtocol.PersistenceResponse> ReleaseCharacterAsync(
            MasterPersistenceProtocol.CharacterReleaseRequest request,
            CancellationToken cancellationToken)
        {
            return PostAsync<MasterPersistenceProtocol.PersistenceResponse>(
                MasterPersistenceProtocol.CHARACTER_RELEASE_PATH,
                request,
                response => response.Result,
                ValidatePersistenceResponse,
                cancellationToken);
        }

        private async Task<TResponse> PostAsync<TResponse>(
            string path,
            object request,
            Func<TResponse, MasterPersistenceProtocol.PersistenceResult> resultSelector,
            Action<TResponse> validateResponse,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A nonblank persistence path is required.", nameof(path));
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (resultSelector == null)
                throw new ArgumentNullException(nameof(resultSelector));
            if (validateResponse == null)
                throw new ArgumentNullException(nameof(validateResponse));

            string requestBody = JsonConvert.SerializeObject(request);
            (HttpStatusCode statusCode, string responseBody) = await _worldClient.PostJsonAsync(
                path,
                requestBody,
                cancellationToken).ConfigureAwait(false);

            JObject responseObject = ParseResponseObject(responseBody);
            if (!responseObject.TryGetValue("result", out JToken resultToken)
                || resultToken.Type == JTokenType.Null)
            {
                throw new InvalidOperationException("The Master persistence response has no result.");
            }

            TResponse response = DeserializeResponse<TResponse>(responseBody);
            MasterPersistenceProtocol.PersistenceResult result = resultSelector(response);
            if (!Enum.IsDefined(typeof(MasterPersistenceProtocol.PersistenceResult), result))
                throw new InvalidOperationException("The Master persistence response has an unknown result.");

            ValidatePersistenceStatus(statusCode, result);
            validateResponse(response);
            return response;
        }

        private static TResponse DeserializeResponse<TResponse>(string responseBody)
        {
            try
            {
                TResponse response = JsonConvert.DeserializeObject<TResponse>(responseBody);
                if (response == null)
                    throw new InvalidOperationException("The Master persistence response was null.");
                return response;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException("The Master persistence response was malformed.", exception);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException("The Master persistence response was malformed.", exception);
            }
        }

        private static JObject ParseResponseObject(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
                throw new InvalidOperationException("The Master persistence response body was empty.");

            try
            {
                JObject responseObject = JsonConvert.DeserializeObject<JObject>(responseBody);
                if (responseObject == null)
                    throw new InvalidOperationException("The Master persistence response was not a JSON object.");
                return responseObject;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException("The Master persistence response was malformed JSON.", exception);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException("The Master persistence response was malformed JSON.", exception);
            }
        }

        private static void ValidatePersistenceResponse(MasterPersistenceProtocol.PersistenceResponse response)
        {
            if (response == null)
                throw new InvalidOperationException("The Master persistence response was null.");
            if (!Enum.IsDefined(typeof(MasterPersistenceProtocol.PersistenceResult), response.Result))
                throw new InvalidOperationException("The Master persistence response has an unknown result.");
        }

        internal static void ValidatePersistenceStatus(
            HttpStatusCode statusCode,
            MasterPersistenceProtocol.PersistenceResult result)
        {
            HttpStatusCode expectedStatusCode = result switch
            {
                MasterPersistenceProtocol.PersistenceResult.Accepted => HttpStatusCode.OK,
                MasterPersistenceProtocol.PersistenceResult.InvalidRequest => HttpStatusCode.BadRequest,
                MasterPersistenceProtocol.PersistenceResult.Unauthorized => HttpStatusCode.Unauthorized,
                MasterPersistenceProtocol.PersistenceResult.InvalidCredentials => HttpStatusCode.Forbidden,
                MasterPersistenceProtocol.PersistenceResult.WorldNotFound => HttpStatusCode.NotFound,
                MasterPersistenceProtocol.PersistenceResult.AccountNotFound => HttpStatusCode.NotFound,
                MasterPersistenceProtocol.PersistenceResult.CharacterNotFound => HttpStatusCode.NotFound,
                MasterPersistenceProtocol.PersistenceResult.WorldNotActive => HttpStatusCode.ServiceUnavailable,
                MasterPersistenceProtocol.PersistenceResult.PersistenceUnavailable => HttpStatusCode.ServiceUnavailable,
                _ when Enum.IsDefined(typeof(MasterPersistenceProtocol.PersistenceResult), result) => HttpStatusCode.Conflict,
                _ => throw new InvalidOperationException("The Master persistence response has an unknown result.")
            };

            if (statusCode != expectedStatusCode)
                throw new InvalidOperationException("The Master persistence status did not match its result.");
        }
    }
}
