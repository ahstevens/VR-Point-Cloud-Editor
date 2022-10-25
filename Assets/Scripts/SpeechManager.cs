using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using UnityEngine.Windows.Speech;

public class SpeechManager : MonoBehaviour
{
    private KeywordRecognizer _keywordRecognizer;
    Dictionary<string, System.Action> _keywords = new Dictionary<string, System.Action>();

    [SerializeField]
    private UnityEngine.UI.Text commandDisplay;

    // Start is called before the first frame update
    void Start()
    {
        PrepareBuiltinCommands();

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
    }

    private void LoadAndPrepareClassifiers()
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
