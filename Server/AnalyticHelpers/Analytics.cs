using Contracts.Models;
using System;
using System.Configuration;

namespace Server.AnalyticHelpers
{
    public class Analytics
    {
        public delegate void WarningEventHandler(object sender, PpgSample sample);

        public event WarningEventHandler HrOutOfRangeWarning;
        public event WarningEventHandler IbiSpikeWarning;
        public event WarningEventHandler ExcessiveMotionWarning;
        public event WarningEventHandler WeakPpgWarning;

        public int HrOutOfRangeWarningCount { get; private set; }
        public int IbiSpikeWarningCount { get; private set; }
        public int ExcessiveMotionWarningCount { get; private set; }
        public int WeakPpgWarningCount { get; private set; }

        public int HrMinBpm { get; }
        public int HrMaxBpm { get; }
        public double AccThreshold { get; }
        public double IbiOutOfRangePct { get; }
        public int PpgMinSignalThreshold { get; }

        public Analytics()
        {
            HrMinBpm = int.Parse(ConfigurationManager.AppSettings["HrMinBpm"]);
            HrMaxBpm = int.Parse(ConfigurationManager.AppSettings["HrMaxBpm"]);
            AccThreshold = double.Parse(ConfigurationManager.AppSettings["AccMotionThreshold"]);
            IbiOutOfRangePct = double.Parse(ConfigurationManager.AppSettings["IbiOutOfRangePct"]);
            PpgMinSignalThreshold = int.Parse(ConfigurationManager.AppSettings["PpgMinSignalThreshold"]);
        }

        public void AnalyzePpgSample(PpgSample prevSample, PpgSample currSample)
        {
            if (prevSample is null || currSample is null) return;

            double aNorm = Math.Sqrt(
                Math.Pow(currSample.AccX.GetValueOrDefault(), 2) +
                Math.Pow(currSample.AccY.GetValueOrDefault(), 2) +
                Math.Pow(currSample.AccZ.GetValueOrDefault(), 2));

            if (aNorm > AccThreshold)
            {
                ExcessiveMotionWarningCount++;
                ExcessiveMotionWarning?.Invoke(this, currSample);
            }

            double ibiDelta = prevSample.IBI_ms - currSample.IBI_ms;
            if (ibiDelta > IbiOutOfRangePct * prevSample.IBI_ms)
            {
                IbiSpikeWarningCount++;
                IbiSpikeWarning?.Invoke(this, currSample);
            }

            if (currSample.HeartRate < HrMinBpm || currSample.HeartRate > HrMaxBpm)
            {
                HrOutOfRangeWarningCount++;
                HrOutOfRangeWarning?.Invoke(this, currSample);
            }

            if (currSample.PpgGreen < PpgMinSignalThreshold ||
                currSample.PpgRed < PpgMinSignalThreshold ||
                currSample.PpgIr < PpgMinSignalThreshold)
            {
                WeakPpgWarningCount++;
                WeakPpgWarning?.Invoke(this, currSample);
            }
        }

        public void ResetWarningCounts()
        {
            HrOutOfRangeWarningCount = 0;
            IbiSpikeWarningCount = 0;
            ExcessiveMotionWarningCount = 0;
            WeakPpgWarningCount = 0;
        }
    }
}
