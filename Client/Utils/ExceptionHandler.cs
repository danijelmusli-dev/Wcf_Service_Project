using Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Wcf_Service_Project.Utils
{
    public class ExceptionHandler : IDisposable
    {

        public int ValidationFaultCount { get; set; } = 0;
        public int DataFormatFaultCount { get; set; } = 0;
        public int OtherFaultCount { get; set; } = 0;
        public int TotalFaultCount { get { return ValidationFaultCount + DataFormatFaultCount; } }

        public Queue<ValidationFault> ValidationFaluts { get; set; } = new Queue<ValidationFault>();
        public Queue<DataFormatFault> DataFormatFaults { get; set; } = new Queue<DataFormatFault>();

        public void AddFaultException(FaultException fex)
        {
            if (fex is null) return;

            if (fex is FaultException<ValidationFault> vex)
            { 
                this.ValidationFaultCount++;
                this.ValidationFaluts.Enqueue(vex.Detail);
            }
            else if (fex is FaultException<DataFormatFault> dfex)
            {
                this.DataFormatFaultCount++;
                this.DataFormatFaults.Enqueue(dfex.Detail);
            }
            else
            {
                this.OtherFaultCount++;
            }

        }

        public void Reset(bool dispose = false)
        {
            this.ValidationFaultCount = 0;
            this.DataFormatFaultCount = 0;
            this.OtherFaultCount = 0;

            if (dispose) this.Dispose();
     
        }

        public void Dispose()
        {
            this.ValidationFaluts.Clear();
            this.DataFormatFaults.Clear();
            this.ValidationFaluts.TrimExcess();
            this.DataFormatFaults.TrimExcess();
        }

    }
}
