using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Logica compartida del flash de golpe: un filtro blanco (mismo silueta que el sprite,
/// generado a partir de su canal alfa) superpuesto que parpadea de manera intermitente y
/// rapida (patron sinusoidal) por un instante. El sprite base nunca se toca — solo se le
/// asigna un sprite blanco generado aparte al Image de overlay y se anima su alpha — asi
/// que el material/color/sprite original queda intacto entre parpadeos y al terminar.
/// Usado tanto por BodyPartOptionUI (extremidades del enemigo) como por CreatureBattleView
/// (sprite completo del jugador o del enemigo sin partes de cuerpo), para que ambos casos
/// compartan exactamente el mismo efecto en vez de dos implementaciones distintas.
/// </summary>
public static class HitFlashEffect
{
    private static readonly Dictionary<Sprite, Sprite> whiteSpriteCache = new Dictionary<Sprite, Sprite>();

    /// <summary>
    /// Genera (y cachea) una variante blanca de un sprite: mismo canal alfa, RGB en blanco
    /// puro. Requiere que la textura tenga Read/Write Enabled (ver CLAUDE.md).
    /// </summary>
    public static Sprite GetOrCreateWhiteSprite(Sprite source)
    {
        if (source == null || source.texture == null)
            return null;

        Sprite cached;
        if (whiteSpriteCache.TryGetValue(source, out cached) && cached != null)
            return cached;

        Rect r = source.rect;
        Color[] pixels = source.texture.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color(1f, 1f, 1f, pixels[i].a);

        Texture2D whiteTex = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGBA32, false);
        whiteTex.SetPixels(pixels);
        whiteTex.Apply();

        Vector2 normalizedPivot = new Vector2(source.pivot.x / r.width, source.pivot.y / r.height);
        Sprite whiteSprite = Sprite.Create(whiteTex, new Rect(0f, 0f, r.width, r.height), normalizedPivot, source.pixelsPerUnit);

        whiteSpriteCache[source] = whiteSprite;
        return whiteSprite;
    }

    /// <summary>
    /// Anima el alpha de un Image de overlay (ya con el sprite blanco asignado) de 0 a
    /// maxAlpha en un pulso sinusoidal unico, y lo deja en 0 (invisible) al terminar.
    /// </summary>
    public static IEnumerator PlayOverlay(Image overlay, float duration, float speed, float maxAlpha)
    {
        if (overlay == null || duration <= 0f)
            yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = (Mathf.Sin(elapsed * speed) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(0f, maxAlpha, t);
            overlay.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        overlay.color = new Color(1f, 1f, 1f, 0f);
    }
}
