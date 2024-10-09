using Dissonance;
using Dissonance.Integrations.SteamworksP2P;
using Dissonance.Networking;
using Player;
using ProximityChat.TalkState;
using SNetwork;
using Steamworks;

namespace ProximityChat.SteamComms
{
    public class SteamLink
    {
        // Setup instance linking.
        private MainPlugin rootInstance;
        private Dissonance.DissonanceUtils dissonanceInstance;
        private PlayerHandler.SlotManager slotManager;
        private PlayerHandler.PlayerManager playerManager;
        private SleeperWake sleeperWake;

        // Giver Instance Loader
        private static SteamLink _instance;

        public static SteamLink Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SteamLink();
                return _instance;
            }
        }

        // Proceed.
        public void Init()
        {
            // Grab instances.
            rootInstance = MainPlugin.Instance;
            dissonanceInstance = Dissonance.DissonanceUtils.Instance;
            slotManager = PlayerHandler.SlotManager.Instance;
            playerManager = PlayerHandler.PlayerManager.Instance;
            sleeperWake = SleeperWake.Instance;
        }

        public void HostSteamP2P()
        {
            var DSObj = rootInstance.globalDS;
            var p2pServer = DSObj.GetComponent<global::Dissonance.Integrations.SteamworksP2P.SteamworksP2PCommsNetwork>().Server;

            var serverClients = p2pServer._clients;
            var clientsByName = serverClients._clientsByName;
            var clientsByID = serverClients._clientsByPlayerId;
            // May need to check if these are null with an if statement.

            MainPlugin.SendLog.LogInfo($"[SteamP2PHost] Found {clientsByName.Count} clients by name and {clientsByID.Count} by player ID.");

            foreach (var clientEntry in clientsByName)
            {
                try
                {
                    string DUID = clientEntry.Key;
                    var clientInfo = clientEntry.Value;
                    CSteamID clientSteamID = clientInfo.Connection;

                    foreach (var slotEntry in clientsByID)
                    {
                        ushort playerSlot = slotEntry.Key;
                        var playerInfo = slotEntry.Value;
                        CSteamID slotSteamID = playerInfo.Connection;

                        if (clientSteamID == slotSteamID) // If both SteamIDs match,
                        { // Could cause an error if someone leaves and a new person joins during the elevator sequence.
                            MainPlugin.SendLog.LogInfo($"[SteamP2PHost] DUID {DUID} is in player slot #{playerSlot}.");
                            dissonanceInstance.SetupIncomingAudio(DUID, playerSlot);
                        }
                    }
                } catch (Exception ex)
                {
                    MainPlugin.SendLog.LogError($"[SteamP2PClient] Inaccessible Object! Skipping... (Error: {ex.Message})");
                    continue;
                }
                
            }
            MainPlugin.SendLog.LogInfo("[SteamP2PHost] Player is Host! Using different linking method..");

            PlayerAgent localAgent = slotManager.GetPlayerAgentBySlot(SNet.LocalPlayer.CharacterIndex);
            dissonanceInstance.LinkPositionUpdater(localAgent, DSObj.gameObject);

            if (sleeperWake.sleepWakeEnabled)
                sleeperWake.enableSleeperWake(localAgent, null, DSObj.gameObject.GetComponent<DissonanceComms>().Players[0]);
        }

        public void ClientSteamP2P() // this is the new one that needs different values to work properly.
        {
            var DSObj = rootInstance.globalDS;
            var p2pClient = DSObj.GetComponent<global::Dissonance.Integrations.SteamworksP2P.SteamworksP2PCommsNetwork>().Client;

            var baseClient = p2pClient as BaseClient<SteamworksP2PServer, SteamworksP2PClient, CSteamID>;
            var clientPeers = baseClient._peers;  // Attempt direct access without reflection

            var peersByName = clientPeers._clientsByName;
            var peersByID = clientPeers._clientsByPlayerId;

            MainPlugin.SendLog.LogInfo($"[SteamP2PClient] Found {peersByName.Count} clients by name and {peersByID.Count} by player ID.");

            foreach (var clientEntry in peersByName)
            {
                try
                {
                    MainPlugin.SendLog.LogInfo("[SteamP2PClient] Queued client.");
                    string DUID = clientEntry.Key;

                    CSteamID clientSteamID;

                    MainPlugin.SendLog.LogInfo($"[SteamP2PClient] Processing {DUID}. Local DUID is '{clientPeers._playerName}'"); // Probably exclude this from release.

                    if (DUID == clientPeers._playerName) // If this loop is the local player
                    {
                        MainPlugin.SendLog.LogInfo("[SteamP2PClient] Looped DUID matches local DUID! Redirecting...");
                        dissonanceInstance.LinkPositionUpdater(slotManager.GetPlayerAgentBySlot(SNet.LocalPlayer.CharacterIndex), DSObj.gameObject);
                        continue;

                    }
                    else // Inspecting your own Value entry crashes the game for some reason. Thanks 10C.
                    {
                        var clientInfo = clientEntry.Value;
                        clientSteamID = new CSteamID(clientInfo.Connection.Value.m_SteamID);
                    }

                    foreach (var slotEntry in peersByID)
                    {
                        ushort playerSlot = slotEntry.Key;
                        var playerInfo = slotEntry.Value; // i think its this and this below vvv
                        CSteamID slotSteamID = new CSteamID(playerInfo.Connection.Value.m_SteamID);

                        if (clientSteamID == slotSteamID) // If both SteamIDs match,
                        { // Could cause an error if someone leaves and a new person joins during the elevator sequence.
                            MainPlugin.SendLog.LogInfo($"[SteamP2PClient] DUID {DUID} is in player slot #{playerSlot}.");
                            dissonanceInstance.SetupIncomingAudio(DUID, playerSlot);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MainPlugin.SendLog.LogError($"[SteamP2PClient] Inaccessible Object! Skipping... (Error: {ex.Message})");
                    continue;
                }
            }
        }
    }
}
