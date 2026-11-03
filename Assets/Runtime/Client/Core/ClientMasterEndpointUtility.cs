using System;
using Game.Shared;

namespace Game.Client
{
    /// <summary>Builds validated endpoints for the configured Master Server.</summary>
    internal static class ClientMasterEndpointUtility
    {
        internal static bool TryCreateEndpoint(
            string relativePath,
            out Uri endpoint,
            out string failure)
        {
            endpoint = null;

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                failure = "Master endpoint path is blank";
                return false;
            }

            if (relativePath.StartsWith("/", StringComparison.Ordinal) ||
                relativePath.StartsWith("\\", StringComparison.Ordinal) ||
                !Uri.TryCreate(relativePath, UriKind.Relative, out Uri relativeUri))
            {
                failure = "Master endpoint path is not a relative path without a leading slash";
                return false;
            }

            string masterServerUrl;
            try
            {
                masterServerUrl = GameConfigurationManager.Config.MasterServerURL;
            }
            catch (InvalidOperationException)
            {
                failure = "Game configuration is unavailable";
                return false;
            }

            if (string.IsNullOrWhiteSpace(masterServerUrl))
            {
                failure = "Master Server URL is blank";
                return false;
            }

            if (!Uri.TryCreate(masterServerUrl, UriKind.Absolute, out Uri masterServerUri))
            {
                failure = "Master Server URL is not absolute";
                return false;
            }

            if ((masterServerUri.Scheme != Uri.UriSchemeHttp && masterServerUri.Scheme != Uri.UriSchemeHttps) ||
                string.IsNullOrEmpty(masterServerUri.Host))
            {
                failure = "Master Server URL does not use HTTP(S) with an authority";
                return false;
            }

            if (!string.IsNullOrEmpty(masterServerUri.UserInfo) ||
                !string.IsNullOrEmpty(masterServerUri.Query) ||
                !string.IsNullOrEmpty(masterServerUri.Fragment))
            {
                failure = "Master Server URL contains unsupported user info, query, or fragment data";
                return false;
            }

            if (!string.Equals(masterServerUri.AbsolutePath, "/", StringComparison.Ordinal))
            {
                failure = "Master Server URL must not contain a path";
                return false;
            }

            endpoint = new Uri(masterServerUri, relativeUri);
            failure = null;
            return true;
        }
    }
}
