using System;
using System.IO;

namespace WorkerConsumoAPIContagem
{
    public static class LogFileHelper
    {
        public static void WriteMessage(string message)
        {
            File.AppendAllText("log-polly.txt", message +
                Environment.NewLine +
                Environment.NewLine);

        }
    }
}