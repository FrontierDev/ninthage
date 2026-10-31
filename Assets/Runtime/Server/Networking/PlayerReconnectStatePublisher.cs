using Game.Server.Core;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using PurrNet;
using System;

namespace Game.Server.Networking
{
    /// <summary>Publishes owner-private state from the live actor during reconnect.</summary>
    internal static class PlayerReconnectStatePublisher
    {
        /// <summary>Publishes all current owner-private state for an attached live actor.</summary>
        internal static void Publish(
            PlayerSessionData session,
            PlayerID playerID)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            if (session.PlayerActor == null)
                throw new InvalidOperationException("Reconnect publication requires a live actor.");
            if (!session.HasPlayerAttachment || session.PlayerID != playerID)
                throw new InvalidOperationException("Reconnect publication requires the current Player ID attachment.");

            Actor actor = session.PlayerActor;
            if (!actor.Owner.HasValue || actor.Owner.Value != playerID)
                throw new InvalidOperationException("Reconnect publication requires actor ownership by the Player ID.");

            if (!ServerStatManager.IsInitialized)
                throw new InvalidOperationException("The server stat manager is unavailable for reconnect publication.");
            ServerStatManager.Instance.SendSnapshot(actor);

            PlayerActor playerActor = actor as PlayerActor;
            if (playerActor == null)
                throw new InvalidOperationException("Reconnect publication requires a PlayerActor.");
            if (!ServerCooldownManager.IsInitialized)
                throw new InvalidOperationException("The server cooldown manager is unavailable for reconnect publication.");
            ServerCooldownManager.Instance.PublishCooldownBootstrap(playerActor);

            if (!ServerAuraManager.IsInitialized)
                throw new InvalidOperationException("The server aura manager is unavailable for reconnect publication.");
            ServerAuraManager.Instance.PublishAuraBootstrap(actor, playerID);

            actor.GetComponent<PlayerInventory>().PublishOwnerBootstrap(playerID);
            actor.GetComponent<PlayerEquipment>().PublishOwnerBootstrap(playerID);
            actor.GetComponent<ActorSpellcaster>().PublishOwnerBootstrap(playerID);
            actor.GetComponent<PlayerExperience>().PublishOwnerBootstrap(playerID);
            actor.GetComponent<PlayerTalents>().PublishOwnerBootstrap(playerID);
            actor.GetComponent<PlayerReputation>().PublishOwnerBootstrap(playerID);
            actor.GetComponent<PlayerQuests>().PublishOwnerBootstrap(playerID);
        }
    }
}
