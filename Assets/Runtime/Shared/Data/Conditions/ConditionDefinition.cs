namespace Game.Shared.Data.Conditions
{
    [System.Serializable]
    /// <summary>Defines a condition used to filter authored game content.</summary>
    public abstract class ConditionDefinition
    {
        /// <summary>Evaluates the condition for a context object.</summary>
        public abstract bool Evaluate(object context);
        /// <summary>Gets the condition tooltip text.</summary>
        public abstract string GetTooltipLine();
    }
}
