/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEngine;

/// <summary>
/// Base class - singleton for elevation manager
/// </summary>
/// <typeparam name="T">Type of elevation manager</typeparam>
public abstract class OnlineMapsElevationManager<T>: OnlineMapsElevationManagerBase
    where T: OnlineMapsElevationManager<T>
{
    public new static T instance
    {
        get
        {
            return _instance as T;
        }
    }

    /// <summary>
    /// Elevation manager is enabled
    /// </summary>
    public static bool isEnabled
    {
        get { return instance.enabled; }
    }

    protected virtual void OnDisable()
    {
        if (map != null) map.Redraw();
    }

    protected virtual void OnDestroy()
    {
        
    }

    protected virtual void OnEnable()
    {
        _instance = (T)this;
        if (control != null)
        {
            control.OnUpdateMeshBefore += UpdateSizeInScene;
        }
        if (map != null) map.Redraw();
    }

    public override void RequestNewElevationData()
    {
        //TODO: Remove this method
    }

    /// <summary>
    /// Sets elevation data
    /// </summary>
    /// <param name="data">Array of elevation data. By default: 32x32.</param>
    public virtual void SetElevationData(short[,] data)
    {
        
    }

    private void UpdateSizeInScene()
    {
        _sizeInScene = control.sizeInScene;
    }
}