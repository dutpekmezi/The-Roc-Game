using System;
using UnityEngine;

namespace GameLift.ObjectFlowAnimator
{
    public interface IUIFlowAnimator
    {
        public UIFlowAnimatorSettings Settings { get; }
        public void AddNewDestinationAction(string id, DestinationActionProperties destinationActionProperties);
        public void AddNewDestinationAction(string id, Vector3 startScreenPos, Vector3 endScreenPos, Sprite sprite, int particleCount, RectTransform parent = null,
            DestinationActionData destinationActionData = null, FlowParticle prefab = null, Action onSpawn = null, Action onReceivedItem = null, Action onCompleted = null);
        public bool IsPlaying(string id);
    }
}
