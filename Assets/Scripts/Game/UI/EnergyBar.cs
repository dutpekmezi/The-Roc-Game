using Game.Systems;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    public class EnergyBar : MonoBehaviour
    {
        [SerializeField] private TMP_Text amountText;

        private EnergyService energyService;

        private void Awake()
        {
            ResolveAmountText();
        }

        private void OnEnable()
        {
            TryBindEnergyService();
            RefreshAmount();
        }

        private void Start()
        {
            TryBindEnergyService();
            RefreshAmount();
        }

        private void Update()
        {
            if (energyService == null)
            {
                TryBindEnergyService();
            }
        }

        private void OnDisable()
        {
            UnbindEnergyService();
        }

        private void OnValidate()
        {
            ResolveAmountText();
        }

        public void RefreshAmount()
        {
            ResolveAmountText();

            if (amountText != null)
            {
                amountText.text = (energyService != null ? energyService.CurrentEnergy : 0).ToString();
            }
        }

        public void Refresh()
        {
            RefreshAmount();
        }

        private void TryBindEnergyService()
        {
            EnergyService service = EnergyService.Instance;
            if (energyService == service)
            {
                return;
            }

            UnbindEnergyService();
            energyService = service;

            if (energyService != null)
            {
                energyService.EnergyChanged += HandleEnergyChanged;
            }

            RefreshAmount();
        }

        private void UnbindEnergyService()
        {
            if (energyService != null)
            {
                energyService.EnergyChanged -= HandleEnergyChanged;
                energyService = null;
            }
        }

        private void HandleEnergyChanged(int amount)
        {
            if (amountText != null)
            {
                amountText.text = amount.ToString();
            }
        }

        private void ResolveAmountText()
        {
            if (amountText == null)
            {
                amountText = GetComponentInChildren<TMP_Text>(true);
            }
        }
    }
}
