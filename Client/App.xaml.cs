using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Wcf_Service_Project
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                Log($"AppDomain Unhandled: {args.ExceptionObject}");
            };
            Current.DispatcherUnhandledException += (s, args) =>
            {
                Log($"Dispatcher Unhandled: {args.Exception}");
                MessageBox.Show($"Unexpected error:\n{args.Exception.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
            TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                Log($"Task Unobserved: {args.Exception}");
                args.SetObserved();
            };
            base.OnStartup(e);
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
