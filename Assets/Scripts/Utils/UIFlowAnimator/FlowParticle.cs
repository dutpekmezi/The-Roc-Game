using UnityEngine;
using UnityEngine.UI;
using Utils.Pools;

namespace Utils.ObjectFlowAnimator
{
    public class FlowParticle : MonoBehaviour, IPoolable
    {
        [SerializeField] private Image image;
        public Image Image => image;

        public void OnSpawn()
        {
            transform.localScale = Vector3.one;
        }

        public void OnDespawn()
        {
            
        }
    }
}
