using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CameraMotionDistortion : MonoBehaviour
{
    public Material DistortionMaterial;
    public GameObject DistortionBillboard;
    public float CustomScaleX = 1.0f;
    public float CustomScaleY = 1.0f;



    void Start()
    {
        CreateBillboard();
    }

    [ExecuteInEditMode]
    void OnEnable()
    {
        if (DistortionBillboard != null)
        { DestroyBillboard(); }
        CreateBillboard();
    }

    void CreateBillboard()
    {
        if (this.DistortionBillboard == null)
        {
            DistortionBillboard = GameObject.CreatePrimitive(PrimitiveType.Quad);
            DistortionBillboard.gameObject.transform.SetPositionAndRotation(this.gameObject.transform.position, this.gameObject.transform.rotation);
            DistortionBillboard.gameObject.transform.SetParent(this.gameObject.transform);
            DistortionBillboard.gameObject.layer = LayerMask.NameToLayer("Distortion");

            DistortionMaterial.SetFloat("_ScaleX", this.gameObject.transform.localScale.x * CustomScaleX);
            DistortionMaterial.SetFloat("_ScaleY", this.gameObject.transform.localScale.y * CustomScaleY);

            DistortionBillboard.GetComponent<MeshRenderer>().material = DistortionMaterial;
        }
    }

    private void OnDestroy()
    {
        DestroyBillboard();
    }

    [ExecuteInEditMode]
    private void DestroyBillboard()
    {
        if (runInEditMode)
        {
            DestroyImmediate(DistortionBillboard);
        }
        else
        {
            Destroy(DistortionBillboard);
        }
        DistortionBillboard = null;
    }

    [ExecuteInEditMode]
    private void OnDisable()
    {
        DestroyBillboard();
    }
}
