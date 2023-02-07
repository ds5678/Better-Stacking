using MelonLoader;

namespace BetterStacking
{
    internal class Implementation : MelonMod
    {

        public static Implementation? Instance { get; private set; }

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg($"[{Info.Name}] Version {Info.Version} loaded!");
        }

        public Implementation()
        {
            Instance = this;
        }

        internal static void Log(string message)
        {
            if (Instance is not null)
            {
                Instance.LoggerInstance.Msg(message);
            }
            else
            {
                MelonLogger.Msg(message);
            }
        }

        internal static void LogWarning(string message)
        {
            if (Instance is not null)
            {
                Instance.LoggerInstance.Warning(message);
            }
            else
            {
                MelonLogger.Warning(message);
            }
        }
    }
}