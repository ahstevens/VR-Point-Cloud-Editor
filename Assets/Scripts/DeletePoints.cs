using System;
using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem;

public class DeletePoints : MonoBehaviour
{
    [DllImport("PointCloudPlugin")]
    private static extern void RequestToDeleteFromUnity(IntPtr center, float size);
    [DllImport("PointCloudPlugin")]
    static public extern void setHighlightDeletedPointsActive(bool active);

    public InputAction deleteSphereAction;

    public float deleteRate = 0.25f;

    private GameObject deletionSphere;

    // Start is called before the first frame update
    void Start()
    {
        deletionSphere = this.gameObject;

        deleteSphereAction.started += ctx => OnBeginDeleteSphere();
        deleteSphereAction.canceled += ctx => OnEndDeleteSphere();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnEnable()
    {
        deleteSphereAction.Enable();
    }

    void OnDisable()
    {
        deleteSphereAction.Disable();
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
