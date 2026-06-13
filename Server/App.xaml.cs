using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Server
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Registruj globalne handlere odmah pri startu
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            base.OnStartup(e);
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log($"Dispatcher Unhandled: {e.Exception}");
            // dok debagiraš, možeš postaviti e.Handled = false da debugger uhvati; u produkciji obično true
            e.Handled = false;
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
                var msg = $"{DateTime.Now:O} [Thread:{Thread.CurrentThread.ManagedThreadId}] {message   }";
                Debug.WriteLine(msg);
                Trace.WriteLine(msg);
            }
            catch { /* ne radi ništa ako logging zakaže */ }

        }
    }
}
