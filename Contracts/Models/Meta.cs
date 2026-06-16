using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Models
{
    [DataContract]
    public class Meta
    {

        [DataMember] public string ParticipantId { get; set; }
        [DataMember] public string DeviceId { get; set; }
        [DataMember] public double SampleRateHz { get; set; }
        [DataMember] public long TimeStampOffsetMs { get; set; }

        public Meta(string participantId, string deviceId, PpgSample firstSample, PpgSample secondSample)
        {
            this.ParticipantId = participantId;
            this.DeviceId = deviceId;

            this.SampleRateHz = (1000 / (secondSample.TimestampMs - firstSample.TimestampMs));
            this.TimeStampOffsetMs = (secondSample.TimestampMs - firstSample.TimestampMs);
        }
        public override string ToString()
        {
            return $"{ParticipantId} | {DeviceId} | {SampleRateHz} | {TimeStampOffsetMs}"; // prilagodi poljima Meta klase
        }

    }
}
