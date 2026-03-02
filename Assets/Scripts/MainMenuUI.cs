using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Optional Panels")]
    [SerializeField] private GameObject mainWindow;     // El MainWindow
    [SerializeField] private GameObject optionsPanel;   // opcional Menú

    [Header("Debug")]
    [SerializeField] private bool logActions = true;

    private void Start()
    {
        // Asegura estado "normal" si se viene desde pausa u otra escena
        Time.timeScale = 1f;

        // Estado inicial de UI
        if (mainWindow != null) mainWindow.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    public void Play()
    {
        if (logActions) Debug.Log("MainMenu: Play()");
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenOptions()
    {
        if (logActions) Debug.Log("MainMenu: OpenOptions()");
        if (optionsPanel == null) return;

        if (mainWindow != null) mainWindow.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        if (logActions) Debug.Log("MainMenu: CloseOptions()");
        if (optionsPanel == null) return;

        optionsPanel.SetActive(false);
        if (mainWindow != null) mainWindow.SetActive(true);
    }

    public void Quit()
    {
        if (logActions) Debug.Log("MainMenu: Quit()");
        Application.Quit();
    }
}
