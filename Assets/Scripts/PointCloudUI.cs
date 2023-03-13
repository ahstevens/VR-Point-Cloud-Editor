using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using SimpleFileBrowser;
using HSVPicker;

public class PointCloudUI : MonoBehaviour
{
    [SerializeField] private InputActionReference openMenu;

    [SerializeField] private GameObject mainUIPanel;

    // UI Panels
    [SerializeField] private GameObject loadMenuPanel;
    [SerializeField] private GameObject saveMenuPanel;
    [SerializeField] private GameObject backingMenuPanel;
    [SerializeField] private GameObject outlierMenuPanel;
    [SerializeField] private GameObject classifiersMenuPanel;
    [SerializeField] private GameObject colorMenuPanel;

    [SerializeField] private GameObject fileBrowserPanel;

    // Load Panel Objects
    [SerializeField] private Button loadButton;
    [SerializeField] private TMPro.TextMeshProUGUI loadButtonText;
    [SerializeField] private TMPro.TextMeshProUGUI buildInfoText;
    [SerializeField] private TMPro.TextMeshProUGUI showGroundPlaneButtonText;
    [SerializeField] private Toggle autoHideGroundPlaneToggle;

    // Main Menu Panel Objects
    [SerializeField] private Button saveButton;
    [SerializeField] private TMPro.TextMeshProUGUI saveButtonText;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMPro.TextMeshProUGUI closeButtonText;
    [SerializeField] private TMPro.TextMeshProUGUI backingOptionsText;
    [SerializeField] private Button resetPointCloudTransforms;
    [SerializeField] private Button refreshENCButton;
    [SerializeField] private TMPro.TextMeshProUGUI refreshENCButtonText;
    [SerializeField] private Button classifyPointsButton;
    [SerializeField] private TMPro.TextMeshProUGUI classifyPointsButtonText;
    [SerializeField] private Button classifierMenuButton;

    // Outlier Menu Panel Objects
    [SerializeField] private Slider outlierDistanceSlider;
    [SerializeField] private TMPro.TextMeshProUGUI outlierDistValText;
    [SerializeField] private Slider outlierNeighborSlider;
    [SerializeField] private TMPro.TextMeshProUGUI outlierNeighborValText;
    [SerializeField] private TMPro.TextMeshProUGUI showOutliersButtonText;

    // Backing Menu Panel Objects
    //[SerializeField] private Slider backingScaleSlider;
    //[SerializeField] private Slider backingGraduationSlider;
    //[SerializeField] private Slider backingLineThicknessSlider;


    [SerializeField] private ColorPicker colorPicker;
    [SerializeField] private GameObject groundPlane;
    [SerializeField] private GameObject editingCursor;
    [SerializeField] private GameObject connector;

    public UnityEngine.XR.Interaction.Toolkit.XRInteractorLineVisual pointer;

    [SerializeField] private Material pointCloudBackingMaterial;

    private bool _menuOpen; 
    
    public bool MenuOpen
    {
        get { return _menuOpen; }
    }

    private bool loading;
    private bool saving;
    private bool showingOutliers;
    private bool refreshingENC;

    private string lastSaveDirectory;
    private string lastLoadDirectory;

    // Start is called before the first frame update
    void Start()
    {
        openMenu.action.started += ctx => OpenMenuAction();
        openMenu.action.canceled += ctx => CloseMenuAction();

        _menuOpen = false;

        mainUIPanel.SetActive(false);
        pointer.enabled = false;

        buildInfoText.text = Application.version;

        loading = false;
        saving = false;
        refreshingENC = false;
        showingOutliers = false;

        lastLoadDirectory = lastSaveDirectory = null;

        SetInitialValues();

        if (PointCloudManager.commandLineMode)
        {
            loadButtonText.text = "Revert";
            saveButtonText.text = "Save";
            closeButtonText.text = "Quit";

            loading = true;
        }

        if (UserSettings.instance.preferences.openMenuOnStart)
            OpenMenu();
    }

    // Update is called once per frame
    void Update()
    {        
        if (loading)
        {
            if (PointCloudManager.isWaitingToLoad)
            {
                loadButton.interactable = false;
                loadButtonText.fontSize = 36;

                int numberOfElipsis = 0 + (int)(4f * (Time.timeSinceLevelLoad % 1f));
                loadButtonText.text = "Loading" + new string('.', numberOfElipsis) + new string(' ', 3 - numberOfElipsis);
            }
            else // just finished loading a file
            {
                bool pointCloudLoaded = PointCloudManager.GetPointCloudsInScene().Length > 0;

                loadButton.interactable = !pointCloudLoaded;
                loadButtonText.fontSize = 60;
                loadButtonText.text = (PointCloudManager.commandLineMode && pointCloudLoaded) ? "Revert" : "Load";

                loading = false;

                refreshingENC = pointCloudLoaded;

                saveMenuPanel.SetActive(pointCloudLoaded);

                if (pointCloudLoaded && groundPlane.activeSelf && autoHideGroundPlaneToggle.isOn)
                {
                    ToggleGroundPlane();
                }
            }
        }

        if (refreshingENC)
        {
            if (FindObjectOfType<MapManager>().refreshing)
            {
                refreshENCButtonText.text = "Refreshing...";
                refreshENCButton.interactable = false;
            }
            else
            {
                if (!PointCloudManager.GetPointCloudsInScene()[0].validEPSG)
                {
                    refreshENCButtonText.text = "No Valid ENC";
                    refreshENCButton.interactable = false;
                }
                else
                {
                    refreshENCButtonText.text = "Refresh ENC";
                    refreshENCButton.interactable = true;
                }

                refreshingENC = false;
            }
        }

        if (PointCloudManager.commandLineMode && saving && PointCloudManager.IsLastAsyncSaveFinished())
            Application.Quit();

        BlockUnloadWhileSaving();
    }

    private void OnEnable()
    {
        openMenu.action.Enable();        
    }

    private void OnDisable()
    {
        openMenu.action.Disable();
    }

    private void OpenMenuAction()
    {
        if (UserSettings.instance.preferences.stickyUI && mainUIPanel.activeSelf)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    private void CloseMenuAction()
    {
        if (!UserSettings.instance.preferences.stickyUI)
        {
            CloseMenu();
        }
    }

    private void OpenMenu()
    {
        _menuOpen = true;

        mainUIPanel.SetActive(true);
        loadMenuPanel.SetActive(true);

        var pointCloudsLoaded = PointCloudManager.GetPointCloudsInScene().Length > 0;

        if (pointCloudsLoaded)
            saveMenuPanel.SetActive(true);

        loadButton.interactable = !pointCloudsLoaded || PointCloudManager.commandLineMode;

        FindObjectOfType<ModifyPoints>().SetBrushVisibility(false);

        pointer.enabled = true;
    }

    public void CloseMenu()
    {
        _menuOpen = false;

        mainUIPanel.SetActive(false);
        HideNonPersistentMenus();

        fileBrowserPanel.SetActive(false);

        if (PointCloudManager.GetPointCloudsInScene().Length > 0)
            FindObjectOfType<ModifyPoints>().SetBrushVisibility(true);

        pointer.enabled = false;        
    }

    public void LoadFile()
    {
        if (PointCloudManager.commandLineMode) // this resets the point cloud in command line mode
        {
            UnloadFile();
            LoadFile(PointCloudManager.commandLineInputFile);
        }
        else
        {
            fileBrowserPanel.SetActive(true);

            loadMenuPanel.SetActive(false);
            HideNonPersistentMenus();

            FileBrowser.SetFilters(true, new FileBrowser.Filter("Point Clouds", ".las", ".laz", ".npz"));
            FileBrowser.SetDefaultFilter(".las");
            FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");
            FileBrowser.AddQuickLink("Project Data", Application.dataPath, null);

            if (UserSettings.instance.preferences.lastLoadDirectory != "")
                FileBrowser.AddQuickLink("Last Load", UserSettings.instance.preferences.lastLoadDirectory, null);

            if (UserSettings.instance.preferences.lastSaveDirectory != "")
                FileBrowser.AddQuickLink("Last Save", UserSettings.instance.preferences.lastSaveDirectory, null);

            StartCoroutine(ShowLoadDialogCoroutine());
        }
    }

    IEnumerator ShowLoadDialogCoroutine()
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, lastLoadDirectory == null ? Application.dataPath : lastLoadDirectory, null, "Load Point Cloud", "Load");

        if (FileBrowser.Success)
        {
            LoadFile(FileBrowser.Result[0]);

            lastLoadDirectory = Path.GetDirectoryName(FileBrowser.Result[0]);
            UserSettings.instance.preferences.lastLoadDirectory = lastLoadDirectory;
            UserSettings.instance.SaveToFile();
        }

        loadMenuPanel.SetActive(true);
    }

    public bool LoadFile(string filePath)
    {
        loading = PointCloudManager.LoadLAZFile(filePath);

        return loading;
    }

    public void SaveFile()
    {
        if (PointCloudManager.commandLineMode)
        {
            if (PointCloudManager.commandLineOutputFile != "")
                PointCloudManager.SaveLAZFile(PointCloudManager.commandLineOutputFile, PointCloudManager.GetPointCloudsInScene()[0].ID);
            else
                PointCloudManager.SaveLAZFile(PointCloudManager.commandLineInputFile, PointCloudManager.GetPointCloudsInScene()[0].ID);

            saving = true;
        }
        else
        {
            fileBrowserPanel.SetActive(true);

            loadMenuPanel.SetActive(false);
            HideNonPersistentMenus();

            FileBrowser.SetFilters(true, new FileBrowser.Filter("Point Clouds", ".las", ".laz", ".npz"));
            FileBrowser.SetDefaultFilter(".las");
            FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");
            FileBrowser.AddQuickLink("Project Data", Application.dataPath, null);

            if (UserSettings.instance.preferences.lastLoadDirectory != "")
                FileBrowser.AddQuickLink("Last Load", UserSettings.instance.preferences.lastLoadDirectory, null);

            if (UserSettings.instance.preferences.lastSaveDirectory != "")
                FileBrowser.AddQuickLink("Last Save", UserSettings.instance.preferences.lastSaveDirectory, null);

            //StartCoroutine(ShowSaveDialogCoroutine(dd.options[dd.value].text + "_edit.laz"));
            var pc = PointCloudManager.GetPointCloudsInScene()[0];
            StartCoroutine(ShowSaveDialogCoroutine(pc.name + "_edit" + (pc.pathToRawData[^3..] == "npz" ? ".npz" : ".laz")));
        }
    }

    IEnumerator ShowSaveDialogCoroutine(string filename)
    {
        yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Folders, false, lastSaveDirectory == null ? lastLoadDirectory : lastSaveDirectory, null, "Select Folder to Save " + filename, "Save");

        //Debug.Log(FileBrowser.Success);

        if (FileBrowser.Success)
        {
            var pcs = PointCloudManager.GetPointCloudsInScene();
            //Debug.Log("Saving point cloud " + pcs[dd.value].ID + " to " + FileBrowser.Result[0] + "/" + filename);
            Debug.Log("Saving point cloud " + pcs[0].ID + " to " + FileBrowser.Result[0] + "/" + filename);
            //pointCloudManager.SaveLAZFile(FileBrowser.Result[0] + "/" + filename, pcs[dd.value].ID);
            PointCloudManager.SaveLAZFile(FileBrowser.Result[0] + "/" + filename, pcs[0].ID);
            lastSaveDirectory = FileBrowser.Result[0];
            UserSettings.instance.preferences.lastSaveDirectory = lastSaveDirectory;
            UserSettings.instance.SaveToFile();
        }

        loadMenuPanel.SetActive(true);
    }

    public void CloseButton()
    {
        if (PointCloudManager.commandLineMode)
            Application.Quit();
        else
            UnloadFile();
    }

    public void UnloadFile()
    {
        var pcs = PointCloudManager.GetPointCloudsInScene();

        PointCloudManager.UnLoad(pcs[0].ID);

        loadButton.interactable = true;

        HideNonPersistentMenus();
    }

    private void HideNonPersistentMenus()
    {
        saveMenuPanel.SetActive(false);
        backingMenuPanel.SetActive(false);
        outlierMenuPanel.SetActive(false);
        classifiersMenuPanel.SetActive(false);
        colorMenuPanel.SetActive(false);
    }

    private void SetInitialValues()
    {
        var prefs = UserSettings.instance.preferences;

        outlierDistanceSlider.value = prefs.outlierDistance;
        outlierNeighborSlider.value = prefs.outlierNeighborCount;
        //backingScaleSlider.value = prefs.backingLineScale;
        //backingGraduationSlider.value = prefs.backingLineGraduationScale;
        //backingLineThicknessSlider.value = prefs.backingLineThickness;

        ChangePointCloudBackingColor(prefs.backingBackgroundColor);
        ChangePointCloudBackingMajorGridColor(prefs.backingMajorGridColor);
        ChangePointCloudBackingMinorGridColor(prefs.backingMinorGridColor);
        ChangePointCloudBackingScale(prefs.backingLineScale);
        ChangePointCloudBackingGraduationScale(prefs.backingLineGraduationScale);
        ChangePointCloudBackingLineThickness(prefs.backingLineThickness);

        autoHideGroundPlaneToggle.isOn = prefs.autoHideGroundPlaneOnLoad;

        if (prefs.lastLoadDirectory != "" &&
            Directory.Exists(prefs.lastLoadDirectory))
            lastLoadDirectory = prefs.lastLoadDirectory;

        if (prefs.lastSaveDirectory != "" &&
            Directory.Exists(prefs.lastSaveDirectory))
            lastSaveDirectory = prefs.lastSaveDirectory;
    }
 
    public void ResetPointCloudsTransforms()
    {
        PointCloudManager.GetPointCloudsInScene()[0].ResetMiniature(
            UserSettings.instance.preferences.fitSizeOnLoad, 
            UserSettings.instance.preferences.distanceOnLoad
        );
    }
    
    public void HighlightOutliers()
    {
        if (showingOutliers)
        {
            showingOutliers = false;

            showOutliersButtonText.text = "Show";
        }
        else
        {
            showingOutliers = true;

            showOutliersButtonText.text = "Hide";
        }

        UpdateOutliers();
    }

    public void UpdateOutliers()
    {
        var pcs = PointCloudManager.GetPointCloudsInScene();

        if (pcs.Length == 0)
            return;

        var d = showingOutliers ? outlierDistanceSlider.value : 0f;
        int n = showingOutliers ? (int)outlierNeighborSlider.value : 0;

        //pointCloudManager.HighlightOutliers(d, n, pcs[dd.value].ID);
        PointCloudManager.HighlightOutliers(d, n, pcs[0].ID);
    }

    public void DeleteOutliers()
    {
        var pcs = PointCloudManager.GetPointCloudsInScene();
        //pointCloudManager.HighlightOutliers(outliersDistance, outlierNeighborCount, pcs[dd.value].ID);
        PointCloudManager.HighlightOutliers(outlierDistanceSlider.value, (int)outlierNeighborSlider.value, pcs[0].ID);
        //pointCloudManager.DeleteOutliers(pcs[dd.value].ID);
        PointCloudManager.DeleteOutliers(pcs[0].ID);
        showingOutliers = false;
        showOutliersButtonText.text = "Show";
    }

    public void AdjustOutlierDistance(float dist)
    {
        outlierDistValText.text = dist.ToString("F1");
    }

    public void AdjustOutlierNeighborCount(float num)
    {
        outlierNeighborValText.text = ((int)num).ToString();
    }

    public void ToggleGroundPlane()
    {
        groundPlane.SetActive(!groundPlane.activeSelf);

        showGroundPlaneButtonText.text = (groundPlane.activeSelf ? "Hide" : "Show");
    }

    public void ToggleClassifyPointClouds()
    {
        FindObjectOfType<ModifyPoints>().ActivateClassificationMode(!ModifyPoints.classifierMode);

        if (ModifyPoints.classifierMode)
        {
            classifyPointsButtonText.text = "Removal Mode";
            classifierMenuButton.interactable = true;
        }
        else
        {
            classifyPointsButtonText.text = "Classify Mode";
            classifierMenuButton.interactable = false;
            classifiersMenuPanel.SetActive(false);
        }
    }

    public void ToggleBackingMenu()
    {
        backingMenuPanel.SetActive(!backingMenuPanel.activeSelf);

        string openClose = backingMenuPanel.activeSelf ? "Close" : "Open";
        backingOptionsText.text = openClose + " Backing Volume Options";
    }

    public void ToggleOutliersMenu()
    {
        outlierMenuPanel.SetActive(!outlierMenuPanel.activeSelf);
    }

    public void ToggleClassifiersMenu()
    {
        classifiersMenuPanel.SetActive(!classifiersMenuPanel.activeSelf);
    }

    public void ToggleBackingBox(bool onOff)
    {
        var pc = FindObjectOfType<PointCloud>();
        var go = pc.transform.Find(pc.gameObject.name + " Backing");
        go.gameObject.SetActive(onOff);
    }

    private void BlockUnloadWhileSaving()
    {
        if (PointCloudManager.IsLastAsyncSaveFinished()) // not saving
        {
            saveButton.interactable = true;
            saveButtonText.fontSize = 60;
            saveButtonText.text = "Save";

            closeButton.interactable = true;
        }
        else // saving
        {
            saveButton.interactable = false;
            saveButtonText.fontSize = 36;

            int numberOfElipsis = 0 + (int)(4f * (Time.timeSinceLevelLoad % 1f));
            saveButtonText.text = "Saving" + new string('.', numberOfElipsis) + new string(' ', 3 - numberOfElipsis);

            closeButton.interactable = false;
        }
    }

    public void RefreshENC()
    {
        refreshingENC = true;
        StartCoroutine(FindObjectOfType<MapManager>().CreateENC(FindObjectOfType<GEOReference>(), PointCloudManager.GetPointCloudsInScene()[0], true));
    }

    public void ColorPickerBackgroundColor()
    {
        colorPicker.onValueChanged.RemoveAllListeners();
        colorPicker.CurrentColor = Camera.main.backgroundColor;
        colorPicker.onValueChanged.AddListener(ChangeBackgroundColor);
    }

    public void ChangeBackgroundColor(Color color)
    {
        Camera.main.backgroundColor = color;
    }

    public void ColorPickerBackingColor()
    {
        colorPicker.onValueChanged.RemoveAllListeners();
        colorPicker.CurrentColor = pointCloudBackingMaterial.GetColor("_Color");
        colorPicker.onValueChanged.AddListener(ChangePointCloudBackingColor);
    }

    public void ChangePointCloudBackingColor(Color color)
    {
        pointCloudBackingMaterial.SetColor("_Color", color);
    }

    public void ColorPickerMajorGridColor()
    {
        colorPicker.onValueChanged.RemoveAllListeners();
        colorPicker.CurrentColor = pointCloudBackingMaterial.GetColor("_MajorColor");
        colorPicker.onValueChanged.AddListener(ChangePointCloudBackingMajorGridColor);
    }

    public void ChangePointCloudBackingMajorGridColor(Color color)
    {
        pointCloudBackingMaterial.SetColor("_MajorColor", color);
    }

    public void ColorPickerMinorGridColor()
    {
        colorPicker.onValueChanged.RemoveAllListeners();
        colorPicker.CurrentColor = pointCloudBackingMaterial.GetColor("_MinorColor");
        colorPicker.onValueChanged.AddListener(ChangePointCloudBackingMinorGridColor);
    }

    public void ChangePointCloudBackingMinorGridColor(Color color)
    {
        pointCloudBackingMaterial.SetColor("_MinorColor", color);
    }

    public void ChangePointCloudBackingScale(float scale)
    {
        pointCloudBackingMaterial.SetFloat("_Scale", scale);
    }

    public void ChangePointCloudBackingGraduationScale(float scale)
    {
        pointCloudBackingMaterial.SetFloat("_GraduationScale", scale);
    }

    public void ChangePointCloudBackingLineThickness(float thickness)
    {
        pointCloudBackingMaterial.SetFloat("_Thickness", thickness);
    }
}
