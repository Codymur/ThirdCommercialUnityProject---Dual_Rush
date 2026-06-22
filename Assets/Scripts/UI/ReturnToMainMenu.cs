using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Loads the Main Menu scene when the player presses Escape.
/// Uses the scene's LevelLoader for a transition animation when one is available,
/// and falls back to a direct load otherwise.
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
    /// Loads the Main Menu scene with a transition animation.
    /// Can also be called from a UI button.
    /// </summary>
    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        LevelLoader loader = Object.FindFirstObjectByType<LevelLoader>();
        if (loader != null)
            loader.LoadLevel(mainMenuSceneName);
        else
            SceneManager.LoadScene(mainMenuSceneName);
    }
}
