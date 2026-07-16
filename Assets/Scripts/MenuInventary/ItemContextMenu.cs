using System;
using UnityEngine;
using UnityEngine.UI;

public class ItemContextMenu : MonoBehaviour
{
    [SerializeField]
    private Button useButton;

    public event Action OnUseClicked;

    private void Awake()
    {
        useButton.onClick.AddListener(() => OnUseClicked?.Invoke());
        Hide();
    }

    public void Show(Vector3 worldPosition)
    {
        gameObject.SetActive(true);
        transform.position = worldPosition;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
