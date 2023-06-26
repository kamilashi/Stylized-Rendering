using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class OutlinesPostPro : PostProPP
{
    public bool outlineMapView;
    [Range(0.001f, 0.7f)]
    public float outlineThreshold = 0.1f;
    public Camera outlineCameraDuplicate;
    public Shader outlineShader;

    public bool readyForPostPro;

    public RenderTexture outlineMap;
    private int kernelOutline;
    private int kernelCombine;

    protected override void Init()
    {
        kernelName = "Outline";
        base.Init();
        kernelOutline = shader.FindKernel(kernelName);
        outlineCameraDuplicate.enabled = false;
        outlineCameraDuplicate.SetReplacementShader(outlineShader, "OutlineType");
    }

    protected override void CreateTextures()
    {
        base.CreateTextures();
        CreateTexture(ref outlineMap);
        shader.SetTexture(kernelOutline, "outlineMap", outlineMap);
    }

    private void OnValidate()
    {
        if (!init)
            Init();

        SetProperties();
    }

    protected void SetProperties()
    {
        shader.SetBool("outlineMapView", outlineMapView);
        shader.SetFloat("outlineThreshold", outlineThreshold);
    }

    protected override void DispatchWithSource(ref RenderTexture source, ref RenderTexture destination)
    {
        if ((!init) || (!readyForPostPro)) return;
        Graphics.Blit(source, renderedSource);

        outlineCameraDuplicate.targetTexture = outlineMap;
        outlineCameraDuplicate.RenderWithShader(outlineShader, "OutlineType");
        outlineMap = outlineCameraDuplicate.activeTexture;

        shader.Dispatch(kernelOutline, groupSize.x, groupSize.y, 1);
        shader.Dispatch(kernelCombine, groupSize.x, groupSize.y, 1);
        Graphics.Blit(output, destination);
    }

    protected override void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        outlineCameraDuplicate.CopyFrom(thisCamera);
        if (shader == null)
        {
            Graphics.Blit(source, destination);
        }
        else
        {
            bool resChange = false;
            CheckResolution(out resChange);
            if (resChange) SetProperties();

            //outlineCameraDuplicate.RenderWithShader(outlineShader, "OutlineType");


            DispatchWithSource(ref source, ref destination);
        }
    }

}
