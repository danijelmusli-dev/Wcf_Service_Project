using Contracts.Models;
using System;
using System.Collections.Generic;

namespace Contracts.Utils
{
    public static class PpgConverter
    {
        public static PpgSample ConvertToOnePpgSample(string accLine, string hrLine, string ppgLine, string participantId, int rowIndex)
        {
            var sample = new PpgSample();

            string[] accData = accLine.Split(',');
            string[] hrData = hrLine.Split(new[] { ',' }, 4, StringSplitOptions.RemoveEmptyEntries);
            string[] ibiData = hrLine.Split(new[] { '[' }, 2, StringSplitOptions.RemoveEmptyEntries)[1].Split(']');
            string[] ppgData = ppgLine.Split(',');

            sample.TimestampMs = long.Parse(accData[1]);

            sample.PpgGreen = double.Parse(ppgData[2]);
            sample.PpgRed = double.Parse(ppgData[3]);
            sample.PpgIr = double.Parse(ppgData[4]);

            sample.AccX = double.Parse(accData[2]);
            sample.AccY = double.Parse(accData[3]);
            sample.AccZ = double.Parse(accData[4]);

            sample.HeartRate = int.Parse(hrData[2]);

            var ibiValues = ibiData[0].Replace("\"", "").Replace("[", "").Replace("]", "")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var ibiStats = ibiData[1].Replace("\"", "").Replace("[", "").Replace("]", "")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            int lastIndex = Array.LastIndexOf(ibiStats, "0");
            if (lastIndex >= 0 && lastIndex < ibiValues.Length && int.TryParse(ibiValues[lastIndex], out int res))
                sample.IBI_ms = res;
            else
                sample.IBI_ms = 0;

            sample.ParticipantId = participantId;
            sample.RowIndex = rowIndex;

            return sample;
        }

        public static List<PpgSample> ConvertToPpgSamples(string participantId, string deviceName)
        {
            var samples = new List<PpgSample>();

            var accLines = CsvReader.ExtractLines(participantId, deviceName, "ACC.csv");
            var hrLines = CsvReader.ExtractLines(participantId, deviceName, "HR.csv");
            var ppgLines = CsvReader.ExtractLines(participantId, deviceName, "PPG.csv");

            if (accLines is null || hrLines is null || ppgLines is null)
                return samples;

            int samplesNum = Math.Min(accLines.Count, Math.Min(hrLines.Count, ppgLines.Count));

            for (int i = 0; i < samplesNum; i++)
                samples.Add(ConvertToOnePpgSample(accLines[i], hrLines[i], ppgLines[i], participantId, i));

            return samples;
        }
    }
}
