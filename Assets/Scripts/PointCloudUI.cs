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
    public InputAction openMenu;

    public GameObject fileBrowserCanvas;
    public GameObject loadButton;
    public GameObject saveButton;
    public GameObject fileDropdown;
    public GameObject loadingText;
    public GameObject loadingIcon;
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

    private Dropdown dd;

    public ColorPicker colorPicker;

    public GameObject panel;

    public Button colorPickerButton;

    public Button resetPointCloudTransforms;

    public GameObject resettableObject;

    public GameObject editingCursor;

    public XRFlyingInterface xrf;

    public Button flyButton;

    public UnityEngine.XR.Interaction.Toolkit.XRInteractorLineVisual pointer;

    private GameObject connector;

    private bool colorPickerActive;

    private bool menuOpen;

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
        openMenu.started += ctx => OpenMenu();
        openMenu.canceled += ctx => CloseMenu();

        menuOpen = false;

        thisCanvas = GetComponent<Canvas>();

        thisCanvas.enabled = false;

        dd = fileDropdown.GetComponent<Dropdown>();

        pointer.enabled = false;
        
        connector = GameObject.Find("Connector");

        colorPickerActive = false;
        colorPicker.gameObject.SetActive(false);

        colorPicker.CurrentColor = Camera.main.backgroundColor;

        colorPicker.onValueChanged.AddListener(color =>
        {
            Camera.main.backgroundColor = color;
        });

        fileDropdown.SetActive(false);
        saveButton.SetActive(false);
        loadingText.SetActive(false);
        loadingIcon.SetActive(false);
        resetPointCloudTransforms.gameObject.SetActive(false);
        unloadButton.SetActive(false);

        autoHideGroundPlane = autoHideGroundPlaneToggle.GetComponent<Toggle>().isOn;

        buildInfo.GetComponent<Text>().text = Application.version;

        ActivateOutliersUI(false);

        fileBrowsing = false;
        loading = false;
        saving = false;

        showingOutliers = false;

        lastSaveDirectory = null;
        lastLoadDirectory = null;

        AdjustOutlierDistance(outliersDistance);
        AdjustOutlierNeighborCount(outlierNeighborCount);

        if (groundPlane == null)
            groundPlane = GameObject.Find("Ground Plane");
    }

    // Update is called once per frame
    void Update()
    {

        if (connector == null)
            connector = GameObject.Find("Connector");

        // just finished loading a file
        if (loading && !pointCloudManager.isWaitingToLoad)
        {
            loading = false;

            ActivateLoadingUI(false);
            UpdateDropdownFiles();

            Debug.Log("Auto-Hide Ground Plane is " + autoHideGroundPlane);
            Debug.Log("Ground Plane Active: " + groundPlane.activeSelf);

            if (groundPlane.activeSelf && autoHideGroundPlane)
            {
                ToggleGroundPlane();
            }
        }

        // idle state
        if (!loading && !menuOpen)
        {
            thisCanvas.enabled = false;
        }

        if (!colorPickerActive)
        {
            // reduced UI
            if (fileBrowsing || loading || saving)
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

        BlockUnloadWhileSaving();
    }

    private void OnEnable()
    {
        openMenu.Enable();        
    }

    private void OnDisable()
    {
        openMenu.Disable();
    }

    private void OpenMenu()
    {
        thisCanvas.enabled = true;
        menuOpen = true;

        if (!xrf.enabled)
        {
            editingCursor.SetActive(false);
            connector.SetActive(false);

            if (pointer)
                pointer.enabled = true;
        }
    }

    private void CloseMenu()
    {        
        menuOpen = false;

        if (fileBrowsing)
        {
            fileBrowsing = false;
            fileBrowserCanvas.SetActive(false);
        }

        if (!xrf.enabled)
        {
            editingCursor.SetActive(true);
            connector.SetActive(true);

            if (pointer)
                pointer.enabled = false;
        }
    }

    public void LoadFile()
    {
        fileBrowsing = true;

        fileBrowserCanvas.SetActive(true);

        FileBrowser.SetFilters(true, new FileBrowser.Filter("Point Clouds", ".las", ".laz"));
        FileBrowser.SetDefaultFilter(".las");
        FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");
        FileBrowser.AddQuickLink("Project Data", Application.dataPath, null);

        StartCoroutine(ShowLoadDialogCoroutine());
    }

    IEnumerator ShowLoadDialogCoroutine()
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, lastLoadDirectory == null ? Application.dataPath : lastLoadDirectory, null, "Load Point Cloud", "Load");

        //Debug.Log(FileBrowser.Success);

        if (FileBrowser.Success)
        {
            if (pointCloudManager.loadLAZFile(FileBrowser.Result[0]))
            {
                loading = true;
                ActivateLoadingUI(true);
            }

            lastLoadDirectory = Path.GetDirectoryName(FileBrowser.Result[0]);
        }

        fileBrowsing = false;
    }

    public void SaveFile()
    {
        fileBrowsing = true;

        fileBrowserCanvas.SetActive(true);

        FileBrowser.SetFilters(true, new FileBrowser.Filter("Point Clouds", ".las", ".laz"));
        FileBrowser.SetDefaultFilter(".las");
        FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");
        FileBrowser.AddQuickLink("Project Data", Application.dataPath, null);

        StartCoroutine(ShowSaveDialogCoroutine(dd.options[dd.value].text + "_edit.laz"));
    }

    IEnumerator ShowSaveDialogCoroutine(string filename)
    {
        yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Folders, false, lastSaveDirectory == null ? lastLoadDirectory : lastSaveDirectory, null, "Select Folder to Save " + filename, "Save");

        Debug.Log(FileBrowser.Success);

        if (FileBrowser.Success)
        {
            var pcs = pointCloudManager.getPointCloudsInScene();
            Debug.Log("Saving point cloud " + pcs[dd.value].ID + " to " + FileBrowser.Result[0] + "/" + filename);
            pointCloudManager.SaveLAZFile(FileBrowser.Result[0] + "/" + filename, pcs[dd.value].ID);
            lastSaveDirectory = FileBrowser.Result[0];
        }

        fileBrowsing = false;
    }

    public void UnloadFile()
    {
        var pcs = pointCloudManager.getPointCloudsInScene();

        pointCloudManager.UnLoad(pcs[dd.value].ID);

        UpdateDropdownFiles();
    }

    private void UpdateDropdownFiles()
    {
        dd.options.Clear();

        var pcs = pointCloudManager.getPointCloudsInScene();

        foreach (var pc in pcs)
            dd.options.Add(new Dropdown.OptionData(pc.name));

        dd.RefreshShownValue();
    }

    private void ActivateLoadAndSaveUI(bool activate)
    {
        if (activate)
        {
            loadButton.SetActive(true);
            showGroundPlaneButton.SetActive(true);
            floorText.SetActive(true);
            autoHideGroundPlaneToggle.SetActive(true);

            bool pointCloudsLoaded = pointCloudManager.getPointCloudsInScene().Length > 0;

            saveButton.SetActive(pointCloudsLoaded);
            fileDropdown.SetActive(pointCloudsLoaded);
            resetPointCloudTransforms.gameObject.SetActive(pointCloudsLoaded);

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
            fileDropdown.SetActive(false);
            resetPointCloudTransforms.gameObject.SetActive(false);
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

    private void ActivateLoadingUI(bool activate)
    {
        if (activate)
        {
            loadingText.SetActive(true);
            loadingIcon.SetActive(true);
        }
        else
        {
            loadingText.SetActive(false);
            loadingIcon.SetActive(false);
        }
    }

    public void resetPointCloudsTransforms()
    {
        resettableObject.transform.position = Vector3.zero;
        resettableObject.transform.rotation = Quaternion.identity;
        resettableObject.transform.localScale = Vector3.one;
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
        var pcs = pointCloudManager.getPointCloudsInScene();

        if (pcs.Length == 0)
            return;

        var d = showingOutliers ? outliersDistance : 0f;
        var n = showingOutliers ? outlierNeighborCount : 0;

        pointCloudManager.HighlightOutliers(d, n, pcs[dd.value].ID);
    }

    public void DeleteOutliers()
    {
        var pcs = pointCloudManager.getPointCloudsInScene();
        pointCloudManager.HighlightOutliers(outliersDistance, outlierNeighborCount, pcs[dd.value].ID);
        pointCloudManager.DeleteOutliers(pcs[dd.value].ID);
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
        Debug.Log("Auto-Hide Ground Plane is " + autoHideGroundPlane);
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
}
