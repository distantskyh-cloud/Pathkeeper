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
    /// Core progression pipeline that advances level setup phases naturally or via dev skips
    /// </summary>
    public void AdvanceWave()
    {
        currentWave++;
        Debug.Log($"[WAVE UPDATE] Moving to Planning Phase for Wave {currentWave}/{totalWaves}");

        if (currentWave > totalWaves)
        {
            ChangeState(GameState.Victory);
        }
        else
        {
            // 1. RE-RANDOMIZE PUZZLE CONSTRAINTS FOR THE NEW PLANNING PHASE
            GridManager gridRef = FindObjectOfType<GridManager>();
            if (gridRef != null)
            {
                gridRef.GenerateNewWaveConstraints(currentWave);
            }

            // 2. STOP HERE! 
            // We intentionally do NOT call spawnerScript.StartWave() anymore.
            // The game will sit peacefully in the planning phase, letting the player
            // inspect tiles, upgrade, morph, and fix their maze until they press the UI button.
        }
    }

    void OnGUI()
    {
        GridManager gridRef = FindObjectOfType<GridManager>();
        bool isPathValid = (gridRef != null) ? gridRef.isCurrentPathValid : true;

        // --- MASTER HEIGHT CALCULATION ---
        // Dynamically scales panel box height to accommodate error messages, metrics, and upgrade elements
        int panelHeight = 150; // Menu baseline height default
        if (currentState == GameState.Gameplay)
        {
            panelHeight = 310; // INCREASED FROM 245: Leaves a safe padding buffer for dynamic metrics rows
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

            // =========================================================================
            // LIVE PATH GOAL HUD DISPLAY
            // =========================================================================
            int runningYOffset = 125; // Re-align starting height offset dynamically

            if (gridRef != null)
            {
                // Draw a visual separator line
                GUI.Box(new Rect(25, runningYOffset, 200, 2), "");
                runningYOffset += 8;

                // Grab active layout length step dimensions from world position list tracking arrays
                int currentActiveLength = gridRef.currentPathWorldPositions.Count;

                // Dynamically assert color feedback tags depending on length state parameters
                string lengthColorTag = (currentActiveLength >= gridRef.minPathLength && currentActiveLength <= gridRef.maxPathLength)
                    ? "<color=#00FF00>"  // Bright Green
                    : "<color=#FFA500>"; // Warning Orange

                GUIStyle dynamicRichTextStyle = new GUIStyle(GUI.skin.label) { richText = true };

                GUI.Label(new Rect(25, runningYOffset, 200, 20), $"🔹 Min Required: {gridRef.minPathLength} tiles");
                runningYOffset += 18;
                GUI.Label(new Rect(25, runningYOffset, 200, 20), $"🔺 Max Required: {gridRef.maxPathLength} tiles");
                runningYOffset += 18;
                GUI.Label(new Rect(25, runningYOffset, 200, 20), $"📏 Current Path: {lengthColorTag}<b>{currentActiveLength}</b></color> tiles", dynamicRichTextStyle);
                runningYOffset += 22;

                // Draw a matching visual closing track line
                GUI.Box(new Rect(25, runningYOffset, 200, 2), "");
                runningYOffset += 10;
            }

            // --- SECTION 2: PATH CONSTRAINTS DISPLAYER & GATEKEEPER ---
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
                GUI.enabled = true; // Safely unlock status tracking
                runningYOffset += 30;

                // BUTTON B: MORPH TILE TYPE
                string morphText = $"🎲 Morph Element (-{morphHazardCost}g)";

                // Allow Morphing at all times as long as they can afford it!
                if (currentGold < morphHazardCost) GUI.enabled = false;

                if (GUI.Button(new Rect(25, runningYOffset, 200, 25), morphText))
                {
                    if (EconomyManager.Instance.SpendGold(morphHazardCost))
                    {
                        int totalTypes = System.Enum.GetValues(typeof(TileProperty.TileType)).Length;
                        int randomTypeIndex = Random.Range(1, totalTypes); // Skips Normal (0), selects 1 to max hazard types

                        selectedTileProperty.type = (TileProperty.TileType)randomTypeIndex;
                        selectedTileProperty.RefreshVisuals(isHighlighted: false);

                        // Fire grid updater pass so path recalculations match instantly
                        if (gridRef != null) gridRef.TracePath();

                        Debug.Log($"[DYNAMISM] Morphed element target into: {selectedTileProperty.type}");
                    }
                }
                GUI.enabled = true; // Safely unlock status tracking
                runningYOffset += 30;

                if (!isTileSwitchingEnabled)
                {
                    GUI.Label(new Rect(25, runningYOffset, 200, 22), "🔒 Layout Locked (Safe Upgrade Mode)");
                    runningYOffset += 25;
                }
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

    /// <summary>
    /// Checks if the player's mouse cursor is hovering within the boundaries of the IMGUI command panels
    /// </summary>
    public bool IsMouseOverUserInterface()
    {
        if (currentState != GameState.Gameplay) return false;

        // In Unity IMGUI, screen coordinates start at (0,0) from the TOP-LEFT of the screen.
        // We flip the mouse's vertical position to match this orientation.
        Vector2 mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

        // Define the exact dimensions of your left side panel bounding box
        GridManager gridRef = FindObjectOfType<GridManager>();
        bool isPathValid = (gridRef != null) ? gridRef.isCurrentPathValid : true;

        int panelHeight = 310;
        if (!isPathValid) panelHeight += 20;
        if (selectedTileProperty != null) panelHeight += 115;

        Rect uiPanelRect = new Rect(15, 15, 220, panelHeight);

        // Returns true if the cursor is directly over the left panel, blocking selection raycasts
        return uiPanelRect.Contains(mousePos);
    }
}