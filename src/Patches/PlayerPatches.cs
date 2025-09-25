using HarmonyLib;

namespace ThrallForked.Patches
{
    [HarmonyPatch(typeof(Player))]
    internal static class PlayerPatches
    {
        [HarmonyPatch(nameof(Player.OnSpawned))]
        [HarmonyPostfix]
        private static void OnSpawnedPostfix(Player __instance)
        {
            CompanionManager.TrySpawn(__instance);
        }

        [HarmonyPatch(nameof(Player.OnDestroy))]
        [HarmonyPrefix]
        private static void OnDestroyPrefix(Player __instance)
        {
            if (__instance != Player.m_localPlayer)
            {
                return;
            }

            CompanionManager.TryDespawn();
        }

        [HarmonyPatch(nameof(Player.Update))]
        [HarmonyPostfix]
        private static void UpdatePostfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer)
            {
                return;
            }

            CompanionManager.Update();
        }
    }
}