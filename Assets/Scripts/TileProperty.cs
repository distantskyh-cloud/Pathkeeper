using UnityEngine;

public class TileProperty : MonoBehaviour
{
    [System.Serializable]
    public struct HazardData
    {
        public float damage;            // Instant damage (Spikes/Pitfall)
        public float speedMult;         // 1.0 = normal, 0.5 = slow, 0 = freeze
        public float dotDamage;         // Damage per second (Burn/Poison)
        public float duration;          // How long the effect lasts
    }

    public HazardData currentData;

    [Header("Visual Variations")]
    public Material[] randomMaterials;

    [System.Serializable]
    public struct HazardVisual
    {
        public TileType type;
        public Sprite overlayImage;
        public Material overlayMaterial;
        public GameObject vfxPrefab;
    }

    [Header("Hazard Overlays")]
    public Renderer hazardOverlayRenderer;
    public HazardVisual[] hazardVisuals;
    
    private GameObject currentVFX;

    void Awake()
    {
        // Randomly assign a material base if we have any setup
        if (randomMaterials != null && randomMaterials.Length > 0)
        {
            Renderer r = GetComponent<Renderer>();
            if (r != null)
            {
                int randomIndex = Random.Range(0, randomMaterials.Length);
                r.sharedMaterial = randomMaterials[randomIndex];
            }
        }

        // Initialize the default visual state (this hides the Quad overlay by default)
        ApplyHazardVisuals();
    }

    // Tile types
    public enum TileType { Normal, Spike, Slow, Burn, Freeze, Pitfall, Poison, Static, Bleed, Curse }
    public TileType type = TileType.Normal;

    // Setting the tile type
    public void SetType(TileType newType)
    {
        type = newType;
        ApplyHazardVisuals();
    }

    public void SetTileColor(Color customColor)
    {
        Renderer r = GetComponent<Renderer>();
        if (r is SpriteRenderer sr)
        {
            sr.color = customColor;
        }
        else if (r != null)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetColor("_Color", customColor);      // Built-in Pipeline
            block.SetColor("_BaseColor", customColor);  // URP Pipeline safety
            r.SetPropertyBlock(block);
        }
    }

    void ApplyHazardVisuals()
    {
        // 1. Reset base rock color to pure white
        SetTileColor(Color.white);

        // 2. Hide existing overlay by default
        if (hazardOverlayRenderer != null)
        {
            hazardOverlayRenderer.gameObject.SetActive(false);
        }

        // 3. Clear existing VFX
        if (currentVFX != null)
        {
            Destroy(currentVFX);
            currentVFX = null;
        }

        // 4. Find and apply the matching overlay and VFX
        if (hazardVisuals != null)
        {
            foreach (HazardVisual visual in hazardVisuals)
            {
                if (visual.type == type)
                {
                    if (hazardOverlayRenderer != null)
                    {
                        if (hazardOverlayRenderer is SpriteRenderer sr && visual.overlayImage != null)
                        {
                            sr.sprite = visual.overlayImage;
                            hazardOverlayRenderer.gameObject.SetActive(true);
                        }
                        else if (hazardOverlayRenderer is MeshRenderer mr && visual.overlayMaterial != null)
                        {
                            mr.material = visual.overlayMaterial;
                            hazardOverlayRenderer.gameObject.SetActive(true);
                        }
                    }

                    if (visual.vfxPrefab != null)
                    {
                        // Spawn the VFX as a child of this tile
                        currentVFX = Instantiate(visual.vfxPrefab, transform);
                        
                        // Discover the true visual center using the manually placed Triangle!
                        Transform triangle = transform.Find("Triangle");
                        if (triangle != null)
                        {
                            // Use the Triangle's perfectly centered local position!
                            currentVFX.transform.localPosition = new Vector3(triangle.localPosition.x, triangle.localPosition.y, visual.vfxPrefab.transform.position.z);
                        }
                        else
                        {
                            // Fallback
                            currentVFX.transform.localPosition = new Vector3(0, 0, visual.vfxPrefab.transform.position.z);
                        }
                    }

                    break;
                }
            }
        }

        // --- SAFETY CORRECTION USING TILEROTATION ---
        TileRotation myRotationScript = GetComponent<TileRotation>();
        GridManager grid = FindObjectOfType<GridManager>();

        if (myRotationScript != null && grid != null)
        {
            // Compare TileRotation's grid coordinates to the GridManager's end coordinates
            if (grid.endCoords.x == myRotationScript.gridX && grid.endCoords.y == myRotationScript.gridY)
            {
                Transform goal = transform.Find("GoalIndicator");
                if (goal != null)
                {
                    goal.gameObject.SetActive(true);
                }
            }
        }
    }

    // Call this from GridManager's HighlightPath
    public void RefreshVisuals(bool isHighlighted)
    {
        if (isHighlighted)
        {
            // Tint the base rock yellow to show the path
            SetTileColor(Color.yellow);
        }
        else
        {
            // Reset to normal base rock color (without destroying and recreating VFX!)
            SetTileColor(Color.white); 
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-populate the hazardVisuals array to save you from manually clicking '+' 10 times!
        int enumCount = System.Enum.GetValues(typeof(TileType)).Length;
        if (hazardVisuals == null || hazardVisuals.Length != enumCount)
        {
            TileType[] allTypes = (TileType[])System.Enum.GetValues(typeof(TileType));
            HazardVisual[] newVisuals = new HazardVisual[enumCount];
            
            for (int i = 0; i < enumCount; i++)
            {
                newVisuals[i].type = allTypes[i];
                
                // Preserve any images, materials, or VFX you already assigned
                if (hazardVisuals != null)
                {
                    foreach (var oldVisual in hazardVisuals)
                    {
                        if (oldVisual.type == allTypes[i])
                        {
                            newVisuals[i].overlayImage = oldVisual.overlayImage;
                            newVisuals[i].overlayMaterial = oldVisual.overlayMaterial;
                            newVisuals[i].vfxPrefab = oldVisual.vfxPrefab;
                            break;
                        }
                    }
                }
            }
            hazardVisuals = newVisuals;
        }
    }
#endif
}