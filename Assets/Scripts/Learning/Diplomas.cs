using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.Learning
{
    /// <summary>
    /// A diploma for every world the child has finished (all five levels, in the language
    /// being learned): a picture with the child's name, the world, the stars and the date,
    /// drawn off screen and saved as a PNG that a parent can share or print.
    /// </summary>
    public static class Diplomas
    {
        public const int Width = 1600;
        public const int Height = 1131;

        /// <summary>Worlds finished in the current language, in play order.</summary>
        public static List<string> Earned()
        {
            var earned = new List<string>();

            foreach (string pack in WordPack.PackOrder)
            {
                if (LearningProgress.LevelsDone(pack) >= LearningProgress.LevelsPerWorld)
                {
                    earned.Add(pack);
                }
            }

            return earned;
        }

        public static string Folder => Path.Combine(Application.persistentDataPath, "Diplomas");

        /// <summary>The saved picture for a world, e.g. .../Diplomas/Amina_World1_bs.png.</summary>
        public static string FilePath(string pack, string childName)
        {
            string safe = OffscreenPicture.SafeName(childName, "dijete");
            return Path.Combine(Folder, $"{safe}_{pack}_{LearningProgress.Language}.png");
        }

        /// <summary>Draws the diploma off screen. The caller destroys the texture.</summary>
        public static Texture2D Render(string pack, string childName)
        {
            WordPack words = WordPack.Load(pack);
            int stars = LearningProgress.TotalStars(pack);
            string title = words != null ? words.Title(LearningProgress.HomeLanguage) : pack;
            string date = DailyRewards.Clock().ToString("dd. MM. yyyy.");

            return OffscreenPicture.Render(Width, Height, new Color(1f, 0.97f, 0.88f, 1f),
                canvas => Compose(canvas, childName, title, words, stars, date));
        }

        /// <summary>Draws and saves the diploma; returns the file, or null if it could not be written.</summary>
        public static string Save(string pack, string childName)
        {
            return OffscreenPicture.SavePng(Render(pack, childName), FilePath(pack, childName));
        }

        /// <summary>Opens the phone's share sheet with the picture; false where sharing is not possible.</summary>
        public static bool Share(string path)
        {
            return OffscreenPicture.Share(path);
        }

        private static void Compose(Transform canvas, string childName, string title, WordPack words, int stars, string date)
        {
            Image frame = OffscreenPicture.Panel(canvas, Vector2.zero, new Vector2(Width - 60f, Height - 60f),
                new Color(0.98f, 0.78f, 0.25f, 1f));
            OffscreenPicture.Panel(frame.transform, Vector2.zero, new Vector2(Width - 110f, Height - 110f),
                new Color(1f, 0.98f, 0.92f, 1f));

            var grey = new Color(0.4f, 0.44f, 0.5f, 1f);
            OffscreenPicture.Label(canvas, "DIPLOMA", new Vector2(0f, 400f), new Vector2(1200f, 160f), 140f,
                new Color(0.95f, 0.6f, 0.1f, 1f));
            OffscreenPicture.Label(canvas, "Run and Learn", new Vector2(0f, 290f), new Vector2(1000f, 70f), 56f,
                new Color(0.2f, 0.45f, 0.75f, 1f));
            OffscreenPicture.Label(canvas, LearningProgress.Ui("za", "for", "für"), new Vector2(0f, 200f),
                new Vector2(600f, 60f), 46f, grey);
            OffscreenPicture.Label(canvas, childName ?? "", new Vector2(0f, 100f), new Vector2(1200f, 150f), 130f,
                new Color(0.16f, 0.2f, 0.3f, 1f));
            OffscreenPicture.Label(canvas,
                LearningProgress.Ui("za završen svijet", "for finishing the world", "für die geschaffte Welt"),
                new Vector2(0f, -20f), new Vector2(1100f, 60f), 46f, grey);
            OffscreenPicture.Label(canvas, title, new Vector2(0f, -110f), new Vector2(1200f, 110f), 90f,
                new Color(0.27f, 0.6f, 0.3f, 1f));
            int most = LearningProgress.LevelsPerWorld * 3;
            OffscreenPicture.Label(canvas,
                LearningProgress.Ui($"Zvjezdice: {stars} / {most}", $"Stars: {stars} / {most}", $"Sterne: {stars} / {most}"),
                new Vector2(0f, -220f), new Vector2(900f, 70f), 52f, new Color(0.95f, 0.6f, 0.1f, 1f));
            OffscreenPicture.Label(canvas, date, new Vector2(0f, -320f), new Vector2(700f, 60f), 42f, grey);

            OffscreenPicture.Picture(canvas, Resources.Load<Sprite>("Menu/rukajja_wave"), new Vector2(-560f, -180f),
                new Vector2(300f, 420f));
            OffscreenPicture.Picture(canvas, WordPack.Icon("parrot"), new Vector2(580f, -220f), new Vector2(220f, 220f));
            Sprite cover = words != null ? words.Picture(WordPack.CoverWordId(words.Name)) : null;
            OffscreenPicture.Picture(canvas, cover, new Vector2(-580f, 330f), new Vector2(180f, 180f));
            OffscreenPicture.Picture(canvas, WordPack.Icon("star"), new Vector2(580f, 330f), new Vector2(180f, 180f));
        }
    }
}
