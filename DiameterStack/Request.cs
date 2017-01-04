using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Configuration;
using DiameterStack.Common;
namespace DiameterStack
{

    /// <summary>
    /// @Author Inam Khosa 
    /// @Date: October, 2013
    /// </summary>

    public class Request : Message
    {
      
        public Request()
        {
           
        }
        /// <summary>
        /// This method Creates CAPABILITY EXCHANGE REQUEST MESSAGE
        /// </summary>
        /// <returns></returns>

        public Message CapabilityExchangeRequest(StackContext stackContext)
        {

            Message cerReq = new Message(stackContext);
            cerReq.CommandCode = DiameterMessageCode.CAPABILITY_EXCHANGE;
            cerReq.EndtoEndIdentifier = cerReq.GetEndToEndIdentifier();
            cerReq.HopbyHopIdentifier = cerReq.GetHopByHopIdentifier();
            cerReq.ApplicationID = 0;

           
            //Set Mandatory Attributes
            cerReq.avps.Add(new Avp(DiameterAvpCode.Origin_Host, stackContext.OrigionHost) { isMandatory = true });
            cerReq.avps.Add(new Avp(DiameterAvpCode.Origin_Realm, stackContext.OrigionRealm) { isMandatory = true });
            cerReq.avps.Add(new Avp(DiameterAvpCode.Host_IP_Address, IPAddress.Parse(Utility.LocalIPAddress())) { isMandatory = true });
            cerReq.avps.Add(new Avp(DiameterAvpCode.Vendor_Id, 42452) { isMandatory = true });
            cerReq.avps.Add(new Avp(DiameterAvpCode.Product_Name, "EMI Networks (SMC-PVT) LTD.") { isMandatory = true });

            cerReq.avps.Add(new Avp(DiameterAvpCode.Origin_State_Id, 1));
            cerReq.avps.Add(new Avp(DiameterAvpCode.Supported_Vendor_Id, 10415));
            cerReq.avps.Add(new Avp(DiameterAvpCode.Auth_Application_Id, 167772151));
            //cerReq.avps.Add(new Avp(DiameterAvpCode.Inband_Security_Id, 1234)); //Not Recommended in CER/CEA Messages
            cerReq.avps.Add(new Avp(DiameterAvpCode.Acct_Application_Id, 0));

            ////Create Vendor_Specific_Application_Id Grouped Avp
            Avp avpVendorSpecificAppId = new Avp(DiameterAvpCode.Vendor_Specific_Application_Id) { isGrouped = true };
            Avp avpVendorId = new Avp(DiameterAvpCode.Vendor_Id, 11);
            Avp avpAuthApplicationId = new Avp(DiameterAvpCode.Auth_Application_Id, 167772151);
            avpVendorSpecificAppId.groupedAvps.Add(avpVendorId); //Add Avp to Grouped Avp
            avpVendorSpecificAppId.groupedAvps.Add(avpAuthApplicationId); //Add Avp to Grouped Avp

            cerReq.avps.Add(avpVendorSpecificAppId);//Now Add Avp to CER Command

            //Second Loop 
            avpVendorSpecificAppId = new Avp(DiameterAvpCode.Vendor_Specific_Application_Id) { isGrouped = true };
            avpVendorSpecificAppId.groupedAvps.Clear();
            avpVendorSpecificAppId.groupedAvps.Add(avpVendorId);
            Avp avpAcctApplicationId = new Avp(DiameterAvpCode.Acct_Application_Id, 0);
            avpVendorSpecificAppId.groupedAvps.Add(avpAcctApplicationId);
            cerReq.avps.Add(avpVendorSpecificAppId);


            cerReq.avps.Add(new Avp(DiameterAvpCode.Firmware_Revision, 1));
            
            return cerReq;
        }

        
        /// <summary>
        /// This method create Credit Control Request for DCCA Server
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="destinationHost"></param>
        /// <param name="destinationRealm"></param>
        /// <param name="requestType"></param>
        /// <param name="UserName"></param>
        /// <param name="MSIDN"></param>
        /// <param name="callingPartyAddress"></param>
        /// <param name="calledPartyAddress"></param>
        /// <param name="realCalledNumber"></param>
        /// <returns></returns>
        public Message CreditControlRequest(StackContext stackContext, string OrigionHost, string OrigionRealm, string DestinationRealm, int AuthApplicationId, CC_Request_Type CCRequestType, int CCRequestNumber, string SessionId)
        {                        
            Message ccrReq = new Message(stackContext);
            ccrReq.CommandCode = 272;
            ccrReq.EndtoEndIdentifier = ccrReq.GetEndToEndIdentifier();
            ccrReq.HopbyHopIdentifier = ccrReq.GetHopByHopIdentifier();
            ccrReq.ApplicationID = 4;

            //Set Mandatory Attributes
            ccrReq.avps.Add(new Avp(DiameterAvpCode.Session_Id, SessionId));
            ccrReq.SetSessionId(SessionId);
            //Origion and Destination attrib s

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Origin_Host, OrigionHost));
            ccrReq.avps.Add(new Avp(DiameterAvpCode.Origin_Realm, OrigionRealm));
            ccrReq.avps.Add(new Avp(DiameterAvpCode.Destination_Realm, DestinationRealm));

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Auth_Application_Id, AuthApplicationId));
            ccrReq.avps.Add(new Avp(DiameterAvpCode.CC_Request_Type, CCRequestType));
            ccrReq.avps.Add(new Avp(DiameterAvpCode.CC_Request_Number, CCRequestNumber));

            return ccrReq;

        }

    }
}
