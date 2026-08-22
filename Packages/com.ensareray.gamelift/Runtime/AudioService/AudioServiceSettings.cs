using System.Collections.Generic;
using UnityEngine;

namespace GameLift.Audio
{
    [CreateAssetMenu(fileName = "AudioServiceSettings", menuName = "Game Lift/Audio/AudioServiceSettings")]
    public class AudioServiceSettings : ScriptableObject
    {
        [SerializeField] private List<SoundData> sounds;
        [SerializeField] private int sfxPoolSize = 10;

        public List<SoundData> Sounds => sounds;
        public int SfxPoolSize => sfxPoolSize;
    }
}
