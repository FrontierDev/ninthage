namespace Game.Shared.Data
{
    /// <summary>Base definition for selecting spell targets.</summary>
    public abstract class SpellTargetDefinition
    {
        internal bool _requiresTarget;
        /// <summary>Gets whether this target definition requires a target.</summary>
        public bool RequiresTarget => _requiresTarget;

        /// <summary>Creates a copy of this target definition.</summary>
        public abstract SpellTargetDefinition Clone();
    }
}
