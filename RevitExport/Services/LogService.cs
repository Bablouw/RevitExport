using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitExport.Services
{
    public static class LogService
    {
        private static string LogFilePath {  get; set; }

        public static void Initialize(string logFilePath)
        {
            LogFilePath = logFilePath + "\\log.txt";
        }

        public static void LogError(string message)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath));
                File.AppendAllText(LogFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm} {message}\n");
            }
            catch
            {

            }
        }
    }
}
