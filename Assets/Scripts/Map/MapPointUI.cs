using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Marcador que se dibuja sobre el mapa. Se coloca usando la posición XY normalizada
/// del <see cref="MapPoint"/> y al hacer clic avisa al mapa para que abra su imagen.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MapPointUI : MonoBehaviour
{
    [SerializeField] private Image markerImage;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Button button;

    private RectTransform rectTransform;
    private MapPoint point;
    private Action<MapPoint> onClicked;

    /// <summary>Punto de datos que representa este marcador.</summary>
    public MapPoint Point => point;

    /// <summary>
    /// Posición XY actual del marcador sobre el mapa (0 a 1), calculada desde su
    /// posición real dentro del contenedor. Así también funciona si se arrastró a mano.
    /// </summary>
    public Vector2 NormalizedPosition
    {
        get
        {
            CacheRect();

            RectTransform parent = rectTransform.parent as RectTransform;
            if (parent == null) return rectTransform.anchorMin;

            Rect area = parent.rect;
            if (Mathf.Approximately(area.width, 0f) || Mathf.Approximately(area.height, 0f))
                return rectTransform.anchorMin;

            Vector3 world = rectTransform.TransformPoint(rectTransform.rect.center);
            Vector3 local = parent.InverseTransformPoint(world);

            return new Vector2(
                (local.x - area.xMin) / area.width,
                (local.y - area.yMin) / area.height);
        }
    }

    private void CacheRect()
    {
        if (rectTransform == null)
            rectTransform = (RectTransform)transform;
    }

    private void Awake()
    {
        CacheRect();

        if (button == null) button = GetComponent<Button>();
        if (markerImage == null) markerImage = GetComponent<Image>();

        if (button != null)
            button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClick);
    }

    /// <summary>Coloca el marcador en el mapa con los datos del punto.</summary>
    public void Setup(MapPoint mapPoint, Sprite defaultIcon, Action<MapPoint> clickCallback)
    {
        point = mapPoint;
        onClicked = clickCallback;

        if (point == null) return;

        SetNormalizedPosition(point.normalizedPosition);

        if (markerImage != null)
        {
            if (point.markerIcon != null) markerImage.sprite = point.markerIcon;
            else if (defaultIcon != null) markerImage.sprite = defaultIcon;

            markerImage.color = point.markerColor;
        }

        if (label != null)
            label.text = point.title;

        gameObject.name = "Point_" + point.title;
        gameObject.SetActive(point.visible);
    }

    /// <summary>Mueve el marcador a una posición XY del mapa (0 a 1).</summary>
    public void SetNormalizedPosition(Vector2 normalized)
    {
        CacheRect();

        normalized.x = Mathf.Clamp01(normalized.x);
        normalized.y = Mathf.Clamp01(normalized.y);

        rectTransform.anchorMin = normalized;
        rectTransform.anchorMax = normalized;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
    }

    private void HandleClick()
    {
        if (onClicked != null && point != null)
            onClicked(point);
    }
}
