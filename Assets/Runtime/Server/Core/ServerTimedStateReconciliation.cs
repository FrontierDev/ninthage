using System;

namespace Game.Server.Core
{
    /// <summary>Identifies the only saved UTC origin currently supported by reconciliation.</summary>
    public enum ServerUtcSource
    {
        /// <summary>No trusted server source was supplied.</summary>
        Unspecified,

        /// <summary>The instant was read from server-owned persistence.</summary>
        ServerPersistence
    }

    /// <summary>Contains validated real elapsed time and the domain-authorised eligible portion.</summary>
    public readonly struct ServerTimedStateReconciliationResult
    {
        internal ServerTimedStateReconciliationResult(TimeSpan validatedRealElapsed, TimeSpan eligibleElapsed)
        {
            ValidatedRealElapsed = validatedRealElapsed;
            EligibleElapsed = eligibleElapsed;
        }

        /// <summary>Gets the elapsed server UTC time validated by this operation.</summary>
        public TimeSpan ValidatedRealElapsed { get; }

        /// <summary>Gets the elapsed time explicitly authorised by the domain rule.</summary>
        public TimeSpan EligibleElapsed { get; }
    }

    /// <summary>
    /// Validates a saved server UTC instant against a current trusted observation without mutating gameplay state.
    /// </summary>
    public static class ServerTimedStateReconciliation
    {
        /// <summary>
        /// Evaluates the eligible elapsed time for one domain-owned offline policy.
        /// Returning <see cref="TimeSpan.Zero"/> from a valid rule deliberately excludes offline time.
        /// </summary>
        /// <param name="savedServerUtc">The UTC-offset-zero instant read from server-owned persistence.</param>
        /// <param name="currentTrustedObservation">A current observation from the live server time authority.</param>
        /// <param name="savedSource">The trusted origin of <paramref name="savedServerUtc"/>.</param>
        /// <param name="domainRule">A required rule that consumes only validated real elapsed time.</param>
        /// <returns>The validated real elapsed time and the rule-authorised eligible time.</returns>
        /// <exception cref="ArgumentException">Thrown for an untrusted source, invalid UTC or observation.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="domainRule"/> is missing.</exception>
        /// <exception cref="InvalidOperationException">Thrown for a future instant, overflow or invalid rule result.</exception>
        public static ServerTimedStateReconciliationResult EvaluateEligibleElapsed(
            DateTimeOffset savedServerUtc,
            ServerUtcObservation currentTrustedObservation,
            ServerUtcSource savedSource,
            Func<TimeSpan, TimeSpan> domainRule)
        {
            if (savedSource != ServerUtcSource.ServerPersistence)
                throw new ArgumentException("The saved UTC instant must come from server-owned persistence.", nameof(savedSource));
            if (savedServerUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException("The saved UTC instant must use offset zero.", nameof(savedServerUtc));
            if (domainRule == null)
                throw new ArgumentNullException(nameof(domainRule));

            ServerTimeAuthority.ValidateObservationPair(
                currentTrustedObservation,
                currentTrustedObservation,
                TimeSpan.Zero);

            if (savedServerUtc > currentTrustedObservation.UtcInstant)
                throw new InvalidOperationException("The saved server UTC instant is in the future.");

            long elapsedTicks;
            try
            {
                elapsedTicks = checked(
                    currentTrustedObservation.UtcInstant.UtcDateTime.Ticks
                    - savedServerUtc.UtcDateTime.Ticks);
            }
            catch (OverflowException exception)
            {
                throw new InvalidOperationException("Saved UTC elapsed-time arithmetic overflowed.", exception);
            }

            TimeSpan validatedRealElapsed = new(elapsedTicks);
            TimeSpan eligibleElapsed = domainRule(validatedRealElapsed);
            if (eligibleElapsed < TimeSpan.Zero)
                throw new InvalidOperationException("The domain rule returned negative eligible elapsed time.");
            if (eligibleElapsed > validatedRealElapsed)
                throw new InvalidOperationException("The domain rule returned more elapsed time than was validated.");

            return new ServerTimedStateReconciliationResult(validatedRealElapsed, eligibleElapsed);
        }
    }
}
