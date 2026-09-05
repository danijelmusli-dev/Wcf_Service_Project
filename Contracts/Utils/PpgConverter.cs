using Contracts.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Contracts.Utils
{
    public static class PpgConverter
    {
        public static PpgSample ConvertToOnePpgSample(
            string accLine, string hrLine, string bvpLine, string ibiLine,
            string participantId, int rowIndex)
        {
            var sample = new PpgSample { ParticipantId = participantId, RowIndex = rowIndex };

            try
            {
                // ACC.csv format: x,y,z,timestamp
                string[] accData = accLine.Split(',');
                if (accData.Length < 4) throw new FormatException($"ACC line has {accData.Length} columns, expected 4");
                sample.AccX = double.Parse(accData[0], CultureInfo.InvariantCulture);
                sample.AccY = double.Parse(accData[1], CultureInfo.InvariantCulture);
                sample.AccZ = double.Parse(accData[2], CultureInfo.InvariantCulture);
                sample.TimestampMs = long.Parse(accData[3]) / 1000;

                // HR.csv format: value,timestamp
                string[] hrData = hrLine.Split(',');
                if (hrData.Length < 1) throw new FormatException("HR line is empty");
                sample.HeartRate = (int)double.Parse(hrData[0], CultureInfo.InvariantCulture);

                // BVP.csv format: value,timestamp (single PPG channel, E4 BVP is differential)
                string[] bvpData = bvpLine.Split(',');
                if (bvpData.Length < 1) throw new FormatException("BVP line is empty");
                double bvpValue = double.Parse(bvpData[0], CultureInfo.InvariantCulture);
                sample.PpgGreen = bvpValue;
                sample.PpgRed = bvpValue;
                sample.PpgIr = bvpValue;

                // E4 IBI.csv: each line is a single float value in seconds
                if (ibiLine != null && double.TryParse(ibiLine.Trim(), System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out double ibiSeconds))
                    sample.IBI_ms = (int)(ibiSeconds * 1000);
                else
                    sample.IBI_ms = 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Parse error at row {rowIndex}: {ex.Message}");
            }

            return sample;
        }

        public static List<PpgSample> ConvertToPpgSamples(string participantId, string deviceName)
        {
            List<string> accLines, hrLines, bvpLines;

            try { accLines = CsvReader.ExtractLines(participantId, deviceName, "ACC.csv"); }
            catch (Exception ex) { throw new InvalidOperationException($"Failed to load ACC.csv: {ex.Message}", ex); }

            try { hrLines = CsvReader.ExtractLines(participantId, deviceName, "HR.csv"); }
            catch (Exception ex) { throw new InvalidOperationException($"Failed to load HR.csv: {ex.Message}", ex); }

            try { bvpLines = CsvReader.ExtractLines(participantId, deviceName, "BVP.csv"); }
            catch (Exception ex) { throw new InvalidOperationException($"Failed to load BVP.csv: {ex.Message}", ex); }

            List<string> ibiLines = null;
            try { ibiLines = CsvReader.ExtractLines(participantId, deviceName, "IBI.csv"); }
            catch { /* IBI is optional */ }

            var samples = new List<PpgSample>();
            int samplesNum = Math.Min(accLines.Count, Math.Min(hrLines.Count, bvpLines.Count));

            for (int i = 0; i < samplesNum; i++)
            {
                string ibiLine = (ibiLines != null && i < ibiLines.Count) ? ibiLines[i] : null;
                samples.Add(ConvertToOnePpgSample(accLines[i], hrLines[i], bvpLines[i], ibiLine, participantId, i));
            }

            return samples;
        }
    }
}
