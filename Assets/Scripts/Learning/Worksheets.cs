using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.Learning
{
    /// <summary>
    /// Worksheets to print, made from a world's words in the language being learned:
    /// "Piši slova" (each letter the world's words start with, with a picture, a dark
    /// model and pale copies to trace with a pencil) and "Poveži" (pictures on the left,
    /// their words mixed up on the right, to join with a line). A4 portrait, white
    /// background, little colour, so it prints well at home.
    /// </summary>
    public static class Worksheets
    {
        public enum Kind
        {
            Letters,
            Match
        }

        /// <summary>A4 portrait at 150 dpi.</summary>
        public const int Width = 1240;
        public const int Height = 1754;

        public const int LetterRows = 6;
        public const int MatchPairs = 6;
        public const int CopiesPerRow = 4;

        private static readonly Color Ink = new Color(0.12f, 0.15f, 0.22f, 1f);
        private static readonly Color Grey = new Color(0.55f, 0.58f, 0.64f, 1f);
        private static readonly Color Line = new Color(0.8f, 0.82f, 0.86f, 1f);
        private static readonly Color Accent = new Color(0.25f, 0.5f, 0.85f, 1f);

        public static string Folder => Path.Combine(Application.persistentDataPath, "Worksheets");

        public static string FilePath(string pack, Kind kind)
        {
            return Path.Combine(Folder, $"{pack}_{kind.ToString().ToLowerInvariant()}_{LearningProgress.Language}.png");
        }

        public static string Title(Kind kind)
        {
            return kind == Kind.Letters
                ? LearningProgress.Ui("Piši slova", "Write the letters", "Schreib die Buchstaben")
                : LearningProgress.Ui("Poveži sliku i riječ", "Match the picture and the word", "Verbinde Bild und Wort");
        }

        /// <summary>The letters for the letter sheet, each with a word of the world that starts with it.</summary>
        public static List<(TracingLetters.Letter letter, WordEntry word)> LettersFor(WordPack pack, string language)
        {
            var result = new List<(TracingLetters.Letter, WordEntry)>();

            foreach (TracingLetters.Letter letter in TracingLetters.ForWorld(pack, language))
            {
                WordEntry word = null;

                foreach (WordEntry entry in pack.Words)
                {
                    if (WordEntry.Initial(entry.Text(language)) == letter.letter)
                    {
                        word = entry;
                        break;
                    }
                }

                result.Add((letter, word));

                if (result.Count >= LetterRows)
                {
                    break;
                }
            }

            return result;
        }

        /// <summary>Draws the sheet off screen. The caller destroys the texture.</summary>
        public static Texture2D Render(string pack, Kind kind)
        {
            WordPack words = WordPack.Load(pack);
            string language = LearningProgress.Language;

            return OffscreenPicture.Render(Width, Height, Color.white, canvas =>
            {
                Header(canvas, words, kind);

                if (words == null)
                {
                    return;
                }

                if (kind == Kind.Letters)
                {
                    ComposeLetters(canvas, words, language);
                }
                else
                {
                    ComposeMatch(canvas, words, language);
                }
            });
        }

        /// <summary>Draws and saves the sheet; returns the file, or null if it could not be written.</summary>
        public static string Save(string pack, Kind kind)
        {
            return OffscreenPicture.SavePng(Render(pack, kind), FilePath(pack, kind));
        }

        private static void Header(Transform canvas, WordPack words, Kind kind)
        {
            OffscreenPicture.Bar(canvas, new Vector2(0f, Height / 2f - 18f), new Vector2(Width, 36f), Accent);
            OffscreenPicture.Label(canvas, Title(kind), new Vector2(0f, 730f), new Vector2(1100f, 110f), 84f, Ink);
            string world = words != null ? words.Title(LearningProgress.HomeLanguage) : "";
            OffscreenPicture.Label(canvas, $"Run and Learn  ·  {world}", new Vector2(0f, 645f), new Vector2(1100f, 50f),
                36f, Grey);

            // A line for the child's name.
            var name = OffscreenPicture.Label(canvas, LearningProgress.Ui("Ime:", "Name:", "Name:"),
                new Vector2(-420f, 565f), new Vector2(200f, 60f), 40f, Ink);
            name.alignment = TMPro.TextAlignmentOptions.Left;
            OffscreenPicture.Bar(canvas, new Vector2(60f, 545f), new Vector2(760f, 4f), Grey);
            OffscreenPicture.Picture(canvas, WordPack.Icon("parrot"), new Vector2(510f, 575f), new Vector2(110f, 110f));
        }

        private static void ComposeLetters(Transform canvas, WordPack words, string language)
        {
            List<(TracingLetters.Letter letter, WordEntry word)> rows = LettersFor(words, language);
            const float top = 380f;
            const float step = 205f;

            for (int i = 0; i < rows.Count; i++)
            {
                float y = top - i * step;
                (TracingLetters.Letter letter, WordEntry word) = rows[i];
                Sprite guide = TracingLetters.Guide(letter.id);

                if (word != null)
                {
                    OffscreenPicture.Picture(canvas, words.Picture(word.PictureId), new Vector2(-510f, y),
                        new Vector2(130f, 130f));
                }

                // The model in dark ink, then pale copies to trace.
                Image model = OffscreenPicture.Picture(canvas, guide, new Vector2(-330f, y), new Vector2(180f, 180f));
                if (model != null)
                {
                    model.color = new Color(0.25f, 0.3f, 0.45f, 1f);
                }

                for (int c = 0; c < CopiesPerRow; c++)
                {
                    OffscreenPicture.Picture(canvas, guide, new Vector2(-130f + c * 190f, y), new Vector2(180f, 180f));
                }

                OffscreenPicture.Bar(canvas, new Vector2(0f, y - 100f), new Vector2(Width - 120f, 3f), Line);
            }
        }

        private static void ComposeMatch(Transform canvas, WordPack words, string language)
        {
            List<WordEntry> pairs = MiniGames.PickWords(words, MatchPairs);
            var mixed = new List<WordEntry>(pairs);

            // Mixed so that no word sits next to its own picture.
            for (int tries = 0; tries < 20; tries++)
            {
                MiniGames.Shuffle(mixed);
                bool sameRow = false;

                for (int i = 0; i < mixed.Count; i++)
                {
                    sameRow |= mixed[i] == pairs[i];
                }

                if (!sameRow || pairs.Count < 2)
                {
                    break;
                }
            }

            const float top = 360f;
            const float step = 205f;
            bool latin = LearningProgress.ShowTransliteration;

            for (int i = 0; i < pairs.Count; i++)
            {
                float y = top - i * step;
                OffscreenPicture.Panel(canvas, new Vector2(-370f, y), new Vector2(180f, 180f),
                    new Color(0.95f, 0.96f, 0.98f, 1f), 1.2f);
                OffscreenPicture.Picture(canvas, words.Picture(pairs[i].PictureId), new Vector2(-370f, y),
                    new Vector2(150f, 150f));
                Dot(canvas, new Vector2(-230f, y));

                Dot(canvas, new Vector2(130f, y));
                var word = OffscreenPicture.Label(canvas, mixed[i].CardText(language, latin, 50), new Vector2(370f, y),
                    new Vector2(400f, 170f), 64f, Ink);
                word.alignment = TMPro.TextAlignmentOptions.Center;
            }
        }

        private static void Dot(Transform canvas, Vector2 position)
        {
            Image dot = OffscreenPicture.Picture(canvas, Utilities.RuntimeSprites.Circle(), position, new Vector2(30f, 30f));
            if (dot != null)
            {
                dot.color = Ink;
                dot.preserveAspect = false;
            }
        }
    }
}
