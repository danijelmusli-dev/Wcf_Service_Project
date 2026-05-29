using Contracts.Models;
using System;
using System.Collections.Generic;
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
            _sessionWriter = new StreamWriter(sessionFilePath, append: true);
            _rejectsWriter = new StreamWriter(rejectsFilePath, append: true);

            if (new FileInfo(sessionFilePath).Length == 0)
                _sessionWriter.WriteLine("RowIndex,TimestampMs,PpgGreen,PpgRed,PpgIr,AccX,AccY,AccZ,HeartRate,IBI_ms");

            if (new FileInfo(rejectsFilePath).Length == 0)
                _rejectsWriter.WriteLine("RowIndex,Reason,OriginalLine");

            _sessionWriter.Flush();
            _rejectsWriter.Flush();
        }

        public void LogReject(string reason, PpgSample sample)
        {
            _rejectsWriter.WriteLine($"{sample.RowIndex},{reason},{sample.ToString()}");
            _rejectsWriter.Flush();
        }

        public void LogToSession(PpgSample sample)
        {
            _sessionWriter.WriteLine(sample.ToString());
            _sessionWriter.Flush();
        }

        public void Dispose() 
        {
            this._sessionWriter?.Dispose();
            this._rejectsWriter?.Dispose();
        }

    }
}
