using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mapa de la UVG que se abre desde el menú de pausa.
/// Dibuja los puntos de <see cref="CampusMapData"/> sobre la imagen del mapa y,
/// al hacer clic en uno, muestra su imagen y su descripción.
/// </summary>
public class UVGMapUI : MonoBehaviour
{
    [Header("Datos")]
    [SerializeField] private CampusMapData mapData;

    [Header("Mapa")]
    [SerializeField] private GameObject mapRoot;
    [SerializeField] private Image mapImage;
    [SerializeField] private RectTransform pointsRoot;
    [SerializeField] private MapPointUI pointPrefab;
    [SerializeField] private Sprite defaultMarkerIcon;

    [Header("Detalle del punto")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private Image detailImage;
    [SerializeField] private TMP_Text detailTitle;
    [SerializeField] private TMP_Text detailDescription;

    [Header("Comportamiento")]
    [SerializeField] private bool logActions = false;

    private readonly List<MapPointUI> spawnedPoints = new List<MapPointUI>();

    /// <summary>Se dispara cuando el mapa se cierra (lo usa el menú de pausa para volver).</summary>
    public event Action OnClosed;

    /// <summary>True mientras el mapa está visible.</summary>
    public bool IsOpen => mapRoot != null && mapRoot.activeSelf;

    /// <summary>Asset de datos que está usando el mapa.</summary>
    public CampusMapData MapData => mapData;

    private void Awake()
    {
        if (mapRoot == null) mapRoot = gameObject;

        mapRoot.SetActive(false);

        if (detailPanel != null)
            detailPanel.SetActive(false);
    }

    /// <summary>Abre el mapa y (re)dibuja los puntos.</summary>
    public void Open()
    {
        if (mapRoot == null) mapRoot = gameObject;

        RebuildPoints();
        CloseDetail();

        mapRoot.SetActive(true);

        if (logActions) Debug.Log("UVGMapUI: mapa abierto");
    }

    /// <summary>Cierra el mapa y avisa a quien lo abrió.</summary>
    public void Close()
    {
        CloseDetail();

        if (mapRoot != null)
            mapRoot.SetActive(false);

        if (logActions) Debug.Log("UVGMapUI: mapa cerrado");

        if (OnClosed != null)
            OnClosed();
    }

    /// <summary>Abre o cierra el mapa (para conectarlo directo a un botón).</summary>
    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    /// <summary>
    /// Borra los marcadores actuales y los vuelve a crear desde el asset.
    /// Funciona también en el editor para poder acomodar los puntos sin entrar a Play.
    /// </summary>
    public void RebuildPoints()
    {
        ClearPoints();

        if (mapImage != null && mapData != null && mapData.mapImage != null)
            mapImage.sprite = mapData.mapImage;

        if (mapData == null || pointsRoot == null || pointPrefab == null)
        {
            if (logActions) Debug.LogWarning("UVGMapUI: faltan referencias (mapData, pointsRoot o pointPrefab).");
            return;
        }

        for (int i = 0; i < mapData.points.Count; i++)
        {
            MapPoint point = mapData.points[i];
            if (point == null) continue;

            MapPointUI marker = Instantiate(pointPrefab, pointsRoot);
            marker.Setup(point, defaultMarkerIcon, ShowPoint);
            spawnedPoints.Add(marker);
        }
    }

    /// <summary>Destruye los marcadores dibujados sobre el mapa.</summary>
    public void ClearPoints()
    {
        spawnedPoints.Clear();

        if (pointsRoot == null) return;

        for (int i = pointsRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = pointsRoot.GetChild(i).gameObject;

            if (Application.isPlaying)
            {
                // Destroy tarda hasta el final del frame: se apaga ya para que no se dupliquen
                child.SetActive(false);
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }

    /// <summary>Muestra la imagen y la descripción de un punto.</summary>
    public void ShowPoint(MapPoint point)
    {
        if (point == null || detailPanel == null) return;

        if (detailTitle != null)
            detailTitle.text = point.title;

        if (detailDescription != null)
            detailDescription.text = point.description;

        if (detailImage != null)
        {
            detailImage.sprite = point.image;
            detailImage.enabled = point.image != null;
        }

        detailPanel.SetActive(true);

        if (logActions) Debug.Log("UVGMapUI: punto abierto -> " + point.title);
    }

    /// <summary>Cierra el detalle y regresa al mapa.</summary>
    public void CloseDetail()
    {
        if (detailPanel != null)
            detailPanel.SetActive(false);
    }

    /// <summary>Cambia la imagen de un punto (por nombre) durante el juego.</summary>
    public void SetPointImage(string pointTitle, Sprite image)
    {
        MapPoint point = mapData != null ? mapData.FindPoint(pointTitle) : null;
        if (point == null)
        {
            Debug.LogWarning("UVGMapUI: no existe el punto '" + pointTitle + "'.");
            return;
        }

        point.image = image;
    }

    /// <summary>Cambia la descripción de un punto (por nombre) durante el juego.</summary>
    public void SetPointDescription(string pointTitle, string description)
    {
        MapPoint point = mapData != null ? mapData.FindPoint(pointTitle) : null;
        if (point == null)
        {
            Debug.LogWarning("UVGMapUI: no existe el punto '" + pointTitle + "'.");
            return;
        }

        point.description = description;
    }

    /// <summary>Cambia la posición XY (0 a 1) de un punto y lo mueve en el mapa.</summary>
    public void SetPointPosition(string pointTitle, Vector2 normalizedPosition)
    {
        MapPoint point = mapData != null ? mapData.FindPoint(pointTitle) : null;
        if (point == null)
        {
            Debug.LogWarning("UVGMapUI: no existe el punto '" + pointTitle + "'.");
            return;
        }

        point.normalizedPosition = normalizedPosition;

        for (int i = 0; i < spawnedPoints.Count; i++)
            if (spawnedPoints[i] != null && spawnedPoints[i].Point == point)
                spawnedPoints[i].SetNormalizedPosition(normalizedPosition);
    }

    /// <summary>
    /// Copia al asset la posición XY en la que quedó cada marcador.
    /// Sirve para acomodar los puntos arrastrándolos y luego guardarlos.
    /// </summary>
    public int SavePointPositionsToData()
    {
        if (pointsRoot == null) return 0;

        int saved = 0;

        for (int i = 0; i < pointsRoot.childCount; i++)
        {
            MapPointUI marker = pointsRoot.GetChild(i).GetComponent<MapPointUI>();
            if (marker == null || marker.Point == null) continue;

            marker.Point.normalizedPosition = marker.NormalizedPosition;
            saved++;
        }

        return saved;
    }
}
