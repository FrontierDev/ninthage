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
            TransferIdentity(
                context,
                sourceSceneName,
                context,
                destinationSceneName,
                identity);
        }

        /// <summary>
        /// Transfers a registered network identity between context-qualified scene chunks.
        /// </summary>
        /// <param name="sourceContext">The context containing the registered source identity.</param>
        /// <param name="sourceSceneName">The registered source scene name.</param>
        /// <param name="destinationContext">The destination identity context.</param>
        /// <param name="destinationSceneName">The destination scene name.</param>
        /// <param name="identity">The registered network identity to transfer.</param>
        /// <exception cref="ArgumentException">Thrown when a context or scene name is invalid.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="identity"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the source registration is missing, or when source removal or rollback fails.
        /// </exception>
        public static void TransferIdentity(
            ActorSpatialContext sourceContext,
            string sourceSceneName,
            ActorSpatialContext destinationContext,
            string destinationSceneName,
            NetworkIdentity identity)
        {
            ValidateContextAndScene(sourceContext, sourceSceneName);
            ValidateContextAndScene(destinationContext, destinationSceneName);
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));

            if (sourceContext.Equals(destinationContext)
                && StringComparer.Ordinal.Equals(sourceSceneName, destinationSceneName))
            {
                return;
            }

            var sourceKey = new ChunkSubscriptionKey(sourceContext, sourceSceneName);
            if (!_identitiesByChunk.TryGetValue(sourceKey, out var sourceSet)
                || !sourceSet.Contains(identity))
            {
                throw new InvalidOperationException(
                    $"Identity is not registered in source context and scene '{sourceSceneName}'.");
            }

            var destinationKey = new ChunkSubscriptionKey(destinationContext, destinationSceneName);
            if (!_identitiesByChunk.TryGetValue(destinationKey, out var destinationSet))
            {
                destinationSet = new();
                _identitiesByChunk[destinationKey] = destinationSet;
            }

            bool destinationAdded = false;
            bool sourceRemoved = false;
            try
            {
                destinationAdded = destinationSet.Add(identity);
                if (!sourceSet.Remove(identity))
                {
                    throw new InvalidOperationException(
                        $"Identity could not be removed from source context and scene '{sourceSceneName}'.");
                }

                sourceRemoved = true;
                if (sourceSet.Count == 0)
                    _identitiesByChunk.Remove(sourceKey);
            }
            catch (Exception transferException)
            {
                try
                {
                    if (sourceRemoved)
                    {
                        if (!_identitiesByChunk.ContainsKey(sourceKey))
                            _identitiesByChunk[sourceKey] = sourceSet;
                        sourceSet.Add(identity);
                    }

                    if (destinationAdded)
                    {
                        if (!destinationSet.Remove(identity))
                        {
                            throw new InvalidOperationException(
                                "Identity could not be removed during destination rollback.");
                        }

                        if (destinationSet.Count == 0)
                            _identitiesByChunk.Remove(destinationKey);
                    }
                }
                catch (Exception rollbackException)
                {
                    throw new InvalidOperationException(
                        "Identity context transfer failed and rollback also failed.",
                        new AggregateException(transferException, rollbackException));
                }

                throw;
            }
        }

        /// <summary>
        /// Transfers an actor's derived scene subscriptions between context-qualified scene sets.
        /// </summary>
        /// <param name="sourceContext">The actor's previous authoritative context.</param>
        /// <param name="sourceSceneNames">The source scene memberships currently owned by the actor.</param>
        /// <param name="destinationContext">The actor's current authoritative destination context.</param>
        /// <param name="destinationSceneNames">The destination scene memberships to establish.</param>
        /// <param name="actor">The actor whose derived memberships are transferred.</param>
        /// <exception cref="ArgumentException">Thrown when a context or scene name is invalid.</exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a scene collection or actor is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the actor context or source memberships are invalid, or when transfer or rollback fails.
        /// </exception>
        public static void TransferActorSubscriptions(
            ActorSpatialContext sourceContext,
            IReadOnlyCollection<string> sourceSceneNames,
            ActorSpatialContext destinationContext,
            IReadOnlyCollection<string> destinationSceneNames,
            Actor actor)
        {
            if (!sourceContext.IsValid)
                throw new ArgumentException(
                    "A valid source actor spatial context is required.",
                    nameof(sourceContext));
            if (!destinationContext.IsValid)
                throw new ArgumentException(
                    "A valid destination actor spatial context is required.",
                    nameof(destinationContext));
            if (sourceSceneNames == null)
                throw new ArgumentNullException(nameof(sourceSceneNames));
            if (destinationSceneNames == null)
                throw new ArgumentNullException(nameof(destinationSceneNames));
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));
            if (!actor.SpatialContext.Equals(destinationContext))
            {
                throw new InvalidOperationException(
                    $"Actor '{actor.ID}' context does not match the destination subscription context.");
            }

            var sourceScenes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var sceneName in sourceSceneNames)
            {
                ValidateSceneName(sceneName);
                sourceScenes.Add(sceneName);
            }

            var destinationScenes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var sceneName in destinationSceneNames)
            {
                ValidateSceneName(sceneName);
                destinationScenes.Add(sceneName);
            }

            var sourceKeys = new HashSet<ChunkSubscriptionKey>();
            foreach (var sceneName in sourceScenes)
                sourceKeys.Add(new ChunkSubscriptionKey(sourceContext, sceneName));

            var destinationKeys = new HashSet<ChunkSubscriptionKey>();
            foreach (var sceneName in destinationScenes)
                destinationKeys.Add(new ChunkSubscriptionKey(destinationContext, sceneName));

            foreach (var sourceKey in sourceKeys)
            {
                if (!_actorsByChunk.TryGetValue(sourceKey, out var sourceSet)
                    || !sourceSet.Contains(actor))
                {
                    throw new InvalidOperationException(
                        $"Actor '{actor.ID}' is not registered in source scene '{sourceKey.SceneName}'.");
                }
            }

            var addedDestinationKeys = new List<ChunkSubscriptionKey>();
            var removedSourceKeys = new List<ChunkSubscriptionKey>();
            try
            {
                foreach (var destinationKey in destinationKeys)
                {
                    if (sourceKeys.Contains(destinationKey))
                        continue;

                    if (!_actorsByChunk.TryGetValue(destinationKey, out var destinationSet))
                    {
                        destinationSet = new();
                        _actorsByChunk[destinationKey] = destinationSet;
                    }

                    if (destinationSet.Add(actor))
                    {
                        addedDestinationKeys.Add(destinationKey);
                        NetworkingPerformanceMetrics.RecordChunkSubscriptionChange();
                    }
                }

                foreach (var sourceKey in sourceKeys)
                {
                    if (destinationKeys.Contains(sourceKey))
                        continue;

                    if (!_actorsByChunk.TryGetValue(sourceKey, out var sourceSet)
                        || !sourceSet.Remove(actor))
                    {
                        throw new InvalidOperationException(
                            $"Actor '{actor.ID}' could not be removed from source scene '{sourceKey.SceneName}'.");
                    }

                    removedSourceKeys.Add(sourceKey);
                    NetworkingPerformanceMetrics.RecordChunkSubscriptionChange();
                    if (sourceSet.Count == 0)
                        _actorsByChunk.Remove(sourceKey);
                }
            }
            catch (Exception transferException)
            {
                try
                {
                    for (int i = addedDestinationKeys.Count - 1; i >= 0; i--)
                    {
                        var destinationKey = addedDestinationKeys[i];
                        if (!_actorsByChunk.TryGetValue(destinationKey, out var destinationSet)
                            || !destinationSet.Remove(actor))
                        {
                            throw new InvalidOperationException(
                                $"Actor '{actor.ID}' destination subscription rollback failed.");
                        }

                        NetworkingPerformanceMetrics.RecordChunkSubscriptionChange();
                        if (destinationSet.Count == 0)
                            _actorsByChunk.Remove(destinationKey);
                    }

                    foreach (var sourceKey in removedSourceKeys)
                    {
                        if (!_actorsByChunk.TryGetValue(sourceKey, out var sourceSet))
                        {
                            sourceSet = new();
                            _actorsByChunk[sourceKey] = sourceSet;
                        }

                        if (!sourceSet.Add(actor))
                        {
                            throw new InvalidOperationException(
                                $"Actor '{actor.ID}' source subscription rollback found an existing membership.");
                        }

                        NetworkingPerformanceMetrics.RecordChunkSubscriptionChange();
                    }
                }
                catch (Exception rollbackException)
                {
                    throw new InvalidOperationException(
                        $"Actor '{actor.ID}' subscription transfer failed and rollback also failed.",
                        new AggregateException(transferException, rollbackException));
                }

                throw;
            }
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
