using System;
using UnityEngine;

namespace Utils.ObjectFlowAnimator
{
    public interface IUIFlowAnimator
    {
        public UIFlowAnimatorSettings Settings { get; }
        public void AddNewDestinationAction(DestinationActionProperties destinationActionProperties);
        public void AddNewDestinationAction(Vector3 startScreenPos, Vector3 endScreenPos, Sprite sprite, RectTransform parent, int particleCount,
            DestinationActionData destinationActionData = null, FlowParticle prefab = null, Action onSpawn = null, Action onReceivedItem = null, Action onCompleted = null);
    }
}
