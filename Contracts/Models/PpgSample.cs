using System.Runtime.Serialization;

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
            return $"Row {RowIndex} | Timestamp: {TimestampMs} ms | " +
                   $"PPG(G={PpgGreen ?? 0}, R={PpgRed ?? 0}, Ir={PpgIr ?? 0}) | " +
                   $"ACC(X={AccX ?? 0}, Y={AccY ?? 0}, Z={AccZ ?? 0}) | " +
                   $"HR={HeartRate} bpm | IBI={IBI_ms} ms | {ParticipantId}";
        }
    }
}
