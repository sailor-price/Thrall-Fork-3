// CompanionManager.cs
// Compile for .NET Framework 4.7.2
using BepInEx;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Windows;
using BepInEx.Configuration;

// Optional: If you pull in Jötunn, you can swap Debug.Log with Jotunn.Logger.LogInfo, etc.
// using Jotunn.Managers;

namespace CompanionMod
{
    [BepInPlugin("com.yourname.valheim.companion", "Companion Manager", "1.0.0")]
    public class CompanionManagerPlugin : BaseUnityPlugin
    {
        private const KeyCode SpawnToggleKey = KeyCode.F6;
        private const KeyCode ReFollowKey = KeyCode.F7;

        private GameObject _companion;
        private CompanionAI _companionAI;

        private void Update()
        {
            if (Player.m_localPlayer == null || ZNetScene.instance == null) return;

            if (UnityEngine.Input.GetKeyDown(SpawnToggleKey))

            {
                if (_companion == null) SpawnCompanion();
                else DespawnCompanion();
            }

            if (Input.GetKeyDown(ReFollowKey))
            {
                if (_companionAI != null && Player.m_localPlayer != null)
                {
                    _companionAI.SetFollowTarget(Player.m_localPlayer.gameObject);
                    Debug.Log("[Companion] Re-set follow target to player.");
                }
            }
        }

        private void SpawnCompanion()
        {
            try
            {
                if (_companion != null) return;

                var player = Player.m_localPlayer;
                var prefab = ZNetScene.instance.GetPrefab("Player");
                if (prefab == null)
                {
                    Debug.LogError("[Companion] Could not find 'Player' prefab in ZNetScene.");
                    return;
                }

                // Spawn beside the player
                Vector3 pos = player.transform.position + player.transform.right * 1.5f;
                Quaternion rot = Quaternion.LookRotation(-player.transform.forward, Vector3.up);

                // Instantiate a visual clone of the player prefab
                var go = Instantiate(prefab, pos, rot);

                // Remove Player brain (we'll add Humanoid + AI)
                var playerComp = go.GetComponent<Player>();
                if (playerComp != null) DestroyImmediate(playerComp);

                // Ensure it has a Humanoid/Character we can drive via MonsterAI/BaseAI
                var humanoid = go.GetComponent<Humanoid>() ?? go.AddComponent<Humanoid>();

                // Add our very light AI (derives from MonsterAI)
                _companionAI = go.AddComponent<CompanionAI>();
                _companionAI.name = "PlayerCompanionAI";

                // Make it friendly & passive by default (also clears any previous target)
                _companionAI.MakeTame(); // sets Character.Tamed, clears targets. :contentReference[oaicite:4]{index=4}
                _companionAI.SetHuntPlayer(false);

                // Make sure it follows the local player
                _companionAI.SetFollowTarget(Player.m_localPlayer.gameObject); // calls through to BaseAI.Follow in UpdateAI. :contentReference[oaicite:5]{index=5} :contentReference[oaicite:6]{index=6}

                // Safety knobs to minimize enemy acquisition
                _companionAI.m_aggravatable = false;  // don't enter aggravated state (BaseAI flag). :contentReference[oaicite:7]{index=7}
                _companionAI.m_attackPlayerObjects = false;
                _companionAI.m_enableHuntPlayer = false;

                // Greatly reduce vision/hearing so it doesn't wander into combat
                _companionAI.m_viewRange = 0.1f;  // BaseAI public field. :contentReference[oaicite:8]{index=8}
                _companionAI.m_hearRange = 0.1f;  // BaseAI public field. :contentReference[oaicite:9]{index=9}

                // Optional: Copy a few movement stats by reflection from the real Player (keeps speeds sane)
                TryCopyMovementFromPlayer(humanoid, player);

                // Give it a friendly name
                TrySetCharacterName(humanoid, $"{player.GetPlayerName()} (Companion)");

                _companion = go;
                Debug.Log("[Companion] Spawned companion clone.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Companion] Failed to spawn companion: {e}");
                DespawnCompanion();
            }
        }

        private void DespawnCompanion()
        {
            try
            {
                if (_companion == null) return;

                // If it has a ZNetView, destroy via network so it cleans up properly
                var znv = _companion.GetComponent<ZNetView>();
                if (znv != null)
                {
                    znv.Destroy();
                }
                else
                {
                    Destroy(_companion);
                }

                _companion = null;
                _companionAI = null;
                Debug.Log("[Companion] Companion despawned.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Companion] Failed to despawn companion: {e}");
            }
        }

        // --- Helpers ---------------------------------------------------------

        private static void TryCopyMovementFromPlayer(Humanoid dst, Player src)
        {
            try
            {
                var srcHum = src as Humanoid;
                if (srcHum == null) return;

                // Copy a handful of common Character/Humanoid movement fields by reflection.
                // If any field does not exist on the current Valheim version, it's silently ignored.
                CopyFieldIfExists(srcHum, dst, "m_walkSpeed");
                CopyFieldIfExists(srcHum, dst, "m_runSpeed");
                CopyFieldIfExists(srcHum, dst, "m_turnSpeed");
                CopyFieldIfExists(srcHum, dst, "m_acceleration");
                CopyFieldIfExists(srcHum, dst, "m_jumpForce");
                CopyFieldIfExists(srcHum, dst, "m_runTurnSpeed");
            }
            catch { /* non-fatal */ }
        }

        private static void TrySetCharacterName(Humanoid h, string name)
        {
            try
            {
                var character = h as Character;
                if (character == null) return;

                // public string m_name on Character (typical in Valheim)
                var f = typeof(Character).GetField("m_name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null) f.SetValue(character, name);
            }
            catch { /* non-fatal */ }
        }

        private static void CopyFieldIfExists(object src, object dst, string field)
        {
            var st = src.GetType();
            var dt = dst.GetType();

            var sf = st.GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var df = dt.GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (sf != null && df != null)
            {
                var val = sf.GetValue(src);
                df.SetValue(dst, val);
            }
        }
    }

    /// <summary>
    /// Minimal companion AI:
    /// - Tame
    /// - Not aggravated
    /// - Narrow senses so it doesn't pick fights
    /// Follows target through MonsterAI -> BaseAI.Follow.
    /// </summary>
    public class CompanionAI : MonsterAI
    {
        protected override void Awake()
        {
            base.Awake();

            // Be nice by default
            m_aggravatable = false;        // Don't flip to aggravated state. :contentReference[oaicite:10]{index=10}
            m_attackPlayerObjects = false; // Never whack bases
            m_enableHuntPlayer = false;    // Never enter hunt mode
            m_fleeIfNotAlerted = false;

            // Keep senses tiny so enemy acquisition basically never triggers
            m_viewRange = 0.1f;  // BaseAI field used by CanSee/FindEnemy. :contentReference[oaicite:11]{index=11}
            m_hearRange = 0.1f;  // BaseAI field used by CanHear/FindEnemy. :contentReference[oaicite:12]{index=12}

            // Make tame & calm
            MakeTame(); // Sets tamed:true, clears targets, un-alerts. :contentReference[oaicite:13]{index=13}
            SetAlerted(false);
        }
    }
}
