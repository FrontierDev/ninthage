using Game.Core;
using Game.Server.Core;
using Game.Shared.Networking;
using Game.Shared.Persistence;
using Game.Shared.Runtime;
using Game.Shared.Runtime.Actors;
using RuntimeAuthority = Game.Shared.Runtime.Runtime;
using NUnit.Framework;
using PurrNet;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Runtime.Tests.Server
{
    public sealed class ServerBindingsTests
    {
        private static readonly FieldInfo INITIALIZED_FIELD =
            typeof(ServerBindings).GetField("_initialized", BindingFlags.Static | BindingFlags.NonPublic);

        private bool _originalInitialized;
        private Action<PlayerID, NewCharacterData> _originalCharacterCreationRequest;
        private Action<PlayerID, string> _originalEnterWorldRequest;
        private Action<Actor> _originalActorDeath;

        [SetUp]
        public void SetUp()
        {
            Assert.That(INITIALIZED_FIELD, Is.Not.Null);

            _originalInitialized = ServerBindings.Initialized;
            _originalCharacterCreationRequest = AccountEndpoint.onCharacterCreationRequest;
            _originalEnterWorldRequest = AccountEndpoint.onEnterWorldRequest;
            _originalActorDeath = ActorEndpoint.onActorDeath;

            AccountEndpoint.onCharacterCreationRequest = null;
            AccountEndpoint.onEnterWorldRequest = null;
            ActorEndpoint.onActorDeath = null;
            SetInitialized(false);
        }

        [TearDown]
        public void TearDown()
        {
            AccountEndpoint.onCharacterCreationRequest = _originalCharacterCreationRequest;
            AccountEndpoint.onEnterWorldRequest = _originalEnterWorldRequest;
            ActorEndpoint.onActorDeath = _originalActorDeath;
            SetInitialized(_originalInitialized);
        }

        [Test]
        public void InitializeIsIdempotentForServerBindingCallbacks()
        {
            ServerBindings.Initialize();
            ServerBindings.Initialize();

            Assert.That(AccountEndpoint.onCharacterCreationRequest, Is.Not.Null);
            Assert.That(AccountEndpoint.onCharacterCreationRequest.GetInvocationList(), Has.Length.EqualTo(1));
            Assert.That(AccountEndpoint.onEnterWorldRequest, Is.Not.Null);
            Assert.That(AccountEndpoint.onEnterWorldRequest.GetInvocationList(), Has.Length.EqualTo(1));
            Assert.That(ActorEndpoint.onActorDeath, Is.Not.Null);
            Assert.That(ActorEndpoint.onActorDeath.GetInvocationList(), Has.Length.EqualTo(1));
        }

        private static void SetInitialized(bool value)
        {
            INITIALIZED_FIELD.SetValue(null, value);
        }
    }

    public sealed class RuntimeRoleSelectionTests
    {
        private static readonly MethodInfo RESET_METHOD =
            typeof(RuntimeAuthority).GetMethod("Reset", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo RESOLVE_RUNTIME_ROLE_METHOD =
            typeof(GameBootstrapper).GetMethod("ResolveRuntimeRole", BindingFlags.Static | BindingFlags.NonPublic);

        [SetUp]
        public void SetUp()
        {
            ResetRuntimeRole();
        }

        [TearDown]
        public void TearDown()
        {
            ResetRuntimeRole();
        }

        [TestCase(false, false, RuntimeRole.Client, false)]
        [TestCase(false, true, RuntimeRole.Server, true)]
        [TestCase(true, false, RuntimeRole.Server, true)]
        public void BootstrapSelectsTheExpectedRuntimeRole(
            bool headlessServer,
            bool forceServerMode,
            RuntimeRole expectedRole,
            bool expectedIsServer)
        {
            var resolvedRole = ResolveRuntimeRole(headlessServer, forceServerMode);
            RuntimeAuthority.Initialize(resolvedRole);

            Assert.That(RuntimeAuthority.Role, Is.EqualTo(expectedRole));
            Assert.That(RuntimeAuthority.IsServer(), Is.EqualTo(expectedIsServer));
            Assert.That(RuntimeAuthority.IsClient(), Is.EqualTo(!expectedIsServer));
        }

        [Test]
        public void RuntimeRoleRejectsUninitializedAndConflictingInitialization()
        {
            Assert.Throws<ArgumentException>(() => RuntimeAuthority.Initialize(RuntimeRole.Uninitialized));

            RuntimeAuthority.Initialize(RuntimeRole.Server);
            Assert.DoesNotThrow(() => RuntimeAuthority.Initialize(RuntimeRole.Server));
            Assert.Throws<InvalidOperationException>(() => RuntimeAuthority.Initialize(RuntimeRole.Client));
            Assert.That(RuntimeAuthority.Role, Is.EqualTo(RuntimeRole.Server));
        }

        [Test]
        public void SubsystemResetReturnsRuntimeRoleToUninitialized()
        {
            RuntimeAuthority.Initialize(RuntimeRole.Client);
            ResetRuntimeRole();

            Assert.That(RuntimeAuthority.Role, Is.EqualTo(RuntimeRole.Uninitialized));
            Assert.That(RuntimeAuthority.IsServer(), Is.False);
            Assert.That(RuntimeAuthority.IsClient(), Is.False);
        }

        [Test]
        public void NPCCreatedAfterForcedServerRoleSelectionKeepsServerMovementSettings()
        {
            RuntimeAuthority.Initialize(ResolveRuntimeRole(false, true));
            var actorObject = new GameObject("RuntimeRoleSelection NPC");
            try
            {
                var agent = actorObject.AddComponent<NavMeshAgent>();
                actorObject.AddComponent<NPCActor>();

                Assert.That(RuntimeAuthority.IsServer(), Is.True);
                Assert.That(agent.enabled, Is.True);
                Assert.That(agent.updatePosition, Is.True);
                Assert.That(agent.updateRotation, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(actorObject);
            }
        }

        private static RuntimeRole ResolveRuntimeRole(bool headlessServer, bool forceServerMode)
        {
            Assert.That(RESOLVE_RUNTIME_ROLE_METHOD, Is.Not.Null);
            return (RuntimeRole)RESOLVE_RUNTIME_ROLE_METHOD.Invoke(null, new object[] { headlessServer, forceServerMode });
        }

        private static void ResetRuntimeRole()
        {
            Assert.That(RESET_METHOD, Is.Not.Null);
            RESET_METHOD.Invoke(null, null);
        }
    }
}
