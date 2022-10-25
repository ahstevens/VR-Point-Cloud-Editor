using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class S57Object
{
    public int id;
    public string code;
    public string desc;    
    public List<string> keywords;
}

[System.Serializable]
public class S57Objects
{
    public S57Object[] s57objects;
}