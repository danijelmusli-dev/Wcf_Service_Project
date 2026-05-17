using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Models
{
    [DataContract]
    public class PpgSample
    {
        [DataMember] public long TimestampMs { get; set; }

        [DataMember] public double? PpgGreen { get; set; }
        [DataMember] public double? PpgRed { get; set; }
        [DataMember] public double? PpgIr { get; set; }

        [DataMember] public double? AccX { get; set; }
        [DataMember] public double? AccY { get; set; }
        [DataMember] public double? AccZ { get; set; }

        [DataMember] public int HeartRate { get; set; }
        [DataMember] public int IBI_ms { get; set; }

        [DataMember] public string ParticipantId { get; set; }
        [DataMember] public int RowIndex { get; set; }

        public override string ToString()
        {
            return $"Row {RowIndex} | " +
                   $"Timestamp: {TimestampMs} ms | " +
                   $"PPG(Green={PpgGreen?.ToString() ?? "null"}, " +
                   $"Red={PpgRed?.ToString() ?? "null"}, " +
                   $"Ir={PpgIr?.ToString() ?? "null"}) | " +
                   $"ACC(X={AccX?.ToString() ?? "null"}, " +
                   $"Y={AccY?.ToString() ?? "null"}, " +
                   $"Z={AccZ?.ToString() ?? "null"}) | " +
                   $"HR={HeartRate} bpm | " +
                   $"IBI={IBI_ms} ms | " +
                   $"Participant={ParticipantId}";
        }

    }
}
