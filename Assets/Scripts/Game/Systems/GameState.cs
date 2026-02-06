using System.Collections.Generic;

namespace Game.Systems
{
    public enum GameFlowState
    {
        Menu,
        InGame,
        GameOver
    }

    public class GameState
    {
        private readonly Dictionary<string, int> pendingCurrencyRewards = new();

        public static GameState Instance { get; private set; }

        public GameFlowState CurrentState { get; private set; } = GameFlowState.Menu;

        public bool HasPendingCurrencyRewards => pendingCurrencyRewards.Count > 0;

        public GameState()
        {
            if (Instance != null && Instance != this)
            {
                return;
            }

            Instance = this;
        }

        public void SetState(GameFlowState state)
        {
            CurrentState = state;
        }

        public void SetPendingCurrencyRewards(Dictionary<string, int> rewards)
        {
            pendingCurrencyRewards.Clear();

            if (rewards == null)
            {
                return;
            }

            foreach (var reward in rewards)
            {
                if (reward.Value > 0)
                {
                    pendingCurrencyRewards[reward.Key] = reward.Value;
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
