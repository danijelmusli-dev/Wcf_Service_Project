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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Wcf_Service_Project.Utils;

namespace Wcf_Service_Project
{
    public partial class MainWindow : Window
    {
        private bool IsSessionStarted { get; set; }
        private List<PpgSample> PpgSamples { get; set; } = new List<PpgSample>();
        private string CurrentDirectoryName { get; set; } = string.Empty;
        private int BatchSize { get; set; } = int.Parse(ConfigurationManager.AppSettings["BatchSize"]);
        private ExceptionHandler ExceptionHandling { get; set; } = new ExceptionHandler();

        private int _sendIndex;
        private int _totalSamples;

        private CancellationTokenSource _sessionCts;
        private Task _sessionTask;
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
                string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../DataSet");
                var directories = Directory.GetDirectories(root).Select(d => new DirectoryInfo(d));

                foreach (var d in directories)
                {
                    var newDir = new DataDirectory { DirName = d.Name };
                    newDir.MouseDoubleClick += DataDirectory_MouseDoubleClick;
                    DirectoriesSP.Children.Add(newDir);
                }
            }
            catch
            {
                LoadedRowsTB.Text += "Error loading directories.\n";
            }
        }

        private async void DataDirectory_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (IsSessionStarted) return;

            try
            {
                var dir = (DataDirectory)sender;
                if (CurrentDirectoryName.Equals(dir.DirName)) return;

                PpgSamples.Clear();
                PpgSamples.TrimExcess();

                var stopwatch = Stopwatch.StartNew();
                PpgSamples = PpgConverter.ConvertToPpgSamples(dir.DirName, "GalaxyWatch");
                stopwatch.Stop();

                CurrentDirectoryName = dir.DirName;

                LoadingDirPB.Value = 0;
                LoadingDirPB.Maximum = 100;
                for (int i = 0; i <= 100; i++)
                {
                    LoadingDirPB.Value = i;
                    await Task.Delay(1);
                }

                SessionInfoTB.Text = "Data Loaded";
                SessionInfoTB.Foreground = Brushes.Green;
                RowNumInfoTB.Text = $"Rows Loaded: {PpgSamples.Count}";
                LoadingTimeInfoTB.Text = $"Loading time: {stopwatch.Elapsed.TotalMilliseconds:F0} ms";

                LoadedRowsTB.Text = string.Empty;
                int range = Math.Min(30, PpgSamples.Count);
                foreach (var sample in PpgSamples.GetRange(0, range))
                    LoadedRowsTB.Text += sample + "\n";

                if (PpgSamples.Count > 30)
                    LoadedRowsTB.Text += "..........";
            }
            catch (Exception ex)
            {
                SessionInfoTB.Text = "Error loading data";
                SessionInfoTB.Foreground = Brushes.DarkRed;
                Debug.WriteLine($"DataDirectory load error: {ex}");
                MessageBox.Show($"Error loading data:\n{ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SafeClose(ICommunicationObject obj)
        {
            if (obj is null) return;
            try
            {
                if (obj.State == CommunicationState.Faulted)
                    obj.Abort();
                else
                    obj.Close();
            }
            catch { obj.Abort(); }
        }

        private async void StartSessionBTN_Click(object sender, RoutedEventArgs e)
        {
            if (IsSessionStarted)
            {
                SessionInfoTB.Text = "Session Already Started";
                SessionInfoTB.Foreground = Brushes.DarkCyan;
                return;
            }

            _sessionCts = new CancellationTokenSource();
            var token = _sessionCts.Token;

            await (_sessionTask = Task.Run(async () =>
            {
                _factory = null;
                _clientChannel = null;
                _proxy = null;

                try
                {
                    _factory = new ChannelFactory<IPpgService>("SessionHandlingService");
                    _proxy = _factory.CreateChannel();
                    _clientChannel = _proxy as IClientChannel;

                    if (PpgSamples.Count < 2)
                    {
                        await SafeInvokeUIAsync(() =>
                        {
                            SessionInfoTB.Text = "Not enough data (need at least 2 samples)";
                            SessionInfoTB.Foreground = Brushes.DarkRed;
                        });
                        return;
                    }

                    await SafeInvokeUIAsync(() =>
                    {
                        IsSessionStarted = true;
                        StartSessionBTN.IsEnabled = false;
                    });

                    var metaData = new Meta(CurrentDirectoryName, "Galaxy Watch", PpgSamples[0], PpgSamples[1]);
                    _proxy.StartSession(metaData);

                    _sendIndex = 0;
                    _totalSamples = PpgSamples.Count;

                    await SafeInvokeUIAsync(() =>
                    {
                        SessionInfoTB.Text = "Sending...";
                        SessionInfoTB.Foreground = Brushes.Green;
                        LoadingDirPB.Maximum = _totalSamples;
                        LoadingDirPB.Value = 0;
                    });

                    while (_sendIndex < _totalSamples)
                    {
                        if (token.IsCancellationRequested) break;

                        int count = Math.Min(BatchSize, _totalSamples - _sendIndex);
                        var batch = PpgSamples.GetRange(_sendIndex, count);

                        var results = _proxy.PushSamples(batch);
                        foreach (var result in results)
                        {
                            if (!result.IsValid)
                                ExceptionHandling.AddRejection(result);
                        }

                        _sendIndex += count;

                        int sent = _sendIndex;
                        int total = _totalSamples;
                        int rejected = ExceptionHandling.TotalFaultCount;
                        await SafeInvokeUIAsync(() =>
                        {
                            RowNumInfoTB.Text = $"Sent: {sent} / {total}";
                            ExceptionsTB.Text = $"Rejected: {rejected}";
                            LoadingDirPB.Value = sent;
                        });

                        await Task.Delay(20);
                    }

                    try { _proxy?.EndSession(); }
                    catch (Exception ex) { Debug.WriteLine($"EndSession Error: {ex.Message}"); }

                    SafeClose(_clientChannel);
                    SafeClose(_factory);

                    await SafeInvokeUIAsync(() =>
                    {
                        SessionInfoTB.Text = "Session Completed";
                        SessionInfoTB.Foreground = Brushes.Green;
                    });
                }
                catch (Exception ex)
                {
                    SafeClose(_clientChannel);
                    SafeClose(_factory);

                    await SafeInvokeUIAsync(() =>
                    {
                        Debug.WriteLine(ex.Message);
                        SessionInfoTB.Text = "Session Aborted";
                        SessionInfoTB.Foreground = Brushes.DarkRed;
                    });
                }
                finally
                {
                    await SafeInvokeUIAsync(() =>
                    {
                        IsSessionStarted = false;
                        StartSessionBTN.IsEnabled = true;
                    });

                    ExceptionHandling.Dispose();
                    PpgSamples.Clear();
                    PpgSamples.TrimExcess();
                    _sendIndex = 0;
                    _totalSamples = 0;

                    _proxy = null;
                    _clientChannel = null;
                    _factory = null;
                    _sessionCts?.Dispose();
                    _sessionCts = null;
                }
            }, token));
        }

        private Task SafeInvokeUIAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (action is null) return Task.CompletedTask;
            if (Dispatcher.CheckAccess())
            {
                try { action(); }
                catch (Exception ex) { Debug.WriteLine($"UI action error: {ex.Message}"); }
                return Task.CompletedTask;
            }

            var op = Dispatcher.BeginInvoke(action, priority);
            return op.Task.ContinueWith(t =>
            {
                if (t.Exception != null)
                    Debug.WriteLine($"UI invoke exception: {t.Exception.Message}");
            });
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!IsSessionStarted) return;

            e.Cancel = true;

            try { _proxy?.EndSession(); } catch { }
            try { _sessionCts?.Cancel(); } catch { }
            try { await Task.WhenAny(_sessionTask ?? Task.CompletedTask, Task.Delay(2000)); } catch { }

            SafeClose(_clientChannel);
            SafeClose(_factory);

            IsSessionStarted = false;
            _proxy = null;
            _clientChannel = null;
            _factory = null;

            Application.Current.Shutdown();
        }
    }
}
