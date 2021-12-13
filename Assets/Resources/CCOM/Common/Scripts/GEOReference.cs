using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GEOReference : MonoBehaviour
{
    public double realWorldX = 0.0;
    public double realWorldZ = 0.0;

    public int UTMZone = 0;

    public float scale = 1.0f;
    public float maxDepth = 0.0f;

    public void setRealWorldX(double newValue)
    {
        realWorldX = newValue;
    }

    public void setRealWorldZ(double newValue)
    {
        realWorldZ = newValue;
    }

    void Start()
    {
    }

    void Update()
    {
    }

    void OnValidate()
    {
        
    }
}
