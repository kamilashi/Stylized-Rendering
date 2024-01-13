using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class RedirectRender : MonoBehaviour
{
    public RenderTexture textToBlitTo;

    [ExecuteInEditMode]
    protected virtual void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, textToBlitTo);
    }

}
