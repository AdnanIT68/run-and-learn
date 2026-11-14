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
                        AddArabicFallback(_display);
                    }
                }

                return _display;
            }
        }

        private static TMP_FontAsset _arabic;

        /// <summary>
        /// Noto Sans Arabic Bold, built at runtime like Titan One. Titan One has no
        /// Arabic letters, so TextMeshPro takes them from this font instead. The text
        /// itself arrives already joined and right to left (WordEntry.Display).
        /// </summary>
        public static TMP_FontAsset Arabic
        {
            get
            {
                if (_arabic == null)
                {
                    var font = Resources.Load<Font>("Fonts/NotoSansArabic-Bold");

                    if (font != null)
                    {
                        _arabic = TMP_FontAsset.CreateFontAsset(font);
                        _arabic.name = "NotoSansArabic (runtime)";
                    }
                }

                return _arabic;
            }
        }

        /// <summary>Lets <paramref name="asset"/> draw Arabic letters it does not have itself.</summary>
        public static void AddArabicFallback(TMP_FontAsset asset)
        {
            TMP_FontAsset arabic = Arabic;

            if (asset == null || arabic == null || asset == arabic)
            {
                return;
            }

            if (asset.fallbackFontAssetTable == null)
            {
                asset.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
            }

            if (!asset.fallbackFontAssetTable.Contains(arabic))
            {
                asset.fallbackFontAssetTable.Add(arabic);
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
