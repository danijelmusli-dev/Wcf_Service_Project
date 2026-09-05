using Contracts.Models;
using Server.AnalyticHelpers;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace Server
{
    public partial class MainWindow : Window
    {
        private ServiceHost Host { get; set; }
        private SessionHandlingService _service;
        private Analytics _analytics;

        private PpgSample _currSample;
        private PpgSample _prevSample;
        private int _receivedCount;
        private int _rejectedCount;

        private readonly object _sampleLock = new object();
        private readonly object _serverLock = new object();

        private int _lastWarningUiTick;

        private readonly ConcurrentQueue<(PpgSample Prev, PpgSample Curr)> _analyticsQueue =
            new ConcurrentQueue<(PpgSample, PpgSample)>();
        private CancellationTokenSource _analyticsCts;
        private Task _analyticsTask;

        public MainWindow()
        {
            InitializeComponent();
            _analytics = new Analytics();
        }

        private void StartServerBTN_Click(object sender, RoutedEventArgs e)
        {
            if (Host?.State == CommunicationState.Opened)
            {
                if (_service?.IsSessionActive == true)
                {
                    MessageBox.Show("Cannot stop server during active session!");
                    return;
                }
                StopServer();
            }
            else
            {
                StartServer();
            }
        }

        private void StartServer()
        {
            try
            {
                _analyticsCts = new CancellationTokenSource();
                _analyticsTask = DrainAnalyticsAsync(_analyticsCts.Token);

                _service = new SessionHandlingService();
                SubscribeToEvents();

                Host = new ServiceHost(_service);
                Host.Faulted += OnHostFaulted;
                Host.Open();

                SafeInvokeUIAsync(() =>
                {
                    StartServerBTN.Content = "Stop";
                    ServerStatusIndicator.Fill = Brushes.Green;
                    ServerStatusTB.Text = "Running";
                    ServerStatusTB.Foreground = Brushes.Green;
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                var cts = _analyticsCts;
                _analyticsCts = null;
                _analyticsTask = null;
                try { cts?.Cancel(); cts?.Dispose(); } catch { }
                Host?.Abort();
                Host = null;
                UnsubscribeFromEvents();
                _service?.Dispose();
                _service = null;
            }
        }

        private void StopServer()
        {
            lock (_serverLock)
            {
                // Cancel analytics drain under lock — prevents concurrent StopServer race
                // (UI thread via Stop button and WCF thread via OnHostFaulted can both call StopServer)
                var cts = _analyticsCts;
                _analyticsCts = null;
                _analyticsTask = null;
                try { cts?.Cancel(); cts?.Dispose(); } catch { }

                try
                {
                    if (Host != null)
                    {
                        Host.Faulted -= OnHostFaulted;
                        if (Host.State == CommunicationState.Faulted)
                            Host.Abort();
                        else
                            Host.Close();

                        Host = null;

                        UnsubscribeFromEvents();
                        _service?.Dispose();
                        _service = null;
                    }

                    SafeInvokeUIAsync(() =>
                    {
                        StartServerBTN.Content = "Start";
                        ServerStatusIndicator.Fill = Brushes.Red;
                        ServerStatusTB.Text = "Stopped";
                        ServerStatusTB.Foreground = Brushes.Red;
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("StopServer Error: " + ex.Message);
                    Host?.Abort();
                    Host = null;
                    _service = null;
                }
            }
        }

        private void OnHostFaulted(object sender, EventArgs e)
        {
            SafeInvokeUIAsync(() =>
            {
                AppendEvent("Client disconnected unexpectedly.");
            });

            // M-6: snapshot and clear _service before StopServer to prevent use-after-dispose race
            SessionHandlingService svc;
            lock (_serverLock)
            {
                svc = _service;
                _service = null;
            }
            svc?.Dispose();

            StopServer();
        }

        private void OnTransferStarted(object sender, EventArgs e)
        {
            _analytics.ResetWarningCounts();
            Interlocked.Exchange(ref _receivedCount, 0);
            Interlocked.Exchange(ref _rejectedCount, 0);

            SafeInvokeUIAsync(() =>
            {
                AppendEvent("Transfer Started!");
                if (sender is Meta meta)
                    MetaDataLV.Items.Add(meta);
            });
        }

        private void OnSampleReceived(object sender, EventArgs e)
        {
            if (!(sender is PpgSample sample)) return;

            PpgSample prev, curr;
            lock (_sampleLock)
            {
                _prevSample = _currSample;
                _currSample = sample;
                prev = _prevSample;
                curr = _currSample;
            }

            // Enqueue for background drain — WCF thread returns immediately
            _analyticsQueue.Enqueue((prev, curr));
        }

        private async Task DrainAnalyticsAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                while (_analyticsQueue.TryDequeue(out var pair))
                {
                    int count = Interlocked.Increment(ref _receivedCount);
                    _analytics.AnalyzePpgSample(pair.Prev, pair.Curr);

                    if (count % 100 == 0)
                        SafeInvokeUIAsync(() => IncomingRowsTB.Text = $"Received: {count}");
                }
                try { await Task.Delay(5, ct); }
                catch (OperationCanceledException) { break; }
            }

            // Drain remaining after cancel
            while (_analyticsQueue.TryDequeue(out var pair))
            {
                Interlocked.Increment(ref _receivedCount);
                _analytics.AnalyzePpgSample(pair.Prev, pair.Curr);
            }
        }

        private void OnTransferCompleted(object sender, EventArgs e)
        {
            int received = _receivedCount;
            SafeInvokeUIAsync(() =>
            {
                AppendEvent("Transfer Completed!");
                IncomingRowsTB.Text = $"Received: {received} (Done)";
            });
            // Server stays running — ready for next session
        }

        private void OnWarningRaised(object sender, EventArgs e)
        {
            Interlocked.Increment(ref _rejectedCount);
            // Throttle: one UI update per 200 ms — prevents dispatcher flooding under high rejection rates
            int now = Environment.TickCount;
            if (unchecked(now - _lastWarningUiTick) < 200) return;
            _lastWarningUiTick = now;
            int rejected = _rejectedCount;
            SafeInvokeUIAsync(() => RejectedCSVTB.Text = $"Total rejected: {rejected}");
        }

        private void OnHrOutOfRangeWarning(object sender, PpgSample sample)
        {
            int count = _analytics.HrOutOfRangeWarningCount;
            SafeInvokeUIAsync(() =>
            {
                HearthRate_PG.ProgressValue = Math.Min(count, 100);
            });
        }

        private void OnIbiSpikeWarning(object sender, PpgSample sample)
        {
            int count = _analytics.IbiSpikeWarningCount;
            SafeInvokeUIAsync(() =>
            {
                IBI_PG.ProgressValue = Math.Min(count, 100);
            });
        }

        private void OnExcessiveMotionWarning(object sender, PpgSample sample)
        {
            int count = _analytics.ExcessiveMotionWarningCount;
            SafeInvokeUIAsync(() =>
            {
                ANORM_PG.ProgressValue = Math.Min(count, 100);
            });
        }

        private void OnWeakPpgWarning(object sender, PpgSample sample)
        {
            SafeInvokeUIAsync(() =>
            {
                AppendEvent($"Weak PPG signal at row {sample.RowIndex}");
            });
        }

        private void AppendEvent(string message)
        {
            const int MaxLines = 200;
            var text = EventsTB.Text + message + "\n";
            var lines = text.Split('\n');
            if (lines.Length > MaxLines)
                text = string.Join("\n", lines, lines.Length - MaxLines, MaxLines);
            EventsTB.Text = text;
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

        private void SubscribeToEvents()
        {
            if (_service != null)
            {
                _service.TransferStarted += OnTransferStarted;
                _service.SampleReceived += OnSampleReceived;
                _service.OnTransferCompleted += OnTransferCompleted;
                _service.OnWarningRaised += OnWarningRaised;
            }
            if (_analytics != null)
            {
                _analytics.HrOutOfRangeWarning += OnHrOutOfRangeWarning;
                _analytics.WeakPpgWarning += OnWeakPpgWarning;
                _analytics.ExcessiveMotionWarning += OnExcessiveMotionWarning;
                _analytics.IbiSpikeWarning += OnIbiSpikeWarning;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_service != null)
            {
                _service.TransferStarted -= OnTransferStarted;
                _service.SampleReceived -= OnSampleReceived;
                _service.OnTransferCompleted -= OnTransferCompleted;
                _service.OnWarningRaised -= OnWarningRaised;
            }
            if (_analytics != null)
            {
                _analytics.HrOutOfRangeWarning -= OnHrOutOfRangeWarning;
                _analytics.WeakPpgWarning -= OnWeakPpgWarning;
                _analytics.ExcessiveMotionWarning -= OnExcessiveMotionWarning;
                _analytics.IbiSpikeWarning -= OnIbiSpikeWarning;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            UnsubscribeFromEvents();
            StopServer();
        }
    }
}
