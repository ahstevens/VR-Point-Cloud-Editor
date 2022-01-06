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
    public GameObject showGroundPlaneButton;
    public GameObject groundPlane;

    private Canvas thisCanvas;

    private Dropdown dd;

    public ColorPicker colorPicker;

    public GameObject panel;

    public Button colorPickerButton;

    public Button resetPointCloudTransforms;

    public GameObject resettableObject;

    public GameObject editingCursor;

    private bool colorPickerActive;

    private bool menuOpen;

    private bool fileBrowsing;

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

        ActivateOutliersUI(false);

        fileBrowsing = false;
        loading = false;
        saving = false;

        showingOutliers = false;

        lastSaveDirectory = null;
        lastLoadDirectory = null;

        AdjustOutlierDistance();
        AdjustOutlierNeighborCount();

        if (groundPlane == null)
            groundPlane = GameObject.Find("Ground Plane");
    }

    // Update is called once per frame
    void Update()
    {
        if (loading && !pointCloudManager.isWaitingToLoad)
        {
            loading = false;

            ActivateLoadingUI(false);
            UpdateDropdownFiles();
        }

        if (!loading && !menuOpen)
        {
            thisCanvas.enabled = false;
        }

        if (!colorPickerActive)
        {
            if (fileBrowsing || loading || saving)
            {
                ActivateColorPickerUI(false);

                ActivateLoadAndSaveUI(false);

                panel.SetActive(false);
            }
            else
            {
                ActivateColorPickerUI(true);

                ActivateLoadAndSaveUI(true);

                panel.SetActive(true);
            }
        }
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

        editingCursor.SetActive(false);
    }

    private void CloseMenu()
    {        
        menuOpen = false;

        if (fileBrowsing)
        {
            fileBrowsing = false;
            fileBrowserCanvas.SetActive(false);
        }

        editingCursor.SetActive(true);
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

        Debug.Log(FileBrowser.Success);

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
        yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Folders, false, lastSaveDirectory == null ? Application.dataPath : lastSaveDirectory, filename, "Select Folder to Save " + filename, "Save");

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
            loadButton.SetActive(false);
            saveButton.SetActive(false);
            fileDropdown.SetActive(false);
            resetPointCloudTransforms.gameObject.SetActive(false);
            unloadButton.SetActive(false);

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

        var d = showingOutliers ? outliersDistance : 0f;
        var n = showingOutliers ? outlierNeighborCount : 0;

        pointCloudManager.HighlightOutliers(d, n, pcs[dd.value].ID);
    }

    public void DeleteOutliers()
    {
        var pcs = pointCloudManager.getPointCloudsInScene();
        pointCloudManager.HighlightOutliers(outliersDistance, outlierNeighborCount, pcs[dd.value].ID);
        pointCloudManager.DeleteOutliers(pcs[dd.value].ID);
    }

    public void AdjustOutlierDistance()
    {
        outliersDistance = outlierDistSlider.GetComponent<Slider>().value;
        outlierDistValText.GetComponent<Text>().text = outliersDistance.ToString("F1");

        UpdateOutliers();
    }

    public void AdjustOutlierNeighborCount()
    {
        outlierNeighborCount = (int)outlierNeighborSlider.GetComponent<Slider>().value;
        outlierNeighborValText.GetComponent<Text>().text = outlierNeighborCount.ToString();

        UpdateOutliers();
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

        showGroundPlaneButton.GetComponentInChildren<Text>().text = (groundPlane.activeSelf ? "Hide" : "Show") + " Ground Plane";
    }
}
