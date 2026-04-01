using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject pauseOverlay;

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

        Time.timeScale = 1f;
        isPaused = false;

        if (logActions) Debug.Log("PauseManager Awake OK - DontDestroyOnLoad activado");
    }

    private void Update()
    {
        // No permite pausar en ciertas escenas (ej: MainMenu)
        if (IsSceneWithoutPause()) return;

        if (Input.GetKeyDown(pauseKey))
        {
            if (logActions) Debug.Log("ESC detectado -> TogglePause()");
            TogglePause();
        }
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

        if (pauseOverlay != null)
            pauseOverlay.SetActive(isPaused);

        Time.timeScale = isPaused ? 0f : 1f;

        if (pauseAudio)
            AudioListener.pause = isPaused;
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
