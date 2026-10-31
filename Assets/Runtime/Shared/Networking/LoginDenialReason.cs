namespace Game.Shared.Networking
{
    /// <summary>Identifies the actionable reason a World Server denied client authentication.</summary>
    public enum LoginDenialReason : byte
    {
        /// <summary>Compatibility passed, but normal authentication or session admission failed.</summary>
        AuthenticationRejected = 0,

        /// <summary>The client build version or immutable client build identity requires an update.</summary>
        ClientUpdateRequired = 1,

        /// <summary>The client and World require different network protocol versions.</summary>
        ProtocolMismatch = 2,

        /// <summary>The client requires the pinned gameplay content revision.</summary>
        GameplayContentUpdateRequired = 3,

        /// <summary>The pinned gameplay content revision has a different manifest hash.</summary>
        GameplayContentVerificationFailed = 4,

        /// <summary>The client uses a build target different from the pinned target.</summary>
        UnsupportedBuildTarget = 5,

        /// <summary>The client requires the pinned Addressables catalog revision.</summary>
        AddressablesCatalogUpdateRequired = 6,

        /// <summary>The pinned Addressables catalog revision has a different catalog hash.</summary>
        AddressablesCatalogVerificationFailed = 7
    }
}
