using Game.Server.Core;
using NUnit.Framework;
using System;

namespace Game.Shared.Tests.Server
{
    /// <summary>Verifies server-UTC validation and domain-owned timed-state reconciliation.</summary>
    public sealed class ServerTimeAuthorityTests
    {
        [Test]
        public void ReconciliationValidatesElapsedTimeAndRejectsInvalidCredit()
        {
            DateTimeOffset currentUtc = new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
            long monotonicTimestamp = 10_000L;
            var authority = new ServerTimeAuthority(
                () => currentUtc,
                () => monotonicTimestamp,
                1_000L);
            ServerUtcObservation currentObservation = authority.ReadObservation();
            DateTimeOffset savedUtc = currentUtc.AddMinutes(-15);
            TimeSpan expectedElapsed = TimeSpan.FromMinutes(15);

            bool ruleCalled = false;
            TimeSpan ruleInput = default;
            ServerTimedStateReconciliationResult fullCredit =
                ServerTimedStateReconciliation.EvaluateEligibleElapsed(
                    savedUtc,
                    currentObservation,
                    ServerUtcSource.ServerPersistence,
                    elapsed =>
                    {
                        ruleCalled = true;
                        ruleInput = elapsed;
                        return elapsed;
                    });

            Assert.That(ruleCalled, Is.True);
            Assert.That(ruleInput, Is.EqualTo(expectedElapsed));
            Assert.That(fullCredit.ValidatedRealElapsed, Is.EqualTo(expectedElapsed));
            Assert.That(fullCredit.EligibleElapsed, Is.EqualTo(expectedElapsed));

            ServerTimedStateReconciliationResult frozenCredit =
                ServerTimedStateReconciliation.EvaluateEligibleElapsed(
                    savedUtc,
                    currentObservation,
                    ServerUtcSource.ServerPersistence,
                    _ => TimeSpan.Zero);

            Assert.That(frozenCredit.ValidatedRealElapsed, Is.EqualTo(expectedElapsed));
            Assert.That(frozenCredit.EligibleElapsed, Is.EqualTo(TimeSpan.Zero));

            bool futureRuleCalled = false;
            var futureException = Assert.Throws<InvalidOperationException>(
                () => ServerTimedStateReconciliation.EvaluateEligibleElapsed(
                    currentUtc.AddMinutes(1),
                    currentObservation,
                    ServerUtcSource.ServerPersistence,
                    _ =>
                    {
                        futureRuleCalled = true;
                        return TimeSpan.Zero;
                    }));
            StringAssert.Contains("future", futureException.Message);
            Assert.That(futureRuleCalled, Is.False);

            var sourceException = Assert.Throws<ArgumentException>(
                () => ServerTimedStateReconciliation.EvaluateEligibleElapsed(
                    savedUtc,
                    currentObservation,
                    ServerUtcSource.Unspecified,
                    _ => expectedElapsed));
            StringAssert.Contains("server-owned persistence", sourceException.Message);

            var overCreditException = Assert.Throws<InvalidOperationException>(
                () => ServerTimedStateReconciliation.EvaluateEligibleElapsed(
                    savedUtc,
                    currentObservation,
                    ServerUtcSource.ServerPersistence,
                    elapsed => elapsed + TimeSpan.FromSeconds(1)));
            StringAssert.Contains("more elapsed time", overCreditException.Message);
        }
    }
}
