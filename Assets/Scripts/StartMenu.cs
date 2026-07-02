using UnityEngine;
using UnityEngine.SceneManagement; // Required for manipulating and changing scenes

public class StartMenu : MonoBehaviour
{
    // This function will be triggered by your UI Play Button
    public void PlayGame()
    {
        // Sends a message to the Unity Console terminal
        Debug.Log("Play button clicked! Loading the next scene...");

        // Gets the build index of the current active scene and adds 1 to load the next one
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // This function will be triggered by your UI Quit Button
    public void QuitGame()
    {
        // Sends a message to the Unity Console terminal
        Debug.Log("Quit button clicked! Exiting application...");

        // Closes the application (Only works in a standalone built game)
        Application.Quit();
    }
}