using System;
using System.Collections.Generic;
using GameLift.Pooling;
using UnityEngine;
using VContainer.Unity;

namespace GameLift.ObjectFlowAnimator
{
    public class UIFlowAnimator : IUIFlowAnimator, IFixedTickable
    {
        private Dictionary<string, DestinationAction> _destinationActions = new();
        private UIFlowAnimatorSettings _settings;

        private List<string> _keysToRemove = new();

        private readonly IPools _pools;
        private readonly RectTransform _flowCanvas;

        public UIFlowAnimatorSettings Settings => _settings;

        public UIFlowAnimator(UIFlowAnimatorSettings settings, IPools pools, RectTransform flowCanvas)
        {
            _settings = settings;
            _pools = pools;
            _flowCanvas = flowCanvas;
        }

        public void AddNewDestinationAction(string id, Vector3 startScreenPos, Vector3 endScreenPos, Sprite sprite, int particleCount, RectTransform parent = null,
            DestinationActionData destinationActionData = null, FlowParticle prefab = null, Action onSpawn = null, Action onReceivedItem = null, Action onCompleted = null)
        {
            DestinationActionProperties dap = new DestinationActionProperties();
            dap.startPos = startScreenPos;
            dap.endPos = endScreenPos;
            dap.sprite = sprite;
            dap.parent = parent;
            dap.particleCount = particleCount;
            dap.destinationActionData = destinationActionData;
            dap.prefab = prefab;
            dap.onSpawn = onSpawn;
            dap.onReceivedItem = onReceivedItem;
            dap.onCompleted = onCompleted;

            AddNewDestinationAction(id, dap);
        }

        public void AddNewDestinationAction(string id, DestinationActionProperties destinationActionProperties)
        {
            if (destinationActionProperties.prefab == null)
            {
                destinationActionProperties.prefab = _settings.defaultUIAnimParticle;
            }

            if (destinationActionProperties.destinationActionData == null)
            {
                destinationActionProperties.destinationActionData = _settings.defaultDestinationActionData;
            }

            if (destinationActionProperties.parent == null)
            {
                destinationActionProperties.parent = _flowCanvas;
            }

            _destinationActions.Add(id, new DestinationAction(_pools, destinationActionProperties));
        }

        public void FixedTick()
        {
            foreach (var destinationAction in _destinationActions)
            {
                destinationAction.Value.Tick();

                if (destinationAction.Value.IsDone())
                {
                    _keysToRemove.Add(destinationAction.Key);
                }
            }

            for (int i = 0; i < _keysToRemove.Count; i++)
            {
                string key = _keysToRemove[i];

                _destinationActions.Remove(key);

                _keysToRemove.Remove(key);  
                i--;
            }
        }

        public bool IsPlaying(string id)
        {
            return _destinationActions.ContainsKey(id);
        }
    }
}
