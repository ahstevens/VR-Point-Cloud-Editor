/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for interactive element managers
/// </summary>
/// <typeparam name="T">Type of manager</typeparam>
/// <typeparam name="U">Type of interactive element</typeparam>
[Serializable]
public abstract class OnlineMapsInteractiveElementManager<T, U>: MonoBehaviour, IEnumerable<U>, IOnlineMapsInteractiveElementManager
    where T: OnlineMapsInteractiveElementManager<T, U>
    where U : IOnlineMapsInteractiveElement
{
    /// <summary>
    /// The event that occurs when an item is added.
    /// </summary>
    public static Action<U> OnAddItem;

    /// <summary>
    /// The event that occurs when an item is removed.
    /// </summary>
    public static Action<U> OnRemoveItem;

    protected static T _instance;

    [SerializeField]
    protected List<U> _items;

    protected OnlineMaps _map;

    /// <summary>
    /// Instance of the manager
    /// </summary>
    public static T instance
    {
        get
        {
            if (_instance == null && OnlineMaps.isPlaying) Init();
            return _instance;
        }
    }

    /// <summary>
    /// Gets / sets the list of items
    /// </summary>
    public List<U> items
    {
        get
        {
            if (_items == null) _items = new List<U>();
            return _items;
        }
        set
        {
            _items = new List<U>(value);
        }
    }

    public OnlineMaps map
    {
        get { return _map; }
    }

    /// <summary>
    /// Count items
    /// </summary>
    public static int CountItems
    {
        get { return instance.Count; }
    }

    /// <summary>
    /// Count items
    /// </summary>
    public int Count
    {
        get { return items.Count; }
    }

    /// <summary>
    /// Gets / sets item by index
    /// </summary>
    /// <param name="index">Index of the item</param>
    /// <returns>Item</returns>
    public U this[int index]
    {
        get { return items[index]; }
        set { items[index] = value; }
    }

    /// <summary>
    /// Adds an item to the list
    /// </summary>
    /// <param name="item">Item</param>
    /// <returns>Item</returns>
    public static U AddItem(U item)
    {
        if (instance != null) instance.Add(item);
        return item;
    }

    /// <summary>
    /// Adds items to the list
    /// </summary>
    /// <param name="collection">Collection of items</param>
    public static void AddItems(IEnumerable<U> collection)
    {
        if (instance != null) instance.AddRange(collection);
    }

    /// <summary>
    /// Initializes the manager
    /// </summary>
    public static void Init()
    {
        _instance = FindObjectOfType<T>();
        if (_instance == null)
        {
            OnlineMaps map = FindObjectOfType<OnlineMaps>();
            if (map != null) map.gameObject.AddComponent<T>();
        }
    }

    /// <summary>
    /// Remove all items
    /// </summary>
    public static void RemoveAllItems()
    {
        if (instance != null) instance.RemoveAll();
    }

    /// <summary>
    /// Removes all items which matches the predicate
    /// </summary>
    /// <param name="match">Predicate</param>
    public static void RemoveAllItems(Predicate<U> match)
    {
        if (instance != null) instance.items.RemoveAll(match);
    }

    /// <summary>
    /// Remove an item
    /// </summary>
    /// <param name="item">Item</param>
    /// <param name="dispose">Dispose the item</param>
    /// <returns>True - success, false - otherwise</returns>
    public static bool RemoveItem(U item, bool dispose = true)
    {
        if (instance != null) return instance.Remove(item, dispose);
        return false;
    }

    /// <summary>
    /// Remove an item by index
    /// </summary>
    /// <param name="index">Index of item</param>
    /// <returns>Item that was removed</returns>
    public static U RemoveItemAt(int index)
    {
        if (instance != null) return instance.RemoveAt(index);
        return default(U);
    }

    protected static void Redraw()
    {
        if (_instance != null && _instance._map != null) _instance._map.Redraw();
    }

    /// <summary>
    /// Sets the collection of items
    /// </summary>
    /// <param name="collection">Collection of items</param>
    public static void SetItems(IEnumerable<U> collection)
    {
        if (instance != null)
        {
            instance.items = new List<U>(collection);
            foreach (U el in instance.items) el.manager = instance;
        }
    }

    /// <summary>
    /// Adds an item to the list
    /// </summary>
    /// <param name="item">Item</param>
    /// <returns>Item</returns>
    public U Add(U item)
    {
        items.Add(item);
        item.manager = this;
        if (OnAddItem != null) OnAddItem(item);
        Redraw();
        return item;
    }

    /// <summary>
    /// Adds items to the list
    /// </summary>
    /// <param name="collection">Collection of items</param>
    public void AddRange(IEnumerable<U> collection)
    {
        items.AddRange(collection);
        foreach (U item in collection)
        {
            item.manager = this;
            if (OnAddItem != null) OnAddItem(item);
        }
        Redraw();
    }

    public IEnumerator GetEnumerator()
    {
        return items.GetEnumerator();
    }

    IEnumerator<U> IEnumerable<U>.GetEnumerator()
    {
        return items.GetEnumerator();
    }

    public int IndexOf(IOnlineMapsInteractiveElement element)
    {
        return items.IndexOf((U)element);
    }

    protected virtual void OnDisable()
    {
        _map = null;
    }

    protected virtual void OnEnable()
    {
        _map = GetComponent<OnlineMaps>();
    }

    /// <summary>
    /// Remove an item
    /// </summary>
    /// <param name="item">Item</param>
    /// <param name="dispose">Dispose the item</param>
    /// <returns>True - success, false - otherwise</returns>
    public bool Remove(U item, bool dispose = true)
    {
        if (OnRemoveItem != null) OnRemoveItem(item);
        item.DestroyInstance();
        if (dispose) item.Dispose();
        Redraw();
        return items.Remove(item);
    }

    /// <summary>
    /// Remove all items
    /// </summary>
    /// <param name="dispose">Dispose the item</param>
    public void RemoveAll(bool dispose = true)
    {
        foreach (U item in items)
        {
            if (OnRemoveItem != null) OnRemoveItem(item);
            item.DestroyInstance();
            if (dispose) item.Dispose();
        }
        items.Clear();
        Redraw();
    }

    /// <summary>
    /// Removes all items which matches the predicate
    /// </summary>
    /// <param name="match">Predicate</param>
    /// <param name="dispose">Dispose the item</param>
    public void RemoveAll(Predicate<U> match, bool dispose = true)
    {
        for (int i = Count - 1; i >= 0; i--)
        {
            U item = items[i];
            if (match(item))
            {
                if (OnRemoveItem != null) OnRemoveItem(item);
                item.DestroyInstance();
                if (dispose) item.Dispose();
                items.RemoveAt(i);
            }
        }

        Redraw();
    }

    /// <summary>
    /// Remove an item by index
    /// </summary>
    /// <param name="index">Index of item</param>
    /// <param name="dispose">Dispose the item</param>
    /// <returns>Item that was removed</returns>
    public U RemoveAt(int index, bool dispose = true)
    {
        if (index < 0 || index >= items.Count) return default(U);
        U item = items[index];
        if (OnRemoveItem != null) OnRemoveItem(item);
        item.DestroyInstance();
        if (dispose) item.Dispose();
        items.RemoveAt(index);
        Redraw();
        return item;
    }
}