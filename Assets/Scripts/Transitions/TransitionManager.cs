using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

[Serializable]
public class LoadingMessage
{
    public string banner;
    public string content;
}

[Serializable]
public class LoadingMessageList
{
    public List<LoadingMessage> messages;
}

public class TransitionManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup backgroundCanvasGroup;
    [SerializeField] private RectTransform textCanva;
    [SerializeField] private TextMeshProUGUI textBanner;
    [SerializeField] private TextMeshProUGUI contenido;

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private float zoomDuration = 0.4f;
    [SerializeField] private float zoomInScale = 0.85f;   // escala inicial del zoom in
    [SerializeField] private float zoomOutScale = 1.15f;  // escala final del zoom out

    [Header("Messages (opcional override)")]
    [SerializeField] private string jsonFileName = "loadingMessages"; // en Resources/
    [SerializeField] private bool pickRandom = true;

    // ── Estado interno ──────────────────────────────────────────
    private string pendingScene;
    private LoadingMessageList messageList;

    // Singleton liviano
    public static TransitionManager Instance { get; private set; }

    // ────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Empieza invisible
        backgroundCanvasGroup.alpha = 0f;
        textCanva.gameObject.SetActive(false);

        LoadMessages();
    }

    // ── API pública ──────────────────────────────────────────────

    /// <summary>Llama esto desde TransitionEscene u otro script.</summary>
    public void GoToScene(string sceneName)
    {
        if (pendingScene != null) return; // evita doble llamada
        pendingScene = sceneName;
        StartCoroutine(PlayTransition());
    }

    // ── Coroutine principal ──────────────────────────────────────

    private IEnumerator PlayTransition()
    {
        SetMessage();

        // 1. Fade in del fondo
        yield return StartCoroutine(FadeBackground(0f, 1f, fadeDuration));

        // 2. Zoom in del panel
        yield return StartCoroutine(ZoomPanel(zoomInScale, 1f, zoomDuration));

        // 3. Carga la escena en background
        AsyncOperation op = SceneManager.LoadSceneAsync(pendingScene);
        op.allowSceneActivation = false;

        // Espera a que cargue (y al menos X segundos para que se vea la pantalla)
        float minWait = 2.5f;
        float elapsed = 0f;
        while (!op.isDone)
        {
            elapsed += Time.deltaTime;
            if (op.progress >= 0.9f && elapsed >= minWait)
                break;
            yield return null;
        }

        // 4. Zoom out del panel
        yield return StartCoroutine(ZoomPanel(1f, zoomOutScale, zoomDuration));
        textCanva.gameObject.SetActive(false);

        // 5. Activar escena + fade out del fondo
        op.allowSceneActivation = true;
        yield return StartCoroutine(FadeBackground(1f, 0f, fadeDuration));

        pendingScene = null;
    }

    // ── Animaciones ──────────────────────────────────────────────

    private IEnumerator FadeBackground(float from, float to, float duration)
    {
        float t = 0f;
        backgroundCanvasGroup.alpha = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            backgroundCanvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        backgroundCanvasGroup.alpha = to;
    }

    private IEnumerator ZoomPanel(float fromScale, float toScale, float duration)
    {
        textCanva.gameObject.SetActive(true);
        float t = 0f;
        textCanva.localScale = Vector3.one * fromScale;
        while (t < duration)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(fromScale, toScale, t / duration);
            textCanva.localScale = Vector3.one * s;
            yield return null;
        }
        textCanva.localScale = Vector3.one * toScale;
    }

    // ── Mensajes ─────────────────────────────────────────────────

    private void LoadMessages()
        {
            // Intenta con Resources primero
            TextAsset asset = Resources.Load<TextAsset>(jsonFileName);
            
            // Si no encuentra, intenta con ruta absoluta desde Assets
            if (asset == null)
            {
                #if UNITY_EDITOR
                string fullPath = System.IO.Path.Combine(Application.dataPath, jsonFileName);
                if (System.IO.File.Exists(fullPath))
                {
                    string json = System.IO.File.ReadAllText(fullPath);
                    messageList = JsonUtility.FromJson<LoadingMessageList>(json);
                    return;
                }
                #endif
                Debug.LogWarning($"TransitionManager: No se encontró '{jsonFileName}'.");
                return;
            }
            messageList = JsonUtility.FromJson<LoadingMessageList>(asset.text);
        }

    private void SetMessage()
    {
        if (messageList == null || messageList.messages == null || messageList.messages.Count == 0)
        {
            textBanner.text = "CARGANDO";
            contenido.text = "";
            return;
        }

        LoadingMessage msg = pickRandom
            ? messageList.messages[UnityEngine.Random.Range(0, messageList.messages.Count)]
            : messageList.messages[0];

        textBanner.text = msg.banner;
        contenido.text  = msg.content;
    }
}