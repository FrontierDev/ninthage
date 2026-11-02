using System;
#if UNITY_EDITOR
using System.Diagnostics;
#endif
using System.IO;
using System.Text;
using UnityEngine;

namespace Game.Shared.Utility
{
    /// <summary>Reads the source branch for the running Editor checkout or packaged Player.</summary>
    public static class ClientSourceBranch
    {
        private const int GIT_COMMAND_TIMEOUT_MILLISECONDS = 3000;

        /// <summary>Gets the filename of the packaged source-branch presentation sidecar.</summary>
        public const string METADATA_FILE_NAME = "ninthage-client-source-branch.txt";

        /// <summary>
        /// Reads the source branch for the running Editor checkout or packaged Player.
        /// </summary>
        /// <returns>The validated, nonempty branch name.</returns>
        /// <exception cref="InvalidOperationException">
        /// Branch metadata is absent, malformed or Git cannot resolve a branch.
        /// </exception>
        public static string LoadCurrent()
        {
#if UNITY_EDITOR
            string projectRoot;
            try
            {
                projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            }
            catch (Exception exception) when (
                exception is ArgumentException
                || exception is NotSupportedException
                || exception is PathTooLongException)
            {
                throw new InvalidOperationException(
                    "The Unity project root could not be resolved for Git branch lookup.",
                    exception);
            }

            return LoadEditorBranch(projectRoot);
#else
            string streamingAssetsPath = Application.streamingAssetsPath;
            if (string.IsNullOrWhiteSpace(streamingAssetsPath))
                throw new InvalidOperationException(
                    "Application.streamingAssetsPath is required to load source branch metadata.");

            string metadataPath;
            try
            {
                metadataPath = Path.Combine(streamingAssetsPath, METADATA_FILE_NAME);
            }
            catch (Exception exception) when (
                exception is ArgumentException
                || exception is NotSupportedException
                || exception is PathTooLongException)
            {
                throw new InvalidOperationException(
                    "Application.streamingAssetsPath is not a valid source branch metadata directory.",
                    exception);
            }

            if (!File.Exists(metadataPath))
                throw new InvalidOperationException(
                    "Packaged source branch metadata is missing from StreamingAssets.");

            string metadata;
            try
            {
                metadata = File.ReadAllText(metadataPath, Encoding.UTF8);
            }
            catch (Exception exception) when (
                exception is IOException
                || exception is UnauthorizedAccessException
                || exception is ArgumentException
                || exception is NotSupportedException)
            {
                throw new InvalidOperationException(
                    "Packaged source branch metadata could not be read.",
                    exception);
            }

            return ValidateBranch(metadata, "Packaged source branch metadata");
#endif
        }

#if UNITY_EDITOR
        private static string LoadEditorBranch(string projectRoot)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "symbolic-ref --quiet --short HEAD",
                WorkingDirectory = projectRoot,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            using (var process = new Process())
            {
                process.StartInfo = startInfo;
                try
                {
                    if (!process.Start())
                        throw new InvalidOperationException("Git branch lookup did not start.");
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException(
                        $"Git branch lookup could not start ({exception.GetType().Name}).",
                        exception);
                }

                if (!process.WaitForExit(GIT_COMMAND_TIMEOUT_MILLISECONDS))
                {
                    try
                    {
                        if (!process.HasExited)
                            process.Kill();
                    }
                    catch (Exception exception)
                    {
                        throw new InvalidOperationException(
                            "Git branch lookup timed out and could not be terminated.",
                            exception);
                    }

                    throw new InvalidOperationException("Git branch lookup timed out after 3 seconds.");
                }

                string branchOutput = process.StandardOutput.ReadToEnd();
                if (process.ExitCode != 0)
                    throw new InvalidOperationException(
                        $"Git branch lookup failed with exit code {process.ExitCode}.");

                return ValidateBranch(branchOutput, "Git source branch output");
            }
        }
#endif

        private static string ValidateBranch(string rawBranch, string description)
        {
            if (rawBranch == null)
                throw new InvalidOperationException($"{description} is missing.");

            int branchLength = rawBranch.Length;
            while (branchLength > 0
                && (rawBranch[branchLength - 1] == '\r' || rawBranch[branchLength - 1] == '\n'))
            {
                branchLength--;
            }

            string branch = rawBranch.Substring(0, branchLength);
            if (branch.Length < 1 || branch.Length > 128)
                throw new InvalidOperationException(
                    $"{description} must contain 1 to 128 characters.");

            if (string.Equals(branch, "HEAD", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"{description} must identify a concrete branch, not HEAD.");

            foreach (char character in branch)
            {
                bool isAsciiLetter = character >= 'A' && character <= 'Z'
                    || character >= 'a' && character <= 'z';
                bool isAsciiDigit = character >= '0' && character <= '9';
                bool isAllowedPunctuation = character == '_'
                    || character == '-'
                    || character == '.'
                    || character == '/';
                if (char.IsWhiteSpace(character)
                    || char.IsControl(character)
                    || !isAsciiLetter && !isAsciiDigit && !isAllowedPunctuation)
                {
                    throw new InvalidOperationException(
                        $"{description} contains invalid branch characters.");
                }
            }

            return branch;
        }
    }
}
