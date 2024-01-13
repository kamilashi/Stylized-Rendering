using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public abstract class PostProPP : MonoBehaviour
{
    public ComputeShader shader = null;

  //  private string baseKernel = "Combine";

    protected Vector2Int texSize = new Vector2Int(0, 0);
    protected Vector2Int groupSize = new Vector2Int();
    protected Camera thisCamera;

/*
    [SerializeField]
    private RenderTexture sourceTexture = null;
    private string sourceTextureName;
    [SerializeField]
    private RenderTexture outputTexture = null;
    private string outputTextureName;*/

  //  private int defaultKernel = -1;
    protected bool init = false;

    protected virtual void Init()
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

        //defaultKernel = shader.FindKernel(baseKernel);

        thisCamera = GetComponent<Camera>();

        if (!thisCamera)
        {
            Debug.LogError("Object has no Camera");
            return;
        }

        init = true;
        //CreateTextures();
    }

    protected void ClearTexture(ref RenderTexture textureToClear)
    {
        if (null != textureToClear)
        {
            textureToClear.Release();
            textureToClear = null;
        }
    }
    protected virtual void ClearTextures()
    {
/*
        ClearTexture(ref outputTexture);
        ClearTexture(ref sourceTexture);*/
    }

    protected void CreateTexture(ref RenderTexture textureToMake, int divide = 1)
    {
        textureToMake = new RenderTexture(texSize.x / divide, texSize.y / divide, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        textureToMake.enableRandomWrite = true;
        textureToMake.Create();
    }

    protected virtual void CreateTextures()//only used in base class
    {
/*
        texSize.x = thisCamera.pixelWidth;
        texSize.y = thisCamera.pixelHeight;


        if (shader)
        {
            shader.SetInts("screenResolution", new int[] { texSize.x, texSize.y });
            uint x, y;
            shader.GetKernelThreadGroupSizes(defaultKernel, out x, out y, out _);
            groupSize.x = Mathf.CeilToInt((float)texSize.x / (float)x);
            groupSize.y = Mathf.CeilToInt((float)texSize.y / (float)y);
        }

        CreateTexture(ref sourceTexture);
        CreateTexture(ref outputTexture);

        shader.SetTexture(defaultKernel, this.sourceTextureName, sourceTexture);
        shader.SetTexture(defaultKernel, this.outputTextureName, outputTexture);*/
    }

    protected void OnEnable()
    {
        Init();
        CreateTextures();
    }

    protected void OnDisable()
    {
        ClearTextures();
        init = false;
    }

    protected void OnDestroy()
    {
        ClearTextures();
        init = false;
    }

    protected virtual void DispatchWithSource(ref RenderTexture source, ref RenderTexture destination)
    {
        Graphics.Blit(source, destination);
/*
        Graphics.Blit(source, sourceTexture);

        shader.Dispatch(defaultKernel, groupSize.x, groupSize.y, 1);

        Graphics.Blit(outputTexture, destination);*/
    }

    protected virtual void CheckResolution(out bool resChange)
    {
        resChange = false;

        if (texSize.x != thisCamera.pixelWidth || texSize.y != thisCamera.pixelHeight)
        {
            resChange = true;
            CreateTextures();
        }
    }

    protected virtual void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!init || shader == null)
        {
            Graphics.Blit(source, destination);
        }
        else
        {
            CheckResolution(out _);
            DispatchWithSource(ref source, ref destination);
        }
    }

}

