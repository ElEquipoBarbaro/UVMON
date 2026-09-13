using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PixelCampusAssetBuilder
{
    private static readonly Color32 Void = new Color32(0, 0, 0, 0);
    private static readonly Color32 Outer = new Color32(7, 22, 31, 255);
    private static readonly Color32 Navy = new Color32(16, 42, 58, 255);
    private static readonly Color32 Teal = new Color32(23, 75, 83, 255);
    private static readonly Color32 Green = new Color32(25, 122, 88, 255);
    private static readonly Color32 Fresh = new Color32(53, 185, 121, 255);
    private static readonly Color32 Cyan = new Color32(75, 183, 200, 255);
    private static readonly Color32 Muted = new Color32(143, 184, 180, 255);
    private static readonly Color32 Ivory = new Color32(243, 241, 215, 255);

[MenuItem("Tools/UVGMon/Build Pixel Campus Assets")]
    public static void BuildAll()
    {
        LiquidCrystalUIRefiner.ApplyAll();
    }

    private static Texture2D NewTexture(int width, int height)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color32[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Void;
        texture.SetPixels32(pixels);
        return texture;
    }

    private static void Rect(Texture2D texture, int x, int y, int width, int height, Color32 color)
    {
        int x0 = Mathf.Clamp(x, 0, texture.width);
        int y0 = Mathf.Clamp(y, 0, texture.height);
        int x1 = Mathf.Clamp(x + width, 0, texture.width);
        int y1 = Mathf.Clamp(y + height, 0, texture.height);
        for (int py = y0; py < y1; py++)
            for (int px = x0; px < x1; px++)
                texture.SetPixel(px, py, color);
    }

    private static void StepRect(Texture2D texture, int x, int y, int width, int height, int cut, Color32 color)
    {
        int safeCut = Mathf.Max(0, Mathf.Min(cut, Mathf.Min(width, height) / 3));
        Rect(texture, x + safeCut, y, width - safeCut * 2, height, color);
        Rect(texture, x, y + safeCut, width, height - safeCut * 2, color);
    }

    private static void Frame(Texture2D texture, int x, int y, int width, int height, int cut, Color32 fill, Color32 accent)
    {
        StepRect(texture, x, y, width, height, cut, Outer);
        StepRect(texture, x + 4, y + 4, width - 8, height - 8, Mathf.Max(2, cut - 2), accent);
        StepRect(texture, x + 8, y + 8, width - 16, height - 16, Mathf.Max(2, cut - 4), Teal);
        StepRect(texture, x + 12, y + 12, width - 24, height - 24, Mathf.Max(2, cut - 6), fill);
    }

    private static void CornerKeys(Texture2D t, int x, int y, int w, int h)
    {
        Rect(t, x + 12, y + h - 20, 24, 8, Fresh);
        Rect(t, x + 12, y + h - 28, 8, 16, Green);
        Rect(t, x + w - 36, y + 12, 24, 8, Green);
        Rect(t, x + w - 20, y + 12, 8, 16, Fresh);
    }

    private static void SavePng(Texture2D texture, string path)
    {
        texture.Apply(false, false);
        string absolute = ProjectRoot(path);
        Directory.CreateDirectory(Path.GetDirectoryName(absolute));
        File.WriteAllBytes(absolute, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static void SaveTga(Texture2D texture, string path)
    {
        texture.Apply(false, false);
        string absolute = ProjectRoot(path);
        Directory.CreateDirectory(Path.GetDirectoryName(absolute));
        File.WriteAllBytes(absolute, texture.EncodeToTGA());
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static string ProjectRoot(string assetPath)
    {
        return Path.Combine(Application.dataPath.Substring(0, Application.dataPath.Length - 6), assetPath);
    }

    private static void ConfigureSprite(string path, Vector4 border)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.sRGBTexture = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.spritePixelsPerUnit = 100f;
        importer.spriteBorder = border;
        importer.SaveAndReimport();
    }

    private static void BuildPanel()
    {
        var t = NewTexture(1024, 576);
        Frame(t, 8, 8, 1008, 560, 20, Navy, Cyan);
        Rect(t, 28, 540, 968, 4, Teal);
        Rect(t, 28, 32, 968, 4, Outer);
        CornerKeys(t, 8, 8, 1008, 560);
        SavePng(t, "Assets/UI/PixelCampus/Sprites/Core/panel_base.png");
    }

    private static void BuildTitle()
    {
        var t = NewTexture(1024, 104);
        Frame(t, 4, 4, 1016, 96, 14, Navy, Cyan);
        Rect(t, 32, 80, 960, 4, Muted);
        Rect(t, 32, 16, 80, 4, Fresh);
        Rect(t, 912, 16, 80, 4, Green);
        SavePng(t, "Assets/UI/PixelCampus/Sprites/Core/title_bar.png");
    }

    private static Texture2D Button(Color32 fill, Color32 top, int insetY)
    {
        var t = NewTexture(768, 96);
        Frame(t, 4, 4, 760, 88, 14, fill, Teal);
        Rect(t, 28, 72 - insetY, 712, 4, top);
        Rect(t, 28, 16 - insetY, 56, 4, Green);
        Rect(t, 684, 16 - insetY, 56, 4, Green);
        return t;
    }

    private static void BuildButtons()
    {
        SavePng(Button(Navy, Cyan, 0), "Assets/UI/PixelCampus/Sprites/Core/button_normal.png");
        SavePng(Button(Green, Fresh, 0), "Assets/UI/PixelCampus/Sprites/Core/button_hover.png");
        SavePng(Button(new Color32(18, 92, 68, 255), Green, 4), "Assets/UI/PixelCampus/Sprites/Core/button_pressed.png");
        SavePng(Button(new Color32(30, 50, 59, 255), new Color32(70, 92, 94, 255), 0), "Assets/UI/PixelCampus/Sprites/Core/button_disabled.png");
    }

    private static void BuildTabs()
    {
        var active = NewTexture(512, 96);
        Frame(active, 4, 8, 504, 84, 14, Green, Teal);
        Rect(active, 28, 76, 456, 4, Fresh);
        Rect(active, 184, 4, 144, 8, Green);
        SavePng(active, "Assets/UI/PixelCampus/Sprites/Core/tab_active.png");

        var inactive = NewTexture(512, 96);
        Frame(inactive, 4, 8, 504, 84, 14, Navy, Teal);
        Rect(inactive, 28, 76, 456, 4, Muted);
        Rect(inactive, 208, 4, 96, 4, Teal);
        SavePng(inactive, "Assets/UI/PixelCampus/Sprites/Core/tab_inactive.png");
    }

    private static void BuildSlots()
    {
        var item = NewTexture(256, 256);
        Frame(item, 8, 8, 240, 240, 18, Navy, Cyan);
        Rect(item, 24, 216, 48, 8, Fresh);
        Rect(item, 24, 32, 8, 48, Green);
        StepRect(item, 164, 20, 64, 36, 6, Outer);
        StepRect(item, 168, 24, 56, 28, 4, Navy);
        SavePng(item, "Assets/UI/PixelCampus/Sprites/Inventory/item_slot.png");

        var mon = NewTexture(256, 256);
        Frame(mon, 8, 8, 240, 240, 18, Navy, Cyan);
        Rect(mon, 24, 216, 48, 8, Fresh);
        Rect(mon, 24, 24, 208, 16, Green);
        Rect(mon, 32, 28, 120, 4, Fresh);
        SavePng(mon, "Assets/UI/PixelCampus/Sprites/Inventory/uvgmon_slot.png");
    }

    private static void BuildCombat()
    {
        var status = NewTexture(768, 160);
        Frame(status, 4, 4, 760, 152, 18, Navy, Cyan);
        Rect(status, 28, 124, 180, 4, Fresh);
        Rect(status, 28, 52, 712, 4, Teal);
        StepRect(status, 112, 20, 600, 24, 6, Outer);
        SavePng(status, "Assets/UI/PixelCampus/Sprites/Combat/battle_status_panel.png");

        var message = NewTexture(1024, 128);
        Frame(message, 4, 4, 1016, 120, 16, Navy, Cyan);
        Rect(message, 28, 104, 968, 4, Teal);
        Rect(message, 956, 20, 16, 4, Fresh);
        Rect(message, 960, 16, 8, 4, Fresh);
        SavePng(message, "Assets/UI/PixelCampus/Sprites/Combat/message_panel.png");

        var hp = NewTexture(768, 64);
        StepRect(hp, 4, 16, 760, 32, 8, Green);
        Rect(hp, 16, 36, 736, 8, Fresh);
        Rect(hp, 16, 20, 736, 4, new Color32(13, 89, 65, 255));
        Rect(hp, 16, 28, 8, 8, Ivory);
        SavePng(hp, "Assets/UI/PixelCampus/Sprites/Combat/hp_fill.png");
    }

    private static void BuildSmallControls()
    {
        var close = NewTexture(128, 128);
        Frame(close, 8, 8, 112, 112, 16, Navy, Cyan);
        for (int i = 0; i < 7; i++)
        {
            Rect(close, 38 + i * 8, 38 + i * 8, 8, 8, Ivory);
            Rect(close, 82 - i * 8, 38 + i * 8, 8, 8, Ivory);
        }
        Rect(close, 16, 96, 16, 8, Fresh);
        SavePng(close, "Assets/UI/PixelCampus/Sprites/Core/close_button.png");

        var badge = NewTexture(512, 96);
        Frame(badge, 4, 12, 504, 72, 18, Teal, Cyan);
        Rect(badge, 20, 28, 12, 40, Green);
        Rect(badge, 36, 68, 96, 4, Fresh);
        SavePng(badge, "Assets/UI/PixelCampus/Sprites/Core/status_badge.png");

        var selection = NewTexture(256, 256);
        Rect(selection, 8, 220, 52, 8, Cyan); Rect(selection, 8, 196, 8, 32, Cyan);
        Rect(selection, 196, 220, 52, 8, Fresh); Rect(selection, 240, 196, 8, 32, Fresh);
        Rect(selection, 8, 28, 52, 8, Fresh); Rect(selection, 8, 28, 8, 32, Fresh);
        Rect(selection, 196, 28, 52, 8, Cyan); Rect(selection, 240, 28, 8, 32, Cyan);
        Rect(selection, 20, 208, 28, 4, Green); Rect(selection, 208, 208, 28, 4, Green);
        Rect(selection, 20, 44, 28, 4, Green); Rect(selection, 208, 44, 28, 4, Green);
        SavePng(selection, "Assets/UI/PixelCampus/Sprites/Core/selection_frame.png");

        var ring = NewTexture(256, 256);
        for (int y = 8; y < 248; y += 8)
            for (int x = 8; x < 248; x += 8)
            {
                float dx = x + 4 - 128;
                float dy = y + 4 - 128;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d >= 91 && d <= 111)
                {
                    int segment = ((int)(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 360f) / 30) % 2;
                    Rect(ring, x, y, 8, 8, segment == 0 ? Fresh : Cyan);
                }
            }
        SavePng(ring, "Assets/UI/PixelCampus/Sprites/Combat/qte_ring.png");

        var track = NewTexture(64, 640);
        Frame(track, 4, 4, 56, 632, 10, Navy, Teal);
        Rect(track, 24, 40, 16, 560, Outer);
        Rect(track, 28, 52, 8, 536, new Color32(13, 57, 66, 255));
        Rect(track, 20, 604, 24, 4, Cyan);
        Rect(track, 20, 32, 24, 4, Cyan);
        SavePng(track, "Assets/UI/PixelCampus/Sprites/Core/scrollbar_track.png");

        var thumb = NewTexture(64, 192);
        Frame(thumb, 4, 4, 56, 184, 10, Green, Teal);
        Rect(thumb, 20, 104, 24, 8, Cyan);
        Rect(thumb, 20, 80, 24, 8, Cyan);
        SavePng(thumb, "Assets/UI/PixelCampus/Sprites/Core/scrollbar_thumb.png");
    }

    private static void BuildTransitionSprites()
    {
        var diamond = NewTexture(256, 256);
        for (int y = 0; y < 256; y += 4)
            for (int x = 0; x < 256; x += 4)
            {
                int d = Mathf.Abs(x + 2 - 128) + Mathf.Abs(y + 2 - 128);
                if (d <= 116) Rect(diamond, x, y, 4, 4, d > 106 ? Outer : d > 98 ? Cyan : d > 86 ? Teal : Navy);
            }
        Rect(diamond, 112, 112, 32, 32, Green);
        Rect(diamond, 120, 120, 16, 16, Fresh);
        SaveTga(diamond, "Assets/Resources/TransitionSprites/pixel_campus_diamond.tga");

        var leaf = NewTexture(384, 256);
        float angle = -22f * Mathf.Deg2Rad;
        for (int y = 0; y < 256; y += 4)
            for (int x = 0; x < 384; x += 4)
            {
                float dx = x + 2 - 192;
                float dy = y + 2 - 128;
                float u = dx * Mathf.Cos(angle) - dy * Mathf.Sin(angle);
                float v = dx * Mathf.Sin(angle) + dy * Mathf.Cos(angle);
                float q = (u * u) / (172f * 172f) + (v * v) / (66f * 66f);
                if (q <= 1f) Rect(leaf, x, y, 4, 4, q > .82f ? Outer : q > .68f ? Teal : Green);
                if (q <= .9f && Mathf.Abs(v) < 5f) Rect(leaf, x, y, 4, 4, Cyan);
            }
        Rect(leaf, 188, 40, 8, 176, Fresh);
        SaveTga(leaf, "Assets/Resources/TransitionSprites/pixel_campus_leaf.tga");

        var streak = NewTexture(1024, 256);
        for (int y = 0; y < 256; y += 8)
            for (int x = 0; x < 1024; x += 8)
            {
                float center = 128f + (x - 512f) * .09f;
                float band = Mathf.Abs((y + 4) - center);
                float taper = Mathf.Min(x + 4, 1024 - x - 4) / 4f;
                if (band < 56f && taper > band)
                {
                    Color32 c = band < 8 ? Fresh : band < 20 ? Cyan : band < 38 ? Teal : Navy;
                    Rect(streak, x, y, 8, 8, c);
                }
            }
        SaveTga(streak, "Assets/Resources/TransitionSprites/pixel_campus_streak.tga");

        var core = NewTexture(384, 384);
        for (int y = 0; y < 384; y += 4)
            for (int x = 0; x < 384; x += 4)
            {
                int dx = Mathf.Abs(x + 2 - 192);
                int dy = Mathf.Abs(y + 2 - 192);
                int hex = Mathf.Max(dx, (dx + dy * 2) / 2);
                if (hex <= 164) Rect(core, x, y, 4, 4, hex > 152 ? Outer : hex > 140 ? Cyan : hex > 124 ? Teal : Navy);
            }
        for (int y = 96; y < 288; y += 4)
            for (int x = 96; x < 288; x += 4)
            {
                int d = Mathf.Abs(x + 2 - 192) + Mathf.Abs(y + 2 - 192);
                if (d <= 76) Rect(core, x, y, 4, 4, d > 64 ? Green : Fresh);
            }
        Rect(core, 188, 116, 8, 152, Ivory);
        SaveTga(core, "Assets/Resources/TransitionSprites/pixel_campus_core.tga");
    }
}
