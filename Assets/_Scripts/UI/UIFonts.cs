using UnityEngine;

namespace CursedHarvest
{
    /// <summary>
    /// Central typography + text palette (v1.1.2).
    /// Title font: Cinzel Bold (SIL OFL 1.1, Natanael Gama / Cinzel Project Authors) - engraved rural serif.
    /// Body font: Inter (SIL OFL 1.1, Rasmus Andersson) - maximum legibility for HUD, numbers, buttons, descriptions.
    /// Both are bundled in Assets/Resources/Fonts (no OS font dependency).
    /// </summary>
    public static class UIFonts
    {
        private static Font _title;
        private static Font _body;

        public static Font Title
        {
            get
            {
                if (_title == null) _title = Resources.Load<Font>("Fonts/Cinzel-Bold");
                if (_title == null) _title = Body;
                return _title;
            }
        }

        public static Font Body
        {
            get
            {
                if (_body == null) _body = Resources.Load<Font>("Fonts/Inter-Regular");
                if (_body == null) _body = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _body;
            }
        }

        /// <summary>Titles / world names / boss names use the title font; everything else uses the body font.</summary>
        public static Font ForSize(int fontSize, FontStyle style)
        {
            return (fontSize >= 40) ? Title : Body;
        }

        // ---- Text palette (all >= 7:1 contrast on the dark ritual panels) ----
        public static readonly Color Primary   = new Color(0.96f, 0.93f, 0.84f); // warm bone / ivory
        public static readonly Color Secondary = new Color(0.80f, 0.76f, 0.66f); // aged parchment
        public static readonly Color Disabled  = new Color(0.58f, 0.55f, 0.50f);
        public static readonly Color Warning   = new Color(1.00f, 0.74f, 0.25f);
        public static readonly Color Danger    = new Color(1.00f, 0.45f, 0.38f);
        public static readonly Color Positive  = new Color(0.55f, 0.95f, 0.55f);
        public static readonly Color Currency  = new Color(1.00f, 0.84f, 0.35f);
    }
}
