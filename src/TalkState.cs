using Agents;
using Dissonance;
using Dissonance.Audio.Playback;
using Player;
using PlayFab.DataModels;
using ProximityChat.Dissonance;
using ProximityChat.PlayerHandler;
using ProximityChat.SteamComms;
using SNetwork;
using UnityEngine;

namespace ProximityChat.TalkState
{
    public class SleeperWake
    {
        // Setup instance linking.
        private MainPlugin rootInstance;
        private DissonanceUtils dissonanceInstance;
        private SteamLink steamLink;
        private SlotManager slotManager;

        // Giver Instance Loader.
        private static SleeperWake _instance;
        public static SleeperWake Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SleeperWake();
                return _instance;
            }
        }

        public bool sleepWakeEnabled = false;

        // Proceed.
        public void Init()
        {
            if (!ProximityConfig.enableSleeperWake.Value)
                return;

            MainPlugin.SendLog.LogInfo("[SleeperWake] Config is true, checking host...");
            if (!SNet.LocalPlayer.IsMaster)
            {
                MainPlugin.SendLog.LogError("[SleeperWake] SleeperWake is enabled, but user is not the host. Disabling SleeperWake...");
                return;
            }
            MainPlugin.SendLog.LogInfo("[SleeperWake] User is indeed the host, enabling SleeperWake functionality!");
            
            sleepWakeEnabled = true; // Tells LinkPositionUpdater to pass Player Variables to us.

        }

        public async void enableSleeperWake(PlayerAgent sourceAgent, VoicePlayback? sourceObject = null, VoicePlayerState? selfObject = null)
        {
            // Save local copies to prevent nulling mfw
            var pAgent = sourceAgent;
            VoicePlayback dissonanceObject = sourceObject;

            MainPlugin.SendLog.LogInfo($"[SleeperWake] Starting listener for {pAgent.PlayerName}!");

            while (pAgent != null)
            {
                if (GameStateManager.CurrentStateName.ToString() != "InLevel")
                {
                    MainPlugin.SendLog.LogInfo($"[SleeperWake] Supposedly exited level, shutting down!");
                    break;
                }

                await Task.Delay(12);

                if (sourceObject == null && selfObject != null) // If local player
                {
                    while (selfObject.IsSpeaking)
                    {
                        pAgent.Noise = Agent.NoiseType.Walk;
                        await Task.Delay(12);
                    }
                    continue;
                }

                while (dissonanceObject.IsSpeaking)
                {
                    pAgent.Noise = Agent.NoiseType.Walk;
                    await Task.Delay(12);
                }
            }
            MainPlugin.SendLog.LogError("[SleeperWake] Connection to player unexpectedly severed!");
        }
    }
}
