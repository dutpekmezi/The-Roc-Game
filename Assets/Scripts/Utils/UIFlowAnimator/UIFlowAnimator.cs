using System;
using System.Collections.Generic;
using System.Collections;
using GameLift.Audio;
using UnityEngine;
using Utils.Scene;
using Utils.Signal;
using Utils.Singleton;

namespace Utils.ObjectFlowAnimator
{
    public class UIFlowAnimator : Singleton<UIFlowAnimator>, IUIFlowAnimator
    {
        [SerializeField] private UIFlowAnimatorSettings settings;
        [SerializeField] private RectTransform flowCanvas;
        private List<DestinationAction> destinationActions = new();
        private IAudioService audioService;

        public UIFlowAnimatorSettings Settings => settings;

        [VContainer.Inject]
        private void Construct(IAudioService audioService)
        {
            this.audioService = audioService;
        }

        private void Start()
        {
            SignalBus.Get<OnSceneTransitionStarted>().Subscribe(OnSceneTransitionStarted);
        }

        private void OnDestroy()
        {
            SignalBus.Get<OnSceneTransitionStarted>().Unsubscribe(OnSceneTransitionStarted);
        }

        public void AddNewDestinationAction(Vector3 startScreenPos, Vector3 endScreenPos, Sprite sprite, RectTransform parent, int particleCount,
            float startDelay = 0f, DestinationActionData destinationActionData = null, FlowParticle prefab = null, Action onSpawn = null, Action onReceivedItem = null, Action onCompleted = null,
            string receivedSoundName = null)
        {
            if (startDelay > 0f)
            {
                StartCoroutine(AddNewDestinationActionAfterDelay(startDelay, startScreenPos, () => endScreenPos, sprite, parent, particleCount, destinationActionData, prefab, onSpawn, onReceivedItem, onCompleted, receivedSoundName));
                return;
            }

            DestinationActionProperties dap = new DestinationActionProperties();
            dap.startPos = startScreenPos;
            dap.endPos = endScreenPos;
            dap.sprite = sprite;
            dap.parent = parent;
            dap.particleCount = particleCount;
            dap.startDelay = startDelay;
            dap.destinationActionData = destinationActionData;
            dap.prefab = prefab;
            dap.onSpawn = onSpawn;
            dap.onReceivedItem = onReceivedItem;
            dap.onCompleted = onCompleted;
            dap.receivedSoundName = receivedSoundName;

            AddNewDestinationAction(dap);
        }

        public void AddNewDestinationAction(Vector3 startScreenPos, Func<Vector3> endScreenPosProvider, Sprite sprite, RectTransform parent, int particleCount,
            float startDelay = 0f, DestinationActionData destinationActionData = null, FlowParticle prefab = null, Action onSpawn = null, Action onReceivedItem = null, Action onCompleted = null,
            string receivedSoundName = null)
        {
            if (startDelay > 0f)
            {
                StartCoroutine(AddNewDestinationActionAfterDelay(startDelay, startScreenPos, endScreenPosProvider, sprite, parent, particleCount, destinationActionData, prefab, onSpawn, onReceivedItem, onCompleted, receivedSoundName));
                return;
            }

            Vector3 endScreenPos = endScreenPosProvider != null ? endScreenPosProvider() : startScreenPos;

            AddNewDestinationAction(startScreenPos, endScreenPos, sprite, parent, particleCount, 0f, destinationActionData, prefab, onSpawn, onReceivedItem, onCompleted, receivedSoundName);
        }

        public void AddNewDestinationAction(DestinationActionProperties destinationActionProperties)
        {
            if (destinationActionProperties.prefab == null)
            {
                destinationActionProperties.prefab = settings.defaultUIAnimParticle;
            }

            if (destinationActionProperties.destinationActionData == null)
            {
                destinationActionProperties.destinationActionData = settings.defaultDestinationActionData;
            }

            if (destinationActionProperties.parent == null)
            {
                destinationActionProperties.parent = flowCanvas;
            }

            destinationActionProperties.audioService ??= audioService;

            destinationActions.Add(new DestinationAction(destinationActionProperties));
        }

        public void CancelAllDestinationActions()
        {
            StopAllCoroutines();

            for (int i = 0; i < destinationActions.Count; i++)
            {
                destinationActions[i].Cancel();
            }

            destinationActions.Clear();
        }

        private IEnumerator AddNewDestinationActionAfterDelay(float delay, Vector3 startScreenPos, Func<Vector3> endScreenPosProvider, Sprite sprite, RectTransform parent, int particleCount,
            DestinationActionData destinationActionData, FlowParticle prefab, Action onSpawn, Action onReceivedItem, Action onCompleted, string receivedSoundName)
        {
            yield return new WaitForSeconds(delay);

            Vector3 endScreenPos = endScreenPosProvider != null ? endScreenPosProvider() : startScreenPos;

            AddNewDestinationAction(startScreenPos, endScreenPos, sprite, parent, particleCount, 0f, destinationActionData, prefab, onSpawn, onReceivedItem, onCompleted, receivedSoundName);
        }

        private void FixedUpdate()
        {
            for (int i = 0; i < destinationActions.Count; i++)
            {
                destinationActions[i].Tick();

                if (destinationActions[i].IsDone())
                {
                    destinationActions.RemoveAt(i);
                    --i;
                }
            }
        }

        private void OnSceneTransitionStarted(SceneConfig _)
        {
            CancelAllDestinationActions();
        }
    }
}
