using UnityEngine;

namespace Utils.Audio
{
    public class AudioChannel
    {
        private readonly AudioSource _source;

        public AudioChannel(GameObject owner, SoundType type)
        {
            _source = owner.AddComponent<AudioSource>();
            _source.playOnAwake = false;

            if (type == SoundType.Music)
                _source.loop = true;
        }

        public void Play(SoundData data)
        {
            _source.clip = data.clip;
            _source.volume = data.volume;
            _source.pitch = data.pitch;
            _source.loop = data.loop;
            _source.Play();
        }

        public void Stop() => _source.Stop();

        public bool IsPlaying => _source.isPlaying;
    }
}