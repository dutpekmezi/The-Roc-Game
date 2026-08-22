using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace GameLift.Audio
{
    public class AudioService : IAudioService, IStartable
    {
        private readonly AudioServiceSettings _settings;
        private readonly Dictionary<string, SoundData> _soundLookup = new();
        private readonly Dictionary<SoundType, AudioSource> _mainChannels = new();
        private readonly List<AudioSource> _sfxPool = new();
        private GameObject _audioRoot;

        public AudioService(AudioServiceSettings settings)
        {
            _settings = settings;
        }

        public void Start()
        {
            _audioRoot = new GameObject("[AudioService]");
            Object.DontDestroyOnLoad(_audioRoot);

            // Build lookup
            foreach (var sound in _settings.Sounds)
                _soundLookup[sound.soundName] = sound;

            // Create persistent channels for non-SFX types
            foreach (SoundType type in System.Enum.GetValues(typeof(SoundType)))
            {
                if (type == SoundType.SFX)
                    continue;

                var src = _audioRoot.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = (type == SoundType.Music || type == SoundType.Ambience);
                _mainChannels[type] = src;
            }

            // Create SFX pool
            for (int i = 0; i < _settings.SfxPoolSize; i++)
            {
                var src = _audioRoot.AddComponent<AudioSource>();
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

            if (freeSource == null)
                return;

            freeSource.clip = data.clip;
            freeSource.volume = data.volume;
            freeSource.pitch = data.pitch;
            freeSource.loop = data.loop;
            freeSource.Play();
        }

        public void SetSoundTypeEnabled(SoundType type, bool enabled)
        {
            if (type == SoundType.SFX || type == SoundType.UI)
            {
                foreach (var src in _sfxPool)
                    src.mute = !enabled;
            }

            if (_mainChannels.TryGetValue(type, out var channel))
                channel.mute = !enabled;
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
