using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

using Contracts;
using Contracts.Models;
using Contracts.Services;
using Contracts.UserControls;
using Contracts.Utils;

namespace Wcf_Service_Project
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        bool IsSessionStarted { get; set; }
        List<PpgSample> PpgSamples { get; set; } = new List<PpgSample>();
        List<PpgSample> RejectedPpgSamples { get; set; } = new List<PpgSample>();
        string CurrentDirectoryName { get; set; } = string.Empty;

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

                    this.DirectoriesSP.Children.Add(newDir);
                }
            }
            catch
            {
                this.LoadedRowsTB.Text += "Error loading directories.\n";
            }
        }

        private async void DataDirectory_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

            Stopwatch stopwatch = new Stopwatch();
            if (!this.CurrentDirectoryName.Equals(((DataDirectory)sender).DirName))
            {
                stopwatch.Start();
                this.PpgSamples.Clear();
                this.PpgSamples = PpgConverter.ConvertToPpgSamples(((DataDirectory)sender).DirName, "GalaxyWatch");

                this.CurrentDirectoryName = ((DataDirectory)sender).DirName;

                stopwatch.Stop();
            }
            else return;


            // Loading ProgressBar
            this.LoadingDirPB.Value = 0;
            for (int i = 0; i <= 100; i++)
            {
                this.LoadingDirPB.Value++;
                await Task.Delay(1);
            }

            // Session Started
            this.SessionInfoTB.Text = (this.IsSessionStarted) ? "Session started" : "No session started.";
            this.SessionInfoTB.Foreground = (this.IsSessionStarted) ? Brushes.Green : Brushes.Red;

            // Rows Loaded
            this.RowNumInfoTB.Text = $"Rows Loaded: {this.PpgSamples.Count}";

            // Loading Time
            this.LoadingTimeInfoTB.Text = $"Loading time: {stopwatch.Elapsed.TotalMilliseconds} ms";

            // Rejected rows
            this.RejectedPpgSamples.Clear();
            this.RejectedPpgSamples = PpgSampleValidator.AllInValidSamples(this.PpgSamples);

            PpgSampleValidator.RemoveAllInValidSamples(this.PpgSamples);

            this.RejectedRowsTB.Text = string.Empty;
            int range = (this.RejectedPpgSamples.Count >= 30) ? 30 : this.RejectedPpgSamples.Count;
            foreach (var sample in this.RejectedPpgSamples.GetRange(0, range))
            {
                this.RejectedRowsTB.Text += sample.ToString() + '\n';
            }
            this.RejectedRowsTB.Text += "..........";

            // Loaded rows
            this.LoadedRowsTB.Text = string.Empty;
            range = (this.PpgSamples.Count >= 30) ? 30 : this.PpgSamples.Count;
            foreach (var sample in this.PpgSamples.GetRange(0, range))
            {
                this.LoadedRowsTB.Text += sample.ToString() + '\n';
            }
            this.LoadedRowsTB.Text += "..........";


            // Logging rejected rows
            foreach (var sample in this.RejectedPpgSamples)
            {
                Logger.LogToRejectedClient(sample.ToString());
            }

        }

        private void StartSessionBTN_Click(object sender, RoutedEventArgs e)
        {
            if (this.PpgSamples.Count == 0) return;
            if (this.PpgSamples.Count < 2) return; // cannot initialize metaData without at least 2 ppg samples

            try
            {
                using (ChannelFactory<IPpgService> factory = new ChannelFactory<IPpgService>("SessionHandlingService"))
                {
                    IPpgService proxy = factory.CreateChannel();

                    var metaData = new Meta(this.CurrentDirectoryName, "Galaxy Watch", this.PpgSamples[0], this.PpgSamples[1]);
                    proxy.StartSession(metaData);

                    this.SessionInfoTB.Text = "Session Started";
                    this.SessionInfoTB.Foreground = Brushes.Green;

                    foreach (PpgSample sample in this.PpgSamples)
                    {
                        proxy.PushSample(sample);
                    }
                    proxy.EndSession();

                    ((IClientChannel)proxy).Close();
                }
            }
            catch
            {
                if (String.IsNullOrEmpty(this.CurrentDirectoryName))
                {
                    MessageBox.Show("Load directory first");
                }
            }
            finally
            {
                this.SessionInfoTB.Text = "Session Ended";
                this.SessionInfoTB.Foreground = Brushes.Red;
            }

        }
    }
}
