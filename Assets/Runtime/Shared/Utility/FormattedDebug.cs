namespace Game.Shared.Utility
{
    /// <summary>Writes consistently formatted diagnostic messages.</summary>
    public static class FormattedDebug
    {
        /// <summary>Writes an informational message.</summary>
        public static void Log(string message)
        {
            string caller = GetCallerName();
            UnityEngine.Debug.Log(string.Format("INFO | {0} | [{1}] {2}", System.DateTime.Now.ToString("HH:mm:ss"), caller, message));
        }

        /// <summary>Writes a warning message.</summary>
        public static void Warning(string message)
        {
            string caller = GetCallerName();
            UnityEngine.Debug.LogWarning(string.Format("WARN | {0} | [{1}] {2}", System.DateTime.Now.ToString("HH:mm:ss"), caller, message));
        }

        /// <summary>Writes an error message.</summary>
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
