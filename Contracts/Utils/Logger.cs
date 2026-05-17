using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Utils
{
    public static class Logger
    {
        private static string GetProjectPath(string relativePath)
        {
            string root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\.."));
            return Path.Combine(root, relativePath);
        }

        private static string _clientPath = GetProjectPath(@"Client\rejected_client.csv");
        private static string _serverPath = GetProjectPath(@"Client\rejected.csv");

        public static void LogToRejectedClient(string message)
        {
            using (StreamWriter sw = new StreamWriter(_clientPath, true))
            {
                sw.WriteLine(message);
            }
        }

        public static void LogToRejectedServer(string message)
        {
            using (StreamWriter sw = new StreamWriter(_serverPath, true))
            {
                sw.WriteLine(message);
            }
        }
    }
}
