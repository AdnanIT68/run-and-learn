using System.Collections.Generic;
using FootballRunner.Learning;
using FootballRunner.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FootballRunner.UI
{
    /// <summary>
    /// The modern main menu: sky with drifting clouds, a big waving Rukajja with
    /// Pepi, a bouncing logo, rounded buttons with icons and stat chips. It
    /// restyles the scene's existing buttons instead of replacing them, so every
    /// listener and test keeps working.
    /// </summary>
    public class MainMenuStyle : MonoBehaviour
    {
        private const string MenuArt = "Menu/";

        private readonly List<RectTransform> _clouds = new List<RectTransform>();
        private readonly List<float> _cloudSpeeds = new List<float>();
        private readonly List<(TMP_Text source, TMP_Text shown)> _chips = new List<(TMP_Text, TMP_Text)>();
        private RectTransform _hero;
        private RectTransform _pepi;
        private RectTransform _logo;
        private Vector2 _heroBase;
        private Vector2 _pepiBase;

        /// <summary>The rounded display font for titles and buttons; null if it failed to load.</summary>
        public static TMP_FontAsset TitleFont => GameFonts.Display;

        public static MainMenuStyle Apply(Transform canvas, Button play, Button settings, Button quit,
            Button howToPlay, TMP_Text best, TMP_Text coins)
        {
            var style = canvas.gameObject.GetComponent<MainMenuStyle>();

            if (style != null)
            {
                return style;
            }

            style = canvas.gameObject.AddComponent<MainMenuStyle>();
            style.Build(canvas, play, settings, quit, howToPlay, best, coins);
            return style;
        }

        private void Build(Transform canvas, Button play, Button settings, Button quit, Button howToPlay,
            TMP_Text best, TMP_Text coins)
        {
            KeepHeight(canvas);
            Transform safe = canvas.Find("SafeArea");
            Transform layer = safe != null ? safe : canvas;

            BuildBackground(canvas);
            HideOldDecor(canvas);
            HideOldDecor(layer);
            BuildHero(layer);
            BuildLogo(layer);

            StyleBigButton(play, LearningProgress.Ui("IGRAJ", "PLAY", "SPIELEN"), new Vector2(420f, 70f), new Vector2(600f, 170f),
                new Color(0.27f, 0.74f, 0.32f), Load("play"));
            StyleCard(layer.Find("WorldsButton"), new Vector2(120f, -120f), new Color(0.24f, 0.55f, 0.92f));
            StyleCard(layer.Find("CharactersButton"), new Vector2(420f, -120f), new Color(0.2f, 0.62f, 0.42f));
            StyleCard(layer.Find("AlbumButton"), new Vector2(720f, -120f), new Color(0.97f, 0.55f, 0.18f));
            StylePill(layer.Find("LanguageButton"), new Vector2(420f, -265f), new Vector2(600f, 90f),
                new Color(0.18f, 0.32f, 0.62f), Load("icon_language"));

            StyleRoundIcon(settings, new Vector2(-150f, 80f), Load("icon_settings"));
            StyleRoundIcon(howToPlay, new Vector2(-290f, 80f), Load("icon_help"));
            StyleRoundIcon(quit, new Vector2(-10f, 80f), Load("icon_exit"));
            StyleRoundIcon(layer.Find("TodayButton")?.GetComponent<Button>(), new Vector2(-430f, 80f),
                WordPack.Icon("gift"));

            BuildChip(layer, best, Load("icon_trophy"), new Vector2(-470f, -60f));
            BuildChip(layer, coins, Load("icon_coin"), new Vector2(-230f, -60f));
        }

        /// <summary>
        /// Landscape phones are wider than 16:9. Scaling by height keeps the vertical
        /// layout fixed and lets the extra width open up at the sides, instead of
        /// squeezing the logo into the Play button.
        /// </summary>
        public static void KeepHeight(Transform canvas)
        {
            var scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;

            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 1f;
            }
        }

        private static Sprite Load(string name)
        {
            Sprite sprite = Resources.Load<Sprite>(MenuArt + name);

            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>(MenuArt + name);

                if (texture != null)
                {
                    sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), 100f);
                }
            }

            return sprite;
        }

        private void BuildBackground(Transform canvas)
        {
            Image sky = CreateImage(canvas, "MenuBackground", Load("background"));
            Stretch(sky.rectTransform);
            sky.preserveAspect = false;
            sky.raycastTarget = false;
            sky.transform.SetAsFirstSibling();

            Sprite cloud = Load("cloud");
            float[] heights = { -120f, -260f, -60f };
            float[] starts = { -500f, 300f, 900f };
            float[] scales = { 1f, 0.7f, 0.55f };

            for (int i = 0; i < heights.Length; i++)
            {
                Image image = CreateImage(sky.transform, "Cloud" + i, cloud);
                RectTransform rect = image.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(420f, 200f) * scales[i];
                rect.anchoredPosition = new Vector2(starts[i], heights[i]);
                image.color = new Color(1f, 1f, 1f, 0.9f);
                image.raycastTarget = false;
                _clouds.Add(rect);
                _cloudSpeeds.Add(18f + i * 9f);
            }
        }

        /// <summary>The old flat title, stat captions and dark scrim give way to the logo, chips and sky.</summary>
        private static void HideOldDecor(Transform layer)
        {
            // Scrim darkened the old world background for contrast; the new sky does not need it.
            foreach (string name in new[] { "Title", "BestCaption", "CoinsCaption", "Scrim" })
            {
                Transform old = layer.Find(name);

                if (old != null)
                {
                    old.gameObject.SetActive(false);
                }
            }
        }

        private void BuildHero(Transform layer)
        {
            Image hero = CreateImage(layer, "Hero", Load("rukajja_wave"));
            _hero = hero.rectTransform;
            _hero.anchorMin = _hero.anchorMax = new Vector2(0f, 0f);
            _hero.pivot = new Vector2(0.5f, 0f);
            _hero.sizeDelta = new Vector2(560f, 780f);
            _heroBase = new Vector2(430f, -10f);
            _hero.anchoredPosition = _heroBase;
            hero.raycastTarget = false;

            Image pepi = CreateImage(layer, "Pepi", WordPack.Icon("parrot"));
            _pepi = pepi.rectTransform;
            _pepi.anchorMin = _pepi.anchorMax = new Vector2(0f, 0f);
            _pepi.sizeDelta = new Vector2(190f, 190f);
            _pepiBase = new Vector2(780f, 680f);
            _pepi.anchoredPosition = _pepiBase;
            // The parrot art faces left; turn him towards Rukajja's side of the screen.
            _pepi.localScale = new Vector3(-1f, 1f, 1f);
            pepi.raycastTarget = false;
        }

        private void BuildLogo(Transform layer)
        {
            var go = new GameObject("Logo", typeof(RectTransform));
            go.transform.SetParent(layer, false);
            _logo = go.GetComponent<RectTransform>();
            _logo.anchorMin = _logo.anchorMax = new Vector2(0.5f, 1f);
            _logo.anchoredPosition = new Vector2(420f, -190f);
            _logo.sizeDelta = new Vector2(900f, 200f);

            TMP_Text title = CreateText(go.transform, "Title", "Run and Learn", 118f, Color.white);
            Stretch(title.rectTransform);
            title.outlineWidth = 0.22f;
            title.outlineColor = new Color32(30, 86, 150, 255);
            var shadow = title.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.15f, 0.35f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -8f);

            TMP_Text subtitle = CreateText(go.transform, "Subtitle", "Bosanski  ·  English  ·  Deutsch", 40f,
                new Color32(30, 86, 150, 255));
            RectTransform sub = subtitle.rectTransform;
            sub.anchorMin = sub.anchorMax = new Vector2(0.5f, 0f);
            sub.anchoredPosition = new Vector2(0f, -20f);
            sub.sizeDelta = new Vector2(1000f, 60f);
        }

        private static void StyleBigButton(Button button, string label, Vector2 position, Vector2 size, Color colour,
            Sprite icon)
        {
            if (button == null)
            {
                return;
            }

            RectTransform rect = PlaceCentre(button, position, size);
            Round(button.GetComponent<Image>(), colour, 1.2f);
            AddDepth(button.gameObject);

            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);

            if (text != null)
            {
                text.text = label;
                StyleLabel(text, 84f);
                text.rectTransform.offsetMin = new Vector2(140f, 0f);
            }

            Image glyph = CreateImage(rect, "Icon", icon);
            glyph.rectTransform.anchorMin = glyph.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            glyph.rectTransform.anchoredPosition = new Vector2(110f, 0f);
            glyph.rectTransform.sizeDelta = new Vector2(96f, 96f);
            glyph.raycastTarget = false;
        }

        private static void StyleCard(Transform card, Vector2 position, Color colour)
        {
            if (card == null)
            {
                return;
            }

            var button = card.GetComponent<Button>();
            PlaceCentre(button, position, new Vector2(280f, 150f));
            Round(card.GetComponent<Image>(), colour, 1.4f);
            AddDepth(card.gameObject);

            // Same layout on every card: picture on the left, label filling the rest.
            foreach (Image image in card.GetComponentsInChildren<Image>(true))
            {
                if (image.transform == card)
                {
                    continue;
                }

                RectTransform icon = image.rectTransform;
                icon.anchorMin = icon.anchorMax = new Vector2(0f, 0.5f);
                icon.pivot = new Vector2(0.5f, 0.5f);
                icon.anchoredPosition = new Vector2(66f, 0f);
                icon.sizeDelta = new Vector2(92f, 92f);
            }

            foreach (TMP_Text text in card.GetComponentsInChildren<TMP_Text>(true))
            {
                StyleLabel(text, 38f);
                RectTransform label = text.rectTransform;
                label.anchorMin = Vector2.zero;
                label.anchorMax = Vector2.one;
                label.offsetMin = new Vector2(118f, 12f);
                label.offsetMax = new Vector2(-12f, -12f);
            }
        }

        private static void StylePill(Transform pill, Vector2 position, Vector2 size, Color colour, Sprite icon)
        {
            if (pill == null)
            {
                return;
            }

            RectTransform rect = PlaceCentre(pill.GetComponent<Button>(), position, size);
            Round(pill.GetComponent<Image>(), colour, 1.6f);
            AddDepth(pill.gameObject);

            TMP_Text text = pill.GetComponentInChildren<TMP_Text>(true);

            if (text != null)
            {
                StyleLabel(text, 32f);
                text.rectTransform.offsetMin = new Vector2(80f, 0f);
            }

            Image glyph = CreateImage(rect, "Icon", icon);
            glyph.rectTransform.anchorMin = glyph.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            glyph.rectTransform.anchoredPosition = new Vector2(56f, 0f);
            glyph.rectTransform.sizeDelta = new Vector2(56f, 56f);
            glyph.raycastTarget = false;
        }

        /// <summary>A white round button with only an icon, anchored to the bottom-right corner.</summary>
        private static void StyleRoundIcon(Button button, Vector2 fromBottomRight, Sprite icon)
        {
            if (button == null)
            {
                return;
            }

            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = new Vector2(120f, 120f);
            rect.anchoredPosition = fromBottomRight - new Vector2(40f, 40f);

            var image = button.GetComponent<Image>();
            image.sprite = RuntimeSprites.Circle();
            image.type = Image.Type.Simple;
            image.color = new Color(1f, 1f, 1f, 0.95f);
            AddDepth(button.gameObject);

            foreach (TMP_Text text in button.GetComponentsInChildren<TMP_Text>(true))
            {
                text.gameObject.SetActive(false);
            }

            Image glyph = CreateImage(rect, "Icon", icon);
            Stretch(glyph.rectTransform, 22f);
            glyph.raycastTarget = false;
        }

        /// <summary>White pill in the top-right corner holding an icon and a number.</summary>
        private void BuildChip(Transform layer, TMP_Text value, Sprite icon, Vector2 position)
        {
            if (value == null)
            {
                return;
            }

            Image chip = CreateImage(layer, value.name + "Chip", RuntimeSprites.RoundedRect());
            chip.type = Image.Type.Sliced;
            chip.pixelsPerUnitMultiplier = 1.6f;
            chip.color = new Color(1f, 1f, 1f, 0.92f);
            chip.raycastTarget = false;
            RectTransform rect = chip.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(220f, 84f);
            AddDepth(chip.gameObject);

            Image glyph = CreateImage(rect, "Icon", icon);
            glyph.rectTransform.anchorMin = glyph.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            glyph.rectTransform.anchoredPosition = new Vector2(48f, 0f);
            glyph.rectTransform.sizeDelta = new Vector2(56f, 56f);
            glyph.raycastTarget = false;

            // A fresh label mirrors the scene's value text, which keeps its own layout
            // rules and would not sit cleanly inside the chip.
            TMP_Text number = CreateText(rect, "Value", value.text, 44f, new Color32(30, 60, 100, 255));
            RectTransform text = number.rectTransform;
            text.anchorMin = Vector2.zero;
            text.anchorMax = Vector2.one;
            text.offsetMin = new Vector2(92f, 0f);
            text.offsetMax = new Vector2(-18f, 0f);
            number.alignment = TextAlignmentOptions.Left;
            number.enableAutoSizing = true;
            number.fontSizeMin = 24f;
            number.fontSizeMax = 44f;
            value.gameObject.SetActive(false);
            _chips.Add((value, number));
        }

        private static RectTransform PlaceCentre(Component target, Vector2 position, Vector2 size)
        {
            var rect = target.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static void Round(Image image, Color colour, float cornerScale)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = RuntimeSprites.RoundedRect();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = cornerScale;
            image.color = colour;
        }

        /// <summary>A soft drop shadow and a squash when pressed: the two cues that read as "tappable".</summary>
        private static void AddDepth(GameObject target)
        {
            if (target.GetComponent<Shadow>() == null)
            {
                var shadow = target.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0.1f, 0.25f, 0.28f);
                shadow.effectDistance = new Vector2(0f, -10f);
            }

            if (target.GetComponent<Button>() != null && target.GetComponent<PressSquash>() == null)
            {
                target.AddComponent<PressSquash>();
            }
        }

        private static void StyleLabel(TMP_Text text, float size)
        {
            // Font first: outline settings below create a material instance from
            // whatever material is current, and it must be the new font's.
            UseTitleFont(text);
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = size * 0.5f;
            text.fontSizeMax = size;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.outlineWidth = 0.12f;
            text.outlineColor = new Color32(0, 0, 0, 70);
        }

        private static void UseTitleFont(TMP_Text text)
        {
            GameFonts.UseDisplay(text);
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.enabled = sprite != null;
            return image;
        }

        private static TMP_Text CreateText(Transform parent, string name, string content, float size, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = colour;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            UseTitleFont(text);
            return text;
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private void LateUpdate()
        {
            // The menu writes best score and coins into the scene texts; show those values.
            foreach ((TMP_Text source, TMP_Text shown) in _chips)
            {
                if (source != null && shown.text != source.text)
                {
                    shown.text = source.text;
                }
            }
        }

        private void Update()
        {
            float time = Time.unscaledTime;

            for (int i = 0; i < _clouds.Count; i++)
            {
                RectTransform cloud = _clouds[i];
                Vector2 position = cloud.anchoredPosition;
                position.x += _cloudSpeeds[i] * Time.unscaledDeltaTime;

                if (position.x > 1250f)
                {
                    position.x = -1250f;
                }

                cloud.anchoredPosition = position;
            }

            if (_hero != null)
            {
                _hero.anchoredPosition = _heroBase + new Vector2(0f, Mathf.Sin(time * 2f) * 8f);
                _hero.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 1.3f) * 1.5f);
            }

            if (_pepi != null)
            {
                _pepi.anchoredPosition = _pepiBase + new Vector2(Mathf.Sin(time * 0.9f) * 14f, Mathf.Sin(time * 2.6f) * 16f);
            }

            if (_logo != null)
            {
                float pulse = 1f + Mathf.Sin(time * 2.2f) * 0.025f;
                _logo.localScale = new Vector3(pulse, pulse, 1f);
            }
        }
    }

    /// <summary>Shrinks a button slightly while it is held, so taps feel physical.</summary>
    public class PressSquash : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private Vector3 _baseScale = Vector3.one;
        private bool _pressed;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
            transform.localScale = _baseScale * 0.94f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Release();
        }

        private void Release()
        {
            if (!_pressed)
            {
                return;
            }

            _pressed = false;
            transform.localScale = _baseScale;
        }
    }
}
