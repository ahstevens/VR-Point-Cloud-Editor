using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Connector : MonoBehaviour
{
    public GameObject connectedObject;    
    public GameObject connector;

    // Start is called before the first frame update
    void Start()
    {
        connector.transform.parent = this.transform;
        connector.transform.localRotation = Quaternion.Euler(90, 0, 0);
        connector.transform.localScale = new Vector3(0.001f, 0.01f, 0.001f);
    }

    // Update is called once per frame
    void Update()
    {
        var distanceToEdge = (connectedObject.transform.position - this.transform.position).magnitude - (connectedObject.transform.localScale.x);
        var midpoint = this.transform.position + (connectedObject.transform.position - this.transform.position) * (distanceToEdge * 0.5f);

        connector.transform.localPosition = Vector3.forward * (connectedObject.transform.localPosition.z - connectedObject.transform.localScale.x) * 0.5f;
        connector.transform.localScale = new Vector3(connector.transform.localScale.x, (connectedObject.transform.localPosition.z - connectedObject.transform.localScale.x) * 0.5f, connector.transform.localScale.z);
    }
}
