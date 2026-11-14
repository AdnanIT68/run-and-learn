using System;
using System.Collections.Generic;
using UnityEngine;

namespace FootballRunner.Learning
{
    /// <summary>One word in every language. The id names its picture and voice files.</summary>
    [Serializable]
    public class WordEntry
    {
        public string id;
        public string bs;
        public string en;

        [Tooltip("German, nouns with their article (der Apfel).")]
        public string de;

        [Tooltip("Arabic in reading order, with harakat; used for voices and first letters.")]
        public string ar;

        [Tooltip("Arabic as drawn: joined letter forms, already right to left, made by Tools/generate_words.py.")]
        public string ar_shown;

        [Tooltip("Latin transliteration of the Arabic, shown small under it.")]
        public string ar_latin;

        [Tooltip("Counting phrases only: the word whose picture is shown, e.g. \"hen\".")]
        public string picture;

        [Tooltip("Counting phrases only: how many pictures the bubble shows.")]
        public int count;

        [Tooltip("Spoken commands only: jump, stay or double. The child answers by acting.")]
        public string action;

        [Tooltip("Size phrases only: big or small. The bubble draws the picture at that size.")]
        public string size;

        [Tooltip("Sentences only: entries of one group are each other's wrong answers.")]
        public string group;

        [Tooltip("Letters only: the letter shown, for example Š or Lj.")]
        public string letter;

        [Tooltip("Letters only: the language whose alphabet and voice this letter belongs to.")]
        public string lang;

        /// <summary>The picture file to show: the word itself, or the counted word.</summary>
        public string PictureId => string.IsNullOrEmpty(picture) ? id : picture;

        public int Count => Mathf.Max(1, count);

        public bool IsCounting => count > 0;

        public bool IsCommand => !string.IsNullOrEmpty(action);

        public bool IsSize => !string.IsNullOrEmpty(size);

        public bool IsSentence => !string.IsNullOrEmpty(group);

        public bool IsLetter => !string.IsNullOrEmpty(letter);

        /// <summary>
        /// First letter as children learn it. Bosnian digraphs (dž, lj, nj) count as
        /// one letter, and a German noun's article is skipped (der Apfel: A); matches
        /// Tools/generate_words.py.
        /// </summary>
        public static string Initial(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                return string.Empty;
            }

            if (IsArabicText(word))
            {
                return ArabicInitial(word);
            }

            string lower = word.ToLowerInvariant();

            foreach (string article in new[] { "der ", "die ", "das " })
            {
                if (lower.StartsWith(article) && lower.Length > article.Length)
                {
                    lower = lower.Substring(article.Length);
                    break;
                }
            }

            foreach (string digraph in new[] { "dž", "lj", "nj" })
            {
                if (lower.StartsWith(digraph))
                {
                    return char.ToUpperInvariant(digraph[0]) + digraph.Substring(1);
                }
            }

            return char.ToUpperInvariant(lower[0]).ToString();
        }

        public static bool IsArabicText(string text)
        {
            foreach (char c in text)
            {
                if (c >= '؀' && c <= 'ۿ')
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The first Arabic letter, skipping harakat and tatweel; alif with hamza or
        /// madda counts as alif. Matches initial() in Tools/generate_words.py.
        /// </summary>
        private static string ArabicInitial(string word)
        {
            foreach (char c in word)
            {
                bool mark = (c >= 'ً' && c <= 'ٟ') || c == 'ٰ' || c == 'ـ';

                if (mark || char.IsWhiteSpace(c))
                {
                    continue;
                }

                return c == 'أ' || c == 'إ' || c == 'آ' || c == 'ٱ' ? "ا" : c.ToString();
            }

            return string.Empty;
        }

        /// <summary>The word in reading order: what is spoken and what first letters come from.</summary>
        public string Text(string language)
        {
            switch (language)
            {
                case LearningProgress.English: return en;
                case LearningProgress.German: return string.IsNullOrEmpty(de) ? en : de;
                case LearningProgress.Arabic: return string.IsNullOrEmpty(ar) ? bs : ar;
                default: return bs;
            }
        }

        /// <summary>
        /// The word as drawn on screen. TextMeshPro neither joins Arabic letters nor
        /// runs right to left, so Arabic uses the pre-shaped form from the generator.
        /// </summary>
        public string Display(string language)
        {
            if (language == LearningProgress.Arabic && !string.IsNullOrEmpty(ar_shown))
            {
                return ar_shown;
            }

            return Text(language);
        }

        /// <summary>The Latin transliteration under an Arabic word, or null for other languages.</summary>
        public string Latin(string language)
        {
            return language == LearningProgress.Arabic && !string.IsNullOrEmpty(ar_latin) ? ar_latin : null;
        }

        /// <summary>
        /// What a word card shows: the word, and for Arabic the transliteration small
        /// underneath when the parents corner switch is on.
        /// </summary>
        public string CardText(string language, bool showLatin)
        {
            string shown = Display(language);
            string latin = Latin(language);
            return showLatin && latin != null ? $"{shown}\n<size=45%>{latin}</size>" : shown;
        }
    }

    /// <summary>
    /// A themed set of words loaded from Resources/Words/&lt;Pack&gt;. Content is
    /// produced by Tools/generate_words.py, so adding words never needs code.
    /// </summary>
    public class WordPack
    {
        public const string DefaultPack = "World1";
        private const string Root = "Words";

        /// <summary>Worlds in play order. Each one unlocks from progress in the one before.</summary>
        public static readonly string[] PackOrder =
            { "World1", "World2", "World3", "World4", "World5", "World6", "World7", "World8" };

        /// <summary>One picture standing for each world on the world chooser.</summary>
        public static string CoverWordId(string pack)
        {
            switch (pack)
            {
                case "World2": return "cow";
                case "World3": return "house";
                case "World4": return "mosque";
                case "World5": return "bridge";
                case "World6": return "snowman";
                case "World7": return "school";
                case "World8": return "moon";
                default: return "apple";
            }
        }

        [Serializable]
        private class Manifest
        {
            public string pack;
            public string title_bs;
            public string title_en;
            public string title_de;
            public WordEntry[] words;
            public WordEntry[] phrases;
            public WordEntry[] counts;
            public WordEntry[] commands;
            public WordEntry[] sizes;
            public WordEntry[] sentences;
            public WordEntry[] numbers;
            public WordEntry[] letters;
            public bool reading;
            public bool review;
            public int goal;
        }

        private static readonly Dictionary<string, WordPack> Cache = new Dictionary<string, WordPack>();
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        private readonly Manifest _manifest;

        public string Name { get; }

        public IReadOnlyList<WordEntry> Words => _manifest.words ?? Array.Empty<WordEntry>();

        public IReadOnlyList<WordEntry> Phrases => _manifest.phrases ?? Array.Empty<WordEntry>();

        /// <summary>Number-and-word phrases ("tri kokoši"). Empty in worlds that do not count yet.</summary>
        public IReadOnlyList<WordEntry> Counts => Merged(m => m.counts);

        /// <summary>Spoken commands ("Skoči!"). Empty in worlds that do not use them yet.</summary>
        public IReadOnlyList<WordEntry> Commands => Merged(m => m.commands);

        /// <summary>Adjective phrases ("velika riba"). Empty in worlds that do not use them yet.</summary>
        public IReadOnlyList<WordEntry> Sizes => Merged(m => m.sizes);

        /// <summary>Short sentences ("Pada snijeg.") matched to a picture. Empty until a world uses them.</summary>
        public IReadOnlyList<WordEntry> Sentences => Merged(m => m.sentences);

        /// <summary>Number words ("sedam") caught as big numerals.</summary>
        public IReadOnlyList<WordEntry> Numbers => Merged(m => m.numbers);

        /// <summary>First letters of the words, per language ("Slovo O.").</summary>
        public IReadOnlyList<WordEntry> Letters => Merged(m => m.letters);

        /// <summary>True for worlds that also ask silent, read-only words.</summary>
        public bool HasReading => _manifest.reading || IsReview;

        /// <summary>
        /// The finale: besides its own words it asks everything from the worlds before
        /// it, and borrows their pictures and voices.
        /// </summary>
        public bool IsReview => _manifest.review;

        /// <summary>Words to catch to finish this world's level; 0 keeps the usual five.</summary>
        public int GoalSize => _manifest.goal;

        /// <summary>
        /// Words a run asks: the world's own, and for the finale also the words the
        /// child already knows from earlier worlds (all of them if few are known yet).
        /// </summary>
        public IReadOnlyList<WordEntry> ChallengeWords
        {
            get
            {
                if (!IsReview)
                {
                    return Words;
                }

                var known = new List<WordEntry>(Words);
                var all = new List<WordEntry>();

                foreach (WordPack earlier in EarlierPacks())
                {
                    foreach (WordEntry word in earlier.Words)
                    {
                        all.Add(word);

                        if (LearningProgress.IsLearned(word.id))
                        {
                            known.Add(word);
                        }
                    }
                }

                if (known.Count < Words.Count * 2)
                {
                    foreach (WordEntry word in all)
                    {
                        if (!known.Contains(word))
                        {
                            known.Add(word);
                        }
                    }
                }

                return known;
            }
        }

        private Dictionary<string, IReadOnlyList<WordEntry>> _merged;

        private IReadOnlyList<WordEntry> Merged(Func<Manifest, WordEntry[]> select)
        {
            WordEntry[] own = select(_manifest) ?? Array.Empty<WordEntry>();

            if (!IsReview)
            {
                return own;
            }

            _merged ??= new Dictionary<string, IReadOnlyList<WordEntry>>();
            string key = select.Method.Name;

            if (_merged.TryGetValue(key, out IReadOnlyList<WordEntry> cached))
            {
                return cached;
            }

            var list = new List<WordEntry>(own);

            foreach (WordPack earlier in EarlierPacks())
            {
                list.AddRange(select(earlier._manifest) ?? Array.Empty<WordEntry>());
            }

            _merged[key] = list;
            return list;
        }

        /// <summary>Every world before this one, in play order.</summary>
        private IEnumerable<WordPack> EarlierPacks()
        {
            foreach (string name in PackOrder)
            {
                if (name == Name)
                {
                    yield break;
                }

                WordPack pack = Load(name);

                if (pack != null)
                {
                    yield return pack;
                }
            }
        }

        private WordPack(string name, Manifest manifest)
        {
            Name = name;
            _manifest = manifest;
        }

        public string Title(string language)
        {
            switch (language)
            {
                case LearningProgress.English: return _manifest.title_en;
                case LearningProgress.German:
                    return string.IsNullOrEmpty(_manifest.title_de) ? _manifest.title_en : _manifest.title_de;
                default: return _manifest.title_bs;
            }
        }

        public static WordPack Load(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (Cache.TryGetValue(name, out WordPack cached))
            {
                return cached;
            }

            var json = Resources.Load<TextAsset>($"{Root}/{name}/pack");

            if (json == null)
            {
                Debug.LogWarning($"[WordPack] Resources/{Root}/{name}/pack.json not found.");
                return null;
            }

            Manifest manifest = JsonUtility.FromJson<Manifest>(json.text);

            if (manifest == null || manifest.words == null)
            {
                Debug.LogWarning($"[WordPack] {name} has no words.");
                return null;
            }

            var pack = new WordPack(name, manifest);
            Cache[name] = pack;
            return pack;
        }

        /// <summary>
        /// The world's own picture, a shared icon of that name (commands use "star"),
        /// or for the finale the picture from the earlier world it belongs to.
        /// </summary>
        public Sprite Picture(string wordId)
        {
            Sprite sprite = LoadSprite($"{Root}/{Name}/Pictures/{wordId}") ?? Icon(wordId);

            if (sprite == null && IsReview)
            {
                foreach (WordPack earlier in EarlierPacks())
                {
                    sprite = earlier.Picture(wordId);

                    if (sprite != null)
                    {
                        break;
                    }
                }
            }

            return sprite;
        }

        public AudioClip Voice(string language, string entryId)
        {
            var clip = Resources.Load<AudioClip>($"{Root}/{Name}/Audio/{language}/{entryId}");

            if (clip == null && IsReview)
            {
                foreach (WordPack earlier in EarlierPacks())
                {
                    clip = earlier.Voice(language, entryId);

                    if (clip != null)
                    {
                        break;
                    }
                }
            }

            return clip;
        }

        /// <summary>A random praise line ("Bravo!", "Super!"). Phrase ids starting with "praise" qualify.</summary>
        /// <summary>The same word said slowly (Audio/&lt;lang&gt;/slow), or null when there is none.</summary>
        public AudioClip SlowVoice(string language, string entryId)
        {
            var clip = Resources.Load<AudioClip>($"{Root}/{Name}/Audio/{language}/slow/{entryId}");

            if (clip == null && IsReview)
            {
                foreach (WordPack earlier in EarlierPacks())
                {
                    clip = earlier.SlowVoice(language, entryId);

                    if (clip != null)
                    {
                        break;
                    }
                }
            }

            return clip;
        }

        public AudioClip RandomPraise(string language)
        {
            int count = 0;
            WordEntry chosen = null;

            // Reservoir pick, so no temporary list is allocated per catch.
            for (int i = 0; i < Phrases.Count; i++)
            {
                if (Phrases[i].id.StartsWith("praise") && UnityEngine.Random.Range(0, ++count) == 0)
                {
                    chosen = Phrases[i];
                }
            }

            return chosen != null ? Voice(language, chosen.id) : null;
        }

        public static Sprite Icon(string name)
        {
            return LoadSprite($"{Root}/Icons/{name}");
        }

        /// <summary>
        /// Falls back to building a sprite from the texture, so a picture still
        /// shows if its import type was left as Default instead of Sprite.
        /// </summary>
        private static Sprite LoadSprite(string path)
        {
            if (SpriteCache.TryGetValue(path, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite sprite = Resources.Load<Sprite>(path);

            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>(path);

                if (texture != null)
                {
                    sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), texture.width);
                }
            }

            if (sprite != null)
            {
                SpriteCache[path] = sprite;
            }

            return sprite;
        }
    }
}
