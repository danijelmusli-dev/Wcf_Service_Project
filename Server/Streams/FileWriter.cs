using Contracts.Models;
using System;
using System.Diagnostics;
using System.IO;

namespace Server.Streams
{
    public class FileWriter : IDisposable
    {
        private StreamWriter _sessionWriter;
        private StreamWriter _rejectsWriter;

        public FileWriter(string sessionFilePath, string rejectsFilePath)
        {
            try
            {
                _sessionWriter = new StreamWriter(sessionFilePath, append: true);
                _rejectsWriter = new StreamWriter(rejectsFilePath, append: true);
            }
            catch
            {
                _sessionWriter?.Dispose();
                throw;
            }

            _sessionWriter.AutoFlush = true;
            _rejectsWriter.AutoFlush = true;

            if (new FileInfo(sessionFilePath).Length == 0)
                _sessionWriter.WriteLine("RowIndex,TimestampMs,PpgGreen,PpgRed,PpgIr,AccX,AccY,AccZ,HeartRate,IBI_ms");

            if (new FileInfo(rejectsFilePath).Length == 0)
                _rejectsWriter.WriteLine("RowIndex,Reason,OriginalLine");
        }

        public void LogReject(string reason, PpgSample sample)
        {
            try
            {
                // Quote fields that may contain commas
                string safeReason = $"\"{reason}\"";
                string safeSample = $"\"{sample?.ToString()?.Replace("\"", "\"\"")}\"";
                _rejectsWriter?.WriteLine($"{sample?.RowIndex ?? 0},{safeReason},{safeSample}");
            }
            catch (Exception ex) { Debug.WriteLine($"LogReject Error: {ex.Message}"); }
        }

        public void LogToSession(PpgSample sample)
        {
            try
            {
                _sessionWriter?.WriteLine(sample.ToString());
            }
            catch (Exception ex) { Debug.WriteLine($"LogToSession Error: {ex.Message}"); }
        }

        public void Dispose()
        {
            _sessionWriter?.Dispose();
            _rejectsWriter?.Dispose();
            _sessionWriter = null;
            _rejectsWriter = null;
        }
    }
}
