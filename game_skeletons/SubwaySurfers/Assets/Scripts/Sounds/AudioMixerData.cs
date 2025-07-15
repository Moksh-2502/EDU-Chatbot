using System;
using UnityEngine.Serialization;

namespace Sounds
{
    [Serializable]
    public class AudioMixerData
    {
        public string name;
        [FormerlySerializedAs("defaultVolume")] public float volume;
    }
}