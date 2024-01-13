using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class OutlinesPostPro : PostProPP
{
    public bool readyForPostPro;
    public Camera outlineCameraDuplicate;
    public Camera distortionCameraDuplicate;

    public RenderTexture renderedSource;
    public RenderTexture tempTexture;

    public bool outlineMapView;
    [Range(0.001f, 0.7f)]
    public float outlineThreshold = 0.1f;
    public Shader outlineShader;
    public RenderTexture outlineMap;
    public RenderTexture outputOutline;
    private int kernelOutline;

    public bool distortionMapView;
    public Shader distortionShader;
    public RenderTexture distortionMap;
    RedirectRender distortionRedirectRender;
    public RenderTexture outputDistortion;
    public Vector3 cameraMotionDirection;
    private int kernelDistortion;


    [ExecuteInEditMode]
    protected override void Init()
    {
        base.Init();
        CreateTextures();
        //tempTexture = RenderTexture.GetTemporary();

        kernelOutline = shader.FindKernel("Outline");
        outlineCameraDuplicate.enabled = false;
        outlineCameraDuplicate.SetReplacementShader(outlineShader, "OutlineType"); //????


        kernelDistortion = shader.FindKernel("CameraMotionDistortion");
        distortionCameraDuplicate.targetTexture = distortionMap;
        distortionCameraDuplicate.enabled = false;
        distortionRedirectRender = distortionCameraDuplicate.gameObject.GetComponent<RedirectRender>();
        distortionRedirectRender.textToBlitTo = distortionMap;
    }

    protected override void ClearTextures()
    {
        ClearTexture(ref renderedSource);
        ClearTexture(ref outlineMap);
        ClearTexture(ref outputOutline);

        ClearTexture(ref distortionMap);
        ClearTexture(ref outputDistortion);
    }

    [ExecuteInEditMode]
    protected override void CreateTextures()
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
        shader.SetTexture(kernelOutline, "source", renderedSource);
        CreateTexture(ref outlineMap);
        shader.SetTexture(kernelOutline, "outlineMap", outlineMap);
        CreateTexture(ref outputOutline);
        //outputOutline.enableRandomWrite = true;
        shader.SetTexture(kernelOutline, "outputOutline", outputOutline);

        shader.SetTexture(kernelDistortion, "outputOutline", outputOutline);
        CreateTexture(ref distortionMap);
        shader.SetTexture(kernelDistortion, "distortionMap", distortionMap);
        CreateTexture(ref outputDistortion);
        shader.SetTexture(kernelDistortion, "outputDistortion", outputDistortion);
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
    }

    [ExecuteInEditMode]
    protected override void DispatchWithSource(ref RenderTexture source, ref RenderTexture destination)
    {
        if ((!init) || (!readyForPostPro)) return;

        Graphics.Blit(source, renderedSource);
        //Graphics.Blit(tempTexture, renderedSource);

        outlineCameraDuplicate.targetTexture = outlineMap;
        outlineCameraDuplicate.RenderWithShader(outlineShader, "OutlineType");
        outlineMap = outlineCameraDuplicate.activeTexture;

        shader.Dispatch(kernelOutline, groupSize.x, groupSize.y, 1);

        distortionCameraDuplicate.targetTexture = distortionMap;
        distortionCameraDuplicate.Render();
        distortionMap = distortionCameraDuplicate.activeTexture;
        shader.Dispatch(kernelDistortion, groupSize.x, groupSize.y, 1);

        Graphics.Blit(outputDistortion, destination);
    }

    [ExecuteInEditMode]
    protected override void OnRenderImage(RenderTexture source, RenderTexture destination)
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

    protected override void CheckResolution(out bool resChange)
    {
        resChange = false;

        if (texSize.x != thisCamera.pixelWidth || texSize.y != thisCamera.pixelHeight)
        {
            resChange = true;
            CreateTextures();
        }
    }

}
