using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    // Singleton instance allowing any script to easily read the game state
    public static GameManager Instance { get; private set; }

    [Header("Developer Test Flags")]
    [Tooltip("Toggle this via the Dev UI to enable or completely lock tile selection interactions.")]
    public bool isTileSwitchingEnabled = true;

    [Header("Economy Interaction Target")]
    [Tooltip("The tile currently being highlighted by the SelectionManager.")]
    public TileProperty selectedTileProperty;

    // Economic price settings
    public int tierUpgradeCost = 40;
    public int morphHazardCost = 60;

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

        // --- STEP 3 (A): INITIALIZE PUZZLE CONSTRAINTS ---
        // Setup the baseline layout puzzle challenges for the very first wave!
        GridManager gridRef = FindObjectOfType<GridManager>();
        if (gridRef != null)
        {
            gridRef.GenerateNewWaveConstraints(1);
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
        else
        {
            // --- STEP 3 (B): RE-RANDOMIZE PUZZLE CONSTRAINTS FOR NEXT ROUND ---
            // Triggers fresh mandatory checkpoint and grid size bounds between rounds
            GridManager gridRef = FindObjectOfType<GridManager>();
            if (gridRef != null)
            {
                gridRef.GenerateNewWaveConstraints(currentWave);
            }
        }
    }

    void OnGUI()
    {
        GridManager gridRef = FindObjectOfType<GridManager>();
        bool isPathValid = (gridRef != null) ? gridRef.isCurrentPathValid : true;

        // --- MASTER HEIGHT CALCULATION ---
        // Dynamically scales panel box height to accommodate error messages and selected tile parameters cleanly
        int panelHeight = 150; // Menu baseline height default
        if (currentState == GameState.Gameplay)
        {
            panelHeight = 245; // Baseline gameplay stats dimensions
            if (!isPathValid) panelHeight += 20; // Room for validation warning labels
            if (selectedTileProperty != null) panelHeight += 115; // Room for upgrade sub-menus
        }

        GUI.Box(new Rect(15, 15, 220, panelHeight), $"⚙️ DEV SYSTEM [{currentState.ToString().ToUpper()}]");

        // ==========================================
        // 1. GAMEPLAY STATE PANEL CONTENT
        // ==========================================
        if (currentState == GameState.Gameplay)
        {
            // --- SECTION 1: MATCH STATS ---
            GUI.Label(new Rect(25, 40, 200, 22), $"Difficulty: {selectedDifficultyName.ToUpper()}");
            GUI.Label(new Rect(25, 60, 200, 22), $"❤️ Base HP: {currentBaseHealth} / {maxBaseHealth}");
            GUI.Label(new Rect(25, 80, 200, 22), $"⚔️ Wave: {currentWave} / {totalWaves}");

            int currentGold = EconomyManager.Instance != null ? EconomyManager.Instance.GetCurrentGold() : 0;
            GUI.Label(new Rect(25, 100, 200, 22), $"💰 Current Gold: {currentGold}g");

            // --- STEP 3 (C): PATH CONSTRAINTS DISPLAYER & GATEKEEPER ---
            int runningYOffset = 125;

            if (spawnerScript == null) spawnerScript = FindObjectOfType<EnemySpawner>();

            if (spawnerScript != null && !spawnerScript.IsWaveRunning())
            {
                // If the map path violates rules, render the error text string in bright red
                if (!isPathValid && gridRef != null)
                {
                    GUI.color = Color.red;
                    GUI.Label(new Rect(25, runningYOffset, 200, 22), gridRef.pathValidationErrorMessage);
                    GUI.color = Color.white;

                    runningYOffset += 22; // Offset positions downwards to prevent drawing on buttons
                    GUI.enabled = false;   // LOCKS the button so clicking it does nothing!
                }

                string spawnButtonText = (spawnerScript.currentWaveIndex == 0 && spawnerScript.currentEnemyIndex == 0) ? "🚀 START WAVE 1" : "▶️ START NEXT WAVE";
                if (GUI.Button(new Rect(25, runningYOffset, 200, 25), spawnButtonText))
                {
                    spawnerScript.StartWave(spawnerScript.currentWaveIndex);
                }
                GUI.enabled = true; // Safely unlock interactive elements for sections following it
            }
            else
            {
                GUI.enabled = false;
                GUI.Button(new Rect(25, runningYOffset, 200, 25), "🔒 WAVE IN PROGRESS...");
                GUI.enabled = true;
            }

            runningYOffset += 30; // Move forward to the swap options segment

            // --- SECTION 3: TILE SWAPPING TOGGLE ---
            string toggleText = isTileSwitchingEnabled ? "🟢 SWAPPING: ALLOWED" : "🔴 SWAPPING: LOCKED";
            if (GUI.Button(new Rect(25, runningYOffset, 200, 25), toggleText))
            {
                isTileSwitchingEnabled = !isTileSwitchingEnabled;
            }

            runningYOffset += 35; // Move down for utility rows

            // --- SECTION 4: CHEATS & UTILITIES ---
            if (GUI.Button(new Rect(25, runningYOffset, 95, 25), "💥 Hit Base")) DamageBase(5);
            if (GUI.Button(new Rect(130, runningYOffset, 95, 25), "⏭️ Skip Wave")) AdvanceWave();

            runningYOffset += 35;

            // ==========================================
            // DYNAMIC CONTEXT MENU: TILE UPGRADE OVERLAY
            // ==========================================
            if (selectedTileProperty != null)
            {
                // Visual dividing line
                GUI.Box(new Rect(20, runningYOffset, 210, 3), "");
                runningYOffset += 10;

                GUI.Label(new Rect(25, runningYOffset, 200, 22), $"🎯 Target: {selectedTileProperty.type.ToString().ToUpper()}");
                runningYOffset += 20;
                GUI.Label(new Rect(25, runningYOffset, 200, 22), $"⭐ Current Strength: Tier {selectedTileProperty.currentTier}");
                runningYOffset += 25;

                // BUTTON A: UPGRADE TIER
                bool canUpgrade = selectedTileProperty.currentTier < TileProperty.maxTier;
                string upgradeText = canUpgrade ? $"🔺 Upgrade Tier (-{tierUpgradeCost}g)" : "🔺 MAX STRENGTH";

                if (!canUpgrade || currentGold < tierUpgradeCost) GUI.enabled = false;
                if (GUI.Button(new Rect(25, runningYOffset, 200, 25), upgradeText))
                {
                    if (EconomyManager.Instance.SpendGold(tierUpgradeCost))
                    {
                        selectedTileProperty.UpgradeTileTier();
                    }
                }
                GUI.enabled = true;
                runningYOffset += 30;

                // BUTTON B: MORPH TILE TYPE
                string morphText = $"🎲 Morph Element (-{morphHazardCost}g)";
                if (currentGold < morphHazardCost || selectedTileProperty.type == TileProperty.TileType.Normal) GUI.enabled = false;

                if (GUI.Button(new Rect(25, runningYOffset, 200, 25), morphText))
                {
                    if (EconomyManager.Instance.SpendGold(morphHazardCost))
                    {
                        int totalTypes = System.Enum.GetValues(typeof(TileProperty.TileType)).Length;
                        int randomTypeIndex = Random.Range(1, totalTypes); // Skip normal
                        selectedTileProperty.type = (TileProperty.TileType)randomTypeIndex;
                        selectedTileProperty.RefreshVisuals(isHighlighted: false);
                        Debug.Log($"[DYNAMISM] Morphed element target into: {selectedTileProperty.type}");
                    }
                }
                GUI.enabled = true;
            }
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