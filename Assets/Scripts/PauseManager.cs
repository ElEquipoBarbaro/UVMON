using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject pauseOverlay;

    [Header("Mapa de la UVG")]
    [SerializeField] private UVGMapUI mapUI;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Behavior")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private bool pauseAudio = false;
    [SerializeField] private bool logActions = true;

    private bool isPaused;

    // Escenas donde NO debe aparecer el menú de pausa
    private readonly string[] scenesWithoutPause = { "MainMenu" };

    private void Awake()
    {
        // Patrón Singleton: si ya existe uno, destruye este duplicado
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Sobrevive al cambiar de escena

        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);

        if (mapUI != null)
            mapUI.OnClosed += HandleMapClosed;

        Time.timeScale = 1f;
        isPaused = false;

        if (logActions) Debug.Log("PauseManager Awake OK - DontDestroyOnLoad activado");
    }

    private void OnDestroy()
    {
        if (mapUI != null)
            mapUI.OnClosed -= HandleMapClosed;
    }

    private void Update()
    {
        // No permite pausar en ciertas escenas (ej: MainMenu)
        if (IsSceneWithoutPause()) return;

        if (!Input.GetKeyDown(pauseKey)) return;

        // Si el mapa está abierto, ESC solo regresa al menú de pausa
        if (mapUI != null && mapUI.IsOpen)
        {
            if (logActions) Debug.Log("ESC detectado -> cerrar mapa");
            mapUI.Close();
            return;
        }

        if (logActions) Debug.Log("ESC detectado -> TogglePause()");
        TogglePause();
    }

    private bool IsSceneWithoutPause()
    {
        string current = SceneManager.GetActiveScene().name;
        foreach (string s in scenesWithoutPause)
            if (current == s) return true;
        return false;
    }

    public void TogglePause()
    {
        SetPaused(!isPaused);
    }

    private void SetPaused(bool value)
    {
        isPaused = value;

        if (logActions) Debug.Log("Paused = " + isPaused);

        // Al despausar, el mapa no debe quedarse abierto
        if (!isPaused && mapUI != null && mapUI.IsOpen)
            mapUI.Close();

        if (pauseOverlay != null)
            pauseOverlay.SetActive(isPaused);

        Time.timeScale = isPaused ? 0f : 1f;

        if (pauseAudio)
            AudioListener.pause = isPaused;
    }

    /// <summary>
    /// Acción del botón "Mapa": esconde el menú de pausa y abre el mapa de la UVG.
    /// El juego sigue pausado mientras el mapa está abierto.
    /// </summary>
    public void OpenMap()
    {
        if (mapUI == null)
        {
            Debug.LogWarning("PauseManager: no hay un UVGMapUI asignado en el Inspector.");
            return;
        }

        if (logActions) Debug.Log("Boton Mapa -> OpenMap()");

        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);

        mapUI.Open();
    }

    // Al cerrar el mapa se vuelve a mostrar el menú de pausa si el juego sigue pausado
    private void HandleMapClosed()
    {
        if (isPaused && pauseOverlay != null)
            pauseOverlay.SetActive(true);
    }

    public void Resume()
    {
        SetPaused(false);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        if (pauseAudio) AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void MainMenu()
    {
        SetPaused(false); // Limpia pausa antes de cambiar
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void Quit()
    {
        Time.timeScale = 1f;
        if (pauseAudio) AudioListener.pause = false;
        Application.Quit();
    }
}
