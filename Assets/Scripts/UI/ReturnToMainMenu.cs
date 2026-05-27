using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Loads the Main Menu scene when the player presses Escape.
/// Attach to any persistent GameObject in a level scene.
/// </summary>
public class ReturnToMainMenu : MonoBehaviour
{
    // ── Input ────────────────────────────────────────────────────────────────
    [Header("Input")]
    public KeyCode returnKey = KeyCode.Escape;

    // ── Scene ────────────────────────────────────────────────────────────────
    [Header("Scene")]
    [Tooltip("Exact name of the Main Menu scene as it appears in Build Settings.")]
    public string mainMenuSceneName = "MainMenu";

    private void Update()
    {
        if (Input.GetKeyDown(returnKey))
            ReturnToMenu();
    }

    /// <summary>
    /// Loads the Main Menu scene. Can also be called from a UI button.
    /// </summary>
    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
