using Game.Shared.Runtime.Actors;
using PurrNet;
using System;
using System.Collections.Generic;

namespace Game.Shared.Networking
{
    /// <summary>Tracks actor and identity subscriptions for streamed scene chunks.</summary>
    public static class ChunkSubscriptions
    {
        private readonly struct ChunkSubscriptionKey : IEquatable<ChunkSubscriptionKey>
        {
            public ActorSpatialContext Context { get; }
            public string SceneName { get; }

            public ChunkSubscriptionKey(ActorSpatialContext context, string sceneName)
            {
                if (!context.IsValid)
                    throw new ArgumentException("A valid actor spatial context is required.", nameof(context));
                ValidateSceneName(sceneName);

                Context = context;
                SceneName = sceneName;
            }

            public bool Equals(ChunkSubscriptionKey other)
            {
                return Context.Equals(other.Context)
                    && StringComparer.Ordinal.Equals(SceneName, other.SceneName);
            }

            public override bool Equals(object obj)
            {
                return obj is ChunkSubscriptionKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = Context.GetHashCode();
                    hashCode = (hashCode * 397)
                        ^ (SceneName == null ? 0 : StringComparer.Ordinal.GetHashCode(SceneName));
                    return hashCode;
                }
            }
        }

        private static readonly Dictionary<ChunkSubscriptionKey, HashSet<Actor>> _actorsByChunk = new();
        /// <summary>Gets the actor associated with each player.</summary>
        public static readonly Dictionary<PlayerID, Actor> PLAYER_ACTORS = new();
        private static readonly Dictionary<ChunkSubscriptionKey, HashSet<NetworkIdentity>> _identitiesByChunk = new();

        /// <summary>Registers an actor in a scene chunk.</summary>
        public static void AddActorToScene(
            ActorSpatialContext context,
            string sceneName,
            Actor actor)
        {
            ValidateActorSubscription(context, sceneName, actor);
            var key = new ChunkSubscriptionKey(context, sceneName);
            if (!_actorsByChunk.TryGetValue(key, out var set))
            {
                set = new();
                _actorsByChunk[key] = set;
            }
            if (set.Add(actor))
                NetworkingPerformanceMetrics.RecordChunkSubscriptionChange();
        }

        /// <summary>Removes an actor from a scene chunk.</summary>
        public static void RemoveActorFromScene(
            ActorSpatialContext context,
            string sceneName,
            Actor actor)
        {
            ValidateActorSubscription(context, sceneName, actor);
            var key = new ChunkSubscriptionKey(context, sceneName);
            if (_actorsByChunk.TryGetValue(key, out var set))
            {
                if (set.Remove(actor))
                    NetworkingPerformanceMetrics.RecordChunkSubscriptionChange();
                if (set.Count == 0)
                    _actorsByChunk.Remove(key);
            }
        }

        /// <summary>Returns whether an actor is registered in the exact context and scene chunk.</summary>
        public static bool ContainsActor(
            ActorSpatialContext context,
            string sceneName,
            Actor actor)
        {
            ValidateActorSubscription(context, sceneName, actor);
            return _actorsByChunk.TryGetValue(new ChunkSubscriptionKey(context, sceneName), out var set)
                && set.Contains(actor);
        }

        /// <summary>Associates a player with its actor.</summary>
        public static void RegisterPlayerActor(PlayerID playerID, Actor actor)
        {
            PLAYER_ACTORS[playerID] = actor;
        }

        /// <summary>Removes a player's actor association.</summary>
        public static void UnregisterPlayerActor(PlayerID playerID)
        {
            PLAYER_ACTORS.Remove(playerID);
        }

        /// <summary>Registers a network identity in a scene chunk.</summary>
        public static void RegisterIdentity(
            ActorSpatialContext context,
            string sceneName,
            NetworkIdentity identity)
        {
            ValidateIdentity(context, sceneName, identity);
            var key = new ChunkSubscriptionKey(context, sceneName);
            if (!_identitiesByChunk.TryGetValue(key, out var set))
            {
                set = new();
                _identitiesByChunk[key] = set;
            }
            set.Add(identity);
        }

        /// <summary>Removes a network identity from a scene chunk.</summary>
        public static void UnregisterIdentity(
            ActorSpatialContext context,
            string sceneName,
            NetworkIdentity identity)
        {
            ValidateIdentity(context, sceneName, identity);
            var key = new ChunkSubscriptionKey(context, sceneName);
            if (_identitiesByChunk.TryGetValue(key, out var set))
            {
                set.Remove(identity);
                if (set.Count == 0)
                    _identitiesByChunk.Remove(key);
            }
        }

        /// <summary>Appends identities registered in the exact context and scene chunk.</summary>
        public static int AppendIdentities(
            ActorSpatialContext context,
            string sceneName,
            List<NetworkIdentity> resultBuffer)
        {
            ValidateContextAndScene(context, sceneName);
            if (resultBuffer == null)
                throw new ArgumentNullException(nameof(resultBuffer));

            if (!_identitiesByChunk.TryGetValue(new ChunkSubscriptionKey(context, sceneName), out var set))
                return 0;

            int appendedCount = 0;
            foreach (var identity in set)
            {
                resultBuffer.Add(identity);
                appendedCount++;
            }

            return appendedCount;
        }

        /// <summary>Moves a registered identity between scene chunks in one spatial context.</summary>
        public static void MoveIdentity(
            ActorSpatialContext context,
            string sourceSceneName,
            string destinationSceneName,
            NetworkIdentity identity)
        {
            ValidateIdentity(context, sourceSceneName, identity);
            ValidateContextAndScene(context, destinationSceneName);
            if (StringComparer.Ordinal.Equals(sourceSceneName, destinationSceneName))
                return;

            var sourceKey = new ChunkSubscriptionKey(context, sourceSceneName);
            if (!_identitiesByChunk.TryGetValue(sourceKey, out var sourceSet)
                || !sourceSet.Contains(identity))
            {
                throw new InvalidOperationException(
                    $"Identity is not registered in source scene '{sourceSceneName}'.");
            }

            var destinationKey = new ChunkSubscriptionKey(context, destinationSceneName);
            if (!_identitiesByChunk.TryGetValue(destinationKey, out var destinationSet))
            {
                destinationSet = new();
                _identitiesByChunk[destinationKey] = destinationSet;
            }

            destinationSet.Add(identity);
            sourceSet.Remove(identity);
            if (sourceSet.Count == 0)
                _identitiesByChunk.Remove(sourceKey);
        }

        private static void ValidateActorSubscription(
            ActorSpatialContext context,
            string sceneName,
            Actor actor)
        {
            ValidateContextAndScene(context, sceneName);
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));
            if (!actor.SpatialContext.IsCompatibleWith(context))
            {
                throw new InvalidOperationException(
                    $"Actor '{actor.ID}' does not belong to the supplied spatial context.");
            }
        }

        private static void ValidateIdentity(
            ActorSpatialContext context,
            string sceneName,
            NetworkIdentity identity)
        {
            ValidateContextAndScene(context, sceneName);
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));
        }

        private static void ValidateContextAndScene(
            ActorSpatialContext context,
            string sceneName)
        {
            if (!context.IsValid)
                throw new ArgumentException("A valid actor spatial context is required.", nameof(context));
            ValidateSceneName(sceneName);
        }

        private static void ValidateSceneName(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                throw new ArgumentException("A nonempty scene name is required.", nameof(sceneName));
        }
    }
}
