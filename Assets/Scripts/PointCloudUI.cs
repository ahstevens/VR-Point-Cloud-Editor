using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using SimpleFileBrowser;
using System;

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

    private bool menuOpen;

    private bool fileBrowsing;

    private bool loading;
    private bool saving;

    private string lastSaveDirectory;
    private string lastLoadDirectory;

    // Start is called before the first frame update
    void Start()
    {
        menuOpen = false;
        openMenu.started += ctx => OpenMenu();

        thisCanvas = GetComponent<Canvas>();

        thisCanvas.enabled = false;

        dd = fileDropdown.GetComponent<Dropdown>();

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
            loadingText.SetActive(false);
            loadingIcon.SetActive(false);
            UpdateDropdownFiles();
        }

        if (fileBrowsing || loading || saving)
        {
            loadButton.SetActive(false);
            saveButton.SetActive(false);
            fileDropdown.SetActive(false);
        }
        else
        {
            loadButton.SetActive(true);

            if (pointCloudManager.pointClouds.Count > 0)
            {
                saveButton.SetActive(true);
                fileDropdown.SetActive(true);
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
        if (menuOpen)
        {
            thisCanvas.enabled = false;
            menuOpen = false;

            if (fileBrowsing)
            {
                fileBrowsing = false;
                fileBrowserCanvas.SetActive(false);
            }
        }
        else
        {
            thisCanvas.enabled = true;
            menuOpen = true;
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
                loadButton.SetActive(false); 
                loadingText.SetActive(true);
                loadingIcon.SetActive(true);
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
}
