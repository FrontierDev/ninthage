namespace Game.Shared.Data
{
    [System.Serializable]
    public abstract class ConditionDefinition
    {
        public abstract bool Evaluate(object context);
        public abstract string GetTooltipLine();
    }
}