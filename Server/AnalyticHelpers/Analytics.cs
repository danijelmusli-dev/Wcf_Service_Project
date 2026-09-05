using Contracts.Models;
using System;
using System.Configuration;
using System.Threading;

namespace Server.AnalyticHelpers
{
    public class Analytics
    {
        public delegate void WarningEventHandler(object sender, PpgSample sample);

        public event WarningEventHandler HrOutOfRangeWarning;
        public event WarningEventHandler IbiSpikeWarning;
        public event WarningEventHandler ExcessiveMotionWarning;
        public event WarningEventHandler WeakPpgWarning;

        private int _hrOutOfRangeWarningCount;
        private int _ibiSpikeWarningCount;
        private int _excessiveMotionWarningCount;
        private int _weakPpgWarningCount;

        public int HrOutOfRangeWarningCount => _hrOutOfRangeWarningCount;
        public int IbiSpikeWarningCount => _ibiSpikeWarningCount;
        public int ExcessiveMotionWarningCount => _excessiveMotionWarningCount;
        public int WeakPpgWarningCount => _weakPpgWarningCount;

        public int HrMinBpm { get; }
        public int HrMaxBpm { get; }
        public double AccThreshold { get; }
        public double IbiOutOfRangePct { get; }
        public int PpgMinSignalThreshold { get; }

        public Analytics()
        {
            HrMinBpm = ParseInt("HrMinBpm", 30);
            HrMaxBpm = ParseInt("HrMaxBpm", 220);
            AccThreshold = ParseDouble("AccMotionThreshold", 2.5);
            IbiOutOfRangePct = ParseDouble("IbiOutOfRangePct", 0.20);
            PpgMinSignalThreshold = ParseInt("PpgMinSignalThreshold", 1000);
        }

        private static int ParseInt(string key, int fallback)
        {
            var raw = ConfigurationManager.AppSettings[key];
            return int.TryParse(raw, out int val) ? val : fallback;
        }

        private static double ParseDouble(string key, double fallback)
        {
            var raw = ConfigurationManager.AppSettings[key];
            return double.TryParse(raw, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double val) ? val : fallback;
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
                Interlocked.Increment(ref _excessiveMotionWarningCount);
                ExcessiveMotionWarning?.Invoke(this, currSample);
            }

            // Skip IBI spike check when either sample has no IBI data (IBI_ms == 0)
            if (prevSample.IBI_ms > 0 && currSample.IBI_ms > 0)
            {
                double ibiDelta = Math.Abs(prevSample.IBI_ms - currSample.IBI_ms);
                if (ibiDelta > IbiOutOfRangePct * prevSample.IBI_ms)
                {
                    Interlocked.Increment(ref _ibiSpikeWarningCount);
                    IbiSpikeWarning?.Invoke(this, currSample);
                }
            }

            if (currSample.HeartRate < HrMinBpm || currSample.HeartRate > HrMaxBpm)
            {
                Interlocked.Increment(ref _hrOutOfRangeWarningCount);
                HrOutOfRangeWarning?.Invoke(this, currSample);
            }

            // Only warn if all three channels have data and any is below threshold
            if (currSample.PpgGreen.HasValue && currSample.PpgRed.HasValue && currSample.PpgIr.HasValue)
            {
                if (currSample.PpgGreen.Value < PpgMinSignalThreshold ||
                    currSample.PpgRed.Value < PpgMinSignalThreshold ||
                    currSample.PpgIr.Value < PpgMinSignalThreshold)
                {
                    Interlocked.Increment(ref _weakPpgWarningCount);
                    WeakPpgWarning?.Invoke(this, currSample);
                }
            }
        }

        public void ResetWarningCounts()
        {
            Interlocked.Exchange(ref _hrOutOfRangeWarningCount, 0);
            Interlocked.Exchange(ref _ibiSpikeWarningCount, 0);
            Interlocked.Exchange(ref _excessiveMotionWarningCount, 0);
            Interlocked.Exchange(ref _weakPpgWarningCount, 0);
        }
    }
}
