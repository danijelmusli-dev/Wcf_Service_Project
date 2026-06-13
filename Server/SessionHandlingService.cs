using Contracts.Models;
using Contracts.Services;
using Contracts.Utils;
using Server.AnalyticHelpers;
using Server.Streams;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class SessionHandlingService : IPpgService, IDisposable
    {
        // raised when the client channel closes/faults unexpectedly
        public event EventHandler ClientDisconnected;
        public event EventHandler TransferStarted;
        public event EventHandler SampleRecieved;
        public event EventHandler OnTransferCompleted;
        public event EventHandler OnWarningRaised;

        FileWriter fileWriter;

        public bool IsSessionActive { get; set; } = false;

        [OperationBehavior(AutoDisposeParameters = true)]
        public void StartSession(Meta metaData)
        {
            string rootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"../../../"));
            string folderPath = Path.Combine(rootPath, "Data", metaData.ParticipantId, metaData.DeviceId, DateTime.Now.ToString("yyyy-MM-dd"));

            try { Directory.CreateDirectory(folderPath); } 
            catch (Exception ex) { MessageBox.Show(ex.Message); }

            string sessionFilePath = Path.Combine(folderPath, "session.csv");
            string rejectsFilePath = Path.Combine(folderPath, "rejects.csv");

            fileWriter?.Dispose(); // Dispose previous writer if exists
            fileWriter = new FileWriter(sessionFilePath, rejectsFilePath);

            this.IsSessionActive = true;
            TransferStarted?.Invoke(metaData, EventArgs.Empty);
        }

        [OperationBehavior(AutoDisposeParameters = true)]
        public void PushSample(PpgSample sample)
        {
            if (!PpgSampleValidator.ValidateSampleHR(sample))
            {
                fileWriter.LogReject("HeartRate out of range", sample);
                OnWarningRaised?.Invoke(sample, EventArgs.Empty);

                throw new FaultException<ValidationFault>(
                    new ValidationFault("HeartRate out of range", sample),
                    new FaultReason("Validation failed"));
            }

            if (!PpgSampleValidator.ValidateSampleIBI(sample))
            {
                fileWriter.LogReject("IBI out of range", sample);
                OnWarningRaised?.Invoke(sample, EventArgs.Empty);

                throw new FaultException<ValidationFault>(
                    new ValidationFault("IBI out of range", sample),
                    new FaultReason("Validation failed"));
            }

            if (!PpgSampleValidator.ValidateSamplePpg(sample))
            {
                fileWriter.LogReject("Negative PPG channel value", sample);
                OnWarningRaised?.Invoke(sample, EventArgs.Empty);

                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Negative PPG channel value", sample),
                    new FaultReason("Data format error"));
            }

            fileWriter.LogToSession(sample);
            SampleRecieved?.Invoke(sample, EventArgs.Empty); // Valid sample passed
        }

        [OperationBehavior(AutoDisposeParameters = true)]
        public void EndSession()
        {
            try
            {
                this.IsSessionActive = false;
                fileWriter?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("End Session Error: " + ex);
            }

            try
            {
                Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        OnTransferCompleted?.Invoke(null, EventArgs.Empty);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void Dispose()
        {
            fileWriter?.Dispose();
            fileWriter = null;

            this.IsSessionActive = false;

            try { TransferStarted = null; } catch { }
            try { SampleRecieved = null; } catch { }
            try { OnTransferCompleted = null; } catch { }
            try { OnWarningRaised = null; } catch { }
            try { ClientDisconnected = null; } catch { }
        }

    }
}
