using Dissonance.Audio.Playback;
using Player;
using ProximityChat.PlayerHandler;
using ProximityChat.SteamComms;
using UnityEngine;
using HarmonyLib;
using GTFO.API;
using SNetwork;

namespace ProximityChat.Dissonance
{
    public class DissonanceUtils
    {
        // Setup instance linking.
        private MainPlugin? rootInstance;
        private SlotManager slotManager;
        private SteamLink steamLink;
        private PlayerHandler.PlayerManager playerManager;
        public bool isInLevel = false; // Initialize and store isInLevel check.

        // Giver Instance Loader.
        private static DissonanceUtils? _instance;
        public static DissonanceUtils Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new DissonanceUtils();
                return _instance;
            }
        }

        // Procced.

        public void Init() // Called once on plugin load.
        {
            // Grab instances.
            rootInstance = MainPlugin.Instance;
            slotManager = SlotManager.Instance;
            steamLink = SteamLink.Instance;
            playerManager = PlayerHandler.PlayerManager.Instance;

            LevelAPI.OnEnterLevel += onEnterLevel;
            LevelAPI.OnLevelCleanup += OnExitLevel;
        }

        public void onEnterLevel() // Called when level is started.
        {
            MainPlugin.SendLog.LogInfo("Entering Level!");
            isInLevel = true;
            
            playerManager.RefreshPlayers();
        }

        public void OnExitLevel() // Called when level is closed.
        {
            MainPlugin.SendLog.LogInfo("Exiting Level!");
            isInLevel = false;
        }


        public void SetupIncomingAudio(string targetDUID, int pSlot)
        {
            var DSObj = rootInstance.globalDS;
            var selfDUID = DSObj.GetComponent<global::Dissonance.Integrations.SteamworksP2P.SteamworksP2PCommsNetwork>().PlayerName;

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
            var playerClone = player;
            var userClone = userObject;

            MainPlugin.SendLog.LogInfo($"Linking {playerName}'s position!");
            while (isInLevel && GameStateManager.CurrentStateName.ToString() == "InLevel") // Basically while true when in level.
            {
                if (player == null || userObject == null || !userObject.activeInHierarchy)
                {
                    MainPlugin.SendLog.LogError($"Connection to player unexpectedly severed!");
                    break;
                }

                userClone.transform.position = playerClone.Position;
                userClone.transform.forward = playerClone.Forward;

                // MainPlugin.SendLog.LogInfo($"Updated {playerName}'s X position and X rotation to {playerClone.Position.x}, {playerClone.Forward.x}");
                await Task.Delay(50); // tune this

            }
            MainPlugin.SendLog.LogInfo($"Unlinked {playerName}! ({isInLevel}, {GameStateManager.CurrentStateName.ToString()})");
        }

        public async void OverrideBlend(AudioSource audioSourceComponent)
        {   // Not the best solution currently, but it works. Might show noticable lag if scaling past 4 players.
            while (isInLevel)
            {
                audioSourceComponent.spatialBlend = 1f;
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

    [HarmonyPatch(typeof(SNet_GlobalManager), "OnPlayerEvent")]
    class Patch_OnPlayerEvent
    {
        static void Postfix(SNetwork.SNet_Player player, SNetwork.SNet_PlayerEvent playerEvent)
        {
            // Check if the event is PlayerIsSynced
            if (playerEvent == SNetwork.SNet_PlayerEvent.PlayerIsSynced)
            {
                MainPlugin.SendLog.LogInfo($"Player {player.NickName} has synced with the game.");
                PlayerHandler.PlayerManager.Instance.RefreshPlayers();
            }

            if (playerEvent == SNetwork.SNet_PlayerEvent.PlayerLeftSessionHub)
            {
                MainPlugin.SendLog.LogInfo($"Player {player.NickName} has disconnected from the game.");
            }
        }
    }
}
