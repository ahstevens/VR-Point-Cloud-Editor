using System.IO;
using UnityEngine;

public class UserSettings : MonoBehaviour
{
    [SerializeField]
    public class Preferences
    {
        public bool autoHideGroundPlaneOnLoad = true;
        public Color backgroundColor = Color.black;
        public float cursorDeletionRate = 10f;
        public float cursorDistance = 0.25f;
        public float cursorDistanceMax = 1f;
        public float cursorDistanceMin = 0.1f;
        public float cursorRadius = 0.05f;
        public float cursorRadiusMax = 0.25f;
        public float cursorRadiusMin = 0.01f;
        public float distanceOnLoad = 0.75f;
        public int encResolution = 4096;
        public float fitSizeOnLoad = 1f;
        public string lastLoadDirectory = "";
        public string lastSaveDirectory = "";
        public float nearPlaneDistance = 0.1f;
        public bool openMenuOnStart = true;
        public float outlierDistance = 5f;
        public int outlierNeighborCount = 5;
        public bool saveCursorOnExit = true;
        public bool showGroundPlane = true;
        public bool stickyMaps = false;
        public bool stickyUI = true;
    }

    private Preferences _prefs;
    public Preferences preferences
    {
        get { return _prefs; }
    }

    private static UserSettings _instance;

    public static UserSettings instance
    {
        get { return _instance; }
    }

    private void Awake()
    {
        _instance = this;
        _prefs = LoadFromFile();
    }

    // Start is called before the first frame update
    void Start()
    {
        Camera.main.nearClipPlane = _prefs.nearPlaneDistance;
        Camera.main.backgroundColor = _prefs.backgroundColor;

        if (!_prefs.showGroundPlane)
            FindObjectOfType<PointCloudUI>().ToggleGroundPlane();

        FindObjectOfType<PointCloudUI>().ToggleAutoHideGroundPlane(_prefs.autoHideGroundPlaneOnLoad);

        GameObject.Find("OutlierDistanceSlider").GetComponent<UnityEngine.UI.Slider>().value = _prefs.outlierDistance;
        GameObject.Find("OutlierCountSlider").GetComponent<UnityEngine.UI.Slider>().value = _prefs.outlierNeighborCount;
    }

    // Update is called once per frame
    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            Application.Quit();
    }

    public Preferences LoadFromFile()
    {
        Preferences p;

        if (File.Exists(Application.dataPath + "/../userPreferences.json"))
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
        SaveToFile(_prefs);
    }

    public void SaveToFile(Preferences prefs)
    {
        File.WriteAllText(Application.dataPath + "/../userPreferences.json", JsonUtility.ToJson(prefs, true));
    }

    public void LoadFromPlayerPrefs(int slot)
    {
        _prefs.autoHideGroundPlaneOnLoad = PlayerPrefs.GetInt("autoHideGroundPlaneOnLoad" + slot) == 1;
        ColorUtility.TryParseHtmlString("#" + PlayerPrefs.GetString("backgroundColor" + slot), out _prefs.backgroundColor);
        _prefs.cursorDeletionRate = PlayerPrefs.GetFloat("cursorDeletionRate" + slot);
        _prefs.cursorDistance = PlayerPrefs.GetFloat("cursorDistance" + slot);
        _prefs.cursorDistanceMax = PlayerPrefs.GetFloat("cursorDistanceMax" + slot);
        _prefs.cursorDistanceMin = PlayerPrefs.GetFloat("cursorDistanceMin" + slot);
        _prefs.cursorRadius = PlayerPrefs.GetFloat("cursorRadius" + slot);
        _prefs.cursorRadiusMax = PlayerPrefs.GetFloat("cursorRadiusMax" + slot);
        _prefs.cursorRadiusMin = PlayerPrefs.GetFloat("cursorRadiusMin" + slot);
        _prefs.distanceOnLoad = PlayerPrefs.GetFloat("distanceOnLoad" + slot);
        _prefs.encResolution = PlayerPrefs.GetInt("encResolution" + slot);
        _prefs.fitSizeOnLoad = PlayerPrefs.GetFloat("fitSizeOnLoad" + slot);
        _prefs.lastLoadDirectory = PlayerPrefs.GetString("lastLoadDirectory" + slot);
        _prefs.lastSaveDirectory = PlayerPrefs.GetString("lastSaveDirectory" + slot);
        _prefs.nearPlaneDistance = PlayerPrefs.GetFloat("nearPlaneDistance" + slot);
        _prefs.openMenuOnStart = PlayerPrefs.GetInt("openMenuOnStart" + slot) == 1;
        _prefs.outlierDistance = PlayerPrefs.GetFloat("outlierDistance" + slot);
        _prefs.outlierNeighborCount = PlayerPrefs.GetInt("outlierNeighborCount" + slot);
        _prefs.saveCursorOnExit = PlayerPrefs.GetInt("saveCursorOnExit" + slot) == 1;
        _prefs.showGroundPlane = PlayerPrefs.GetInt("showGroundPlane" + slot) == 1;
        _prefs.stickyMaps = PlayerPrefs.GetInt("stickyMaps" + slot) == 1;
        _prefs.stickyUI = PlayerPrefs.GetInt("stickyUI" + slot) == 1;
    }

    public void SaveToPlayerPrefs(int slot)
    {
        PlayerPrefs.SetInt("autoHideGroundPlaneOnLoad" + slot, _prefs.autoHideGroundPlaneOnLoad ? 1 : 0);
        PlayerPrefs.SetString("backgroundColor" + slot, ColorUtility.ToHtmlStringRGB(_prefs.backgroundColor));
        PlayerPrefs.SetFloat("cursorDeletionRate" + slot, _prefs.cursorDeletionRate);
        PlayerPrefs.SetFloat("cursorDistance" + slot, _prefs.cursorDistance);
        PlayerPrefs.SetFloat("cursorDistanceMax" + slot, _prefs.cursorDistanceMax);
        PlayerPrefs.SetFloat("cursorDistanceMin" + slot, _prefs.cursorDistanceMin);
        PlayerPrefs.SetFloat("cursorRadius" + slot, _prefs.cursorRadius);
        PlayerPrefs.SetFloat("cursorRadiusMax" + slot, _prefs.cursorRadiusMax);
        PlayerPrefs.SetFloat("cursorRadiusMin" + slot, _prefs.cursorRadiusMin);
        PlayerPrefs.SetFloat("distanceOnLoad" + slot, _prefs.distanceOnLoad);
        PlayerPrefs.SetFloat("encResolution" + slot, _prefs.encResolution);
        PlayerPrefs.SetFloat("fitSizeOnLoad" + slot, _prefs.fitSizeOnLoad);
        PlayerPrefs.SetString("lastLoadDirectory" + slot, _prefs.lastLoadDirectory);
        PlayerPrefs.SetString("lastSaveDirectory" + slot, _prefs.lastSaveDirectory);
        PlayerPrefs.SetFloat("nearPlaneDistance" + slot, _prefs.nearPlaneDistance);
        PlayerPrefs.SetInt("openMenuOnStart" + slot, _prefs.openMenuOnStart ? 1 : 0);
        PlayerPrefs.SetFloat("outlierDistance" + slot, _prefs.outlierDistance);
        PlayerPrefs.SetInt("outlierNeighborCount" + slot, _prefs.outlierNeighborCount);
        PlayerPrefs.SetInt("saveCursorOnExit" + slot, _prefs.saveCursorOnExit ? 1 : 0);
        PlayerPrefs.SetInt("showGroundPlane" + slot, _prefs.showGroundPlane ? 1 : 0);
        PlayerPrefs.SetInt("stickyMaps" + slot, _prefs.stickyMaps ? 1 : 0);
        PlayerPrefs.SetInt("stickyUI" + slot, _prefs.stickyUI ? 1 : 0);
    }

    public static T ImportJson<T>(string path)
    {
        return JsonUtility.FromJson<T>(File.ReadAllText(path));
    }
}
