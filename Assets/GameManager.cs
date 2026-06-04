using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    // Singleton instance allowing any script to easily read the game state
    public static GameManager Instance { get; private set; }

    // Define the distinct game states
    public enum GameState { MainMenu, DifficultySelect, Gameplay, GameOver, Victory }

    [Header("Current State")]
    public GameState currentState;

    [Header("Player Base Stats")]
    public int maxBaseHealth = 20;
    public int currentBaseHealth;

    [Header("Difficulty Modifiers")]
    public float enemyHealthMultiplier = 1f;
    public float enemySpeedMultiplier = 1f;
    public string selectedDifficultyName = "Normal";

    [Header("Wave Tracker (Optional placeholder)")]
    public int currentWave = 1;
    public int totalWaves = 5;

    private EnemySpawner spawnerScript;

    void Awake()
    {
        // Setup Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Keeps flow intact across scene transitions if needed
    }

    void Start()
    {
        spawnerScript = FindObjectOfType<EnemySpawner>();

        // Boot the game straight into the Main Menu state
        ChangeState(GameState.MainMenu);
    }

    /// <summary>
    /// Master method to route game state changes cleanly with tracking logs
    /// </summary>
    public void ChangeState(GameState newState)
    {
        currentState = newState;
        Debug.Log($"[GAMEMANAGER] State transitioned to: {currentState}");

        switch (currentState)
        {
            case GameState.MainMenu:
                Time.timeScale = 0f; // Pause any background updates
                if (spawnerScript != null) spawnerScript.enabled = false;
                break;

            case GameState.DifficultySelect:
                Time.timeScale = 0f;
                break;

            case GameState.Gameplay:
                Time.timeScale = 1f; // Run the engine at normal speed
                ResetGameplaySession();
                if (spawnerScript != null) spawnerScript.enabled = true; // Wake up spawner
                break;

            case GameState.GameOver:
                Time.timeScale = 0f; // Freeze game on loss
                if (spawnerScript != null) spawnerScript.enabled = false;
                Debug.Log("[GAME OVER] The base was overrun!");
                break;

            case GameState.Victory:
                Time.timeScale = 0f; // Freeze game on win
                if (spawnerScript != null) spawnerScript.enabled = false;
                Debug.Log("[VICTORY] All waves cleared successfully!");
                break;
        }
    }

    private void ResetGameplaySession()
    {
        currentBaseHealth = maxBaseHealth;
        currentWave = 1;
        // Destroy any leftover enemies in the scene when restarting
        Enemy[] activeEnemies = FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in activeEnemies)
        {
            Destroy(enemy.gameObject);
        }
    }

    /// <summary>
    /// Configures game modifiers dynamically before the match initializes
    /// </summary>
    public void SelectDifficulty(string difficulty)
    {
        selectedDifficultyName = difficulty;

        switch (difficulty.ToLower())
        {
            case "easy":
                enemyHealthMultiplier = 0.75f;
                enemySpeedMultiplier = 0.8f;
                break;
            case "normal":
                enemyHealthMultiplier = 1.0f;
                enemySpeedMultiplier = 1.0f;
                break;
            case "hard":
                enemyHealthMultiplier = 1.5f;
                enemySpeedMultiplier = 1.2f;
                break;
        }

        Debug.Log($"[DIFFICULTY SET] {selectedDifficultyName} Mode: HP x{enemyHealthMultiplier}, Speed x{enemySpeedMultiplier}");

        // Automatically start the map once difficulty is locked in
        ChangeState(GameState.Gameplay);
    }

    /// <summary>
    /// Call this from your destination point/base trigger when an enemy sneaks through
    /// </summary>
    public void DamageBase(int damageAmount)
    {
        if (currentState != GameState.Gameplay) return;

        currentBaseHealth -= damageAmount;
        Debug.Log($"[BASE DAMAGE] Base took {damageAmount} damage! Health Remaining: {currentBaseHealth}/{maxBaseHealth}");

        if (currentBaseHealth <= 0)
        {
            currentBaseHealth = 0;
            ChangeState(GameState.GameOver);
        }
    }

    /// <summary>
    /// Placeholder loop mechanism to test moving to a victory screen
    /// </summary>
    public void AdvanceWave()
    {
        currentWave++;
        Debug.Log($"[WAVE UPDATE] Starting Wave {currentWave}/{totalWaves}");
        if (currentWave > totalWaves)
        {
            ChangeState(GameState.Victory);
        }
    }

    // --- TEMPORARY SCREEN DRAFTING UI ---
    // This renders raw buttons on the screen so you can completely bypass UI designs for now.
    void OnGUI()
    {
        // Custom box styling for transparency
        GUI.Box(new Rect(10, 10, 250, 160), $"--- DEV FLOW PANEL ---");
        GUI.Label(new Rect(20, 35, 230, 25), $"Current State: {currentState}");

        if (currentState == GameState.Gameplay)
        {
            GUI.Label(new Rect(20, 60, 230, 25), $"Base HP: {currentBaseHealth}/{maxBaseHealth}");
            GUI.Label(new Rect(20, 85, 230, 25), $"Difficulty: {selectedDifficultyName}");
            GUI.Label(new Rect(20, 110, 230, 25), $"Wave: {currentWave}/{totalWaves}");

            // Debug button to test losing health manually
            if (GUI.Button(new Rect(20, 135, 100, 25), "Damage Base")) DamageBase(5);
            if (GUI.Button(new Rect(130, 135, 100, 25), "Next Wave")) AdvanceWave();
        }

        // State Machine Screen Controls
        if (currentState == GameState.MainMenu)
        {
            if (GUI.Button(new Rect(40, 60, 180, 40), "START GAME"))
            {
                ChangeState(GameState.DifficultySelect);
            }
        }
        else if (currentState == GameState.DifficultySelect)
        {
            GUI.Label(new Rect(20, 45, 230, 25), "Pick Difficulty:");
            if (GUI.Button(new Rect(40, 70, 50, 30), "Easy")) SelectDifficulty("easy");
            if (GUI.Button(new Rect(100, 70, 60, 30), "Normal")) SelectDifficulty("normal");
            if (GUI.Button(new Rect(170, 70, 50, 30), "Hard")) SelectDifficulty("hard");
        }
        else if (currentState == GameState.GameOver)
        {
            GUI.Label(new Rect(40, 60, 180, 30), "☠️ BASE OVERRUN! ☠️");
            if (GUI.Button(new Rect(40, 95, 180, 35), "Return to Menu")) ChangeState(GameState.MainMenu);
        }
        else if (currentState == GameState.Victory)
        {
            GUI.Label(new Rect(40, 60, 180, 30), "🏆 VICTORY! MAP CLEARED! 🏆");
            if (GUI.Button(new Rect(40, 95, 180, 35), "Play Again")) ChangeState(GameState.MainMenu);
        }
    }
}