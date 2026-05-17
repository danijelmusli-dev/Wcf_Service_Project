using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Models
{
    [DataContract]
    public class ValidationFault
    {
        [DataMember] public string ExceptionMessage { get; set; }
        [DataMember] public PpgSample RejectedSample { get; set; }

        public ValidationFault(string message)
        {
            this.ExceptionMessage = message;
        }

        public ValidationFault(string message, PpgSample sample)
        {
            this.ExceptionMessage = message;
            this.RejectedSample = sample;
        }

    }
}
