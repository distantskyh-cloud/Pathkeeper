using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class PathVisualizer : MonoBehaviour
{
    private LineRenderer line;
    
    [Header("Pulse Settings")]
    public float scrollSpeed = 1.5f;
    public float lineThickness = 0.15f;
    public float pulsesPerTile = 0.5f; // E.g. 0.5 means 1 pulse every 2 tiles
    
    // We will generate a gradient texture in memory to act as our energy pulse
    private Texture2D pulseTexture;
    private Material pulseMaterial;
    
    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.startWidth = lineThickness;
        line.endWidth = lineThickness;
        
        // We use CornerVertices to make the sharp 90-degree turns look smooth and rounded!
        line.numCornerVertices = 5;
        line.numCapVertices = 5;
        
        // Make sure it uses world space coordinates
        line.useWorldSpace = true;
        
        GeneratePulseMaterial();
    }

    void GeneratePulseMaterial()
    {
        // 1. Create a 256x1 texture
        pulseTexture = new Texture2D(256, 1, TextureFormat.RGBA32, false);
        pulseTexture.wrapMode = TextureWrapMode.Repeat;
        
        // 2. Draw the pulse (Dim grey for the base line, Bright White for the hot spot)
        for (int i = 0; i < 256; i++)
        {
            float t = i / 255f;
            
            // A simple bell curve / sine wave for the glowing hot spot
            float intensity = Mathf.Sin(t * Mathf.PI);
            
            // Raise intensity to a high power to make the glowing spot sharper and thinner
            intensity = Mathf.Pow(intensity, 12f);
            
            // Because we use an Additive shader later, 0.2f draws a dim white line. 
            // 1.0f draws an extremely bright glowing white spot!
            float baseLine = 0.15f;
            float finalColor = Mathf.Max(baseLine, intensity);
            
            Color color = new Color(finalColor, finalColor, finalColor, 1f);
            pulseTexture.SetPixel(i, 0, color);
        }
        pulseTexture.Apply();

        // 3. Create an Additive material so it glows beautifully
        // In built-in pipeline, Legacy Shaders/Particles/Additive is the most reliable glowing shader
        Shader additiveShader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (additiveShader == null) additiveShader = Shader.Find("Particles/Standard Unlit"); // Fallback
        
        pulseMaterial = new Material(additiveShader);
        pulseMaterial.mainTexture = pulseTexture;
        
        // Assign to line renderer
        line.material = pulseMaterial;
        
        // Ensure the line itself doesn't darken the texture
        line.startColor = Color.white;
        line.endColor = Color.white;
    }

    public void UpdatePath(List<Vector3> points)
    {
        if (points == null || points.Count == 0)
        {
            line.positionCount = 0;
            return;
        }

        line.positionCount = points.Count;
        line.SetPositions(points.ToArray());
        
        // To make the texture scroll evenly across the whole path regardless of how long it is,
        // we tile the texture based on the number of points (length of the path).
        // This ensures the pulse doesn't stretch weirdly on very long paths!
        pulseMaterial.mainTextureScale = new Vector2(points.Count * pulsesPerTile, 1f);
    }

    void Update()
    {
        if (pulseMaterial != null && line.positionCount > 0)
        {
            // Scroll the texture offset backwards so the pulse moves FORWARD from start to end
            float offset = Time.time * -scrollSpeed;
            pulseMaterial.mainTextureOffset = new Vector2(offset, 0);
        }
    }
}
