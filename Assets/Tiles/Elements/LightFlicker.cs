using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    [Tooltip("The lowest intensity the light will drop to.")]
    public float minIntensity = 0.8f;
    
    [Tooltip("The highest intensity the light will spike to.")]
    public float maxIntensity = 1.2f;
    
    [Tooltip("How fast the light flickers.")]
    public float flickerSpeed = 3f;

    private Light myLight;
    private float randomOffset;

    void Start()
    {
        myLight = GetComponent<Light>();
        
        // Give each light a random offset so they don't all pulse in perfect sync!
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (myLight != null)
        {
            // Perlin noise generates smooth, organic randomness (perfect for fire and magma)
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed + randomOffset, 0f);
            
            // Lerp blends smoothly between the minimum and maximum based on the noise
            myLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
        }
    }
}
