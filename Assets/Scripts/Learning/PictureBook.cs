using System;
using System.Collections.Generic;
using UnityEngine;

namespace FootballRunner.Learning
{
    /// <summary>
    /// A world's picture book: four short narrated pages with the world's words, read in
    /// the language being learned (Resources/Books/&lt;Pack&gt;, from Tools/generate_books.py).
    /// </summary>
    public class PictureBook
    {
        [Serializable]
        private class PageData
        {
            public string[] words;
            public string bs;
            public string en;
            public string de;
            public string ar;
            public string ar_shown;
            public string ar_latin;
        }

        [Serializable]
        private class BookData
        {
            public string pack;
            public PageData[] pages;
        }

        public class Page
        {
            /// <summary>The world's words drawn on the page; tapping one says it.</summary>
            public IReadOnlyList<string> Words;

            /// <summary>The page's sentences in every language, shown like a word card.</summary>
            public WordEntry Text;
        }

        private const string Root = "Books/";
        private static readonly Dictionary<string, PictureBook> Cache = new Dictionary<string, PictureBook>();

        public string Pack { get; private set; }
        public IReadOnlyList<Page> Pages { get; private set; }

        /// <summary>The book of a world, or null if it has none.</summary>
        public static PictureBook Load(string pack)
        {
            if (string.IsNullOrEmpty(pack))
            {
                return null;
            }

            if (Cache.TryGetValue(pack, out PictureBook cached))
            {
                return cached;
            }

            var asset = Resources.Load<TextAsset>(Root + pack + "/book");
            PictureBook book = null;

            if (asset != null)
            {
                BookData data = JsonUtility.FromJson<BookData>(asset.text);
                var pages = new List<Page>();

                foreach (PageData page in data.pages ?? Array.Empty<PageData>())
                {
                    pages.Add(new Page
                    {
                        Words = page.words ?? Array.Empty<string>(),
                        Text = new WordEntry
                        {
                            id = "page", bs = page.bs, en = page.en, de = page.de, ar = page.ar, ar_shown = page.ar_shown,
                            ar_latin = page.ar_latin
                        }
                    });
                }

                book = new PictureBook { Pack = pack, Pages = pages };
            }

            Cache[pack] = book;
            return book;
        }

        /// <summary>A page read aloud in a language (page numbers start at 0).</summary>
        public AudioClip Voice(string language, int page)
        {
            return Resources.Load<AudioClip>($"{Root}{Pack}/Audio/{language}/page_{page + 1}");
        }
    }
}
