using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TransitionManager : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.4f;

    private CanvasGroup backgroundCanvasGroup;
    private string pendingScene;

    public static TransitionManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateFadeCanvas();
    }

    private void CreateFadeCanvas()
    {
        GameObject canvasGO = new GameObject("FadeCanvas");
        canvasGO.transform.SetParent(transform);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("FadePanel");
        panel.transform.SetParent(canvasGO.transform, false);

        Image img = panel.AddComponent<Image>();
        img.color = Color.black;

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        backgroundCanvasGroup = panel.AddComponent<CanvasGroup>();
        backgroundCanvasGroup.alpha = 0f;
        backgroundCanvasGroup.blocksRaycasts = false;
        // panel siempre activo, alpha controla visibilidad
    }

    public void GoToScene(string sceneName)
    {
        if (pendingScene != null) return;
        pendingScene = sceneName;
        StartCoroutine(PlayTransition());
    }

    private IEnumerator PlayTransition()
    {
        backgroundCanvasGroup.blocksRaycasts = true;

        yield return StartCoroutine(Fade(0f, 1f));

        AsyncOperation op = SceneManager.LoadSceneAsync(pendingScene);
        yield return new WaitUntil(() => op.isDone);

        yield return null;
        yield return null;

        yield return StartCoroutine(Fade(1f, 0f));

        backgroundCanvasGroup.blocksRaycasts = false;
        pendingScene = null;
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        backgroundCanvasGroup.alpha = from;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            backgroundCanvasGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        backgroundCanvasGroup.alpha = to;
    }
}