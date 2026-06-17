using Contracts;
using Contracts.Models;
using Contracts.Services;
using Contracts.UserControls;
using Contracts.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Wcf_Service_Project.Utils;

namespace Wcf_Service_Project
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool IsSessionStarted { get; set; }

        List<PpgSample> PpgSamples { get; set; } = new List<PpgSample>();
        List<PpgSample> rejects { get; set; } = new List<PpgSample>();
        string CurrentDirectoryName { get; set; } = string.Empty;

        int BatchSize { get; set; } = int.Parse(ConfigurationManager.AppSettings["BatchSize"]);

        int rejectedRows = 0;
        ExceptionHandler ExceptionHandling { get; set; } = new ExceptionHandler();

        private CancellationTokenSource _sessionCts;
        private Task _sessionTask;
        // Keep references so window closing can close the session/channel
        private ChannelFactory<IPpgService> _factory;
        private IClientChannel _clientChannel;
        private IPpgService _proxy;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void DirectoriesSP_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                List<DirectoryInfo> directories = new List<DirectoryInfo>();
                string root = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../DataSet");
                Directory.GetDirectories(root).ToList().ForEach(d => directories.Add(new DirectoryInfo(d)));
               
                foreach (DirectoryInfo d in directories)
                {
                    var newDir = new DataDirectory();
                    newDir.DirName = d.Name;
                    newDir.MouseDoubleClick += DataDirectory_MouseDoubleClick;

                    var border = new Border
                    {
                        BorderBrush = (Brush)Application.Current.Resources["SystemAccentColorBrush"],
                        BorderThickness = new Thickness(1),
                        Background = (Brush)Application.Current.Resources["PanelBackgroundBrush"],

                    };

                    border.Child = newDir;

                    DirectoriesSP.Children.Add(border);
                }
            }
            catch
            {
                this.LoadedRowsTB.Text += "Error loading directories.\n";
            }
        }

        private async void DataDirectory_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.IsSessionStarted) return;

            Stopwatch stopwatch = new Stopwatch();
            if (!this.CurrentDirectoryName.Equals(((DataDirectory)sender).DirName))
            {
                this.PpgSamples.Clear();
                this.PpgSamples.TrimExcess();

                stopwatch.Start();
                this.PpgSamples = PpgConverter.ConvertToPpgSamples(((DataDirectory)sender).DirName, "GalaxyWatch");
                stopwatch.Stop();

                this.CurrentDirectoryName = ((DataDirectory)sender).DirName;

            }
            else return;


            // Loading ProgressBar
            this.LoadingDirPB.Value = 0;
            await this.SafeInvokeUIAsync(() => this.LoadingDirPB.Visibility = Visibility.Visible);
           


            // Session Started
            this.SessionInfoTB.Text = (this.IsSessionStarted) ? "Session started" : "Session Ended";
            this.SessionInfoTB.Foreground = (this.IsSessionStarted) ? Brushes.Green : Brushes.Red;

            // Rows Loaded
            this.RowNumInfoTB.Text = $"Rows Loaded: {this.PpgSamples.Count}";

            // Loading Time
            this.LoadingTimeInfoTB.Text = $"Loading time: {stopwatch.Elapsed.TotalMilliseconds} ms";

            // Loaded rows
            this.LoadedRowsTB.Text = string.Empty;
            int range = (this.PpgSamples.Count >= 30) ? 30 : this.PpgSamples.Count;
            foreach (var sample in this.PpgSamples.GetRange(0, range))
            {
                this.LoadedRowsTB.Text += sample.ToString() + '\n';
                LoadingDirPB.Value+=5;
                await Task.Delay(1);
            }
            this.LoadedRowsTB.Text += "..........";

        }

        void SafeClose(ICommunicationObject obj)
        {
            if (obj is null) return;
            try
            {
                if (obj.State == CommunicationState.Faulted)
                    obj.Abort();
                else
                    obj.Close();
            }
            catch (TimeoutException) { obj.Abort(); }
            catch (CommunicationException) { obj.Abort(); }
            catch (Exception) { obj.Abort(); }
        }

        private async void StartSessionBTN_Click(object sender, RoutedEventArgs e)
        {
            if (this.IsSessionStarted)
            {
                this.SessionInfoTB.Text = "Session Already Started";
                this.SessionInfoTB.Foreground = Brushes.DarkCyan;
                return;
            }

            this._sessionCts = new CancellationTokenSource();
            var token = this._sessionCts.Token;
            

            this._sessionTask = Task.Run(async () =>      
            {
                // use instance fields so Window_Closing can access them
                this._factory = null;
                this._clientChannel = null;
                this._proxy = null;
                this.rejectedRows = 0;

                try
                {
                    this._factory = new ChannelFactory<IPpgService>("SessionHandlingService");
                    this._proxy = this._factory.CreateChannel();
                    this._clientChannel = this._proxy as IClientChannel;

                    await this.SafeInvokeUIAsync(() => this.IsSessionStarted = true);
                    await this.SafeInvokeUIAsync(() => this.StartSessionBTN.IsEnabled = false);
                   


                    var metaData = new Meta(this.CurrentDirectoryName, "Galaxy Watch", this.PpgSamples[0], this.PpgSamples[1]);
                    this._proxy.StartSession(metaData);

                    await this.SafeInvokeUIAsync(() =>
                    {
                        this.SessionInfoTB.Text = "Session Started";
                        this.SessionInfoTB.Foreground = Brushes.Green;
                    });

                    while (this.PpgSamples.Count > 0)
                    {
                        this.SendSamplesBatch(this._proxy);
                        await Task.Delay(50);
                        // Update UI with error counts
                        await this.SafeInvokeUIAsync(() => 
                        this.RejectedRowsTxtB.Text = $"Rejected Rows: {rejectedRows}");

                        await this.SafeInvokeUIAsync(() =>
                        {
                            this.ExceptionsTB.Text = string.Empty;
                            List<PpgSample> last10 = rejects.Skip(Math.Max(0, rejects.Count - 10)).ToList();
                            string text = string.Join(Environment.NewLine, last10.Select(s => s.ToString()));
                            this.ExceptionsTB.Text += text;
                        });

                    }

                    try { this._proxy?.EndSession(); }
                    catch (Exception ex) 
                    { Debug.WriteLine($"End Session Error: ${ex.Message}"); }

                    this.SafeClose(this._clientChannel);
                    this.SafeClose(this._factory);

                    await this.SafeInvokeUIAsync(() =>
                    {
                        this.SessionInfoTB.Text = "Session Ended";
                        this.SessionInfoTB.Foreground = Brushes.Red;
                    });
                }
                catch (Exception ex)
                {
                    this.SafeClose(this._clientChannel);
                    this.SafeClose(this._factory);

                    await this.SafeInvokeUIAsync(() =>
                    {
                        Debug.WriteLine(ex.Message);
                        this.SessionInfoTB.Text = "Session Aborted";
                        this.SessionInfoTB.Foreground = Brushes.DarkRed;
                    });
                }
                finally
                {
                    await this.SafeInvokeUIAsync(() => { 
                        this.IsSessionStarted = false; 
                        this.StartSessionBTN.IsEnabled = true;
                    });

                    this.ExceptionHandling.Dispose();
                    
                    this.PpgSamples.Clear();
                    this.PpgSamples.TrimExcess();

                    // clear stored references
                    this._proxy = null;
                    this._clientChannel = null;
                    this._factory = null;
                }

            }, token);

        }

        // Called after each batch is sent to update the UI with the latest info
        private Task SafeInvokeUIAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (action is null) return Task.CompletedTask;
            if (Dispatcher.CheckAccess())
            {
                try 
                { 
                    action(); 
                }
                catch(Exception ex)
                {
                    Debug.WriteLine($"UI action error: {ex.Message}");
                }
                return Task.CompletedTask;
            }
            else
            {
                var op = Dispatcher.BeginInvoke(action, priority);
                return op.Task.ContinueWith(t => { 
                    if(t.Exception != null)
                    {
                        Debug.WriteLine($"UI invoke exception: {t.Exception.Message}");
                    }
                });
            }
        }

        private void SendSamplesBatch(IPpgService proxy)
        {
            foreach (PpgSample sample in this.PpgSamples.GetRange(0, Math.Min(this.BatchSize, this.PpgSamples.Count)))
            {
                try
                {
                    proxy.PushSample(sample);
                }
                catch (FaultException fex)
                {
                    this.ExceptionHandling.AddFaultException(fex);
                    rejects.Add(sample);
                    rejectedRows++;
                    continue;
                }
            }

            this.PpgSamples.RemoveRange(0, Math.Min(this.BatchSize, this.PpgSamples.Count));
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.IsSessionStarted)
            {
                e.Cancel = true;
                // try to end session gracefully
                try
                {
                    this._proxy?.EndSession();
                }
                catch { }

                // cancel the background task and wait a short time
                try { this._sessionCts?.Cancel(); } catch { }
                try { await Task.WhenAny(this._sessionTask ?? Task.CompletedTask, Task.Delay(2000)); } catch { }

                // ensure channels/factory closed
                try { this.SafeClose(this._clientChannel); } catch { }
                try { this.SafeClose(this._factory); } catch { }

                // clear state
                this.IsSessionStarted = false;
                this._proxy = null;
                this._clientChannel = null;
                this._factory = null;

                Application.Current.Shutdown();
            }
        }
    }
}
