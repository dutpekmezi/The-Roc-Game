using GameLift.Audio;
using GameLift.Feedbacks;
using GameLift.Save;
using VContainer.Unity;

namespace GameLift.Settings
{
    public class GameSettingsService : IStartable
    {
        private const string VibrationKey = "settings_vibration";
        private const string AudioKey = "settings_audio";
        private const string MusicKey = "settings_music";

        private readonly ISaveService _saveService;
        private readonly IAudioService _audioService;
        private readonly IFeedbackService _feedbackService;

        private bool _vibrationEnabled;
        private bool _audioEnabled;
        private bool _musicEnabled;

        public bool IsVibrationEnabled => _vibrationEnabled;
        public bool IsAudioEnabled => _audioEnabled;
        public bool IsMusicEnabled => _musicEnabled;

        public GameSettingsService(ISaveService saveService, IAudioService audioService, IFeedbackService feedbackService)
        {
            _saveService = saveService;
            _audioService = audioService;
            _feedbackService = feedbackService;
        }

        public void Start()
        {
            var raw = _saveService.Raw;

            _vibrationEnabled = LoadBool(raw, VibrationKey);
            _audioEnabled = LoadBool(raw, AudioKey);
            _musicEnabled = LoadBool(raw, MusicKey);

            ApplyVibration();
            ApplyAudio();
            ApplyMusic();
        }

        public void ToggleVibration()
        {
            _vibrationEnabled = !_vibrationEnabled;
            ApplyVibration();
            _saveService.Raw.Save(VibrationKey, _vibrationEnabled.ToString());
        }

        public void ToggleAudio()
        {
            _audioEnabled = !_audioEnabled;
            ApplyAudio();
            _saveService.Raw.Save(AudioKey, _audioEnabled.ToString());
        }

        public void ToggleMusic()
        {
            _musicEnabled = !_musicEnabled;
            ApplyMusic();
            _saveService.Raw.Save(MusicKey, _musicEnabled.ToString());
        }

        private void ApplyVibration()
        {
            _feedbackService.SetVibrationEnabled(_vibrationEnabled);
        }

        private void ApplyAudio()
        {
            _audioService.SetSoundTypeEnabled(SoundType.SFX, _audioEnabled);
            _audioService.SetSoundTypeEnabled(SoundType.UI, _audioEnabled);
        }

        private void ApplyMusic()
        {
            _audioService.SetSoundTypeEnabled(SoundType.Music, _musicEnabled);
        }

        private static bool LoadBool(PrimitiveSaveHelper raw, string key)
        {
            var data = raw.LoadData(key);
            return string.IsNullOrEmpty(data) || data == true.ToString();
        }
    }
}
