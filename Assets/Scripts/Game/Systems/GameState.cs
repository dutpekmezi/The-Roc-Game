using System.Collections.Generic;
using System;
using Utils.Signal;

namespace Game.Systems
{
    public enum GameFlowState
    {
        Menu,
        WaitingToStart,
        InGame,
        GameOver
    }

    public class GameState
    {
        private readonly Dictionary<string, int> pendingCurrencyRewards = new();
        private bool startGameSceneImmediately;
        private bool nextGameStartEnergySpent;
        private string activeRunId = string.Empty;
        private bool gameplaySignalsSubscribed;

        public static GameState Instance { get; private set; }

        public GameFlowState CurrentState { get; private set; } = GameFlowState.Menu;

        public bool HasPendingCurrencyRewards => pendingCurrencyRewards.Count > 0;
        public string ActiveRunId => activeRunId;

        public event Action<GameFlowState> StateChanged;

        public GameState()
        {
            if (Instance != null && Instance != this)
            {
                Instance.UnsubscribeGameplaySignals();
            }

            Instance = this;
            SubscribeGameplaySignals();
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
            SetState(GameFlowState.InGame);
        }

        private void HandleGameplayStopped()
        {
            SetState(GameFlowState.GameOver);
        }

        public void SetState(GameFlowState state)
        {
            if (CurrentState == state)
            {
                return;
            }

            CurrentState = state;
            StateChanged?.Invoke(CurrentState);
        }

        public void RequestImmediateGameStart()
        {
            startGameSceneImmediately = true;
        }

        public bool ConsumeImmediateGameStartRequest()
        {
            if (!startGameSceneImmediately)
            {
                return false;
            }

            startGameSceneImmediately = false;
            return true;
        }

        public void MarkNextGameStartEnergySpent()
        {
            nextGameStartEnergySpent = true;
        }

        public bool ConsumeNextGameStartEnergySpent()
        {
            if (!nextGameStartEnergySpent)
            {
                return false;
            }

            nextGameStartEnergySpent = false;
            return true;
        }

        public void SetActiveRunId(string runId)
        {
            activeRunId = runId ?? string.Empty;
        }

        public string ConsumeActiveRunId()
        {
            string runId = activeRunId;
            activeRunId = string.Empty;
            return runId;
        }

        public void SetPendingCurrencyRewards(Dictionary<string, int> rewards)
        {
            pendingCurrencyRewards.Clear();
            AddPendingCurrencyRewards(rewards);
        }

        public void AddPendingCurrencyRewards(Dictionary<string, int> rewards)
        {
            if (rewards == null)
            {
                return;
            }

            foreach (var reward in rewards)
            {
                if (string.IsNullOrEmpty(reward.Key) || reward.Value <= 0)
                {
                    continue;
                }

                pendingCurrencyRewards.TryGetValue(reward.Key, out var currentAmount);
                pendingCurrencyRewards[reward.Key] = currentAmount + reward.Value;
            }
        }

        public Dictionary<string, int> GetPendingCurrencyRewardsSnapshot()
        {
            return new Dictionary<string, int>(pendingCurrencyRewards);
        }

        public void RemovePendingCurrencyRewards(Dictionary<string, int> rewards)
        {
            if (rewards == null)
            {
                return;
            }

            foreach (var reward in rewards)
            {
                if (string.IsNullOrEmpty(reward.Key) || reward.Value <= 0)
                {
                    continue;
                }

                if (!pendingCurrencyRewards.TryGetValue(reward.Key, out var currentAmount))
                {
                    continue;
                }

                var remainingAmount = currentAmount - reward.Value;
                if (remainingAmount <= 0)
                {
                    pendingCurrencyRewards.Remove(reward.Key);
                }
                else
                {
                    pendingCurrencyRewards[reward.Key] = remainingAmount;
                }
            }
        }

        public Dictionary<string, int> ConsumePendingCurrencyRewards()
        {
            if (pendingCurrencyRewards.Count == 0)
            {
                return new Dictionary<string, int>();
            }

            var result = new Dictionary<string, int>(pendingCurrencyRewards);
            pendingCurrencyRewards.Clear();
            return result;
        }
    }
}
