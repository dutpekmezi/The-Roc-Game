using UnityEngine;

namespace Utils.Audio
{
    [CreateAssetMenu(menuName = "Audio/Sound Data")]
    public class SoundData : ScriptableObject
    {
        public string soundName;
        public AudioClip clip;
        public SoundType soundType;

        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        public bool loop;
    }

    public enum SoundType
    {
        SFX,
        Music,
        Ambience,
        UI
    }
}