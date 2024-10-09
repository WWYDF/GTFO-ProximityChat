using BepInEx.Logging;
using Dissonance;
using HarmonyLib;
using Player;
using ProximityChat;
using ProximityChat.Dissonance;
using ProximityChat.SteamComms;
using SNetwork;

namespace ProximityChat.PlayerHandler
{
    public class SlotManager
    {
        // Giver Instance Loader.
        private static SlotManager _instance;
        public static SlotManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SlotManager();
                return _instance;
            }
        }

        public Dictionary<int, PlayerAgent> playerAgentsBySlot = new Dictionary<int, PlayerAgent>();

        public void UpdatePlayerSlot(int slotNumber, PlayerAgent playerAgent)
        {
            // Add or update the PlayerAgent in the corresponding slot
            if (playerAgentsBySlot.ContainsKey(slotNumber))
            {
                playerAgentsBySlot[slotNumber] = playerAgent;  // Update existing slot
            }
            else
            {
                playerAgentsBySlot.Add(slotNumber, playerAgent);  // Add new slot
            }
        }


        public void RemovePlayerFromSlot(int slotNumber)
        {
            if (playerAgentsBySlot.ContainsKey(slotNumber))
            {
                playerAgentsBySlot[slotNumber] = null;  // Set the slot to null (or remove if preferred)
            }
        }

        public void ClearAllSlots()
        {
            playerAgentsBySlot.Clear();
            MainPlugin.SendLog.LogWarning("[PlayerHandler] PlayerSlots Dictionary has been cleared.");
        }


        public PlayerAgent GetPlayerAgentBySlot(int slotNumber)
        {
            if (playerAgentsBySlot.ContainsKey(slotNumber))
            {
                return playerAgentsBySlot[slotNumber];  // Returns the PlayerAgent in the specified slot
            }
            else
            {
                MainPlugin.SendLog.LogWarning($"[PlayerHandler] Slot {slotNumber} does not exist or is empty.");
                return null;  // Return null if the slot is empty or doesn't exist
            }
        }


        public int? GetSlotByPlayerAgent(PlayerAgent playerAgent)
        {
            foreach (var entry in playerAgentsBySlot)
            {
                if (entry.Value == playerAgent)
                {
                    return entry.Key;  // Return the slot number when the PlayerAgent is found
                }
            }

            MainPlugin.SendLog.LogWarning($"[PlayerHandler] PlayerAgent {playerAgent.PlayerName} not found in any slot.");
            return null;  // Return null if the PlayerAgent is not found in any slot
        }
    }

    public class PlayerManager
    {
        // Setup instance linking.
        private MainPlugin rootInstance;
        private DissonanceUtils dissonanceInstance;
        private SteamLink steamLink;
        private SlotManager slotManager;

        // Giver Instance Loader.
        private static PlayerManager _instance;
        public static PlayerManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new PlayerManager();
                return _instance;
            }
        }

        public void Init()
        {
            // Grab instances.
            rootInstance = MainPlugin.Instance;
            dissonanceInstance = DissonanceUtils.Instance;
            slotManager = SlotManager.Instance;
            steamLink = SteamLink.Instance;
        }

        public void RefreshDictionary()
        {
            if (slotManager.playerAgentsBySlot != null)
                slotManager.ClearAllSlots();

            foreach (var player in SNet.LobbyPlayers)
            {
                int pSlot = player.CharacterIndex;
                PlayerAgent agent = null;
                bool tryGetPlayerAgent = Player.PlayerManager.TryGetPlayerAgent(ref pSlot, out agent);

                if (agent == null || !tryGetPlayerAgent)
                {
                    MainPlugin.SendLog.LogError($"Player '{player.NickName}' doesn't have a PlayerAgent Object!");
                    continue;
                }

                slotManager.UpdatePlayerSlot(pSlot, agent);
                MainPlugin.SendLog.LogInfo($"Added {agent.PlayerName} in slot #{pSlot} to Dictionary!");
            }
        }

        public void RefreshPlayers() // Runs when a new player joins, or just needs to refresh logic.
        {
            if (!dissonanceInstance.isInLevel)
                return;

            MainPlugin.SendLog.LogInfo("[PlayerManager] Refreshing...");
            RefreshDictionary(); // wipe/load slot dictionary

            rootInstance.UpdateGlobalDS();

            if (SNet.LocalPlayer.IsMaster)
                steamLink.HostSteamP2P();
            else
                steamLink.ClientSteamP2P();
        }
    }
}
