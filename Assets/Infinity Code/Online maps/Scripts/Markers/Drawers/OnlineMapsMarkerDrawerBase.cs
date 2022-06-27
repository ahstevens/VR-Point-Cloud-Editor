/*         INFINITY CODE         */
/*   https://infinity-code.com   */

/// <summary>
/// Base class for drawing markers
/// </summary>
public abstract class OnlineMapsMarkerDrawerBase
{
    protected OnlineMaps map;

    private bool elevationManagerInited = false;
    protected OnlineMapsElevationManagerBase _elevationManager;

    protected OnlineMapsElevationManagerBase elevationManager
    {
        get
        {
            if (!elevationManagerInited)
            {
                elevationManagerInited = true;

                OnlineMapsControlBaseDynamicMesh control = map.control as OnlineMapsControlBaseDynamicMesh;
                if (control != null) _elevationManager = control.elevationManager;
            }

            return _elevationManager;
        }
    }

    protected bool hasElevation
    {
        get
        {
            return elevationManager != null && elevationManager.enabled;
        }
    }

    /// <summary>
    /// Dispose the current drawer
    /// </summary>
    public virtual void Dispose()
    {
        map = null;
        _elevationManager = null;
    }
}