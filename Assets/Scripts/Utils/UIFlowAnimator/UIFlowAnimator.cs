using System;
using System.Collections.Generic;
using UnityEngine;
using Utils.Singleton;

namespace Utils.ObjectFlowAnimator
{
    public class UIFlowAnimator : Singleton<UIFlowAnimator>, IUIFlowAnimator
    {
        [SerializeField] private UIFlowAnimatorSettings settings;
        [SerializeField] private RectTransform flowCanvas;
        private List<DestinationAction> destinationActions = new();

        public UIFlowAnimatorSettings Settings => settings;

        public void AddNewDestinationAction(Vector3 startScreenPos, Vector3 endScreenPos, Sprite sprite, RectTransform parent, int particleCount,
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

            AddNewDestinationAction(dap);
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

            destinationActions.Add(new DestinationAction(destinationActionProperties));
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
    }
}