using Contracts.Models;
using Server.Streams;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.AnalyticHelpers
{

    public class Analytics
    {

        public delegate void EventHandler(object sender, PpgSample sample);
        public event EventHandler HrOutOfRangeWarning;
        public event EventHandler IbiSpikeWarning;
        public event EventHandler ExcessiveMotionWarning;
        public event EventHandler WeakPpgWarning;

        public int HrOutOfRangeWarningCount { get; set; }
        public int IbiSpikeWarningCount { get; set; }
        public int ExcessiveMotionWarningCount { get; set; }
        public int WeakPpgWarningCount { get; set; }

        public int HrMinBpm { get; set; }
        public int HrMaxBpm { get; set; }
        public double AccThreshold { get; set; }
        public double IbiOutOfRangePct { get; set; }
        public int PpgMinSignalThreshold { get; set; }

        public double aNorm {  get; set; }

        public double ibiDisc {  get; set; } = 0;
        public double ibi { get; set; } = 0;
        public Analytics()
        {
            this.HrMinBpm = int.Parse(ConfigurationManager.AppSettings["HrMinBpm"]);
            this.HrMaxBpm = int.Parse(ConfigurationManager.AppSettings["HrMaxBpm"]);
            this.AccThreshold = double.Parse(ConfigurationManager.AppSettings["AccMotionThreshold"]);
            this.IbiOutOfRangePct = double.Parse(ConfigurationManager.AppSettings["IbiOutOfRangePct"]);
            this.PpgMinSignalThreshold = int.Parse(ConfigurationManager.AppSettings["PpgMinSignalThreshold"]);
        }

        public void AnalizePpgSample(PpgSample prevSample, PpgSample currSample)
        {
            if (prevSample is null) return;
            if (currSample is null) return;

            aNorm = Math.Sqrt(Math.Pow(currSample.AccX.GetValueOrDefault(0.0), 2) + Math.Pow(currSample.AccY.GetValueOrDefault(0.0), 2) + Math.Pow(currSample.AccZ.GetValueOrDefault(0.0), 2));
            if (aNorm > this.AccThreshold)
            {
                this.ExcessiveMotionWarningCount += 1;
                ExcessiveMotionWarning?.Invoke(this, currSample);
            }
            ibiDisc= prevSample.IBI_ms - currSample.IBI_ms;
            ibi = this.IbiOutOfRangePct * prevSample.IBI_ms;
            if (ibiDisc > ibi)
            {
                this.IbiSpikeWarningCount += 1;
                IbiSpikeWarning?.Invoke(this, currSample);
            }

            if (currSample.HeartRate < this.HrMinBpm || currSample.HeartRate > this.HrMaxBpm)
            {
                this.HrOutOfRangeWarningCount += 1;
                HrOutOfRangeWarning?.Invoke(this, currSample);
            }

            if (currSample.PpgGreen < this.PpgMinSignalThreshold || currSample.PpgRed < this.PpgMinSignalThreshold || currSample.PpgIr < this.PpgMinSignalThreshold)
            {
                this.WeakPpgWarningCount += 1;
                WeakPpgWarning?.Invoke(this, currSample);
            }

        }

        public void ResetWarningCounts()
        {
            this.HrOutOfRangeWarningCount = 0;
            this.IbiSpikeWarningCount = 0;
            this.ExcessiveMotionWarningCount = 0;
            this.WeakPpgWarningCount = 0;
        }

    }
}
