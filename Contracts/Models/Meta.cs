using System;
using System.Runtime.Serialization;

namespace Contracts.Models
{
    [DataContract]
    public class Meta
    {
        [DataMember] public string ParticipantId { get; set; }
        [DataMember] public string DeviceId { get; set; }
        [DataMember] public double SampleRateHz { get; set; }
        [DataMember] public long TimeStampOffsetMs { get; set; }

        public Meta() { }

        public Meta(string participantId, string deviceId, PpgSample firstSample, PpgSample secondSample)
        {
            this.ParticipantId = participantId;
            this.DeviceId = deviceId;

            long delta = secondSample.TimestampMs - firstSample.TimestampMs;
            this.TimeStampOffsetMs = delta;
            this.SampleRateHz = (delta != 0) ? 1000.0 / delta : 0;
        }
    }
}
