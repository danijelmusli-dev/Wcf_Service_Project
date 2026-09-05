using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Server
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            base.OnStartup(e);
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log($"Dispatcher Unhandled: {e.Exception}");
            System.Windows.MessageBox.Show($"Unexpected error:\n{e.Exception.Message}", "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log($"AppDomain Unhandled: {e.ExceptionObject}");
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Log($"Task Unobserved: {e.Exception}");
            e.SetObserved();
        }

        private static void Log(string message)
        {
            try
            {
                var msg = $"{DateTime.Now:O} [Thread:{Thread.CurrentThread.ManagedThreadId}] {message}";
                Debug.WriteLine(msg);
                Trace.WriteLine(msg);
            }
            catch { }
        }
    }
}
