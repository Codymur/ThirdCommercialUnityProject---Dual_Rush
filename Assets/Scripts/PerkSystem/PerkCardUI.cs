using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives a single perk card slot in the selection UI.
/// Bind <see cref="Initialise"/> from <see cref="PerkSelectionUI"/>.
/// </summary>
public class PerkCardUI : MonoBehaviour
{
    [Header("UI References")]
    public Image       iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI categoryText;
    public Button      selectButton;

    private PerkSO currentPerk;
    private Action<PerkSO> onSelected;

    private void Awake()
    {
        selectButton.onClick.AddListener(OnButtonClicked);
    }

    /// <summary>Fills in the card visuals and wires the selection callback.</summary>
    public void Initialise(PerkSO perk, Action<PerkSO> callback)
    {
        currentPerk = perk;
        onSelected  = callback;

        if (perk == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (nameText        != null) nameText.text        = perk.perkName;
        if (descriptionText != null) descriptionText.text = perk.description;
        if (categoryText    != null) categoryText.text    = perk.category.ToString();
        if (iconImage       != null)
        {
            iconImage.sprite  = perk.icon;
            iconImage.enabled = perk.icon != null;
        }

        var fx = GetComponentInChildren<PerkCardFX>(true);
        if (fx != null) fx.SetCategory(perk.category);
    }

    private void OnButtonClicked() => onSelected?.Invoke(currentPerk);
}
