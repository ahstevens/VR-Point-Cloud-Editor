using System;
using UnityEngine;
using UnityEngine.UI;

public class CommandLineDisplaySelector : MonoBehaviour
{
    static string cmdInfo = "";

    public Text buildInfo;

    void Start()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        foreach (string arg in arguments)
        {
            cmdInfo += arg.ToString() + "\n ";
            buildInfo.text = arg.ToString();
        }
    }

    void OnGUI()
    {
        Rect r = new Rect(5, 5, 800, 500);
        GUI.Label(r, cmdInfo);
    }
}