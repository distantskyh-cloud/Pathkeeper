using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    // Singleton instance allowing any script to easily read the game state
    public static GameManager Instance { get; private set; }

    [Header("Developer Test Flags")]
    [Tooltip("Toggle this via the Dev UI to enable or completely lock tile selection interactions.")]
    public bool isTileSwitchingEnabled = true;

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
    public void SelectDifficulty(string difficultyName)
    {
        selectedDifficultyName = difficultyName;

        switch (difficultyName.ToLower())
        {
            case "easy":
                enemyHealthMultiplier = 0.75f;
                enemySpeedMultiplier = 0.85f;
                break;
            case "normal":
                enemyHealthMultiplier = 1.0f;
                enemySpeedMultiplier = 1.0f;
                break;
            case "hard":
                enemyHealthMultiplier = 1.4f;
                enemySpeedMultiplier = 1.2f;
                break;
        }

        Debug.Log($"[DIFFICULTY SET] Selected: {selectedDifficultyName}. HP Mult: {enemyHealthMultiplier}, Speed Mult: {enemySpeedMultiplier}");

        // Move into the gameplay state, but DO NOT automatically kickstart the spawner anymore!
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
        // --- STATE-ADAPTIVE MASTER BOX PANEL ---
        // Width reduced by 10 (now 220). Height scales automatically based on what screen you are looking at!
        int panelHeight = (currentState == GameState.Gameplay) ? 225 : 150;
        GUI.Box(new Rect(15, 15, 220, panelHeight), $"⚙️ DEV SYSTEM [{currentState.ToString().ToUpper()}]");

        // ==========================================
        // 1. GAMEPLAY STATE PANEL CONTENT
        // ==========================================
        if (currentState == GameState.Gameplay)
        {
            // --- SECTION 1: MATCH STATS ---
            GUI.Label(new Rect(25, 40, 200, 22), $"Difficulty: {selectedDifficultyName.ToUpper()}");
            GUI.Label(new Rect(25, 60, 200, 22), $"Base HP: {currentBaseHealth} / {maxBaseHealth}");
            GUI.Label(new Rect(25, 80, 200, 22), $"Wave: {currentWave} / {totalWaves}");

            // NEW ECONOMY TRACKER DISPLAY
            int currentGold = EconomyManager.Instance != null ? EconomyManager.Instance.GetCurrentGold() : 0;
            GUI.Label(new Rect(25, 100, 200, 22), $"Current Gold: {currentGold}g");

            // --- SECTION 2: WAVE CONTROL BUTTON ---
            if (spawnerScript == null) spawnerScript = FindObjectOfType<EnemySpawner>();

            if (spawnerScript != null && !spawnerScript.IsWaveRunning())
            {
                string spawnButtonText = (spawnerScript.currentWaveIndex == 0 && spawnerScript.currentEnemyIndex == 0) ? "🚀 START WAVE 1" : "▶️ START NEXT WAVE";
                if (GUI.Button(new Rect(25, 130, 200, 25), spawnButtonText))
                {
                    spawnerScript.StartWave(spawnerScript.currentWaveIndex);
                }
            }
            else
            {
                GUI.enabled = false;
                GUI.Button(new Rect(25, 130, 200, 25), "🔒 WAVE IN PROGRESS...");
                GUI.enabled = true;
            }

            // --- SECTION 3: TILE SWAPPING TOGGLE ---
            string toggleText = isTileSwitchingEnabled ? "🟢 SWAPPING: ALLOWED" : "🔴 SWAPPING: LOCKED";
            if (GUI.Button(new Rect(25, 170, 200, 25), toggleText))
            {
                isTileSwitchingEnabled = !isTileSwitchingEnabled;
                Debug.Log($"[DEV TOOL] Tile swapping state changed! Allowed = {isTileSwitchingEnabled}");
            }

            // --- SECTION 4: CHEATS & UTILITIES ---
            // Side-by-side buttons split the 200px width perfectly (95px each with a 10px gap)
            if (GUI.Button(new Rect(25, 205, 95, 25), "💥 Hit Base")) DamageBase(5);
            if (GUI.Button(new Rect(130, 205, 95, 25), "⏭️ Skip Wave")) AdvanceWave();
        }
        // ==========================================
        // 2. MAIN MENU STATE
        // ==========================================
        else if (currentState == GameState.MainMenu)
        {
            if (GUI.Button(new Rect(35, 60, 180, 40), "START GAME"))
            {
                ChangeState(GameState.DifficultySelect);
            }
        }
        // ==========================================
        // 3. DIFFICULTY SELECT STATE
        // ==========================================
        else if (currentState == GameState.DifficultySelect)
        {
            GUI.Label(new Rect(25, 45, 200, 25), "Pick Difficulty:");
            if (GUI.Button(new Rect(25, 75, 50, 30), "Easy")) SelectDifficulty("easy");
            if (GUI.Button(new Rect(85, 75, 65, 30), "Normal")) SelectDifficulty("normal");
            if (GUI.Button(new Rect(160, 75, 50, 30), "Hard")) SelectDifficulty("hard");
        }
        // ==========================================
        // 4. GAME OVER STATE
        // ==========================================
        else if (currentState == GameState.GameOver)
        {
            GUI.Label(new Rect(25, 50, 200, 30), "☠️ BASE OVERRUN! ☠️");
            if (GUI.Button(new Rect(25, 85, 200, 35), "Return to Menu")) ChangeState(GameState.MainMenu);
        }
        // ==========================================
        // 5. VICTORY STATE
        // ==========================================
        else if (currentState == GameState.Victory)
        {
            GUI.Label(new Rect(25, 50, 200, 30), "🏆 MAP CLEARED! 🏆");
            if (GUI.Button(new Rect(25, 85, 200, 35), "Play Again")) ChangeState(GameState.MainMenu);
        }
    }
}