using System.Collections.Generic;
using GameLift.Audio;
using UnityEngine;
using VContainer.Unity;

namespace GameLift.Feedbacks
{
    public class FeedbackService : IFeedbackService, IStartable
    {
        private readonly FeedbackServiceSettings _settings;
        private readonly IAudioService _audioService;
        private readonly Dictionary<string, FeedbackConfig> _feedbackLookup = new();
        private bool _vibrationEnabled = true;

        public bool IsVibrationEnabled => _vibrationEnabled;

        public FeedbackService(FeedbackServiceSettings settings, IAudioService audioService)
        {
            _settings = settings;
            _audioService = audioService;
        }

        public void Start()
        {
            Vibration.Init();

            foreach (var config in _settings.Feedbacks)
            {
                if (string.IsNullOrEmpty(config.Id))
                {
                    Debug.LogWarning("[FeedbackService] Feedback config with empty Id found, skipping.");
                    continue;
                }

                if (!_feedbackLookup.TryAdd(config.Id, config))
                {
                    Debug.LogWarning($"[FeedbackService] Duplicate feedback Id '{config.Id}' found, skipping.");
                }
            }
        }

        public void Play(string id)
        {
            if (!_feedbackLookup.TryGetValue(id, out var config))
            {
                Debug.LogWarning($"[FeedbackService] Feedback '{id}' not found.");
                return;
            }

            if (config.Audio is { Enabled: true, Sound: not null })
            {
                _audioService.Play(config.Audio.Sound);
            }

            if (config.Vibration is { Enabled: true })
            {
                PlayHaptic(config.Vibration.HapticType);
            }
        }

        public void SetVibrationEnabled(bool enabled)
        {
            _vibrationEnabled = enabled;
        }

        public void PlayHaptic(HapticType hapticType)
        {
            if (!_vibrationEnabled || !Vibration.IsAvailable())
            {
                return;
            }

#if UNITY_IOS
            switch (hapticType)
            {
                case HapticType.Selection:
                    Vibration.VibratePop();
                    break;
                case HapticType.Light:
                    Vibration.VibrateIOS(ImpactFeedbackStyle.Light);
                    break;
                case HapticType.Medium:
                    Vibration.VibrateIOS(ImpactFeedbackStyle.Medium);
                    break;
                case HapticType.Heavy:
                    Vibration.VibrateIOS(ImpactFeedbackStyle.Heavy);
                    break;
                case HapticType.Success:
                    Vibration.VibrateIOS(NotificationFeedbackStyle.Success);
                    break;
                case HapticType.Warning:
                    Vibration.VibrateIOS(NotificationFeedbackStyle.Warning);
                    break;
                case HapticType.Failure:
                    Vibration.VibrateIOS(NotificationFeedbackStyle.Error);
                    break;
            }
#elif UNITY_ANDROID
            switch (hapticType)
            {
                case HapticType.Selection:
                    Vibration.VibrateAndroid(35);
                    break;
                case HapticType.Light:
                    Vibration.VibrateAndroid(50);
                    break;
                case HapticType.Medium:
                    Vibration.VibrateAndroid(80);
                    break;
                case HapticType.Heavy:
                    Vibration.VibrateAndroid(150);
                    break;
                case HapticType.Success:
                    Vibration.VibrateAndroid(100);
                    break;
                case HapticType.Warning:
                    Vibration.VibrateAndroid(200);
                    break;
                case HapticType.Failure:
                    Vibration.VibrateAndroid(300);
                    break;
            }
#endif
        }
    }
}
