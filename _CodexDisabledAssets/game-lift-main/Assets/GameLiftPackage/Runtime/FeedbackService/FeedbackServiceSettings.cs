using System;
using System.Collections.Generic;
using GameLift.Audio;
using UnityEngine;

namespace GameLift.Feedbacks
{
    public enum HapticType
    {
        Light,
        Medium,
        Heavy,
        Selection,
        Success,
        Warning,
        Failure
    }

    [Serializable]
    public class FeedbackAudioConfig
    {
        public bool Enabled;
        public SoundData Sound;
    }

    [Serializable]
    public class FeedbackVibrationConfig
    {
        public bool Enabled;
        public HapticType HapticType;
    }

    [Serializable]
    public class FeedbackConfig
    {
        public string Id;
        public FeedbackAudioConfig Audio;
        public FeedbackVibrationConfig Vibration;
    }

    [CreateAssetMenu(fileName = "FeedbackServiceSettings", menuName = "Game Lift/Feedback/FeedbackServiceSettings")]
    public class FeedbackServiceSettings : ScriptableObject
    {
        [SerializeField] private List<FeedbackConfig> feedbacks;

        public List<FeedbackConfig> Feedbacks => feedbacks;
    }
}
