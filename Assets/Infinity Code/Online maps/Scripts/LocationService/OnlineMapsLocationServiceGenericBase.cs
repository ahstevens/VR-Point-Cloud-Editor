/*         INFINITY CODE         */
/*   https://infinity-code.com   */

/// <summary>
/// Controls map using Location Service.
/// </summary>
public abstract class OnlineMapsLocationServiceGenericBase<T> : OnlineMapsLocationServiceBase where T : OnlineMapsLocationServiceGenericBase<T>
{
    private static T _instance;

    /// <summary>
    /// Instance of LocationService.
    /// </summary>
    public static T instance
    {
        get { return _instance; }
    }

    protected override void OnEnable()
    {
        _instance = (T)this;
        base.OnEnable();
    }
}
