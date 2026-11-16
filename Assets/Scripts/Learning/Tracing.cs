using System;
using System.Collections.Generic;
using UnityEngine;

namespace FootballRunner.Learning
{
    /// <summary>
    /// The letters to trace with a finger, from Tools/generate_tracing.py: a pale guide
    /// picture per letter, a 32 x 32 mask of the cells the letter covers, the alphabet of
    /// each language and a voice per letter ("Slovo A.").
    /// </summary>
    public static class TracingLetters
    {
        [Serializable]
        public class Letter
        {
            public string id;
            public string letter;
        }

        [Serializable]
        private class Mask
        {
            public string id;
            public string cells;
        }

        [Serializable]
        private class Alphabet
        {
            public string lang;
            public Letter[] letters;
        }

        [Serializable]
        private class Manifest
        {
            public int grid;
            public Mask[] masks;
            public Alphabet[] alphabets;
        }

        private static Manifest _manifest;

        private static Manifest Data
        {
            get
            {
                if (_manifest == null)
                {
                    var asset = Resources.Load<TextAsset>("Tracing/masks");
                    _manifest = asset != null ? JsonUtility.FromJson<Manifest>(asset.text) : new Manifest();
                }

                return _manifest;
            }
        }

        public static int Grid => Data.grid > 0 ? Data.grid : 32;

        public static IReadOnlyList<Letter> AlphabetOf(string language)
        {
            if (Data.alphabets != null)
            {
                foreach (Alphabet alphabet in Data.alphabets)
                {
                    if (alphabet.lang == language)
                    {
                        return alphabet.letters;
                    }
                }
            }

            return Array.Empty<Letter>();
        }

        /// <summary>True for the cells the letter covers, row by row from the top.</summary>
        public static bool[] MaskOf(string id)
        {
            if (Data.masks != null)
            {
                foreach (Mask mask in Data.masks)
                {
                    if (mask.id == id)
                    {
                        var cells = new bool[mask.cells.Length];

                        for (int i = 0; i < cells.Length; i++)
                        {
                            cells[i] = mask.cells[i] == '1';
                        }

                        return cells;
                    }
                }
            }

            return null;
        }

        public static Sprite Guide(string id)
        {
            return Resources.Load<Sprite>("Tracing/Glyphs/" + id);
        }

        public static AudioClip Voice(string language, string id)
        {
            return Resources.Load<AudioClip>($"Tracing/Audio/{language}/{id}");
        }

        /// <summary>
        /// The letters this world's words start with, in the language being learned (the
        /// letters the child meets while playing), in alphabet order; the first five
        /// letters of the alphabet if none match.
        /// </summary>
        public static List<Letter> ForWorld(WordPack pack, string language)
        {
            IReadOnlyList<Letter> alphabet = AlphabetOf(language);
            var initials = new HashSet<string>();

            if (pack != null)
            {
                foreach (WordEntry word in pack.Words)
                {
                    initials.Add(WordEntry.Initial(word.Text(language)));
                }
            }

            var result = new List<Letter>();

            foreach (Letter letter in alphabet)
            {
                if (initials.Contains(letter.letter))
                {
                    result.Add(letter);
                }
            }

            for (int i = 0; result.Count == 0 && i < alphabet.Count && i < 5; i++)
            {
                result.Add(alphabet[i]);
            }

            return result;
        }
    }

    /// <summary>
    /// One letter being traced: the child's finger marks the letter's cells it passes
    /// over. The letter is done once most of it is covered without too much drawing
    /// outside it. Stroke order is not checked: this is for small children.
    /// </summary>
    public class TracingGame
    {
        /// <summary>How wide the finger draws, in mask cells (radius).</summary>
        public const float BrushCells = 2.2f;

        /// <summary>Share of the letter that must be covered.</summary>
        public const float Needed = 0.72f;

        /// <summary>Share of the drawing that may fall outside the letter.</summary>
        public const float MaxOutside = 0.45f;

        private readonly bool[] _mask;
        private readonly bool[] _covered;
        private readonly int _grid;
        private readonly int _inside;
        private int _coveredCount;
        private int _samples;
        private int _outsideSamples;

        public string LetterId { get; }

        public TracingGame(string letterId, bool[] mask, int grid)
        {
            LetterId = letterId;
            _grid = grid;
            _mask = mask ?? new bool[grid * grid];
            _covered = new bool[_mask.Length];

            foreach (bool cell in _mask)
            {
                _inside += cell ? 1 : 0;
            }
        }

        public float Coverage => _inside == 0 ? 0f : (float)_coveredCount / _inside;

        public float Outside => _samples == 0 ? 0f : (float)_outsideSamples / _samples;

        public bool IsDone => Coverage >= Needed && Outside <= MaxOutside;

        /// <summary>The finger at <paramref name="point"/> (0..1 on both axes, y up, over the guide picture).</summary>
        public void Touch(Vector2 point)
        {
            float column = Mathf.Clamp01(point.x) * _grid;
            float row = (1f - Mathf.Clamp01(point.y)) * _grid;
            _samples++;

            if (!IsInside(Mathf.FloorToInt(column), Mathf.FloorToInt(row)))
            {
                _outsideSamples++;
            }

            int reach = Mathf.CeilToInt(BrushCells);

            for (int y = Mathf.FloorToInt(row) - reach; y <= Mathf.FloorToInt(row) + reach; y++)
            {
                for (int x = Mathf.FloorToInt(column) - reach; x <= Mathf.FloorToInt(column) + reach; x++)
                {
                    float dx = x + 0.5f - column;
                    float dy = y + 0.5f - row;

                    if (dx * dx + dy * dy <= BrushCells * BrushCells && IsInside(x, y) && !_covered[y * _grid + x])
                    {
                        _covered[y * _grid + x] = true;
                        _coveredCount++;
                    }
                }
            }
        }

        /// <summary>Wipes the drawing to start the letter again.</summary>
        public void Clear()
        {
            Array.Clear(_covered, 0, _covered.Length);
            _coveredCount = 0;
            _samples = 0;
            _outsideSamples = 0;
        }

        private bool IsInside(int x, int y)
        {
            return x >= 0 && y >= 0 && x < _grid && y < _grid && _mask[y * _grid + x];
        }
    }
}
