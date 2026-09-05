using Contracts.Models;
using Contracts.Services;
using Contracts.Utils;
using Server.Streams;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;
using System.Configuration;
using System.Text.RegularExpressions;
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

        private volatile bool _isSessionActive;
        public bool IsSessionActive => _isSessionActive;

        // Only alphanumeric, hyphen, underscore — prevents path traversal
        private static readonly Regex SafePathSegment = new Regex(@"^[a-zA-Z0-9_\-]{1,64}$", RegexOptions.Compiled);

        public void StartSession(Meta metaData)
        {
            if (metaData == null)
                throw new FaultException("metaData cannot be null");

            if (!SafePathSegment.IsMatch(metaData.ParticipantId ?? ""))
                throw new FaultException("Invalid ParticipantId — use only letters, digits, hyphens, underscores (max 64 chars)");

            if (!SafePathSegment.IsMatch(metaData.DeviceId ?? ""))
                throw new FaultException("Invalid DeviceId — use only letters, digits, hyphens, underscores (max 64 chars)");

            string configuredRoot = ConfigurationManager.AppSettings["DataRootPath"];
            string rootPath = !string.IsNullOrWhiteSpace(configuredRoot)
                ? Path.GetFullPath(configuredRoot)
                : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"../../../"));
            string folderPath = Path.Combine(rootPath, "Data", metaData.ParticipantId, metaData.DeviceId, DateTime.Now.ToString("yyyy-MM-dd"));

            // Verify resolved path stays inside Data/ to prevent traversal
            string resolvedFolder = Path.GetFullPath(folderPath);
            string dataRoot = Path.GetFullPath(Path.Combine(rootPath, "Data"));
            if (!resolvedFolder.StartsWith(dataRoot, StringComparison.OrdinalIgnoreCase))
                throw new FaultException("Resolved path is outside the Data directory");

            try { Directory.CreateDirectory(folderPath); }
            catch (Exception ex)
            {
                Debug.WriteLine($"CreateDirectory error: {ex.Message}");
                Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                    MessageBox.Show(ex.Message, "Server Error", MessageBoxButton.OK, MessageBoxImage.Error)));
                throw new FaultException($"Cannot create session directory: {ex.Message}");
            }

            string sessionFilePath = Path.Combine(folderPath, "session.csv");
            string rejectsFilePath = Path.Combine(folderPath, "rejects.csv");

            lock (_writeLock)
            {
                _fileWriter?.Dispose();
                _fileWriter = new FileWriter(sessionFilePath, rejectsFilePath);
            }

            _isSessionActive = true;
            TransferStarted?.Invoke(metaData, EventArgs.Empty);
        }

        public void PushSample(PpgSample sample)
        {
            FileWriter writer;
            lock (_writeLock) { writer = _fileWriter; }
            if (writer == null)
                throw new FaultException("No active session. Call StartSession first.");

            if (!PpgSampleValidator.ValidateSampleHR(sample))
            {
                lock (_writeLock) { _fileWriter?.LogReject("HeartRate out of range", sample); }
                OnWarningRaised?.Invoke(sample, EventArgs.Empty);
                throw new FaultException<ValidationFault>(
                    new ValidationFault("HeartRate out of range", sample),
                    new FaultReason("Validation failed"));
            }

            if (!PpgSampleValidator.ValidateSampleIBI(sample))
            {
                lock (_writeLock) { _fileWriter?.LogReject("IBI out of range", sample); }
                OnWarningRaised?.Invoke(sample, EventArgs.Empty);
                throw new FaultException<ValidationFault>(
                    new ValidationFault("IBI out of range", sample),
                    new FaultReason("Validation failed"));
            }

            if (!PpgSampleValidator.ValidateSamplePpg(sample))
            {
                lock (_writeLock) { _fileWriter?.LogReject("Negative PPG channel value", sample); }
                OnWarningRaised?.Invoke(sample, EventArgs.Empty);
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Negative PPG channel value", sample),
                    new FaultReason("Data format error"));
            }

            lock (_writeLock) { _fileWriter?.LogToSession(sample); }
            SampleReceived?.Invoke(sample, EventArgs.Empty);
        }

        public List<PpgSampleResult> PushSamples(List<PpgSample> samples)
        {
            if (samples == null)
                throw new FaultException("samples cannot be null");

            FileWriter writer;
            lock (_writeLock) { writer = _fileWriter; }
            if (writer == null)
                throw new FaultException("No active session. Call StartSession first.");

            var results = new List<PpgSampleResult>(samples.Count);

            foreach (var sample in samples)
            {
                if (sample == null)
                {
                    results.Add(new PpgSampleResult(-1, false, "Null sample in batch"));
                    continue;
                }

                string reason = null;

                if (!PpgSampleValidator.ValidateSampleHR(sample))
                    reason = "HeartRate out of range";
                else if (!PpgSampleValidator.ValidateSampleIBI(sample))
                    reason = "IBI out of range";
                else if (!PpgSampleValidator.ValidateSamplePpg(sample))
                    reason = "Negative PPG channel value";

                if (reason != null)
                {
                    lock (_writeLock) { _fileWriter?.LogReject(reason, sample); }
                    OnWarningRaised?.Invoke(sample, EventArgs.Empty);
                    results.Add(new PpgSampleResult(sample.RowIndex, false, reason));
                }
                else
                {
                    lock (_writeLock) { _fileWriter?.LogToSession(sample); }
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
                _isSessionActive = false;
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

            _isSessionActive = false;
            TransferStarted = null;
            SampleReceived = null;
            OnTransferCompleted = null;
            OnWarningRaised = null;
        }
    }
}
