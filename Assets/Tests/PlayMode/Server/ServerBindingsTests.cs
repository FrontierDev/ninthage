using Game.Server.Core;
using Game.Shared.Networking;
using Game.Shared.Persistence;
using Game.Shared.Runtime.Actors;
using NUnit.Framework;
using PurrNet;
using System;
using System.Reflection;

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
}
