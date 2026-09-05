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
        private int BatchSize { get; set; } = int.TryParse(ConfigurationManager.AppSettings["BatchSize"], out int bs) ? bs : 500;
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

                CurrentDirectoryName = dir.DirName;
                LoadingDirPB.Value = 0;
                LoadingDirPB.Maximum = 100;
                SessionInfoTB.Text = "Loading...";
                SessionInfoTB.Foreground = Brushes.DarkOrange;

                string dirName = dir.DirName;
                var stopwatch = Stopwatch.StartNew();
                List<PpgSample> loaded = await Task.Run(() => PpgConverter.ConvertToPpgSamples(dirName, "E4"));
                stopwatch.Stop();

                PpgSamples = loaded;
                LoadingDirPB.Value = 100;

                SessionInfoTB.Text = "Data Loaded";
                SessionInfoTB.Foreground = Brushes.Green;
                RowNumInfoTB.Text = $"Rows Loaded: {PpgSamples.Count}";
                LoadingTimeInfoTB.Text = $"Loading time: {stopwatch.Elapsed.TotalMilliseconds:F0} ms";

                int range = Math.Min(30, PpgSamples.Count);
                var sb = new System.Text.StringBuilder();
                foreach (var sample in PpgSamples.GetRange(0, range))
                    sb.AppendLine(sample.ToString());
                if (PpgSamples.Count > 30)
                    sb.AppendLine("..........");
                LoadedRowsTB.Text = sb.ToString();
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

            // Set synchronously on UI thread — prevents double-click double-session race
            IsSessionStarted = true;
            StartSessionBTN.IsEnabled = false;
            SessionInfoTB.Text = "Connecting...";
            SessionInfoTB.Foreground = Brushes.DarkOrange;

            // Snapshot UI-thread-owned values before entering background task
            string participantId = CurrentDirectoryName;
            var samples = PpgSamples;

            _sessionCts = new CancellationTokenSource();
            var token = _sessionCts.Token;
            var cts = _sessionCts;

            await (_sessionTask = Task.Run(async () =>
            {
                _factory = null;
                _clientChannel = null;
                _proxy = null;

                try
                {
                    if (samples.Count < 2)
                    {
                        await SafeInvokeUIAsync(() =>
                        {
                            SessionInfoTB.Text = "Not enough data (need at least 2 samples)";
                            SessionInfoTB.Foreground = Brushes.DarkRed;
                        });
                        return;
                    }

                    _factory = new ChannelFactory<IPpgService>("SessionHandlingService");
                    _proxy = _factory.CreateChannel();
                    _clientChannel = _proxy as IClientChannel;

                    var metaData = new Meta(participantId, "E4", samples[0], samples[1]);
                    _proxy.StartSession(metaData);

                    _sendIndex = 0;
                    _totalSamples = samples.Count;

                    await SafeInvokeUIAsync(() =>
                    {
                        SessionInfoTB.Text = "Sending...";
                        SessionInfoTB.Foreground = Brushes.Green;
                        LoadingDirPB.Maximum = _totalSamples;
                        LoadingDirPB.Value = 0;
                    });

                    var uiSw = Stopwatch.StartNew();
                    while (_sendIndex < _totalSamples)
                    {
                        if (token.IsCancellationRequested) break;

                        int count = Math.Min(BatchSize, _totalSamples - _sendIndex);
                        var batch = samples.GetRange(_sendIndex, count);

                        var results = _proxy.PushSamples(batch);
                        foreach (var result in results)
                        {
                            if (!result.IsValid)
                                ExceptionHandling.AddRejection(result);
                        }

                        _sendIndex += count;

                        if (uiSw.ElapsedMilliseconds >= 200 || _sendIndex >= _totalSamples)
                        {
                            uiSw.Restart();
                            int sent = _sendIndex;
                            int total = _totalSamples;
                            int rejected = ExceptionHandling.TotalFaultCount;
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                RowNumInfoTB.Text = $"Sent: {sent} / {total}";
                                ExceptionsTB.Text = $"Rejected: {rejected}";
                                LoadingDirPB.Value = sent;
                            }));
                        }
                    }

                    try { _proxy?.EndSession(); }
                    catch (Exception ex) { Debug.WriteLine($"EndSession Error: {ex.Message}"); }

                    await SafeInvokeUIAsync(() =>
                    {
                        SessionInfoTB.Text = "Session Completed";
                        SessionInfoTB.Foreground = Brushes.Green;
                    });
                }
                catch (EndpointNotFoundException)
                {
                    await SafeInvokeUIAsync(() =>
                    {
                        SessionInfoTB.Text = "Server not reachable — is the server running?";
                        SessionInfoTB.Foreground = Brushes.DarkRed;
                    });
                }
                catch (CommunicationException ex)
                {
                    await SafeInvokeUIAsync(() =>
                    {
                        Debug.WriteLine($"CommunicationException: {ex.Message}");
                        SessionInfoTB.Text = "Connection lost — session aborted";
                        SessionInfoTB.Foreground = Brushes.DarkRed;
                    });
                }
                catch (Exception ex)
                {
                    await SafeInvokeUIAsync(() =>
                    {
                        Debug.WriteLine(ex.Message);
                        SessionInfoTB.Text = "Session Aborted";
                        SessionInfoTB.Foreground = Brushes.DarkRed;
                    });
                }
                finally
                {
                    // SafeClose covers all exit paths including early return and exceptions
                    SafeClose(_clientChannel);
                    SafeClose(_factory);

                    await SafeInvokeUIAsync(() =>
                    {
                        // Clear samples before re-enabling — prevents DataDirectory race
                        PpgSamples = new List<PpgSample>();
                        IsSessionStarted = false;
                        StartSessionBTN.IsEnabled = true;
                    });

                    ExceptionHandling.Reset();
                    _sendIndex = 0;
                    _totalSamples = 0;
                    _proxy = null;
                    _clientChannel = null;
                    _factory = null;
                    cts?.Dispose();
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

            // Snapshot fields before the background task's finally block can null them
            var proxy = _proxy;
            var cts = _sessionCts;
            var task = _sessionTask;
            var channel = _clientChannel;
            var factory = _factory;

            try { proxy?.EndSession(); } catch { }
            try { cts?.Cancel(); } catch { }
            try { await Task.WhenAny(task ?? Task.CompletedTask, Task.Delay(2000)); } catch { }

            SafeClose(channel);
            SafeClose(factory);

            Application.Current.Shutdown();
        }
    }
}
