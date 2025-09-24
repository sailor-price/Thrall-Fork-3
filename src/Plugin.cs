using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace ThrallForked
{
    [BepInPlugin(GUID, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "com.yourname.thrallforked";
        public const string Name = "ThrallForked";
        public const string Version = "0.1.0";

        internal static ManualLogSource Log;
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"{Name} {Version} loaded");
            _harmony = new Harmony(GUID);
            // _harmony.PatchAll(); // enable when patches are added
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
