using Dissonance;
using Dissonance.Audio.Playback;
using GTFO.API;
using Player;
using ProximityChat.PlayerHandler;
using Steamworks;
using UnityEngine;

namespace ProximityChat.DissonanceUtils
{
    public class DissonanceUtils
    {
        private PlayerHandler.SlotManager? slotManager;
        public GameObject? globalDS;
        public bool isInLevel = false;

        public async void Init()
        {
            isInLevel = true;
            MainPlugin.SendLog.LogInfo("[DissonanceUtils] Initialized!");
            UpdateGlobalDS();
            slotManager = SlotManager.Instance;

            LevelAPI.OnLevelCleanup += OnExitLevel;

            ViewSteamP2P();
        }

        public void UpdateGlobalDS() // Grabs Main Dissonance GameObject.
        {
            GameObject globalObj = GameObject.Find("GLOBAL");
            MainPlugin.SendLog.LogInfo($"[UpdateDS] Found GLOBAL GameObject: {globalObj.name}");

            GameObject DSObj = globalObj.transform.Find("Managers/Chat/DissonanceSetup").gameObject;
            MainPlugin.SendLog.LogInfo($"[UpdateDS] Found DissonanceSetup GameObject: {DSObj.name}");

            globalDS = DSObj; // Updates globalDS.
        }

        public async void ViewSteamP2P()
        {
            await Task.Delay(2000); // Delay moved here so this function is async and doesn't cause lag to base game.

            var DSObj = globalDS;
            var p2pServer = DSObj.GetComponent<Dissonance.Integrations.SteamworksP2P.SteamworksP2PCommsNetwork>().Server;

            var serverClients = p2pServer._clients;
            var clientsByName = serverClients._clientsByName;
            var clientsByID = serverClients._clientsByPlayerId;
            // May need to check if these are null with an if statement.

            MainPlugin.SendLog.LogInfo($"[SteamP2P] Found {clientsByName.Count} clients by name and {clientsByID.Count} by player ID.");

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
                        MainPlugin.SendLog.LogInfo($"[SteamP2P] DUID {DUID} is in player slot #{playerSlot}.");
                        SetupIncomingAudio(DUID, playerSlot);
                    }
                }
            }
        }

        public async void SetupIncomingAudio(string targetDUID, int pSlot)
        {
            var DSObj = globalDS;

            if (pSlot == 0)
            {
                LinkPositionUpdater(slotManager.GetPlayerAgentBySlot(pSlot), DSObj.gameObject);
                return;
            }

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
                            MainPlugin.SendLog.LogInfo($"dicks before: {audioSourceComponent.spatialBlend}");
                            OverrideBlend(audioSourceComponent);
                            audioSourceComponent.spatialize = true;
                            audioSourceComponent.spatializePostEffects = true;
                            audioSourceComponent.maxDistance = 50.0f;
                            audioSourceComponent.minDistance = 1.0f;
                            voicePlaybackComponent._IsApplyingAudioSpatialization_k__BackingField = true;
                            MainPlugin.SendLog.LogInfo($"dicks after: {audioSourceComponent.spatialBlend}");
                            MainPlugin.SendLog.LogInfo("[SetupAudio] Successfully enabled audio spatialization.");

                        }
                    }
                }
            }
        }

        public async void LinkPositionUpdater(PlayerAgent player, GameObject userObject)
        {
            MainPlugin.SendLog.LogInfo($"Linked {player.PlayerName}'s position!");
            while (isInLevel || player != null || userObject != null) // Basically while true
            {
                userObject.transform.position = player.Position;
                userObject.transform.rotation = player.Rotation;
                await Task.Delay(50);
            }
            MainPlugin.SendLog.LogWarning($"Unlinked {player.PlayerName}!");
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
    }
}
