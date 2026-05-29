using Contracts;
using Contracts.Models;
using Contracts.Utils;
using Server.AnalyticHelpers;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
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
        Analytics _analitic { get; set; }

        PpgSample _currSample = new PpgSample();
        PpgSample _prevSample = new PpgSample();

        public MainWindow()
        {
            InitializeComponent();

            this._service = new SessionHandlingService();

            this._service.TransferStarted += this.OnTransferStarted;
            this._service.SampleRecieved += this.OnSampleRecieved;
            this._service.OnTransferCompleted += this.OnTransferCompleted;
            this._service.OnWarningRaised += this.OnWarningRaised;

            this._analitic = new Analytics();

            this._analitic.HrOutOfRangeWarning += this.OnHrOutOfRangeWarning;
            this._analitic.WeakPpgWarning += this.OnWeakPpgWarning;
            this._analitic.ExcessiveMotionWarning += this.OnExcessiveMotionWarning;
            this._analitic.IbiSpikeWarning += this.OnIbiSpikeWarning;
            
        }

        private void StartServerBTN_Click(object sender, RoutedEventArgs e)
        {
            if (this.Host?.State == CommunicationState.Opened)
            {
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
                this.Host = new ServiceHost(this._service);
                this.Host.Open();

                this.StartServerBTN.Content = "Stop";
                this.ServerStatusIndicator.Fill = Brushes.Green;
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
            try
            {
                if (this.Host != null)
                {
                    if (this.Host.State == CommunicationState.Faulted)
                        this.Host.Abort();
                    else
                        this.Host.Close();

                    this.Host = null;
                }

                this.StartServerBTN.Content = "Start";
                this.ServerStatusIndicator.Fill = Brushes.Red;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                this.Host?.Abort();
                this.Host = null;
            }
        }

        private void OnTransferStarted(object sender, EventArgs e)
        {
            this.EventsTB.Text += "Transfer Started! \n";

            if (sender is Meta)
            { 
                this.MetaDataLV.Items.Add(sender as Meta);
            }
                
        }

        private void OnSampleRecieved(object sender, EventArgs e)
        {
            if (sender is PpgSample sample)
            {
                this._prevSample = this._currSample;
                this._currSample = sample;
            }

            _analitic.AnalizePpgSample(this._prevSample, this._currSample);
        }

        private void OnTransferCompleted(object sender, EventArgs e)
        {
            this.StopServer();

            this.EventsTB.Text += "Transfer Completed! \n";

        }

        private void OnWarningRaised(object sender, EventArgs e)
        {
            
        }
        private void OnHrOutOfRangeWarning(object sender, PpgSample sample)
        {
            this._service.OnWarningRaised?.Invoke(sender, EventArgs.Empty);
        }
        private void OnIbiSpikeWarning(object sender, PpgSample sample)
        {
            this._service.OnWarningRaised?.Invoke(sender, EventArgs.Empty);
        }
        private void OnExcessiveMotionWarning(object sender, PpgSample sample)
        {
            this._service.OnWarningRaised?.Invoke(sender, EventArgs.Empty);
        }
        private void OnWeakPpgWarning(object sender, PpgSample sample)
        {
            this._service.OnWarningRaised?.Invoke(sender, EventArgs.Empty);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            this._service.TransferStarted -= this.OnTransferStarted;
            this._service.SampleRecieved -= this.OnSampleRecieved;
            this._service.OnTransferCompleted -= this.OnTransferCompleted;
            this._service.OnWarningRaised -= this.OnWarningRaised;

            this._analitic.HrOutOfRangeWarning -= this.OnHrOutOfRangeWarning;
            this._analitic.WeakPpgWarning -= this.OnWeakPpgWarning;
            this._analitic.ExcessiveMotionWarning -= this.OnExcessiveMotionWarning;
            this._analitic.IbiSpikeWarning -= this.OnIbiSpikeWarning;

            if (this.Host != null)
            {
                try
                {
                    if (this.Host.State == CommunicationState.Faulted)
                        this.Host.Abort();
                    else
                        this.Host.Close();
                }
                catch
                {
                    this.Host?.Abort();
                }
                this.Host = null;
            }
        }
    }
}
