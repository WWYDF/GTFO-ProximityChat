using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx;
using HarmonyLib;
using ProximityChat.PlayerHandler;
using ProximityChat.SteamComms;
using ProximityChat.Dissonance;
using UnityEngine;
using BepInEx.Configuration;
using ProximityChat.TalkState;
using GTFO.API;
using GameData;

namespace ProximityChat
{
    public class ProximityConfig
    {
        // Proximity Settings
        public static ConfigEntry<float> changeMaxDistance;
        public static ConfigEntry<float> changeMinDistance;
        public static ConfigEntry<int> changePollingFrequency;

        // AudioEffects Settings
        public static ConfigEntry<bool> enableLowPass;

        // TalkState Settings
        public static ConfigEntry<bool> enableSleeperWake;
        public static ConfigEntry<bool> enableHardMode;
    }

    [BepInPlugin("net.devante.gtfo.proximitychat", "ProximityChat", "0.4")]
    public class MainPlugin : BasePlugin
    {
        // Setup instance linking.
        private Dissonance.DissonanceUtils? dissonanceInstance;
        private PlayerHandler.SlotManager? slotManagerInstance;
        private SteamComms.SteamLink? steamLink;
        private PlayerHandler.PlayerManager? playerManager;
        private TalkState.SleeperWake? sleeperWake;
        private AudioEffects.AudioEffects? audioEffects;

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
        // Enable Config
            ProximityConfig.changeMaxDistance = Config.Bind("Proximity Settings", "Maximum Distance", 52f, "How far should you be able to hear other players?");
            ProximityConfig.changeMinDistance = Config.Bind("Proximity Settings", "Minimum Distance", 2f, "How close should other players have to be in order to be at full volume?");
            ProximityConfig.changePollingFrequency = Config.Bind("Proximity Settings", "Polling Rate", 50, "How fast Player Positions should be updated in milliseconds.");

            ProximityConfig.enableLowPass = Config.Bind("Audio Effects", "Enable Low Pass", true, "When a player is behind a wall, should the audio be muffled?");

            ProximityConfig.enableSleeperWake = Config.Bind("SleeperWake", "Enabled", false, "Allows sleepers to wake up from talking near them. [HOST ONLY]");
            ProximityConfig.enableHardMode = Config.Bind("SleeperWake", "Use Hard Mode?", false, "Makes sleepers instantly wake up from talking near them. (Requires base to be enabled)");

        // Grab instances.
            dissonanceInstance = DissonanceUtils.Instance;
            slotManagerInstance = SlotManager.Instance;
            steamLink = SteamLink.Instance;
            playerManager = PlayerHandler.PlayerManager.Instance;
            sleeperWake = SleeperWake.Instance;
            audioEffects = AudioEffects.AudioEffects.Instance;

        // Assign BepInEx logger to static field
            SendLog = Log;

        // Subscribe to events
            LevelAPI.OnBuildStart += sleeperWake.Init;

        // Initialize other classes.
            dissonanceInstance.Init();
            steamLink.Init();
            playerManager.Init();
            audioEffects.Init();

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