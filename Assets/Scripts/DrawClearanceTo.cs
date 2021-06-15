using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;



public class DrawClearanceTo : MonoBehaviour
{

    public Camera targetCamera;
    public GameObject clearancePoint;
    public bool sidewaysHeightFix;

    GameObject measureSpherePrefab;
    GameObject measureLinePrefab;
    GameObject measureTextPrefab;

    GameObject firstMeasurePoint;
    GameObject secondMeasurePoint;
    GameObject measureLine;
    GameObject measureText;

    LineRenderer measureLineLineRenderer;

    private int measurePointCounter;

    // Start is called before the first frame update
    void Start()
    {
        measurePointCounter = 0;
        measureSpherePrefab = Resources.Load("Prefabs/MeasureSphere", typeof(GameObject)) as GameObject;
        measureLinePrefab = Resources.Load("Prefabs/MeasureLine", typeof(GameObject)) as GameObject;
        measureTextPrefab = Resources.Load("Prefabs/MeasureText", typeof(GameObject)) as GameObject;

        firstMeasurePoint = Instantiate(measureSpherePrefab);//, new Vector3(0, 0, 0), Quaternion.identity);
        firstMeasurePoint.name = "Measure Point " + measurePointCounter++;
        firstMeasurePoint.transform.localPosition = this.transform.position;

        secondMeasurePoint = Instantiate(measureSpherePrefab);
        secondMeasurePoint.name = "Measure Point " + measurePointCounter++;
        secondMeasurePoint.transform.localPosition = clearancePoint.transform.position;

        measureLine = Instantiate(measureLinePrefab);
        measureLine.name = "Measure Line (" + (measurePointCounter - 2) + " to " + (measurePointCounter - 1) + ")";
        measureLineLineRenderer = measureLine.GetComponent<LineRenderer>();
        measureLineLineRenderer.SetPosition(0, firstMeasurePoint.transform.position);
        measureLineLineRenderer.SetPosition(1, secondMeasurePoint.transform.position);

        float distance = (secondMeasurePoint.transform.position - firstMeasurePoint.transform.position).magnitude;
        Vector3 midpoint = (secondMeasurePoint.transform.position + firstMeasurePoint.transform.position) / 2;
        measureText = Instantiate(measureTextPrefab, midpoint, Quaternion.identity);
        measureText.name = "Measure Text (" + (measurePointCounter - 2) + " to " + (measurePointCounter - 1) + ")";
        measureText.transform.position = midpoint + new Vector3(0, 0.01f, 0);
        TextMeshPro thisText = measureText.GetComponent<TextMeshPro>();
        thisText.text = (distance).ToString("0.00") + "m";
    }

    // Update is called once per frame
    void Update()
    {
        firstMeasurePoint.transform.localPosition = this.transform.position;
        measureLine.GetComponent<LineRenderer>().SetPosition(0, firstMeasurePoint.transform.position);

        if (sidewaysHeightFix)
            secondMeasurePoint.transform.localPosition = new Vector3(secondMeasurePoint.transform.localPosition.x, firstMeasurePoint.transform.localPosition.y, secondMeasurePoint.transform.localPosition.z);

        measureLineLineRenderer.SetPosition(0, firstMeasurePoint.transform.position);
        measureLineLineRenderer.SetPosition(1, secondMeasurePoint.transform.position);

        float distance = (secondMeasurePoint.transform.position - firstMeasurePoint.transform.position).magnitude;
        Vector3 midpoint = (secondMeasurePoint.transform.position + firstMeasurePoint.transform.position) / 2;
        measureText.transform.position = midpoint + new Vector3(0, 3.0f, 0);
        measureText.GetComponent<TextMeshPro>().text = (distance).ToString("0.00") + "m";


    }
}
