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

    private Canvas thisCanvas;

    private Dropdown dd;

    public ColorPicker colorPicker;

    public Button colorPickerButton;

    private bool colorPickerActive;

    private bool menuOpen;

    private bool fileBrowsing;

    private bool loading;
    private bool saving;

    private string lastSaveDirectory;
    private string lastLoadDirectory;

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

        colorPickerButton.onClick.AddListener(() => {
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
        });

        fileDropdown.SetActive(false);
        saveButton.SetActive(false);
        loadingText.SetActive(false);
        loadingIcon.SetActive(false);

        fileBrowsing = false;
        loading = false;
        saving = false;

        lastSaveDirectory = null;
        lastLoadDirectory = null;
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
            }
            else
            {
                ActivateColorPickerUI(true);

                ActivateLoadAndSaveUI(true);
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
    }

    private void CloseMenu()
    {        
        menuOpen = false;

        if (fileBrowsing)
        {
            fileBrowsing = false;
            fileBrowserCanvas.SetActive(false);
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
            pointCloudManager.SaveLAZFile(FileBrowser.Result[0] + "/" + filename, dd.value);
            lastSaveDirectory = FileBrowser.Result[0];
        }

        fileBrowsing = false;
    }

    private void UpdateDropdownFiles()
    {
        var dd = fileDropdown.GetComponent<Dropdown>();

        dd.options.Clear();

        foreach (var pcName in pointCloudManager.pointClouds)
            dd.options.Add(new Dropdown.OptionData(pcName.inSceneRepresentation.name));
    }

    private void ActivateLoadAndSaveUI(bool activate)
    {
        if (activate)
        {
            loadButton.SetActive(true);

            if (pointCloudManager.pointClouds.Count > 0)
            {
                saveButton.SetActive(true);
                fileDropdown.SetActive(true);
            }
        }
        else
        {
            loadButton.SetActive(false);
            saveButton.SetActive(false);
            fileDropdown.SetActive(false);
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
}
