using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    private TileRotation firstSelected;
    public Color highlightColor = Color.white; // To show what is selected

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left Click
        {
            HandleSelection();
        }
    }

    void HandleSelection()
    {
        // Convert mouse position to 2D coordinates
        Vector2 rayPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // DEBUG 1: Did the click even register?
        Debug.Log($"[Step 1] Mouse clicked at {rayPos}");

        // Perform the raycast
        RaycastHit2D hit = Physics2D.Raycast(rayPos, Vector2.zero);

        if (hit.collider != null)
        {
            // DEBUG 2: What did we physically hit?
            Debug.Log($"[Step 2] Raycast HIT object: {hit.collider.name} on Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");

            TileRotation clickedTile = hit.collider.GetComponent<TileRotation>();

            if (clickedTile != null)
            {
                // DEBUG 3: Did we find the script?
                Debug.Log($"[Step 3] Successfully found TileRotation script on {clickedTile.gridX}, {clickedTile.gridY}");

                if (firstSelected == null)
                {
                    firstSelected = clickedTile;
                    // Visual confirmation
                    //firstSelected.GetComponent<SpriteRenderer>().color = Color.gray;
                }
                else
                {
                    // Execute swap
                    FindObjectOfType<GridManager>().SwapTiles(firstSelected, clickedTile);
                    firstSelected = null;
                }
            }
            else
            {
                Debug.LogWarning("[Step 3 FAILED] Hit an object, but it has no TileRotation script!");
            }
        }
        else
        {
            // If you see this, your BoxCollider2D is missing or the Z-axis is wrong
            Debug.Log("[Step 2 FAILED] Raycast hit nothing.");
        }
    }
}