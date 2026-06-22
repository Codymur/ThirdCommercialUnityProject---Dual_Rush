using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class PixelPerfectMover : MonoBehaviour
{
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // Always use LateUpdate so movement calculations happen after physics/camera updates
    void LateUpdate()
    {
        Vector3 currentPos = rectTransform.anchoredPosition3D;

        // Round the position coordinates to the absolute nearest integer pixel
        float snappedX = Mathf.Round(currentPos.x);
        float snappedY = Mathf.Round(currentPos.y);

        rectTransform.anchoredPosition3D = new Vector3(snappedX, snappedY, currentPos.z);
    }
}
