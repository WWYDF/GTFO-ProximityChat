using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx;
using GTFO.API;
using SNetwork;
using Player;
using ProximityChat.PlayerHandler;
using ProximityChat.DissonanceUtils;
using TenChambers.EventHandlers;
using HarmonyLib;

namespace ProximityChat
{
    [BepInPlugin("net.devante.gtfo.proximitychat", "ProximityChat", "1.0.0")]
    public class MainPlugin : BasePlugin
    {
        public static ManualLogSource SendLog;
        public bool isPlayerInLevel = false;
        private PlayerHandler.SlotManager? slotManager;
        private DissonanceUtils.DissonanceUtils dissonanceUtils;

        public override void Load() // Runs once when plugin is loaded.
        {
            // Assign BepInEx logger to static field
            SendLog = Log;
            slotManager = SlotManager.Instance;
            dissonanceUtils = DissonanceUtils.DissonanceUtils.Dissinstance;

            // dissonanceUtils.ReportGSM();

            LevelAPI.OnEnterLevel += EnterLevel;
            SendLog.LogInfo("Loaded plugin and utilities!");

            var harmony = new Harmony("net.devante.gtfo.proximitychat");
            harmony.PatchAll();
        }

        public void EnterLevel()
        {
            dissonanceUtils.Init();
        }
    }
}