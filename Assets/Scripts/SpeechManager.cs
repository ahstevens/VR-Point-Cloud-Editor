using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Windows.Speech;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using HSVPicker;

public class SpeechManager : MonoBehaviour
{
    private KeywordRecognizer _keywordRecognizer;
    Dictionary<string, System.Action> _keywords = new Dictionary<string, System.Action>();

    [SerializeField]
    private Text voiceCommandDisplay;

    private bool _needsToBeInitialized = true;

    public bool needsToBeInitialized
    {
        get { return _needsToBeInitialized; }
    }

    // Start is called before the first frame update
    void Start()
    {
        //PrepareBuiltinCommands();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Initialize()
    {
        _keywordRecognizer = new KeywordRecognizer(_keywords.Keys.ToArray());

        _keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_DebugLogging;

        _keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_UpdateCommandDisplay;

        _keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_OnPhraseRecognized;

        _keywordRecognizer.Start();

        _needsToBeInitialized = false;
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

        _keywords.Add("toggle floor", () =>
        {
            FindObjectOfType<PointCloudUI>().ToggleGroundPlane();
        });

        _keywords.Add("reset view", () =>
        {
            if (PointCloudManager.GetPointCloudsInScene().Length > 0)
                PointCloudManager.GetPointCloudsInScene()[0].ResetMiniature(
                    UserSettings.instance.preferences.fitSizeOnLoad,
                    UserSettings.instance.preferences.distanceOnLoad
                );
        });

        _keywords.Add("put cloud at origin", () =>
        {
            if (PointCloudManager.GetPointCloudsInScene().Length > 0)
                PointCloudManager.GetPointCloudsInScene()[0].ResetOrigin();
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
            if (PointCloudManager.GetPointCloudsInScene().Length == 0)
                PointCloudManager.LoadDemoFile();
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

    public bool AddCommand(string keyword, Action action)
    {
        if (!_keywords.ContainsKey(keyword))
        {
            _keywords.Add(keyword, action);
            _needsToBeInitialized = true;
            return true;
        }
                
        return false;        
    }

    public bool AddCommand(string keyword, string[] pretexts, Action action)
    {
        bool result = true;

        foreach (var p in pretexts)
        {
            var k = p + " " + keyword;

            if (!AddCommand(k, action))
            {
                result = false;
            }
        }

        return result;
    }

    private void KeywordRecognizer_DebugLogging(PhraseRecognizedEventArgs args)
    {
        Debug.Log($"\"{args.text}\" ({args.confidence}, {args.phraseDuration})");
    }

    private void KeywordRecognizer_UpdateCommandDisplay(PhraseRecognizedEventArgs args)
    {
        voiceCommandDisplay.text = $"\"{args.text}\"";
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
