using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public Animator transitionAnimator;
    public float transitionTime = 0.5f;

    public void LoadNextScene(string sceneName)
    {
        StartCoroutine(TransitionCoroutine(sceneName));
    }

    private IEnumerator TransitionCoroutine(string sceneName)
    {
        // Trigger the fade out animation
        transitionAnimator.SetTrigger("Start");

        // Wait for the animation to finish playing
        yield return new WaitForSeconds(transitionTime);

        // Load the gameplay scene grid
        SceneManager.LoadScene(sceneName);
    }
}