using Contracts;
using Contracts.Models;
using Contracts.Services;
using Contracts.UserControls;
using Contracts.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
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
        List<string> PreviousDirectories { get; set; } = new List<string>();

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
            if (this.IsSessionStarted) return;

            Stopwatch stopwatch = new Stopwatch();
            if (!this.PreviousDirectories.Contains((sender as DataDirectory).DirName))
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
            }
            this.LoadedRowsTB.Text += "..........";

        }

        private async void StartSessionBTN_Click(object sender, RoutedEventArgs e)
        {
            if (this.IsSessionStarted) return;
            if (this.PpgSamples.Count < 2) return;

            if (this.PreviousDirectories.Contains(this.CurrentDirectoryName)) return;
            this.PreviousDirectories.Add(this.CurrentDirectoryName);

            await Task.Run(() =>
            {
                try
                {
                    using (ChannelFactory<IPpgService> factory = new ChannelFactory<IPpgService>("SessionHandlingService"))
                    {
                        IPpgService proxy = factory.CreateChannel();

                        this.IsSessionStarted = true;

                        var metaData = new Meta(this.CurrentDirectoryName, "Galaxy Watch", this.PpgSamples[0], this.PpgSamples[1]);
                        proxy.StartSession(metaData);

                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.SessionInfoTB.Text = "Session Started";
                            this.SessionInfoTB.Foreground = Brushes.Green;
                        }));

                        foreach (PpgSample sample in this.PpgSamples)
                        {

                            try 
                            {
                                proxy.PushSample(sample);
                            }
                            catch (FaultException<ValidationFault> ex)
                            {
                                Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    this.ExceptionsTB.Text += $"{ex.Detail.RejectedSample.RowIndex}: {ex.Detail.ExceptionMessage}\n";
                                }));
                            }
                            catch (FaultException<DataFormatFault> ex)
                            {
                                Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    this.ExceptionsTB.Text += $"{ex.Detail.RejectedSample.RowIndex}: {ex.Detail.ExceptionMessage}\n";
                                }));
                            }
                            catch (FaultException ex)
                            {
                                Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    this.ExceptionsTB.Text += ex.Message + '\n';
                                }));
                            }

                        }

                        try
                        {
                            proxy.EndSession();
                            ((IClientChannel)proxy).Close();
                        }
                        catch
                        {
                            ((IClientChannel)proxy).Abort();
                        }

                        this.IsSessionStarted = false;

                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.SessionInfoTB.Text = "Session Ended";
                            this.SessionInfoTB.Foreground = Brushes.Red;
                        }));
                    }
                }
                catch (CommunicationException cmx)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.ExceptionsTB.Text += cmx.Message + '\n';
                    }));
                }
                finally 
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.SessionInfoTB.Text = "Session Aborted";
                        this.SessionInfoTB.Foreground = Brushes.DarkRed;
                    }));
                }
            });
        }
    }
}
