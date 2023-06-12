using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScalingIllustrator : MonoBehaviour
{
    [SerializeField] private GameObject LeftHandController;
    [SerializeField] private GameObject RightHandController;

    public GameObject scalePointPrefab;
    public GameObject connectorPrefab;

    private GameObject scalingPointObject;
    private GameObject scaleRod;
    private GameObject leftConnector;
    private GameObject rightConnector;

    [SerializeField] private XRScaling scalingScript;

    // Start is called before the first frame update
    void Start()
    {
        if (scalingScript == null)
            scalingScript = FindObjectOfType<XRScaling>();

        scalingPointObject = Instantiate(scalePointPrefab);
        scalingPointObject.SetActive(false);

        scaleRod = Instantiate(connectorPrefab);
        var scaleRodScript = scaleRod.GetComponent<CylinderConnector>();
        scaleRodScript.anchor = RightHandController.transform;
        scaleRodScript.target = LeftHandController.transform;
        scaleRodScript.thickness = 0.001f;
        scaleRodScript.dynamic = true;
        scaleRod.SetActive(false);

        scalingScript.PreScalingBegin += ShowScalingMarker;
        scalingScript.PreScalingEnd += HideScalingMarker;
        scalingScript.ScalingBegin += OnScalingBegin;
        scalingScript.ScalingEnd += OnScalingEnd;
    }

    // Update is called once per frame
    void Update()
    {
        if (scalingPointObject.activeSelf)
        {
            scalingPointObject.transform.position = scalingScript.GetActiveScalingPoint();
        }

    }

    private void ShowScalingMarker()
    {
        scalingPointObject.SetActive(true);
    }

    private void HideScalingMarker()
    {

        scalingPointObject.SetActive(false);

        DestroyConnectingLines();
    }

    private void OnScalingBegin()
    {
        scaleRod.SetActive(true);
    }

    private void OnScalingEnd()
    {
        scaleRod.SetActive(false);
    }

    private void CreateConnectingLinesToControllers(Transform connectedObject)
    {
        leftConnector = Instantiate(connectorPrefab);
        var leftConnectorScript = leftConnector.GetComponent<CylinderConnector>();
        leftConnectorScript.target = connectedObject;
        leftConnectorScript.anchor = LeftHandController.transform;
        leftConnectorScript.thickness = 0.001f;
        leftConnectorScript.dynamic = true;

        rightConnector = Instantiate(connectorPrefab);
        var rightConnectorScript = rightConnector.GetComponent<CylinderConnector>();
        rightConnectorScript.target = connectedObject;
        rightConnectorScript.anchor = RightHandController.transform;
        rightConnectorScript.thickness = 0.001f;
        rightConnectorScript.dynamic = true;
    }

    private void DestroyConnectingLines()
    {
        if (leftConnector) Destroy(leftConnector);
        if (rightConnector) Destroy(rightConnector);
    }
}
