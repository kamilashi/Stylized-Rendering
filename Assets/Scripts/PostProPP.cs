using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PostProPP : MonoBehaviour
{
    public ComputeShader shader = null;

    protected string kernelName = "OutlinePostPro";

    protected Vector2Int texSize = new Vector2Int(0, 0);
    protected Vector2Int groupSize = new Vector2Int();
    protected Camera thisCamera;
    
    [SerializeField]
    protected RenderTexture output = null;
    [SerializeField]
    protected RenderTexture renderedSource = null;

    protected int kernelPostPro = -1;
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

        kernelPostPro = shader.FindKernel(kernelName);

        thisCamera = GetComponent<Camera>();

        if (!thisCamera)
        {
            Debug.LogError("Object has no Camera");
            return;
        }

        CreateTextures();

        init = true;
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
        ClearTexture(ref output);
        ClearTexture(ref renderedSource);
    }

    protected void CreateTexture(ref RenderTexture textureToMake, int divide = 1)
    {
        textureToMake = new RenderTexture(texSize.x / divide, texSize.y / divide, 0);
        textureToMake.enableRandomWrite = true;
        textureToMake.Create();
    }


    protected virtual void CreateTextures()
    {
        texSize.x = thisCamera.pixelWidth;
        texSize.y = thisCamera.pixelHeight;


        if (shader)
        {
            shader.SetInts("screenResolution", new int[] { texSize.x, texSize.y });
            uint x, y;
            shader.GetKernelThreadGroupSizes(kernelPostPro, out x, out y, out _);
            groupSize.x = Mathf.CeilToInt((float)texSize.x / (float)x);
            groupSize.y = Mathf.CeilToInt((float)texSize.y / (float)y);
        }

        CreateTexture(ref output);
        CreateTexture(ref renderedSource);

        shader.SetTexture(kernelPostPro, "source", renderedSource);
        shader.SetTexture(kernelPostPro, "output", output);
    }

    protected virtual void OnEnable()
    {
        Init();
        CreateTextures();
    }

    protected virtual void OnDisable()
    {
        ClearTextures();
        init = false;
    }

    protected virtual void OnDestroy()
    {
        ClearTextures();
        init = false;
    }

    protected virtual void DispatchWithSource(ref RenderTexture source, ref RenderTexture destination)
    {
        Graphics.Blit(source, renderedSource);

        shader.Dispatch(kernelPostPro, groupSize.x, groupSize.y, 1);

        Graphics.Blit(output, destination);
    }

    protected void CheckResolution(out bool resChange)
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

