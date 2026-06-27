using UnityEngine;
using System.IO;

public class MakeCookie
{
    public static void Run()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for(int y=0; y<size; y++) {
            for(int x=0; x<size; x++) {
                // Black border, white center
                if (x < 2 || x > size - 3 || y < 2 || y > size - 3) 
                    tex.SetPixel(x, y, Color.black);
                else 
                    tex.SetPixel(x, y, Color.white);
            }
        }
        tex.Apply();
        File.WriteAllBytes("Assets/BoxCookie.png", tex.EncodeToPNG());
        Debug.Log("Cookie saved!");
    }
}
