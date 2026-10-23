using TMPro;
using UnityEngine;

namespace FootballRunner.Utilities
{
    /// <summary>
    /// The rounded display font (Titan One) shared by the menu, album and world
    /// chooser. Built once at runtime from the TTF in Resources/Fonts.
    /// </summary>
    public static class GameFonts
    {
        private static TMP_FontAsset _display;

        /// <summary>Null if the font file is missing; callers keep their current font then.</summary>
        public static TMP_FontAsset Display
        {
            get
            {
                if (_display == null)
                {
                    var font = Resources.Load<Font>("Fonts/TitanOne-Regular");

                    if (font != null)
                    {
                        _display = TMP_FontAsset.CreateFontAsset(font);
                        _display.name = "TitanOne (runtime)";
                    }
                }

                return _display;
            }
        }

        /// <summary>
        /// Swaps in the display font together with its material. Do this before any
        /// outline or face setting, which copy whatever material is current.
        /// </summary>
        public static void UseDisplay(TMP_Text text)
        {
            TMP_FontAsset display = Display;

            if (text == null || display == null)
            {
                return;
            }

            text.font = display;
            text.fontSharedMaterial = display.material;
        }
    }
}
