using UnityEngine;
using UnityEngine.UI;
using Game.Systems;

namespace Game.UI
{
    public class SlotItem : MonoBehaviour
    {
        [SerializeField] private Image _image;

        public ProductConfig ProductConfig { get; private set; }

        public void Init(ProductConfig productConfig)
        {
            ProductConfig = productConfig;
            Init(productConfig != null ? productConfig.Sprite : null);
        }

        public void Init(Sprite sprite)
        {
            if (_image != null)
            {
                _image.sprite = sprite;
            }
        }
    }
}
