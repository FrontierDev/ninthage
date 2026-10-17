using System;
using System.Diagnostics;

namespace Game.Server.Core
{
    /// <summary>Captures one server UTC observation and its paired monotonic source reading.</summary>
    public readonly struct ServerUtcObservation
    {
        internal ServerUtcObservation(DateTimeOffset utcInstant, long monotonicTimestamp, long monotonicFrequency, Guid sourceID)
        {
            UtcInstant = utcInstant;
            MonotonicTimestamp = monotonicTimestamp;
            MonotonicFrequency = monotonicFrequency;
            SourceID = sourceID;
        }

        /// <summary>Gets the canonical UTC instant captured by the server.</summary>
        public DateTimeOffset UtcInstant { get; }

        /// <summary>Gets the monotonic process timestamp paired with <see cref="UtcInstant"/>.</summary>
        public long MonotonicTimestamp { get; }

        /// <summary>Gets the frequency of the paired monotonic process timestamp.</summary>
        public long MonotonicFrequency { get; }

        /// <summary>Gets the live in-process source identity; it is not comparable across restarts.</summary>
        public Guid SourceID { get; }
    }

    /// <summary>Provides server-authoritative UTC observations independently of the active simulation clock.</summary>
    public sealed class ServerTimeAuthority
    {
        private readonly Func<DateTimeOffset> _utcNow;
        private readonly Func<long> _monotonicTimestamp;
        private readonly long _monotonicFrequency;
        private readonly Guid _sourceID;

        /// <summary>Creates an authority using the real server UTC and monotonic system sources.</summary>
        public ServerTimeAuthority()
            : this(() => DateTimeOffset.UtcNow, Stopwatch.GetTimestamp, Stopwatch.Frequency)
        {
        }

        /// <summary>Creates an authority with explicitly supplied sources for deterministic in-process use.</summary>
        /// <param name="utcNow">The server UTC observation source.</param>
        /// <param name="monotonicTimestamp">The monotonic timestamp source.</param>
        /// <param name="monotonicFrequency">The positive frequency for the timestamp source.</param>
        public ServerTimeAuthority(Func<DateTimeOffset> utcNow, Func<long> monotonicTimestamp, long monotonicFrequency)
        {
            _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
            _monotonicTimestamp = monotonicTimestamp ?? throw new ArgumentNullException(nameof(monotonicTimestamp));
            if (monotonicFrequency <= 0)
                throw new ArgumentOutOfRangeException(nameof(monotonicFrequency), "The monotonic frequency must be positive.");

            _monotonicFrequency = monotonicFrequency;
            _sourceID = Guid.NewGuid();
            if (_sourceID == Guid.Empty)
                throw new InvalidOperationException("The monotonic source identity could not be created.");
        }

        /// <summary>Reads a canonical UTC instant and its paired monotonic process timestamp.</summary>
        public ServerUtcObservation ReadObservation()
        {
            DateTimeOffset utcInstant = _utcNow();
            if (utcInstant.Offset != TimeSpan.Zero)
                throw new InvalidOperationException("The server UTC source returned a non-UTC instant.");

            return new ServerUtcObservation(utcInstant, _monotonicTimestamp(), _monotonicFrequency, _sourceID);
        }

        /// <summary>Validates two observations from the same live source against an explicit drift tolerance.</summary>
        /// <param name="earlier">The earlier observation.</param>
        /// <param name="later">The later observation.</param>
        /// <param name="maximumClockDrift">The non-negative caller-approved drift tolerance.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown for a negative tolerance.</exception>
        /// <exception cref="ArgumentException">Thrown when observations are not from the same source.</exception>
        /// <exception cref="InvalidOperationException">Thrown for reversed observations, overflow or discontinuity.</exception>
        public static void ValidateObservationPair(ServerUtcObservation earlier, ServerUtcObservation later, TimeSpan maximumClockDrift)
        {
            ValidateObservation(earlier, nameof(earlier));
            ValidateObservation(later, nameof(later));
            if (maximumClockDrift < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(maximumClockDrift), "Clock-drift tolerance cannot be negative.");
            if (earlier.SourceID != later.SourceID)
                throw new ArgumentException("Observations must come from the same live monotonic source.");
            if (earlier.MonotonicFrequency != later.MonotonicFrequency)
                throw new ArgumentException("Observations must use the same monotonic frequency.");
            if (later.MonotonicTimestamp < earlier.MonotonicTimestamp)
                throw new InvalidOperationException("The monotonic source moved backwards between observations.");
            if (later.UtcInstant < earlier.UtcInstant)
                throw new InvalidOperationException("The server UTC source moved backwards between observations.");

            long monotonicElapsed;
            long utcElapsed;
            try
            {
                monotonicElapsed = checked(later.MonotonicTimestamp - earlier.MonotonicTimestamp);
                utcElapsed = checked(later.UtcInstant.UtcDateTime.Ticks - earlier.UtcInstant.UtcDateTime.Ticks);
            }
            catch (OverflowException exception)
            {
                throw new InvalidOperationException("Observation duration arithmetic overflowed.", exception);
            }

            try
            {
                decimal monotonicElapsedTicks = (decimal)monotonicElapsed * TimeSpan.TicksPerSecond / earlier.MonotonicFrequency;
                decimal driftTicks = utcElapsed - monotonicElapsedTicks;
                decimal toleranceTicks = maximumClockDrift.Ticks;
                if (driftTicks > toleranceTicks)
                    throw new InvalidOperationException("The server UTC clock moved forward beyond the approved drift tolerance.");
                if (driftTicks < -toleranceTicks)
                    throw new InvalidOperationException("The server UTC clock moved backward beyond the approved drift tolerance.");
            }
            catch (OverflowException exception)
            {
                throw new InvalidOperationException("Observation duration conversion overflowed.", exception);
            }
        }

        private static void ValidateObservation(ServerUtcObservation observation, string parameterName)
        {
            if (observation.UtcInstant.Offset != TimeSpan.Zero)
                throw new ArgumentException("The observation must contain a canonical UTC instant.", parameterName);
            if (observation.MonotonicFrequency <= 0)
                throw new ArgumentException("The observation must contain a positive monotonic frequency.", parameterName);
            if (observation.SourceID == Guid.Empty)
                throw new ArgumentException("The observation must contain a live source identity.", parameterName);
        }
    }
}
