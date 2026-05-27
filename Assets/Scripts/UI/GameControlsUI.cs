using UnityEngine;

/// <summary>
/// Toggles the GameControlsPanel visibility when the Tab key is pressed.
/// Attach to the Canvas or any persistent GameObject in the scene.
/// </summary>
public class GameControlsUI : MonoBehaviour
{
    // ── References ───────────────────────────────────────────────────────────
    [Header("References")]
    [Tooltip("The panel GameObject to show/hide.")]
    public GameObject gameControlsPanel;

    // ── Input ────────────────────────────────────────────────────────────────
    [Header("Input")]
    public KeyCode toggleKey = KeyCode.Tab;

    private void Start()
    {
        if (gameControlsPanel != null)
            gameControlsPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            Toggle();
    }

    /// <summary>
    /// Toggles the panel between visible and hidden.
    /// </summary>
    public void Toggle()
    {
        if (gameControlsPanel == null) return;
        gameControlsPanel.SetActive(!gameControlsPanel.activeSelf);
    }
}
