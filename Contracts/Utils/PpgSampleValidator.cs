using Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Utils
{
    public static class PpgSampleValidator
    {

        public static bool ValidateSample(PpgSample sample)
        {
            if (sample is null)
            { return false; }
            if (sample.PpgGreen < 0 || sample.PpgRed < 0 || sample.PpgIr < 0)
            { return false; }
            if (sample.HeartRate < 30 || sample.HeartRate > 220)
            { return false; }
            if (sample.IBI_ms < 250 || sample.IBI_ms > 2000)
            { return false; }
            return true;
        }

        public static bool ValidateSampleHR(PpgSample sample)
        {
            if (sample is null)
            { return false; }
            if (sample.HeartRate < 30 || sample.HeartRate > 220)
            { return false; }
            return true;
        }

        public static bool ValidateSampleIBI(PpgSample sample)
        {
            if (sample is null)
            { return false; }
            if (sample.IBI_ms < 250 || sample.IBI_ms > 2000)
            { return false; }
            return true;
        }

        public static bool ValidateSamplePpg(PpgSample sample)
        {
            if (sample is null)
            { return false; }
            if (sample.PpgGreen < 0 || sample.PpgRed < 0 || sample.PpgIr < 0)
            { return false; }
            return true;
        }

        public static List<PpgSample> AllValidSamples(List<PpgSample> samples)
        {
            return samples.FindAll(x => PpgSampleValidator.ValidateSample(x) == true);
        }

        public static List<PpgSample> AllInValidSamples(List<PpgSample> samples)
        {
            return samples.FindAll(x => PpgSampleValidator.ValidateSample(x) == false);
        }

        public static int RemoveAllInValidSamples(List<PpgSample> samples)
        {
            return samples.RemoveAll(x => PpgSampleValidator.ValidateSample(x) == false);
        }

    }
}
