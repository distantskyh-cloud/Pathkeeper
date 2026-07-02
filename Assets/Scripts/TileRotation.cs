using UnityEngine;
using System.Collections;

public class TileRotation : MonoBehaviour
{
    // A list of possible directions
    public enum Direction { Up, Right, Down, Left }
    public Direction currentDirection;

    // Add these so the tile knows its 'address'
    [HideInInspector] public int gridX;
    [HideInInspector] public int gridY;

    [Header("Visuals")]
    public Transform directionalArrow; // Assign the black Triangle here
    public GameObject hoverSymbol;     // Assign the new rotate icon here

    void Start()
    {
        if (hoverSymbol != null) hoverSymbol.SetActive(false);
    }

    private void OnMouseEnter()
    {
        if (hoverSymbol != null) hoverSymbol.SetActive(true);
    }

    private void OnMouseExit()
    {
        if (hoverSymbol != null) hoverSymbol.SetActive(false);
    }

    public void SetDirection(Direction newDir)
    {
        currentDirection = newDir;
        if (directionalArrow != null)
        {
            directionalArrow.eulerAngles = new Vector3(0, 0, (int)newDir * -90f);
        }
        else
        {
            transform.eulerAngles = new Vector3(0, 0, (int)newDir * -90f);
        }
    }

    void Update()
    {
        // Animate the hover symbol if it's currently active (mouse is over the tile)
        if (hoverSymbol != null && hoverSymbol.activeSelf)
        {
            hoverSymbol.transform.Rotate(0, 0, -150f * Time.deltaTime);
        }
    }

    private void OnMouseDown()
    {
        StartCoroutine(ClickBump());

        UpdateDirection();
        SetDirection(currentDirection);

        // Find the GridManager in the scene and tell it to re-scan the path
        FindObjectOfType<GridManager>().TracePath();
    }

    IEnumerator ClickBump()
    {
        Vector3 originalScale = transform.localScale;
        // Quickly shrink to 90% size
        transform.localScale = originalScale * 0.9f;
        
        // Wait for a split second (0.05 seconds)
        yield return new WaitForSeconds(0.05f);
        
        // Snap back to normal size
        transform.localScale = originalScale;
    }

    void UpdateDirection()
    {
        // This cycles through our enum list
        if (currentDirection == Direction.Left)
            currentDirection = Direction.Up; // Loop back to start
        else
            currentDirection++; // Move to next direction (Right, then Down, etc.)
    }
}