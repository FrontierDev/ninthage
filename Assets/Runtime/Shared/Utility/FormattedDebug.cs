namespace Game.Shared
{
    public static class FormattedDebug
    {
        public static void Log(string message)
        {
            string caller = GetCallerName();
            UnityEngine.Debug.Log(string.Format("INFO | {0} | [{1}] {2}", System.DateTime.Now.ToString("HH:mm:ss"), caller, message));
        }

        public static void Warning(string message)
        {
            string caller = GetCallerName();
            UnityEngine.Debug.LogWarning(string.Format("WARN | {0} | [{1}] {2}", System.DateTime.Now.ToString("HH:mm:ss"), caller, message));
        }

        public static void Error(string message)
        {
            string caller = GetCallerName();
            UnityEngine.Debug.LogError(string.Format("ERRX | {0} | [{1}] {2}", System.DateTime.Now.ToString("HH:mm:ss"), caller, message));
        }

        private static string GetCallerName()
        {
            var type = new System.Diagnostics.StackFrame(2, false).GetMethod()?.DeclaringType;
            // Unwrap compiler-generated types (coroutines, lambdas, etc.)
            while (type != null && type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false))
                type = type.DeclaringType;
            return type?.Name ?? "Unknown";
        }
    }
}