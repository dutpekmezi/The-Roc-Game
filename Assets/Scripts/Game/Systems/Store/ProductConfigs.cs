using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using Utils.Currency;

namespace Game.Systems
{
    [System.Serializable]
    public class SectionConfig
    {
        public ProductSection section;
        public Color color = Color.white;
    }

    [CreateAssetMenu(fileName = "ProductConfigs", menuName = "Game/Product/Product Configs")]
    public class ProductConfigs : ScriptableObject
    {
        public List<ProductConfig> configs;
        public List<SectionConfig> sectionConfigs;

        public bool TryGetSectionColor(ProductSection section, out Color color)
        {
            if (sectionConfigs != null)
            {
                for (int i = 0; i < sectionConfigs.Count; i++)
                {
                    var config = sectionConfigs[i];

                    if (config != null && config.section == section)
                    {
                        color = config.color;
                        return true;
                    }
                }
            }

            color = Color.white;
            return false;
        }
    }
}
