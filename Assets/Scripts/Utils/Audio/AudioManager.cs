using System.Collections.Generic;
using UnityEngine;
using Utils.Singleton;

namespace Utils.Audio
{
    public class AudioManager : Singleton<AudioManager>
    {
        [SerializeField] private List<SoundData> sounds;
        [SerializeField] private int sfxPoolSize = 10; // how many SFX can overlap

        private readonly Dictionary<string, SoundData> _soundLookup = new();
        private readonly Dictionary<SoundType, AudioSource> _mainChannels = new();
        private readonly List<AudioSource> _sfxPool = new();

        protected override void Awake()
        {
            base.Awake();

            // Build lookup
            foreach (var sound in sounds)
                _soundLookup[sound.soundName] = sound;

            // Create persistent channels for non-SFX types
            foreach (SoundType type in System.Enum.GetValues(typeof(SoundType)))
            {
                if (type == SoundType.SFX)
                    continue;

                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = (type == SoundType.Music || type == SoundType.Ambience);
                _mainChannels[type] = src;
            }

            // Create SFX pool
            for (int i = 0; i < sfxPoolSize; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _sfxPool.Add(src);
            }
        }

        public void Play(string soundName)
        {
            if (!_soundLookup.TryGetValue(soundName, out var data))
            {
                Debug.LogWarning($"Sound '{soundName}' not found.");
                return;
            }

            Play(data);
        }

        public void Play(SoundData data)
        {
            switch (data.soundType)
            {
                case SoundType.SFX:
                    PlaySFX(data);
                    break;
                default:
                    PlayMain(data);
                    break;
            }
        }

        private void PlayMain(SoundData data)
        {
            var src = _mainChannels[data.soundType];
            src.clip = data.clip;
            src.volume = data.volume;
            src.pitch = data.pitch;
            src.loop = data.loop;
            src.Play();
        }

        private void PlaySFX(SoundData data)
        {
            AudioSource freeSource = _sfxPool.Find(s => !s.isPlaying);

            // if none are free, reuse the oldest one
            if (freeSource == null)
                freeSource = _sfxPool[0];

            freeSource.clip = data.clip;
            freeSource.volume = data.volume;
            freeSource.pitch = data.pitch;
            freeSource.loop = data.loop;
            freeSource.Play();
        }

        public void Stop(SoundType type)
        {
            if (type == SoundType.SFX)
            {
                foreach (var src in _sfxPool)
                    src.Stop();
            }
            else if (_mainChannels.TryGetValue(type, out var src))
            {
                src.Stop();
            }
        }
    }
}