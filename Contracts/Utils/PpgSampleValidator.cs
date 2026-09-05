using Contracts.Models;
using System.Collections.Generic;

namespace Contracts.Utils
{
    public static class PpgSampleValidator
    {
        public static bool ValidateSample(PpgSample sample)
        {
            if (sample is null) return false;
            return ValidateSampleHR(sample) && ValidateSampleIBI(sample) && ValidateSamplePpg(sample);
        }

        public static bool ValidateSampleHR(PpgSample sample)
        {
            if (sample is null) return false;
            return sample.HeartRate >= 30 && sample.HeartRate <= 220;
        }

        public static bool ValidateSampleIBI(PpgSample sample)
        {
            if (sample is null) return false;
            // IBI_ms == 0 means no IBI data for this sample — not a validation failure
            if (sample.IBI_ms == 0) return true;
            return sample.IBI_ms >= 250 && sample.IBI_ms <= 2000;
        }

        public static bool ValidateSamplePpg(PpgSample sample)
        {
            if (sample is null) return false;
            // E4 BVP is a differential signal and can be negative — no sign constraint
            return true;
        }

        public static List<PpgSample> AllValidSamples(List<PpgSample> samples)
        {
            return samples.FindAll(x => ValidateSample(x));
        }

        public static List<PpgSample> AllInvalidSamples(List<PpgSample> samples)
        {
            return samples.FindAll(x => !ValidateSample(x));
        }

        public static int RemoveAllInvalidSamples(List<PpgSample> samples)
        {
            return samples.RemoveAll(x => !ValidateSample(x));
        }
    }
}
