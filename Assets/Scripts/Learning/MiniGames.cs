using System.Collections.Generic;
using UnityEngine;

namespace FootballRunner.Learning
{
    /// <summary>
    /// The rules of the short, optional games between levels, kept apart from their
    /// screens so they can be tested: Parovi (match a picture to its word), Šta čuješ?
    /// (tap the written word the voice says) and Složi riječ (put the letters in order).
    /// They use the world's words, the child's learned ones first.
    /// </summary>
    public static class MiniGames
    {
        /// <summary>Coins for finishing a mini-game; small, so the run stays the main way to earn.</summary>
        public const int Reward = 3;

        public enum Kind
        {
            Memory,
            Hear,
            Build,
            Trace
        }

        /// <summary>Spelling a word letter by letter needs separate letters; joined Arabic letters change shape.</summary>
        public static bool IsAvailable(Kind kind, string language)
        {
            return kind != Kind.Build || language != LearningProgress.Arabic;
        }

        /// <summary><paramref name="count"/> different words of the world, learned ones first, in random order.</summary>
        public static List<WordEntry> PickWords(WordPack pack, int count)
        {
            var learned = new List<WordEntry>();
            var others = new List<WordEntry>();

            foreach (WordEntry word in pack.Words)
            {
                (LearningProgress.IsLearned(word.id) ? learned : others).Add(word);
            }

            Shuffle(learned);
            Shuffle(others);
            learned.AddRange(others);

            if (learned.Count > count)
            {
                learned.RemoveRange(count, learned.Count - count);
            }

            Shuffle(learned);
            return learned;
        }

        public static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// The letters children write for a word: Bosnian dž, lj and nj are one letter, a
        /// German noun's article is left out (der Apfel: A, p, f, e, l). Lower case.
        /// </summary>
        public static List<string> Letters(string word)
        {
            var letters = new List<string>();

            if (string.IsNullOrEmpty(word))
            {
                return letters;
            }

            string text = word.ToLowerInvariant();

            foreach (string article in new[] { "der ", "die ", "das " })
            {
                if (text.StartsWith(article))
                {
                    text = text.Substring(article.Length);
                    break;
                }
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    continue;
                }

                if (i + 1 < text.Length)
                {
                    string pair = text.Substring(i, 2);

                    if (pair == "dž" || pair == "lj" || pair == "nj")
                    {
                        letters.Add(pair);
                        i++;
                        continue;
                    }
                }

                letters.Add(text[i].ToString());
            }

            return letters;
        }
    }

    /// <summary>Parovi: picture cards and word cards face down; open two that belong together.</summary>
    public class MemoryGame
    {
        public enum Result
        {
            Ignored,
            FirstOpened,
            Match,
            NoMatch
        }

        public class Card
        {
            public WordEntry Word;
            public bool IsPicture;
            public bool Open;
            public bool Matched;
        }

        public readonly List<Card> Cards = new List<Card>();

        private int _first = -1;
        private int _second = -1;

        public bool IsDone => Cards.TrueForAll(c => c.Matched);

        /// <summary>True while two cards that do not belong together are open, waiting to be turned back.</summary>
        public bool WaitingToTurnBack => _second >= 0;

        public MemoryGame(IList<WordEntry> words)
        {
            foreach (WordEntry word in words)
            {
                Cards.Add(new Card { Word = word, IsPicture = true });
                Cards.Add(new Card { Word = word, IsPicture = false });
            }

            MiniGames.Shuffle(Cards);
        }

        public Result Open(int index)
        {
            if (index < 0 || index >= Cards.Count || WaitingToTurnBack || Cards[index].Open || Cards[index].Matched)
            {
                return Result.Ignored;
            }

            Cards[index].Open = true;

            if (_first < 0)
            {
                _first = index;
                return Result.FirstOpened;
            }

            Card a = Cards[_first];
            Card b = Cards[index];

            if (a.Word == b.Word && a.IsPicture != b.IsPicture)
            {
                a.Matched = b.Matched = true;
                _first = -1;
                return Result.Match;
            }

            _second = index;
            return Result.NoMatch;
        }

        /// <summary>Turns a wrong pair face down again.</summary>
        public void TurnBack()
        {
            if (!WaitingToTurnBack)
            {
                return;
            }

            Cards[_first].Open = false;
            Cards[_second].Open = false;
            _first = _second = -1;
        }
    }

    /// <summary>Šta čuješ?: the voice says a word; the child taps it among written words.</summary>
    public class HearGame
    {
        public const int Rounds = 5;
        public const int Options = 3;

        private readonly List<WordEntry> _pool;

        public int Round { get; private set; }
        public int Right { get; private set; }
        public WordEntry Target { get; private set; }
        public readonly List<WordEntry> Choices = new List<WordEntry>();

        public bool IsDone => Round >= Rounds;

        public HearGame(IList<WordEntry> pool)
        {
            _pool = new List<WordEntry>(pool);
            NextRound();
        }

        /// <summary>True for the right word; then the next round starts. A wrong tap may be tried again.</summary>
        public bool Answer(int index)
        {
            if (IsDone || index < 0 || index >= Choices.Count)
            {
                return false;
            }

            if (Choices[index] != Target)
            {
                return false;
            }

            Right++;
            Round++;

            if (!IsDone)
            {
                NextRound();
            }

            return true;
        }

        private void NextRound()
        {
            WordEntry previous = Target;
            var candidates = _pool.FindAll(w => w != previous);
            Target = candidates[Random.Range(0, candidates.Count)];

            Choices.Clear();
            Choices.Add(Target);
            var others = _pool.FindAll(w => w != Target);
            MiniGames.Shuffle(others);

            for (int i = 0; i < others.Count && Choices.Count < Options; i++)
            {
                Choices.Add(others[i]);
            }

            MiniGames.Shuffle(Choices);
        }
    }

    /// <summary>Složi riječ: the letters of a word, mixed up; tap them in the right order.</summary>
    public class BuildWordGame
    {
        public WordEntry Word { get; }
        public List<string> Letters { get; }

        /// <summary>The mixed-up letter tiles; a used tile becomes null.</summary>
        public readonly List<string> Tiles;

        public int Placed { get; private set; }
        public bool IsDone => Placed >= Letters.Count;

        public BuildWordGame(WordEntry word, string language)
        {
            Word = word;
            Letters = MiniGames.Letters(word.Text(language));
            Tiles = new List<string>(Letters);

            // Mix until the order differs, when the word has more than one kind of letter.
            for (int tries = 0; tries < 10; tries++)
            {
                MiniGames.Shuffle(Tiles);

                if (string.Join("", Tiles) != string.Join("", Letters))
                {
                    break;
                }
            }
        }

        /// <summary>A word fit for spelling: one word of 3 to 7 letters.</summary>
        public static bool Suits(WordEntry word, string language)
        {
            List<string> letters = MiniGames.Letters(word.Text(language));
            string text = word.Text(language) ?? "";
            int spaces = text.Split(' ').Length - 1;
            bool article = text.StartsWith("der ") || text.StartsWith("die ") || text.StartsWith("das ");
            return letters.Count >= 3 && letters.Count <= 7 && spaces == (article ? 1 : 0);
        }

        /// <summary>True when the tile holds the next letter; it is then placed.</summary>
        public bool Tap(int tileIndex)
        {
            if (IsDone || tileIndex < 0 || tileIndex >= Tiles.Count || Tiles[tileIndex] == null)
            {
                return false;
            }

            if (Tiles[tileIndex] != Letters[Placed])
            {
                return false;
            }

            Tiles[tileIndex] = null;
            Placed++;
            return true;
        }
    }
}
