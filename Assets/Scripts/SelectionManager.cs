using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    private TileRotation firstSelected;

    [Header("Visual Feedback Settings")]
    public Color selectionHighlightColor = Color.gray;

    void Update()
    {
        // FAILSAFE 2: Check our new GameManager developer toggle button before running selection math!
        if (GameManager.Instance != null && !GameManager.Instance.isTileSwitchingEnabled)
        {
            // If the developer turned off tile switching via the Dev GUI, clear selections and exit
            if (firstSelected != null)
            {
                ResetTileVisual(firstSelected);
                firstSelected = null;
            }
            return;
        }

        // Left Click
        if (Input.GetMouseButtonDown(0))
        {
            HandleSelection();
        }
    }

    void HandleSelection()
    {
        Vector3 clickWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 rayPos = new Vector2(clickWorldPos.x, clickWorldPos.y);

        Collider2D hitCollider = Physics2D.OverlapPoint(rayPos);
        if (hitCollider == null)
        {
            if (firstSelected != null)
            {
                ResetTileVisual(firstSelected);
                firstSelected = null;
                Debug.Log("[SELECTION] Clicked empty space. Cleared active selection.");
            }
            return;
        }

        TileRotation clickedTile = hitCollider.GetComponentInParent<TileRotation>();
        if (clickedTile == null) clickedTile = hitCollider.GetComponentInChildren<TileRotation>();

        if (clickedTile != null)
        {
            // --- FAILSAFE 1: START & END POINTS PROTECTION ---
            GridManager gridManager = FindObjectOfType<GridManager>();
            if (gridManager != null)
            {
                Vector2Int clickedCoords = new Vector2Int(clickedTile.gridX, clickedTile.gridY);

                if (clickedCoords == gridManager.startCoords || clickedCoords == gridManager.endCoords)
                {
                    Debug.LogWarning($"[SELECTION DENIED] Tile at ({clickedTile.gridX}, {clickedTile.gridY}) is a critical Start or End zone and cannot be moved!");
                    return; // Stop processing this selection completely!
                }
            }
            // --------------------------------------------------

            // CASE 1: This is the very first tile being selected
            if (firstSelected == null)
            {
                firstSelected = clickedTile;

                // --- NEW BRIDGE HOOK ---
                // Push this script target reference directly to the GameManager UI!
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.selectedTileProperty = firstSelected.GetComponent<TileProperty>();
                }
                // ------------------------

                SpriteRenderer sr = firstSelected.GetComponent<SpriteRenderer>();
                if (sr == null) sr = firstSelected.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.color = selectionHighlightColor;

                Debug.Log($"[SELECTION] First tile selected at coordinate: {firstSelected.gridX}, {firstSelected.gridY}");
            }
            // CASE 2: Deselect Same Tile
            else if (firstSelected == clickedTile)
            {
                ResetTileVisual(firstSelected);
                firstSelected = null;

                // Clear the active GUI context target
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.selectedTileProperty = null;
                }
            }
            // CASE 3: A second, distinct tile was clicked -> Perform the swap!
            else
            {
                Debug.Log($"[SELECTION] Second tile selected at {clickedTile.gridX}, {clickedTile.gridY}. Checking transaction parameters.");

                int swapCost = 10; // Customize your swap cost balancing here!

                // Verify the player has the money available before running calculations
                if (EconomyManager.Instance != null && !EconomyManager.Instance.CanAfford(swapCost))
                {
                    Debug.LogWarning($"[ECONOMY TRANSACTION DENIED] Swapping tiles costs {swapCost}g. You can't afford this!");
                    ResetTileVisual(firstSelected);
                    firstSelected = null;
                    return;
                }

                GridManager grid = FindObjectOfType<GridManager>();
                if (grid != null)
                {
                    // Spend the gold. If successful, authorize the physical asset swap!
                    if (EconomyManager.Instance != null && EconomyManager.Instance.SpendGold(swapCost))
                    {
                        grid.SwapTiles(firstSelected, clickedTile);
                        Debug.Log($"[ECONOMY] Charged -{swapCost}g for executing grid reorganization.");
                    }
                }

                // Reset the visual tint on the first tile back to its original hazard color after swapping
                ResetTileVisual(firstSelected);
                firstSelected = null;
            }
        }
    }

    private void ApplySelectionTint(TileRotation tile, Color tint)
    {
        if (tile == null) return;
        SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
        if (sr == null) sr = tile.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = tint;
    }

    private void ResetTileVisual(TileRotation tile)
    {
        if (tile == null) return;
        TileProperty propertyScript = tile.GetComponent<TileProperty>();
        if (propertyScript == null) propertyScript = tile.GetComponentInChildren<TileProperty>();

        if (propertyScript != null)
        {
            propertyScript.RefreshVisuals(isHighlighted: false);
        }
        else
        {
            SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
            if (sr == null) sr = tile.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = Color.white;
        }
    }
}