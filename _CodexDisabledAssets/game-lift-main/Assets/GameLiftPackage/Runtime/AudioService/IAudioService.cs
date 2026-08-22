namespace GameLift.Audio
{
    public interface IAudioService
    {
        void Play(string soundName);
        void Play(SoundData data);
        void Stop(SoundType type);
        void SetSoundTypeEnabled(SoundType type, bool enabled);
    }
}
