using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Developer Test Flags")]
    [Tooltip("Toggle this via the Dev UI to allow or lock physical tile swapping.")]
    // SUGGESTION: Default is now disabled so players don't accidentally swap layout configurations.
    public bool isTileSwitchingEnabled = false;

    [Tooltip("Toggle this via the Dev UI to allow or lock tile rotation interactions globally.")]
    public bool isTileRotationEnabled = true;

    [Header("Economy Interaction Target")]
    public TileProperty selectedTileProperty;

    public int tierUpgradeCost = 40;
    public int morphHazardCost = 60;

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

    [Header("Wave Tracker")]
    public int currentWave = 1;
    public int totalWaves = 5;

    private EnemySpawner spawnerScript;
    private int selectedMorphIndex = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        spawnerScript = FindObjectOfType<EnemySpawner>();
        ChangeState(GameState.MainMenu);
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;
        switch (currentState)
        {
            case GameState.MainMenu:
                Time.timeScale = 0f;
                if (spawnerScript != null) spawnerScript.enabled = false;
                break;
            case GameState.DifficultySelect:
                Time.timeScale = 0f;
                break;
            case GameState.Gameplay:
                Time.timeScale = 1f;
                ResetGameplaySession();
                if (spawnerScript != null) spawnerScript.enabled = true;
                break;
            case GameState.GameOver:
                Time.timeScale = 0f;
                if (spawnerScript != null) spawnerScript.enabled = false;
                break;
            case GameState.Victory:
                Time.timeScale = 0f;
                if (spawnerScript != null) spawnerScript.enabled = false;
                break;
        }
    }

    private void ResetGameplaySession()
    {
        currentBaseHealth = maxBaseHealth;
        currentWave = 1;

        Enemy[] activeEnemies = FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in activeEnemies) Destroy(enemy.gameObject);

        GridManager gridRef = FindObjectOfType<GridManager>();
        if (gridRef != null) gridRef.GenerateNewWaveConstraints(1);
    }

    public void SelectDifficulty(string difficultyName)
    {
        selectedDifficultyName = difficultyName;
        switch (difficultyName.ToLower())
        {
            case "easy": enemyHealthMultiplier = 0.75f; enemySpeedMultiplier = 0.85f; break;
            case "normal": enemyHealthMultiplier = 1.0f; enemySpeedMultiplier = 1.0f; break;
            case "hard": enemyHealthMultiplier = 1.4f; enemySpeedMultiplier = 1.2f; break;
        }
        ChangeState(GameState.Gameplay);
    }

    public void DamageBase(int damageAmount)
    {
        if (currentState != GameState.Gameplay) return;
        currentBaseHealth -= damageAmount;
        if (currentBaseHealth <= 0)
        {
            currentBaseHealth = 0;
            ChangeState(GameState.GameOver);
        }
    }

    public void AdvanceWave()
    {
        currentWave++;
        if (currentWave > totalWaves) ChangeState(GameState.Victory);
        else
        {
            GridManager gridRef = FindObjectOfType<GridManager>();
            if (gridRef != null) gridRef.GenerateNewWaveConstraints(currentWave);
        }
    }

    void OnGUI()
    {
        GridManager gridRef = FindObjectOfType<GridManager>();
        bool isPathValid = (gridRef != null) ? gridRef.isCurrentPathValid : true;

        // Dynamic padding adjustment for the extra UI button line
        int panelHeight = 150;
        if (currentState == GameState.Gameplay)
        {
            panelHeight = 345; // Increased slightly to comfortably room both toggle tracks
            if (!isPathValid) panelHeight += 20;
            if (selectedTileProperty != null) panelHeight += 160;
        }

        GUI.Box(new Rect(15, 15, 230, panelHeight), $"⚙️ DEV SYSTEM [{currentState.ToString().ToUpper()}]");

        if (currentState == GameState.Gameplay)
        {
            GUI.Label(new Rect(25, 40, 200, 22), $"Difficulty: {selectedDifficultyName.ToUpper()}");
            GUI.Label(new Rect(25, 60, 200, 22), $"❤️ Base HP: {currentBaseHealth} / {maxBaseHealth}");
            GUI.Label(new Rect(25, 80, 200, 22), $"⚔️ Wave: {currentWave} / {totalWaves}");

            int currentGold = EconomyManager.Instance != null ? EconomyManager.Instance.GetCurrentGold() : 0;
            GUI.Label(new Rect(25, 100, 200, 22), $"💰 Current Gold: {currentGold}g");

            int runningYOffset = 125;

            if (gridRef != null)
            {
                GUI.Box(new Rect(25, runningYOffset, 210, 2), "");
                runningYOffset += 8;

                int currentActiveLength = gridRef.currentPathWorldPositions.Count;
                string lengthColorTag = (currentActiveLength >= gridRef.minPathLength && currentActiveLength <= gridRef.maxPathLength) ? "<color=#00FF00>" : "<color=#FFA500>";
                GUIStyle dynamicRichTextStyle = new GUIStyle(GUI.skin.label) { richText = true };

                GUI.Label(new Rect(25, runningYOffset, 200, 20), $"🔹 Min Required: {gridRef.minPathLength} tiles");
                runningYOffset += 18;
                GUI.Label(new Rect(25, runningYOffset, 200, 20), $"🔺 Max Required: {gridRef.maxPathLength} tiles");
                runningYOffset += 18;
                GUI.Label(new Rect(25, runningYOffset, 200, 20), $"📏 Current Path: {lengthColorTag}<b>{currentActiveLength}</b></color> tiles", dynamicRichTextStyle);
                runningYOffset += 22;

                GUI.Box(new Rect(25, runningYOffset, 210, 2), "");
                runningYOffset += 10;
            }

            if (spawnerScript == null) spawnerScript = FindObjectOfType<EnemySpawner>();

            if (spawnerScript != null && !spawnerScript.IsWaveRunning())
            {
                if (!isPathValid && gridRef != null)
                {
                    GUI.color = Color.red;
                    GUI.Label(new Rect(25, runningYOffset, 200, 22), gridRef.pathValidationErrorMessage);
                    GUI.color = Color.white;
                    runningYOffset += 22;
                    GUI.enabled = false;
                }

                string spawnButtonText = (spawnerScript.currentWaveIndex == 0 && spawnerScript.currentEnemyIndex == 0) ? "🚀 START WAVE 1" : "▶️ START NEXT WAVE";
                if (GUI.Button(new Rect(25, runningYOffset, 210, 25), spawnButtonText))
                {
                    spawnerScript.StartWave(spawnerScript.currentWaveIndex);
                }
                GUI.enabled = true;
            }
            else
            {
                GUI.enabled = false;
                GUI.Button(new Rect(25, runningYOffset, 210, 25), "🔒 WAVE IN PROGRESS...");
                GUI.enabled = true;
            }

            runningYOffset += 30;

            // --- SEPARATE BUTTON 1: TILE SWAPPING INTERACTION ---
            string swapToggleText = isTileSwitchingEnabled ? "🟢 TILE SWAPPING: ON" : "🔴 TILE SWAPPING: OFF";
            if (GUI.Button(new Rect(25, runningYOffset, 210, 25), swapToggleText))
            {
                isTileSwitchingEnabled = !isTileSwitchingEnabled;
            }

            runningYOffset += 30;

            // --- SEPARATE BUTTON 2: TILE ROTATION LOCK ---
            string rotToggleText = isTileRotationEnabled ? "🔄 TILE ROTATION: UNLOCKED" : "🔒 TILE ROTATION: LOCKED";
            if (GUI.Button(new Rect(25, runningYOffset, 210, 25), rotToggleText))
            {
                isTileRotationEnabled = !isTileRotationEnabled;
            }

            runningYOffset += 35;

            if (GUI.Button(new Rect(25, runningYOffset, 100, 25), "💥 Hit Base")) DamageBase(5);
            if (GUI.Button(new Rect(135, runningYOffset, 100, 25), "⏭️ Skip Wave")) AdvanceWave();

            runningYOffset += 35;

            if (selectedTileProperty != null)
            {
                GUI.Box(new Rect(20, runningYOffset, 220, 3), "");
                runningYOffset += 10;

                GUI.Label(new Rect(25, runningYOffset, 210, 22), $"🎯 Target: {selectedTileProperty.type.ToString().ToUpper()}");
                runningYOffset += 18;
                GUI.Label(new Rect(25, runningYOffset, 210, 22), $"⭐ Strength: Tier {selectedTileProperty.currentTier}");
                runningYOffset += 22;

                bool canUpgrade = selectedTileProperty.currentTier < TileProperty.maxTier;
                string upgradeText = canUpgrade ? $"🔺 Upgrade Tier (-{tierUpgradeCost}g)" : "🔺 MAX STRENGTH";

                if (!canUpgrade || currentGold < tierUpgradeCost) GUI.enabled = false;
                if (GUI.Button(new Rect(25, runningYOffset, 210, 25), upgradeText))
                {
                    if (EconomyManager.Instance.SpendGold(tierUpgradeCost)) selectedTileProperty.UpgradeTileTier();
                }
                GUI.enabled = true;
                runningYOffset += 30;

                string[] rawEnumNames = System.Enum.GetNames(typeof(TileProperty.TileType));
                GUI.Label(new Rect(25, runningYOffset, 210, 18), "Select Target Morph Type:");
                runningYOffset += 18;

                if (GUI.Button(new Rect(25, runningYOffset, 30, 20), "<"))
                {
                    selectedMorphIndex = (selectedMorphIndex == 0) ? rawEnumNames.Length - 1 : selectedMorphIndex - 1;
                }
                GUI.Box(new Rect(60, runningYOffset, 140, 20), rawEnumNames[selectedMorphIndex].ToUpper());
                if (GUI.Button(new Rect(205, runningYOffset, 30, 20), ">"))
                {
                    selectedMorphIndex = (selectedMorphIndex == rawEnumNames.Length - 1) ? 0 : selectedMorphIndex + 1;
                }

                runningYOffset += 25;

                string morphText = $"🎲 Morph to Chosen Element (-{morphHazardCost}g)";
                if (currentGold < morphHazardCost) GUI.enabled = false;

                if (GUI.Button(new Rect(25, runningYOffset, 210, 25), morphText))
                {
                    if (EconomyManager.Instance.SpendGold(morphHazardCost))
                    {
                        selectedTileProperty.type = (TileProperty.TileType)selectedMorphIndex;
                        selectedTileProperty.RefreshVisuals(isHighlighted: false);
                        if (gridRef != null) gridRef.TracePath();
                    }
                }
                GUI.enabled = true;
                runningYOffset += 30;
            }
        }
        else if (currentState == GameState.MainMenu)
        {
            if (GUI.Button(new Rect(35, 60, 180, 40), "START GAME")) ChangeState(GameState.DifficultySelect);
        }
        else if (currentState == GameState.DifficultySelect)
        {
            GUI.Label(new Rect(25, 45, 200, 25), "Pick Difficulty:");
            if (GUI.Button(new Rect(25, 75, 50, 30), "Easy")) SelectDifficulty("easy");
            if (GUI.Button(new Rect(85, 75, 65, 30), "Normal")) SelectDifficulty("normal");
            if (GUI.Button(new Rect(160, 75, 50, 30), "Hard")) SelectDifficulty("hard");
        }
        else if (currentState == GameState.GameOver)
        {
            GUI.Label(new Rect(25, 50, 200, 30), "☠️ BASE OVERRUN! ☠️");
            if (GUI.Button(new Rect(25, 85, 200, 35), "Return to Menu")) ChangeState(GameState.MainMenu);
        }
        else if (currentState == GameState.Victory)
        {
            GUI.Label(new Rect(25, 50, 200, 30), "🏆 MAP CLEARED! 🏆");
            if (GUI.Button(new Rect(25, 85, 200, 35), "Play Again")) ChangeState(GameState.MainMenu);
        }
    }

    public bool IsMouseOverUserInterface()
    {
        if (currentState != GameState.Gameplay) return false;

        Vector2 mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        GridManager gridRef = FindObjectOfType<GridManager>();
        bool isPathValid = (gridRef != null) ? gridRef.isCurrentPathValid : true;

        int panelHeight = 345;
        if (!isPathValid) panelHeight += 20;
        if (selectedTileProperty != null) panelHeight += 160;

        Rect uiPanelRect = new Rect(15, 15, 230, panelHeight);
        return uiPanelRect.Contains(mousePos);
    }
}