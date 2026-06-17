using Contracts;
using Contracts.Models;
using Contracts.Utils;
using Server.AnalyticHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ServiceHost Host { get; set; } = null;
        SessionHandlingService _service;
        List<PpgSample> RejectedPpgSamples { get; set; } = new List<PpgSample>();
        List<PpgSample> ReceivedSamples { get; set; } = new List<PpgSample>();
        Analytics Analitic { get; set; }

        PpgSample _currSample = new PpgSample();
        PpgSample _prevSample = new PpgSample();

        private readonly object _sampleLock = new object();
        private readonly object _serverLock = new object();

        public MainWindow()
        {
            InitializeComponent();    
            
            this.Analitic = new Analytics();
            
        }

        private void StartServerBTN_Click(object sender, RoutedEventArgs e)
        {
            if (this.Host?.State == CommunicationState.Opened)
            {
                if (this._service?.IsSessionActive == true)
                {
                    MessageBox.Show("Cannot stop server during active session!");
                    return;
                }
                this.StopServer();
            }
            else
            {
                this.StartServer();
            }
        }

        private void StartServer()
        {
            try
            {
                this._service = new SessionHandlingService();
                this.SubscribeToEvents();

                this.Host = new ServiceHost(this._service);
                this.Host.Faulted += this.OnHostFaulted;
                this.Host.Open();

                this.SafeInvokeUIAsync(() =>
                {
                    this.StartServerBTN.Content = "Stop";
                    this.ServerStatusIndicator.Fill = Brushes.Green;
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                this.Host?.Abort();
                this.Host = null;
            }
        }
        private void StopServer()
        {
            lock (this._serverLock)
            {
                try
                {
                    if (this.Host != null)
                    {

                        this.Host.Faulted -= this.OnHostFaulted;
                        if (this.Host.State == CommunicationState.Faulted)
                            this.Host.Abort();
                        else
                            this.Host.Close();

                        this.Host = null;

                        this.UnsubscribeFromEvents();
                        this._service.Dispose();
                        this._service = null;
                    }

                    this.SafeInvokeUIAsync(() => {
                        this.StartServerBTN.Content = "Start";
                        this.ServerStatusIndicator.Fill = Brushes.Red;
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Start Server Error: " + ex.Message);
                    this.Host?.Abort();

                    this.Host = null;
                    this._service = null;
                }
            }
        }

        // this methog gets called if the connection is 
        // unexpectedly stopped
        private void OnHostFaulted(object sender, EventArgs e)
        { 
            this.SafeInvokeUIAsync(() => {
                this.EventsTB.Text += "\"Client disconnected unexpectedly - forcing session end.\n\";";
            });

            this.StopServer();
        }

        private void OnTransferStarted(object sender, EventArgs e)
        {
            this.Analitic.ResetWarningCounts(); 

            MinHrTb.Text = $"Minimum Heartrate: {Analitic.HrMinBpm}";
            MaxHrTb.Text = $"Maximum Heartrate: {Analitic.HrMaxBpm}";
            MaxMotionTb.Text = $"Motion Treshold: {Analitic.AccThreshold}";
            ValidIbiTb.Text = $"Valid IBI Treshold: {0}";

            this.SafeInvokeUIAsync(() => { 
                
                this.EventsTB.Text += "Transfer Started! \n";

                if (sender is Meta)
                {
                    this.MetaDataLV.Items.Add(sender as Meta);
                    this.CurrentPartTxB.Text = MetaDataLV.Items[MetaDataLV.Items.Count - 1].ToString();
                }
            });
        }

        private void OnSampleRecieved(object sender, EventArgs e)
        {
            if (sender is PpgSample sample)
            {
                lock (this._sampleLock)
                {
                    this._prevSample = this._currSample;
                    this._currSample = sample;
                    this.ReceivedSamples.Add(sample);
                    
                    this.SafeInvokeUIAsync(() =>
                    {
                        this.IncomingRowsTB.Text = string.Empty;

                        List<PpgSample> last10 = this.ReceivedSamples
                            .Skip(Math.Max(0, this.ReceivedSamples.Count - 10))
                            .ToList();

                        CurrHr.Text = $"Heartrate: {sample.HeartRate}";
                        CurrMotion.Text = $"Motion: {Analitic.aNorm}";
                        ValidIbiTb.Text = $"Valid IBI Treshold: {Analitic.ibiDisc}";
                        CurrIbi.Text = $"IBI: {Analitic.ibi}";

                    this.IncomingRowsTB.Text = string.Join(Environment.NewLine, last10.Select(s => s.ToString()));
                        this.IncomingRowsTB.ScrollToEnd();
                    });
                }
                


            }

            Analitic.AnalizePpgSample(this._prevSample, this._currSample);
        }

        private void OnTransferCompleted(object sender, EventArgs e)
        {
            this.SafeInvokeUIAsync(() => this.EventsTB.Text += "Transfer Completed! \n");
            this.StopServer();
        }

        private async void OnWarningRaised(object sender, EventArgs e)
        {
            
            return;
        }
        private async void OnHrOutOfRangeWarning(object sender, PpgSample sample)
        {
            await this.SafeInvokeUIAsync(() =>
            {
                this.WarningLogTB.Text += $"Heart rate out of range! min|max: {Analitic.HrMinBpm}|{Analitic.HrMaxBpm} , current: {sample.HeartRate} , HrOutOfRangeWarning count: {Analitic.HrOutOfRangeWarningCount}\n";
            });
                return;
        }
        private async void OnIbiSpikeWarning(object sender, PpgSample sample)
        {
            await this.SafeInvokeUIAsync(() =>
            {
                this.WarningLogTB.Text += $"Ibi Spiked! IbiSpikeWarning count: {Analitic.IbiSpikeWarningCount}\n";
            });
            return;
            
        }
        private async void OnExcessiveMotionWarning(object sender, PpgSample sample)
        {
            await this.SafeInvokeUIAsync(() =>
            {
                this.WarningLogTB.Text += $"Excessive motion! ExcessiveMotionWarning count: {Analitic.ExcessiveMotionWarningCount}\n";
            });
            return;
        }
        private async void OnWeakPpgWarning(object sender, PpgSample sample)
        {
            await this.SafeInvokeUIAsync(() =>
            {
                this.WarningLogTB.Text += $"Weak Ppg! Green: {sample.PpgGreen} , Red: {sample.PpgRed} , Ir: {sample.PpgIr} , WeakPpgWarning count: {Analitic.WeakPpgWarningCount}\n";
            });
            return;
            
        }

        private Task SafeInvokeUIAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (action is null) return Task.CompletedTask;
            if (Dispatcher.CheckAccess())
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"UI action error: {ex.Message}");
                }
                return Task.CompletedTask;
            }
            else
            {
                var op = Dispatcher.BeginInvoke(action, priority);
                return op.Task.ContinueWith(t => {
                    if (t.Exception != null)
                    {
                        Debug.WriteLine($"UI invoke exception: {t.Exception.Message}");
                    }
                });
            }
        }

        private void SubscribeToEvents()
        {
            if (this._service != null)
            {
                this._service.TransferStarted += this.OnTransferStarted;
                this._service.SampleRecieved += this.OnSampleRecieved;
                this._service.OnTransferCompleted += this.OnTransferCompleted;
                this._service.OnWarningRaised += this.OnWarningRaised;
            }
            if (this.Analitic != null)
            {
                this.Analitic.HrOutOfRangeWarning += this.OnHrOutOfRangeWarning;
                this.Analitic.WeakPpgWarning += this.OnWeakPpgWarning;
                this.Analitic.ExcessiveMotionWarning += this.OnExcessiveMotionWarning;
                this.Analitic.IbiSpikeWarning += this.OnIbiSpikeWarning;
            }
        }   

        private void UnsubscribeFromEvents()
        {
            if(this._service != null)
            {
                this._service.TransferStarted -= this.OnTransferStarted;
                this._service.SampleRecieved -= this.OnSampleRecieved;
                this._service.OnTransferCompleted -= this.OnTransferCompleted;
                this._service.OnWarningRaised -= this.OnWarningRaised;
            }

            if (this.Analitic != null)
            {
                this.Analitic.HrOutOfRangeWarning -= this.OnHrOutOfRangeWarning;
                this.Analitic.WeakPpgWarning -= this.OnWeakPpgWarning;
                this.Analitic.ExcessiveMotionWarning -= this.OnExcessiveMotionWarning;
                this.Analitic.IbiSpikeWarning -= this.OnIbiSpikeWarning;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.UnsubscribeFromEvents();
            this.StopServer();

            this.EventsTB.Text = string.Empty;
            this.MetaDataLV.Items.Clear();

            this.RejectedPpgSamples.Clear();
            this.RejectedPpgSamples.TrimExcess();
        }

        private void EventsTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            EventsTB.ScrollToEnd();
        }


    }
}
