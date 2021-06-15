using System;
using UnityEngine;

internal class pointCloudRawDataContainer : MonoBehaviour
{
    public String pathToRawData;

    public pointCloudRawDataContainer(String pathToRawData)
    {
        this.pathToRawData = pathToRawData;
    }
}