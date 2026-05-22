using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class PerkHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Idle Settings")]
    float idleRotationSpeed;
    float idleRotationAmount;

    [Header("Hover Settings")]
    public float maxTilt = 15f;

    [Header("Spring Settings")]
    [Tooltip("How strongly the card snaps toward the target rotation.")]
    public float springStiffness = 200f;
    [Tooltip("How much the spring resists overshooting. Lower = bouncier.")]
    public float springDamping   = 15f;

    [Header("Scale Settings")]
    public float hoverScale       = 1.08f;
    public float scaleInDuration  = 0.3f;
    public float scaleOutDuration = 0.2f;

    // The card root — one level up from this button child.
    private RectTransform cardRect;
    private bool isHovered;

    // Spring state — kept in Euler space (small angles, no gimbal concern).
    private Vector3 targetEuler;
    private Vector3 currentEuler;
    private Vector3 rotationVelocity;

    private void Start()
    {
        idleRotationSpeed = Random.Range(8,12);
        idleRotationAmount = Random.Range(4,8);

        // This script sits on the button child; the visual card to tilt is the parent.
        cardRect         = transform.parent.GetComponent<RectTransform>();
        targetEuler      = Vector3.zero;
        currentEuler     = Vector3.zero;
        rotationVelocity = Vector3.zero;
    }

    private void Update()
    {
        if (isHovered)
        {
            // Map mouse position into the card's local space (-1 … +1 on each axis).
            Vector2 localPos = cardRect.InverseTransformPoint(Input.mousePosition);
            float normX = Mathf.Clamp(localPos.x / (cardRect.rect.width  * 0.5f), -1f, 1f);
            float normY = Mathf.Clamp(localPos.y / (cardRect.rect.height * 0.5f), -1f, 1f);

            // Pitch: mouse above centre → top tilts away (negative X rotation in Unity UI).
            // Roll:  mouse right of centre → right edge tilts away (positive Y rotation).
            targetEuler = new Vector3(-normY * maxTilt, normX * maxTilt, 0f);
        }
        else
        {
            // Idle wobble — unscaled so it works while timeScale == 0.
            float t = Time.unscaledTime * idleRotationSpeed;
            targetEuler = new Vector3(
                Mathf.Sin(t) * idleRotationAmount,
                Mathf.Cos(t) * idleRotationAmount,
                0f
            );
        }

        // Critically-damped spring integration (semi-implicit Euler, unscaled).
        float dt             = Time.unscaledDeltaTime;
        Vector3 displacement = targetEuler - currentEuler;
        Vector3 springForce  = displacement        * springStiffness;
        Vector3 dampForce    = rotationVelocity    * springDamping;

        rotationVelocity += (springForce - dampForce) * dt;
        currentEuler     += rotationVelocity           * dt;

        cardRect.localRotation = Quaternion.Euler(currentEuler);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;

        // Kill any in-progress scale tween, then pop forward with an overshoot bounce.
        cardRect.DOKill(false);
        cardRect.DOScale(hoverScale, scaleInDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);   // SetUpdate(true) = unscaled, works at timeScale 0.
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        cardRect.DOKill(false);
        cardRect.DOScale(1f, scaleOutDuration)
                .SetEase(Ease.InOutQuad)
                .SetUpdate(true);
    }
}
