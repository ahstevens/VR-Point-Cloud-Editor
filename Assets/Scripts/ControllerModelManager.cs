using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class ControllerModelManager : MonoBehaviour
{
    [SerializeField] ActionBasedController m_LeftController;
    [SerializeField] ActionBasedController m_RightController;

    [SerializeField] Transform m_GenericModelPrefab;

    [SerializeField] Transform m_ViveFocusLeftModelPrefab;
    [SerializeField] Transform m_ViveFocusRightModelPrefab;

    [SerializeField] Transform m_ViveWandModelPrefab;

    [SerializeField] Transform m_ValveIndexLeftModelPrefab;
    [SerializeField] Transform m_ValveIndexRightModelPrefab;

    [SerializeField] Transform m_WMRLeftModelPrefab;
    [SerializeField] Transform m_WMRRightModelPrefab;

    [SerializeField] Transform m_MetaQuestLeftModelPrefab;
    [SerializeField] Transform m_MetaQuestRightModelPrefab;

    [SerializeField] Transform m_HPReverbLeftModelPrefab;
    [SerializeField] Transform m_HPReverbRightModelPrefab;

    [SerializeField] Material scaleButtonMaterial;
    [SerializeField] Material editButtonMaterial;
    [SerializeField] Material grabButtonMaterial;

    List<HighlightMesh> scaleButtonMeshes = new();
    List<HighlightMesh> editButtonMeshes = new();
    List<HighlightMesh> grabButtonMeshes = new();

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

        //if (Keyboard.current.digit1Key.wasPressedThisFrame)
        //    SetController(ControllerType.Generic);
        //
        //if (Keyboard.current.digit2Key.wasPressedThisFrame)
        //    SetController(ControllerType.ViveWand);
        //
        //if (Keyboard.current.digit3Key.wasPressedThisFrame)
        //    SetController(ControllerType.ViveFocus);
        //
        //if (Keyboard.current.digit4Key.wasPressedThisFrame)
        //    SetController(ControllerType.ValveIndex);
        //
        //if (Keyboard.current.digit5Key.wasPressedThisFrame)
        //    SetController(ControllerType.MetaQuest);
        //
        //if (Keyboard.current.digit6Key.wasPressedThisFrame)
        //    SetController(ControllerType.WMR);
        //
        //if (Keyboard.current.digit7Key.wasPressedThisFrame)
        //    SetController(ControllerType.HPReverb);

        if (Keyboard.current.sKey.wasPressedThisFrame)
            foreach (var m in scaleButtonMeshes)
                m.enabled = !m.enabled;

        if (Keyboard.current.gKey.wasPressedThisFrame)
            foreach (var m in grabButtonMeshes)
                m.enabled = !m.enabled;

        if (Keyboard.current.eKey.wasPressedThisFrame)
            foreach (var m in editButtonMeshes)
                m.enabled = !m.enabled;
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
                var leftComponents = m_LeftParent.gameObject.GetComponentsInChildren<HighlightMesh>();
                var rightComponents = m_RightParent.gameObject.GetComponentsInChildren<HighlightMesh>();
                foreach (var c in leftComponents)
                {
                    if (c.name == "grip_left" || c.name == "grip_right")
                        scaleButtonMeshes.Add(c);

                    if (c.name == "trigger")
                        grabButtonMeshes.Add(c);
                }

                foreach (var c in rightComponents)
                {
                    if (c.name == "grip_left" || c.name == "grip_right")
                        scaleButtonMeshes.Add(c);

                    if (c.name == "trigger")
                        editButtonMeshes.Add(c);
                }
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

        modelSet = true;
    }
}
