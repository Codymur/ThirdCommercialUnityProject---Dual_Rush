using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PerkSelectionUI : MonoBehaviour
{
    public static PerkSelectionUI Instance { get; private set; }

    [Header("Card Slots")]
    [Tooltip("Exactly 3 PerkCardUI children — one per card slot.")]
    public PerkCardUI[] cardSlots = new PerkCardUI[3];

    [Header("All Available Perks")]
    [Tooltip("Drag all PerkSO assets here. 3 random ones will be offered each time.")]
    public PerkSO[] perkPool;

    [Header("Canvas Root")]
    [Tooltip("The root CanvasGroup / panel to toggle. Assign the parent panel here.")]
    public GameObject panelRoot;

    [Header("Dismiss")]
    [Tooltip("Button that skips perk selection without picking any card.")]
    public Button dismissButton;

    [Header("Close Animation")]
    [Tooltip("How long the chosen card celebrates before it zooms out (realtime seconds).")]
    [Range(0f, 1.5f)] public float celebrateHold = 0.45f;
    [Tooltip("Pop scale of the chosen card on pick.")]
    public float chosenPopScale = 1.18f;
    [Tooltip("Final scale the chosen card grows to as it zooms toward the camera and fades.")]
    public float chosenZoomScale = 1.6f;
    [Tooltip("How far the losing cards shrink before they vanish.")]
    public float loserShrinkScale = 0.55f;

    [Header("Background Wallpaper")]
    [Tooltip("Seconds the wallpaper takes to power on when the panel opens.")]
    [Range(0f, 2f)] public float bgRevealInDuration = 0.55f;
    [Tooltip("Seconds the wallpaper takes to power off as the panel closes.")]
    [Range(0f, 2f)] public float bgRevealOutDuration = 0.40f;

    private static readonly int DissolveProp = Shader.PropertyToID("_Dissolve");
    private static readonly int ID_Reveal = Shader.PropertyToID("_Reveal");

    private Action onPerkChosen;
    private bool isResolving;

    public bool PerkChoosing = false;

    //Background shader
    public GameObject backgroundShaderObject;

    public GameObject CursorCanvas;

    private PerkSO[] _offered;
    private Material _bgMat;
    private Tweener _revealTween;



    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (dismissButton != null)
            dismissButton.onClick.AddListener(OnDismissClicked);

        Hide();
    }

    public void Show(Action callback)
    {
        if (perkPool == null || perkPool.Length == 0)
        {
            Debug.LogWarning("[PerkSelectionUI] Perk pool is empty — skipping selection.");
            callback?.Invoke();
            return;
        }

        onPerkChosen = callback;
        isResolving = false;

        // Make sure last close's tweens are fully reverted before we re-deal.
        ResetCardsVisualState();

        _offered = SamplePerks(3);
        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (cardSlots[i] == null) continue;
            cardSlots[i].Initialise(_offered.Length > i ? _offered[i] : null, OnCardClicked);
        }

        panelRoot.SetActive(true);
        backgroundShaderObject.SetActive(true);
        CursorCanvas.SetActive(true);
        SetCursorState(true);

        // Cards render solid every time the panel opens (no leftover dissolve).
        foreach (var m in GatherDissolveMaterials())
            m.SetFloat(DissolveProp, 0f);

        // Power the wallpaper ON (dithered wipe up from palette black).
        var bg = BgMaterial();
        if (bg != null)
        {
            _revealTween?.Kill();
            bg.SetFloat(ID_Reveal, 0f);
            _revealTween = DOTween.To(() => bg.GetFloat(ID_Reveal),
                                      v => bg.SetFloat(ID_Reveal, v), 1f, bgRevealInDuration)
                                  .SetEase(Ease.OutQuad).SetUpdate(true);
        }

        PerkChoosing = true;
        Time.timeScale = 0f;
    }

    private void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (backgroundShaderObject != null) backgroundShaderObject.SetActive(false);
        if (CursorCanvas != null) CursorCanvas.SetActive(false);
        SetCursorState(false);
        Time.timeScale = 1f;
        PerkChoosing = false;
    }

    private void OnCardClicked(PerkSO perk)
    {
        if (isResolving) return;
        isResolving = true;

        PerkManager.Instance.AddPerk(perk);
        int idx = _offered != null ? Array.IndexOf(_offered, perk) : -1;
        AnimateAndClose(idx);
    }

    private void OnDismissClicked()
    {
        if (isResolving) return;
        isResolving = true;

        AnimateAndClose(-1);   // no winner — every card just clears away
    }

    // ── Close animation ──────────────────────────────────────────────────

    private void AnimateAndClose(int chosenIndex)
    {
        // Freeze hover so its per-frame tilt/scale writes don't fight our tweens.
        var hovers = panelRoot.GetComponentsInChildren<PerkHoverEffect>(true);
        foreach (var h in hovers)
        {
            h.enabled = false;
            var cr = h.transform.parent;            // the tilted/scaled card visual
            if (cr != null)
            {
                cr.DOKill();
                cr.localScale = Vector3.one;
                cr.localRotation = Quaternion.identity;
            }
        }

        var seq = DOTween.Sequence().SetUpdate(true);   // unscaled — runs at timeScale 0
        float zoomStart = 0.30f + Mathf.Max(0f, celebrateHold);

        for (int i = 0; i < cardSlots.Length; i++)
        {
            var slot = cardSlots[i];
            if (slot == null || !slot.gameObject.activeSelf) continue;

            var t = (RectTransform)slot.transform;
            var grp = GetGroup(slot.gameObject);
            t.DOKill();

            if (i == chosenIndex)
            {
                // Celebrate: snap upright, punch up, settle, hold, then zoom toward camera and fade.
                seq.Insert(0f, t.DOLocalRotate(Vector3.zero, 0.18f).SetEase(Ease.OutBack));
                seq.Insert(0f, t.DOScale(chosenPopScale, 0.18f).SetEase(Ease.OutBack));
                seq.Insert(0.18f, t.DOScale(1f, 0.12f).SetEase(Ease.OutQuad));
                seq.Insert(zoomStart, t.DOScale(chosenZoomScale, 0.40f).SetEase(Ease.InBack));
                seq.Insert(zoomStart + 0.10f, grp.DOFade(0f, 0.30f).SetEase(Ease.InQuad));
            }
            else
            {
                // Losers shrink, tip slightly, and fade out quickly.
                float dir = (i < chosenIndex || chosenIndex < 0) ? -1f : 1f;
                seq.Insert(0.05f, t.DOScale(loserShrinkScale, 0.30f).SetEase(Ease.InBack));
                seq.Insert(0.05f, t.DOLocalRotate(new Vector3(0f, 0f, dir * 8f), 0.30f).SetEase(Ease.InQuad));
                seq.Insert(0.05f, grp.DOFade(0f, 0.28f).SetEase(Ease.InQuad));
            }
        }

        // Fade the whole panel last so the background and any chrome leave cleanly.
        var panelGrp = GetGroup(panelRoot);
        panelGrp.blocksRaycasts = false;            // no stray clicks mid-close
        float panelStart = (chosenIndex >= 0) ? zoomStart + 0.18f : 0.30f;
        seq.Insert(panelStart, panelGrp.DOFade(0f, 0.28f).SetEase(Ease.InQuad));

        // Power the wallpaper OFF in sync (dithered wipe back down to palette black).
        var bg = BgMaterial();
        if (bg != null)
        {
            _revealTween?.Kill();
            float bgStart = (chosenIndex >= 0) ? zoomStart : 0.10f;
            _revealTween = DOTween.To(() => bg.GetFloat(ID_Reveal),
                                      v => bg.SetFloat(ID_Reveal, v), 0f, bgRevealOutDuration)
                                  .SetEase(Ease.InQuad);
            seq.Insert(bgStart, _revealTween);       // managed by the sequence (unscaled)
        }

        seq.OnComplete(FinishClose);
    }

    private void FinishClose()
    {
        Hide();
        ResetCardsVisualState();
        onPerkChosen?.Invoke();
        onPerkChosen = null;
    }

    /// <summary>Reverts everything the close animation touched, ready for the next open.</summary>
    private void ResetCardsVisualState()
    {
        if (panelRoot == null) return;

        foreach (var slot in cardSlots)
        {
            if (slot == null) continue;
            var t = slot.transform;
            t.DOKill();
            t.localScale = Vector3.one;
            t.localRotation = Quaternion.identity;

            var grp = slot.GetComponent<CanvasGroup>();
            if (grp != null) { grp.alpha = 1f; grp.interactable = true; grp.blocksRaycasts = true; }
        }

        var hovers = panelRoot.GetComponentsInChildren<PerkHoverEffect>(true);
        foreach (var h in hovers)
        {
            var cr = h.transform.parent;
            if (cr != null)
            {
                cr.DOKill();
                cr.localScale = Vector3.one;
                cr.localRotation = Quaternion.identity;
            }
            h.enabled = true;
        }

        var panelGrp = panelRoot.GetComponent<CanvasGroup>();
        if (panelGrp != null) { panelGrp.alpha = 1f; panelGrp.blocksRaycasts = true; }
    }

    private Material BgMaterial()
    {
        if (_bgMat != null) return _bgMat;
        if (backgroundShaderObject == null) return null;

        // Share the SAME instance the time driver feeds _UnscaledTime to —
        // otherwise _Reveal and _UnscaledTime land on different materials and the
        // visible one freezes.
        var driver = backgroundShaderObject.GetComponent<PerkBackgroundTimeDriver>();
        if (driver != null) _bgMat = driver.ActiveMaterial;
        if (_bgMat == null)
        {
            var r = backgroundShaderObject.GetComponent<Renderer>();
            if (r != null) _bgMat = r.material;
        }
        return _bgMat;
    }

    private static CanvasGroup GetGroup(GameObject go)
    {
        var g = go.GetComponent<CanvasGroup>();
        if (g == null) g = go.AddComponent<CanvasGroup>();
        return g;
    }

    /// <summary>
    /// Finds every UI Graphic under panelRoot whose material exposes _Dissolve —
    /// i.e. every object using the DualRush/PerkCard/DitherGlow shader.
    /// </summary>
    private List<Material> GatherDissolveMaterials()
    {
        var list = new List<Material>();
        if (panelRoot == null) return list;

        var graphics = panelRoot.GetComponentsInChildren<Graphic>(true);
        foreach (var g in graphics)
        {
            var m = g.material;
            if (m != null && m.HasProperty(DissolveProp))
                list.Add(m);
        }
        return list;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private PerkSO[] SamplePerks(int count)
    {
        count = Mathf.Min(count, perkPool.Length);
        List<PerkSO> pool = new List<PerkSO>(perkPool);
        PerkSO[] result = new PerkSO[count];

        for (int i = 0; i < count; i++)
        {
            int idx = UnityEngine.Random.Range(0, pool.Count);
            result[i] = pool[idx];
            pool.RemoveAt(idx);
        }

        return result;
    }

    private static void SetCursorState(bool visible)
    {
        Cursor.visible = false;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}