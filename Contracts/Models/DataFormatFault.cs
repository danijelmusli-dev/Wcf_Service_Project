using System.Runtime.Serialization;

namespace Contracts.Models
{
    [DataContract]
    public class DataFormatFault
    {
        [DataMember] public string ExceptionMessage { get; set; }
        [DataMember] public PpgSample RejectedSample { get; set; }

        public DataFormatFault() { }

        public DataFormatFault(string message, PpgSample sample)
        {
            ExceptionMessage = message;
            RejectedSample = sample;
        }
    }
}
