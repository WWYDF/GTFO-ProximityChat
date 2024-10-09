using BepInEx.Logging;
using Dissonance;
using HarmonyLib;
using Player;
using ProximityChat;
using ProximityChat.DissonanceUtils;
using SNetwork;

namespace ProximityChat.PlayerHandler
{
    public class SlotManager
    {
        private static SlotManager _instance;

        // Public property to access the singleton instance
        public static SlotManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SlotManager();
                }
                return _instance;
            }
        }

        Dictionary<int, PlayerAgent> playerAgentsBySlot = new Dictionary<int, PlayerAgent>();

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
}
