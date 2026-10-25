using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CursedHarvest
{
    /// <summary>
    /// Master UI Styling & Asset Repository for CURSED HARVEST. Folk-Horror Aesthetic.
    /// Provides 9-sliced authentic dark bloodwood, blackened iron, embossed gold and danger buttons,
    /// consecrated progress bars, and runic dividers with zero runtime per-frame overhead.
    /// </summary>
    public static class FolkHorrorUITheme
    {
        // ==================== AUTHENTIC FOLK-HORROR COLOR PALETTE ====================
        public static readonly Color ColorBgCharcoal       = new Color(0.07f, 0.08f, 0.07f, 0.96f); // #121512
        public static readonly Color ColorPanelWood        = new Color(0.11f, 0.13f, 0.11f, 0.98f); // #1B211B
        public static readonly Color ColorPanelIron        = new Color(0.14f, 0.17f, 0.14f, 0.98f); // #242B24
        public static readonly Color ColorCardElevated     = new Color(0.17f, 0.20f, 0.17f, 0.98f); // #2B332B

        public static readonly Color ColorTextCream        = new Color(0.94f, 0.91f, 0.84f, 1.00f); // #EFE7D5
        public static readonly Color ColorTextMuted        = new Color(0.72f, 0.70f, 0.64f, 1.00f); // #B8B3A3
        public static readonly Color ColorTextGold         = new Color(0.98f, 0.82f, 0.28f, 1.00f); // #FAD147
        public static readonly Color ColorTextDanger       = new Color(0.95f, 0.28f, 0.24f, 1.00f); // #F2473D
        public static readonly Color ColorTextOccultGreen  = new Color(0.46f, 0.88f, 0.52f, 1.00f); // #75E085

        public static readonly Color ColorGoldAccent       = new Color(0.84f, 0.60f, 0.22f, 1.00f); // #D59A38
        public static readonly Color ColorIronBorder       = new Color(0.24f, 0.28f, 0.25f, 0.95f); // #3D4740
        public static readonly Color ColorCrimsonAccent    = new Color(0.64f, 0.22f, 0.20f, 1.00f); // #A43833

        // Cached Sprite References loaded on-demand
        private static Sprite _panelWood;
        private static Sprite _panelIron;
        private static Sprite _buttonGoldNormal;
        private static Sprite _buttonGoldPressed;
        private static Sprite _buttonDarkNormal;
        private static Sprite _buttonDarkPressed;
        private static Sprite _buttonDangerNormal;
        private static Sprite _buttonDangerPressed;
        private static Sprite _barHpFill;
        private static Sprite _barXpFill;
        private static Sprite _barTrack;
        private static Sprite _divider;
        private static Sprite _ritualRing;

        public static Sprite PanelWood => _panelWood ??= LoadSprite("Panel_Wood_9Slice");
        public static Sprite PanelIron => _panelIron ??= LoadSprite("Panel_Iron_9Slice");
        public static Sprite ButtonGoldNormal => _buttonGoldNormal ??= LoadSprite("Button_Gold_Normal_9Slice");
        public static Sprite ButtonGoldPressed => _buttonGoldPressed ??= LoadSprite("Button_Gold_Pressed_9Slice");
        public static Sprite ButtonDarkNormal => _buttonDarkNormal ??= LoadSprite("Button_Dark_Normal_9Slice");
        public static Sprite ButtonDarkPressed => _buttonDarkPressed ??= LoadSprite("Button_Dark_Pressed_9Slice");
        public static Sprite ButtonDangerNormal => _buttonDangerNormal ??= LoadSprite("Button_Danger_Normal_9Slice");
        public static Sprite ButtonDangerPressed => _buttonDangerPressed ??= LoadSprite("Button_Danger_Pressed_9Slice");
        public static Sprite BarHpFill => _barHpFill ??= LoadSprite("Bar_HP_Fill_9Slice");
        public static Sprite BarXpFill => _barXpFill ??= LoadSprite("Bar_XP_Fill_9Slice");
        public static Sprite BarTrack => _barTrack ??= LoadSprite("Bar_Track_9Slice");
        public static Sprite Divider => _divider ??= LoadSprite("Ornament_Divider_256x16");
        public static Sprite RitualRing => _ritualRing ??= LoadSprite("Ritual_Ring_128x128");

        private static Sprite LoadSprite(string name)
        {
#if UNITY_EDITOR
            string path = $"Assets/_Textures/UI_FolkHorror/{name}.png";
            Sprite s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) return s;
#endif
            return null;
        }

        public static void StylePanel(Image img, bool useWood = true)
        {
            if (img == null) return;
            Sprite s = useWood ? PanelWood : PanelIron;
            if (s != null)
            {
                img.sprite = s;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            else
            {
                img.color = useWood ? ColorPanelWood : ColorPanelIron;
            }
        }

        public static void StyleButton(Button btn, Image img, string style = "gold")
        {
            if (btn == null || img == null) return;

            Sprite normalSprite = null;
            Sprite pressedSprite = null;

            switch (style.ToLowerInvariant())
            {
                case "danger":
                    normalSprite = ButtonDangerNormal;
                    pressedSprite = ButtonDangerPressed;
                    break;
                case "dark":
                    normalSprite = ButtonDarkNormal;
                    pressedSprite = ButtonDarkPressed;
                    break;
                case "gold":
                default:
                    normalSprite = ButtonGoldNormal;
                    pressedSprite = ButtonGoldPressed;
                    break;
            }

            if (normalSprite != null)
            {
                img.sprite = normalSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;

                btn.transition = Selectable.Transition.SpriteSwap;
                SpriteState ss = btn.spriteState;
                ss.highlightedSprite = normalSprite;
                ss.pressedSprite = pressedSprite ?? normalSprite;
                ss.selectedSprite = normalSprite;
                ss.disabledSprite = ButtonDarkNormal ?? normalSprite;
                btn.spriteState = ss;
            }
            else
            {
                // Fallback procedural tinting
                btn.transition = Selectable.Transition.ColorTint;
                Color baseC = style == "danger" ? ColorCrimsonAccent : (style == "dark" ? ColorPanelIron : ColorGoldAccent);
                img.color = baseC;
                ColorBlock cb = btn.colors;
                cb.normalColor = baseC;
                cb.highlightedColor = Color.Lerp(baseC, Color.white, 0.2f);
                cb.pressedColor = Color.Lerp(baseC, Color.black, 0.25f);
                cb.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
                btn.colors = cb;
            }
        }
    }
}
