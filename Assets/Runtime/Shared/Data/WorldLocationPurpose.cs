using System;

namespace Game.Shared.Data
{
    /// <summary>Describes the authored purposes supported by a world location.</summary>
    [Flags]
    public enum WorldLocationPurpose
    {
        None = 0,
        Starting = 1,
        SafeReturn = 2,
        RegionalFallback = 4
    }
}
