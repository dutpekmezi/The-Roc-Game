namespace GameLift.Feedbacks
{
    public interface IFeedbackService
    {
        bool IsVibrationEnabled { get; }
        void Play(string id);
        void PlayHaptic(HapticType hapticType);
        void SetVibrationEnabled(bool enabled);
    }
}
