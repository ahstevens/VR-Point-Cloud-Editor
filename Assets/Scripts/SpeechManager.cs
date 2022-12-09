using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Windows.Speech;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SpeechManager : MonoBehaviour
{
    [DllImport("PointCloudPlugin")]
    private static extern void AddClassificationEntry(int id, float redNormalized, float greenNormalized, float blueNormalized);

    [DllImport("PointCloudPlugin")]
    private static extern bool UpdateClassificationEntry(int id, float redNormalized, float greenNormalized, float blueNormalized);

    [DllImport("PointCloudPlugin")]
    private static extern void ClearClassificationTable();

    private KeywordRecognizer _keywordRecognizer;
    Dictionary<string, System.Action> _keywords = new Dictionary<string, System.Action>();

    [SerializeField]
    private UnityEngine.UI.Text commandDisplay;

    [SerializeField]
    private GameObject classifierScrollViewContent;

    // Start is called before the first frame update
    void Start()
    {
        PrepareBuiltinCommands();

        ClearClassificationTable();

        LoadAndPrepareClassifiers();

        //keywordRecognizer = new KeywordRecognizer(keywords.Keys.ToArray());
        _keywordRecognizer = new KeywordRecognizer(_keywords.Keys.ToArray());

        _keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_DebugLogging;

        _keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_UpdateCommandDisplay;

        _keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_OnPhraseRecognized;

        _keywordRecognizer.Start();
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.xKey.wasPressedThisFrame)
            AddClassificationEntry(155, 1f, 1f, 0f);
    }

    private void PrepareBuiltinCommands()
    {
        _keywords.Add("background black", () =>
        {
            Camera.main.backgroundColor = Color.black;
        });

        _keywords.Add("background gray", () =>
        {
            Camera.main.backgroundColor = Color.gray;
        });

        _keywords.Add("background white", () =>
        {
            Camera.main.backgroundColor = Color.white;
        });

        _keywords.Add("background color", () =>
        {
            FindObjectOfType<PointCloudUI>().ToggleColorPicker();
        });

        _keywords.Add("background choose", () =>
        {
            FindObjectOfType<PointCloudUI>().ToggleColorPicker();
        });

        _keywords.Add("toggle floor", () =>
        {
            FindObjectOfType<PointCloudUI>().ToggleGroundPlane();
        });

        _keywords.Add("reset view", () =>
        {
            if (pointCloudManager.GetPointCloudsInScene().Length > 0)
                pointCloudManager.GetPointCloudsInScene()[0].ResetMiniature(
                    UserSettings.instance.preferences.fitSizeOnLoad,
                    UserSettings.instance.preferences.distanceOnLoad
                );
        });

        _keywords.Add("put cloud at origin", () =>
        {
            if (pointCloudManager.GetPointCloudsInScene().Length > 0)
                pointCloudManager.GetPointCloudsInScene()[0].ResetOrigin();
        });

        _keywords.Add("computer, enhance", () =>
        {
            if (pointCloudManager.GetPointCloudsInScene().Length > 0)
                pointCloudManager.GetPointCloudsInScene()[0].ResetMiniature(
                    UserSettings.instance.preferences.fitSizeOnLoad * 2f,
                    UserSettings.instance.preferences.distanceOnLoad
                );            
        });

        _keywords.Add("detect outliers", () =>
        {
            FindObjectOfType<PointCloudUI>().HighlightOutliers();
        });

        _keywords.Add("show outliers", () =>
        {
            FindObjectOfType<PointCloudUI>().HighlightOutliers();
        });

        _keywords.Add("detect noise", () =>
        {
            FindObjectOfType<PointCloudUI>().HighlightOutliers();
        });

        _keywords.Add("show noise", () =>
        {
            FindObjectOfType<PointCloudUI>().HighlightOutliers();
        });

        _keywords.Add("remove outliers", () =>
        {
            FindObjectOfType<PointCloudUI>().DeleteOutliers();
        });

        _keywords.Add("delete outliers", () =>
        {
            FindObjectOfType<PointCloudUI>().DeleteOutliers();
        });

        _keywords.Add("remove noise", () =>
        {
            FindObjectOfType<PointCloudUI>().DeleteOutliers();
        });

        _keywords.Add("delete noise", () =>
        {
            FindObjectOfType<PointCloudUI>().DeleteOutliers();
        });

        _keywords.Add("load demo file", () =>
        {
            if (pointCloudManager.GetPointCloudsInScene().Length == 0)
                pointCloudManager.LoadDemoFile();
        });

        _keywords.Add("show RGB", () =>
        {
            FindObjectOfType<ModifyPoints>().ActivateClassificationMode(false);
        });

        _keywords.Add("show classifiers", () =>
        {
            FindObjectOfType<ModifyPoints>().ActivateClassificationMode(true);
        });
    }

    private void LoadAndPrepareOldClassifiers()
    {
        var reader = new System.IO.StreamReader(Application.dataPath + "/../classification_keywords.conf");
        int lineNum = 1;

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            lineNum++;

            try
            {
                var value = line.Split(',')[0];
                var key = line.Split('"')[1].ToLower();
                var pretexts = new string[] { 
                    "set classifier",
                    "set classification",
                    "change classifier",
                    "change classification"
                };

                foreach (var p in pretexts )
                {
                    var keyword = p + " " + key;

                    if (!_keywords.ContainsKey(keyword))
                    {
                        _keywords.Add(keyword, () =>
                        {
                            Debug.Log("COMMAND: " + p);
                            Debug.Log("KEY: " + key);
                            Debug.Log("VALUE: " + value);
                        });
                    }
                    else
                    {
                        Debug.Log($"Line {lineNum}: Keyword {key} already exists! Skipping...");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.Log("ERROR Loading line " + lineNum + ":");
                Debug.Log(line);
                Debug.LogException(e);
            }
        }

        reader.Close();
    }

    private void LoadAndPrepareClassifiers()
    {
        var reader = new System.IO.StreamReader(Application.dataPath + "/../classifiers.conf");

        var header = reader.ReadLine();

        if (header.Length == 0 || !header.Equals("id,label,r,g,b,commands"))
        {
            Debug.Log("Classifier configuration file is malformed. Aborting...");
            return;
        }

        int lineNum = 1;

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            lineNum++;

            try
            {
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

                var button = DefaultControls.CreateButton(new DefaultControls.Resources());
                button.layer = LayerMask.NameToLayer("UI");
                button.name = label;
                button.GetComponentInChildren<Text>().text = label;
                var oldColors = button.GetComponent<Button>().colors;
                oldColors.normalColor = classColor;
                button.GetComponent<Button>().colors = oldColors;
                button.transform.SetParent(classifierScrollViewContent.transform, false);
                var newSize = classifierScrollViewContent.GetComponent<RectTransform>().sizeDelta;
                newSize.y += 30;
                classifierScrollViewContent.GetComponent<RectTransform>().sizeDelta = newSize;

                button.GetComponent<Button>().onClick.AddListener(() =>
                {
                    modifyPointsScript.SetModificationClassifier(classifierID, classColor);
                    Debug.Log("Classifier " + classifierID + " selected");
                });

                var pretexts = new string[] {
                    "set classifier",
                    "set classification",
                    "change classifier",
                    "change classification"
                };

                var keys = new List<string>();
                keys.Add(label);

                for (int i = 3; i < splitByQuote.Length - 1; i++)
                {
                    if (splitByQuote[i] != ",")
                        keys.Add(splitByQuote[i].ToLower());
                }

                foreach (var p in pretexts)
                {
                    foreach (var key in keys)
                    {
                        var keyword = p + " " + key;

                        Debug.Log("COMMAND: " + p);
                        Debug.Log("KEY: " + key);
                        Debug.Log("KEYWORD: " + keyword);
                        Debug.Log("VALUE: " + classifierID);
                        Debug.Log("COLOR: " + red + ", " + green + ", " + blue);

                        if (!_keywords.ContainsKey(keyword))
                        {                      
                            _keywords.Add(keyword, () =>
                            {
                                // This lambda is where you add the keyword actions
                                Debug.Log("COMMAND: " + p);
                                Debug.Log("KEY: " + key);
                                Debug.Log("VALUE: " + classifierID);
                                Debug.Log("COLOR: " + red + ", " + green + ", " + blue);

                                FindObjectOfType<ModifyPoints>().SetModificationClassifier(classifierID, classColor);
                            });
                        }
                        else
                        {
                            Debug.Log($"Line {lineNum}: Keyword {key} already exists! Skipping...");
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.Log("ERROR Loading line " + lineNum + ":");
                Debug.Log(line);
                Debug.LogException(e);
            }
        }

        reader.Close();
    }

    private void KeywordRecognizer_DebugLogging(PhraseRecognizedEventArgs args)
    {
        Debug.Log($"\"{args.text}\" ({args.confidence}, {args.phraseDuration})");
    }

    private void KeywordRecognizer_UpdateCommandDisplay(PhraseRecognizedEventArgs args)
    {
        commandDisplay.text = $"\"{args.text}\"";
    }

    private void KeywordRecognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        System.Action keywordAction;

        // if the keyword recognized is in our dictionary, call that Action.
        if (_keywords.TryGetValue(args.text, out keywordAction))
        {
            keywordAction.Invoke();
        }
    }
}
