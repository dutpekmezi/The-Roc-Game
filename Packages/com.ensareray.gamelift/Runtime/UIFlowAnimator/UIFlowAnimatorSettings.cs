using UnityEngine;

namespace GameLift.ObjectFlowAnimator
{
    [CreateAssetMenu(fileName = "UIFlowAnimatorSettings", menuName = "Game Lift/UIFlowAnimator/UIFlowAnimatorSettings", order = 0)]
    public class UIFlowAnimatorSettings : ScriptableObject
    {
        public FlowParticle defaultUIAnimParticle;
        public DestinationActionData defaultDestinationActionData;
    }
}
