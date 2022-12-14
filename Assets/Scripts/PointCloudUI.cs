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
    public InputActionReference openMenu;

    public GameObject fileBrowserCanvas;
    public GameObject loadButton;
    public GameObject saveButton;
    //public GameObject fileDropdown;
    public GameObject refreshENCButton;
    public GameObject classifyPointsButton;
    public GameObject classifierMenuButton;
    public GameObject classifierPanel;
    public GameObject unloadButton;
    public GameObject outlierText;
    public GameObject outlierShowButton;
    public GameObject outlierDeleteButton;
    public GameObject outlierDistSlider;
    public GameObject outlierNeighborSlider;
    public GameObject outlierDistText;
    public GameObject outlierNeighborText;
    public GameObject outlierDistValText;
    public GameObject outlierNeighborValText;
    public GameObject floorText;
    public GameObject showGroundPlaneButton;
    public GameObject autoHideGroundPlaneToggle;
    public GameObject groundPlane;
    public GameObject buildInfo;

    private Canvas thisCanvas;

    //private Dropdown dd;

    public ColorPicker colorPicker;

    public GameObject panel;

    public Button colorPickerButton;

    public Button resetPointCloudTransforms;

    public GameObject resettableObject;

    public GameObject editingCursor;

    public XRFlyingInterface xrf;

    public Button flyButton;

    public UnityEngine.XR.Interaction.Toolkit.XRInteractorLineVisual pointer;

    public GameObject connector;

    private bool colorPickerActive;

    private bool _menuOpen; 
    
    public bool MenuOpen
    {
        get { return _menuOpen; }
    }

    private bool fileBrowsing;

    private bool autoHideGroundPlane;

    private bool loading;
    private bool saving;

    private bool showingOutliers;

    private string lastSaveDirectory;
    private string lastLoadDirectory;

    private float outliersDistance = 5f;
    private int outlierNeighborCount = 5;

    // Start is called before the first frame update
    void Start()
    {
        openMenu.action.started += ctx => OpenMenuAction();
        openMenu.action.canceled += ctx => CloseMenuAction();

        _menuOpen = false;

        thisCanvas = GetComponent<Canvas>();

        thisCanvas.enabled = false;

        //dd = fileDropdown.GetComponent<Dropdown>();

        pointer.enabled = UserSettings.instance.preferences.openMenuOnStart;

        colorPickerActive = false;
        colorPicker.gameObject.SetActive(false);

        colorPicker.CurrentColor = Camera.main.backgroundColor;

        colorPicker.onValueChanged.AddListener(color =>
        {
            Camera.main.backgroundColor = color;
        });

        //fileDropdown.SetActive(false);
        saveButton.SetActive(false);
        resetPointCloudTransforms.gameObject.SetActive(false);
        classifyPointsButton.SetActive(false);
        classifierMenuButton.SetActive(false);
        unloadButton.SetActive(false);

        autoHideGroundPlane = autoHideGroundPlaneToggle.GetComponent<Toggle>().isOn = UserSettings.instance.preferences.autoHideGroundPlaneOnLoad; ;

        buildInfo.GetComponent<Text>().text = Application.version;

        ActivateOutliersUI(false);

        fileBrowsing = false;
        loading = false;
        saving = false;

        showingOutliers = false;

        lastLoadDirectory = lastSaveDirectory = null;

        if (UserSettings.instance.preferences.lastLoadDirectory != "" &&
            Directory.Exists(UserSettings.instance.preferences.lastLoadDirectory))
            lastLoadDirectory = UserSettings.instance.preferences.lastLoadDirectory;

        if (UserSettings.instance.preferences.lastSaveDirectory != "" &&
            Directory.Exists(UserSettings.instance.preferences.lastSaveDirectory))
            lastSaveDirectory = UserSettings.instance.preferences.lastSaveDirectory;

        AdjustOutlierDistance(outliersDistance);
        AdjustOutlierNeighborCount(outlierNeighborCount);

        if (groundPlane == null)
            groundPlane = GameObject.Find("Ground Plane");

        if (pointCloudManager.commandLineMode)
        {
            loadButton.GetComponentInChildren<Text>().text = "Revert";
            saveButton.GetComponentInChildren<Text>().text = "Save";
            unloadButton.GetComponentInChildren<Text>().text = "Quit";

            loading = true;
        }

        classifyPointsButton.GetComponent<Button>().onClick.AddListener(() => 
        {
            FindObjectOfType<ModifyPoints>().ActivateClassificationMode(!ModifyPoints.classifierMode);
        });

        classifierMenuButton.GetComponent<Button>().onClick.AddListener(() => 
        { 
            classifierPanel.SetActive(!classifierPanel.activeSelf); 
        });

        if (UserSettings.instance.preferences.openMenuOnStart)
            OpenMenu();
    }

    // Update is called once per frame
    void Update()
    {        
        if (loading)
        {
            if (pointCloudManager.isWaitingToLoad)
            {
                loadButton.GetComponent<Button>().interactable = false;
                loadButton.GetComponentInChildren<Text>().fontSize = 36;

                int numberOfElipsis = 0 + (int)(4f * (Time.timeSinceLevelLoad % 1f));
                loadButton.GetComponentInChildren<Text>().text = "Loading" + new string('.', numberOfElipsis) + new string(' ', 3 - numberOfElipsis);

                //unloadButton.GetComponent<Button>().interactable = false;
            }
            else // just finished loading a file
            {
                loadButton.GetComponent<Button>().interactable = true;
                loadButton.GetComponentInChildren<Text>().fontSize = 60;
                loadButton.GetComponentInChildren<Text>().text = pointCloudManager.commandLineMode ? "Revert" : "Load";

                //unloadButton.GetComponent<Button>().interactable = true;

                loading = false;

                //ActivateLoadingUI(false);
                //UpdateDropdownFiles();

                if (groundPlane.activeSelf && autoHideGroundPlane)
                {
                    ToggleGroundPlane();
                }

                //CloseMenu();
            }
        }
        else
        {
            // idle state
            if (!_menuOpen)
                thisCanvas.enabled = false;
        }

        

        if (!colorPickerActive)
        {
            // reduced UI
            if (fileBrowsing)
            {
                ActivateColorPickerUI(false);

                ActivateLoadAndSaveUI(false);

                //flyButton.gameObject.SetActive(false);
                buildInfo.SetActive(false);

                panel.SetActive(false);
            }
            else
            {
                ActivateColorPickerUI(true);

                ActivateLoadAndSaveUI(true);

                //flyButton.gameObject.SetActive(true);
                buildInfo.SetActive(true);

                panel.SetActive(true);                
            }
        }

        if (pointCloudManager.commandLineMode && saving && pointCloudManager.IsLastAsyncSaveFinished())
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
        if (UserSettings.instance.preferences.stickyUI && thisCanvas.enabled == true)
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
        thisCanvas.enabled = true;
        _menuOpen = true;

        if (!xrf.enabled)
        {
            FindObjectOfType<ModifyPoints>().SetBrushVisibility(false);

            if (pointer)
                pointer.enabled = true;
        }
    }

    private void CloseMenu()
    {
        _menuOpen = false;

        if (fileBrowsing)
        {
            fileBrowsing = false;
            fileBrowserCanvas.SetActive(false);
        }

        if (!xrf.enabled)
        {
            FindObjectOfType<ModifyPoints>().SetBrushVisibility(true);

            if (pointer)
                pointer.enabled = false;
        }
    }

    public void LoadFile()
    {
        if (pointCloudManager.commandLineMode) // this resets the point cloud in command line mode
        {
            UnloadFile();
            LoadFile(pointCloudManager.commandLineInputFile);
        }
        else
        {
            fileBrowsing = true;

            fileBrowserCanvas.SetActive(true);

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

        //Debug.Log(FileBrowser.Success);

        if (FileBrowser.Success)
        {
            LoadFile(FileBrowser.Result[0]);

            lastLoadDirectory = Path.GetDirectoryName(FileBrowser.Result[0]);
            UserSettings.instance.preferences.lastLoadDirectory = lastLoadDirectory;
            UserSettings.instance.SaveToFile();
        }

        fileBrowsing = false;
    }

    public bool LoadFile(string filePath)
    {
        if (pointCloudManager.LoadLAZFile(filePath))
        {
            loading = true;
            //ActivateLoadingUI(true);
            fileBrowsing = false;
            return true;
        }

        return false;
    }

    public void SaveFile()
    {
        if (pointCloudManager.commandLineMode)
        {
            if (pointCloudManager.commandLineOutputFile != "")
                pointCloudManager.SaveLAZFile(pointCloudManager.commandLineOutputFile, pointCloudManager.GetPointCloudsInScene()[0].ID);
            else
                pointCloudManager.SaveLAZFile(pointCloudManager.commandLineInputFile, pointCloudManager.GetPointCloudsInScene()[0].ID);

            saving = true;
        }
        else
        {
            fileBrowsing = true;

            fileBrowserCanvas.SetActive(true);

            FileBrowser.SetFilters(true, new FileBrowser.Filter("Point Clouds", ".las", ".laz", ".npz"));
            FileBrowser.SetDefaultFilter(".las");
            FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");
            FileBrowser.AddQuickLink("Project Data", Application.dataPath, null);

            if (UserSettings.instance.preferences.lastLoadDirectory != "")
                FileBrowser.AddQuickLink("Last Load", UserSettings.instance.preferences.lastLoadDirectory, null);

            if (UserSettings.instance.preferences.lastSaveDirectory != "")
                FileBrowser.AddQuickLink("Last Save", UserSettings.instance.preferences.lastSaveDirectory, null);

            //StartCoroutine(ShowSaveDialogCoroutine(dd.options[dd.value].text + "_edit.laz"));
            var pc = pointCloudManager.GetPointCloudsInScene()[0];
            StartCoroutine(ShowSaveDialogCoroutine(pc.name + "_edit" + (pc.pathToRawData[^3..] == "npz" ? ".npz" : ".laz")));
        }
    }

    IEnumerator ShowSaveDialogCoroutine(string filename)
    {
        yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Folders, false, lastSaveDirectory == null ? lastLoadDirectory : lastSaveDirectory, null, "Select Folder to Save " + filename, "Save");

        //Debug.Log(FileBrowser.Success);

        if (FileBrowser.Success)
        {
            var pcs = pointCloudManager.GetPointCloudsInScene();
            //Debug.Log("Saving point cloud " + pcs[dd.value].ID + " to " + FileBrowser.Result[0] + "/" + filename);
            Debug.Log("Saving point cloud " + pcs[0].ID + " to " + FileBrowser.Result[0] + "/" + filename);
            //pointCloudManager.SaveLAZFile(FileBrowser.Result[0] + "/" + filename, pcs[dd.value].ID);
            pointCloudManager.SaveLAZFile(FileBrowser.Result[0] + "/" + filename, pcs[0].ID);
            lastSaveDirectory = FileBrowser.Result[0];
            UserSettings.instance.preferences.lastSaveDirectory = lastSaveDirectory;
            UserSettings.instance.SaveToFile();
        }

        fileBrowsing = false;
    }

    public void CloseButton()
    {
        if (pointCloudManager.commandLineMode)
            Application.Quit();
        else
            UnloadFile();
    }

    public void UnloadFile()
    {
        var pcs = pointCloudManager.GetPointCloudsInScene();

        //pointCloudManager.UnLoad(pcs[dd.value].ID);
        pointCloudManager.UnLoad(pcs[0].ID);

        //UpdateDropdownFiles();
    }

    //private void UpdateDropdownFiles()
    //{
    //    dd.options.Clear();
    //
    //    var pcs = pointCloudManager.getPointCloudsInScene();
    //
    //    foreach (var pc in pcs)
    //        dd.options.Add(new Dropdown.OptionData(pc.name));
    //
    //    dd.RefreshShownValue();
    //}

    private void ActivateLoadAndSaveUI(bool activate)
    {
        if (activate)
        {
            bool pointCloudsLoaded = pointCloudManager.GetPointCloudsInScene().Length > 0;

            loadButton.SetActive(true);
            loadButton.GetComponent<Button>().interactable = !pointCloudsLoaded || pointCloudManager.commandLineMode;

            showGroundPlaneButton.SetActive(true);
            floorText.SetActive(true);
            autoHideGroundPlaneToggle.SetActive(true);

            saveButton.SetActive(pointCloudsLoaded);
            //fileDropdown.SetActive(pointCloudsLoaded);
            resetPointCloudTransforms.gameObject.SetActive(pointCloudsLoaded);
            classifyPointsButton.SetActive(pointCloudsLoaded);
            classifierMenuButton.SetActive(pointCloudsLoaded);

            refreshENCButton.SetActive(pointCloudsLoaded);

            if (pointCloudsLoaded)
            {
                Button b = refreshENCButton.GetComponent<Button>();

                if (!pointCloudManager.GetPointCloudsInScene()[0].validEPSG)
                {
                    b.GetComponentInChildren<Text>().text = "No Valid ENC";
                    refreshENCButton.GetComponent<Button>().interactable = false;
                }
                else if (FindObjectOfType<MapManager>().refreshing)
                {
                    b.GetComponentInChildren<Text>().text = "Refreshing...";
                    refreshENCButton.GetComponent<Button>().interactable = false;
                }
                else
                {
                    b.GetComponentInChildren<Text>().text = "Refresh ENC";
                    refreshENCButton.GetComponent<Button>().interactable = true;
                }


                if (ModifyPoints.classifierMode)
                {
                    classifyPointsButton.GetComponentInChildren<Text>().text = "Remove Points";
                    classifierMenuButton.GetComponent<Button>().interactable = true;
                }
                else
                {
                    classifyPointsButton.GetComponentInChildren<Text>().text = "Classify Points";
                    classifierMenuButton.GetComponent<Button>().interactable = false;
                    classifierPanel.SetActive(false);
                }
            }
            else
            {
                classifierPanel.SetActive(false);
            }

            unloadButton.SetActive(pointCloudsLoaded);

            ActivateOutliersUI(pointCloudsLoaded);                     
        }
        else
        {
            showGroundPlaneButton.SetActive(false);
            floorText.SetActive(false);
            autoHideGroundPlaneToggle.SetActive(false);
            loadButton.SetActive(false);
            saveButton.SetActive(false);
            //fileDropdown.SetActive(false);
            resetPointCloudTransforms.gameObject.SetActive(false);
            refreshENCButton.gameObject.SetActive(false);
            classifyPointsButton.SetActive(false);
            classifierMenuButton.SetActive(false);
            classifierPanel.SetActive(false);
            unloadButton.SetActive(false);
            buildInfo.SetActive(false);

            ActivateOutliersUI(false);
        }
    }

    public void ToggleColorPicker()
    {
        if (colorPickerActive)
        {
            colorPickerActive = false;

            ActivateLoadAndSaveUI(true);

            colorPicker.gameObject.SetActive(false);
            colorPickerButton.GetComponentInChildren<Text>().text = "Choose Environment Color";
        }
        else
        {
            colorPickerActive = true;

            ActivateLoadAndSaveUI(false);

            colorPicker.gameObject.SetActive(true);
            colorPickerButton.GetComponentInChildren<Text>().text = "Close Color Picker";
        }
    }

    private void ActivateColorPickerUI(bool activate)
    {
        if (activate)
        {
            colorPickerButton.gameObject.SetActive(true);
        }
        else
        {
            colorPickerButton.gameObject.SetActive(false);
            colorPicker.gameObject.SetActive(false);
        }
    }

    public void resetPointCloudsTransforms()
    {
        //resettableObject.transform.position = Vector3.zero;
        //resettableObject.transform.rotation = Quaternion.identity;
        //resettableObject.transform.localScale = Vector3.one;

        pointCloudManager.GetPointCloudsInScene()[0].ResetMiniature(
            UserSettings.instance.preferences.fitSizeOnLoad, 
            UserSettings.instance.preferences.distanceOnLoad
        );
    }
    
    public void HighlightOutliers()
    {
        if (showingOutliers)
        {
            showingOutliers = false;

            outlierShowButton.GetComponent<Button>().GetComponentInChildren<Text>().text = "Show";
        }
        else
        {
            showingOutliers = true;

            outlierShowButton.GetComponent<Button>().GetComponentInChildren<Text>().text = "Hide";
        }

        UpdateOutliers();
    }

    public void UpdateOutliers()
    {
        var pcs = pointCloudManager.GetPointCloudsInScene();

        if (pcs.Length == 0)
            return;

        var d = showingOutliers ? outliersDistance : 0f;
        var n = showingOutliers ? outlierNeighborCount : 0;

        //pointCloudManager.HighlightOutliers(d, n, pcs[dd.value].ID);
        pointCloudManager.HighlightOutliers(d, n, pcs[0].ID);
    }

    public void DeleteOutliers()
    {
        var pcs = pointCloudManager.GetPointCloudsInScene();
        //pointCloudManager.HighlightOutliers(outliersDistance, outlierNeighborCount, pcs[dd.value].ID);
        pointCloudManager.HighlightOutliers(outliersDistance, outlierNeighborCount, pcs[0].ID);
        //pointCloudManager.DeleteOutliers(pcs[dd.value].ID);
        pointCloudManager.DeleteOutliers(pcs[0].ID);
        showingOutliers = false;
        outlierShowButton.GetComponent<Button>().GetComponentInChildren<Text>().text = "Show";
    }

    public void AdjustOutlierDistance(System.Single dist)
    {
        outliersDistance = dist;
        outlierDistValText.GetComponent<Text>().text = dist.ToString("F1");
    }

    public void AdjustOutlierNeighborCount(System.Single num)
    {
        outlierNeighborCount = (int)num;
        outlierNeighborValText.GetComponent<Text>().text = num.ToString();
    }

    private void ActivateOutliersUI(bool activate)
    {
        outlierDeleteButton.SetActive(activate);
        outlierShowButton.SetActive(activate);
        outlierText.SetActive(activate);
        outlierDistSlider.SetActive(activate);
        outlierDistText.SetActive(activate);
        outlierDistValText.SetActive(activate);
        outlierNeighborSlider.SetActive(activate);
        outlierNeighborText.SetActive(activate);
        outlierNeighborValText.SetActive(activate);
    }

    public void ToggleGroundPlane()
    {
        groundPlane.SetActive(!groundPlane.activeSelf);

        showGroundPlaneButton.GetComponentInChildren<Text>().text = (groundPlane.activeSelf ? "Hide" : "Show");// + " Ground Plane";
    }

    public void ToggleFlyingMode()
    {
        xrf.enabled = !xrf.enabled;
        
        if (xrf.enabled)
            flyButton.GetComponentInChildren<Text>().text = "EDIT MODE";
        else
            flyButton.GetComponentInChildren<Text>().text = "FLY MODE";
    }

    public void ToggleAutoHideGroundPlane(bool isOn)
    {
        autoHideGroundPlane = isOn;
    }

    private void BlockUnloadWhileSaving()
    {
        //unloadButton.GetComponent<Button>().enabled = pointCloudManager.IsLastAsyncSaveFinished();
        //unloadButton.SetActive(pointCloudManager.IsLastAsyncSaveFinished());

        if (pointCloudManager.IsLastAsyncSaveFinished()) // not saving
        {
            saveButton.GetComponent<Button>().interactable = true;
            saveButton.GetComponentInChildren<Text>().fontSize = 60;
            saveButton.GetComponentInChildren<Text>().text = "Save";

            unloadButton.GetComponent<Button>().interactable = true;
        }
        else // saving
        {
            saveButton.GetComponent<Button>().interactable = false;
            saveButton.GetComponentInChildren<Text>().fontSize = 36;


            int numberOfElipsis = 0 + (int)(4f * (Time.timeSinceLevelLoad % 1f));
            saveButton.GetComponentInChildren<Text>().text = "Saving" + new string('.', numberOfElipsis) + new string(' ', 3 - numberOfElipsis);

            unloadButton.GetComponent<Button>().interactable = false;
        }
    }

    public void RefreshENC()
    {
        StartCoroutine(FindObjectOfType<MapManager>().CreateENC(FindObjectOfType<GEOReference>(), pointCloudManager.GetPointCloudsInScene()[0], true));
    }
}
