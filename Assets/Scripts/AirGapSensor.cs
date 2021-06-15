using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class AirGapSensor : MonoBehaviour
{
    private float airgapDistance = 45.72f;

    public GameObject airGapTarget;
    public GameObject water;

    GameObject measureSpherePrefab;
    GameObject measureLinePrefab;
    GameObject measureTextPrefab;

    GameObject firstMeasurePoint;
    GameObject secondMeasurePoint;
    GameObject measureLine;
    GameObject measureText;


    // Start is called before the first frame update
    void Start()
    {

        StartCoroutine(GetPORTSdata());
       

    }

    IEnumerator GetPORTSdata()
    {
        Debug.Log("Connecting to NOAA PORTS...");
        using (UnityWebRequest request = UnityWebRequest.Get("https://tidesandcurrents.noaa.gov/ports/textscreen.shtml?port=lm"))
        {
            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError) // Error
            {
                Debug.Log(request.error);
            }
            else // Success
            {
                //Debug.Log(request.downloadHandler.text);

                //separate all that text in to lines:
                string longStringFromFile = request.downloadHandler.text;

                int labelIndex = longStringFromFile.IndexOf("Crescent City Air G");
                string cut = longStringFromFile.Substring(labelIndex + 22, 10);
                int ftIndex = cut.IndexOf("ft");
                string data = cut.Substring(0, ftIndex);

                Debug.Log("data is: " + data);

                //Convert to meters
                airgapDistance = float.Parse(data) * 0.3048f;

                Debug.Log("air gap is: " + airgapDistance + " meters");

                Vector3 targetLoc = this.transform.position;
                targetLoc.y -= airgapDistance;
                airGapTarget.transform.position = targetLoc;

                //move water
                water.transform.position = new Vector3(water.transform.position.x, targetLoc.y, water.transform.position.z);

                measureSpherePrefab = Resources.Load("Prefabs/MeasureSphere", typeof(GameObject)) as GameObject;
                measureLinePrefab = Resources.Load("Prefabs/MeasureLine", typeof(GameObject)) as GameObject;
                measureTextPrefab = Resources.Load("Prefabs/MeasureText", typeof(GameObject)) as GameObject;

                firstMeasurePoint = Instantiate(measureSpherePrefab);//, new Vector3(0, 0, 0), Quaternion.identity);
                firstMeasurePoint.name = "AirGapSensorMarker ";
                firstMeasurePoint.transform.localPosition = this.transform.position;

                secondMeasurePoint = Instantiate(measureSpherePrefab);
                secondMeasurePoint.name = "AirGapSensorTargerMarker";
                secondMeasurePoint.transform.localPosition = airGapTarget.transform.position;

                measureLine = Instantiate(measureLinePrefab);
                measureLine.name = "AirGapLine";
                LineRenderer measureLineLineRenderer = measureLine.GetComponent<LineRenderer>();
                measureLineLineRenderer.SetPosition(0, firstMeasurePoint.transform.position);
                measureLineLineRenderer.SetPosition(1, secondMeasurePoint.transform.position);

                float distance = (secondMeasurePoint.transform.position - firstMeasurePoint.transform.position).magnitude;
                Vector3 midpoint = (secondMeasurePoint.transform.position + firstMeasurePoint.transform.position) / 2;
                measureText = Instantiate(measureTextPrefab, midpoint, Quaternion.identity);
                measureText.name = "AirGapText";
                measureText.transform.position = midpoint + new Vector3(0, 0.01f, 0);
                TextMeshPro thisText = measureText.GetComponent<TextMeshPro>();
                //thisText.text = (distance).ToString("0.00") + "m  / " + airgapDistance.ToString("0.00") + "m";
                thisText.text = "AirGap: " + airgapDistance.ToString("0.00") + "m (" + (airgapDistance* 3.28084f).ToString("0.00") + "ft)";




                //List<string> lines = new List<string>(longStringFromFile.Split(new string[] { "\r","\n" }, StringSplitOptions.RemoveEmptyEntries) );
                //// filter to line with air gap figure
                //lines = lines.Where(line => line.Contains("Crescent City Air G")).ToList();

                //for (int i = 0; i < lines.Count; i++)
                //    Debug.Log(lines.ElementAt(i));
            }
        }

               
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
