using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.Foundation;

namespace UnoAppExample
{
    // https://github.com/xamarin/Essentials/blob/main/Xamarin.Essentials/MainThread/MainThread.uwp.cs
    // https://github.com/xamarin/Essentials/blob/main/Xamarin.Essentials/MainThread/MainThreadExtensions.uwp.cs
    // https://github.com/xamarin/Essentials/blob/main/Xamarin.Essentials/MainThread/MainThread.shared.cs

    public static class MainThread
    {
        static bool PlatformIsMainThread
        {
            get
            {
                // if there is no main window, then this is either a service
                // or the UI is not yet constructed, so the main thread is the
                // current thread
                try
                {
                    if (CoreApplication.MainView?.CoreWindow == null)
                        return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Unable to validate MainView creation. {ex.Message}");
                    return true;
                }

                return CoreApplication.MainView.CoreWindow.Dispatcher?.HasThreadAccess ?? false;
            }
        }

        static void PlatformBeginInvokeOnMainThread(Action action)
        {
            var dispatcher = CoreApplication.MainView?.CoreWindow?.Dispatcher;

            if (dispatcher == null)
                throw new InvalidOperationException("Unable to find main thread.");
            dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action()).WatchForError();
        }

        internal static void WatchForError(this IAsyncAction self) =>
            self.AsTask().WatchForError();

        internal static void WatchForError<T>(this IAsyncOperation<T> self) =>
            self.AsTask().WatchForError();

        internal static void WatchForError(this Task self)
        {
            var context = SynchronizationContext.Current;
            if (context == null)
                return;

            self.ContinueWith(
                t =>
                {
                    var exception = t.Exception.InnerExceptions.Count > 1 ? t.Exception : t.Exception.InnerException;

                    context.Post(e => { throw (Exception)e; }, exception);
                }, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }

        public static bool IsMainThread =>
         PlatformIsMainThread;

        public static void BeginInvokeOnMainThread(Action action)
        {
            if (IsMainThread)
            {
                action();
            }
            else
            {
                PlatformBeginInvokeOnMainThread(action);
            }
        }

        public static Task InvokeOnMainThreadAsync(Action action)
        {
            if (IsMainThread)
            {
                action();
#if NETSTANDARD1_0
                return Task.FromResult(true);
#else
                return Task.CompletedTask;
#endif
            }

            var tcs = new TaskCompletionSource<bool>();

            BeginInvokeOnMainThread(() =>
            {
                try
                {
                    action();
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            return tcs.Task;
        }

        public static Task<T> InvokeOnMainThreadAsync<T>(Func<T> func)
        {
            if (IsMainThread)
            {
                return Task.FromResult(func());
            }

            var tcs = new TaskCompletionSource<T>();

            BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var result = func();
                    tcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            return tcs.Task;
        }

        public static Task InvokeOnMainThreadAsync(Func<Task> funcTask)
        {
            if (IsMainThread)
            {
                return funcTask();
            }

            var tcs = new TaskCompletionSource<object>();

            BeginInvokeOnMainThread(
                async () =>
                {
                    try
                    {
                        await funcTask().ConfigureAwait(false);
                        tcs.SetResult(null);
                    }
                    catch (Exception e)
                    {
                        tcs.SetException(e);
                    }
                });

            return tcs.Task;
        }

        public static Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask)
        {
            if (IsMainThread)
            {
                return funcTask();
            }

            var tcs = new TaskCompletionSource<T>();

            BeginInvokeOnMainThread(
                async () =>
                {
                    try
                    {
                        var ret = await funcTask().ConfigureAwait(false);
                        tcs.SetResult(ret);
                    }
                    catch (Exception e)
                    {
                        tcs.SetException(e);
                    }
                });

            return tcs.Task;
        }

        public static async Task<SynchronizationContext> GetMainThreadSynchronizationContextAsync()
        {
            SynchronizationContext ret = null;
            await InvokeOnMainThreadAsync(() =>
                ret = SynchronizationContext.Current).ConfigureAwait(false);
            return ret;
        }
    }
}
