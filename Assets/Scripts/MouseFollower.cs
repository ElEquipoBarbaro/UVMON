using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MouseFollower : MonoBehaviour
{
    [SerializeField]
    private Canvas canvas;



    [SerializeField]

    private Camera mainCam;

    [SerializeField]
    private UIInventoryItem item;

    [SerializeField]
    private CanvasGroup canvasGroup;

     public void Awake()
    {
        canvas = transform.root.GetComponent<Canvas>();
        mainCam = Camera.main;
        item = GetComponentInChildren<UIInventoryItem>();

        // The ghost item must never intercept pointer events: it sits on top of
        // (and directly follows) the cursor, so without this its Image/Text
        // raycast targets steal OnDrop from the real ItemUI slot underneath.
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    public void SetData(Sprite sprite, int quantity)
    {
        item.SetData(sprite, quantity);
    }
    void Update()
    {
        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)canvas.transform,
            Input.mousePosition,
            canvas.worldCamera,
            out position
                );
        transform.position = canvas.transform.TransformPoint(position);
    }

    public void Toggle(bool val)
    {
        Debug.Log($"Item toggled {val}");
        gameObject.SetActive(val);
    }


}
