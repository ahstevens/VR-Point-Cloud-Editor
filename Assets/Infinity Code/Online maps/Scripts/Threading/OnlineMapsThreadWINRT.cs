/*         INFINITY CODE         */
/*   https://infinity-code.com   */

#if NETFX_CORE
namespace System.Threading
{
    using System;
    using Tasks;

    public sealed class OnlineMapsThreadWINRT : IDisposable
    {
        public delegate void ParameterizedThreadStart(object obj);
        public delegate void ThreadStart();

        private CancellationTokenSource _cancellationTokenSource;
        private Task _task;
        private ParameterizedThreadStart _parameterizedThreadStart;
        private ThreadStart _threadStart;

        public OnlineMapsThreadWINRT(ParameterizedThreadStart start)
        {
            _parameterizedThreadStart = start;
            _threadStart = null;
            _task = null;
            _cancellationTokenSource = null;
        }

        public OnlineMapsThreadWINRT(ThreadStart start)
        {
            _threadStart = start;
            _parameterizedThreadStart = null;
            _task = null;
            _cancellationTokenSource = null;
        }

        public void Abort()
        {
            if (_task != null && !_task.IsCompleted && _cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }
            _task = null;
        }

        public static void Sleep(int millisecondsTimeout)
        {
            Task.Delay(millisecondsTimeout).Wait();
        }

        public static void Sleep(TimeSpan timeout)
        {
            Task.Delay(timeout).Wait();
        }

        public void Start()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Dispose();
            }
            _cancellationTokenSource = new CancellationTokenSource();
            _task = new Task(new Action(_threadStart), _cancellationTokenSource.Token);
            _task.Start();
        }

        public void Start(object parameter)
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Dispose();
            }
            _cancellationTokenSource = new CancellationTokenSource();
            _task = new Task(new Action<object>(_parameterizedThreadStart), parameter, _cancellationTokenSource.Token);
            _task.Start();
        }

        public void Dispose()
        {
            Abort();
        }
    }
}

#endif