using Contracts;
using Contracts.Models;
using Contracts.Utils;
using System;
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


        public MainWindow()
        {
            InitializeComponent();


            this._service = new SessionHandlingService();
            this.Host = new ServiceHost(this._service);

            this._service.TransferStarted += this.OnTransferStarted;
            this._service.SampleRecieved += this.OnSampleRecieved;
            this._service.OnTransferCompleted += this.OnTransferCompleted;
            this._service.OnWarningRaised += this.OnWarningRaised;
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
            // prikazati da je prenos krenuo
        }
        private void OnSampleRecieved(object sender, EventArgs e)
        {
            if (!PpgSampleValidator.ValidateSample(PpgSampleBase.PpgSamples.Last()))
            {

            }
        }
        private async void OnTransferCompleted(object sender, EventArgs e)
        {
            foreach (var sample in PpgSampleBase.PpgSamples)
            {
                this.IncomingRowsTB.Text += sample.ToString() + "\n";
            }
        }
        private void OnWarningRaised(object sender, EventArgs e)
        {

        }

        private void WriteIncomingRows()
        {
            foreach (var sample in PpgSampleBase.PpgSamples)
            {
                this.IncomingRowsTB.Text += sample.ToString() + "\n";
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
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
