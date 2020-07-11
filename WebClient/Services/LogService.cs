using System;
using System.Runtime.CompilerServices;

namespace FeedReader.WebClient.Services
{
    public class LogService
    {
        public void Debug(string msg, [CallerMemberName] string func = null, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            #if DEBUG
            Log("Verb", msg, func, file, line);
            #endif
        }

        public void Info(string msg, [CallerMemberName] string func = null, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            Log("Info", msg, func, file, line);
        }

        public void Warning(string msg, [CallerMemberName] string func = null, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            Log("Warn", msg, func, file, line);
        }

        public void Error(string msg, [CallerMemberName] string func = null, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            Log("Errr", msg, func, file, line);
        }

        private void Log(string level, string msg, string func, string file, int line)
        {
            Console.WriteLine($"[FeedReader.{level}]: {msg}({func}, {file}:{line}");
        }
    }
}
