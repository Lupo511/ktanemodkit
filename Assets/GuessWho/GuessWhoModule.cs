using UnityEngine;
using System.Collections;

public class GuessWhoModule : MonoBehaviour
{
    public Texture2D ComponentBaseTexture;
    public RenderTexture ModuleRenderTexture;

    public void Start()
    {
        
    }

    public void Update()
    {

    }

    public void OnGUI()
    {
        if (Event.current.type == EventType.Repaint)
        {
            RenderTexture oldRenderTexture = RenderTexture.active;
            RenderTexture.active = ModuleRenderTexture;
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, 1024, 1024, 0);
            GL.Clear(true, true, Color.white);
            Graphics.DrawTexture(new Rect(0, 0, 1024, 1024), ComponentBaseTexture);
            GL.PopMatrix();
            RenderTexture.active = oldRenderTexture;
        }
    }
}
