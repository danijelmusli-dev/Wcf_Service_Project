using System.Runtime.Serialization;

namespace Contracts.Models
{
    [DataContract]
    public class ValidationFault
    {
        [DataMember] public string ExceptionMessage { get; set; }
        [DataMember] public PpgSample RejectedSample { get; set; }

        public ValidationFault() { }

        public ValidationFault(string message, PpgSample sample)
        {
            ExceptionMessage = message;
            RejectedSample = sample;
        }
    }
}
