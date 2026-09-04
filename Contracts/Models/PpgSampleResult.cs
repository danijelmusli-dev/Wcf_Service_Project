using System.Runtime.Serialization;

namespace Contracts.Models
{
    [DataContract]
    public class PpgSampleResult
    {
        [DataMember] public int RowIndex { get; set; }
        [DataMember] public bool IsValid { get; set; }
        [DataMember] public string Reason { get; set; }

        public PpgSampleResult() { }

        public PpgSampleResult(int rowIndex, bool isValid, string reason = null)
        {
            RowIndex = rowIndex;
            IsValid = isValid;
            Reason = reason;
        }
    }
}
