using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AxisNames {
    X,
    XY,
    Y,
    MinusXY,
    MinusX,
    MinusXMinusY,
    MinusY,
    XMinusY
}

public static class Utils
{
    public static float Remap(this float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class PostProcessing : MonoBehaviour
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
    [Range(0, 359.9f)]
    public float skewAngleDegrees;
    private float skewAngleRadians;
    public float cameraVelocityMagnitude;
    public float velocityMagnitudeThreshold;
    public float MaxMouseMotionLength;
    public float dissipationSpeed;
    public int blurRadius;
    public Vector2Int translateXY;
    [Range (0, 10)]
    public float scale;
    private int kernelCombinePasses;
    private int kernelApplyDistortion;

    // texture rotation
    int kernelSkewTexture;
    int kernelUnskewTexture;
    public RenderTexture skewedPreDistortion;
    public RenderTexture unskewedPostDistortion;

    public Hashtable AxisMap;
    //private bool fadeout;
    //private float timer;

    private void Start()
    {
        Init();
        CreateTextures();
        MaxMouseMotionLength = Vector2.SqrMagnitude(new Vector2(Screen.width, Screen.height));
    }
    private void Update()
    {
        if (!init)
            Init();
        SetProperties();


        if (cameraVelocityMagnitude > 0)
        {
            cameraVelocityMagnitude -= dissipationSpeed * Time.deltaTime;
        }
    }

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


        kernelCombinePasses = shader.FindKernel("CombineDistortiondMap");
        kernelApplyDistortion = shader.FindKernel("DisortionAlongVelocity");

        distortionCameraDuplicate.targetTexture = distortionMap;
        distortionCameraDuplicate.enabled = false;
        distortionRedirectRender = distortionCameraDuplicate.gameObject.GetComponent<RedirectRender>();
        distortionRedirectRender.textToBlitTo = distortionMap;

        kernelSkewTexture = shader.FindKernel("SkewTexture");
        kernelUnskewTexture = shader.FindKernel("UnskewTexture");

        AxisMap = new Hashtable();
        AxisMap.Add(AxisNames.X, new Vector2Int(1, 0));                 // 0
        AxisMap.Add(AxisNames.XY, new Vector2Int(1, 1));                // 1
        AxisMap.Add(AxisNames.Y, new Vector2Int(0, 1));                 //...
        AxisMap.Add(AxisNames.MinusXY, new Vector2Int(-1, 1));
        AxisMap.Add(AxisNames.MinusX, new Vector2Int(-1, 0));
        AxisMap.Add(AxisNames.MinusXMinusY, new Vector2Int(-1, -1));
        AxisMap.Add(AxisNames.MinusY, new Vector2Int(0, -1));
        AxisMap.Add(AxisNames.XMinusY, new Vector2Int(1, -1));          // 7

        init = true;
    }

    public Vector2Int GetAxisFromDegree(int counterClockDeg) // step = 22 * 2 to each side of the axis + center = 45 degree per covering axis
    {
        counterClockDeg += 22;
        if (counterClockDeg >= 180) 
        {
            if (counterClockDeg >= 270)
            {
                if (counterClockDeg >= 315)
                {
                    if (counterClockDeg >= 359)
                    {
                        return (Vector2Int)AxisMap[AxisNames.X]; // handle overflow from shift (+ 22)
                    }
                    return (Vector2Int)AxisMap[AxisNames.XMinusY];
                }
                else
                {
                    return (Vector2Int)AxisMap[AxisNames.MinusY];
                }
            }
            else
            {
                if (counterClockDeg >= 225)
                {
                    return (Vector2Int)AxisMap[AxisNames.MinusXMinusY];
                }
                else
                {
                    return (Vector2Int)AxisMap[AxisNames.MinusX];
                }
            }
        }
        else // 0 (-22 exc) to 180 (202 inc)
        {
            if (counterClockDeg >= 90)
            {
                if (counterClockDeg >= 135)
                {
                    return (Vector2Int)AxisMap[AxisNames.MinusXY];
                }
                else
                {
                    return (Vector2Int)AxisMap[AxisNames.Y];
                }
            }
            else
            {
                if (counterClockDeg >= 45)
                {
                    return (Vector2Int)AxisMap[AxisNames.XY];
                }
                else
                {
                    return (Vector2Int)AxisMap[AxisNames.X];
                }
            }
        }
    }
    public void UpdateCameraVelocity(Vector2 newVelocity)
    {
        float lengthOfVector = newVelocity.sqrMagnitude;
        //blurRadius = (int) lengthOfVector * 10.0f; // to max blur radius
        if (lengthOfVector > velocityMagnitudeThreshold && lengthOfVector > cameraVelocityMagnitude)
        {
            //lengthOfVector = Utils.Remap(lengthOfVector, 0, MaxMouseMotionLength, 0, 100.0f); // to max alpha
            lengthOfVector /= 10.0f;// Utils.Remap(lengthOfVector, 0, MaxMouseMotionLength, 0, 100.0f); // to max alpha

            newVelocity.Normalize();
            skewAngleRadians = Mathf.Acos( Vector2.Dot(newVelocity, Vector2.right));
            if (Vector2.Dot(newVelocity, Vector2.up) <= 0) // if larger than PI
            {
                skewAngleRadians = Mathf.PI - skewAngleRadians; // normalize to 180 <-> 360
            }
            skewAngleDegrees = skewAngleRadians * Mathf.Rad2Deg;
            cameraVelocityMagnitude = lengthOfVector;
        }
        //else
        //{
        //    fadeout = true;
        //    timer = 3;
        //    //cameraVelocityMagnitude = 0.0f;
        //}
    }

    private void CreateTexture(ref RenderTexture textureToMake, int divide = 1)
    {
        textureToMake = new RenderTexture(texSize.x / divide, texSize.y / divide, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        textureToMake.enableRandomWrite = true;
        textureToMake.wrapMode = TextureWrapMode.Repeat;
        textureToMake.Create();
    }

    [ExecuteAlways]
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
        CreateTexture(ref distortionMap);
        CreateTexture(ref skewedPreDistortion);
        CreateTexture(ref outputPreDistortion);
        CreateTexture(ref unskewedPostDistortion);
        CreateTexture(ref outputPostDistortion);

        shader.SetTexture(kernelOutline, "source", renderedSource);
        shader.SetTexture(kernelOutline, "outlineMap", outlineMap);
        shader.SetTexture(kernelOutline, "outputOutline", outputOutline);

        shader.SetTexture(kernelSkewTexture, "distortionMap", distortionMap);
        shader.SetTexture(kernelSkewTexture, "skewedPreDistortion", skewedPreDistortion);

        shader.SetTexture(kernelApplyDistortion, "distortionMap", distortionMap);
        shader.SetTexture(kernelApplyDistortion, "skewedPreDistortion", skewedPreDistortion);
        shader.SetTexture(kernelApplyDistortion, "outputPreDistortion", outputPreDistortion); 

        shader.SetTexture(kernelUnskewTexture, "outputPreDistortion", outputPreDistortion);
        shader.SetTexture(kernelUnskewTexture, "skewedPreDistortion", skewedPreDistortion);
        shader.SetTexture(kernelUnskewTexture, "unskewedPostDistortion", unskewedPostDistortion);

        shader.SetTexture(kernelCombinePasses, "outputOutline", outputOutline);
        shader.SetTexture(kernelCombinePasses, "outputPreDistortion", outputPreDistortion);
        shader.SetTexture(kernelCombinePasses, "outputPostDistortion", outputPostDistortion);
    }

    [ExecuteAlways]
    private void OnValidate()
    {
        if (!init)
            Init();
        SetProperties();
    }

    [ExecuteAlways]
    protected void SetProperties()
    {
        shader.SetBool("outlineMapView", outlineMapView);
        shader.SetBool("distortionMapView", distortionMapView);
        shader.SetFloat("outlineThreshold", outlineThreshold);
        shader.SetFloat("skewAngleRadians", skewAngleRadians);

        int switchValue = Mathf.CeilToInt( cameraVelocityMagnitude);
        blurRadius = (int)Mathf.Clamp(switchValue*cameraVelocityMagnitude, 0, 10);
        shader.SetInt("blurRadius", blurRadius);

        shader.SetFloat("cameraVelocityMagnitude", cameraVelocityMagnitude);

        shader.SetFloat("scale", scale);
        shader.SetInts("translateXY", translateXY.x, translateXY.y);

        Vector2Int currentAxis = GetAxisFromDegree((int) skewAngleDegrees);
        shader.SetInts("blurAlongXY", currentAxis.x, currentAxis.y);
    }

    [ExecuteAlways]
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

        //shader.Dispatch(kernelSkewTexture, groupSize.x, groupSize.y, 1);
        shader.Dispatch(kernelApplyDistortion, groupSize.x, groupSize.y, 1);
        //shader.Dispatch(kernelUnskewTexture, groupSize.x, groupSize.y, 1);

        shader.Dispatch(kernelCombinePasses, groupSize.x, groupSize.y, 1);

        Graphics.Blit(outputPostDistortion, destination);
    }

    [ExecuteAlways]
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

    [ExecuteAlways]
    private void OnEnable()
    {
        Init();
        CreateTextures();
    }

    [ExecuteAlways]
    private void OnDisable()
    {
        ClearTextures();
        init = false;
    }
    [ExecuteAlways]
    private void OnDestroy()
    {
        ClearTextures();
        init = false;
    }
}
