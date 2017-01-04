using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;

namespace DiameterStack
{

    /// <summary>
    /// @Author Inam Khosa 
    /// @Date: October, 2013
    /// </summary>

    public class ListenAddress
    {
        public string IPAddress;
        public int Port;
        /// <summary>
        /// 
        /// </summary>
        public ListenAddress()
        {
            if (ConfigurationManager.AppSettings["ListenAddresses"] == "")
            {
                string listenAddress = ConfigurationManager.AppSettings["ListenAddresses"];
                string[] addrs = listenAddress.Split(new Char[';']);
               // IPAddress = addrs[0].Split(new Chart[":"])[0];
            }
        }
    }
}
