using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LiquidCrystalUIRefiner
{
    private const string UiRoot = "Assets/UI/PixelCampus/Sprites/";
    private const string BattleBackground = "Assets/Art/Combat/LiquidCrystalBattleArena.png";

    private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
    private static readonly Color32 Ink = new Color32(3, 14, 25, 255);
    private static readonly Color32 Interior = new Color32(10, 34, 48, 246);
    private static readonly Color32 InteriorRaised = new Color32(13, 44, 58, 252);
    private static readonly Color32 CrystalShadow = new Color32(13, 74, 92, 235);
    private static readonly Color32 CrystalBlue = new Color32(47, 170, 206, 245);
    private static readonly Color32 CrystalCyan = new Color32(105, 225, 236, 245);
    private static readonly Color32 CrystalGreen = new Color32(42, 204, 139, 245);
    private static readonly Color32 CrystalLight = new Color32(196, 255, 240, 238);
    private static readonly Color32 Ivory = new Color32(244, 240, 211, 255);
    private static readonly Color32 Muted = new Color32(166, 205, 199, 255);

    [MenuItem("Tools/UVGMon/Apply Liquid Crystal UI Refinement")]
    public static void ApplyAll()
    {
        GenerateAssets();
        AssetDatabase.Refresh();
        ConfigureAssets();
        FixPrefabs();
        FixScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Liquid Crystal UI: assets, prefabs and scenes refined.");
    }

    private static Texture2D Texture(int width, int height)
    {
        Texture2D t = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Clear;
        t.SetPixels32(pixels);
        return t;
    }

    private static void Rect(Texture2D t, int x, int y, int width, int height, Color32 color)
    {
        int x0 = Mathf.Clamp(x, 0, t.width);
        int y0 = Mathf.Clamp(y, 0, t.height);
        int x1 = Mathf.Clamp(x + width, 0, t.width);
        int y1 = Mathf.Clamp(y + height, 0, t.height);
        for (int py = y0; py < y1; py++)
            for (int px = x0; px < x1; px++)
                t.SetPixel(px, py, color);
    }

    private static void StepRect(Texture2D t, int x, int y, int width, int height, int cut, Color32 color)
    {
        int c = Mathf.Max(0, Mathf.Min(cut, Mathf.Min(width, height) / 3));
        Rect(t, x + c, y, width - c * 2, height, color);
        Rect(t, x, y + c, width, height - c * 2, color);
    }

    private static void CrystalFrame(Texture2D t, int x, int y, int width, int height, int cut, Color32 fill, bool active)
    {
        StepRect(t, x, y, width, height, cut, Ink);
        StepRect(t, x + 3, y + 3, width - 6, height - 6, Mathf.Max(3, cut - 2), CrystalShadow);
        StepRect(t, x + 6, y + 6, width - 12, height - 12, Mathf.Max(3, cut - 4), active ? CrystalGreen : CrystalBlue);
        StepRect(t, x + 9, y + 9, width - 18, height - 18, Mathf.Max(3, cut - 6), CrystalCyan);
        StepRect(t, x + 13, y + 13, width - 26, height - 26, Mathf.Max(2, cut - 8), Ink);
        StepRect(t, x + 16, y + 16, width - 32, height - 32, Mathf.Max(2, cut - 10), fill);

        int span = Mathf.Max(24, width / 5);
        Rect(t, x + cut + 10, y + height - 9, span, 3, CrystalLight);
        Rect(t, x + width - cut - span - 10, y + 6, span, 3, active ? CrystalGreen : CrystalBlue);
        Rect(t, x + 8, y + height - cut - 28, 3, 22, CrystalGreen);
        Rect(t, x + width - 11, y + cut + 8, 3, 22, CrystalBlue);
    }

    private static void Save(Texture2D t, string path)
    {
        t.Apply(false, false);
        string absolute = Path.Combine(Application.dataPath.Substring(0, Application.dataPath.Length - 6), path);
        Directory.CreateDirectory(Path.GetDirectoryName(absolute));
        File.WriteAllBytes(absolute, t.EncodeToPNG());
        Object.DestroyImmediate(t);
    }

    private static void GenerateAssets()
    {
        Texture2D t = Texture(1024, 576);
        CrystalFrame(t, 6, 6, 1012, 564, 20, Interior, false);
        Save(t, UiRoot + "Core/panel_base.png");

        t = Texture(1024, 112);
        CrystalFrame(t, 4, 4, 1016, 104, 15, InteriorRaised, false);
        Save(t, UiRoot + "Core/title_bar.png");

        Save(Button(InteriorRaised, false, false), UiRoot + "Core/button_normal.png");
        Save(Button(new Color32(13, 61, 66, 252), true, false), UiRoot + "Core/button_hover.png");
        Save(Button(new Color32(8, 47, 55, 252), true, true), UiRoot + "Core/button_pressed.png");
        Save(Button(new Color32(25, 43, 50, 235), false, false), UiRoot + "Core/button_disabled.png");

        t = Texture(512, 96);
        CrystalFrame(t, 4, 8, 504, 84, 14, new Color32(10, 69, 65, 252), true);
        Rect(t, 164, 4, 184, 7, CrystalGreen);
        Save(t, UiRoot + "Core/tab_active.png");

        t = Texture(512, 96);
        CrystalFrame(t, 4, 8, 504, 84, 14, Interior, false);
        Rect(t, 196, 4, 120, 4, CrystalShadow);
        Save(t, UiRoot + "Core/tab_inactive.png");

        t = Texture(256, 256);
        CrystalFrame(t, 6, 6, 244, 244, 18, InteriorRaised, false);
        Save(t, UiRoot + "Inventory/item_slot.png");

        t = Texture(256, 256);
        CrystalFrame(t, 6, 6, 244, 244, 18, InteriorRaised, true);
        Rect(t, 30, 24, 196, 5, CrystalGreen);
        Save(t, UiRoot + "Inventory/uvgmon_slot.png");

        t = Texture(768, 160);
        CrystalFrame(t, 4, 4, 760, 152, 18, Interior, false);
        Rect(t, 44, 45, 680, 4, CrystalShadow);
        Save(t, UiRoot + "Combat/battle_status_panel.png");

        t = Texture(1024, 128);
        CrystalFrame(t, 4, 4, 1016, 120, 16, Interior, false);
        Rect(t, 964, 18, 18, 4, CrystalGreen);
        Rect(t, 970, 12, 8, 4, CrystalGreen);
        Save(t, UiRoot + "Combat/message_panel.png");

        t = Texture(768, 64);
        StepRect(t, 4, 14, 760, 36, 8, Ink);
        StepRect(t, 8, 18, 752, 28, 6, CrystalGreen);
        Rect(t, 20, 36, 728, 6, CrystalLight);
        Save(t, UiRoot + "Combat/hp_fill.png");

        t = Texture(128, 128);
        CrystalFrame(t, 7, 7, 114, 114, 16, InteriorRaised, false);
        for (int i = 0; i < 7; i++)
        {
            Rect(t, 38 + i * 8, 38 + i * 8, 7, 7, Ivory);
            Rect(t, 83 - i * 8, 38 + i * 8, 7, 7, Ivory);
        }
        Save(t, UiRoot + "Core/close_button.png");

        t = Texture(512, 96);
        CrystalFrame(t, 4, 10, 504, 76, 16, InteriorRaised, true);
        Save(t, UiRoot + "Core/status_badge.png");

        t = Texture(256, 256);
        Rect(t, 8, 220, 52, 7, CrystalCyan); Rect(t, 8, 196, 7, 31, CrystalCyan);
        Rect(t, 196, 220, 52, 7, CrystalGreen); Rect(t, 241, 196, 7, 31, CrystalGreen);
        Rect(t, 8, 29, 52, 7, CrystalGreen); Rect(t, 8, 29, 7, 31, CrystalGreen);
        Rect(t, 196, 29, 52, 7, CrystalCyan); Rect(t, 241, 29, 7, 31, CrystalCyan);
        Save(t, UiRoot + "Core/selection_frame.png");

        t = Texture(256, 256);
        for (int y = 8; y < 248; y += 4)
            for (int x = 8; x < 248; x += 4)
            {
                float dx = x + 2 - 128;
                float dy = y + 2 - 128;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d >= 94 && d <= 110)
                    Rect(t, x, y, 4, 4, ((x + y) / 16) % 2 == 0 ? CrystalCyan : CrystalGreen);
            }
        Save(t, UiRoot + "Combat/qte_ring.png");

        t = Texture(64, 640);
        CrystalFrame(t, 4, 4, 56, 632, 10, Interior, false);
        Rect(t, 25, 42, 14, 556, Ink);
        Save(t, UiRoot + "Core/scrollbar_track.png");

        t = Texture(64, 192);
        CrystalFrame(t, 4, 4, 56, 184, 10, new Color32(9, 72, 68, 255), true);
        Rect(t, 20, 105, 24, 5, CrystalLight);
        Rect(t, 20, 81, 24, 5, CrystalLight);
        Save(t, UiRoot + "Core/scrollbar_thumb.png");
    }

    private static Texture2D Button(Color32 fill, bool active, bool pressed)
    {
        Texture2D t = Texture(768, 96);
        CrystalFrame(t, 4, 4, 760, 88, 14, fill, active);
        if (pressed) Rect(t, 34, 17, 700, 4, CrystalShadow);
        return t;
    }

    private static void ConfigureAssets()
    {
        Configure(UiRoot + "Core/panel_base.png", new Vector4(40, 40, 40, 40));
        Configure(UiRoot + "Core/title_bar.png", new Vector4(40, 24, 40, 24));
        Configure(UiRoot + "Core/button_normal.png", new Vector4(40, 24, 40, 24));
        Configure(UiRoot + "Core/button_hover.png", new Vector4(40, 24, 40, 24));
        Configure(UiRoot + "Core/button_pressed.png", new Vector4(40, 24, 40, 24));
        Configure(UiRoot + "Core/button_disabled.png", new Vector4(40, 24, 40, 24));
        Configure(UiRoot + "Core/tab_active.png", new Vector4(36, 22, 36, 22));
        Configure(UiRoot + "Core/tab_inactive.png", new Vector4(36, 22, 36, 22));
        Configure(UiRoot + "Inventory/item_slot.png", new Vector4(28, 28, 28, 28));
        Configure(UiRoot + "Inventory/uvgmon_slot.png", new Vector4(28, 28, 28, 28));
        Configure(UiRoot + "Combat/battle_status_panel.png", new Vector4(40, 28, 40, 28));
        Configure(UiRoot + "Combat/message_panel.png", new Vector4(40, 24, 40, 24));
        Configure(UiRoot + "Combat/hp_fill.png", new Vector4(12, 12, 12, 12));
        Configure(UiRoot + "Core/close_button.png", Vector4.zero);
        Configure(UiRoot + "Core/status_badge.png", new Vector4(36, 22, 36, 22));
        Configure(UiRoot + "Core/selection_frame.png", Vector4.zero);
        Configure(UiRoot + "Combat/qte_ring.png", Vector4.zero);
        Configure(UiRoot + "Core/scrollbar_track.png", new Vector4(14, 28, 14, 28));
        Configure(UiRoot + "Core/scrollbar_thumb.png", new Vector4(14, 28, 14, 28));
    }

    private static void Configure(string path, Vector4 border)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.sRGBTexture = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.spritePixelsPerUnit = 100f;
        importer.spriteBorder = border;
        importer.SaveAndReimport();
    }

    private static Sprite SpriteAt(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Transform Child(Transform root, string path)
    {
        return root == null ? null : root.Find(path);
    }

    private static void Fixed(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        if (rt == null) return;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
        rt.localScale = Vector3.one;
    }

private static void AnchoredRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rt == null) return;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        rt.localScale = Vector3.one;
    }


private static void ImageStyle(Transform t, string path, Color color, bool preserve)
    {
        if (t == null) return;

        if (color.a <= 0.001f)
        {
            Mask legacyMask = t.GetComponent<Mask>();
            if (legacyMask != null) Object.DestroyImmediate(legacyMask, true);
        }

        Image image = t.GetComponent<Image>();
        if (image == null) image = t.gameObject.AddComponent<Image>();
        if (!string.IsNullOrEmpty(path)) image.sprite = SpriteAt(path);
        image.color = color;
        image.preserveAspect = preserve;

        bool receivesInput = t.GetComponent<Selectable>() != null || t.GetComponent<ScrollRect>() != null;
        MonoBehaviour[] behaviours = t.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length && !receivesInput; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            receivesInput = behaviour is IPointerClickHandler
                || behaviour is IPointerDownHandler
                || behaviour is IPointerUpHandler
                || behaviour is IBeginDragHandler
                || behaviour is IDragHandler
                || behaviour is IEndDragHandler
                || behaviour is IDropHandler
                || behaviour is IScrollHandler;
        }

        image.raycastTarget = receivesInput;
        image.type = image.sprite != null && image.sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
    }

    private static void TextStyle(Transform t, float min, float max, bool wrap, TextOverflowModes overflow, TextAlignmentOptions alignment, Vector4 margin)
    {
        if (t == null) return;
        TMP_Text text = t.GetComponent<TMP_Text>();
        if (text == null) return;
        text.enableAutoSizing = true;
        text.fontSizeMin = min;
        text.fontSizeMax = max;
        text.enableWordWrapping = wrap;
        text.overflowMode = overflow;
        text.alignment = alignment;
        text.margin = margin;
        text.color = Ivory;
        text.raycastTarget = false;
    }

    private static void RemoveLayout<T>(Transform t) where T : Component
    {
        if (t == null) return;
        T component = t.GetComponent<T>();
        if (component != null) Object.DestroyImmediate(component, true);
    }

    private static LayoutElement Layout(Transform t, float width, float height, bool ignore)
    {
        if (t == null) return null;
        LayoutElement element = t.GetComponent<LayoutElement>();
        if (element == null) element = t.gameObject.AddComponent<LayoutElement>();
        element.ignoreLayout = ignore;
        element.preferredWidth = width;
        element.preferredHeight = height;
        element.flexibleWidth = 0f;
        element.flexibleHeight = 0f;
        return element;
    }

    private static void FixPrefabs()
    {
        FixPrefab("Assets/Prefabs/ItemUI.prefab", delegate(GameObject root) { RefineItemCard(root.transform); });
        FixPrefab("Assets/Prefabs/PokemonSlotUI.prefab", delegate(GameObject root) { RefinePokemonSlot(root.transform); });
        FixPrefab("Assets/Prefabs/Inventory.prefab", delegate(GameObject root) { RefineInventory(root.transform); });
        FixPrefab("Assets/Prefabs/PauseManager.prefab", delegate(GameObject root) { RefinePause(root.transform); });
    }

    private static void FixPrefab(string path, System.Action<GameObject> action)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            action(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void RefineItemCard(Transform root)
    {
        RectTransform rr = root as RectTransform;
        if (rr != null)
        {
            rr.sizeDelta = new Vector2(100f, 100f);
            rr.localScale = Vector3.one;
        }
        ImageStyle(root, UiRoot + "Inventory/item_slot.png", Color.white, false);
        Transform border = Child(root, "Border");
        Stretch(border as RectTransform, 4f, 4f, 4f, 4f);
        ImageStyle(border, UiRoot + "Core/selection_frame.png", Color.white, false);
        RemoveLayout<HorizontalLayoutGroup>(border);

        Transform icon = Child(root, "Border/Image");
        Fixed(icon as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(68f, 68f));
        ImageStyle(icon, null, Color.white, true);

        Transform badge = Child(root, "Border/Image/TxtBackground");
        Fixed(badge as RectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-1f, 1f), new Vector2(34f, 24f));
        ImageStyle(badge, UiRoot + "Core/status_badge.png", Color.white, false);
        RemoveLayout<HorizontalLayoutGroup>(badge);

        Transform quantity = Child(root, "Border/Image/TxtBackground/Text (TMP)");
        Stretch(quantity as RectTransform, 3f, 2f, 3f, 2f);
        TextStyle(quantity, 12f, 19f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.Center, Vector4.zero);
    }

private static void RefinePokemonSlot(Transform root)
    {
        RectTransform rr = root as RectTransform;
        if (rr != null)
        {
            rr.sizeDelta = new Vector2(155f, 92f);
            rr.localScale = Vector3.one;
        }
        RemoveLayout<VerticalLayoutGroup>(root);
        ImageStyle(root, UiRoot + "Inventory/uvgmon_slot.png", Color.white, false);

        Transform border = Child(root, "Border");
        Stretch(border as RectTransform, 4f, 4f, 4f, 4f);
        ImageStyle(border, UiRoot + "Core/selection_frame.png", Color.white, false);

        Transform icon = Child(root, "Icon");
        Fixed(icon as RectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(58f, 58f));
        ImageStyle(icon, null, Color.white, true);

        Transform name = Child(root, "NameTxt");
        Fixed(name as RectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(69f, 13f), new Vector2(76f, 24f));
        TextStyle(name, 8f, 14f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.MidlineLeft, new Vector4(2f, 0f, 3f, 0f));

        Transform hp = Child(root, "HpTxt");
        Fixed(hp as RectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(69f, -17f), new Vector2(76f, 22f));
        TextStyle(hp, 8f, 12f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.MidlineLeft, new Vector4(2f, 0f, 3f, 0f));
        TMP_Text hpText = hp != null ? hp.GetComponent<TMP_Text>() : null;
        if (hpText != null) hpText.color = CrystalGreen;
    }

    private static void RefineInventory(Transform root)
    {
        RectTransform rr = root as RectTransform;
        if (rr != null)
        {
            rr.sizeDelta = new Vector2(700f, 350f);
            rr.localScale = Vector3.one;
        }
        RemoveLayout<HorizontalLayoutGroup>(root);
        ImageStyle(root, UiRoot + "Core/panel_base.png", Color.white, false);

        Transform content = Child(root, "InventoryContent");
        Fixed(content as RectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(240f, -175f), new Vector2(450f, 320f));
        ImageStyle(content, UiRoot + "Core/panel_base.png", Color.white, false);

        Transform scroll = Child(content, "Scroll View");
        Stretch(scroll as RectTransform, 10f, 10f, 10f, 10f);
        ImageStyle(scroll, null, Color.clear, false);

        Transform viewport = Child(content, "Scroll View/Viewport");
        Stretch(viewport as RectTransform, 8f, 8f, 26f, 8f);
        ImageStyle(viewport, null, Color.clear, false);
        RemoveLayout<HorizontalLayoutGroup>(viewport);
        if (viewport != null && viewport.GetComponent<RectMask2D>() == null) viewport.gameObject.AddComponent<RectMask2D>();

        Transform gridTransform = Child(content, "Scroll View/Viewport/Content");
        if (gridTransform != null)
        {
            RectTransform gridRect = gridTransform as RectTransform;
            gridRect.anchorMin = new Vector2(0f, 1f);
            gridRect.anchorMax = new Vector2(1f, 1f);
            gridRect.pivot = new Vector2(0f, 1f);
            gridRect.anchoredPosition = Vector2.zero;
            gridRect.sizeDelta = new Vector2(0f, 300f);
            gridRect.localScale = Vector3.one;
            GridLayoutGroup grid = gridTransform.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                grid.cellSize = new Vector2(96f, 96f);
                grid.spacing = new Vector2(8f, 8f);
                grid.padding = new RectOffset(8, 8, 8, 8);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 4;

                ContentSizeFitter fitter = gridTransform.GetComponent<ContentSizeFitter>();
                if (fitter == null) fitter = gridTransform.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        Transform scrollbar = Child(content, "Scroll View/Scrollbar Vertical");
        if (scrollbar != null) ImageStyle(scrollbar, UiRoot + "Core/scrollbar_track.png", Color.white, false);
        Transform handle = Child(content, "Scroll View/Scrollbar Vertical/Sliding Area/Handle");
        if (handle != null) ImageStyle(handle, UiRoot + "Core/scrollbar_thumb.png", Color.white, false);

        Transform description = Child(root, "InventoryDescription");
        Fixed(description as RectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(580f, -175f), new Vector2(210f, 320f));
        RemoveLayout<VerticalLayoutGroup>(description);
        ImageStyle(description, UiRoot + "Core/panel_base.png", Color.white, false);

        Transform imagePanel = Child(description, "ImagePanel");
        Fixed(imagePanel as RectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(105f, -68f), new Vector2(190f, 112f));
        ImageStyle(imagePanel, UiRoot + "Inventory/uvgmon_slot.png", Color.white, false);

        Transform imageBorder = Child(description, "ImagePanel/ImageBorder");
        Fixed(imageBorder as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(92f, 92f));
        RemoveLayout<HorizontalLayoutGroup>(imageBorder);
        ImageStyle(imageBorder, UiRoot + "Core/selection_frame.png", Color.white, false);

        Transform itemImage = Child(description, "ImagePanel/ImageBorder/Image");
        Fixed(itemImage as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(72f, 72f));
        ImageStyle(itemImage, null, Color.white, true);

        Transform descriptionPanel = Child(description, "DescriptionPanel");
        Fixed(descriptionPanel as RectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(105f, -222f), new Vector2(190f, 176f));
        RemoveLayout<VerticalLayoutGroup>(descriptionPanel);
        ImageStyle(descriptionPanel, UiRoot + "Core/panel_base.png", Color.white, false);

        Transform title = Child(descriptionPanel, "TitleTxt");
        Fixed(title as RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -13f), new Vector2(166f, 34f));
        TextStyle(title, 14f, 26f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.Center, new Vector4(4f, 2f, 4f, 2f));

        Transform body = Child(descriptionPanel, "DescriptionTxt");
        Fixed(body as RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(162f, 116f));
        TextStyle(body, 11f, 17f, true, TextOverflowModes.Ellipsis, TextAlignmentOptions.TopLeft, new Vector4(4f, 4f, 4f, 4f));
    }

    private static void RefinePause(Transform root)
    {
        Transform window = Child(root, "Canvas/PauseOverlay/PauseWindow");
        if (window == null) return;
        Fixed(window as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 420f));
        ImageStyle(window, UiRoot + "Core/panel_base.png", Color.white, false);
        VerticalLayoutGroup layout = window.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.padding = new RectOffset(42, 42, 34, 34);
            layout.spacing = 14f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        Transform title = Child(window, "Title");
        Layout(title, 420f, 50f, false);
        TextStyle(title, 25f, 35f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.Center, new Vector4(8f, 2f, 8f, 2f));

        string[] buttons = { "Button_Resume", "Button_Restart", "Button_MainMenu" };
        for (int i = 0; i < buttons.Length; i++)
        {
            Transform button = Child(window, buttons[i]);
            Layout(button, 410f, 68f, false);
            ImageStyle(button, UiRoot + "Core/button_normal.png", Color.white, false);
            Transform label = Child(button, "Text (TMP)");
            Stretch(label as RectTransform, 16f, 8f, 16f, 8f);
            TextStyle(label, 16f, 24f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.Center, Vector4.zero);
        }
    }

    private static void FixScenes()
    {
        Scene current = SceneManager.GetActiveScene();
        FixScenePath("Assets/Scenes/jardinconocimiento.unity", true);
        FixScenePath("Assets/InventoryScene.unity", false);
        if (current.IsValid() && current.path != SceneManager.GetActiveScene().path)
            EditorSceneManager.SetActiveScene(current);
    }

    private static void FixScenePath(string path, bool primary)
    {
        Scene scene = SceneManager.GetSceneByPath(path);
        bool opened = scene.IsValid() && scene.isLoaded;
        if (!opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        if (!scene.IsValid()) return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform root = roots[i].transform;
            if (root.name == "OverworldUI")
            {
                RefineOverworld(root);
                RefineDialogue(root);
            }
            if (root.name == "BattleUI") RefineBattle(root);

            Transform inventory = root.name == "Inventory" ? root : Child(root, "Inventory");
            if (inventory != null) RefineInventory(inventory);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (!opened && !primary) EditorSceneManager.CloseScene(scene, true);
    }

private static void RefineOverworld(Transform root)
    {
        Transform menu = Child(root, "InGameMenu");
        if (menu == null) return;
        ImageStyle(menu, UiRoot + "Core/panel_base.png", Color.white, false);
        RemoveLayout<HorizontalLayoutGroup>(menu);

        Transform tabs = Child(menu, "TabBar");
        AnchoredRect(tabs as RectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(10f, 16f), new Vector2(160f, -16f));
        ImageStyle(tabs, UiRoot + "Core/panel_base.png", Color.white, false);
        RemoveLayout<VerticalLayoutGroup>(tabs);

        SetMenuTab(Child(tabs, "ItemsTabButton"), new Vector2(75f, -45f), true);
        SetMenuTab(Child(tabs, "PokemonTabButton"), new Vector2(75f, -112f), false);

        Transform inventory = Child(menu, "Inventory");
        if (inventory != null)
        {
            RefineInventory(inventory);
            FitEmbeddedInventory(inventory);
        }

        Transform pokemon = Child(menu, "PokemonPanel");
        if (pokemon != null)
        {
            RefinePokemonPanel(pokemon);
            FitEmbeddedPokemon(pokemon);
        }

        Transform mouse = Child(menu, "MouseFollower");
        if (mouse != null) RefineItemCard(mouse);
    }

private static void SetMenuTab(Transform button, Vector2 position, bool active)
    {
        if (button == null) return;
        Fixed(button as RectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), position, new Vector2(132f, 52f));
        ImageStyle(button, UiRoot + (active ? "Core/tab_active.png" : "Core/tab_inactive.png"), Color.white, false);
        Transform label = Child(button, "Text");
        Stretch(label as RectTransform, 10f, 7f, 10f, 7f);
        TextStyle(label, 11f, 19f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.Center, Vector4.zero);
    }

private static void FitEmbeddedInventory(Transform root)
    {
        AnchoredRect(root as RectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(180f, 16f), new Vector2(-10f, -16f));

        Transform content = Child(root, "InventoryContent");
        AnchoredRect(content as RectTransform, new Vector2(0f, 0f), new Vector2(0.66f, 1f), new Vector2(10f, 10f), new Vector2(-5f, -10f));
        Transform gridTransform = Child(content, "Scroll View/Viewport/Content");
        GridLayoutGroup grid = gridTransform != null ? gridTransform.GetComponent<GridLayoutGroup>() : null;
        if (grid != null)
        {
            grid.cellSize = new Vector2(96f, 96f);
            grid.spacing = new Vector2(8f, 8f);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.cellSize = new Vector2(92f, 86f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(grid);
        }

        Transform description = Child(root, "InventoryDescription");
        AnchoredRect(description as RectTransform, new Vector2(0.66f, 0f), new Vector2(1f, 1f), new Vector2(5f, 10f), new Vector2(-10f, -10f));

        Transform imagePanel = Child(description, "ImagePanel");
        AnchoredRect(imagePanel as RectTransform, new Vector2(0f, 0.62f), new Vector2(1f, 1f), new Vector2(10f, 6f), new Vector2(-10f, -10f));

        Transform imageBorder = Child(imagePanel, "ImageBorder");
        Stretch(imageBorder as RectTransform, 18f, 14f, 18f, 14f);
        Transform itemImage = Child(imageBorder, "Image");
        Stretch(itemImage as RectTransform, 8f, 8f, 8f, 8f);

        Transform descriptionPanel = Child(description, "DescriptionPanel");
        AnchoredRect(descriptionPanel as RectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.62f), new Vector2(10f, 10f), new Vector2(-10f, -4f));

        Transform title = Child(descriptionPanel, "TitleTxt");
        Fixed(title as RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(146f, 34f));
        TextStyle(title, 12f, 23f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.Center, new Vector4(4f, 2f, 4f, 2f));

        Transform body = Child(descriptionPanel, "DescriptionTxt");
        Stretch(body as RectTransform, 12f, 12f, 12f, 50f);
        TextStyle(body, 10f, 15f, true, TextOverflowModes.Ellipsis, TextAlignmentOptions.TopLeft, new Vector4(3f, 3f, 3f, 3f));
    }

private static void FitEmbeddedPokemon(Transform panel)
    {
        AnchoredRect(panel as RectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(180f, 16f), new Vector2(-10f, -16f));

        Transform content = Child(panel, "PokemonContent");
        AnchoredRect(content as RectTransform, new Vector2(0f, 0f), new Vector2(0.66f, 1f), new Vector2(10f, 10f), new Vector2(-5f, -10f));

        Transform gridTransform = Child(content, "Scroll View/Viewport/Content");
        GridLayoutGroup grid = gridTransform != null ? gridTransform.GetComponent<GridLayoutGroup>() : null;
        if (grid != null)
        {
            grid.cellSize = new Vector2(155f, 92f);
            grid.spacing = new Vector2(8f, 8f);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            PrefabUtility.RecordPrefabInstancePropertyModifications(grid);
        }

        Transform details = Child(panel, "PokemonDetails");
        AnchoredRect(details as RectTransform, new Vector2(0.66f, 0f), new Vector2(1f, 1f), new Vector2(5f, 10f), new Vector2(-10f, -10f));
        ImageStyle(details, UiRoot + "Core/panel_base.png", Color.white, false);

        Transform imagePanel = Child(details, "DetailsImagePanel");
        AnchoredRect(imagePanel as RectTransform, new Vector2(0f, 0.54f), new Vector2(1f, 1f), new Vector2(10f, 6f), new Vector2(-10f, -10f));
        Transform detailsImage = Child(imagePanel, "DetailsImage");
        Stretch(detailsImage as RectTransform, 22f, 16f, 22f, 16f);

        Transform title = Child(details, "DetailsTitleTxt");
        AnchoredRect(title as RectTransform, new Vector2(0f, 0.39f), new Vector2(1f, 0.54f), new Vector2(10f, 2f), new Vector2(-10f, -2f));
        TextStyle(title, 12f, 23f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.Center, new Vector4(4f, 2f, 4f, 2f));

        Transform stats = Child(details, "DetailsStatsTxt");
        AnchoredRect(stats as RectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.39f), new Vector2(10f, 10f), new Vector2(-10f, -2f));
        TextStyle(stats, 10f, 15f, true, TextOverflowModes.Ellipsis, TextAlignmentOptions.TopLeft, new Vector4(5f, 4f, 5f, 4f));
    }



    private static void RefinePokemonPanel(Transform panel)
    {
        Fixed(panel as RectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(540f, -225f), new Vector2(700f, 350f));
        RemoveLayout<HorizontalLayoutGroup>(panel);
        ImageStyle(panel, UiRoot + "Core/panel_base.png", Color.white, false);

        Transform content = Child(panel, "PokemonContent");
        Fixed(content as RectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(225f, -175f), new Vector2(420f, 320f));
        ImageStyle(content, UiRoot + "Core/panel_base.png", Color.white, false);

        Transform scroll = Child(content, "Scroll View");
        Stretch(scroll as RectTransform, 10f, 10f, 10f, 10f);
        ImageStyle(scroll, null, Color.clear, false);
        Transform viewport = Child(content, "Scroll View/Viewport");
        Stretch(viewport as RectTransform, 8f, 8f, 26f, 8f);
        ImageStyle(viewport, null, Color.clear, false);
        if (viewport != null && viewport.GetComponent<RectMask2D>() == null) viewport.gameObject.AddComponent<RectMask2D>();

        Transform gridTransform = Child(content, "Scroll View/Viewport/Content");
        GridLayoutGroup grid = gridTransform != null ? gridTransform.GetComponent<GridLayoutGroup>() : null;
        if (grid != null)
        {
            grid.cellSize = new Vector2(170f, 92f);
                grid.spacing = new Vector2(8f, 8f);
                grid.padding = new RectOffset(8, 8, 8, 8);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 2;
        }

        Transform scrollbar = Child(content, "Scroll View/Scrollbar Vertical");
        ImageStyle(scrollbar, UiRoot + "Core/scrollbar_track.png", Color.white, false);
        Transform handle = Child(content, "Scroll View/Scrollbar Vertical/Sliding Area/Handle");
        ImageStyle(handle, UiRoot + "Core/scrollbar_thumb.png", Color.white, false);

        Transform details = Child(panel, "PokemonDetails");
        Fixed(details as RectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(565f, -175f), new Vector2(240f, 320f));
        RemoveLayout<VerticalLayoutGroup>(details);

        Transform imagePanel = Child(details, "DetailsImagePanel");
        Fixed(imagePanel as RectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(120f, -84f), new Vector2(220f, 140f));
        ImageStyle(imagePanel, UiRoot + "Inventory/uvgmon_slot.png", Color.white, false);
        Transform detailsImage = Child(imagePanel, "DetailsImage");
        Stretch(detailsImage as RectTransform, 26f, 18f, 26f, 18f);
        ImageStyle(detailsImage, null, Color.white, true);

        Transform title = Child(details, "DetailsTitleTxt");
        Fixed(title as RectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(120f, -174f), new Vector2(212f, 42f));
        TextStyle(title, 15f, 27f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.Center, new Vector4(6f, 2f, 6f, 2f));

        Transform stats = Child(details, "DetailsStatsTxt");
        Fixed(stats as RectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(120f, -249f), new Vector2(208f, 96f));
        TextStyle(stats, 11f, 17f, true, TextOverflowModes.Ellipsis, TextAlignmentOptions.TopLeft, new Vector4(8f, 4f, 8f, 4f));
    }

private static void RefineDialogue(Transform root)
    {
        Transform box = Child(root, "DialogBox");
        if (box == null) return;
        Transform oldBorder = Child(box, "Border");
        if (oldBorder != null) oldBorder.gameObject.SetActive(false);
        Transform oldBackground = Child(box, "BG");
        if (oldBackground != null) oldBackground.gameObject.SetActive(false);

        Transform elements = Child(box, "DialogElements");
        Fixed(elements as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 86f), new Vector2(740f, 145f));
        Transform panel = Child(elements, "PixelCampusBackground");
        Stretch(panel as RectTransform, 0f, 0f, 0f, 0f);
        ImageStyle(panel, UiRoot + "Combat/message_panel.png", Color.white, false);
        Layout(panel, 0f, 0f, true);

        Transform nameRoot = Child(elements, "Name");
        Stretch(nameRoot as RectTransform, 0f, 0f, 0f, 0f);
        Transform nameBackground = Child(nameRoot, "BGName");
        Fixed(nameBackground as RectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -8f), new Vector2(168f, 34f));
        ImageStyle(nameBackground, UiRoot + "Core/title_bar.png", Color.white, false);
        Transform name = Child(nameRoot, "TextName");
        Fixed(name as RectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -10f), new Vector2(148f, 28f));
        TextStyle(name, 13f, 21f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.MidlineLeft, new Vector4(3f, 0f, 3f, 0f));

        Transform dialogText = Child(elements, "DialogText");
        Stretch(dialogText as RectTransform, 22f, 40f, 22f, 38f);
        TextStyle(dialogText, 13f, 20f, true, TextOverflowModes.Ellipsis, TextAlignmentOptions.TopLeft, new Vector4(3f, 3f, 3f, 3f));

        Transform button = Child(elements, "DialogBtn");
        Fixed(button as RectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 18f), new Vector2(132f, 28f));
        ImageStyle(button, UiRoot + "Core/button_normal.png", Color.white, false);
        Transform label = Child(button, "Text (TMP)");
        Stretch(label as RectTransform, 8f, 3f, 8f, 3f);
        TextStyle(label, 10f, 16f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.Center, Vector4.zero);
    }

    private static void RefineBattle(Transform battle)
    {
        Transform background = Child(battle, "bgbattle");
        Stretch(background as RectTransform, 0f, 0f, 0f, 0f);
        ImageStyle(background, BattleBackground, Color.white, false);

        Transform player = Child(battle, "jackmon");
        Fixed(player as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-535f, -40f), new Vector2(330f, 280f));
        ImageStyle(player, null, Color.white, true);

        Transform enemy = Child(battle, "jackmalo");
        Fixed(enemy as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(475f, 195f), new Vector2(300f, 240f));
        ImageStyle(enemy, null, Color.white, true);

        Transform bodyParts = Child(battle, "EnemyBodyPartsContainer");
        Fixed(bodyParts as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(475f, 195f), new Vector2(300f, 240f));

        Transform playerStatus = Child(battle, "PixelCampusPlayerStatus");
        Fixed(playerStatus as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-535f, -300f), new Vector2(390f, 104f));
        ImageStyle(playerStatus, UiRoot + "Combat/battle_status_panel.png", Color.white, false);

        Transform playerName = Child(player, "playerNameLevel");
        if (playerName != null)
        {
            playerName.SetParent(playerStatus, false);
            Fixed(playerName as RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -13f), new Vector2(344f, 34f));
            TextStyle(playerName, 17f, 26f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.MidlineLeft, new Vector4(8f, 0f, 8f, 0f));
        }

        Transform playerHealth = Child(player, "playerhealth");
        if (playerHealth == null) playerHealth = Child(playerStatus, "playerhealth");
        if (playerHealth != null)
        {
            playerHealth.SetParent(playerStatus, false);
            Fixed(playerHealth as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(344f, 40f));
            TextStyle(playerHealth, 20f, 32f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.MidlineLeft, new Vector4(8f, 0f, 8f, 0f));
            TMP_Text hp = playerHealth.GetComponent<TMP_Text>();
            if (hp != null) hp.color = CrystalGreen;
        }

        Transform enemyStatus = Child(battle, "PixelCampusEnemyStatus");
        Fixed(enemyStatus as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(475f, 360f), new Vector2(390f, 96f));
        ImageStyle(enemyStatus, UiRoot + "Combat/battle_status_panel.png", Color.white, false);

        Transform enemyHealth = Child(enemy, "enemyhealth");
        if (enemyHealth == null) enemyHealth = Child(enemyStatus, "enemyhealth");
        if (enemyHealth != null)
        {
            enemyHealth.SetParent(enemyStatus, false);
            Fixed(enemyHealth as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(344f, 52f));
            TextStyle(enemyHealth, 20f, 31f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.Center, new Vector4(8f, 0f, 8f, 0f));
            TMP_Text hp = enemyHealth.GetComponent<TMP_Text>();
            if (hp != null) hp.color = CrystalGreen;
        }

        Transform target = Child(battle, "TargetIndicatorText");
        Fixed(target as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(250f, -140f), new Vector2(800f, 42f));
        TextStyle(target, 16f, 25f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.Center, new Vector4(12f, 0f, 12f, 0f));

        RefineBattleTabs(Child(battle, "CombatTabBar"));

        RefineCommandPanel(Child(battle, "MoveOptionsContainer"), new Vector2(250f, -365f), new Vector2(800f, 230f));
        RefineCommandPanel(Child(battle, "InventoryPanel"), new Vector2(250f, -365f), new Vector2(800f, 230f));
        RefineCommandPanel(Child(battle, "TeamPanel"), new Vector2(250f, -365f), new Vector2(800f, 230f));

        Transform moveTemplate = Child(battle, "MoveOptionTemplate");
        if (moveTemplate != null)
        {
            Fixed(moveTemplate as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 48f));
            Layout(moveTemplate, 760f, 48f, false);
            ImageStyle(moveTemplate, UiRoot + "Core/button_normal.png", Color.white, false);
            Transform label = Child(moveTemplate, "Label");
            Stretch(label as RectTransform, 14f, 6f, 14f, 6f);
            TextStyle(label, 15f, 23f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.Center, Vector4.zero);
        }

        RefineCombatInventorySlot(Child(battle, "CombatInventorySlotTemplate"));
        RefineCombatTeamSlot(Child(battle, "CombatTeamSlotTemplate"));
        RefineTeamActions(Child(battle, "TeamPanel"));

        Transform messagePanel = Child(battle, "PixelCampusMessagePanel");
        Fixed(messagePanel as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -503f), new Vector2(1760f, 70f));
        ImageStyle(messagePanel, UiRoot + "Combat/message_panel.png", Color.white, false);

        Transform message = Child(battle, "BattleMessageText");
        Fixed(message as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -503f), new Vector2(1680f, 48f));
        TextStyle(message, 17f, 28f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.MidlineLeft, new Vector4(18f, 0f, 18f, 0f));

        Transform follower = Child(battle, "BattleMouseFollower");
        if (follower != null) RefineItemCard(follower);
    }

    private static void RefineBattleTabs(Transform tabs)
    {
        if (tabs == null) return;
        Fixed(tabs as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(250f, -190f), new Vector2(720f, 56f));
        RemoveLayout<HorizontalLayoutGroup>(tabs);
        string[] names = { "AttacksTabButton", "InventoryTabButton", "TeamTabButton" };
        float[] xs = { -240f, 0f, 240f };
        for (int i = 0; i < names.Length; i++)
        {
            Transform button = Child(tabs, names[i]);
            Fixed(button as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(xs[i], 0f), new Vector2(220f, 52f));
            ImageStyle(button, UiRoot + (i == 0 ? "Core/tab_active.png" : "Core/tab_inactive.png"), Color.white, false);
            Transform label = Child(button, "Label");
            Stretch(label as RectTransform, 12f, 6f, 12f, 6f);
            TextStyle(label, 14f, 22f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.Center, Vector4.zero);
        }
    }

private static void RefineCommandPanel(Transform panel, Vector2 position, Vector2 size)
    {
        if (panel == null) return;
        Fixed(panel as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
        ImageStyle(panel, UiRoot + "Core/panel_base.png", Color.white, false);

        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.padding = new RectOffset(16, 16, 14, 14);
            layout.spacing = 5f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        Transform background = Child(panel, "PixelCampusBackground");
        if (background != null)
        {
            Image image = background.GetComponent<Image>();
            if (image != null) image.color = Color.clear;
            Layout(background, 0f, 0f, true);
            background.gameObject.SetActive(false);
        }

        Transform empty = Child(panel, "EmptyLabel");
        if (empty != null)
        {
            Layout(empty, 740f, 50f, false);
            TextStyle(empty, 14f, 21f, true, TextOverflowModes.Ellipsis, TextAlignmentOptions.Center, new Vector4(10f, 4f, 10f, 4f));
        }
    }

    private static void RefineCombatInventorySlot(Transform slot)
    {
        if (slot == null) return;
        Fixed(slot as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 26f));
        Layout(slot, 760f, 26f, false);
        RemoveLayout<HorizontalLayoutGroup>(slot);
        ImageStyle(slot, UiRoot + "Inventory/item_slot.png", Color.white, false);

        Transform icon = Child(slot, "Icon");
        Fixed(icon as RectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(22f, 22f));
        ImageStyle(icon, null, Color.white, true);

        Transform name = Child(slot, "NameText");
        Fixed(name as RectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(42f, 0f), new Vector2(610f, 22f));
        TextStyle(name, 12f, 18f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.MidlineLeft, new Vector4(2f, 0f, 6f, 0f));

        Transform quantity = Child(slot, "QuantityText");
        Fixed(quantity as RectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(74f, 22f));
        TextStyle(quantity, 12f, 18f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.MidlineRight, new Vector4(4f, 0f, 4f, 0f));
    }

    private static void RefineCombatTeamSlot(Transform slot)
    {
        if (slot == null) return;
        Fixed(slot as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 60f));
        Layout(slot, 760f, 60f, false);
        RemoveLayout<HorizontalLayoutGroup>(slot);
        ImageStyle(slot, UiRoot + "Inventory/uvgmon_slot.png", Color.white, false);

        Transform marker = Child(slot, "ActiveMarker");
        Stretch(marker as RectTransform, 3f, 3f, 3f, 3f);
        ImageStyle(marker, UiRoot + "Core/selection_frame.png", Color.white, false);

        Transform icon = Child(slot, "Icon");
        Fixed(icon as RectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(52f, 52f));
        ImageStyle(icon, null, Color.white, true);

        Transform info = Child(slot, "Info");
        Fixed(info as RectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(76f, 0f), new Vector2(666f, 50f));
        RemoveLayout<VerticalLayoutGroup>(info);

        Transform header = Child(info, "Header");
        Fixed(header as RectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(666f, 26f));
        RemoveLayout<HorizontalLayoutGroup>(header);

        Transform name = Child(header, "NameText");
        Fixed(name as RectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(470f, 24f));
        TextStyle(name, 12f, 19f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.MidlineLeft, new Vector4(2f, 0f, 6f, 0f));

        Transform hpText = Child(header, "HpText");
        Fixed(hpText as RectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(180f, 24f));
        TextStyle(hpText, 11f, 17f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.MidlineRight, new Vector4(4f, 0f, 4f, 0f));
        TMP_Text hpLabel = hpText != null ? hpText.GetComponent<TMP_Text>() : null;
        if (hpLabel != null) hpLabel.color = CrystalGreen;

        Transform bar = Child(info, "HpBarBackground");
        Fixed(bar as RectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, new Vector2(666f, 14f));
        ImageStyle(bar, null, new Color(0.01f, 0.04f, 0.07f, 0.95f), false);
        Transform fill = Child(bar, "HpBarFill");
        Stretch(fill as RectTransform, 2f, 2f, 2f, 2f);
        ImageStyle(fill, UiRoot + "Combat/hp_fill.png", Color.white, false);
    }

    private static void RefineTeamActions(Transform panel)
    {
        if (panel == null) return;
        Transform status = Child(panel, "SwitchStatus");
        if (status != null)
        {
            Layout(status, 760f, 28f, false);
            TextStyle(status, 12f, 18f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.Center, new Vector4(8f, 0f, 8f, 0f));
        }

        Transform actions = Child(panel, "SwitchActions");
        if (actions == null) return;
        Layout(actions, 760f, 42f, false);
        RemoveLayout<HorizontalLayoutGroup>(actions);
        Fixed(actions as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 42f));

        string[] names = { "ConfirmSwitchButton", "CancelSwitchButton" };
        float[] xs = { -192f, 192f };
        for (int i = 0; i < names.Length; i++)
        {
            Transform button = Child(actions, names[i]);
            Fixed(button as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(xs[i], 0f), new Vector2(368f, 40f));
            ImageStyle(button, UiRoot + "Core/button_normal.png", Color.white, false);
            Transform label = Child(button, "Label");
            Stretch(label as RectTransform, 12f, 5f, 12f, 5f);
            TextStyle(label, 13f, 19f, false, TextOverflowModes.Ellipsis, TextAlignmentOptions.Center, Vector4.zero);
        }
    }
}
