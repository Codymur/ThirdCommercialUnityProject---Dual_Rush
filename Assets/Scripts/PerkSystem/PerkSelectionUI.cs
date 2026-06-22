using System;
using System.Collections;
using System.Collections.Generic;
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

    [Header("Selection Delay")]
    [Tooltip("How long the panel lingers after a card is picked before dissolving.")]
    [Range(0f, 3f)] public float postPickDelay = 0.6f;

    [Tooltip("How long the dissolve animation takes (seconds, realtime).")]
    [Range(0f, 2f)] public float dissolveDuration = 0.55f;

    private static readonly int DissolveProp = Shader.PropertyToID("_Dissolve");

    private Action onPerkChosen;
    private bool isResolving;

    public bool PerkChoosing = false;

    //Background shader
    public GameObject backgroundShaderObject;

    public GameObject CursorCanvas;

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

        PerkSO[] chosen = SamplePerks(3);
        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (cardSlots[i] == null) continue;
            cardSlots[i].Initialise(chosen[i], OnCardClicked);
        }

        panelRoot.SetActive(true);
        backgroundShaderObject.SetActive(true);
        CursorCanvas.SetActive(true);
        SetCursorState(true);

        // reset dissolve so cards appear solid every time the panel opens
        foreach (var m in GatherDissolveMaterials())
            m.SetFloat(DissolveProp, 0f);

        PerkChoosing = true;
        Time.timeScale = 0f;
    }

    private void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        backgroundShaderObject.SetActive(false);
        CursorCanvas.SetActive(false);
        SetCursorState(false);
        Time.timeScale = 1f;
        PerkChoosing = false;
    }

    private void OnCardClicked(PerkSO perk)
    {
        if (isResolving) return;
        isResolving = true;

        PerkManager.Instance.AddPerk(perk);
        StartCoroutine(DissolveAndClose(postPickDelay));
    }

    private void OnDismissClicked()
    {
        if (isResolving) return;
        isResolving = true;

        StartCoroutine(DissolveAndClose(0f));   // no linger on dismiss
    }

    private IEnumerator DissolveAndClose(float lingerSeconds)
    {
        if (lingerSeconds > 0f)
            yield return new WaitForSecondsRealtime(lingerSeconds);

        var mats = GatherDissolveMaterials();

        if (dissolveDuration > 0f && mats.Count > 0)
        {
            float t = 0f;
            while (t < dissolveDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / dissolveDuration);
                for (int i = 0; i < mats.Count; i++)
                    mats[i].SetFloat(DissolveProp, p);
                yield return null;
            }
            for (int i = 0; i < mats.Count; i++)
                mats[i].SetFloat(DissolveProp, 1f);
        }

        Hide();

        // restore so the next open shows solid cards
        for (int i = 0; i < mats.Count; i++)
            mats[i].SetFloat(DissolveProp, 0f);

        onPerkChosen?.Invoke();
        onPerkChosen = null;
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