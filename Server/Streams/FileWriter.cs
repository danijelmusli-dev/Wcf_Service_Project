using Contracts.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Streams
{
    public class FileWriter : IDisposable
    {
        private StreamWriter _sessionWriter;
        private StreamWriter _rejectsWriter;

        public FileWriter(string sessionFilePath, string rejectsFilePath)
        {
            this._sessionWriter = new StreamWriter(sessionFilePath, append: true);
            this._rejectsWriter = new StreamWriter(rejectsFilePath, append: true);

            // Reduce memory retained in buffers by flushing automatically
            this._sessionWriter.AutoFlush = true;
            this._rejectsWriter.AutoFlush = true;

            if (new FileInfo(sessionFilePath).Length == 0)
                this._sessionWriter.WriteLine("RowIndex,TimestampMs,PpgGreen,PpgRed,PpgIr,AccX,AccY,AccZ,HeartRate,IBI_ms");

            if (new FileInfo(rejectsFilePath).Length == 0)
                this._rejectsWriter.WriteLine("RowIndex,Reason,OriginalLine");

            this._sessionWriter.Flush();
            this._rejectsWriter.Flush();
        }

        public void LogReject(string reason, PpgSample sample)
        {
            try
            {
                this._rejectsWriter?.WriteLine($"{sample.RowIndex},{reason},{sample.ToString()}");
                this._rejectsWriter?.Flush();
            }
            catch (Exception ex) { Debug.WriteLine($"LogReject Error: {ex.Message}"); }
        }

        public void LogToSession(PpgSample sample)
        {
            try
            {
                this._sessionWriter?.WriteLine(sample.ToString());
                this._sessionWriter?.Flush();
            }
            catch (Exception ex) { Debug.WriteLine($"LogReject Error: {ex.Message}"); }
        }

        public void Dispose()
        {
            this._sessionWriter?.Dispose();
            this._rejectsWriter?.Dispose();
            this._sessionWriter = null;
            this._rejectsWriter = null;
        }

    }
}
