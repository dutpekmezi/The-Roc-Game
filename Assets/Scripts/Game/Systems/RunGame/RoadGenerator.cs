using UnityEngine;
using Utils.LogicTimer;
using Utils.Signal;

namespace Game.Systems
{
    public class RoadGenerator : MonoBehaviour
    {
        private const int RoadPieceCount = 2;

        [SerializeField] private RoadConfig roadConfig;

        private readonly Transform[] roadPieces = new Transform[RoadPieceCount];
        private bool movementStopped = true;
        private bool gameplaySignalsSubscribed;

        public RoadConfig RoadConfig => roadConfig;

        private void OnEnable()
        {
            SubscribeGameplaySignals();
        }

        private void OnDisable()
        {
            UnsubscribeGameplaySignals();
        }

        public void SetConfig(RoadConfig config)
        {
            if (config != null)
            {
                roadConfig = config;
            }
        }

        public void ResetForRestart(bool stopMovement = false)
        {
            movementStopped = stopMovement;
            EnsureRoadPieces();
            PositionRoadPieces();
        }

        public void StopMovement()
        {
            movementStopped = true;
        }

        public void Tick()
        {
            if (movementStopped || roadConfig == null)
            {
                return;
            }

            EnsureRoadPieces();

            float deltaX = roadConfig.moveSpeed * LogicTimer.FixedDelta;
            if (deltaX <= 0f)
            {
                return;
            }

            for (int i = 0; i < roadPieces.Length; i++)
            {
                if (roadPieces[i] == null)
                {
                    continue;
                }

                roadPieces[i].localPosition += Vector3.left * deltaX;
            }

            RecycleRoadPieces();
        }

        public void Dispose()
        {
            UnsubscribeGameplaySignals();

            for (int i = 0; i < roadPieces.Length; i++)
            {
                if (roadPieces[i] != null)
                {
                    Destroy(roadPieces[i].gameObject);
                    roadPieces[i] = null;
                }
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
            ResetForRestart();
        }

        private void HandleGameplayStopped()
        {
            StopMovement();
        }

        private void EnsureRoadPieces()
        {
            if (roadConfig == null)
            {
                return;
            }

            for (int i = 0; i < roadPieces.Length; i++)
            {
                if (roadPieces[i] != null)
                {
                    continue;
                }

                var prefab = roadConfig.GetPrefab(i);
                if (prefab == null)
                {
                    continue;
                }

                var roadObject = Instantiate(prefab, transform);
                roadObject.name = prefab.name;
                roadObject.SetActive(true);
                roadPieces[i] = roadObject.transform;
            }
        }

        private void PositionRoadPieces()
        {
            if (roadConfig == null)
            {
                return;
            }

            float nextX = roadConfig.startX;

            for (int i = 0; i < roadPieces.Length; i++)
            {
                if (roadPieces[i] == null)
                {
                    continue;
                }

                roadPieces[i].localPosition = new Vector3(
                    nextX,
                    roadConfig.startY,
                    roadConfig.startZ);
                roadPieces[i].localRotation = Quaternion.identity;

                nextX += roadConfig.tileWidth + GetSpawnGapDistance();
            }
        }

        private void RecycleRoadPieces()
        {
            if (roadConfig == null)
            {
                return;
            }

            float recycleX = roadConfig.startX - roadConfig.tileWidth - roadConfig.recyclePadding;

            for (int i = 0; i < roadPieces.Length; i++)
            {
                var piece = roadPieces[i];
                if (piece == null || piece.localPosition.x > recycleX)
                {
                    continue;
                }

                float rightmostX = GetRightmostX(piece);
                piece.localPosition = new Vector3(
                    rightmostX + roadConfig.tileWidth + GetSpawnGapDistance(),
                    roadConfig.startY,
                    roadConfig.startZ);
            }
        }

        private float GetSpawnGapDistance()
        {
            if (roadConfig == null || roadConfig.moveSpeed <= 0f)
            {
                return 0f;
            }

            return roadConfig.moveSpeed * roadConfig.GetSpawnInterval();
        }

        private float GetRightmostX(Transform ignoredPiece)
        {
            float rightmostX = roadConfig != null ? roadConfig.startX : 0f;

            for (int i = 0; i < roadPieces.Length; i++)
            {
                var piece = roadPieces[i];
                if (piece != null && piece != ignoredPiece)
                {
                    rightmostX = Mathf.Max(rightmostX, piece.localPosition.x);
                }
            }

            return rightmostX;
        }
    }
}
