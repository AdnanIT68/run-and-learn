using System;
using System.Collections.Generic;
using UnityEngine;

namespace FootballRunner.Learning
{
    /// <summary>One word in both languages. The id names its picture and voice files.</summary>
    [Serializable]
    public class WordEntry
    {
        public string id;
        public string bs;
        public string en;

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

        /// <summary>The picture file to show: the word itself, or the counted word.</summary>
        public string PictureId => string.IsNullOrEmpty(picture) ? id : picture;

        public int Count => Mathf.Max(1, count);

        public bool IsCounting => count > 0;

        public bool IsCommand => !string.IsNullOrEmpty(action);

        public bool IsSize => !string.IsNullOrEmpty(size);

        public bool IsSentence => !string.IsNullOrEmpty(group);

        public string Text(string language)
        {
            return language == LearningProgress.English ? en : bs;
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
        public static readonly string[] PackOrder = { "World1", "World2", "World3", "World4", "World5", "World6" };

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
                default: return "apple";
            }
        }

        [Serializable]
        private class Manifest
        {
            public string pack;
            public string title_bs;
            public string title_en;
            public WordEntry[] words;
            public WordEntry[] phrases;
            public WordEntry[] counts;
            public WordEntry[] commands;
            public WordEntry[] sizes;
            public WordEntry[] sentences;
        }

        private static readonly Dictionary<string, WordPack> Cache = new Dictionary<string, WordPack>();
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        private readonly Manifest _manifest;

        public string Name { get; }

        public IReadOnlyList<WordEntry> Words => _manifest.words ?? Array.Empty<WordEntry>();

        public IReadOnlyList<WordEntry> Phrases => _manifest.phrases ?? Array.Empty<WordEntry>();

        /// <summary>Number-and-word phrases ("tri kokoši"). Empty in worlds that do not count yet.</summary>
        public IReadOnlyList<WordEntry> Counts => _manifest.counts ?? Array.Empty<WordEntry>();

        /// <summary>Spoken commands ("Skoči!"). Empty in worlds that do not use them yet.</summary>
        public IReadOnlyList<WordEntry> Commands => _manifest.commands ?? Array.Empty<WordEntry>();

        /// <summary>Adjective phrases ("velika riba"). Empty in worlds that do not use them yet.</summary>
        public IReadOnlyList<WordEntry> Sizes => _manifest.sizes ?? Array.Empty<WordEntry>();

        /// <summary>Short sentences ("Pada snijeg.") matched to a picture. Empty until a world uses them.</summary>
        public IReadOnlyList<WordEntry> Sentences => _manifest.sentences ?? Array.Empty<WordEntry>();

        private WordPack(string name, Manifest manifest)
        {
            Name = name;
            _manifest = manifest;
        }

        public string Title(string language)
        {
            return language == LearningProgress.English ? _manifest.title_en : _manifest.title_bs;
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

        /// <summary>The world's own picture, or a shared icon of that name (commands use "star").</summary>
        public Sprite Picture(string wordId)
        {
            return LoadSprite($"{Root}/{Name}/Pictures/{wordId}") ?? Icon(wordId);
        }

        public AudioClip Voice(string language, string entryId)
        {
            return Resources.Load<AudioClip>($"{Root}/{Name}/Audio/{language}/{entryId}");
        }

        /// <summary>A random praise line ("Bravo!", "Super!"). Phrase ids starting with "praise" qualify.</summary>
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
