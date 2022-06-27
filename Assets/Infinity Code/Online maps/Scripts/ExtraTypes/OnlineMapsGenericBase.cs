/*         INFINITY CODE         */
/*   https://infinity-code.com   */

/// <summary>
/// Base class for generic singleton
/// </summary>
/// <typeparam name="T">Type of singleton</typeparam>
public abstract class OnlineMapsGenericBase<T> where T : OnlineMapsGenericBase<T>
{
    /// <summary>
    /// Instance of the class
    /// </summary>
    protected static T _instance;

    /// <summary>
    /// Instance of the class
    /// </summary>
    public static T instance
    {
        get { return _instance; }
    }

    /// <summary>
    /// Constructor
    /// </summary>
    protected OnlineMapsGenericBase()
    {
        _instance = (T)this;
    }
}