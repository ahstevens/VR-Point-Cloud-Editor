using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserSettings : MonoBehaviour
{
    [SerializeField]
    public struct Preferences
    {
        public string lastLoadDirectory;
        public string lastSaveDirectory;
        public float cursorSize;
        public float cursorMinSize;
        public float cursorMaxSize;
        public float cursorDistance;
        public float cursorMinDistance;
        public float cursorMaxDistance;
        public Color backgroundColor;
        public bool showGroundPlane;
        public bool autoHideGroundPlaneOnLoad;
        public Vector3 fitDimensionsOnLoad;
        public float distanceOnLoad;
        public int outlierNeighborCount;
        public float outlierDistance;
        public float nearPlaneDistance;
    }

    Preferences preferences;

    // Start is called before the first frame update
    void Start()
    {
        LoadFromFile();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Preferences GetPreferences()
    {
        return preferences;
    }

    public void LoadFromFile()
    {
        if (System.IO.File.Exists(Application.dataPath + "/../userPreferences.json"))
            preferences = ImportJson<Preferences>(Application.dataPath + "/../userPreferences.json");
        else
            Debug.Log("userPreferences.json is missing!");
    }

    public void SaveToFile()
    {
        System.IO.File.WriteAllText(Application.dataPath + "/../userPreferences.json", JsonUtility.ToJson(preferences, true));
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
