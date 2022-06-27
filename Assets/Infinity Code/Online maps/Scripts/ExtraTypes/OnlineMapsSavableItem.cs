/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;

/// <summary>
/// Item of the plugin, the state of which can be saved
/// </summary>
public class OnlineMapsSavableItem
{
    /// <summary>
    /// Whether save is selected
    /// </summary>
    public bool enabled = true;

    /// <summary>
    /// Called when saving state
    /// </summary>
    public Action invokeCallback;

    /// <summary>
    /// Callback that is called to save
    /// </summary>
    public Func<OnlineMapsJSONItem> jsonCallback;

    /// <summary>
    /// Callback that is called to load
    /// </summary>
    public Action<OnlineMapsJSONObject> loadCallback;

    /// <summary>
    /// The label that is shown to the user
    /// </summary>
    public string label;

    /// <summary>
    /// Name of item
    /// </summary>
    public string name;

    /// <summary>
    /// Priority
    /// </summary>
    public int priority;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="name">Name of item</param>
    /// <param name="label">The label that is shown to the user</param>
    /// <param name="jsonCallback">Callback that is called to save</param>
    public OnlineMapsSavableItem(string name, string label, Func<OnlineMapsJSONItem> jsonCallback)
    {
        this.name = name;
        this.label = label;
        this.jsonCallback = jsonCallback;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="label">The label that is shown to the user</param>
    /// <param name="invokeCallback">Callback that is called to save</param>
    public OnlineMapsSavableItem(string label, Action invokeCallback)
    {
        this.label = label;
        this.invokeCallback = invokeCallback;
    }
}