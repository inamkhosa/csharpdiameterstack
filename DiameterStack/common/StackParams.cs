using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;

namespace DiameterStack.Common
{
    public class StackParams
    {
        public String OrigionHost;
        public String OrigionRealm;
        int LocalPort;
        public String HostIP; 
        //public 

        public StackParams()
        {
            OrigionHost = ConfigurationManager.AppSettings["OrigionHost"];
            OrigionRealm = ConfigurationManager.AppSettings["OrigionRealm"];
            LocalPort = Convert.ToInt32(ConfigurationManager.AppSettings["LocalPort"]);
            HostIP = ConfigurationManager.AppSettings["HostIP"];
         }
    }
}
