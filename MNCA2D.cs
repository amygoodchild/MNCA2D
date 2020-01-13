using UnityEngine;
using EasyButtons;

public class MNCA2D : MonoBehaviour
{
    
    [Header("CCA Primary Params")]

    public int nstates = 2;

    [Header("Setup")]

    [Range(8, 2048)]
    public int rez = 512;

    [Range(0,50)]
    public int stepsPerFrame = 1;

    [Range(1,50)]
    public int stepMod = 1;

    public ComputeShader cs;
    public Material outMat;

    private RenderTexture outTex;
    private RenderTexture readTex;
    private RenderTexture writeTex;

    private int stepKernel;

    [Header("Thresholds")]   

    // Each neighbourhood has it's own min and maxs. I implemented the three neighbourhoods from
    // the softology blog so neighbourhood 0 and 3 have two sets of min and max and neighbourhood
    // 1 and 2 just have one. 

    // Sorry, in the var names I started neighbourhood numbering from 0 and range numbering from 1
    // without thinking and it's a faff to change it. 
    // Also probably I should have made this an array.

    // The 'direction' variable allows me to decide whether each the neighbourhood result for each
    // range results in incrementing the state or decrement it. 

    // Neighbourhood 0 - range 1 
    [Range(0, 225)] public int count0min1 = 0;
    [Range(0, 225)] public int count0max1 = 15;
    public bool count0direction1 = false;

    // Neighbourhood 0 - range 2
    [Range(0, 225)] public int count0min2 = 40;
    [Range(0, 225)] public int count0max2 = 42;
    public bool count0direction2 = true;

    // Neighbourhood 1 - range 1
    [Range(0, 24)] public int count1min1 = 10;
    [Range(0, 24)] public int count1max1 = 13;
	public bool count1direction1 = true;

	// Neighbourhood 2 - range 1
    [Range(0, 84)] public int count2min1 = 9;
    [Range(0, 84)] public int count2max1 = 21;
	public bool count2direction1 = false;

	// Neighbourhood 3 - range 1
    [Range(0, 360)] public int count3min1 = 78;
    [Range(0, 360)] public int count3max1 = 89;
    public bool count3direction1 = false;

	// Neighbourhood 3 - range 2
    [Range(0, 360)] public int count3min2 = 250;
    [Range(0, 360)] public int count3max2 = 360;
	public bool count3direction2 = false;

    // These arrays of vector4's hold the coordinates for the cells to check for each variable. 
    // I'm only using the first 2 slots of each vector4 but the function for passing the data to the
    // compute shader only works with vector4. 
    [HideInInspector]public Vector4[] neighborhood0;
    [HideInInspector]public Vector4[] neighborhood1;
    [HideInInspector]public Vector4[] neighborhood2;
    [HideInInspector]public Vector4[] neighborhood3;

    [Header("Color Variables")]   
    [Range(0, 1)]
    public float colorALow;

    [Range(0, 1)]
    public float colorAHigh; 

    [Range(0, 1)]       
    public float colorBLow;

    [Range(0, 1)]
    public float colorBHigh;

    [Range(0, 1)]
    public float minSaturation;

    [Range(0, 1)]
    public float maxSaturation;

    [Range(0, 1)]
    public float minBrightness;

    [Range(0, 1)]
    public float maxBrightness;



    void Update()
    {

    	// Determines at what speed to run step
    	if (Time.frameCount % stepMod == 0)
    	{
    		for(int i = 0; i < stepsPerFrame; i++)
    		{
    			Step();
    		}
    	}
    }

    /*
    *
    * RESET
    *
    *
    */

    void Start()
    {
    	Reset();		
    }

  
    private void Reset()
    {
    	// Creates 3 textures
    	readTex = CreateTexture(RenderTextureFormat.RFloat);   	// Texture format
    	writeTex = CreateTexture(RenderTextureFormat.RFloat);	// Texture format
    	outTex = CreateTexture(RenderTextureFormat.ARGBFloat);	// Color render texture format

    	// Assigns the id of the shader's StepKernel kernel to stepKernel
    	// A kernel is a function that does computation. A compute shader can have many kernels
    	stepKernel = cs.FindKernel("StepKernel");	

    	// Uses the GPU to reset the kernel's settings
    	GPUResetKernel();
    }


    // Creates the textures
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

    [Button]
    private void resetVariables(){

    	// Passing all the variables to the compute shader

    	cs.SetInt("nstates", nstates);
    	
    	cs.SetBool("count0direction1", count0direction1);
		cs.SetBool("count1direction1", count1direction1);
		cs.SetBool("count2direction1", count2direction1);
		cs.SetBool("count3direction1", count3direction1);
		cs.SetBool("count0direction2", count0direction2);
		cs.SetBool("count3direction2", count3direction2);

    	cs.SetInt("count0min1", count0min1);
		cs.SetInt("count0max1", count0max1);
		cs.SetInt("count1min1", count1min1);
		cs.SetInt("count1max1", count1max1);
		cs.SetInt("count2min1", count2min1);
		cs.SetInt("count2max1", count2max1);
		cs.SetInt("count3min1", count3min1);
		cs.SetInt("count3max1", count3max1);
		cs.SetInt("count0min2", count0min2);
		cs.SetInt("count0max2", count0max2);
		cs.SetInt("count3min2", count3min2);
		cs.SetInt("count3max2", count3max2);

    }

    // Uses the GPU to reset the kernel's settings
    private void GPUResetKernel()
    {
    	// Assigns the id of the shader's ResetKernel kernel to k
    	int k = cs.FindKernel("ResetKernel");

    	// (for which kernel the texture is being set, name of the ID, texture to set)
    	// Sets the writeTexture to the resetKernel
    	cs.SetTexture(k, "writeTex", writeTex);

    	// Sets all the variables to the shader
    	cs.SetInt("nstates", nstates);

    	cs.SetInt("rez", rez);
    	//cs.SetInt("threshold", threshold);

    	resetVariables();

        cs.SetFloat("colorALow", colorALow);
        cs.SetFloat("colorAHigh", colorAHigh);
        cs.SetFloat("colorBLow", colorBLow);
        cs.SetFloat("colorBHigh", colorBHigh);

        cs.SetFloat("minBrightness", minBrightness);
        cs.SetFloat("minSaturation", minSaturation);
        cs.SetFloat("maxBrightness", maxBrightness);
        cs.SetFloat("maxSaturation", maxSaturation);

        // I typed in all of the coordinates for the neighborhoods in the softology blog because I couldn't
        // work out how to do the neighborhood editor thing, and I have a lot of patience.  

    	neighborhood0 = new Vector4[] {
    		new Vector4(-1,-14), new Vector4(0,-14), new Vector4(1, -14),
    		new Vector4(-4,-13), new Vector4(-3,-13), new Vector4(-2, -13), new Vector4(2,-13), new Vector4(3,-13), new Vector4(4, -13),  
    		new Vector4(-6,-12), new Vector4(-5,-12), new Vector4(5, -12), new Vector4(6, -12),
    		new Vector4(-8,-11), new Vector4(-7,-11), new Vector4(7, -11), new Vector4(8, -11),
    		new Vector4(-9, -10), new Vector4(9, -10), new Vector4(-1,-10), new Vector4(0,-10), new Vector4(1, -10),
    		new Vector4(-10, -9), new Vector4(10, -9), new Vector4(-4,-9), new Vector4(-3,-9), new Vector4(-2, -9), new Vector4(2,-9), new Vector4(3,-9), new Vector4(4, -9),  
    		new Vector4(-11, -8), new Vector4(11, -8), new Vector4(-6,-8), new Vector4(-5,-8), new Vector4(5, -8), new Vector4(6, -8),
    		new Vector4(-11, -7), new Vector4(-6, -7), new Vector4(-2, -7), new Vector4(-1, -7), new Vector4(-0, -7), new Vector4(1, -7), new Vector4(2, -7), new Vector4(11, -7), new Vector4(6, -7),
    		new Vector4(-12, -6), new Vector4(-8, -6), new Vector4(-4, -6), new Vector4(-3, -6), new Vector4(12, -6), new Vector4(8, -6), new Vector4(4, -6), new Vector4(3, -6),
    		new Vector4(-12, -5), new Vector4(-8, -5), new Vector4(-5, -5), new Vector4(-1, -5), new Vector4(0, -5), new Vector4(12, -5), new Vector4(8, -5), new Vector4(5, -5), new Vector4(1, -5), 
    		new Vector4(-13, -4), new Vector4(-9, -4), new Vector4(-6, -4), new Vector4(-3, -4), new Vector4(-2, -4), new Vector4(13, -4), new Vector4(9, -4), new Vector4(6, -4), new Vector4(3, -4), new Vector4(2, -4),
    		new Vector4(-13, -3), new Vector4(-9, -3), new Vector4(-6, -3), new Vector4(-4, -3), new Vector4(-1, -3), new Vector4(0, -3), new Vector4(13, -3), new Vector4(9, -3), new Vector4(6, -3), new Vector4(4, -3), new Vector4(1, -3),
    		new Vector4(-13, -2), new Vector4(-9, -2), new Vector4(-7, -2), new Vector4(-4, -2), new Vector4(-2, -2), new Vector4(13, -2), new Vector4(9, -2), new Vector4(7, -2), new Vector4(4, -2), new Vector4(2, -2),
    		new Vector4(-14, -1), new Vector4(-10, -1), new Vector4(-7, -1), new Vector4(-5, -1), new Vector4(-3, -1), new Vector4(-1, -1), new Vector4(0, -1), new Vector4(14, -1), new Vector4(10, -1), new Vector4(7, -1), new Vector4(5, -1), new Vector4(3, -1), new Vector4(1, -1),
    		new Vector4(-14, 0), new Vector4(-10, 0), new Vector4(-7, 0), new Vector4(-5, 0), new Vector4(-3, 0), new Vector4(-1, 0), new Vector4(0, 0), new Vector4(14, 0), new Vector4(10, 0), new Vector4(7, 0), new Vector4(5, 0), new Vector4(3, 0), new Vector4(1, 0),
    	 
			new Vector4(-1,14), new Vector4(0, 14), new Vector4(1, 14),
    		new Vector4(-4,13), new Vector4(-3, 13), new Vector4(-2, 13), new Vector4(2,13), new Vector4(3,13), new Vector4(4, 13),  
    		new Vector4(-6,12), new Vector4(-5, 12), new Vector4(5, 12), new Vector4(6, 12),
    		new Vector4(-8,11), new Vector4(-7, 11), new Vector4(7, 11), new Vector4(8, 11),
    		new Vector4(-9, 10), new Vector4(9, 10), new Vector4(-1,10), new Vector4(0,10), new Vector4(1, 10),
    		new Vector4(-10, 9), new Vector4(10, 9), new Vector4(-4,9), new Vector4(-3,9), new Vector4(-2, 9), new Vector4(2, 9), new Vector4(3, 9), new Vector4(4, 9),  
    		new Vector4(-11, 8), new Vector4(11, 8), new Vector4(-6,8), new Vector4(-5,8), new Vector4(5, 8), new Vector4(6, 8),
    		new Vector4(-11, 7), new Vector4(-6, 7), new Vector4(-2, 7), new Vector4(-1, 7), new Vector4(-0, 7), new Vector4(1, 7), new Vector4(2, 7), new Vector4(11, 7), new Vector4(6, 7),
    		new Vector4(-12, 6), new Vector4(-8, 6), new Vector4(-4, 6), new Vector4(-3, 6), new Vector4(12, 6), new Vector4(8, 6), new Vector4(4, 6), new Vector4(3, 6),
    		new Vector4(-12, 5), new Vector4(-8, 5), new Vector4(-5, 5), new Vector4(-1, 5), new Vector4(0, 5), new Vector4(12, 5), new Vector4(8, 5), new Vector4(5, 5), new Vector4(1, 5), 
    		new Vector4(-13, 4), new Vector4(-9, 4), new Vector4(-6, 4), new Vector4(-3, 4), new Vector4(-2, 4), new Vector4(13, 4), new Vector4(9, 4), new Vector4(6, 4), new Vector4(3, 4), new Vector4(2, 4),
    		new Vector4(-13, 3), new Vector4(-9, 3), new Vector4(-6, 3), new Vector4(-4, 3), new Vector4(-1, 3), new Vector4(0, 3), new Vector4(13, 3), new Vector4(9, 3), new Vector4(6, 3), new Vector4(4, 3), new Vector4(1, 3),
    		new Vector4(-13, 2), new Vector4(-9, 2), new Vector4(-7, 2), new Vector4(-4, 2), new Vector4(-2, 2), new Vector4(13, 2), new Vector4(9, 2), new Vector4(7, 2), new Vector4(4, 2), new Vector4(2, 2),
    		new Vector4(-14, 1), new Vector4(-10, 1), new Vector4(-7, 1), new Vector4(-5, 1), new Vector4(-3, 1), new Vector4(-1, 1), new Vector4(0, 1), new Vector4(14, 1), new Vector4(10, 1), new Vector4(7, 1), new Vector4(5, 1), new Vector4(3, -1), new Vector4(1, 1)
    	}; 

    	neighborhood1 = new Vector4[] {
    		new Vector4(-1,-3), new Vector4(0,-3), new Vector4(1,-3), 
    		new Vector4(-2,-2), new Vector4(2,-2), 
    		new Vector4(-3,-1), new Vector4(-1,-1), new Vector4(0,-1), new Vector4(1,-1), new Vector4(3,-1),
        	new Vector4(-3,-0), new Vector4(-1,0), new Vector4(1,-0), new Vector4(3,0),
        	new Vector4(-1,3), new Vector4(0,3), new Vector4(1,3), 
    		new Vector4(-2,2), new Vector4(2,2), 
    		new Vector4(-3,1), new Vector4(-1,1), new Vector4(0,1), new Vector4(1,1), new Vector4(3,1)   	
        };


        neighborhood2 = new Vector4[] {
    		new Vector4(-1,-6), new Vector4(0,-6), new Vector4(1,-6), 
    		new Vector4(-3,-5), new Vector4(-2,-5), new Vector4(-1,-5), new Vector4(0,-5), new Vector4(1,-5), new Vector4(2,-5), new Vector4(3,-5),
        	new Vector4(-4,-4), new Vector4(-3,-4), new Vector4(-2,-4), new Vector4(-1,-4), new Vector4(0,-4), new Vector4(1,-4), new Vector4(2,-4), new Vector4(3,-4), new Vector4(4,-4), 
            new Vector4(-5,-3), new Vector4(-4,-3), new Vector4(-3,-3), new Vector4(-2,-3),  new Vector4(2,-3), new Vector4(3,-3), new Vector4(4,-3), new Vector4(5,-3), 
            new Vector4(-5,-2), new Vector4(-4,-2), new Vector4(-3,-2), new Vector4(3,-2), new Vector4(4,-2), new Vector4(5,-2), 
			new Vector4(-6,-1), new Vector4(-5,-1), new Vector4(-4,-1), new Vector4(4,-1),  new Vector4(5,-1), new Vector4(6,-1), 
           	new Vector4(-6, 0), new Vector4(-5, 0), new Vector4(-4, 0), new Vector4(4,0),  new Vector4(5,0), new Vector4(6,0),      
       		new Vector4(-1, 6), new Vector4(0,6), new Vector4(1,6), 
    		new Vector4(-3, 5), new Vector4(-2,5), new Vector4(-1,5), new Vector4(0,5), new Vector4(1,5), new Vector4(2,5), new Vector4(3,5),
        	new Vector4(-4, 4), new Vector4(-3,4), new Vector4(-2,4), new Vector4(-1,4), new Vector4(0,4), new Vector4(1,4), new Vector4(2,4), new Vector4(3,4), new Vector4(4,4), 
            new Vector4(-5, 3), new Vector4(-4,3), new Vector4(-3,3), new Vector4(-2,3),  new Vector4(2,3), new Vector4(3,3), new Vector4(4,3), new Vector4(5,3), 
            new Vector4(-5, 2), new Vector4(-4,2), new Vector4(-3,2), new Vector4(3,2), new Vector4(4,2), new Vector4(5,2), 
			new Vector4(-6, 1), new Vector4(-5,1), new Vector4(-4,1), new Vector4(4,1),  new Vector4(5,1), new Vector4(6,1)
        };

        neighborhood3 = new Vector4[] {
    		new Vector4(-3,-14), new Vector4(-2,-14), new Vector4(-1,-14), new Vector4(0,-14), new Vector4(3,-14), new Vector4(2,-14), new Vector4(1,-14), 
	    	new Vector4(-6,-13), new Vector4(-5,-13), new Vector4(-4,-13), new Vector4(-3,-13), new Vector4(-2,-13), new Vector4(-1,-13), new Vector4(0,-13), new Vector4(3,-13), new Vector4(2,-13), new Vector4(1,-13), new Vector4(4,-13), new Vector4(5,-13), new Vector4(6,-13), 
    		new Vector4(-8,-12), new Vector4(-7,-12), new Vector4(-6,-12), new Vector4(-5,-12), new Vector4(-4,-12), new Vector4(-3,-12), new Vector4(-2,-12), new Vector4(-1,-12), new Vector4(0,-12), new Vector4(3,-12), new Vector4(2,-12), new Vector4(1,-12), new Vector4(4,-12), new Vector4(5,-12), new Vector4(6,-12),new Vector4(7,-12), new Vector4(8,-12), 
			new Vector4(-9,-11), new Vector4(-8,-11), new Vector4(-7,-11), new Vector4(-6,-11), new Vector4(-5,-11), new Vector4(-4,-11), new Vector4(-3,-11), new Vector4(-2,-11), new Vector4(-1,-11), new Vector4(0,-11), new Vector4(3,-11), new Vector4(2,-11), new Vector4(1,-11), new Vector4(4,-11), new Vector4(5,-11), new Vector4(6,-11),new Vector4(7,-11), new Vector4(8,-11), new Vector4(9,-11), 
    	    new Vector4(-10,-10), new Vector4(-9,-10), new Vector4(-8,-10), new Vector4(-7,-10), new Vector4(-6,-10), new Vector4(-5,-10),  new Vector4(5,-10), new Vector4(6,-10),new Vector4(7,-10), new Vector4(8,-10), new Vector4(9,-10),new Vector4(10,-10), 
    	    new Vector4(-11,-9), new Vector4(-10,-9), new Vector4(-9,-9), new Vector4(-8,-9), new Vector4(-7,-9), new Vector4(7,-9),  new Vector4(8,-9), new Vector4(9,-9), new Vector4(10,-9), new Vector4(11,-9),
			new Vector4(-12,-8), new Vector4(-11,-8), new Vector4(-10,-8), new Vector4(-9,-8), new Vector4(-8,-8), new Vector4(8,-8),  new Vector4(9,-8), new Vector4(10,-8), new Vector4(11,-8), new Vector4(12,-8), 
    	    new Vector4(-12,-7), new Vector4(-11,-7), new Vector4(-10,-7), new Vector4(-9,-7), new Vector4(9,-7), new Vector4(10,-7),new Vector4(11,-7), new Vector4(12,-7), new Vector4(-2,-7), new Vector4(-1,-7), new Vector4(0,-7), new Vector4(1,-7), new Vector4(2,-7),  	    	
    	    new Vector4(-13,-6), new Vector4(-12,-6), new Vector4(-11,-6), new Vector4(-10,-6), new Vector4(-4,-6), new Vector4(-3,-6),	new Vector4(13,-6), new Vector4(12,-6), new Vector4(11,-6), new Vector4(10,-6), new Vector4(4,-6), new Vector4(3,-6),	
    	    new Vector4(-13,-5), new Vector4(-12,-5), new Vector4(-11,-5), new Vector4(-10,-5), new Vector4(-5,-5), new Vector4(5,-5), new Vector4(13,-5), new Vector4(12,-5), new Vector4(11,-5), new Vector4(10,-5), 
	    	new Vector4(-13,-4), new Vector4(-12,-4), new Vector4(-11,-4), new Vector4(-11,-4), new Vector4(-1,-4), new Vector4(0,-4), new Vector4(1,-4), new Vector4(13,-4), new Vector4(12,-4), new Vector4(11,-4), new Vector4(11,-4),
	    	new Vector4(-14,-3), new Vector4(-13,-3), new Vector4(-12,-3), new Vector4(-11,-3), new Vector4(-6,-3), new Vector4(-2,-3), new Vector4(14,-3), new Vector4(13,-3), new Vector4(12,-3), new Vector4(11,-3), new Vector4(6,-3), new Vector4(2,-3),			
			new Vector4(-14,-2), new Vector4(-13,-2), new Vector4(-12,-2), new Vector4(-11,-2), new Vector4(-7,-2), new Vector4(-3,-2), new Vector4(14,-2), new Vector4(13,-2), new Vector4(12,-2), new Vector4(11,-2), new Vector4(7,-3), new Vector4(3,-3),
			new Vector4(-14,-1), new Vector4(-13,-1), new Vector4(-12,-1), new Vector4(-11,-1), new Vector4(-7,-1), new Vector4(-4,-1), new Vector4(-1,-1), new Vector4(0,-1),new Vector4(1,-1), new Vector4(7,-1), new Vector4(4,-1), new Vector4(14,-1), new Vector4(13,-1), new Vector4(12,-1), new Vector4(11,-1),
			new Vector4(-14,0), new Vector4(-13,0), new Vector4(-12,0), new Vector4(-11,0), new Vector4(-7,0), new Vector4(-4,0), new Vector4(-1,0), new Vector4(1,0), new Vector4(7,0), new Vector4(4,0), new Vector4(14,0), new Vector4(13,0), new Vector4(12,0), new Vector4(11,0),
	    	
	    	new Vector4(-3,14), new Vector4(-2,14), new Vector4(-1,14), new Vector4(0,14), new Vector4(3,14), 	new Vector4(2,14), new Vector4(1,14), 
	    	new Vector4(-6,13), new Vector4(-5,13), new Vector4(-4,13), new Vector4(-3,13), new Vector4(-2,13), new Vector4(-1,13), new Vector4(0,13), new Vector4(3,13), new Vector4(2,13), new Vector4(1,13), new Vector4(4,13), new Vector4(5,13), new Vector4(6,13), 
    		new Vector4(-8,12), new Vector4(-7,12), new Vector4(-6,12), new Vector4(-5,12), new Vector4(-4,12), new Vector4(-3,12), new Vector4(-2,12), new Vector4(-1,12), new Vector4(0,12), new Vector4(3,12), new Vector4(2,12), new Vector4(1,12), new Vector4(4,12), new Vector4(5,12), new Vector4(6,12),new Vector4(7,12), new Vector4(8,12), 
			new Vector4(-9,11), new Vector4(-8,11), new Vector4(-7,11), new Vector4(-6,11), new Vector4(-5,11), new Vector4(-4,11), new Vector4(-3,11), new Vector4(-2,11), new Vector4(-1,11), new Vector4(0,11), new Vector4(3,11), new Vector4(2,11), new Vector4(1,11), new Vector4(4,11), new Vector4(5,11), new Vector4(6,11),new Vector4(7,11), new Vector4(8,11), new Vector4(9,11), 
    	    new Vector4(-10,10), new Vector4(-9,10), new Vector4(-8,10), new Vector4(-7,10), new Vector4(-6,10),new Vector4(-5,10),  new Vector4(5,10), new Vector4(6,10),new Vector4(7,10), new Vector4(8,10), new Vector4(9,10),new Vector4(10,10), 
    	    new Vector4(-11,9), new Vector4(-10,9), new Vector4(-9,9), new Vector4(-8,9), new Vector4(-7,9), 	new Vector4(7,9),  new Vector4(8,9), new Vector4(9,9), new Vector4(10,9), new Vector4(11,9),
			new Vector4(-12,8), new Vector4(-11,8), new Vector4(-10,8), new Vector4(-9,8), new Vector4(-8,8), 	new Vector4(8,8),  new Vector4(9,8), new Vector4(10,8), new Vector4(11,8), new Vector4(12,8), 
    	    new Vector4(-12,7), new Vector4(-11,7), new Vector4(-10,7), new Vector4(-9,7), new Vector4(9,7), 	new Vector4(10,7),new Vector4(11,7), new Vector4(12,7), new Vector4(-2,7), new Vector4(-1,7), new Vector4(0,7), new Vector4(1,7), new Vector4(2,7),  	    	
    	    new Vector4(-13,6), new Vector4(-12,6), new Vector4(-11,6), new Vector4(-10,6), new Vector4(-4,6), 	new Vector4(-3,6),	new Vector4(13,6), new Vector4(12,6), new Vector4(11,6), new Vector4(10,6), new Vector4(4,6), new Vector4(3,6),	
    	    new Vector4(-13,5), new Vector4(-12,5), new Vector4(-11,5), new Vector4(-10,5), new Vector4(-5,5), 	new Vector4(5,5), new Vector4(13,5), new Vector4(12,5), new Vector4(11,5), new Vector4(10,5), 
	    	new Vector4(-13,4), new Vector4(-12,4), new Vector4(-11,4), new Vector4(-11,4), new Vector4(-1,4), 	new Vector4(0,4), new Vector4(1,4), new Vector4(13,4), new Vector4(12,4), new Vector4(11,4), new Vector4(11,4),
	    	new Vector4(-14,3), new Vector4(-13,3), new Vector4(-12,3), new Vector4(-11,3), new Vector4(-6,3), 	new Vector4(-2,3), new Vector4(14,3), new Vector4(13,3), new Vector4(12,3), new Vector4(11,3), new Vector4(6,3), new Vector4(2,3),			
			new Vector4(-14,2), new Vector4(-13,2), new Vector4(-12,2), new Vector4(-11,2), new Vector4(-7,2), 	new Vector4(-3,2), new Vector4(14,2), new Vector4(13,2), new Vector4(12,2), new Vector4(11,2), new Vector4(7,3), new Vector4(3,3),
			new Vector4(-14,1), new Vector4(-13,1), new Vector4(-12,1), new Vector4(-11,1), new Vector4(-7,1), 	new Vector4(-4,1), new Vector4(-1,1), new Vector4(0,1),new Vector4(1,1), new Vector4(7,1), new Vector4(4,1), new Vector4(14,1), new Vector4(13,1), new Vector4(12,1), new Vector4(11,1),
			   
	    };

	   	// This is sending them to the compute shader
    	cs.SetVectorArray("neighborhood0", neighborhood0);
    	cs.SetVectorArray("neighborhood1", neighborhood1);
    	cs.SetVectorArray("neighborhood2", neighborhood2);
    	cs.SetVectorArray("neighborhood3", neighborhood3);

    	// Runs the compute shader for the resetkernel.
    	// Launches the indicated number of compute shader thread groups (rez x rez x 1)
    	cs.Dispatch(k, rez, rez, 1);

    	// Swaps read and write textures.
    	SwapTex();
    }

    /*
    *
    *
    * BUTTONS
    *
    * 
    */


   
 	[Button]
    public void SetRenderStyle()
    {
     
        cs.SetFloat("colorALow", colorALow);
        cs.SetFloat("colorAHigh", colorAHigh);
        cs.SetFloat("colorBLow", colorBLow);
        cs.SetFloat("colorBHigh", colorBHigh);
        cs.SetFloat("minBrightness", minBrightness);
        cs.SetFloat("minSaturation", minSaturation);
        cs.SetFloat("maxBrightness", maxBrightness);
        cs.SetFloat("maxSaturation", maxSaturation);

    }






    /*
    *
    *
    * STEP
    *
    *
    */

    [Button]
    public void Step()
    { 	
    	// Set all the textures to the step kernel
    	cs.SetTexture(stepKernel, "readTex", readTex);
    	cs.SetTexture(stepKernel, "writeTex", writeTex);
    	cs.SetTexture(stepKernel, "outTex", outTex);
    	
    	// Make it render even when not playing
    	if (!Application.isPlaying)
        {
            UnityEditor.SceneView.RepaintAll();
        }

        // Runs the compute shader for the stepKernel.
    	// Launches the indicated number of compute shader thread groups (rez x rez x 1)
    	cs.Dispatch(stepKernel, rez, rez, 1);

    	// Swaps read and write textures
    	SwapTex();


    	// Sets the texture for the material to the out texture
    	outMat.SetTexture("_UnlitColorMap", outTex);


    }

    private void SwapTex()
    {
    	// Swaps read and write textures
    	RenderTexture tmp = readTex;
    	readTex = writeTex;
    	writeTex = tmp;
    }

}
