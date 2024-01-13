using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class OutlinesPostPro : MonoBehaviour
{
    // from parent class:
    public ComputeShader shader = null;
    public bool readyForPostPro;

    private Vector2Int texSize = new Vector2Int(0, 0);
    [SerializeField]
    private Vector2Int groupSize = new Vector2Int();
    private bool init;
    private Camera thisCamera;

    // render cameras
    public Camera outlineCameraDuplicate;
    public Camera distortionCameraDuplicate;

    // source texture
    public RenderTexture renderedSource;

    // outline pass
    public bool outlineMapView;
    [Range(0.001f, 0.7f)]
    public float outlineThreshold = 0.1f;
    public Shader outlineShader;
    public RenderTexture outlineMap;
    public RenderTexture outputOutline;
    private int kernelOutline;

    // distortion pass
    public bool distortionMapView;
    public Shader distortionShader;
    public RenderTexture distortionMap;
    RedirectRender distortionRedirectRender;
    public RenderTexture outputPreDistortion;
    public RenderTexture outputPostDistortion;
    public Vector2 cameraVelocity;
    public float skewAngle;
    public int lengthOfVector;
    private int kernelPreDistortion;
    private int kernelPostDistortion;

    private void Init()
    {
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogError("It seems your target Hardware does not support Compute Shaders.");
            return;
        }
        if (!shader)
        {
            Debug.LogError("No shader");
            return;
        }
        thisCamera = GetComponent<Camera>();
        if (!thisCamera)
        {
            Debug.LogError("Object has no Camera");
            return;
        }

        CreateTextures();

        kernelOutline = shader.FindKernel("Outline");
        outlineCameraDuplicate.enabled = false;
        outlineCameraDuplicate.SetReplacementShader(outlineShader, "OutlineType"); //????


        kernelPreDistortion = shader.FindKernel("CombineDistortiondMap");
        kernelPostDistortion = shader.FindKernel("DisortionAlongVelocity");

        distortionCameraDuplicate.targetTexture = distortionMap;
        distortionCameraDuplicate.enabled = false;
        distortionRedirectRender = distortionCameraDuplicate.gameObject.GetComponent<RedirectRender>();
        distortionRedirectRender.textToBlitTo = distortionMap;

        init = true;
    }

    private void CreateTexture(ref RenderTexture textureToMake, int divide = 1)
    {
        textureToMake = new RenderTexture(texSize.x / divide, texSize.y / divide, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        textureToMake.enableRandomWrite = true;
        textureToMake.Create();
    }
    [ExecuteInEditMode]
    private void CreateTextures()
    {
        texSize.x = thisCamera.pixelWidth;
        texSize.y = thisCamera.pixelHeight;

        if (shader)
        {
            shader.SetInts("screenResolution", new int[] { texSize.x, texSize.y });
            uint x, y;
            shader.GetKernelThreadGroupSizes(kernelOutline, out x, out y, out _);
            groupSize.x = Mathf.CeilToInt((float)texSize.x / (float)x);
            groupSize.y = Mathf.CeilToInt((float)texSize.y / (float)y);
        }

        CreateTexture(ref renderedSource);
        CreateTexture(ref outlineMap);
        CreateTexture(ref outputOutline);
        shader.SetTexture(kernelOutline, "source", renderedSource);
        shader.SetTexture(kernelOutline, "outlineMap", outlineMap);
        shader.SetTexture(kernelOutline, "outputOutline", outputOutline);

        CreateTexture(ref distortionMap);
        CreateTexture(ref outputPreDistortion);
        shader.SetTexture(kernelPreDistortion, "outputOutline", outputOutline);
        shader.SetTexture(kernelPreDistortion, "distortionMap", distortionMap);
        shader.SetTexture(kernelPreDistortion, "outputPreDistortion", outputPreDistortion);

        CreateTexture(ref outputPostDistortion);
        shader.SetTexture(kernelPostDistortion, "distortionMap", distortionMap);
        shader.SetTexture(kernelPostDistortion, "outputPreDistortion", outputPreDistortion);
        shader.SetTexture(kernelPostDistortion, "outputPostDistortion", outputPostDistortion);
    }

    [ExecuteInEditMode]
    private void OnValidate()
    {
        if (!init)
            Init();
        SetProperties();
    }

    [ExecuteInEditMode]
    protected void SetProperties()
    {
        shader.SetBool("outlineMapView", outlineMapView);
        shader.SetBool("distortionMapView", distortionMapView);
        shader.SetFloat("outlineThreshold", outlineThreshold);
        shader.SetFloat("skewAngle", skewAngle);
        shader.SetInt("lengthOfVector", lengthOfVector);
        shader.SetFloats("cameraVelocity",  cameraVelocity.x, cameraVelocity.y);
    }

    [ExecuteInEditMode]
    private void DispatchWithSource(ref RenderTexture source, ref RenderTexture destination)
    {
        if ((!init) || (!readyForPostPro)) return;

        Graphics.Blit(source, renderedSource);

        outlineCameraDuplicate.targetTexture = outlineMap;
        outlineCameraDuplicate.RenderWithShader(outlineShader, "OutlineType");
        outlineMap = outlineCameraDuplicate.activeTexture;

        shader.Dispatch(kernelOutline, groupSize.x, groupSize.y, 1);

        distortionCameraDuplicate.targetTexture = distortionMap;
        distortionCameraDuplicate.Render();
        distortionMap = distortionCameraDuplicate.activeTexture;
        shader.Dispatch(kernelPreDistortion, groupSize.x, groupSize.y, 1);
        shader.Dispatch(kernelPostDistortion, groupSize.x, groupSize.y, 1);

        Graphics.Blit(outputPostDistortion, destination);
    }

    [ExecuteInEditMode]
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        outlineCameraDuplicate.CopyFrom(thisCamera);
        distortionCameraDuplicate.CopyFrom(thisCamera);
        distortionCameraDuplicate.cullingMask = (int) LayerMask.GetMask("Distortion");
        distortionCameraDuplicate.depth = 1;

        if (shader == null)
        {
            Graphics.Blit(source, destination);
        }
        else
        {
            bool resChange = false;
            CheckResolution(out resChange);
            if (resChange) SetProperties();

            DispatchWithSource(ref source, ref destination);
        }
    }

    private void CheckResolution(out bool resChange)
    {
        resChange = false;

        if (texSize.x != thisCamera.pixelWidth || texSize.y != thisCamera.pixelHeight)
        {
            resChange = true;
            CreateTextures();
        }
    }
    private void ClearTexture(ref RenderTexture textureToClear)
    {
        if (null != textureToClear)
        {
            textureToClear.Release();
            textureToClear = null;
        }
    }
    private void ClearTextures()
    {
        ClearTexture(ref renderedSource);
        ClearTexture(ref outlineMap);
        ClearTexture(ref outputOutline);

        ClearTexture(ref distortionMap);
        ClearTexture(ref outputPreDistortion);
    }

    [ExecuteInEditMode]
    private void OnEnable()
    {
        Init();
        CreateTextures();
    }
    [ExecuteInEditMode]
    private void OnDisable()
    {
        ClearTextures();
        init = false;
    }
    [ExecuteInEditMode]
    private void OnDestroy()
    {
        ClearTextures();
        init = false;
    }
}
