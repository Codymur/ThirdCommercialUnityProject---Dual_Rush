using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Screen-space canvas that presents 3 perk card choices to the player.
/// Hides the cursor while closed; shows and locks it while open.
/// Call <see cref="Show"/> from <see cref="PerkPickup"/> to open it.
/// </summary>
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
    public UnityEngine.UI.Button dismissButton;

    private Action onPerkChosen;

    public bool PerkChoosing = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (dismissButton != null)
            dismissButton.onClick.AddListener(OnDismissClicked);

        Hide();
    }

    /// <summary>
    /// Randomly samples 3 perks from the pool and presents the selection UI.
    /// <paramref name="callback"/> is invoked after the player picks one.
    /// </summary>
    public void Show(Action callback)
    {
        if (perkPool == null || perkPool.Length == 0)
        {
            Debug.LogWarning("[PerkSelectionUI] Perk pool is empty — skipping selection.");
            callback?.Invoke();
            return;
        }

        onPerkChosen = callback;

        PerkSO[] chosen = SamplePerks(3);
        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (cardSlots[i] == null) continue;
            cardSlots[i].Initialise(chosen[i], OnCardClicked);
        }

        panelRoot.SetActive(true);
        SetCursorState(true);

        PerkChoosing = true;

        // Pause gameplay time so enemies don't move while choosing.
        Time.timeScale = 0f;
    }

    private void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        SetCursorState(false);
        Time.timeScale = 1f;
        PerkChoosing = false;
    }

    private void OnCardClicked(PerkSO perk)
    {
        PerkManager.Instance.AddPerk(perk);
        Hide();
        onPerkChosen?.Invoke();
        onPerkChosen = null;
    }

    private void OnDismissClicked()
    {
        Hide();
        onPerkChosen?.Invoke();
        onPerkChosen = null;
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
        Cursor.visible   = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
