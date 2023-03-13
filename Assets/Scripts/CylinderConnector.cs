using UnityEngine;

public class CylinderConnector : MonoBehaviour
{
    public Transform anchor;
    public Transform target;
    public float thickness = 0.001f;
    public bool dynamic = false;
 
    private void Start()
    {        
        if (!dynamic) UpdateConnection();
    }

    private void Update()
    {
        if (dynamic) UpdateConnection();
    }

    void UpdateConnection()
    {
        if (!anchor || !target) return;

        transform.localScale = new(thickness, thickness, Vector3.Distance(anchor.position, target.position) / 2f);
        transform.position = anchor.position;
        transform.LookAt(target);
    }
}
