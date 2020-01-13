using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class importImage : MonoBehaviour
{

	// Source image
    public Texture2D inTex;

    // Has to be converted to a render texture
    public RenderTexture inTexRender;

    public ComputeShader cs;
    public int rez = 512;


    void Start()
    {
        int k = cs.FindKernel("ResetKernel");

 		inTexRender = CreateTexture(RenderTextureFormat.ARGBFloat);	

        // Convert source image texture to a render texture
 		Graphics.Blit(inTex, inTexRender);

        // Then send it to the compute shader
 		cs.SetTexture(k, "inTex", inTexRender);  
    }

    // Creates a render texture
    protected RenderTexture CreateTexture(RenderTextureFormat format)
    {
    	RenderTexture texture = new RenderTexture(rez, rez, 1, format);	// New texture at the resolution. 
    														// Format passed above, rfloat or argbfloat
    	texture.enableRandomWrite = true;		// Enable random access write into this render texture on Shader Model 5.0 level shaders.	
    	texture.filterMode = FilterMode.Point;  // Chooses Point (not Bilinear or Trilinear) filter mode for this RenderTexture
    	texture.wrapMode = TextureWrapMode.Repeat; // Texture wraps from one side to the other
    	texture.useMipMap = false;	
    	texture.Create();

    	return texture;
    }




}

