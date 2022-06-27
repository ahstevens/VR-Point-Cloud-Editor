/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;

/// <summary>
/// The base class for working with the web services.
/// </summary>
public abstract class OnlineMapsWebServiceAPI
{
    /// <summary>
    /// Event that occurs when an error response is received from webservice.
    /// </summary>
    public Action<OnlineMapsWebServiceAPI> OnFailed;

    /// <summary>
    /// Event that occurs after OnComplete, when the response from webservice processed.
    /// </summary>
    public Action<OnlineMapsWebServiceAPI> OnFinish;

    /// <summary>
    /// Event that occurs when the current request instance is disposed.
    /// </summary>
    public Action<OnlineMapsWebServiceAPI> OnDispose;

    /// <summary>
    /// Event that occurs when a success response is received from webservice.
    /// </summary>
    public Action<OnlineMapsWebServiceAPI> OnSuccess;

    /// <summary>
    /// In this variable you can put any data that you need to work with requests.
    /// </summary>
    public object customData;

    protected OnlineMapsQueryStatus _status = OnlineMapsQueryStatus.idle;
    protected OnlineMapsWWW www;

    /// <summary>
    /// Gets the current status of the request to webservice.
    /// </summary>
    /// <value>
    /// The status.
    /// </value>
    public OnlineMapsQueryStatus status
    {
        get { return _status; }
    }

    /// <summary>
    /// Destroys the current request to webservice.
    /// </summary>
    public abstract void Destroy();

    /// <summary>
    /// Get request instance
    /// </summary>
    /// <returns>Instance of request</returns>
    public OnlineMapsWWW GetWWW()
    {
        return www;
    }
}