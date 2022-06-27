/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;

/// <summary>
/// Base class for markers managers
/// </summary>
/// <typeparam name="T">Subclass of OnlineMapsMarkerManagerBase</typeparam>
/// <typeparam name="U">Type of markers</typeparam>
[Serializable]
public abstract class OnlineMapsMarkerManagerBase<T, U>: OnlineMapsInteractiveElementManager<T, U>, IOnlineMapsSavableComponent
    where T : OnlineMapsMarkerManagerBase<T, U>
    where U: OnlineMapsMarkerBase
{
    /// <summary>
    /// Called when a marker is created
    /// </summary>
    public Action<U> OnCreateItem;

    protected OnlineMapsSavableItem[] savableItems;

    /// <summary>
    /// Scaling of 3D markers by default
    /// </summary>
    public float defaultScale = 1;

    public static void RemoveItemsByTag(params string[] tags)
    {
        if (instance != null) instance.RemoveByTag(tags);
    }

    protected U _CreateItem(double longitude, double latitude)
    {
        U item = Activator.CreateInstance<U>();
        item.SetPosition(longitude, latitude);
        items.Add(item);
        if (OnCreateItem != null) OnCreateItem(item);
        return item;
    }

    public abstract OnlineMapsSavableItem[] GetSavableItems();

    protected override void OnEnable()
    {
        base.OnEnable();

        _instance = (T)this;
    }

    public void RemoveByTag(params string[] tags)
    {
        if (tags.Length == 0) return;

        RemoveAll(m =>
        {
            for (int j = 0; j < tags.Length; j++) if (m.tags.Contains(tags[j])) return true;
            return false;
        });
    }

    protected virtual OnlineMapsJSONItem SaveSettings()
    {
        OnlineMapsJSONArray jitems = new OnlineMapsJSONArray();
        foreach (U marker in items) jitems.Add(marker.ToJSON());
        OnlineMapsJSONObject json = new OnlineMapsJSONObject();
        json.Add("settings", new OnlineMapsJSONObject());
        json.Add("items", jitems);
        return json;
    }

    protected virtual void Start()
    {
        
    }

    protected virtual void Update()
    {
        
    }
}