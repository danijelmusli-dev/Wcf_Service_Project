using System;
using System.Collections.Generic;
using System.IO;

namespace Contracts.Utils
{
    public static class CsvReader
    {
        public static List<string> ExtractLines(string directoryName, string deviceDirectoryName, string fileName)
        {
            string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"../../../Dataset");
            string path = Path.Combine(root, directoryName, deviceDirectoryName, fileName);
            string fullPath = Path.GetFullPath(path);

            var lines = new List<string>();
            try
            {
                using (var sr = new StreamReader(fullPath))
                {
                    while (!sr.EndOfStream)
                        lines.Add(sr.ReadLine());
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"Cannot read '{fullPath}': {ex.Message}", ex);
            }

            if (lines.Count > 0)
                lines.RemoveAt(0);

            return lines;
        }
    }
}
