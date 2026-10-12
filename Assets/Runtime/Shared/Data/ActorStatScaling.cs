namespace Game.Shared.Data
{
    /// <summary>Defines a stat contribution used to scale an effect.</summary>
    [System.Serializable]
    public sealed class StatScaling
    {
        /// <summary>Gets or sets the source stat.</summary>
        public ActorStatDefinition Stat;
        /// <summary>Gets or sets the coefficient applied to the source stat.</summary>
        public float Coefficient;
    }
}
