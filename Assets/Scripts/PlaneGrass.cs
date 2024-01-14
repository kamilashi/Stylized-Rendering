using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class PlaneGrass : MonoBehaviour
{
    public ComputeShader shader;
    public PostProcessing postproControllerScript;
    public int texResolution = 1024;
    public Material groundMaterial;
    public Material grassMaterial;
    public Material debugMaterial;
    [Range(0, 1)]
    public float density;
    [Range(0.1f, 3)]
    public float scale;
    [Range(10, 45)]
    public float maxBend;
    [Range(0, 2)]
    public float windSpeed;
    [Range(0, 360)]
    public float windDirection;
    [Range(10, 1000)]
    public float windScale;
    [Range(0.1f, 10.0f)]
    public float heightModifier;

    Renderer rend;
    RenderTexture heightMapTexture;
    RenderTexture debugText;

    int kernelHandleGrass;
    struct GrassBlade
    {
        public Vector3 position;
        public float bend;
        public float noise;
        public float fade;
        public float tallness;

        public GrassBlade(Vector3 pos)
        {
            position.x = pos.x;
            position.y = pos.y;
            position.z = pos.z;
            bend = 0;
            noise = Random.Range(0.5f, 1) * 2 - 1;
            fade = Random.Range(0.5f, 1);
            tallness = 1.0f;
        }
    }
    ComputeBuffer bladesBuffer;
    ComputeBuffer argsBuffer;
    int groupSize;
    int timeID;
    GrassBlade[] bladesArray;
    uint[] argsArray = new uint[] { 0, 0, 0, 0, 0 };
    Bounds bounds;
    int SIZE_GRASS_BLADE = 7 * sizeof(float);
    Mesh blade;
    private bool postrpo =false;
    Bounds grass_bounds;
    Mesh Blade
    {
        get
        {
            Mesh mesh;

            if (blade != null)
            {
                mesh = blade;
            }
            else
            {
                mesh = new Mesh();

                float height = 0.2f;
                float rowHeight = height / 4;
                float halfWidth = height / 10;


                Vector3[] vertices =
                {
                    new Vector3(-halfWidth, 0, 0), //bottomLeft
                    new Vector3( halfWidth, 0, 0), //bottom right
                    new Vector3(-halfWidth, rowHeight, 0), //topLeft
                    new Vector3( halfWidth, rowHeight, 0), //topRight
                    new Vector3(-halfWidth*0.9f, rowHeight*2, 0),
                    new Vector3( halfWidth*0.9f, rowHeight*2, 0),
                    new Vector3(-halfWidth*0.8f, rowHeight*3, 0),
                    new Vector3( halfWidth*0.8f, rowHeight*3, 0),
                    new Vector3( 0, rowHeight*4, 0)
                };

                Vector3 normal = new Vector3(0, 0, -1);

                Vector3[] normals =
                {
                    normal,
                    normal,
                    normal,
                    normal,
                    normal,
                    normal,
                    normal,
                    normal,
                    normal
                };

                Vector2[] uvs =
                {
                    new Vector2(0,0),
                    new Vector2(1,0),
                    new Vector2(0,0.25f),
                    new Vector2(1,0.25f),
                    new Vector2(0,0.5f),
                    new Vector2(1,0.5f),
                    new Vector2(0,0.75f),
                    new Vector2(1,0.75f),
                    new Vector2(0.5f,1)
                };

                int[] indices =
                {
                    0,1,2,1,3,2,//row 1
                    2,3,4,3,5,4,//row 2
                    4,5,6,5,7,6,//row 3
                    6,7,8//row 4
                };

                mesh.vertices = vertices;
                mesh.normals = normals;
                mesh.uv = uvs;
                mesh.SetIndices(indices, MeshTopology.Triangles, 0);

            }

            return mesh;
        }
    }

    // Use this for initialization
    void Start()
    {

        kernelHandleGrass = shader.FindKernel("InitializeGrass");

        heightMapTexture = new RenderTexture(texResolution, texResolution, 0);
        heightMapTexture.enableRandomWrite = true;
        heightMapTexture.Create();

        debugText = new RenderTexture(texResolution, texResolution, 0);
        debugText.enableRandomWrite = true;
        debugText.Create();

        rend = GetComponent<Renderer>();
        rend.enabled = true;

        GenerateHeightMap();

        
        blade = Blade; 
        grass_bounds = blade.bounds;

        InitGrass();
    }
    private void GenerateHeightMap()
    {
        int kernelGenerateHeight = shader.FindKernel("GenerateHeight");


        shader.SetInt("texResolution", texResolution);
        shader.SetTexture(kernelGenerateHeight, "GeneratedNoiseMap", heightMapTexture);
        shader.SetTexture(kernelGenerateHeight, "Debug", debugText);
        groundMaterial.SetTexture("_HeightMap", heightMapTexture);
        groundMaterial.SetFloat("_HeightModifier", heightModifier);
        grassMaterial.SetTexture("_HeightMap", heightMapTexture);
        debugMaterial.SetTexture("_DebugMap", debugText);

        uint threadGroupSizeX;
        uint threadGroupSizeY;
        //float planeScale = 1.0f;
        float planeScale = transform.localScale.x;
        shader.SetFloat("planeScale", planeScale);
        shader.SetFloat("heightModifier", heightModifier);
        shader.GetKernelThreadGroupSizes(kernelGenerateHeight, out threadGroupSizeX, out threadGroupSizeY, out _);
        shader.Dispatch(kernelGenerateHeight, Mathf.CeilToInt((float)texResolution / (float)threadGroupSizeX), Mathf.CeilToInt((float)texResolution / (float)threadGroupSizeY), 1);
    }
    RenderParams rp;
    Matrix4x4[] instData;
    private void InitGrass()
    {
        shader.SetTexture(kernelHandleGrass, "GeneratedNoiseMap", heightMapTexture);
        shader.SetTexture(kernelHandleGrass, "Debug", debugText);

        MeshFilter mf = GetComponent<MeshFilter>();
        //Bounds bounds = GetComponent<Renderer>().bounds;
        Bounds bounds = mf.sharedMesh.bounds;

        Vector3 blades = bounds.extents;
        Vector3 vec = transform.localScale / 0.1f * density;
        //Vector3 vec = new Vector3(20.0f, 1, 20.0f) * density;
        blades.x *= vec.x;
        blades.z *= vec.z;
        float planeSideSize = bounds.size.x;
        shader.SetFloat("planeSideSize", planeSideSize);
        shader.SetFloats("planeCenter", new float[]{bounds.center.x, bounds.center.y});

        int total = (int)blades.x * (int)blades.z;

        uint threadGroupSize;
        shader.GetKernelThreadGroupSizes(kernelHandleGrass, out threadGroupSize, out _, out _);
        groupSize = Mathf.CeilToInt((float)total / (float)threadGroupSize);
        int count = groupSize * (int)threadGroupSize;

        bladesArray = new GrassBlade[count];

         rp = new RenderParams(grassMaterial);
        instData = new Matrix4x4[10];

        //for (int i = 0; i < 10; i++)
        //{ instData[i] = Matrix4x4.Translate(new Vector3(-5.0f + i, 0.0f, 0.0f)); }

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3((Random.value * bounds.extents.x * 2 - bounds.extents.x + bounds.center.x),
                                       0,
                                      (Random.value * bounds.extents.z * 2 - bounds.extents.z + bounds.center.z));

            pos = transform.TransformPoint(pos);
            bladesArray[i] = new GrassBlade(pos);
        }

        bladesBuffer = new ComputeBuffer(count, SIZE_GRASS_BLADE);
        bladesBuffer.SetData(bladesArray);

        shader.SetBuffer(kernelHandleGrass, "bladesBuffer", bladesBuffer);
        shader.SetFloat("maxBend", maxBend * Mathf.PI / 180);
        
        Vector4 wind = new Vector4();
        shader.SetVector("wind", wind);

        timeID = Shader.PropertyToID("time");

        argsArray[0] = blade.GetIndexCount(0);
        argsArray[1] = (uint)count;
        Debug.Log("Args = " + argsArray[0] + " , " + argsArray[1]);
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(argsArray);

        grassMaterial.SetBuffer("bladesBuffer", bladesBuffer);
        grassMaterial.SetFloat("_Scale", scale);

        shader.Dispatch(kernelHandleGrass, groupSize, 1, 1);
        postrpo = true;
        postproControllerScript.readyForPostPro = true;
    }
    
    private void OnValidate()
    {
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            float theta = windDirection * Mathf.PI / 180;
            Vector4 wind = new Vector4(Mathf.Cos(theta), Mathf.Sin(theta), windSpeed, windScale);
            shader.SetVector("wind", wind);
    }
    private float Discretize(float value)
    {
        float freqRes = (float)10 /(float) texResolution; //max bound; from 0 - max
        return value * freqRes;
    }
    void Update()
    {

        shader.SetFloat(timeID, Time.time);
        shader.SetFloat("heightModifier", heightModifier);
        shader.Dispatch(kernelHandleGrass, groupSize, 1, 1);
        groundMaterial.SetFloat("_HeightModifier", heightModifier);
        //Graphics.DrawMeshInstancedIndirect(blade, 0, grassMaterial, grass_bounds, argsBuffer);
        Graphics.DrawMeshInstancedProcedural(blade, 0, grassMaterial, grass_bounds, bladesBuffer.count);
        
    }
    private void OnDestroy()
    {
        bladesBuffer.Release();
        argsBuffer.Release();
    }
}
