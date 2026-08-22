using Game.Systems;
using TMPro;
using UnityEngine;
using Utils.Signal;

namespace Game.UI
{
    public class ScoreControllerUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreText;

        private ScoreService scoreService;
        private bool subscribed;
        private bool gameplaySignalsSubscribed;

        private void OnEnable()
        {
            SetScore(0);
            SetScoreVisible(GameState.Instance != null && GameState.Instance.CurrentState == GameFlowState.InGame);
            SubscribeGameplaySignals();
            TrySubscribe();
        }

        private void Start()
        {
            TrySubscribe();
        }

        private void Update()
        {
            if (!subscribed)
            {
                TrySubscribe();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
            UnsubscribeGameplaySignals();
        }

        private void TrySubscribe()
        {
            if (subscribed)
            {
                return;
            }

            scoreService = ScoreService.Instance;
            if (scoreService == null)
            {
                return;
            }

            scoreService.ScoreChanged -= SetScore;
            scoreService.ScoreChanged += SetScore;
            subscribed = true;
            SetScore(scoreService.CurrentScore);
        }

        private void Unsubscribe()
        {
            if (scoreService != null)
            {
                scoreService.ScoreChanged -= SetScore;
            }

            scoreService = null;
            subscribed = false;
        }

        private void SetScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = score.ToString();
            }
        }

        private void SubscribeGameplaySignals()
        {
            if (gameplaySignalsSubscribed)
            {
                return;
            }

            SignalBus.Get<GameplayStartedSignal>().Subscribe(HandleGameplayStarted);
            SignalBus.Get<GameplayStoppedSignal>().Subscribe(HandleGameplayStopped);
            gameplaySignalsSubscribed = true;
        }

        private void UnsubscribeGameplaySignals()
        {
            if (!gameplaySignalsSubscribed)
            {
                return;
            }

            SignalBus.Get<GameplayStartedSignal>().Unsubscribe(HandleGameplayStarted);
            SignalBus.Get<GameplayStoppedSignal>().Unsubscribe(HandleGameplayStopped);
            gameplaySignalsSubscribed = false;
        }

        private void HandleGameplayStarted()
        {
            SetScoreVisible(true);

            if (scoreService != null)
            {
                SetScore(scoreService.CurrentScore);
            }
        }

        private void HandleGameplayStopped()
        {
            SetScoreVisible(false);
        }

        private void SetScoreVisible(bool visible)
        {
            if (scoreText != null)
            {
                scoreText.enabled = visible;
            }
        }
    }
}
