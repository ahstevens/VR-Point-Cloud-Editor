/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEditor;
using UnityEngine;

public abstract class OnlineMapsLocationServiceEditorBase : Editor
{
    private static GUIStyle _toggleStyle;
    private bool showCommonIssues = false;
    private bool showCreateMarker = true;
    private bool showGPSEmulator = true;
    private bool showUpdatePosition = true;
    private SerializedProperty pCreateMarkerInUserPosition;
    private SerializedProperty pMarkerType;
    private SerializedProperty pMarker3DPrefab;
    private SerializedProperty pMarker3DSizeType;
    private SerializedProperty pMarker2DTexture;
    private SerializedProperty pMarker2DAlign;
    private SerializedProperty pMarkerScale;
    private SerializedProperty pMarkerTooltip;
    private SerializedProperty pUseCompassForMarker;
    private SerializedProperty pUseGPSEmulator;
    private SerializedProperty pEmulatorPosition;
    private SerializedProperty pEmulatorCompass;
    private SerializedProperty pCompassThreshold;
    private SerializedProperty pFindLocationByIP;
    private SerializedProperty pUpdatePosition;
    private SerializedProperty pAutoStopUpdateOnInput;
    private SerializedProperty pRestoreAfter;
    private SerializedProperty pDisableEmulatorInPublish;
    private SerializedProperty pLerpCompassValue;
    private SerializedProperty pRotateCameraByCompass;
    private OnlineMapsCameraOrbit cameraOrbit;

    private static GUIStyle toggleStyle
    {
        get
        {
            if (_toggleStyle == null)
            {
                _toggleStyle = new GUIStyle(GUI.skin.toggle);
                _toggleStyle.margin.top = 0;
            }
            return _toggleStyle;
        }
    }

    protected virtual void CacheSerializedProperties()
    {
        if (serializedObject == null) return;
        pCreateMarkerInUserPosition = serializedObject.FindProperty("createMarkerInUserPosition");
        pMarkerType = serializedObject.FindProperty("markerType");
        pMarker3DPrefab = serializedObject.FindProperty("marker3DPrefab");
        pMarker3DSizeType = serializedObject.FindProperty("marker3DSizeType");
        pMarker2DTexture = serializedObject.FindProperty("marker2DTexture");
        pMarker2DAlign = serializedObject.FindProperty("marker2DAlign");
        pMarkerScale = serializedObject.FindProperty("markerScale");
        pMarkerTooltip = serializedObject.FindProperty("markerTooltip");
        pUseCompassForMarker = serializedObject.FindProperty("useCompassForMarker");
        pUseGPSEmulator = serializedObject.FindProperty("useGPSEmulator");
        pDisableEmulatorInPublish = serializedObject.FindProperty("disableEmulatorInPublish");
        pEmulatorPosition = serializedObject.FindProperty("emulatorPosition");
        pEmulatorCompass = serializedObject.FindProperty("emulatorCompass");
        pCompassThreshold = serializedObject.FindProperty("compassThreshold");
        pFindLocationByIP = serializedObject.FindProperty("findLocationByIP");
        pUpdatePosition = serializedObject.FindProperty("updatePosition");
        pAutoStopUpdateOnInput = serializedObject.FindProperty("autoStopUpdateOnInput");
        pRestoreAfter = serializedObject.FindProperty("restoreAfter");
        pLerpCompassValue = serializedObject.FindProperty("lerpCompassValue");
        pRotateCameraByCompass = serializedObject.FindProperty("rotateCameraByCompass");

        cameraOrbit = (target as OnlineMapsLocationServiceBase).GetComponent<OnlineMapsCameraOrbit>();
    }

    public virtual void CustomInspectorGUI()
    {
    }

    public virtual void CustomUpdatePositionGUI()
    {
    }

    private void OnCommonIssues()
    {
        bool lastShowCommonIssues = showCommonIssues;
        if (lastShowCommonIssues)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.BeginHorizontal();
        }
        showCommonIssues = GUILayout.Toggle(showCommonIssues, "Common Issues", EditorStyles.foldout);

        if (lastShowCommonIssues) EditorGUILayout.EndHorizontal();

        if (showCommonIssues)
        {
            EditorGUILayout.LabelField("The most common causes of problems on devices:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("- Application does not have a permission for accessing GPS sensor.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("- The user did not give the application permission to use GPS sensor.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("- GPS is disabled on the device.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("- The user tries to use the application inside a building.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("- AGPS is disabled or not supported on the device, and the user did not wait for GPS initialization (cold start - up to two minutes).", EditorStyles.wordWrappedLabel);
        }

        if (lastShowCommonIssues) EditorGUILayout.EndVertical();
    }

    private void OnCreateMarkerGUI()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);

        bool createMarker = pCreateMarkerInUserPosition.boolValue;
        if (createMarker)
        {
            EditorGUILayout.BeginHorizontal();
            showCreateMarker = GUILayout.Toggle(showCreateMarker, "", EditorStyles.foldout, GUILayout.ExpandWidth(false), GUILayout.Height(16));
        }

        pCreateMarkerInUserPosition.boolValue = GUILayout.Toggle(pCreateMarkerInUserPosition.boolValue, "Create Marker", toggleStyle);

        if (createMarker) EditorGUILayout.EndHorizontal();

        if (pCreateMarkerInUserPosition.boolValue && showCreateMarker)
        {
            pMarkerType.enumValueIndex = EditorGUILayout.Popup("Type", pMarkerType.enumValueIndex, new[] { "2D", "3D" });

            if (pMarkerType.enumValueIndex == (int) OnlineMapsLocationServiceMarkerType.threeD)
            {
                EditorGUILayout.PropertyField(pMarker3DPrefab, new GUIContent("Prefab"));
                EditorGUILayout.PropertyField(pMarker3DSizeType, new GUIContent("Size Type"));
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(pMarker2DTexture, new GUIContent("Texture"));
                if (EditorGUI.EndChangeCheck() && pMarker2DTexture.objectReferenceValue != null) OnlineMapsEditorUtils.CheckMarkerTextureImporter(pMarker2DTexture);
                EditorGUILayout.PropertyField(pMarker2DAlign, new GUIContent("Align"));
            }

            EditorGUILayout.PropertyField(pMarkerTooltip, new GUIContent("Tooltip"));
            EditorGUILayout.PropertyField(pMarkerScale, new GUIContent("Scale"));
            EditorGUILayout.PropertyField(pUseCompassForMarker, new GUIContent("Use Compass"));
        }

        EditorGUILayout.EndVertical();
    }

    private void OnEnable()
    {
        CacheSerializedProperties();
    }

    private void OnGPSEmulatorGUI()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);

        bool useGPSEmulator = pUseGPSEmulator.boolValue;
        if (useGPSEmulator)
        {
            EditorGUILayout.BeginHorizontal();
            showGPSEmulator = GUILayout.Toggle(showGPSEmulator, "", EditorStyles.foldout, GUILayout.ExpandWidth(false));
        }

        pUseGPSEmulator.boolValue = GUILayout.Toggle(pUseGPSEmulator.boolValue, "Use GPS Emulator", toggleStyle);

        if (useGPSEmulator) EditorGUILayout.EndHorizontal();

        if (pUseGPSEmulator.boolValue && showGPSEmulator)
        {
            EditorGUILayout.PropertyField(pDisableEmulatorInPublish);
            if (pDisableEmulatorInPublish.boolValue) EditorGUILayout.HelpBox("The emulator is automatically disabled on the devices and use the data from the sensors.", MessageType.Info);

            if (GUILayout.Button("Copy position from Online Maps"))
            {
                OnlineMaps map = (target as OnlineMapsLocationService).GetComponent<OnlineMaps>();
                if (map != null) pEmulatorPosition.vector2Value = map.position;
            }

            EditorGUI.BeginChangeCheck();

            Vector2 pos = pEmulatorPosition.vector2Value;
            pos.y = EditorGUILayout.FloatField("Latitude", pos.y);
            pos.x = EditorGUILayout.FloatField("Longitude", pos.x);
            if (EditorGUI.EndChangeCheck()) pEmulatorPosition.vector2Value = pos;
            EditorGUILayout.PropertyField(pEmulatorCompass, new GUIContent("Compass (0-360)"));
        }

        EditorGUILayout.EndVertical();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();
        CustomInspectorGUI();

        EditorGUILayout.PropertyField(pCompassThreshold);
        EditorGUILayout.PropertyField(pLerpCompassValue);

        if (cameraOrbit != null)
        {
            float oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 170;
            EditorGUILayout.PropertyField(pRotateCameraByCompass);
            EditorGUIUtility.labelWidth = oldWidth;
        }

        EditorGUILayout.PropertyField(pFindLocationByIP);

        OnUpdatePositionGUI();
        OnCreateMarkerGUI();
        OnGPSEmulatorGUI();

        serializedObject.ApplyModifiedProperties();

        OnCommonIssues();

        if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(target);
    }

    private void OnUpdatePositionGUI()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);


        bool updatePosition = pUpdatePosition.boolValue;
        if (updatePosition)
        {
            EditorGUILayout.BeginHorizontal();
            showUpdatePosition = GUILayout.Toggle(showUpdatePosition, "", EditorStyles.foldout, GUILayout.ExpandWidth(false), GUILayout.Height(16));
        }

        pUpdatePosition.boolValue = GUILayout.Toggle(pUpdatePosition.boolValue, "Update Map Position", toggleStyle);

        if (updatePosition) EditorGUILayout.EndHorizontal();

        if (pUpdatePosition.boolValue && showUpdatePosition)
        {
            CustomUpdatePositionGUI();
            EditorGUILayout.PropertyField(pAutoStopUpdateOnInput, new GUIContent("Auto Stop On Input"));

            bool restoreAfter = pRestoreAfter.intValue != 0;
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            restoreAfter = GUILayout.Toggle(restoreAfter, "", GUILayout.ExpandWidth(false));
            if (EditorGUI.EndChangeCheck()) pRestoreAfter.intValue = restoreAfter ? 10 : 0;
            EditorGUI.BeginDisabledGroup(!restoreAfter);
            EditorGUILayout.PropertyField(pRestoreAfter, new GUIContent("Restore After (sec)"));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }
}
