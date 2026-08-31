using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TransitionManager : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField, Min(0.1f)] private float closeDuration = 0.8f;
    [SerializeField, Min(0.1f)] private float revealDuration = 0.7f;
    [SerializeField, Min(0f)] private float coveredHoldDuration = 0.12f;

    [Header("Mosaic")]
    [SerializeField, Range(4, 16)] private int tileColumns = 9;
    [SerializeField, Range(3, 10)] private int tileRows = 6;
    [SerializeField, Range(2, 16)] private int leafCount = 8;

    [Header("Palette")]
    [SerializeField] private Color backdropColor = new Color(0.005f, 0.055f, 0.035f, 1f);
    [SerializeField] private Color tileTint = new Color(0.72f, 1f, 0.82f, 1f);

    private const string SpriteResourcePath = "TransitionSprites/";

    private readonly List<Image> tileImages = new List<Image>();
    private readonly List<Image> leafImages = new List<Image>();
    private readonly List<Image> streakImages = new List<Image>();

    private CanvasGroup overlayCanvasGroup;
    private Image backdropImage;
    private Image coreImage;
    private string pendingScene;
    private bool isTransitioning;

    public static TransitionManager Instance { get; private set; }
    public static bool IsTransitioning
    {
        get { return Instance != null && Instance.isTransitioning; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private static void EnsureInstance()
    {
        if (Instance != null) return;

        TransitionManager existing = FindObjectOfType<TransitionManager>();
        if (existing != null)
        {
            Instance = existing;
            return;
        }

        GameObject managerObject = new GameObject("TransitionManager");
        managerObject.AddComponent<TransitionManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        CreateTransitionCanvas();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void LoadScene(string sceneName)
    {
        EnsureInstance();
        Instance.GoToScene(sceneName);
    }

    public void GoToScene(string sceneName)
    {
        if (isTransitioning) return;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("TransitionManager: No se proporciono una escena de destino.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("TransitionManager: La escena '" + sceneName + "' no esta incluida en Build Settings.");
            return;
        }

        pendingScene = sceneName;
        StartCoroutine(PlayTransition());
    }

    private void CreateTransitionCanvas()
    {
        if (overlayCanvasGroup != null) return;

        Sprite diamondSprite = Resources.Load<Sprite>(SpriteResourcePath + "emerald_diamond");
        Sprite leafSprite = Resources.Load<Sprite>(SpriteResourcePath + "emerald_leaf");
        Sprite streakSprite = Resources.Load<Sprite>(SpriteResourcePath + "emerald_streak");
        Sprite coreSprite = Resources.Load<Sprite>(SpriteResourcePath + "emerald_core");

        GameObject canvasObject = new GameObject(
            "EmeraldTransitionCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));

        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        overlayCanvasGroup = canvasObject.GetComponent<CanvasGroup>();
        overlayCanvasGroup.alpha = 0f;
        overlayCanvasGroup.blocksRaycasts = false;
        overlayCanvasGroup.interactable = false;
        overlayCanvasGroup.ignoreParentGroups = true;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();

        backdropImage = CreateImage("Backdrop", canvasRect, null, backdropColor);
        StretchToParent(backdropImage.rectTransform);

        RectTransform tileLayer = CreateLayer("DiamondMosaic", canvasRect);
        RectTransform accentLayer = CreateLayer("EnergyAccents", canvasRect);

        CreateDiamondMosaic(tileLayer, diamondSprite);
        CreateLeaves(accentLayer, leafSprite);
        CreateStreaks(accentLayer, streakSprite);

        coreImage = CreateImage("EmeraldCore", accentLayer, coreSprite, Color.white);
        RectTransform coreRect = coreImage.rectTransform;
        coreRect.anchorMin = new Vector2(0.5f, 0.5f);
        coreRect.anchorMax = new Vector2(0.5f, 0.5f);
        coreRect.pivot = new Vector2(0.5f, 0.5f);
        coreRect.sizeDelta = new Vector2(330f, 330f);

        ResetVisuals();
    }

    private void CreateDiamondMosaic(RectTransform parent, Sprite sprite)
    {
        tileImages.Clear();

        for (int row = 0; row < tileRows; row++)
        {
            for (int column = 0; column < tileColumns; column++)
            {
                Image tile = CreateImage(
                    "Diamond_" + row + "_" + column,
                    parent,
                    sprite,
                    tileTint);

                RectTransform rect = tile.rectTransform;
                rect.anchorMin = new Vector2((float)column / tileColumns, (float)row / tileRows);
                rect.anchorMax = new Vector2((float)(column + 1) / tileColumns, (float)(row + 1) / tileRows);
                rect.offsetMin = new Vector2(-28f, -28f);
                rect.offsetMax = new Vector2(28f, 28f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.localScale = Vector3.zero;
                tile.preserveAspect = false;
                tileImages.Add(tile);
            }
        }
    }

    private void CreateLeaves(RectTransform parent, Sprite sprite)
    {
        leafImages.Clear();

        for (int i = 0; i < leafCount; i++)
        {
            Image leaf = CreateImage("EnergyLeaf_" + i, parent, sprite, Color.white);
            RectTransform rect = leaf.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(330f, 220f);
            rect.localEulerAngles = new Vector3(0f, 0f, -8f + (i % 4) * 5f);
            leaf.preserveAspect = true;
            leafImages.Add(leaf);
        }
    }

    private void CreateStreaks(RectTransform parent, Sprite sprite)
    {
        streakImages.Clear();

        for (int i = 0; i < 2; i++)
        {
            Image streak = CreateImage("EnergyStreak_" + i, parent, sprite, Color.white);
            RectTransform rect = streak.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1450f, 320f);
            rect.anchoredPosition = new Vector2(0f, i == 0 ? -220f : 220f);
            rect.localEulerAngles = new Vector3(0f, 0f, i == 0 ? -4f : 5f);
            streak.preserveAspect = true;
            streakImages.Add(streak);
        }
    }

    private IEnumerator PlayTransition()
    {
        isTransitioning = true;
        overlayCanvasGroup.alpha = 1f;
        overlayCanvasGroup.blocksRaycasts = true;

        yield return Animate(closeDuration, UpdateCloseVisuals);

        AsyncOperation operation = SceneManager.LoadSceneAsync(pendingScene);
        while (!operation.isDone) yield return null;

        yield return null;
        yield return null;

        if (coveredHoldDuration > 0f)
            yield return Animate(coveredHoldDuration, delegate { });

        yield return Animate(revealDuration, UpdateRevealVisuals);

        ResetVisuals();
        overlayCanvasGroup.blocksRaycasts = false;
        overlayCanvasGroup.alpha = 0f;
        pendingScene = null;
        isTransitioning = false;
    }

    private void UpdateCloseVisuals(float progress)
    {
        float eased = Smooth(progress);
        SetAlpha(backdropImage, Mathf.Clamp01(progress * 1.45f));

        int diagonalLength = Mathf.Max(1, tileColumns + tileRows - 2);
        for (int i = 0; i < tileImages.Count; i++)
        {
            int row = i / tileColumns;
            int column = i % tileColumns;
            float delay = ((float)(row + column) / diagonalLength) * 0.32f;
            float local = Mathf.Clamp01((progress - delay) / (1f - delay));
            tileImages[i].rectTransform.localScale = Vector3.one * (Smooth(local) * 1.62f);
            SetAlpha(tileImages[i], local);
        }

        for (int i = 0; i < leafImages.Count; i++)
        {
            float lane = leafImages.Count == 1 ? 0.5f : (float)i / (leafImages.Count - 1);
            Vector2 start = new Vector2(-1250f - i * 70f, Mathf.Lerp(-500f, 500f, lane));
            Vector2 end = new Vector2(1250f + i * 55f, start.y + Mathf.Sin(i * 1.7f) * 150f);
            leafImages[i].rectTransform.anchoredPosition = Vector2.LerpUnclamped(start, end, eased);
            leafImages[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.15f, Mathf.Sin(progress * Mathf.PI));
            SetAlpha(leafImages[i], Mathf.Sin(progress * Mathf.PI) * 0.95f);
        }

        for (int i = 0; i < streakImages.Count; i++)
        {
            float local = Mathf.Clamp01((progress - 0.1f - i * 0.08f) / 0.55f);
            streakImages[i].rectTransform.localScale = new Vector3(Smooth(local), 1f, 1f);
            SetAlpha(streakImages[i], Mathf.Sin(local * Mathf.PI) * 0.9f);
        }

        coreImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(0f, 1.08f, Smooth(Mathf.Clamp01((progress - 0.42f) / 0.58f)));
        coreImage.rectTransform.localEulerAngles = new Vector3(0f, 0f, progress * 110f);
        SetAlpha(coreImage, Mathf.Clamp01((progress - 0.35f) / 0.45f));
    }

    private void UpdateRevealVisuals(float progress)
    {
        int diagonalLength = Mathf.Max(1, tileColumns + tileRows - 2);

        for (int i = 0; i < tileImages.Count; i++)
        {
            int row = i / tileColumns;
            int column = i % tileColumns;
            float reverseDiagonal = (float)((tileRows - 1 - row) + (tileColumns - 1 - column)) / diagonalLength;
            float delay = reverseDiagonal * 0.28f;
            float local = Mathf.Clamp01((progress - delay) / (1f - delay));
            tileImages[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(1.62f, 0f, Smooth(local));
            SetAlpha(tileImages[i], 1f - local);
        }

        SetAlpha(backdropImage, 1f - Smooth(Mathf.Clamp01((progress - 0.48f) / 0.52f)));

        for (int i = 0; i < leafImages.Count; i++)
        {
            float lane = leafImages.Count == 1 ? 0.5f : (float)i / (leafImages.Count - 1);
            Vector2 start = new Vector2(1050f + i * 50f, Mathf.Lerp(500f, -500f, lane));
            Vector2 end = new Vector2(-1300f - i * 70f, start.y + Mathf.Cos(i * 1.3f) * 130f);
            leafImages[i].rectTransform.anchoredPosition = Vector2.LerpUnclamped(start, end, Smooth(progress));
            leafImages[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(1.1f, 0.45f, progress);
            SetAlpha(leafImages[i], Mathf.Sin(progress * Mathf.PI) * 0.8f);
        }

        coreImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.08f, 2.25f, Smooth(progress));
        coreImage.rectTransform.localEulerAngles = new Vector3(0f, 0f, 110f + progress * 150f);
        SetAlpha(coreImage, 1f - progress);

        for (int i = 0; i < streakImages.Count; i++)
        {
            float flash = Mathf.Sin(Mathf.Clamp01(progress * 1.7f) * Mathf.PI);
            streakImages[i].rectTransform.localScale = new Vector3(Mathf.Lerp(1f, 1.35f, progress), 1f, 1f);
            SetAlpha(streakImages[i], flash * 0.55f);
        }
    }

    private IEnumerator Animate(float duration, Action<float> update)
    {
        if (duration <= 0f)
        {
            update(1f);
            yield break;
        }

        float elapsed = 0f;
        update(0f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            update(Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        update(1f);
    }

    private void ResetVisuals()
    {
        if (backdropImage != null) SetAlpha(backdropImage, 0f);

        for (int i = 0; i < tileImages.Count; i++)
        {
            tileImages[i].rectTransform.localScale = Vector3.zero;
            SetAlpha(tileImages[i], 0f);
        }

        for (int i = 0; i < leafImages.Count; i++)
        {
            leafImages[i].rectTransform.localScale = Vector3.zero;
            SetAlpha(leafImages[i], 0f);
        }

        for (int i = 0; i < streakImages.Count; i++)
        {
            streakImages[i].rectTransform.localScale = Vector3.zero;
            SetAlpha(streakImages[i], 0f);
        }

        if (coreImage != null)
        {
            coreImage.rectTransform.localScale = Vector3.zero;
            SetAlpha(coreImage, 0f);
        }
    }

    private static RectTransform CreateLayer(string name, RectTransform parent)
    {
        GameObject layerObject = new GameObject(name, typeof(RectTransform));
        RectTransform layer = layerObject.GetComponent<RectTransform>();
        layer.SetParent(parent, false);
        StretchToParent(layer);
        return layer;
    }

    private static Image CreateImage(string name, RectTransform parent, Sprite sprite, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void SetAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = Mathf.Clamp01(alpha);
        image.color = color;
    }

    private static float Smooth(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }
}
