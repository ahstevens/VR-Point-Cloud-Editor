using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatWithDraftValue : MonoBehaviour
{
    public float draftValue;
    public GameObject water;

    // Start is called before the first frame update
    void Start()
    {
        Vector3 pos = this.transform.position;
        pos.y = water.transform.position.y + draftValue;
        this.transform.position = pos;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 pos = this.transform.position;
        pos.y = water.transform.position.y + draftValue;
        this.transform.position = pos;
    }
}
