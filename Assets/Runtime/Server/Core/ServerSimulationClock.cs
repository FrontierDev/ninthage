using Game.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using UnityEngine;

namespace Game.Server.Core
{
    /// <summary>Identifies one authoritative server simulation tick.</summary>
    public readonly struct ServerSimulationTick
    {
        internal ServerSimulationTick(long tickID, double elapsedTime, float deltaTime)
        {
            TickID = tickID;
            ElapsedTime = elapsedTime;
            DeltaTime = deltaTime;
        }

        /// <summary>Gets the monotonically increasing server tick identifier.</summary>
        public long TickID { get; }

        /// <summary>Gets the authoritative simulation time in seconds at the end of this tick.</summary>
        public double ElapsedTime { get; }

        /// <summary>Gets the configured fixed simulation delta in seconds.</summary>
        public float DeltaTime { get; }
    }

    /// <summary>Owns cancellation for one server simulation schedule registration.</summary>
    public readonly struct ServerSimulationRegistrationHandle : IDisposable
    {
        private readonly ServerSimulationClock _clock;
        private readonly long _registrationID;

        internal ServerSimulationRegistrationHandle(ServerSimulationClock clock, long registrationID)
        {
            _clock = clock;
            _registrationID = registrationID;
        }

        /// <summary>Gets whether this handle identifies a registration.</summary>
        public bool IsValid => _clock != null && _registrationID != 0;

        /// <summary>Cancels the registration. Cancellation is idempotent.</summary>
        public void Cancel()
        {
            _clock?.CancelRegistration(_registrationID);
        }

        /// <summary>Cancels the registration.</summary>
        public void Dispose()
        {
            Cancel();
        }
    }

    /// <summary>
    /// Drives the server ECS simulation from one monotonic, bounded fixed-step clock.
    /// The component is created only by the server initialization path.
    /// </summary>
    public sealed class ServerSimulationClock : MonoBehaviour
    {
        private const float MIN_SAFE_TICK_DURATION_SECONDS = 0.001f;
        private const float MAX_SAFE_TICK_DURATION_SECONDS = 1f;
        private const int MAX_SAFE_CATCH_UP_TICKS = 1000;

        private static ServerSimulationClock _active;

        private readonly List<ScheduledWork> _scheduledWork = new();
        private readonly List<ScheduledWork> _pendingScheduledWork = new();

        private float _tickDurationSeconds;
        private int _maxCatchUpTicks;
        private long _lastTimestamp;
        private double _accumulator;
        private long _pendingTickCount;
        private long _nextTickID;
        private long _registrationSequence;
        private long _currentTickID;
        private long _scheduledTickCount;
        private long _executedTickCount;
        private long _deadlineMissCount;
        private long _catchUpCapHitCount;
        private long _deadlineMissesThisFrame;
        private bool _executingTick;
        private bool _started;

        private enum ScheduleKind
        {
            EachTick,
            LowerRate,
            Due
        }

        private sealed class ScheduledWork
        {
            internal ScheduledWork(
                long registrationID,
                ScheduleKind kind,
                Action<ServerSimulationTick> callback,
                long nextTickID,
                long intervalTicks,
                long dueTickID)
            {
                RegistrationID = registrationID;
                Kind = kind;
                Callback = callback;
                NextTickID = nextTickID;
                IntervalTicks = intervalTicks;
                DueTickID = dueTickID;
            }

            internal readonly long RegistrationID;
            internal readonly ScheduleKind Kind;
            internal readonly Action<ServerSimulationTick> Callback;
            internal readonly long IntervalTicks;
            internal readonly long DueTickID;
            internal long NextTickID;
            internal bool Cancelled;
        }

        /// <summary>Gets the active server simulation clock, if one is running.</summary>
        public static ServerSimulationClock Active => _active;

        /// <summary>Gets whether the authoritative server clock is running.</summary>
        public static bool IsRunning => _active != null && _active._started;

        /// <summary>Gets the tick currently being committed, or the last completed tick outside a tick callback.</summary>
        public static long CurrentTickID => _active == null ? 0 : _active._currentTickID;

        /// <summary>Gets the latest emitted tick identifier.</summary>
        public long LastExecutedTickID => _currentTickID;

        /// <summary>Gets the number of ticks still owed to the simulation.</summary>
        public long BacklogTicks => _pendingTickCount;

        /// <summary>Gets the total number of ticks scheduled by monotonic elapsed time.</summary>
        public long ScheduledTickCount => _scheduledTickCount;

        /// <summary>Gets the total number of ticks completed successfully.</summary>
        public long ExecutedTickCount => _executedTickCount;

        /// <summary>Gets the total number of scheduled callbacks that ran after their due tick.</summary>
        public long DeadlineMissCount => _deadlineMissCount;

        /// <summary>Gets the total number of Unity frames that hit the catch-up cap.</summary>
        public long CatchUpCapHitCount => _catchUpCapHitCount;

        /// <summary>
        /// Starts the one server simulation clock on the supplied server host.
        /// </summary>
        /// <param name="host">The server-only host that owns the clock component.</param>
        /// <returns>The started clock.</returns>
        public static ServerSimulationClock Start(MonoBehaviour host)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            if (!ServerECSManager.Initialized)
                throw new InvalidOperationException("The server ECS world must be initialized before starting the simulation clock.");
            if (_active != null)
                throw new InvalidOperationException("A server simulation clock is already running.");
            if (!GameConfigurationManager.IsInitialized || GameConfigurationManager.Config == null)
                throw new InvalidOperationException("The game configuration must be initialized before starting the simulation clock.");

            ValidateConfiguration(GameConfigurationManager.Config);

            var existingClock = host.GetComponent<ServerSimulationClock>();
            if (existingClock != null)
                throw new InvalidOperationException("The server host already owns a simulation clock.");

            var clock = host.gameObject.AddComponent<ServerSimulationClock>();
            try
            {
                clock.StartClock(
                    GameConfigurationManager.Config.ServerSimulationTickDuration,
                    GameConfigurationManager.Config.ServerSimulationMaxCatchUpTicks);
                _active = clock;
                return clock;
            }
            catch
            {
                Destroy(clock);
                throw;
            }
        }

        /// <summary>Stops the active clock and clears all schedule registrations.</summary>
        public static void Stop()
        {
            var clock = _active;
            if (clock == null)
                return;

            clock.StopClock();
            Destroy(clock);
        }

        /// <summary>Registers work that runs once after every complete ECS tick.</summary>
        /// <param name="callback">The callback to invoke in registration order.</param>
        /// <returns>A cancellation handle.</returns>
        public static ServerSimulationRegistrationHandle RegisterEachTick(Action<ServerSimulationTick> callback)
        {
            return RequireActive().Register(
                ScheduleKind.EachTick,
                callback,
                1,
                0);
        }

        /// <summary>Registers work that runs every configured number of ticks.</summary>
        /// <param name="intervalTicks">The positive number of ticks between callback invocations.</param>
        /// <param name="callback">The callback to invoke in registration order.</param>
        /// <returns>A cancellation handle.</returns>
        public static ServerSimulationRegistrationHandle RegisterLowerRate(
            int intervalTicks,
            Action<ServerSimulationTick> callback)
        {
            if (intervalTicks <= 0)
                throw new ArgumentOutOfRangeException(nameof(intervalTicks), "A lower-rate schedule interval must be positive.");

            return RequireActive().Register(
                ScheduleKind.LowerRate,
                callback,
                intervalTicks,
                0);
        }

        /// <summary>Registers one callback for a simulation tick deadline.</summary>
        /// <param name="dueTickID">The tick on which the callback becomes due.</param>
        /// <param name="callback">The callback to invoke when the deadline is due or overdue.</param>
        /// <returns>A cancellation handle.</returns>
        public static ServerSimulationRegistrationHandle RegisterDue(
            long dueTickID,
            Action<ServerSimulationTick> callback)
        {
            if (dueTickID <= 0)
                throw new ArgumentOutOfRangeException(nameof(dueTickID), "A due tick ID must be positive.");

            return RequireActive().Register(
                ScheduleKind.Due,
                callback,
                0,
                dueTickID);
        }

        /// <summary>Validates serialized server simulation settings before a world is created.</summary>
        internal static void ValidateConfiguration(GameConfiguration configuration)
        {
            if (configuration == null)
                throw new InvalidOperationException("The game configuration is required for server simulation startup.");

            float tickDuration = configuration.ServerSimulationTickDuration;
            if (float.IsNaN(tickDuration) || float.IsInfinity(tickDuration)
                || tickDuration < MIN_SAFE_TICK_DURATION_SECONDS
                || tickDuration > MAX_SAFE_TICK_DURATION_SECONDS)
            {
                throw new InvalidOperationException(
                    $"ServerSimulationTickDuration must be finite and between {MIN_SAFE_TICK_DURATION_SECONDS} and {MAX_SAFE_TICK_DURATION_SECONDS} seconds.");
            }

            int maxCatchUpTicks = configuration.ServerSimulationMaxCatchUpTicks;
            if (maxCatchUpTicks <= 0 || maxCatchUpTicks > MAX_SAFE_CATCH_UP_TICKS)
            {
                throw new InvalidOperationException(
                    $"ServerSimulationMaxCatchUpTicks must be between 1 and {MAX_SAFE_CATCH_UP_TICKS}.");
            }
        }

        /// <summary>
        /// Converts an already validated authored seconds value to a non-shortening fixed-step tick count.
        /// Callers retain ownership of subsystem-specific duration and tick-duration validation.
        /// </summary>
        /// <param name="seconds">The authored duration or interval in seconds.</param>
        /// <param name="fixedDeltaTime">The configured fixed simulation tick duration.</param>
        /// <returns>The minimum positive number of authoritative ticks covering the seconds value.</returns>
        internal static long GetNonShorteningTickCount(float seconds, float fixedDeltaTime)
        {
            // Round-trip float values through decimal before ceiling so a nominal multiple is not
            // pushed to a later tick by binary noise, while a just-above-boundary duration is not shortened.
            decimal secondsDecimal = decimal.Parse(
                seconds.ToString("R", CultureInfo.InvariantCulture),
                NumberStyles.Float,
                CultureInfo.InvariantCulture);
            decimal tickDurationDecimal = decimal.Parse(
                fixedDeltaTime.ToString("R", CultureInfo.InvariantCulture),
                NumberStyles.Float,
                CultureInfo.InvariantCulture);
            long tickCount = checked((long)Math.Ceiling(secondsDecimal / tickDurationDecimal));
            return Math.Max(1L, tickCount);
        }

        private static ServerSimulationClock RequireActive()
        {
            if (_active == null || !_active._started)
                throw new InvalidOperationException("The server simulation clock is not running.");

            return _active;
        }

        private void StartClock(float tickDurationSeconds, int maxCatchUpTicks)
        {
            _tickDurationSeconds = tickDurationSeconds;
            _maxCatchUpTicks = maxCatchUpTicks;
            _lastTimestamp = Stopwatch.GetTimestamp();
            _started = true;
            enabled = true;
        }

        private void StopClock()
        {
            _started = false;
            _executingTick = false;
            _scheduledWork.Clear();
            _pendingScheduledWork.Clear();
            _pendingTickCount = 0;
            if (_active == this)
                _active = null;
            enabled = false;
        }

        private ServerSimulationRegistrationHandle Register(
            ScheduleKind kind,
            Action<ServerSimulationTick> callback,
            long intervalTicks,
            long dueTickID)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            long registrationID = checked(++_registrationSequence);
            long firstTickID = checked(_currentTickID + 1);
            var work = new ScheduledWork(
                registrationID,
                kind,
                callback,
                firstTickID,
                intervalTicks,
                dueTickID);

            if (_executingTick)
                _pendingScheduledWork.Add(work);
            else
                _scheduledWork.Add(work);

            return new ServerSimulationRegistrationHandle(this, registrationID);
        }

        internal void CancelRegistration(long registrationID)
        {
            if (registrationID == 0)
                return;

            foreach (var work in _scheduledWork)
            {
                if (work.RegistrationID == registrationID)
                {
                    work.Cancelled = true;
                    if (!_executingTick)
                        CommitScheduledWork();
                    return;
                }
            }

            foreach (var work in _pendingScheduledWork)
            {
                if (work.RegistrationID == registrationID)
                {
                    work.Cancelled = true;
                    return;
                }
            }
        }

        private void Update()
        {
            if (!_started)
                return;

            long scheduledThisFrame = 0;
            int executedThisFrame = 0;
            Exception failure = null;
            _deadlineMissesThisFrame = 0;

            try
            {
                long now = Stopwatch.GetTimestamp();
                long elapsedTimestampTicks = now - _lastTimestamp;
                if (elapsedTimestampTicks < 0)
                    throw new InvalidOperationException("The server simulation monotonic clock moved backwards.");

                _lastTimestamp = now;
                _accumulator += elapsedTimestampTicks / (double)Stopwatch.Frequency;

                scheduledThisFrame = TakeWholeTicksFromAccumulator();
                if (scheduledThisFrame > 0)
                {
                    _pendingTickCount = checked(_pendingTickCount + scheduledThisFrame);
                    _scheduledTickCount = checked(_scheduledTickCount + scheduledThisFrame);
                }

                while (_pendingTickCount > 0 && executedThisFrame < _maxCatchUpTicks)
                {
                    long tickID = checked(++_nextTickID);
                    var tick = new ServerSimulationTick(
                        tickID,
                        tickID * (double)_tickDurationSeconds,
                        _tickDurationSeconds);

                    ExecuteTick(tick);
                    _pendingTickCount--;
                    _executedTickCount = checked(_executedTickCount + 1);
                    executedThisFrame++;
                }

                if (_pendingTickCount > 0)
                    _catchUpCapHitCount = checked(_catchUpCapHitCount + 1);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                try
                {
                    ServerECSManager.RecordSimulationFrameMetrics(
                        scheduledThisFrame,
                        executedThisFrame,
                        _pendingTickCount,
                        _deadlineMissesThisFrame,
                        _pendingTickCount > 0);
                }
                catch (Exception telemetryException)
                {
                    if (failure == null)
                        failure = telemetryException;
                    else
                        failure.Data["SimulationFrameMetricsException"] = telemetryException;
                }
                finally
                {
                    if (failure != null)
                        ServerInitialization.BeginFatalShutdown(failure);
                }
            }
        }

        private long TakeWholeTicksFromAccumulator()
        {
            if (_accumulator < _tickDurationSeconds)
                return 0;

            double wholeTickCount = Math.Floor(_accumulator / _tickDurationSeconds);
            if (wholeTickCount > long.MaxValue)
                throw new InvalidOperationException("The server simulation tick backlog exceeded the supported range.");

            long scheduledTickCount = (long)wholeTickCount;
            _accumulator -= scheduledTickCount * (double)_tickDurationSeconds;
            return scheduledTickCount;
        }

        private void ExecuteTick(ServerSimulationTick tick)
        {
            long startedTimestamp = Stopwatch.GetTimestamp();
            _currentTickID = tick.TickID;
            _executingTick = true;

            try
            {
                ServerECSManager.UpdateSimulation(tick);
                ExecuteScheduledWork(tick);
            }
            finally
            {
                _executingTick = false;
                CommitScheduledWork();

                ServerECSManager.RecordSimulationTickDuration(
                    tick.TickID,
                    (Stopwatch.GetTimestamp() - startedTimestamp) * 1000d / Stopwatch.Frequency,
                    tick.DeltaTime);
            }
        }

        private void ExecuteScheduledWork(ServerSimulationTick tick)
        {
            foreach (var work in _scheduledWork)
            {
                if (work.Cancelled)
                    continue;

                bool due = work.Kind == ScheduleKind.EachTick
                    || work.Kind == ScheduleKind.LowerRate && tick.TickID >= work.NextTickID
                    || work.Kind == ScheduleKind.Due && tick.TickID >= work.DueTickID;
                if (!due)
                    continue;

                if (work.Kind == ScheduleKind.Due && tick.TickID > work.DueTickID)
                {
                    _deadlineMissCount = checked(_deadlineMissCount + 1);
                    _deadlineMissesThisFrame = checked(_deadlineMissesThisFrame + 1);
                }

                work.Callback(tick);

                if (work.Kind == ScheduleKind.Due)
                {
                    work.Cancelled = true;
                }
                else if (work.Kind == ScheduleKind.LowerRate)
                {
                    work.NextTickID = checked(tick.TickID + work.IntervalTicks);
                }
            }
        }

        private void CommitScheduledWork()
        {
            for (int i = _scheduledWork.Count - 1; i >= 0; i--)
            {
                if (_scheduledWork[i].Cancelled)
                    _scheduledWork.RemoveAt(i);
            }

            if (_pendingScheduledWork.Count == 0)
                return;

            foreach (var work in _pendingScheduledWork)
            {
                if (!work.Cancelled)
                    _scheduledWork.Add(work);
            }

            _pendingScheduledWork.Clear();
        }

        private void OnApplicationQuit()
        {
            if (_active != this)
                return;

            StopClock();
            ServerInitialization.AbortInitialization();
            ServerECSManager.Shutdown();
        }

        private void OnDestroy()
        {
            if (_active != this)
                return;

            StopClock();
            ServerInitialization.AbortInitialization();
            ServerECSManager.Shutdown();
        }
    }
}
