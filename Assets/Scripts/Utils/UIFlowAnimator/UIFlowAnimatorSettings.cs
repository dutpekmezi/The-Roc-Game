using UnityEngine;

namespace Utils.ObjectFlowAnimator
{
    [CreateAssetMenu(fileName = "ObjectFlowSettings", menuName = "ObjectFlow/Object Flow Settings", order = 0)]
    public class UIFlowAnimatorSettings : ScriptableObject
    {
        public FlowParticle defaultUIAnimParticle;
        public DestinationActionData defaultDestinationActionData;
    }
}
