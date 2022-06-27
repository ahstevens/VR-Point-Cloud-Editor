/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;

/// <summary>
/// Mark a component as a plugin for Online Maps
/// </summary>
public class OnlineMapsPluginAttribute: Attribute
{
    /// <summary>
    /// Enabled by default?
    /// </summary>
    public readonly bool enabledByDefault;

    /// <summary>
    /// Title of plugin
    /// </summary>
    public readonly string title;

    /// <summary>
    /// Required type of control
    /// </summary>
    public readonly Type requiredType;

    /// <summary>
    /// Group of plugins
    /// </summary>
    public readonly string group;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="title">Title of plugin</param>
    /// <param name="requiredType">Required type of control</param>
    /// <param name="enabledByDefault">Enabled?</param>
    public OnlineMapsPluginAttribute(string title, Type requiredType, bool enabledByDefault = false)
    {
        this.enabledByDefault = enabledByDefault;
        this.title = title;
        this.requiredType = requiredType;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="title">Title of plugin</param>
    /// <param name="requiredType">Required type of control</param>
    /// <param name="group">Group of plugin</param>
    public OnlineMapsPluginAttribute(string title, Type requiredType, string group): this(title, requiredType)
    {
        this.group = group;
    }
}