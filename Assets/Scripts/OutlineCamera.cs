using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OutlineCamera : MonoBehaviour
{
    public RenderTexture textToBlitTo;


    protected virtual void OnRenderImage(RenderTexture source, RenderTexture destination)
    {

        Graphics.Blit(source, textToBlitTo);
    }

}
