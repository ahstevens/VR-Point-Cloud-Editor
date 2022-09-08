using System;
using UnityEngine;

public class LabelManager : MonoBehaviour
{
    string labelFile;

    private S57Objects _labels;

    public S57Objects labels
    {
        get { return _labels; }
    }

    private void Awake()
    {
        // find command line input file, if supplied
        string[] arguments = Environment.GetCommandLineArgs();

        labelFile = "";

        for (int i = 1; i < arguments.Length; ++i)
        {
            if (arguments[i] == "-l" && i != arguments.Length - 1)
            {
                labelFile = arguments[i + 1];
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if (labelFile == "")
            labelFile = Application.dataPath + "/../s57_codes.json";

        Load();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Load()
    {
        if (!System.IO.File.Exists(labelFile))
            return;

        try
        {
            _labels = JsonUtility.FromJson<S57Objects>(System.IO.File.ReadAllText(labelFile));
            Debug.Log("Label successfully loaded from JSON file " + labelFile);
        }
        catch (Exception e)
        {
            Debug.Log("Exception raised while loading JSON file: " + e);
        }
    }
}
