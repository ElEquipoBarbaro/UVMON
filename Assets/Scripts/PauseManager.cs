using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pauseOverlay;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Behavior")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private bool pauseAudio = false;
    [SerializeField] private bool logActions = true;

    private bool isPaused;

    private void Awake()
    {
        // Esto evita que se empiece en pausa por accidente
        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;

        if (logActions) Debug.Log("PauseManager Awake OK");
    }

    private void Update()
    {
        // OJO: en Play, se hace click en la pestaña Game para que reciba teclado
        if (Input.GetKeyDown(pauseKey))
        {
            if (logActions) Debug.Log("ESC detectado -> TogglePause()");
            TogglePause();
        }
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

    // --- Botones ---
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
        Time.timeScale = 1f;
        if (pauseAudio) AudioListener.pause = false;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void Quit()
    {
        Time.timeScale = 1f;
        if (pauseAudio) AudioListener.pause = false;

        Application.Quit();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        if (pauseAudio) AudioListener.pause = false;
    }
}