using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx;
using HarmonyLib;
using ProximityChat.PlayerHandler;
using ProximityChat.SteamComms;
using ProximityChat.Dissonance;
using UnityEngine;

namespace ProximityChat
{
    [BepInPlugin("net.devante.gtfo.proximitychat", "ProximityChat", "1.0.0")]
    public class MainPlugin : BasePlugin
    {
        // Setup instance linking.
        private Dissonance.DissonanceUtils? dissonanceInstance;
        private PlayerHandler.SlotManager? slotManagerInstance;
        private SteamComms.SteamLink? steamLink;
        private PlayerHandler.PlayerManager? playerManager;

        // Giver Instance Loader
        private static MainPlugin _instance;

        public static MainPlugin Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MainPlugin ();
                return _instance;
            }
        }

        // Proceed.

        public static ManualLogSource SendLog;
        public GameObject? globalDS;

        public override void Load() // Runs once when plugin is loaded.
        {
        // Grab instances.
            dissonanceInstance = DissonanceUtils.Instance;
            slotManagerInstance = SlotManager.Instance;
            steamLink = SteamLink.Instance;
            playerManager = PlayerHandler.PlayerManager.Instance;

        // Assign BepInEx logger to static field
            SendLog = Log;

        // Initialize other classes.
            dissonanceInstance.Init();
            steamLink.Init();
            playerManager.Init();

        // Patch Harmony.
            var harmony = new Harmony("net.devante.gtfo.proximitychat");
            harmony.PatchAll();

        // Report Finished.
            SendLog.LogInfo("Loaded plugin and utilities!");
        }

        public void UpdateGlobalDS() // Grabs Main Dissonance GameObject.
        {
            GameObject globalObj = GameObject.Find("GLOBAL");
            SendLog.LogInfo($"[UpdateDS] Found GLOBAL GameObject: {globalObj.name}");

            GameObject DSObj = globalObj.transform.Find("Managers/Chat/DissonanceSetup").gameObject;
            SendLog.LogInfo($"[UpdateDS] Found DissonanceSetup GameObject: {DSObj.name}");

            globalDS = DSObj; // Updates globalDS.
        }
    }
}