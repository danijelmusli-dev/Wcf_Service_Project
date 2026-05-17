using Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Utils
{
    public static class PpgConverter
    {

        public static PpgSample ConvertToOnePpgSample(string accLine, string hrLine, string ppgLine, string participantId, int rowIndex)
        {
            PpgSample sample = new PpgSample();

            string[] accData = accLine.Split(',');
            string[] hrData = hrLine.Split(new char[] { ',' }, 4, StringSplitOptions.RemoveEmptyEntries);
            string[] ibiData = hrLine.Split(new char[] { '[' }, 2, StringSplitOptions.RemoveEmptyEntries)[1].Split(']');
            string[] ppgData = ppgLine.Split(',');

            sample.TimestampMs = long.Parse(accData[1]);

            sample.PpgGreen = double.Parse(ppgData[2]);
            sample.PpgRed = double.Parse(ppgData[2]);
            sample.PpgIr = double.Parse(ppgData[2]);

            sample.AccX = double.Parse(accData[2]);
            sample.AccY = double.Parse(accData[3]);
            sample.AccZ = double.Parse(accData[4]);

            sample.HeartRate = int.Parse(hrData[2]);

            List<string> ibiValues = ibiData[0].Replace("\"", "").Replace("[", "").Replace("]", "").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<string> ibiStats = ibiData[1].Replace("\"", "").Replace("[", "").Replace("]", "").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            //  We are looking for last valid Ibi singal
            // "[662,639,651,709,733,736,740,753]"
            // "[0,0,0,0,0,0,0,0,0] => 753 is the last valid value

            int lastIndex = ibiStats.LastIndexOf("0");
            if (lastIndex >= 0 && int.TryParse(ibiValues[lastIndex], out int res))
            {
                sample.IBI_ms = res;
            }
            else { sample.IBI_ms = 0; }

            sample.ParticipantId = participantId;
            sample.RowIndex = rowIndex;

            return sample;
        }

        public static List<PpgSample> ConvertToPpgSamples(string participantId, string deviceName)
        {
            List<PpgSample> samples = new List<PpgSample>();

            List<string> accLines = CsvReader.ExtractLines(participantId, deviceName, "ACC.csv");
            List<string> hrLines = CsvReader.ExtractLines(participantId, deviceName, "HR.csv");
            List<string> ppgLines = CsvReader.ExtractLines(participantId, deviceName, "PPG.csv");

            if (accLines is null || hrLines is null || ppgLines is null)
            {
                return samples;
            }

            int samplesNum = Math.Min(accLines.Count, Math.Min(hrLines.Count, ppgLines.Count));

            for (int i = 0; i < samplesNum; i++)
            {
                PpgSample sample = ConvertToOnePpgSample(accLines[i], hrLines[i], ppgLines[i], participantId, i);
                samples.Add(sample);
            }

            return samples;
        }
    }
}
