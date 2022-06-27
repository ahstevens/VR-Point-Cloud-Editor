/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public abstract class OnlineMapsFormattedEditor : Editor
{
    protected LayoutItem rootLayoutItem;

    protected abstract void CacheSerializedFields();

    protected virtual void GenerateLayoutItems()
    {
        rootLayoutItem = new LayoutItem("root");
    }

    protected virtual void OnDisable()
    {
        rootLayoutItem.Dispose();
        rootLayoutItem = null;
    }

    protected virtual void OnEnable()
    {
        serializedObject.Update();

        OnEnableBefore();
        CacheSerializedFields();
        GenerateLayoutItems();
        OnEnableLate();

        serializedObject.ApplyModifiedProperties();

        rootLayoutItem.Sort();
    }

    protected virtual void OnEnableBefore()
    {
        
    }

    protected virtual void OnEnableLate()
    {
        
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        float oldWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 170;

        EditorGUI.BeginChangeCheck();

        rootLayoutItem.Draw();

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();

            OnSetDirty();
        }

        EditorGUIUtility.labelWidth = oldWidth;
    }

    protected virtual void OnSetDirty()
    {
        if (!OnlineMaps.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }

    protected class LayoutItem
    {
        public Action OnChanged;
        public Action OnChangedInPlaymode;
        public Action<LayoutItem> OnChildChanged;
        public Func<bool> OnValidateDraw;
        public Func<bool> OnValidateDrawChilds;

        public string id;
        public SerializedProperty property;
        public GUIContent content;
        public Action action;
        public bool? drawGroupBorder;
        public Group drawGroup = Group.none;
        public bool disabledInPlaymode = false;

        public List<LayoutItem> childs;

        private float _priority;
        private int nextIntPriority;

        public float priority
        {
            get { return _priority; }
            set
            {
                _priority = value;
                if ((int)value >= nextIntPriority) nextIntPriority = (int)value + 1;
            }
        }

        public LayoutItem this[string childID]
        {
            get
            {
                if (childs == null) return null;

                string cid = childID;
                int slashIndex = cid.IndexOf('\\');
                if (slashIndex != -1)
                {
                    cid = childID.Substring(0, slashIndex);
                    childID = childID.Substring(slashIndex + 1);
                }

                foreach (LayoutItem child in childs)
                {
                    if (child.id == cid)
                    {
                        if (slashIndex != -1) return child[childID];
                        return child;
                    }
                }
                return null;
            }
        }

        public LayoutItem(string id)
        {
            this.id = id;
            _priority = nextIntPriority;
            nextIntPriority++;
        }

        public LayoutItem(SerializedProperty property) : this(property.name)
        {
            this.property = property;
        }

        public LayoutItem Add(LayoutItem item)
        {
            if (childs == null) childs = new List<LayoutItem>();
            childs.Add(item);
            return item;
        }

        public LayoutItem Create(string id)
        {
            return Add(new LayoutItem(id));
        }

        public LayoutItem Create(string id, Action action)
        {
            LayoutItem item = Add(new LayoutItem(id));
            item.action = action;
            return item;
        }

        public LayoutItem Create(SerializedProperty property)
        {
            return Add(new LayoutItem(property));
        }

        public void Dispose()
        {
            property = null;
            content = null;
            OnChanged = null;
            OnChangedInPlaymode = null;
            OnChildChanged = null;
            OnValidateDraw = null;
            OnValidateDrawChilds = null;

            if (childs != null)
            {
                foreach (LayoutItem item in childs) item.Dispose();
                childs = null;
            }
        }

        public void Draw()
        {
            if (OnValidateDraw != null && !OnValidateDraw()) return;

            bool hasChilds = childs != null && childs.Count > 0;
            bool needDrawChilds = hasChilds;
            if (drawGroup == Group.validated) needDrawChilds = hasChilds && (OnValidateDrawChilds == null || OnValidateDrawChilds());
            else if (drawGroup == Group.valueOn) needDrawChilds = hasChilds && property.propertyType == SerializedPropertyType.Boolean && property.boolValue;

            bool needDrawGroup = drawGroup != Group.none && needDrawChilds;
            if (drawGroupBorder.HasValue) needDrawGroup = drawGroupBorder.Value;

            if (needDrawGroup) EditorGUILayout.BeginVertical(GUI.skin.box);
            if (disabledInPlaymode) EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

            if (property != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(property, content);
                if (EditorGUI.EndChangeCheck())
                {
                    if (OnChanged != null) OnChanged();
                    if (EditorApplication.isPlaying && OnChangedInPlaymode != null) OnChangedInPlaymode();
                }
            }
            if (action != null) action();
            if (needDrawChilds)
            {
                foreach (LayoutItem item in childs)
                {
                    EditorGUI.BeginChangeCheck();
                    item.Draw();
                    if (EditorGUI.EndChangeCheck() && OnChildChanged != null) OnChildChanged(item);
                }
            }

            if (disabledInPlaymode) EditorGUI.EndDisabledGroup();
            if (needDrawGroup) EditorGUILayout.EndVertical();
        }

        public void Remove(string id)
        {
            if (childs == null) return;
            childs.RemoveAll(delegate (LayoutItem c)
            {
                if (c.id == id)
                {
                    c.Dispose();
                    return true;
                }
                return false;
            });
        }

        public void Sort()
        {
            if (childs == null) return;

            childs = childs.OrderBy(c => c.priority).ToList();
            foreach (LayoutItem child in childs) child.Sort();
        }

        public enum Group
        {
            none,
            valueOn,
            validated,
            always
        }
    }
}