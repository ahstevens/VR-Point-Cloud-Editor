/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public abstract class OnlineMapsControlBaseUIEditor<T, U> : OnlineMapsControlBaseEditor<T>
    where T : OnlineMapsControlBaseUI<U>
    where U : MaskableGraphic
{
#if !CURVEDUI
    protected void DrawCurvedUIWarning()
    {
        EditorGUILayout.HelpBox("To make the map work properly with Curved UI, enable integration.", MessageType.Info);
        if (GUILayout.Button("Enable Curved UI")) OnlineMapsEditor.AddCompilerDirective("CURVEDUI");
    }
#endif

    protected override void GenerateLayoutItems()
    {
        base.GenerateLayoutItems();

#if !CURVEDUI
        Type[] types = control.GetType().Assembly.GetTypes();
        for (int i = 0; i < types.Length; i++)
        {
            if (types[i].Namespace == "CurvedUI")
            {
                warningLayoutItem.Create("curvedUIWarning", DrawCurvedUIWarning);
                break;
            }
        }
#endif
    }
}