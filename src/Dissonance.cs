using Dissonance.Audio.Playback;
using Dissonance.Networking;
using GTFO.API;
using Player;
using ProximityChat.PlayerHandler;
using SNetwork;
using Steamworks;
using UnityEngine;
using Dissonance.Integrations.SteamworksP2P;

namespace ProximityChat.DissonanceUtils
{
    public class DissonanceUtils
    {
        private PlayerHandler.SlotManager? slotManager;
        public GameObject? globalDS;
        public bool isInLevel = false;

        public void Init() // Calls each time a player enters a level.
        {
            isInLevel = true;
            MainPlugin.SendLog.LogInfo("[DissonanceUtils] Initialized!");
            UpdateGlobalDS();
            slotManager = SlotManager.Instance;

            LevelAPI.OnLevelCleanup += OnExitLevel;

            if (SNet.LocalPlayer.IsMaster)
                HostSteamP2P();
            else
                ClientSteamP2P();
        }

        public void UpdateGlobalDS() // Grabs Main Dissonance GameObject.
        {
            GameObject globalObj = GameObject.Find("GLOBAL");
            MainPlugin.SendLog.LogInfo($"[UpdateDS] Found GLOBAL GameObject: {globalObj.name}");

            GameObject DSObj = globalObj.transform.Find("Managers/Chat/DissonanceSetup").gameObject;
            MainPlugin.SendLog.LogInfo($"[UpdateDS] Found DissonanceSetup GameObject: {DSObj.name}");

            globalDS = DSObj; // Updates globalDS.
        }

        public async void HostSteamP2P()
        {
            await Task.Delay(2000); // Delay moved here so this function is async and doesn't cause lag to base game.

            var DSObj = globalDS;
            var p2pServer = DSObj.GetComponent<Dissonance.Integrations.SteamworksP2P.SteamworksP2PCommsNetwork>().Server;

            var serverClients = p2pServer._clients;
            var clientsByName = serverClients._clientsByName;
            var clientsByID = serverClients._clientsByPlayerId;
            // May need to check if these are null with an if statement.

            MainPlugin.SendLog.LogInfo($"[SteamP2PHost] Found {clientsByName.Count} clients by name and {clientsByID.Count} by player ID.");

            foreach (var clientEntry in clientsByName)
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
                        SetupIncomingAudio(DUID, playerSlot);
                    }
                }
            }
        }

        public void ClientSteamP2P() // this is the new one that needs different values to work properly.
        {
            try
            {
                var DSObj = globalDS;
                var p2pClient = DSObj.GetComponent<Dissonance.Integrations.SteamworksP2P.SteamworksP2PCommsNetwork>().Client;

                var baseClient = p2pClient as BaseClient<SteamworksP2PServer, SteamworksP2PClient, CSteamID>;
                var clientPeers = baseClient._peers;  // Attempt direct access without reflection

                if (clientPeers == null || baseClient == null)
                {
                    MainPlugin.SendLog.LogError("[SteamP2PClient] Failed to load p2p client! Aborting...");
                    return;
                }

                MainPlugin.SendLog.LogInfo($"Successfully accessed clientPeers via direct access.");
                var peersByName = clientPeers._clientsByName;
                var peersByID = clientPeers._clientsByPlayerId;


                if (peersByID == null || peersByName == null) // Prevent Game Crash
                {
                    MainPlugin.SendLog.LogError($"[SteamP2PClient] Peer Data is unset or null! Disabling Plugin...");
                    return;
                }
                // May need to check if these are null with an if statement.

                MainPlugin.SendLog.LogInfo($"[SteamP2PClient] Found {peersByName.Count} clients by name and {peersByID.Count} by player ID.");

                foreach (var clientEntry in peersByName)
                {
                    try
                    {
                        MainPlugin.SendLog.LogInfo("[SP2PC] Queued client.");
                        string DUID = clientEntry.Key;

                        CSteamID clientSteamID;

                        MainPlugin.SendLog.LogInfo($"Processing {DUID}. Local DUID is '{clientPeers._playerName}'");

                        if (DUID == clientPeers._playerName) // If this loop is the local player
                        {
                            MainPlugin.SendLog.LogInfo("[SteamP2PClient] Looped DUID matches local DUID! Redirecting...");
                            LinkPositionUpdater(slotManager.GetPlayerAgentBySlot(SNet.LocalPlayer.CharacterIndex), DSObj.gameObject);
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
                                SetupIncomingAudio(DUID, playerSlot);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MainPlugin.SendLog.LogError($"[SteamP2PClient] fuckin loop died again. Error: {ex.Message}");
                        continue;
                    }
                    
                }

            }
            catch (Exception ex)
            {
                MainPlugin.SendLog.LogError($"[SteamP2PClient] CRASH PREVENTED. Error: {ex.Message}");
                return;
            }

        }

        public void SetupIncomingAudio(string targetDUID, int pSlot)
        {
            var DSObj = globalDS;
            var selfDUID = DSObj.GetComponent<Dissonance.Integrations.SteamworksP2P.SteamworksP2PCommsNetwork>().PlayerName;

            

            foreach (var child in DSObj.GetComponentsInChildren<Transform>())
            {
                if (child.name.StartsWith("Player ") && child.name.Contains(" voice comms"))
                {
                    string childDUID = child.name.Substring(7, child.name.IndexOf(" voice comms") - 7);
                    
                    if (childDUID == targetDUID) // Compare child in list to GUID passed to function.
                    {
                        GameObject childObject = child.gameObject;
                        MainPlugin.SendLog.LogInfo($"[SetupAudio] Successfully extracted GameObject for DUID '{targetDUID}'.");

                        var cAgent = slotManager.GetPlayerAgentBySlot(pSlot);
                        LinkPositionUpdater(cAgent, childObject);


                        AudioSource audioSourceComponent = childObject.GetComponent<AudioSource>();
                        VoicePlayback voicePlaybackComponent = childObject.GetComponent<VoicePlayback>();

                        if (audioSourceComponent != null )
                        {   // Apply necessary values to enable positional audio.
                            OverrideBlend(audioSourceComponent);
                            audioSourceComponent.spatialize = true;
                            audioSourceComponent.spatializePostEffects = true;
                            audioSourceComponent.maxDistance = 50.0f;
                            audioSourceComponent.minDistance = 1.0f;
                            voicePlaybackComponent._IsApplyingAudioSpatialization_k__BackingField = true;
                            MainPlugin.SendLog.LogInfo("[SetupAudio] Successfully enabled audio spatialization.");

                        }
                    }
                }
            }
        }

        public async void LinkPositionUpdater(PlayerAgent player, GameObject userObject)
        {
            var playerName = player.PlayerName;

            MainPlugin.SendLog.LogInfo($"Linked {playerName}'s position!");
            while (isInLevel && GameStateManager.CurrentStateName.ToString() == "InLevel") // Basically while true when in level.
            {
                try
                {
                    userObject.transform.position = player.Position;
                    userObject.transform.rotation = player.Rotation;
                    await Task.Delay(50); // tune this
                } catch
                {
                    MainPlugin.SendLog.LogError($"Connection to game severed! Unlinked all players!");
                    break;
                }
            }
            MainPlugin.SendLog.LogInfo($"Unlinked {playerName}! ({isInLevel}, {GameStateManager.CurrentStateName.ToString()})");
        }

        public void OnExitLevel()
        {
            MainPlugin.SendLog.LogInfo("Exiting Level!");
            isInLevel = false;
        }

        public async void OverrideBlend(AudioSource audioSourceComponent)
        {   // Not the best solution currently, but it works. Might show noticable lag if scaling past 4 players.
            while (isInLevel)
            {
                audioSourceComponent.spatialBlend = 0.8f;
                await Task.Delay(1000); // tune this
            }
        }



        // ******* DEBUG, REMOVE IN FINAL RELEASE ******* //

        private string lastState = null;
        private DateTime lastLogTime = DateTime.MinValue;

        public async void ReportGSM()
        {
            int logInterval = 10000; // X seconds interval

            while (true)
            {
                var cState = GameStateManager.CurrentStateName.ToString();

                if (cState != lastState)
                {
                    // If cState has changed, log immediately and reset timer
                    MainPlugin.SendLog.LogInfo($"Current Gamestate: {cState}");
                    lastState = cState;
                    lastLogTime = DateTime.Now;
                }
                else if ((DateTime.Now - lastLogTime).TotalMilliseconds >= logInterval)
                {
                    // If 5 seconds have passed since the last log, send the current state
                    MainPlugin.SendLog.LogInfo($"Current Gamestate: {cState}");
                    lastLogTime = DateTime.Now;
                }

                // Delay the next iteration of the loop (this keeps the loop running frequently)
                await Task.Delay(50);
            }
        }
    }
}
