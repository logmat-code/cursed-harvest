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
            if (descriptionText != null) descriptionText.text = upgrade.description;

            if (categoryText != null)
            {
                if (upgrade.currentLevel == 0)
                {
                    bool isWeapon = upgrade.category.Equals("Weapon", StringComparison.OrdinalIgnoreCase);
                    categoryText.text = isWeapon ? "★ NEW WEAPON ★" : "★ NEW ABILITY ★";
                    categoryText.color = new Color(0.2f, 1f, 0.45f); // Vibrant Emerald Green
                }
                else
                {
                    categoryText.text = $"UPGRADE (LVL {upgrade.currentLevel + 1})";
                    categoryText.color = new Color(0.35f, 0.85f, 1f); // Vibrant Cyan
                }
            }

            if (levelText != null)
            {
                int nextLvl = upgrade.currentLevel + 1;
                levelText.text = upgrade.currentLevel == 0 ? "UNLOCKED" : $"Lv. {nextLvl} / {upgrade.maxLevel}";
                levelText.color = upgrade.currentLevel == 0 ? new Color(1f, 0.85f, 0.2f) : Color.white;
            }
        }

        private void HandleClicked()
        {
            onSelected?.Invoke();
        }
    }
}
