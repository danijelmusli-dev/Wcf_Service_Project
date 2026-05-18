using Contracts.Models;
using Contracts.Services;
using Server.AnalyticHelpers;
using Server.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class SessionHandlingService : IPpgService
    {
        public delegate void EventHandler(object sender, EventArgs e);

        public EventHandler TransferStarted;
        public EventHandler SampleRecieved;
        public EventHandler OnTransferCompleted;
        public EventHandler OnWarningRaised;

        FileWriter fileWriter;

        [OperationBehavior(AutoDisposeParameters = true)]
        public void StartSession(Meta metaData)
        {
            string rootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"../../../"));
            string folderPath = Path.Combine(rootPath, "Data", metaData.ParticipantId, metaData.DeviceId, DateTime.Now.ToString("yyyy-MM-dd"));

            Directory.CreateDirectory(folderPath);

            string sessionFilePath = Path.Combine(folderPath, "session.csv");
            string rejectsFilePath = Path.Combine(folderPath, "rejects.csv");

            fileWriter = new FileWriter(sessionFilePath, rejectsFilePath);

            TransferStarted?.Invoke(metaData, EventArgs.Empty);
        }

        [OperationBehavior(AutoDisposeParameters = true)]
        public void PushSample(PpgSample sample)
        {
            if (sample.HeartRate < 30 || sample.HeartRate > 220)
            {
                fileWriter.LogToReject("HeartRate out of range", sample);
                //return;
                throw new FaultException<ValidationFault>(
                    new ValidationFault("HeartRate out of range", sample),
                    new FaultReason("Validation failed"));
            }

            if (sample.IBI_ms < 250 || sample.IBI_ms > 2000)
            {
                fileWriter.LogToReject("IBI out of range", sample);
                //return;
                throw new FaultException<ValidationFault>(
                    new ValidationFault("IBI out of range", sample),
                    new FaultReason("Validation failed"));
            }

            if (sample.PpgGreen < 0 || sample.PpgRed < 0 || sample.PpgIr < 0)
            {
                fileWriter.LogToReject("Negative PPG channel value", sample);
                //return;
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
            fileWriter.Dispose();

            OnTransferCompleted?.Invoke(this, EventArgs.Empty);
        }

    }
}
