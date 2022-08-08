using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserSettings : MonoBehaviour
{
    [SerializeField]
    public class Preferences
    {
        public string lastLoadDirectory = Application.dataPath;
        public string lastSaveDirectory = Application.dataPath;
        public float cursorSize = 0.1f;
        public float cursorMinSize = 0.01f;
        public float cursorMaxSize = 0.5f;
        public float cursorDistance = 0.25f;
        public float cursorMinDistance = 0.1f;
        public float cursorMaxDistance = 2f;
        public Color backgroundColor = Color.black;
        public bool showGroundPlane = true;
        public bool autoHideGroundPlaneOnLoad = true;
        public Vector3 fitDimensionsOnLoad = Vector3.one;
        public float distanceOnLoad = 0.75f;
        public int outlierNeighborCount = 5;
        public float outlierDistance = 5f;
        public float nearPlaneDistance = 0.01f;
    }

    Preferences preferences;

    private static UserSettings _instance;

    public static UserSettings instance
    {
        get { return _instance; }
    }

    private void Awake()
    {
        _instance = this;
        preferences = LoadFromFile();
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Preferences GetPreferences()
    {
        return preferences;
    }

    public Preferences LoadFromFile()
    {
        Preferences p;

        if (System.IO.File.Exists(Application.dataPath + "/../userPreferences.json"))
        {
            Debug.Log("Found userPreferences.json; Loading values from file...");
            p = ImportJson<Preferences>(Application.dataPath + "/../userPreferences.json");
        }
        else
        {
            Debug.Log("userPreferences.json is missing! Loading defaults...");
            p = new Preferences();
            SaveToFile(p);
        }

        return p;
    }

    public void SaveToFile()
    {
        SaveToFile(preferences);
    }

    public void SaveToFile(Preferences prefs)
    {
        System.IO.File.WriteAllText(Application.dataPath + "/../userPreferences.json", JsonUtility.ToJson(prefs, true));
    }

    public void LoadFromPlayerPrefs(int slot)
    {
        preferences.backgroundColor = Color.red;
        preferences.lastLoadDirectory = PlayerPrefs.GetString("lastLoadDirectory" + slot);
        preferences.lastSaveDirectory = PlayerPrefs.GetString("lastSaveDirectory" + slot);
        preferences.cursorSize = PlayerPrefs.GetFloat("" + slot);
        preferences.cursorMinSize = PlayerPrefs.GetFloat("" + slot);
        preferences.cursorMaxSize = PlayerPrefs.GetFloat("" + slot);
        preferences.cursorDistance = PlayerPrefs.GetFloat("" + slot);
        preferences.cursorMinDistance = PlayerPrefs.GetFloat("" + slot);
        preferences.cursorMaxDistance = PlayerPrefs.GetFloat("" + slot);
        ColorUtility.TryParseHtmlString("#" + PlayerPrefs.GetString("backgroundColor" + slot), out preferences.backgroundColor);
        preferences.showGroundPlane = PlayerPrefs.GetInt("showGroundPlane" + slot) == 1;
        preferences.autoHideGroundPlaneOnLoad = PlayerPrefs.GetInt("autoHideGroundPlaneOnLoad" + slot) == 1;
        preferences.fitDimensionsOnLoad.x = PlayerPrefs.GetFloat("fitXDimensionOnLoad" + slot);
        preferences.fitDimensionsOnLoad.y = PlayerPrefs.GetFloat("fitYDimensionOnLoad" + slot);
        preferences.fitDimensionsOnLoad.z = PlayerPrefs.GetFloat("fitZDimensionOnLoad" + slot);
        preferences.distanceOnLoad = PlayerPrefs.GetFloat("distanceOnLoad" + slot);
        preferences.outlierNeighborCount = PlayerPrefs.GetInt("outlierNeighborCount" + slot);
        preferences.outlierDistance = PlayerPrefs.GetFloat("outlierDistance" + slot);
        preferences.nearPlaneDistance = PlayerPrefs.GetFloat("nearPlaneDistance" + slot);
    }

    public void SaveToPlayerPrefs(int slot)
    {
        PlayerPrefs.SetString("lastLoadDirectory" + slot, preferences.lastLoadDirectory);
        PlayerPrefs.SetString("lastSaveDirectory" + slot, preferences.lastSaveDirectory);
        PlayerPrefs.SetFloat("cursorsize" + slot, preferences.cursorSize);
        PlayerPrefs.SetFloat("cursorMinSize" + slot, preferences.cursorMinSize);
        PlayerPrefs.SetFloat("cursorMaxSize" + slot, preferences.cursorMaxSize);
        PlayerPrefs.SetFloat("cursorDistance" + slot, preferences.cursorDistance);
        PlayerPrefs.SetFloat("cursorMinDistance" + slot, preferences.cursorMinDistance);
        PlayerPrefs.SetFloat("cursorMaxDistance" + slot, preferences.cursorMaxDistance);
        PlayerPrefs.SetString("backgroundColor" + slot, ColorUtility.ToHtmlStringRGB(preferences.backgroundColor));
        PlayerPrefs.SetInt("showGroundPlane" + slot, preferences.showGroundPlane ? 1 : 0);
        PlayerPrefs.SetInt("autoHideGroundPlaneOnLoad" + slot, preferences.autoHideGroundPlaneOnLoad ? 1 : 0);
        PlayerPrefs.SetFloat("fitXDimensionOnLoad" + slot, preferences.fitDimensionsOnLoad.x);
        PlayerPrefs.SetFloat("fitYDimensionOnLoad" + slot, preferences.fitDimensionsOnLoad.y);
        PlayerPrefs.SetFloat("fitZDimensionOnLoad" + slot, preferences.fitDimensionsOnLoad.z);
        PlayerPrefs.SetFloat("distanceOnLoad" + slot, preferences.distanceOnLoad);
        PlayerPrefs.SetInt("outlierNeighborCount" + slot, preferences.outlierNeighborCount);
        PlayerPrefs.SetFloat("outlierDistance" + slot, preferences.outlierDistance);
        PlayerPrefs.SetFloat("nearPlaneDistance" + slot, preferences.nearPlaneDistance);
    }

    public static T ImportJson<T>(string path)
    {
        return JsonUtility.FromJson<T>(System.IO.File.ReadAllText(path));
    }
}
