using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ControllerModelManager : MonoBehaviour
{
    [SerializeField]
    ActionBasedController m_LeftController;

    [SerializeField]
    ActionBasedController m_RightController;

    [SerializeField]
    Transform m_GenericModelPrefab;

    [SerializeField]
    Transform m_ViveFocusLeftModelPrefab;
    [SerializeField]
    Transform m_ViveFocusRightModelPrefab;

    [SerializeField]
    Transform m_ViveWandModelPrefab;

    [SerializeField]
    Transform m_ValveIndexLeftModelPrefab;
    [SerializeField]
    Transform m_ValveIndexRightModelPrefab;

    [SerializeField]
    Transform m_WMRLeftModelPrefab;
    [SerializeField]
    Transform m_WMRRightModelPrefab;

    [SerializeField]
    Transform m_MetaQuestLeftModelPrefab;
    [SerializeField]
    Transform m_MetaQuestRightModelPrefab;

    [SerializeField]
    Transform m_HPReverbLeftModelPrefab;
    [SerializeField]
    Transform m_HPReverbRightModelPrefab;

    Transform m_LeftParent;
    Transform m_RightParent;

    bool modelSet = false;

    public enum ControllerType
    {
        Generic = 0,
        ViveWand = 1,
        ViveFocus = 2,
        ValveIndex = 3,
        WMR = 4,
        MetaQuest = 5,
        HPReverb = 6
    }

    ControllerType m_CurrentControllerType;
    public ControllerType currentControllerType
    {
        get => m_CurrentControllerType;
        set => SetController(value);
    }

    class ControlsComponentMap
    {
        public string showMaps;
        public string mapHeight;
        public string grab;
        public string openUI;
        public string scale;
        public string edit;
        public string cursorAdjust;
        public string undo;
    }

    ControlsComponentMap m_ViveFocus3Controls = new ControlsComponentMap();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!modelSet)
        {
            SetController(UserSettings.instance.preferences.controllerType);
            modelSet = true;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
            SetController(ControllerType.Generic);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            SetController(ControllerType.ViveWand);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            SetController(ControllerType.ViveFocus);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            SetController(ControllerType.ValveIndex);

        if (Input.GetKeyDown(KeyCode.Alpha5))
            SetController(ControllerType.MetaQuest);

        if (Input.GetKeyDown(KeyCode.Alpha6))
            SetController(ControllerType.WMR);

        if (Input.GetKeyDown(KeyCode.Alpha7))
            SetController(ControllerType.HPReverb);
    }

    public void SetController(ControllerType type)
    {
        m_CurrentControllerType = type;

        UserSettings.instance.preferences.controllerType = m_CurrentControllerType;

        switch (type) 
        {
            case ControllerType.ViveWand:
                AssignControllerModel(m_ViveWandModelPrefab, m_LeftController, ref m_LeftParent);
                AssignControllerModel(m_ViveWandModelPrefab, m_RightController, ref m_RightParent);
                break;
            case ControllerType.ViveFocus:
                AssignControllerModel(m_ViveFocusLeftModelPrefab, m_LeftController, ref m_LeftParent);
                AssignControllerModel(m_ViveFocusRightModelPrefab, m_RightController, ref m_RightParent);
                break;
            case ControllerType.ValveIndex:
                AssignControllerModel(m_ValveIndexLeftModelPrefab, m_LeftController, ref m_LeftParent);
                AssignControllerModel(m_ValveIndexRightModelPrefab, m_RightController, ref m_RightParent);
                break;
            case ControllerType.WMR:
                AssignControllerModel(m_WMRLeftModelPrefab, m_LeftController, ref m_LeftParent);
                AssignControllerModel(m_WMRRightModelPrefab, m_RightController, ref m_RightParent);
                break;
            case ControllerType.MetaQuest:
                AssignControllerModel(m_MetaQuestLeftModelPrefab, m_LeftController, ref m_LeftParent);
                AssignControllerModel(m_MetaQuestRightModelPrefab, m_RightController, ref m_RightParent);
                break;
            case ControllerType.HPReverb:
                AssignControllerModel(m_HPReverbLeftModelPrefab, m_LeftController, ref m_LeftParent);
                AssignControllerModel(m_HPReverbRightModelPrefab, m_RightController, ref m_RightParent);
                break;
            default:
                AssignControllerModel(m_GenericModelPrefab, m_LeftController, ref m_LeftParent);
                AssignControllerModel(m_GenericModelPrefab, m_RightController, ref m_RightParent);
                break;
        }
    }

    public ControllerType GetControllerType()
    {
        return m_CurrentControllerType;
    }

    void AssignControllerModel(Transform prefab, ActionBasedController controller, ref Transform modelParent)
    {
        if (modelParent != null)
        {
            Destroy(modelParent.gameObject);
        }

        modelParent = new GameObject($"[{controller.name}] Model Parent").transform;
        modelParent.SetParent(controller.transform);
        modelParent.localPosition = Vector3.zero;
        modelParent.localRotation = Quaternion.identity;

        controller.model = Instantiate(prefab, modelParent).transform;
    }
}
