using Contracts.Models;
using Contracts.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

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

        [OperationBehavior(AutoDisposeParameters = true)]
        public void StartSession(Meta metaData)
        {
            // TODO :
            // - create Data/<ParticipantId>/<DeviceId>/<YYYY-MM-DD>/session.csv.
            string rootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"../../"));
            string folderPath = Path.Combine(rootPath, "Data", metaData.ParticipantId, metaData.DeviceId, DateTime.Now.ToString("yyyy-MM-dd"));
            string filePath = Path.Combine(folderPath, "session.csv");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                TransferStarted?.Invoke(this, EventArgs.Empty);
            }

        }

        [OperationBehavior(AutoDisposeParameters = true)]
        public bool PushSample(PpgSample sample)
        {
            try
            {
                PpgSampleBase.PpgSamples.Add(sample);
                SampleRecieved?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch
            {
                return false;
            }
        }


        [OperationBehavior(AutoDisposeParameters = true)]
        public bool EndSession()
        {
            OnTransferCompleted?.Invoke(this, EventArgs.Empty);
            return false;
        }

    }
}
