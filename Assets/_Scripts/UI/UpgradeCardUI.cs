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
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(HandleClicked);
            }
        }

        public void ConfigureReferences(Text title, Text category, Text desc, Text level, Button btn)
        {
            titleText = title;
            categoryText = category;
            descriptionText = desc;
            levelText = level;
            selectButton = btn;
            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(HandleClicked);
            }
        }

        public void Setup(UpgradeData upgrade, Action onSelectCallback)
        {
            onSelected = onSelectCallback;

            if (titleText != null)
            {
                titleText.text = upgrade.title;
                titleText.color = new Color(0.95f, 0.93f, 0.88f); // ivory
            }
            if (descriptionText != null)
            {
                descriptionText.text = upgrade.description;
                descriptionText.color = new Color(0.85f, 0.83f, 0.78f); // soft ivory/parchment
            }

            if (categoryText != null)
            {
                if (upgrade.currentLevel == 0)
                {
                    bool isWeapon = upgrade.category.Equals("Weapon", StringComparison.OrdinalIgnoreCase);
                    if (isWeapon)
                    {
                        categoryText.text = "★ NEW WEAPON ★";
                        categoryText.color = new Color(0.25f, 0.95f, 0.50f); // spectral emerald (#40f280)
                    }
                    else
                    {
                        categoryText.text = "★ NEW ABILITY ★";
                        categoryText.color = new Color(0.38f, 0.69f, 0.47f); // muted green (#62b078)
                    }
                }
                else
                {
                    categoryText.text = $"★ UPGRADE (LVL {upgrade.currentLevel + 1}) ★";
                    categoryText.color = new Color(0.85f, 0.72f, 0.32f); // aged gold (#d9b852)
                }
            }

            if (levelText != null)
            {
                int nextLvl = upgrade.currentLevel + 1;
                levelText.text = upgrade.currentLevel == 0 ? "UNLOCKED" : $"LEVEL {nextLvl} / {upgrade.maxLevel}";
                levelText.color = new Color(0.85f, 0.72f, 0.32f); // aged gold (#d9b852)
            }
        }

        private void HandleClicked()
        {
            onSelected?.Invoke();
        }
    }
}
