using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GEOReference : MonoBehaviour
{
    public double refX = 0.0;
    public double refY = 0.0;

    public float scale = 1.0f;

    public void setReferenceX(double x)
    {
        refX = x;
    }

    public void setReferenceY(double y)
    {
        refY = y;
    }

    public void setReferenceLongitude(double longitude)
    {
        refX = longitude;
    }

    public void setReferenceLatitude(double latitude)
    {
        refY = latitude;
    }

    public void setReferenceEasting(double easting)
    {
        refX = easting;
    }

    public void setReferenceNorthing(double northing)
    {
        refY = northing;
    }
}
