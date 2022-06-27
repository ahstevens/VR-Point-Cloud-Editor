/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using UnityEngine;
#if !UNITY_WEBGL
using System.Threading;
#endif

/// <summary>
/// This class manages the background threads.<br/>
/// <strong>Please do not use it if you do not know what you're doing.</strong>
/// </summary>
public class OnlineMapsThreadManager
{
#if !UNITY_WEBGL
    private static OnlineMapsThreadManager instance;

    private bool isAlive;

#if !NETFX_CORE
    private Thread thread;
#else
    private OnlineMapsThreadWINRT thread;
#endif
    private List<Action> threadActions;
#endif

    private static List<Action> mainThreadActions;

    private OnlineMapsThreadManager(Action action)
    {
#if !UNITY_WEBGL
        instance = this;
        threadActions = new List<Action>();
        Add(action);

        isAlive = true;

#if !NETFX_CORE
        thread = new Thread(StartNextAction);
#else
        thread = new OnlineMapsThreadWINRT(StartNextAction);
#endif
        thread.Start();
#endif
    }

    private void Add(Action action)
    {
#if !UNITY_WEBGL
        lock (threadActions)
        {
            threadActions.Add(action);
        }
#else
        action();
#endif
    }

    public static void AddMainThreadAction(Action action)
    {
        if (mainThreadActions == null) mainThreadActions = new List<Action>();
        lock (mainThreadActions)
        {
            mainThreadActions.Add(action);
        }
    }

    /// <summary>
    /// Adds action queued for execution in a separate thread.
    /// </summary>
    /// <param name="action">Action to be executed.</param>
    public static void AddThreadAction(Action action)
    {
#if !UNITY_WEBGL
        if (instance == null) instance = new OnlineMapsThreadManager(action);
        else instance.Add(action);
#else
        throw new Exception("AddThreadAction not supported for WebGL.");
#endif
    }

    /// <summary>
    /// Disposes of thread manager.
    /// </summary>
    public static void Dispose()
    {
#if !UNITY_WEBGL
        if (instance != null)
        {
            instance.isAlive = false;
            instance.thread = null;
            instance = null;
        }
#endif
    }

    public static void ExecuteMainThreadActions()
    {
        if (mainThreadActions == null) return;

        lock (mainThreadActions)
        {
            float startTime = Time.realtimeSinceStartup;
            int i;

            for (i = 0; i < mainThreadActions.Count; i++)
            {
                try
                {
                    mainThreadActions[i].Invoke();
                }
                catch
                {

                }

                if (Time.realtimeSinceStartup - startTime > 0.1)
                {
                    i++;
                    break;
                }
            }

            if (i == mainThreadActions.Count) mainThreadActions.Clear();
            else
            {
                while (i-- > 0)
                {
                    mainThreadActions.RemoveAt(0);
                }
            }
        }
    }

    private void StartNextAction()
    {
#if !UNITY_WEBGL
        while (isAlive)
        {
            bool actionInvoked = false;
            lock (threadActions)
            {
                if (threadActions.Count > 0)
                {
                    Action action = threadActions[0];
                    threadActions.RemoveAt(0);
                    action();
                    actionInvoked = true;
                }
            }
            if (!actionInvoked) OnlineMapsUtils.ThreadSleep(1);
        }
        threadActions = null;
#endif
    }
}