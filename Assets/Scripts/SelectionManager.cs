using UnityEngine;
using UnityEngine.EventSystems; // Add this if it's missing!

public class SelectionManager : MonoBehaviour
{
    private TileRotation firstSelected;

    [Header("Visual Feedback Settings")]
    public Color selectionHighlightColor = Color.gray;

    void Update()
    {
        // Left Click
        if (Input.GetMouseButtonDown(0))
        {
            // 1. If the mouse is hovering over a modern UI Canvas element, ignore world selection
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // 2. SAFEGUARD: Ask GameManager if the mouse is currently hovering inside the active IMGUI box
            if (GameManager.Instance != null && GameManager.Instance.IsMouseOverUserInterface())
            {
                return; // Let OnGUI process the buttons safely!
            }

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

                // Clear the global inspector view when clicking empty space
                if (GameManager.Instance != null)
                    GameManager.Instance.selectedTileProperty = null;

                Debug.Log("[SELECTION] Clicked empty space. Cleared active selection.");
            }
            return;
        }

        TileRotation clickedTile = hitCollider.GetComponent<TileRotation>();
        if (clickedTile == null) clickedTile = hitCollider.GetComponentInParent<TileRotation>();

        if (clickedTile != null)
        {
            // SAFEGUARD: If this is the starting portal tile, reject the click completely!
            // This prevents the start tile from being selected, upgraded, morphed, or swapped.
            GridManager grid = FindObjectOfType<GridManager>();
            if (grid != null && clickedTile.gridX == grid.startCoords.x && clickedTile.gridY == grid.startCoords.y)
            {
                Debug.Log("[UX PROTECT] Start portal tile cannot be selected or modified.");
                return;
            }
        }

            if (clickedTile != null)
        {
            // -------------------------------------------------------------
            // CASE A: FIRST TIME SELECTING A TILE (Nothing was selected yet)
            // -------------------------------------------------------------
            if (firstSelected == null)
            {
                firstSelected = clickedTile;
                ApplySelectionTint(firstSelected, selectionHighlightColor);

                if (GameManager.Instance != null)
                    GameManager.Instance.selectedTileProperty = firstSelected.GetComponent<TileProperty>();

                Debug.Log($"[SELECTION] Primary tile selected at {firstSelected.gridX}, {firstSelected.gridY}. Ready for inspection/upgrade.");
            }
            // -------------------------------------------------------------
            // CASE B: CLICKING THE EXACT SAME TILE AGAIN (Deselect it)
            // -------------------------------------------------------------
            else if (firstSelected == clickedTile)
            {
                ResetTileVisual(firstSelected);
                firstSelected = null;

                if (GameManager.Instance != null)
                    GameManager.Instance.selectedTileProperty = null;

                Debug.Log("[SELECTION] Clicked the active selection again. Deselecting.");
            }
            // -------------------------------------------------------------
            // CASE C: CLICKING A DIFFERENT TILE WHILE ONE IS ALREADY HIGHLIGHTED
            // -------------------------------------------------------------
            else
            {
                // SAFETY INTERCEPT: If switching is disabled, block the swap and switch inspection focus instead!
                if (GameManager.Instance != null && !GameManager.Instance.isTileSwitchingEnabled)
                {
                    Debug.Log("[UX PROTECT] Switching disabled. Diverting swap attempt to a fresh tile selection instead.");

                    // 1. Un-highlight the old tile visual back to normal
                    ResetTileVisual(firstSelected);

                    // 2. Assign the newly clicked tile as the active selection
                    firstSelected = clickedTile;
                    ApplySelectionTint(firstSelected, selectionHighlightColor);

                    // 3. Point the GameManager inspector panel to the new tile properties
                    GameManager.Instance.selectedTileProperty = firstSelected.GetComponent<TileProperty>();
                    return; // Exit here safely! No money spent, no swap executed.
                }

                // =========================================================================
                // NORMAL SWAP REORGANIZATION MODE (Only runs when isTileSwitchingEnabled is true)
                // =========================================================================
                Debug.Log($"[SELECTION] Second tile selected at {clickedTile.gridX}, {clickedTile.gridY}. Checking transaction parameters.");

                int swapCost = 10;

                // Verify gold balance
                if (EconomyManager.Instance != null && !EconomyManager.Instance.CanAfford(swapCost))
                {
                    Debug.LogWarning($"[ECONOMY TRANSACTION DENIED] Swapping tiles costs {swapCost}g. You can't afford this!");
                    ResetTileVisual(firstSelected);
                    firstSelected = null;

                    if (GameManager.Instance != null)
                        GameManager.Instance.selectedTileProperty = null;

                    return;
                }

                GridManager grid = FindObjectOfType<GridManager>();
                if (grid != null)
                {
                    if (EconomyManager.Instance != null && EconomyManager.Instance.SpendGold(swapCost))
                    {
                        grid.SwapTiles(firstSelected, clickedTile);
                        Debug.Log($"[ECONOMY] Charged -{swapCost}g for executing grid reorganization.");
                    }
                }

                // Clear layout highlights after a successful swap execution pass
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