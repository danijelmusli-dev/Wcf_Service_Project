using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Models
{
    [DataContract]
    public class DataFormatFault
    {
        [DataMember] public string ExceptionMessage { get; set; }
        [DataMember] public PpgSample RejectedSample { get; set; }

        public DataFormatFault(string message, PpgSample sample)
        {
            this.ExceptionMessage = message;
            this.RejectedSample = sample;
        }

    }
}
