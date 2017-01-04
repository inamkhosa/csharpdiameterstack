using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
 using System.Configuration;
using System.IO;

namespace DiameterStack.Common
{
   
    /// <summary>
    /// A Logging class implementing the Singleton pattern and an internal Queue to be flushed perdiodically
    /// </summary>
    public class MessageLogger
    {
        private static MessageLogger instance;
        private static Queue<Log> logQueue;
        private static string logDir ;
        private static string logFile;
        private static int maxLogAge;
        private static int queueSize;
        private static DateTime LastFlushed = DateTime.Now;
        private static bool logEnabled = false;

        /// <summary>
        /// Private constructor to prevent instance creation
        /// </summary>
        private MessageLogger() { }

        /// <summary>
        /// An LogWriter instance that exposes a single instance
        /// </summary>
        public static MessageLogger Instance
        {
            get
            {
                // If the instance is null then create one and init the Queue
                if (instance == null)
                {
                    string logPath = string.Empty;

                    try
                    {
                        logEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings["EnableMessageLogging"]);
                    }
                    catch (Exception)
                    {
                    }
                     if (ConfigurationManager.AppSettings["StackMessageLog"] != "")
                                logPath = ConfigurationManager.AppSettings["StackMessageLog"].ToString();

                     logDir = Path.GetDirectoryName(logPath);
                     logFile = Path.GetFileName(logPath);
                    instance = new MessageLogger();
                    logQueue = new Queue<Log>();
                }
                return instance;
            }
        }

        /// <summary>
        /// The single instance method that writes to the log file
        /// </summary>
        /// <param name="message">The message to write to the log</param>
        public void Write2MessageLog(string message)
        {
            try
            {
                if (logEnabled)
                {
                    // Lock the queue while writing to prevent contention for the log file
                    lock (logQueue)
                    {
                        // Create the entry and push to the Queue
                        Log logEntry = new Log(message);
                        logQueue.Enqueue(logEntry);

                        // If we have reached the Queue Size then flush the Queue
                        if (logQueue.Count >= queueSize || DoPeriodicFlush())
                        {
                            FlushLog();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Common.StackLog.Write2ErrorLog("MessageLogger.Write2MessageLog", "error:" + e.Message + " stack:" + e.StackTrace);
            }
        }

        private bool DoPeriodicFlush()
        {
            TimeSpan logAge = DateTime.Now - LastFlushed;
            if (logAge.TotalSeconds >= maxLogAge)
            {
                LastFlushed = DateTime.Now;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Flushes the Queue to the physical log file
        /// </summary>
        private void FlushLog()
        {
            while (logQueue.Count > 0)
            {
                Log entry = logQueue.Dequeue();
                string logPath = logDir + "_" + logFile;

                // This could be optimised to prevent opening and closing the file for each write
                using (FileStream fs = File.Open(logPath, FileMode.Append, FileAccess.Write))
                {
                    using (StreamWriter log = new StreamWriter(fs))
                    {
                        log.WriteLine(string.Format("{0}\t{1}",entry.LogTime,entry.Message));
                    }
                }
            }            
        }
    }

    /// <summary>
    /// A Log class to store the message and the Date and Time the log entry was created
    /// </summary>
    public class Log
    {
        public string Message { get; set; }
        public string LogTime { get; set; }
        public string LogDate { get; set; }

        public Log(string message)
        {
            Message = message;
            LogDate = DateTime.Now.ToString("yyyy-MM-dd");
            LogTime = DateTime.Now.ToString("hh:mm:ss.fff tt");
        }
    }
}

