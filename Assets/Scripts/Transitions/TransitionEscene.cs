using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionEscene : MonoBehaviour
{
    [Header("Transition")]
    [SerializeField] private string targetSceneName;

    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool logActions = true;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag)) return;

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("TransitionEscene: No hay escena asignada.");
            return;
        }

        if (logActions) Debug.Log($"TransitionEscene: Cargando '{targetSceneName}'");
        
        // ← Solo este cambio
        TransitionManager.Instance.GoToScene(targetSceneName);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.4f);
        Gizmos.DrawCube(transform.position, transform.localScale);
    }
}