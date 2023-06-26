using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class OutlinesPostPro : PostProPP
{
    public bool outlinesToggle;
    [Range(0.001f, 0.1f)]
    public float outlineThreshold = 0.1f;
    public Camera outlineCameraDuplicate;
    //public GameObject outlineCameraObj;
    public Shader outlineShader;

    public bool readyForPostPro;

    public RenderTexture outlineMap;
    private int kernelOutlineId;

    protected override void Init()
    {
        kernelName = "OutlinePostPro";
        base.Init();
        kernelOutlineId = shader.FindKernel(kernelName);
        outlineCameraDuplicate.enabled = false;
        outlineCameraDuplicate.SetReplacementShader(outlineShader, "OutlineType");
        outlineCameraDuplicate.targetTexture = outlineMap;
        //outlineCameraDuplicate.enabled = true;
        //outlineCameraObj.GetComponent<OutlineCamera>().textToBlitTo = outlineMap;
    }

    protected override void CreateTextures()
    {
        base.CreateTextures();
        CreateTexture(ref outlineMap);
        shader.SetTexture(kernelOutlineId, "outlineMap", outlineMap);
    }

    private void OnValidate()
    {
        if (!init)
            Init();

        SetProperties();
    }

    protected void SetProperties()
    {
        shader.SetBool("outlinesToggle", outlinesToggle);
        shader.SetFloat("outlineThreshold", outlineThreshold);
    }

    protected override void DispatchWithSource(ref RenderTexture source, ref RenderTexture destination)
    {
        if ((!init) || (!readyForPostPro)) return;
        Graphics.Blit(source, renderedSource);
        //outlineMap = outlineCameraObj.GetComponent<OutlineCamera>().textToBlitTo;
        outlineCameraDuplicate.targetTexture = outlineMap;
        outlineCameraDuplicate.RenderWithShader(outlineShader, "OutlineType");
        outlineMap = outlineCameraDuplicate.activeTexture;
        shader.Dispatch(kernelPostPro, groupSize.x, groupSize.y, 1);
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
