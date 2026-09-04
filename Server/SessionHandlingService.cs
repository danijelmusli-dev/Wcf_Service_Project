using Contracts.Models;
using Contracts.Services;
using Contracts.Utils;
using Server.Streams;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;
using System.Windows;

namespace Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class SessionHandlingService : IPpgService, IDisposable
    {
        public event EventHandler TransferStarted;
        public event EventHandler SampleReceived;
        public event EventHandler OnTransferCompleted;
        public event EventHandler OnWarningRaised;

        private FileWriter _fileWriter;
        private readonly object _writeLock = new object();

        public bool IsSessionActive { get; set; }

        public void StartSession(Meta metaData)
        {
            string rootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"../../../"));
            string folderPath = Path.Combine(rootPath, "Data", metaData.ParticipantId, metaData.DeviceId, DateTime.Now.ToString("yyyy-MM-dd"));

            try { Directory.CreateDirectory(folderPath); }
            catch (Exception ex) { MessageBox.Show(ex.Message); }

            string sessionFilePath = Path.Combine(folderPath, "session.csv");
            string rejectsFilePath = Path.Combine(folderPath, "rejects.csv");

            lock (_writeLock)
            {
                _fileWriter?.Dispose();
                _fileWriter = new FileWriter(sessionFilePath, rejectsFilePath);
            }

            IsSessionActive = true;
            TransferStarted?.Invoke(metaData, EventArgs.Empty);
        }

        public void PushSample(PpgSample sample)
        {
            if (!PpgSampleValidator.ValidateSampleHR(sample))
            {
                lock (_writeLock) { _fileWriter.LogReject("HeartRate out of range", sample); }
                OnWarningRaised?.Invoke(sample, EventArgs.Empty);
                throw new FaultException<ValidationFault>(
                    new ValidationFault("HeartRate out of range", sample),
                    new FaultReason("Validation failed"));
            }

            if (!PpgSampleValidator.ValidateSampleIBI(sample))
            {
                lock (_writeLock) { _fileWriter.LogReject("IBI out of range", sample); }
                OnWarningRaised?.Invoke(sample, EventArgs.Empty);
                throw new FaultException<ValidationFault>(
                    new ValidationFault("IBI out of range", sample),
                    new FaultReason("Validation failed"));
            }

            if (!PpgSampleValidator.ValidateSamplePpg(sample))
            {
                lock (_writeLock) { _fileWriter.LogReject("Negative PPG channel value", sample); }
                OnWarningRaised?.Invoke(sample, EventArgs.Empty);
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Negative PPG channel value", sample),
                    new FaultReason("Data format error"));
            }

            lock (_writeLock) { _fileWriter.LogToSession(sample); }
            SampleReceived?.Invoke(sample, EventArgs.Empty);
        }

        public List<PpgSampleResult> PushSamples(List<PpgSample> samples)
        {
            var results = new List<PpgSampleResult>(samples.Count);

            foreach (var sample in samples)
            {
                string reason = null;

                if (!PpgSampleValidator.ValidateSampleHR(sample))
                    reason = "HeartRate out of range";
                else if (!PpgSampleValidator.ValidateSampleIBI(sample))
                    reason = "IBI out of range";
                else if (!PpgSampleValidator.ValidateSamplePpg(sample))
                    reason = "Negative PPG channel value";

                if (reason != null)
                {
                    lock (_writeLock) { _fileWriter.LogReject(reason, sample); }
                    OnWarningRaised?.Invoke(sample, EventArgs.Empty);
                    results.Add(new PpgSampleResult(sample.RowIndex, false, reason));
                }
                else
                {
                    lock (_writeLock) { _fileWriter.LogToSession(sample); }
                    SampleReceived?.Invoke(sample, EventArgs.Empty);
                    results.Add(new PpgSampleResult(sample.RowIndex, true));
                }
            }

            return results;
        }

        public void EndSession()
        {
            try
            {
                IsSessionActive = false;
                lock (_writeLock)
                {
                    _fileWriter?.Dispose();
                    _fileWriter = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("EndSession Error: " + ex);
            }

            try
            {
                Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try { OnTransferCompleted?.Invoke(null, EventArgs.Empty); }
                    catch (Exception ex) { Debug.WriteLine(ex.Message); }
                }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("EndSession Dispatcher Error: " + ex.Message);
            }
        }

        public void Dispose()
        {
            lock (_writeLock)
            {
                _fileWriter?.Dispose();
                _fileWriter = null;
            }

            IsSessionActive = false;
            TransferStarted = null;
            SampleReceived = null;
            OnTransferCompleted = null;
            OnWarningRaised = null;
        }
    }
}
