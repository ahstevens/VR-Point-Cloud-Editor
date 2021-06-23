
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using System;
using System.Linq;
using System.Runtime.InteropServices;

public class XRFlyingInterface : MonoBehaviour
{
    [DllImport("PointCloudPlugin")]
    private static extern void RequestToDeleteFromUnity(IntPtr center, float size);

    public bool desktopFlyingMode;

    public float translationMultiplier = 100f;
    public float rotationMultiplier = 0.05f;

    public GameObject XRRigOrMainCamera;

    public GameObject bat;

    public GameObject deletionSphere;

    public InputAction setReferenceAction;
    public InputAction flyAction;
    public InputAction deleteSphereAction;

    public float deleteRate = 0.25f;

    private bool flying;
    private GameObject trackingReference;
    private GameObject flyingOrigin;


    // Start is called before the first frame update
    void Start()
    {        
        setReferenceAction.started += ctx => OnSetReference();
        flyAction.started += ctx => OnBeginFly();
        flyAction.canceled += ctx => OnEndFly();
        deleteSphereAction.started += ctx => OnBeginDeleteSphere();
        deleteSphereAction.canceled += ctx => OnEndDeleteSphere();

        flying = false;

        if (!desktopFlyingMode)
        {
            trackingReference = new GameObject("Tracking Reference");
            trackingReference.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (flying)
        {
            Debug.Log("Flying...");
            Vector3 relativeTranslation = bat.transform.position - flyingOrigin.transform.position;
            Quaternion relativeRotation = bat.transform.rotation * Quaternion.Inverse(flyingOrigin.transform.rotation);

            float displacementCubed = Mathf.Pow(relativeTranslation.magnitude, 3);

            Vector3 cameraOffset = trackingReference.transform.InverseTransformDirection(relativeTranslation);
            cameraOffset = XRRigOrMainCamera.transform.TransformDirection(cameraOffset).normalized;
            
            XRRigOrMainCamera.transform.position = XRRigOrMainCamera.transform.position + cameraOffset * displacementCubed * translationMultiplier;
            XRRigOrMainCamera.transform.rotation = XRRigOrMainCamera.transform.rotation * Quaternion.Slerp(Quaternion.identity, relativeRotation, rotationMultiplier);
        }
    }

    void OnEnable()
    {
        setReferenceAction.Enable();
        flyAction.Enable();
        deleteSphereAction.Enable();
    }

    void OnDisable()
    {
        setReferenceAction.Disable();
        flyAction.Disable();
        deleteSphereAction.Disable();
    }

    void OnSetReference()
    {
        if (trackingReference == null)
            trackingReference = new GameObject("Tracking Reference");

        trackingReference.transform.SetPositionAndRotation(bat.transform.position, bat.transform.rotation);
        
        Debug.Log("Tracking Reference Set!");
    }

    void OnBeginFly()
    {
        if (trackingReference == null)
        {
            Debug.Log("Tracking Reference needs to be set! Flying disabled.");
            return;
        }

        flyingOrigin = new GameObject("Flying Origin");
        flyingOrigin.transform.SetPositionAndRotation(bat.transform.position, bat.transform.rotation);

        flying = true;
        Debug.Log("Flying started");
    }

    void OnEndFly()
    {
        if (flying)
        {
            Destroy(flyingOrigin);

            flying = false;
            Debug.Log("Flying ended");
        }
    }

    void OnBeginDeleteSphere()
    {
        if (deletionSphere == null)
            return;

        InvokeRepeating("deleteInSphere", 0, deleteRate);
        Debug.Log("Editing Started");
    }

    void OnEndDeleteSphere()
    {       
        CancelInvoke("deleteInSphere");
        Debug.Log("Editing Finished");        
    }

    void deleteInSphere()
    {
        Debug.Log("DELETE " + deletionSphere.transform.position + " | " + deletionSphere.transform.localScale);

        float[] center = new float[3];
        center[0] = deletionSphere.transform.position.x;
        center[1] = deletionSphere.transform.position.y;
        center[2] = deletionSphere.transform.position.z;

        GCHandle toDelete = GCHandle.Alloc(center.ToArray(), GCHandleType.Pinned);
        RequestToDeleteFromUnity(toDelete.AddrOfPinnedObject(), deletionSphere.transform.localScale.x / 2.0f);
    }
}
