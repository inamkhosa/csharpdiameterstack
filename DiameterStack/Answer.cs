using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiameterStack
{ 
  /// <summary>
  /// @Author Inam Khosa 
  /// @Date: October, 2013
  /// </summary>
  /// 
    public class Answer : Message
    {

        StackContext stackContext;

        public Answer(StackContext stackContext)
        {
            this.stackContext = stackContext;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="resultCode"></param>
        /// <returns></returns>
        public Message CreateCEA(Request CER)
        {
            Message CEA = new Message();
            
            CEA.CommandCode = 257;

            return CEA ;
        }

        public Message CreateDWA(Message DWR)
        {
              //<DWA>  ::= < Diameter Header: 280 >
              //   { Result-Code }
              //   { Origin-Host }
              //   { Origin-Realm }
              //   [ Error-Message ]
              // * [ Failed-AVP ]
              //   [ Original-State-Id ]

            Message ceaAnswer = new Message(stackContext,0,DiameterMessageCode.DEVICE_WATCHDOG,false,false,false,false);
            ceaAnswer.EndtoEndIdentifier = ceaAnswer.GetEndToEndIdentifier();
            ceaAnswer.HopbyHopIdentifier = ceaAnswer.GetHopByHopIdentifier();
            ceaAnswer.IsRequest = false;
            ceaAnswer.CommandCode = DiameterMessageCode.DEVICE_WATCHDOG;
            ceaAnswer.ApplicationID = 0;


            //Set Mandatory Attributes
            ceaAnswer.avps.Add(new Avp(DiameterAvpCode.Result_Code, 2001) { isMandatory = true });
            ceaAnswer.avps.Add(new Avp(DiameterAvpCode.Origin_Host, stackContext.OrigionHost) { isMandatory = true });
            ceaAnswer.avps.Add(new Avp(DiameterAvpCode.Origin_Realm, stackContext.OrigionRealm) { isMandatory = true });
           
            return ceaAnswer;
        }
     


    }
}
