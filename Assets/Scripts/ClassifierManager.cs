using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ClassifierManager : MonoBehaviour
{
    [DllImport("PointCloudPlugin")]
    private static extern void AddClassificationEntry(int id, float redNormalized, float greenNormalized, float blueNormalized);

    [DllImport("PointCloudPlugin")]
    private static extern bool UpdateClassificationEntry(int id, float redNormalized, float greenNormalized, float blueNormalized);

    [DllImport("PointCloudPlugin")]
    private static extern void ClearClassificationTable();

    [SerializeField]
    TMPro.TextMeshPro currentClassifierText;

    [SerializeField]
    private GameObject classifierScrollRect;

    [SerializeField]
    private GameObject classifierScrollViewContent;

    [SerializeField]
    private GameObject classifierPanel;

    private string defaultConfFile = "classifiers.default.conf";
    private string userConfFile = "classifiers.user.conf";

    private Dictionary<int, Action> classifierActions = new();

    // Start is called before the first frame update
    void Start()
    {
        ClearClassificationTable();

        classifierActions.Clear();

        currentClassifierText.text = "";

        CheckAndGenerateDefaultConf();

        LoadAndPrepareClassifiers();

        FindObjectOfType<SpeechManager>().Initialize();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void CheckAndGenerateDefaultConf()
    {
        bool defaultConfPresent = System.IO.File.Exists(Application.dataPath + "/../" + defaultConfFile);

        if (!defaultConfPresent)
        {
            using System.IO.StreamWriter file = new(Application.dataPath + "/../" + defaultConfFile);

            string[] lines =
            {
                "# VRPCE Default Classifier File",
                "# You can modify this file by hand and save it as classifiers.user.conf to use a custom classifier set within the VRPCE software.",
                "# It is recommended that you instead use the classifier file generator available on the VisLab website: ",
                "# https://ccom.unh.edu/vislab/tools/point_cloud_editor/classifier_generator.html",
                "# Any supplied voice commands will be prepended with the following wake words in addition to working on their own:",
                "# \"set classifier <command>\"",
                "# \"set classification <command>\"",
                "# \"change classifier <command>\"",
                "# \"change classification <command>\"",
                "id,label,r,g,b,commands",
                "0,\"Unclassified\",0,0,0"
            };

            foreach(var line in lines)
            {
                file.WriteLine(line);
            }
        }
    }

    private void LoadAndPrepareClassifiers()
    {        
        bool userConfPresent = System.IO.File.Exists(Application.dataPath + "/../" + userConfFile);

        var reader = new System.IO.StreamReader(Application.dataPath + "/../" + (userConfPresent ? userConfFile : defaultConfFile));

        int lineNum = 0;

        // skip beginning comments
        string curLine;
        do
        {
            curLine = reader.ReadLine();
            lineNum++;
        }
        while (curLine.Trim()[0].Equals('#'));

        var header = curLine;

        if (header.Length == 0 || !header.Equals("id,label,r,g,b,commands"))
        {
            Debug.Log("Classifier configuration file is malformed. Aborting...");
            return;
        }

        var sm = FindObjectOfType<SpeechManager>();

        var pretexts = new string[] {
            "set classifier",
            "set classification",
            "change classifier",
            "change classification"
        };

        int longestLabel = 0;

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            lineNum++;

            try
            {
                // skip comment lines
                if (line.Trim()[0].Equals('#')) { continue; }

                var classifierID = Convert.ToInt32(line.Split(',')[0]);

                var splitByQuote = line.Split('"');

                var label = splitByQuote[1].ToLower();

                var rgb = splitByQuote[2];
                ushort red = Convert.ToUInt16(rgb.Split(",", System.StringSplitOptions.RemoveEmptyEntries)[0]);
                ushort green = Convert.ToUInt16(rgb.Split(",", System.StringSplitOptions.RemoveEmptyEntries)[1]);
                ushort blue = Convert.ToUInt16(rgb.Split(",", System.StringSplitOptions.RemoveEmptyEntries)[2]);

                var classColor = new Color(red / 255f, green / 255f, blue / 255f);

                var modifyPointsScript = FindObjectOfType<ModifyPoints>();

                AddClassificationEntry(classifierID, red / 255f, green / 255f, blue / 255f);

                AddButtonToUI(classifierID, label, classColor);

                int labelLen = (new String($"{classifierID}: {label}")).Length;

                if (labelLen > longestLabel) longestLabel = labelLen;

                Action a = () =>
                {
                    modifyPointsScript.SetModificationClassifier(classifierID, classColor);
                    currentClassifierText.text = $"{classifierID}: {label}";
                    currentClassifierText.color = classColor;
                };

                classifierActions.Add(classifierID, a);

                var keys = new List<string>{ label };

                for (int i = 3; i < splitByQuote.Length - 1; i++)
                {
                    if (splitByQuote[i] != ",")
                        keys.Add(splitByQuote[i].ToLower());
                }

                foreach (var key in keys)
                {

                    // key is added here
                    bool success = sm.AddCommand(key, pretexts, a);

                    if (!success)
                    {
                        Debug.Log($"Line {lineNum}: Keyword {key} already exists! Skipping...");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("ERROR Loading line " + lineNum + ":");
                Debug.Log(line);
                Debug.LogException(e);
            }
        }

        var labelwidth = longestLabel * 10;

        var newSize = classifierScrollRect.GetComponent<RectTransform>().sizeDelta;
        newSize.x = labelwidth;
        classifierScrollRect.GetComponent<RectTransform>().sizeDelta = newSize;

        newSize = classifierPanel.GetComponent<RectTransform>().sizeDelta;
        newSize.x = labelwidth;
        classifierPanel.GetComponent<RectTransform>().sizeDelta = newSize;

        reader.Close();
    }

    void AddButtonToUI(int id, string label, Color color)
    {
        var button = DefaultControls.CreateButton(new DefaultControls.Resources());
        button.layer = LayerMask.NameToLayer("UI");
        button.name = label;
        button.GetComponentInChildren<Text>().text = $"{id}: {label}";
        button.GetComponentInChildren<Text>().color = new Color(1f - color.r, 1f - color.g, 1f - color.b, 1f);
        var oldColors = button.GetComponent<Button>().colors;
        oldColors.normalColor = color;
        button.GetComponent<Button>().colors = oldColors;
        button.transform.SetParent(classifierScrollViewContent.transform, false);
        var newSize = classifierScrollViewContent.GetComponent<RectTransform>().sizeDelta;
        newSize.y += 30;
        classifierScrollViewContent.GetComponent<RectTransform>().sizeDelta = newSize;

        button.GetComponent<Button>().onClick.AddListener(() =>
        {
            FindObjectOfType<ModifyPoints>().SetModificationClassifier(id, color);
            FindObjectOfType<PointCloudUI>().CloseMenu();
            currentClassifierText.text = $"{id}: {label}";
            currentClassifierText.color = color;            
        });
    }
}
