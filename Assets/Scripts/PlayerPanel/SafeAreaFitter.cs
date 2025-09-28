using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rt;
    private Rect lastSafe;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        Apply();
    }

    void OnRectTransformDimensionsChange()
    {
        Apply();
    }

    private void Apply()
    {
        if (Screen.safeArea == lastSafe || rt == null) return;
        lastSafe = Screen.safeArea;

        Vector2 anchorMin = lastSafe.position;
        Vector2 anchorMax = lastSafe.position + lastSafe.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
