using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx;
using GTFO.API;
using SNetwork;
using Player;
using ProximityChat.PlayerHandler;
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
            dissonanceUtils = new DissonanceUtils.DissonanceUtils();

            // dissonanceUtils.ReportGSM();

            LevelAPI.OnEnterLevel += EnterLevel;
            SendLog.LogInfo("Loaded plugin and utilities!");
        }

        public async void EnterLevel()
        {
            await Task.Delay(500);
            foreach (var player in SNet.LobbyPlayers)
            {
                int pSlot = player.CharacterIndex;
                PlayerAgent agent = null;
                bool tryGetPlayerAgent = Player.PlayerManager.TryGetPlayerAgent(ref pSlot, out agent);

                if (agent == null || !tryGetPlayerAgent)
                {
                    SendLog.LogError($"Player '{player.NickName}' doesn't have a PlayerAgent Object!");
                    continue;
                }

                slotManager.UpdatePlayerSlot(pSlot, agent);
                SendLog.LogInfo($"Added {agent.PlayerName} in slot #{pSlot} to Dictionary!");
            }

            dissonanceUtils.Init();
        }
    }
}