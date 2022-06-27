/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

/// <summary>
/// Map log manager
/// </summary>
[OnlineMapsPlugin("Log", typeof(OnlineMapsControlBase))]
[AddComponentMenu("Infinity Code/Online Maps/Plugins/Log")]
public class OnlineMapsLog: MonoBehaviour
{
    private static OnlineMapsLog _instance;
    private static bool missed = false;

    [Tooltip("Display events using IMGUI.")]
    public bool logOnUI = false;

    [Header("Event Types")]
    public bool cacheEvents = false;
    [Tooltip("Shows the events of interactive elements that have subscriptions")]
    public bool interactiveElementEvents = false;
    [Tooltip("Shows the events of map that have subscriptions")]
    public bool mapEvents = false;
    [FormerlySerializedAs("showRequests")]
    public bool requestEvents = false;

    private static List<string> messages = new List<string>();

    public static OnlineMapsLog instance
    {
        get
        {
            if (_instance == null && !missed)
            {
                _instance = FindObjectOfType<OnlineMapsLog>();
                missed = _instance == null;
            }

            return _instance;
        }
    }

    public static void Info(string message, Type type)
    {
        if (!ValidateType(type)) return;

        Debug.Log(message);
        AddUIMessage(message);
    }

    private static void AddUIMessage(string message)
    {
        if (!instance.logOnUI) return;

        while (messages.Count > 19)
        {
            messages.RemoveAt(messages.Count - 1);
        }

        messages.Insert(0, message);
    }

    private void OnEnable()
    {
        _instance = this;
    }

    private void OnGUI()
    {
        if (!logOnUI) return;

        foreach (string message in messages)
        {
            GUILayout.Label(message);
        }
    }

    private static bool ValidateType(Type type)
    {
        if (instance == null) return false;

        switch (type)
        {
            case Type.request:
                return instance.requestEvents;
            case Type.cache:
                return instance.cacheEvents;
            case Type.interactiveElement:
                return instance.interactiveElementEvents;
            case Type.map:
                return instance.mapEvents;
            default:
                return false;
        }
    }

    public static void Warning(string message, Type type)
    {
        if (!ValidateType(type)) return;

        Debug.LogWarning(message);
        AddUIMessage("[WARNING] " + message);
    }

    public enum Type
    {
        cache,
        interactiveElement,
        map,
        request
    }
}