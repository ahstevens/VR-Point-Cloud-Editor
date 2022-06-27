/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

/// <summary>
/// The wrapper for an array of JSON elements.
/// </summary>
public class OnlineMapsJSONArray : OnlineMapsJSONItem
{
    private List<OnlineMapsJSONItem> array;
    private int _count;

    /// <summary>
    /// Count elements
    /// </summary>
    public int count
    {
        get { return _count; }
    }

    public override OnlineMapsJSONItem this[int index]
    {
        get
        {
            if (index < 0 || index >= _count) return null;
            return array[index];
        }
    }

    
    public override OnlineMapsJSONItem this[string key]
    {
        get { return Get(key); }
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public OnlineMapsJSONArray()
    {
        array = new List<OnlineMapsJSONItem>();
    }

    /// <summary>
    /// Adds an element to the array.
    /// </summary>
    /// <param name="item">Element</param>
    public void Add(OnlineMapsJSONItem item)
    {
        array.Add(item);
        _count++;
    }

    /// <summary>
    /// Adds an elements to the array.
    /// </summary>
    /// <param name="collection">Array of elements</param>
    public void AddRange(OnlineMapsJSONArray collection)
    {
        if (collection == null) return;
        array.AddRange(collection.array);
        _count += collection._count;
    }

    public void AddRange(OnlineMapsJSONItem collection)
    {
        AddRange(collection as OnlineMapsJSONArray);
    }

    public override object Deserialize(Type type)
    {
        if (_count == 0) return null;

        if (type.IsArray)
        {
            Type elementType = type.GetElementType();
            Array v = Array.CreateInstance(elementType, _count);
            if (array[0] is OnlineMapsJSONObject)
            {
                IEnumerable<MemberInfo> members = OnlineMapsReflectionHelper.GetMembers(elementType, BindingFlags.Instance | BindingFlags.Public);
                for (int i = 0; i < _count; i++)
                {
                    OnlineMapsJSONItem child = array[i];
                    object item = (child as OnlineMapsJSONObject).Deserialize(elementType, members);
                    v.SetValue(item, i);
                }
            }
            else
            {
                for (int i = 0; i < _count; i++)
                {
                    OnlineMapsJSONItem child = array[i];
                    object item = child.Deserialize(elementType);
                    v.SetValue(item, i);
                }
            }

            return v;
        }
        if (OnlineMapsReflectionHelper.IsGenericType(type))
        {
            Type listType = OnlineMapsReflectionHelper.GetGenericArguments(type)[0];
            object v = Activator.CreateInstance(type);

            if (array[0] is OnlineMapsJSONObject)
            {
                IEnumerable<MemberInfo> members = OnlineMapsReflectionHelper.GetMembers(listType, BindingFlags.Instance | BindingFlags.Public);
                for (int i = 0; i < _count; i++)
                {
                    OnlineMapsJSONItem child = array[i];
                    object item = (child as OnlineMapsJSONObject).Deserialize(listType, members);
                    try
                    {
                        MethodInfo methodInfo = OnlineMapsReflectionHelper.GetMethod(type, "Add");
                        if (methodInfo != null) methodInfo.Invoke(v, new[] { item });
                    }
                    catch
                    {
                    }
                }
            }
            else
            {
                for (int i = 0; i < _count; i++)
                {
                    OnlineMapsJSONItem child = array[i];
                    object item = child.Deserialize(listType);
                    try
                    {
                        MethodInfo methodInfo = OnlineMapsReflectionHelper.GetMethod(type, "Add");
                        if (methodInfo != null) methodInfo.Invoke(v, new[] { item });
                    }
                    catch
                    {
                    }
                }
            }

            return v;
        }


        return null;
    }

    private OnlineMapsJSONItem Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        if (key.StartsWith("//"))
        {
            string k = key.Substring(2);
            if (string.IsNullOrEmpty(k) || k.StartsWith("//")) return null;
            return GetAll(k);
        }
        return GetThis(key);
    }

    private OnlineMapsJSONItem GetThis(string key)
    {
        int kindex;

        if (key.Contains("/"))
        {
            int index = key.IndexOf("/");
            string k = key.Substring(0, index);
            string nextPart = key.Substring(index + 1);

            if (k == "*")
            {
                OnlineMapsJSONArray arr = new OnlineMapsJSONArray();
                for (int i = 0; i < _count; i++)
                {
                    OnlineMapsJSONItem item = array[i][nextPart];
                    if (item != null) arr.Add(item);
                }
                return arr;
            }
            if (int.TryParse(k, out kindex))
            {
                if (kindex < 0 || kindex >= _count) return null;
                OnlineMapsJSONItem item = array[kindex];
                return item[nextPart];
            }
        }
        if (key == "*") return this;
        if (int.TryParse(key, out kindex)) return this[kindex];
        return null;
    }


    public override OnlineMapsJSONItem GetAll(string k)
    {
        OnlineMapsJSONItem item = GetThis(k);
        OnlineMapsJSONArray arr = null;
        if (item != null)
        {
            arr = new OnlineMapsJSONArray();
            arr.Add(item);
        }
        for (int i = 0; i < _count; i++)
        {
            item = array[i];
            OnlineMapsJSONArray subArr = item.GetAll(k) as OnlineMapsJSONArray;
            if (subArr != null)
            {
                if (arr == null) arr = new OnlineMapsJSONArray();
                arr.AddRange(subArr);
            }
        }
        return arr;
    }

    public override IEnumerator<OnlineMapsJSONItem> GetEnumerator()
    {
        return array.GetEnumerator();
    }

    /// <summary>
    /// Parse a string that contains an array
    /// </summary>
    /// <param name="json">JSON string</param>
    /// <returns>Instance</returns>
    public static OnlineMapsJSONArray ParseArray(string json)
    {
        return OnlineMapsJSON.Parse(json) as OnlineMapsJSONArray;
    }

    public override void ToJSON(StringBuilder b)
    {
        b.Append("[");
        for (int i = 0; i < _count; i++)
        {
            if (i != 0) b.Append(",");
            array[i].ToJSON(b);
        }
        b.Append("]");
    }

    public override object Value(Type type)
    {
        if (OnlineMapsReflectionHelper.IsValueType(type)) return Activator.CreateInstance(type);
        return null;

    }
}