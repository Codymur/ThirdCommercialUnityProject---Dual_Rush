using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles asynchronous scene loading with a transition animation.
/// Fires the "Start" trigger on the transition Animator, waits for the
/// cover animation to finish, then activates the loaded scene.
/// The target scene reveals itself through its own Transition_end default state.
/// </summary>
public class LevelLoader : MonoBehaviour
{
    private const string TransitionTrigger = "Start";
    private const float TransitionDuration = 1f;

    [SerializeField] private Animator _transitionAnimator;

    /// <summary>Loads the scene at the given build index with a transition animation.</summary>
    public void LoadLevel(int sceneIndex)
    {
        StartCoroutine(LoadAsynchronously(sceneIndex));
    }

    /// <summary>Loads the scene by name with a transition animation.</summary>
    public void LoadLevel(string sceneName)
    {
        StartCoroutine(LoadAsynchronouslyByName(sceneName));
    }

    private IEnumerator LoadAsynchronouslyByName(string sceneName)
    {
        _transitionAnimator.SetTrigger(TransitionTrigger);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float elapsed = 0f;
        while (elapsed < TransitionDuration || operation.progress < 0.9f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        operation.allowSceneActivation = true;
    }

    private IEnumerator LoadAsynchronously(int sceneIndex)
    {
        _transitionAnimator.SetTrigger(TransitionTrigger);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
        operation.allowSceneActivation = false;

        float elapsed = 0f;
        while (elapsed < TransitionDuration || operation.progress < 0.9f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        operation.allowSceneActivation = true;
    }

    /// <summary>Quits the application.</summary>
    public void QuitButton()
    {
        Application.Quit();
    }
}
