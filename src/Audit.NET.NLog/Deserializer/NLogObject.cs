using System;

namespace Audit.NLog
{
    public class NLogObject
    {
        public DateTime DateTimeObject { get; }
        public LogLevel Level { get; }
        public string LoggerNameObject { get; }
        public string MessageObject { get; }

        public NLogObject(string logEntry)
        {
            var div = logEntry.Split(new []{'|'}, 4);
            DateTimeObject = DateTime.Parse(div[0]);
            LoggerNameObject = div[2];
            MessageObject = div[3];

            switch (div[1].ToLower())
            {
                case "debug":
                    Level = LogLevel.Debug;
                    break;
                case "warn":
                    Level = LogLevel.Warn;
                    break;
                case "error":
                    Level = LogLevel.Error;
                    break;
                case "fatal":
                    Level = LogLevel.Fatal;
                    break;
                case "info":
                default:
                    Level = LogLevel.Info;
                    break;
            }
        }
    }
}