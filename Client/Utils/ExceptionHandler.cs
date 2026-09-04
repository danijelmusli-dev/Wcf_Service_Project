using Contracts.Models;
using System;
using System.ServiceModel;

namespace Wcf_Service_Project.Utils
{
    public class ExceptionHandler : IDisposable
    {
        public int ValidationFaultCount { get; set; }
        public int DataFormatFaultCount { get; set; }
        public int OtherFaultCount { get; set; }
        public int TotalFaultCount => ValidationFaultCount + DataFormatFaultCount + OtherFaultCount;

        public void AddRejection(PpgSampleResult result)
        {
            if (result == null || result.IsValid) return;

            if (result.Reason != null && result.Reason.Contains("PPG"))
                DataFormatFaultCount++;
            else if (result.Reason != null)
                ValidationFaultCount++;
            else
                OtherFaultCount++;
        }

        public void AddFaultException(FaultException fex)
        {
            if (fex is null) return;

            if (fex is FaultException<ValidationFault>)
                ValidationFaultCount++;
            else if (fex is FaultException<DataFormatFault>)
                DataFormatFaultCount++;
            else
                OtherFaultCount++;
        }

        public void Reset()
        {
            ValidationFaultCount = 0;
            DataFormatFaultCount = 0;
            OtherFaultCount = 0;
        }

        public void Dispose()
        {
            Reset();
        }
    }
}
