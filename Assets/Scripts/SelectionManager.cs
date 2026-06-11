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

            // CASE 1: First Tile Selection
            if (firstSelected == null)
            {
                firstSelected = clickedTile;
                ApplySelectionTint(firstSelected, selectionHighlightColor);
                Debug.Log($"[SELECTION SUCCESS] First tile locked at coordinate: ({firstSelected.gridX}, {firstSelected.gridY})");
            }
            // CASE 2: Deselect Same Tile
            else if (firstSelected == clickedTile)
            {
                ResetTileVisual(firstSelected);
                firstSelected = null;
                Debug.Log("[SELECTION] Cleared active selection.");
            }
            // CASE 3: Execute Grid Swap
            else
            {
                Debug.Log($"[SELECTION] Second tile locked at ({clickedTile.gridX}, {clickedTile.gridY}). Processing grid swap...");

                if (gridManager != null)
                {
                    gridManager.SwapTiles(firstSelected, clickedTile);
                }

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