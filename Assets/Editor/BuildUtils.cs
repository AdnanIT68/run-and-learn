using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FootballRunner.EditorTools
{
    /// <summary>
    /// Helpers shared by the generator scripts. Editor-only.
    /// </summary>
    internal static class BuildUtils
    {
        public static readonly Color Sky = new Color(0.45f, 0.75f, 0.95f);
        public static readonly Color Grass = new Color(0.35f, 0.72f, 0.32f);
        public static readonly Color Dirt = new Color(0.55f, 0.40f, 0.26f);
        public static readonly Color PanelBackground = new Color(0.08f, 0.11f, 0.18f, 0.92f);
        public static readonly Color Accent = new Color(0.99f, 0.76f, 0.18f);
        public static readonly Color ButtonGreen = new Color(0.29f, 0.72f, 0.35f);
        public static readonly Color ButtonBlue = new Color(0.25f, 0.55f, 0.90f);
        public static readonly Color ButtonRed = new Color(0.86f, 0.33f, 0.31f);

        /// <summary>Writes a texture to the project and imports it as a sprite.</summary>
        public static Sprite CreateSprite(string assetPath, int size, bool circle, int pixelsPerUnit)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float radius = size * 0.5f;
            var centre = new Vector2(radius, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inside = !circle || Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre) <= radius - 0.5f;
                    pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            EnsureFolder(System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/'));
            System.IO.File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        public static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }

        public static void AddLayer(string layerName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");

            if (assets == null || assets.Length == 0)
            {
                Debug.LogError("[BuildUtils] Could not open TagManager.asset");
                return;
            }

            var tagManager = new SerializedObject(assets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                {
                    return;
                }
            }

            // Indices 0-7 are reserved by Unity apart from a few blanks; start at 6.
            for (int i = 6; i < layers.arraySize; i++)
            {
                SerializedProperty element = layers.GetArrayElementAtIndex(i);

                if (!string.IsNullOrEmpty(element.stringValue))
                {
                    continue;
                }

                element.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[BuildUtils] Added layer '{layerName}' at index {i}.");
                return;
            }

            Debug.LogError($"[BuildUtils] No free layer slot for '{layerName}'.");
        }

        public static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;

            foreach (Transform child in root.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        public static GameObject CreateSpriteObject(string name, Transform parent, Sprite sprite, Color colour,
            Vector3 localPosition, Vector2 localScale, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = new Vector3(localScale.x, localScale.y, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = colour;
            renderer.sortingOrder = sortingOrder;

            return go;
        }

        public static Transform[] CreateSlots(string groupName, Transform parent, IEnumerable<Vector2> positions)
        {
            var group = new GameObject(groupName);
            group.transform.SetParent(parent, false);

            var result = new List<Transform>();

            foreach (Vector2 position in positions)
            {
                var slot = new GameObject($"Slot_{result.Count}");
                slot.transform.SetParent(group.transform, false);
                slot.transform.localPosition = new Vector3(position.x, position.y, 0f);
                result.Add(slot.transform);
            }

            return result.ToArray();
        }

        // ---------- UI helpers ----------

        public static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        public static RectTransform Place(GameObject go, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        public static Image CreatePanel(string name, Transform parent, Color colour)
        {
            GameObject go = CreateUIObject(name, parent);
            var image = go.AddComponent<Image>();
            image.color = colour;
            Place(go, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return image;
        }

        public static TextMeshProUGUI CreateText(string name, Transform parent, string content,
            float fontSize, TextAlignmentOptions alignment, Color colour)
        {
            GameObject go = CreateUIObject(name, parent);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = colour;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(string name, Transform parent, string label, Vector2 size,
            Color colour, float fontSize = 42f)
        {
            GameObject go = CreateUIObject(name, parent);

            var image = go.AddComponent<Image>();
            image.color = colour;
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colours = button.colors;
            colours.highlightedColor = Color.Lerp(colour, Color.white, 0.2f);
            colours.pressedColor = Color.Lerp(colour, Color.black, 0.2f);
            button.colors = colours;

            TextMeshProUGUI text = CreateText("Label", go.transform, label, fontSize,
                TextAlignmentOptions.Center, Color.white);
            Place(text.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            return button;
        }

        public static Toggle CreateToggle(string name, Transform parent, string label, Vector2 size)
        {
            GameObject go = CreateUIObject(name, parent);
            var toggle = go.AddComponent<Toggle>();
            Place(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);

            GameObject box = CreateUIObject("Background", go.transform);
            var boxImage = box.AddComponent<Image>();
            boxImage.color = new Color(1f, 1f, 1f, 0.25f);
            boxImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            boxImage.type = Image.Type.Sliced;
            Place(box, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0f), new Vector2(56f, 56f));

            GameObject check = CreateUIObject("Checkmark", box.transform);
            var checkImage = check.AddComponent<Image>();
            checkImage.color = Accent;
            checkImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            checkImage.type = Image.Type.Sliced;
            Place(check, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(34f, 34f));

            TextMeshProUGUI text = CreateText("Label", go.transform, label, 34f,
                TextAlignmentOptions.Left, Color.white);
            Place(text.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(74f, 0f), new Vector2(-74f, 56f));

            toggle.targetGraphic = boxImage;
            toggle.graphic = checkImage;
            return toggle;
        }
    }
}
