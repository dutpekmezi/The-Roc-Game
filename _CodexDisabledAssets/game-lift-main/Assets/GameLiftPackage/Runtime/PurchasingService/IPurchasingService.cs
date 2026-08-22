using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Purchasing;

namespace GameLift.Purchasing
{
    public interface IPurchasingService
    {
        bool IsInitialized { get; }
        void PurchaseProduct(string productId);
        void RestorePurchases();
        Product GetProduct(string productId);
        List<Product> GetAllProducts();
        List<ProductConfig> GetAllProductConfigs();
    }
}
