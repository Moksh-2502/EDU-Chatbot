using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SubwaySurfers;
using UnityEngine;

namespace Sounds
{
    public class MusicPlayer : MonoBehaviour
    {
    
        [System.Serializable]
        public class Stem
        {
            public AudioSource source;
            public AudioClip clip;
            public float startingSpeedRatio; // The stem will start when this is lower than currentSpeed/maxSpeed.
        }

        static protected MusicPlayer s_Instance;

        static public MusicPlayer instance
        {
            get { return s_Instance; }
        }

        public UnityEngine.Audio.AudioMixer mixer;
        public Stem[] stems;
        public float maxVolume = 0.1f;
        [SerializeField] private AudioMixerData[] mixerConfigs;

        void Awake()
        {
            if (s_Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;

            // As this is one of the first script executed, set that here.
            Application.targetFrameRate = 30;

            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            StartAsync().Forget();
        }

        private async UniTaskVoid StartAsync()
        {
            var playerData = await IPlayerDataProvider.Instance.GetAsync();
            if (playerData.audioData == null || playerData.audioData.Count == 0)
            {
                playerData.audioData = new List<AudioMixerData>(4);
            }
            for (int i = 0; i < mixerConfigs.Length; ++i)
            {
                var mixerData = playerData.audioData.FirstOrDefault(o=> o.name == mixerConfigs[i].name);
                if (mixerData == null)
                {
                    mixerData = new AudioMixerData
                    {
                        name = mixerConfigs[i].name,
                        volume = mixerConfigs[i].volume
                    };
                    playerData.audioData.Add(mixerData);
                }
            }

            foreach (var mixerConfig in playerData.audioData)
            {
                mixer.SetFloat(mixerConfig.name, mixerConfig.volume);
            }
            StartCoroutine(RestartAllStems());
        }

        public void SetStem(int index, AudioClip clip)
        {
            if (stems.Length <= index)
            {
                Debug.LogError("Trying to set an undefined stem");
                return;
            }

            stems[index].clip = clip;
        }

        public AudioClip GetStem(int index)
        {
            return stems.Length <= index ? null : stems[index].clip;
        }

        public IEnumerator RestartAllStems()
        {
            for (int i = 0; i < stems.Length; ++i)
            {
                stems[i].source.clip = stems[i].clip;
                stems[i].source.volume = 0.0f;
                stems[i].source.Play();
            }

            // This is to fix a bug in the Audio Mixer where attenuation will be applied only a few ms after the source start playing.
            // So we play all source at volume 0.0f first, then wait 50 ms before finally setting the actual volume.
            yield return new WaitForSeconds(0.05f);

            for (int i = 0; i < stems.Length; ++i)
            {
                stems[i].source.volume = stems[i].startingSpeedRatio <= 0.0f ? maxVolume : 0.0f;
            }
        }

        public void UpdateVolumes(float currentSpeedRatio)
        {
            const float fadeSpeed = 0.5f;

            for (int i = 0; i < stems.Length; ++i)
            {
                float target = currentSpeedRatio >= stems[i].startingSpeedRatio ? maxVolume : 0.0f;
                stems[i].source.volume = Mathf.MoveTowards(stems[i].source.volume, target, fadeSpeed * Time.deltaTime);
            }
        }
    }
}