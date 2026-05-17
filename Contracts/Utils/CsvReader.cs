using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Utils
{
    public static class CsvReader
    {
        public static List<string> ExtractLines(string directoryName, string deviceDirectoryName, string fileName)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(directoryName);
            string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"../../../Dataset");
            string path = Path.Combine(root, directoryName, deviceDirectoryName, fileName);
            string fullPath = Path.GetFullPath(path);

            List<string> lines = new List<string>();
            try
            {
                using (StreamReader sr = new StreamReader(fullPath))
                {
                    while (!sr.EndOfStream)
                    {
                        lines.Add(sr.ReadLine());
                    }
                }
            }
            catch
            {
                return null;
            }

            // skipping the csv header
            lines.RemoveAt(0);
            return lines;
        }

    }
}
