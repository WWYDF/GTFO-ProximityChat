using Player;
using ProximityChat.Dissonance;
using ProximityChat.PlayerHandler;
using ProximityChat.SteamComms;
using ProximityChat.TalkState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProximityChat.AudioEffects
{
    public class AudioEffects
    {
        // Setup instance linking
        private MainPlugin? rootInstance;
        private Dissonance.DissonanceUtils? dissonanceInstance;
        private PlayerHandler.SlotManager? slotManagerInstance;
        private SteamComms.SteamLink? steamLink;
        private PlayerHandler.PlayerManager? playerManager;
        private TalkState.SleeperWake? sleeperWake;

        // Giver Instance Loader
        public static AudioEffects _instance;
        public static AudioEffects Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AudioEffects();
                return _instance;
            }
        }

        // Setup variables.
        private float maxCutoffFrequency = 5000f;
        private float minCutoffFrequency = 500f;
        private AudioLowPassFilter lowPassFilter;
        public LayerMask obstacleLayerMask;

        // Procced.

        public void Init()
        {
            // Grab instances.
            rootInstance = MainPlugin.Instance;
            dissonanceInstance = DissonanceUtils.Instance;
            slotManagerInstance = SlotManager.Instance;
            steamLink = SteamLink.Instance;
            playerManager = PlayerHandler.PlayerManager.Instance;
            sleeperWake = SleeperWake.Instance;
        }

        public void InitPlayer(AudioSource playerSource, PlayerAgent otherPlayer)
        {
            // Add component to target players.
            lowPassFilter = playerSource.gameObject.AddComponent<AudioLowPassFilter>();
            lowPassFilter.cutoffFrequency = maxCutoffFrequency;
            UpdatePlayer(playerSource, otherPlayer); // Start updating loop.
        }

        public async void UpdatePlayer(AudioSource playerSource, PlayerAgent otherPlayer)
        {
            while (GameStateManager.CurrentStateName.ToString() == "InLevel")
            {
                PlayerAgent localPlayer = Player.PlayerManager.GetLocalPlayerAgent();
                Vector3 directionYield = otherPlayer.transform.position - localPlayer.transform.position;
                float distanceYield = directionYield.magnitude;

                // Raycast to check for obstacles between players.
                if (Physics.Raycast(localPlayer.transform.position, directionYield, out RaycastHit hit, distanceYield, obstacleLayerMask))
                {
                    // If it hits,
                    if (hit.collider != null)
                    {
                        lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassFilter.cutoffFrequency, minCutoffFrequency, Time.deltaTime * 5f);
                        MainPlugin.SendLog.LogInfo($"Obstacle detected, obscuring audio.");
                    }
                } else
                {
                    // No obstacles, clear audio.
                    lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassFilter.cutoffFrequency, maxCutoffFrequency, Time.deltaTime * 5f);
                    MainPlugin.SendLog.LogInfo($"No obstacle detected, clearing audio.");
                }
                await Task.Delay(1000);
            }
        }
    }
}
