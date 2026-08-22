using System;
using GameLift.Audio;
using UnityEngine;
using Utils.LogicTimer;
using Utils.Signal;

namespace Game.Systems
{
    public sealed class ScoreService : BaseSystem
    {
        private readonly ScoreConfig config;
        private readonly IAudioService audioService;

        private float score;
        private float scoringElapsedTime;
        private int displayedScore = int.MinValue;
        private int nextScoreTriggerScore;
        private bool isScoring;

        public static ScoreService Instance { get; private set; }
        public int CurrentScore => Mathf.FloorToInt(score);
        public int RewardableScore => Mathf.FloorToInt(GetScoreClampedToMaxPossibility());
        public float MaxScorePossibility { get; private set; }
        public ScoreConfig Config => config;

        public event Action<int> ScoreChanged;

        public ScoreService(ScoreConfig config, IAudioService audioService = null)
        {
            if (Instance != null && Instance != this)
            {
                Instance.Dispose();
            }

            Instance = this;
            this.config = config != null ? config : ScriptableObject.CreateInstance<ScoreConfig>();
            this.audioService = audioService;
            SignalBus.Get<GameplayStartedSignal>().Subscribe(HandleGameplayStarted);
            SignalBus.Get<GameplayStoppedSignal>().Subscribe(HandleGameplayStopped);
            ResetScore();
        }

        public override void Tick()
        {
            if (!isScoring || config.scoreType != ScoreType.PlayTime)
            {
                return;
            }

            float delta = LogicTimer.FixedDelta;
            scoringElapsedTime += delta;
            GainScore(config.scorePerSecond * delta);
        }

        public void ResetScore()
        {
            score = Mathf.Max(0, config.initialScore);
            scoringElapsedTime = 0f;
            MaxScorePossibility = Mathf.Max(0f, config.scorePerSecond + 1f);
            nextScoreTriggerScore = CurrentScore + config.scoreTrigging;
            isScoring = false;
            RefreshScoreText(force: true);
        }

        public void StartScoring()
        {
            isScoring = true;
            RefreshScoreText(force: true);
        }

        public void StopScoring()
        {
            isScoring = false;
            RefreshScoreText(force: true);
        }

        public void ClampScoreToMaxPossibility()
        {
            score = GetScoreClampedToMaxPossibility();
            RefreshScoreText(force: true);
        }

        public void GainDefaultScore()
        {
            GainScore(config.defaultGainAmount);
        }

        public void GainScore(int amount)
        {
            GainScore((float)amount);
        }

        public void GainScore(float amount)
        {
            if (!isScoring || amount <= 0f)
            {
                return;
            }

            score += amount;
            RefreshScoreText();
            TryPlayScoreTriggerSound();
        }

        public override void Dispose()
        {
            SignalBus.Get<GameplayStartedSignal>().Unsubscribe(HandleGameplayStarted);
            SignalBus.Get<GameplayStoppedSignal>().Unsubscribe(HandleGameplayStopped);

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void HandleGameplayStarted()
        {
            ResetScore();
            StartScoring();
        }

        private void HandleGameplayStopped()
        {
            isScoring = false;
            ClampScoreToMaxPossibility();
        }

        private float GetScoreClampedToMaxPossibility()
        {
            if (config.scoreType != ScoreType.PlayTime)
            {
                return score;
            }

            float maxPossibleScore = Mathf.Max(0, config.initialScore) + MaxScorePossibility * scoringElapsedTime;
            return Mathf.Clamp(score, 0f, maxPossibleScore);
        }

        private void RefreshScoreText(bool force = false)
        {
            int currentScore = CurrentScore;
            if (!force && displayedScore == currentScore)
            {
                return;
            }

            displayedScore = currentScore;

            ScoreChanged?.Invoke(currentScore);
        }

        private void TryPlayScoreTriggerSound()
        {
            if (config.scoreTrigging <= 0 || string.IsNullOrEmpty(config.scoreTriggerSoundName))
            {
                return;
            }

            int currentScore = CurrentScore;
            while (currentScore >= nextScoreTriggerScore)
            {
                audioService?.Play(config.scoreTriggerSoundName);
                nextScoreTriggerScore += config.scoreTrigging;
            }
        }
    }
}
