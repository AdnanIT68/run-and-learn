using System.Collections;
using System.Collections.Generic;
using FootballRunner.Audio;
using FootballRunner.SaveSystem;
using FootballRunner.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.Learning
{
    /// <summary>
    /// The mini-games screen, opened from the level result: a choice of Parovi, Šta čuješ?
    /// and Složi riječ, each short and optional, with the world's words and voices.
    /// Finishing one gives a few coins. Works while the game is frozen (unscaled time).
    /// </summary>
    public class MiniGameUi : MonoBehaviour
    {
        private static readonly Color Dim = new Color(0.08f, 0.16f, 0.3f, 0.97f);
        private static readonly Color CardBack = new Color(0.25f, 0.5f, 0.85f, 1f);
        private static readonly Color CardFace = new Color(1f, 0.98f, 0.92f, 1f);
        private static readonly Color Matched = new Color(0.62f, 0.9f, 0.6f, 1f);
        private static readonly Color Ink = new Color(0.16f, 0.2f, 0.3f, 1f);

        public static MiniGameUi Instance { get; private set; }

        private GameObject _panel;
        private Transform _content;
        private TMP_Text _title;
        private TMP_Text _status;
        private TMP_Text _backLabel;
        private WordPack _pack;

        public MiniGames.Kind? Current { get; private set; }
        public MemoryGame Memory { get; private set; }
        public HearGame Hear { get; private set; }
        public BuildWordGame Build { get; private set; }

        /// <summary>Mini-games finished since this screen was built; for tests.</summary>
        public int Finished { get; private set; }

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public static MiniGameUi EnsureOnCanvas(Transform canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            var ui = canvas.GetComponent<MiniGameUi>();
            return ui != null ? ui : canvas.gameObject.AddComponent<MiniGameUi>();
        }

        private void Awake()
        {
            Instance = this;
            BuildPanel();
            _panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Opens the choice of mini-games for a world.</summary>
        public void Open(WordPack pack)
        {
            _pack = pack;
            _panel.SetActive(true);
            _panel.transform.SetAsLastSibling();
            ShowPicker();
        }

        public void Close()
        {
            AudioManager.Instance?.PlayButton();
            // Nothing may keep talking or building the next round once the screen is gone.
            StopAllCoroutines();
            _panel.SetActive(false);
            Current = null;
        }

        // ------------------------------------------------------------ picker

        private void ShowPicker()
        {
            Clear();
            Current = null;
            _title.text = LearningProgress.Ui("MINI IGRE", "MINI GAMES", "MINISPIELE");
            _status.text = LearningProgress.Ui("Izaberi igru!", "Pick a game!", "Wähle ein Spiel!");
            _backLabel.text = LearningProgress.Ui("NAZAD", "BACK", "ZURÜCK");

            string language = LearningProgress.Language;
            var choices = new List<(MiniGames.Kind kind, string label, string icon)>
            {
                (MiniGames.Kind.Memory, LearningProgress.Ui("PAROVI", "PAIRS", "PAARE"), "album"),
                (MiniGames.Kind.Hear, LearningProgress.Ui("ŠTA ČUJEŠ?", "WHAT DO YOU HEAR?", "WAS HÖRST DU?"), "speaker"),
                (MiniGames.Kind.Build, LearningProgress.Ui("SLOŽI RIJEČ", "BUILD THE WORD", "BAU DAS WORT"), "star"),
                (MiniGames.Kind.Trace, LearningProgress.Ui("PIŠI SLOVO", "WRITE A LETTER", "SCHREIB EINEN BUCHSTABEN"), "tap")
            };
            choices.RemoveAll(c => !MiniGames.IsAvailable(c.kind, language));

            for (int i = 0; i < choices.Count; i++)
            {
                float x = (i - (choices.Count - 1) * 0.5f) * 420f;
                AddChoice(choices[i].kind, choices[i].label, choices[i].icon, new Vector2(x, 0f));
            }
        }

        private void AddChoice(MiniGames.Kind kind, string label, string icon, Vector2 position)
        {
            Image button = Box(_content, "Choose" + kind, position, new Vector2(380f, 400f), CardFace);
            button.gameObject.AddComponent<Button>().onClick.AddListener(() => Play(kind));
            Picture(button.transform, WordPack.Icon(icon), new Vector2(0f, 50f), 200f);
            Text(button.transform, label, new Vector2(0f, -130f), new Vector2(370f, 90f), 42f, Ink);
        }

        /// <summary>Starts a mini-game, as tapping its card does.</summary>
        public void Play(MiniGames.Kind kind)
        {
            AudioManager.Instance?.PlayButton();
            Current = kind;

            switch (kind)
            {
                case MiniGames.Kind.Memory:
                    Memory = new MemoryGame(MiniGames.PickWords(_pack, 4));
                    ShowMemory();
                    break;
                case MiniGames.Kind.Hear:
                    Hear = new HearGame(MiniGames.PickWords(_pack, Mathf.Min(6, _pack.Words.Count)));
                    ShowHear();
                    SayTarget();
                    break;
                case MiniGames.Kind.Trace:
                    // Mixed, so every game brings other letters of the world, not always the first three.
                    _traceLetters = TracingLetters.ForWorld(_pack, LearningProgress.Language);
                    MiniGames.Shuffle(_traceLetters);
                    _traceIndex = 0;
                    TracedLetters = 0;
                    StartLetter();
                    break;
                default:
                    string language = LearningProgress.Language;
                    List<WordEntry> words = MiniGames.PickWords(_pack, _pack.Words.Count)
                        .FindAll(w => BuildWordGame.Suits(w, language));
                    WordEntry word = words.Count > 0 ? words[0] : _pack.Words[0];
                    Build = new BuildWordGame(word, language);
                    ShowBuild();
                    Say(word);
                    break;
            }
        }

        // ------------------------------------------------------------ Parovi

        private readonly List<Image> _memoryCards = new List<Image>();

        private void ShowMemory()
        {
            Clear();
            _title.text = LearningProgress.Ui("PAROVI", "PAIRS", "PAARE");
            _status.text = LearningProgress.Ui("Nađi sliku i njenu riječ!", "Match each picture to its word!",
                "Finde Bild und Wort!");
            _memoryCards.Clear();

            for (int i = 0; i < Memory.Cards.Count; i++)
            {
                int index = i;
                Vector2 position = new Vector2((i % 4 - 1.5f) * 330f, i < 4 ? 140f : -200f);
                Image card = Box(_content, "Card" + i, position, new Vector2(300f, 300f), CardBack);
                card.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenCard(index));
                _memoryCards.Add(card);
            }

            RefreshMemory();
        }

        /// <summary>Same as tapping a card in Parovi.</summary>
        public MemoryGame.Result OpenCard(int index)
        {
            MemoryGame.Result result = Memory.Open(index);

            if (result == MemoryGame.Result.Ignored)
            {
                return result;
            }

            Say(Memory.Cards[index].Word);
            RefreshMemory();

            if (result == MemoryGame.Result.NoMatch)
            {
                StartCoroutine(TurnBackSoon());
            }
            else if (result == MemoryGame.Result.Match && Memory.IsDone)
            {
                Win();
            }

            return result;
        }

        private IEnumerator TurnBackSoon()
        {
            yield return new WaitForSecondsRealtime(0.9f);
            Memory.TurnBack();
            RefreshMemory();
        }

        private void RefreshMemory()
        {
            string language = LearningProgress.Language;

            for (int i = 0; i < _memoryCards.Count; i++)
            {
                MemoryGame.Card card = Memory.Cards[i];
                Image box = _memoryCards[i];

                foreach (Transform child in box.transform)
                {
                    Destroy(child.gameObject);
                }

                bool shown = card.Open || card.Matched;
                box.color = card.Matched ? Matched : shown ? CardFace : CardBack;

                if (!shown)
                {
                    Text(box.transform, "?", Vector2.zero, new Vector2(200f, 200f), 140f, Color.white);
                }
                else if (card.IsPicture)
                {
                    Picture(box.transform, _pack.Picture(card.Word.PictureId), Vector2.zero, 230f);
                }
                else
                {
                    Text(box.transform, card.Word.CardText(language, LearningProgress.ShowTransliteration, 60),
                        Vector2.zero, new Vector2(280f, 260f), 56f, Ink);
                }
            }
        }

        // ------------------------------------------------------------ Šta čuješ?

        private void ShowHear()
        {
            Clear();
            _title.text = LearningProgress.Ui("ŠTA ČUJEŠ?", "WHAT DO YOU HEAR?", "WAS HÖRST DU?");
            _status.text = $"{Hear.Round + 1} / {HearGame.Rounds}";

            Image speaker = Box(_content, "Speaker", new Vector2(0f, 200f), new Vector2(220f, 220f), CardFace);
            speaker.gameObject.AddComponent<Button>().onClick.AddListener(SayTarget);
            Picture(speaker.transform, WordPack.Icon("speaker"), Vector2.zero, 160f);

            string language = LearningProgress.Language;

            for (int i = 0; i < Hear.Choices.Count; i++)
            {
                int index = i;
                Image choice = Box(_content, "Choice" + i, new Vector2((i - 1) * 480f, -170f), new Vector2(440f, 220f),
                    CardFace);
                choice.gameObject.AddComponent<Button>().onClick.AddListener(() => AnswerHear(index));
                Text(choice.transform, Hear.Choices[i].CardText(language, LearningProgress.ShowTransliteration, 60),
                    Vector2.zero, new Vector2(410f, 200f), 64f, Ink);
            }
        }

        private void SayTarget()
        {
            if (Hear != null && !Hear.IsDone)
            {
                Say(Hear.Target);
            }
        }

        /// <summary>Same as tapping a written word in Šta čuješ?.</summary>
        public bool AnswerHear(int index)
        {
            if (!Hear.Answer(index))
            {
                // Not punished: the word is just said again.
                SayTarget();
                return false;
            }

            AudioManager.Instance?.PlayVoice(_pack.RandomPraise(LearningProgress.Language));

            if (Hear.IsDone)
            {
                Win();
            }
            else
            {
                ShowHear();
                StartCoroutine(SayTargetSoon());
            }

            return true;
        }

        private IEnumerator SayTargetSoon()
        {
            yield return new WaitForSecondsRealtime(1.1f);
            SayTarget();
        }

        // ------------------------------------------------------------ Složi riječ

        private void ShowBuild()
        {
            Clear();
            _title.text = LearningProgress.Ui("SLOŽI RIJEČ", "BUILD THE WORD", "BAU DAS WORT");
            _status.text = LearningProgress.Ui("Dodiruj slova po redu!", "Tap the letters in order!",
                "Tippe die Buchstaben der Reihe nach!");

            Image picture = Box(_content, "Picture", new Vector2(0f, 230f), new Vector2(240f, 240f), CardFace);
            picture.gameObject.AddComponent<Button>().onClick.AddListener(() => Say(Build.Word));
            Picture(picture.transform, _pack.Picture(Build.Word.PictureId), Vector2.zero, 200f);

            int count = Build.Letters.Count;
            float step = Mathf.Min(170f, 1500f / Mathf.Max(1, count));

            for (int i = 0; i < count; i++)
            {
                Image slot = Box(_content, "Slot" + i, new Vector2((i - (count - 1) * 0.5f) * step, 20f),
                    new Vector2(step - 20f, step - 20f), new Color(1f, 1f, 1f, 0.25f));
                string letter = i < Build.Placed ? Build.Letters[i] : "";
                Text(slot.transform, letter, Vector2.zero, new Vector2(step - 30f, step - 30f), 80f, Color.white);
            }

            for (int i = 0; i < Build.Tiles.Count; i++)
            {
                if (Build.Tiles[i] == null)
                {
                    continue;
                }

                int index = i;
                Image tile = Box(_content, "Tile" + i, new Vector2((i - (count - 1) * 0.5f) * step, -230f),
                    new Vector2(step - 20f, step - 20f), new Color(1f, 0.85f, 0.35f, 1f));
                tile.gameObject.AddComponent<Button>().onClick.AddListener(() => TapLetter(index));
                Text(tile.transform, Build.Tiles[i], Vector2.zero, new Vector2(step - 30f, step - 30f), 80f, Ink);
            }
        }

        /// <summary>Same as tapping a letter tile in Složi riječ.</summary>
        public bool TapLetter(int tileIndex)
        {
            bool right = Build.Tap(tileIndex);

            if (!right)
            {
                AudioManager.Instance?.PlayButton();
                return false;
            }

            AudioManager.Instance?.PlayCoin();

            if (Build.IsDone)
            {
                Say(Build.Word);
                Win();
            }
            else
            {
                ShowBuild();
            }

            return true;
        }

        // ------------------------------------------------------------ Piši slovo

        /// <summary>Letters to trace in one game.</summary>
        public const int LettersPerGame = 3;

        private List<TracingLetters.Letter> _traceLetters;
        private int _traceIndex;
        private TracingSurface _surface;
        private bool _letterDone;

        public TracingGame Trace { get; private set; }

        /// <summary>Letters finished in the current tracing game; for tests.</summary>
        public int TracedLetters { get; private set; }

        public TracingSurface Surface => _surface;

        private void StartLetter()
        {
            TracingLetters.Letter letter = _traceLetters[_traceIndex % _traceLetters.Count];
            Trace = new TracingGame(letter.id, TracingLetters.MaskOf(letter.id), TracingLetters.Grid);
            _letterDone = false;

            Clear();
            _title.text = LearningProgress.Ui("PIŠI SLOVO", "WRITE A LETTER", "SCHREIB EINEN BUCHSTABEN");
            _status.text = LearningProgress.Ui("Prstom pređi preko slova!", "Trace the letter with your finger!",
                "Fahre den Buchstaben mit dem Finger nach!") + $"   {TracedLetters + 1} / {LettersPerGame}";

            Image board = Box(_content, "Board", new Vector2(0f, -40f), new Vector2(660f, 660f), CardFace);
            Picture(board.transform, TracingLetters.Guide(letter.id), Vector2.zero, 620f);
            _surface = TracingSurface.Create(board.transform, Vector2.zero, 620f);
            _surface.Touched += OnTraced;

            Image speaker = Box(_content, "Speaker", new Vector2(-560f, -40f), new Vector2(200f, 200f), CardFace);
            speaker.gameObject.AddComponent<Button>().onClick.AddListener(SayLetter);
            Picture(speaker.transform, WordPack.Icon("speaker"), Vector2.zero, 150f);

            Image wipe = Box(_content, "Wipe", new Vector2(560f, -40f), new Vector2(240f, 110f), new Color(0.55f, 0.58f, 0.62f, 1f));
            wipe.gameObject.AddComponent<Button>().onClick.AddListener(WipeLetter);
            Text(wipe.transform, LearningProgress.Ui("OBRIŠI", "CLEAR", "LÖSCHEN"), Vector2.zero, new Vector2(220f, 90f), 40f,
                Color.white);

            SayLetter();
        }

        private void SayLetter()
        {
            if (Trace != null)
            {
                AudioManager.Instance?.PlayVoice(TracingLetters.Voice(LearningProgress.Language, Trace.LetterId));
            }
        }

        private void WipeLetter()
        {
            AudioManager.Instance?.PlayButton();
            Trace.Clear();
            _surface.Wipe();
        }

        /// <summary>The finger passing over a point of the letter (0..1, y up); tests call it too.</summary>
        public void TraceAt(Vector2 point)
        {
            _surface.DrawAt(point);
        }

        private void OnTraced(Vector2 point)
        {
            if (_letterDone)
            {
                return;
            }

            Trace.Touch(point);

            if (Trace.IsDone)
            {
                _letterDone = true;
                TracedLetters++;
                StartCoroutine(LetterDone());
            }
        }

        private IEnumerator LetterDone()
        {
            // Pepi praises, then the letter is said once more.
            AudioClip praise = PepiVoice.Line(Random.value < 0.5f ? "pepi_praise_1" : "pepi_praise_2");
            AudioManager.Instance?.PlayVoice(praise);
            yield return new WaitForSecondsRealtime(praise != null ? praise.length + 0.2f : 0.8f);
            SayLetter();
            yield return new WaitForSecondsRealtime(1.2f);

            if (TracedLetters >= LettersPerGame)
            {
                Win();
            }
            else
            {
                _traceIndex++;
                StartLetter();
            }
        }

        // ------------------------------------------------------------ end

        private void Win()
        {
            Finished++;
            SaveManager.Data.totalCoins += MiniGames.Reward;
            SaveManager.Save();
            StartCoroutine(WinSoon());
        }

        private IEnumerator WinSoon()
        {
            yield return new WaitForSecondsRealtime(0.8f);
            AudioManager.Instance?.PlayVoice(_pack.RandomPraise(LearningProgress.Language));
            Clear();
            _title.text = "BRAVO!";
            _status.text = LearningProgress.Ui($"+{MiniGames.Reward} novčića", $"+{MiniGames.Reward} coins",
                $"+{MiniGames.Reward} Münzen");
            Picture(_content, WordPack.Icon("star"), new Vector2(0f, 40f), 260f);
            yield return new WaitForSecondsRealtime(1.6f);

            if (IsOpen)
            {
                ShowPicker();
            }
        }

        private void Say(WordEntry word)
        {
            if (word != null && _pack != null)
            {
                AudioManager.Instance?.PlayVoice(_pack.Voice(LearningProgress.Language, word.id));
            }
        }

        // ------------------------------------------------------------ layout

        private void BuildPanel()
        {
            _panel = new GameObject("MiniGames", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _panel.transform.SetParent(transform, false);
            var rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Image background = _panel.GetComponent<Image>();
            background.color = Dim;
            background.raycastTarget = true;

            _title = Text(_panel.transform, "", new Vector2(0f, 440f), new Vector2(1200f, 100f), 70f, Color.white);
            _status = Text(_panel.transform, "", new Vector2(0f, 370f), new Vector2(1200f, 50f), 34f,
                new Color(1f, 0.9f, 0.45f, 1f));

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(_panel.transform, false);
            _content = content.transform;

            Image back = Box(_panel.transform, "Back", new Vector2(-760f, 440f), new Vector2(240f, 90f),
                new Color(0.06f, 0.16f, 0.3f, 1f));
            back.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                if (Current.HasValue)
                {
                    AudioManager.Instance?.PlayButton();
                    ShowPicker();
                }
                else
                {
                    Close();
                }
            });
            _backLabel = Text(back.transform, "", Vector2.zero, new Vector2(220f, 80f), 36f, Color.white);
        }

        private void Clear()
        {
            StopAllCoroutines();

            foreach (Transform child in _content)
            {
                Destroy(child.gameObject);
            }
        }

        private static Image Box(Transform parent, string name, Vector2 position, Vector2 size, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.sprite = RuntimeSprites.RoundedRect();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1.2f;
            image.color = colour;
            return image;
        }

        private static void Picture(Transform parent, Sprite sprite, Vector2 position, float size)
        {
            var go = new GameObject("Picture", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = sprite != null;
        }

        private static TMP_Text Text(Transform parent, string content, Vector2 position, Vector2 size, float fontSize,
            Color colour)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.AddComponent<TextMeshProUGUI>();
            GameFonts.UseDisplay(text);
            text.text = content;
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = fontSize * 0.4f;
            text.fontSizeMax = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = colour;
            text.raycastTarget = false;
            return text;
        }
    }
}
