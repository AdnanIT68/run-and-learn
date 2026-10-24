using System.Collections.Generic;
using FootballRunner.Learning;
using TMPro;
using UnityEngine;

namespace FootballRunner.UI
{
    /// <summary>
    /// The scenes were built with English labels (SCORE, PAUSED, SETTINGS...). This
    /// swaps them for the language the child already speaks when a scene starts;
    /// labels built in code pick their language themselves through LearningProgress.Ui.
    /// </summary>
    public static class SceneTextLocalizer
    {
        private static readonly Dictionary<string, (string bs, string de)> Texts =
            new Dictionary<string, (string bs, string de)>
            {
                { "SCORE", ("BODOVI", "PUNKTE") },
                { "COINS", ("NOVČIĆI", "MÜNZEN") },
                { "BEST", ("REKORD", "REKORD") },
                { "NEW BEST!", ("NOVI REKORD!", "NEUER REKORD!") },
                { "GAME OVER", ("KRAJ IGRE", "SPIEL VORBEI") },
                { "PAUSED", ("PAUZA", "PAUSE") },
                { "RESUME", ("NASTAVI", "WEITER") },
                { "RESTART", ("PONOVO", "NOCHMAL") },
                { "MAIN MENU", ("MENI", "MENÜ") },
                { "SETTINGS", ("POSTAVKE", "EINSTELLUNGEN") },
                { "SOUND", ("ZVUK", "TON") },
                { "VIBRATION", ("VIBRACIJA", "VIBRATION") },
                { "CLOSE", ("ZATVORI", "SCHLIESSEN") },
                { "EXIT", ("IZLAZ", "BEENDEN") },
                { "PLAY", ("IGRAJ", "SPIELEN") }
            };

        public static void Apply(Transform root)
        {
            if (root == null || LearningProgress.HomeLanguage == LearningProgress.English)
            {
                return;
            }

            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                string original = text.text != null ? text.text.Trim() : string.Empty;

                if (!Texts.TryGetValue(original, out (string bs, string de) words))
                {
                    continue;
                }

                string translated = LearningProgress.Ui(words.bs, original, words.de);

                // Longer words shrink to fit the label they were not drawn for.
                if (translated.Length > original.Length && !text.enableAutoSizing)
                {
                    text.fontSizeMax = text.fontSize;
                    text.fontSizeMin = text.fontSize * 0.5f;
                    text.enableAutoSizing = true;
                }

                text.text = translated;
            }
        }
    }
}
