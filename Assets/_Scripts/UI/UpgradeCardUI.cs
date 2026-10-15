using System;
using UnityEngine;
using UnityEngine.UI;

namespace CursedHarvest
{
    public class UpgradeCardUI : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text categoryText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text levelText;
        [SerializeField] private Button selectButton;

        private Action onSelected;

        private void Awake()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(HandleClicked);
            }
        }

        public void Setup(UpgradeData upgrade, Action onSelectCallback)
        {
            onSelected = onSelectCallback;

            if (titleText != null) titleText.text = upgrade.title;
            if (categoryText != null) categoryText.text = upgrade.category.ToUpper();
            if (descriptionText != null) descriptionText.text = upgrade.description;
            if (levelText != null)
            {
                int nextLvl = upgrade.currentLevel + 1;
                levelText.text = $"Lv. {nextLvl} / {upgrade.maxLevel}";
            }
        }

        private void HandleClicked()
        {
            onSelected?.Invoke();
        }
    }
}
