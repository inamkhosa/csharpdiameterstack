using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
/// <summary>
/// @Author Inam Khosa 
/// 
/// </summary>
namespace DiameterStack.Common
{
    public class Utility
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string LocalIPAddress()
        {
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string CreateSessionId()
        {
            /*<DiameterIdentity>;<high 32 bits>;<low 32 bits>[;<optional value>]
              <high 32 bits> and <low 32 bits> are decimal representations of the
               high and low 32 bits of a monotonically increasing 64-bit value.  The
               64-bit value is rendered in two part to simplify formatting by 32-bit
               processors.  At startup, the high 32 bits of the 64-bit value MAY be
               initialized to the time, and the low 32 bits MAY be initialized to
               zero.  This will for practical purposes eliminate the possibility of
               overlapping Session-Ids after a reboot, assuming the reboot process
               takes longer than a second.  Alternatively, an implementation MAY
               keep track of the increasing value in non-volatile memory.
               
             * <optional value> is implementation specific but may include a modem's
               device Id, a layer 2 address, timestamp, etc.
               Example, in which there is no optional value:
                  accesspoint7.acme.com;1876543210;523
               Example, in which there is an optional value:
                  accesspoint7.acme.com;1876543210;523;mobile@200.1.1.88*/


            string SessionID = "";
            
            String hostIdentity;

            if (ConfigurationManager.AppSettings["OrigionHost"] != "")
                hostIdentity = ConfigurationManager.AppSettings["OrigionHost"];
            else
                hostIdentity = Dns.GetHostName();

            int low32 = Sequence.Next;
            
            uint high32 = 0;

            long id = ((long)low32) << 32 | high32;

            string optional = Sequence.Next.ToString();

            SessionID = hostIdentity + ";" + id + ";" + optional;
            
            return SessionID;
        }

        public static ulong GetCurrentNTPTime()
        {
            ulong millis = Convert.ToUInt64(DateTime.UtcNow.Subtract(new DateTime(1900, 1, 1, 0, 0, 0)).TotalMilliseconds);

            return millis;
        }
    }
    static class Sequence
    {
        private static int _value = -1;
        private static readonly object m_lock = new object();

        public static int Next
        {
            get
            {
                lock (m_lock)
                {
                    if (_value == Int32.MaxValue)
                        _value = -1;
                    return ++_value;
                }
            }
        }
    }
}
