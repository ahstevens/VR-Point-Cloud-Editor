/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class OnlineMapsPrefs
{
    private const string prefsKey = "OM_Settings";

    static OnlineMapsPrefs()
    {
#if !UNITY_2017_2_OR_NEWER
        EditorApplication.playmodeStateChanged += PlaymodeStateChanged;
#else
        EditorApplication.playModeStateChanged += PlaymodeStateChanged;
#endif
    }

    private static bool Exists()
    {
        return EditorPrefs.HasKey(prefsKey);
    }

    public static Object GetObject(int tid)
    {
        if (tid == 0) return null;
        return EditorUtility.InstanceIDToObject(tid);
    }

#if !UNITY_2017_2_OR_NEWER
    private static void PlaymodeStateChanged()
#else
    private static void PlaymodeStateChanged(PlayModeStateChange playModeStateChange)
#endif
    {
        if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (Exists())
            {
#pragma warning disable 618
                OnlineMaps map = ((OnlineMaps[])Object.FindSceneObjectsOfType(typeof(OnlineMaps))).FirstOrDefault();
#pragma warning restore 618
                if (map != null)
                {
                    try
                    {
                        OnlineMaps.isPlaying = false;
                        IOnlineMapsSavableComponent[] savableComponents = map.GetComponents<IOnlineMapsSavableComponent>();
                        OnlineMapsSavableItem[] savableItems = savableComponents.SelectMany(c => c.GetSavableItems()).ToArray();
                        if (savableItems.Length != 0)
                        {
                            string prefs = EditorPrefs.GetString(prefsKey);
                            OnlineMapsJSONObject json = OnlineMapsJSON.Parse(prefs) as OnlineMapsJSONObject;
                            foreach (KeyValuePair<string, OnlineMapsJSONItem> pair in json.table)
                            {
                                OnlineMapsSavableItem savableItem = savableItems.FirstOrDefault(s => s.name == pair.Key);
                                if (savableItem != null && savableItem.loadCallback != null) savableItem.loadCallback(pair.Value as OnlineMapsJSONObject);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        // ignored
                    }

                    EditorPrefs.DeleteKey(prefsKey);
                    EditorUtility.SetDirty(map);
                }
            }

            if (EditorPrefs.HasKey("OnlineMapsRefreshAssets"))
            {
                EditorPrefs.DeleteKey("OnlineMapsRefreshAssets");
                AssetDatabase.Refresh();
            }
        }
    }

    public static void Save(string data)
    {
        EditorPrefs.SetString(prefsKey, data);
    }
}