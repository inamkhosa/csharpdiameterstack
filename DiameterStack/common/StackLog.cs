using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;
using System.Diagnostics;

namespace DiameterStack.Common
{
    public sealed class StackLog
    {
         /// <summary>
        /// Writes a Log Entry into Diameter Stack  Log
        /// </summary>
        /// <param name="EventTime"></param>
        /// <param name="EventLocation"></param>
        /// <param name="MessageToLog"></param>
        
        private static object _fileLockTrace = new object();
        private static object _fileLockError = new object();

        public static void Write2TraceLog(string EventLocation, string MessageToLog)
        {
            try
            {
                
                string logEntry = String.Empty;

                logEntry += "\r\n";

                logEntry += DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff tt") + ",";
                
                logEntry += EventLocation + ",";
                
                logEntry += MessageToLog;
                
                if (DiameterAAAStack.stackContext.EnableDebug)
                {
                    Console.WriteLine();
                    Console.WriteLine(logEntry);
                }
                if (ConfigurationManager.AppSettings["StackTraceLog"] != "")
                {
                    string logPath = string.Empty;
                    if (ConfigurationManager.AppSettings["StackTraceLog"] != null && ConfigurationManager.AppSettings["StackTraceLog"] != "")
                        logPath = ConfigurationManager.AppSettings["StackTraceLog"].ToString();
                    lock (_fileLockTrace)
                    {
                        System.IO.StreamWriter file = new System.IO.StreamWriter(logPath, true);
                        file.WriteLine(logEntry);
                        file.Close();
                    }
                }
            }
            catch (Exception exp)
            {
                string source = "EvcStackLog";
                if (!EventLog.SourceExists(source))
                    EventLog.CreateEventSource("EvcStackLog", "Application");
                EventLog.WriteEntry(source, exp.ToString(), EventLogEntryType.Error);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="EventLocation"></param>
        /// <param name="mesaage"></param>
        public static void Write2ErrorLog(string MethodName,string ErrorMessage)
        {
            try
            {
                string logPath = string.Empty;

                if (ConfigurationManager.AppSettings["StackErrorLog"] != "")
                {

                    logPath = ConfigurationManager.AppSettings["StackErrorLog"].ToString();

                    lock (_fileLockError)
                    {

                        System.IO.StreamWriter file = new System.IO.StreamWriter(logPath, true);

                        file.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":" + MethodName);

                        file.Write(ErrorMessage);

                        file.WriteLine("");
                        file.WriteLine("");

                        file.Close();
                    }
                }
            }
            catch (Exception exp)
            {
                //string source = "EvcStackErrrLog";
                
                //if (!EventLog.SourceExists(source))
                //    EventLog.CreateEventSource("EvcStackLog", "Application");

                //EventLog.WriteEntry(source, exp.ToString(), EventLogEntryType.Error);
            }
        }

       /// <summary>
       /// 
       /// </summary>
       /// <param name="diameterMessage"></param>
        //public static void Write2MessageLog(string diameterMessage)
        //{
        //    try
        //    {
        //        lock (_fileLock)
        //        {
        //            string logPath = string.Empty;

        //            if (DiameterAAAStack.stackContext.EnableMessageLogging && ConfigurationManager.AppSettings["StackMessageLog"] != "")
        //            {

        //                logPath = ConfigurationManager.AppSettings["StackMessageLog"].ToString();

        //                System.IO.StreamWriter file = new System.IO.StreamWriter(logPath, true);

        //                file.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        //                file.Write(diameterMessage);

        //                file.WriteLine("");

        //                file.Close();
        //            }
        //        }
        //    }
        //    catch (Exception exp)
        //    {
        //        //string source = "EvcStackErrrLog";

        //        //if (!EventLog.SourceExists(source))
        //        //    EventLog.CreateEventSource("EvcStackLog", "Application");

        //        //EventLog.WriteEntry(source, exp.ToString(), EventLogEntryType.Error);
        //    }
        //}



    }



}
