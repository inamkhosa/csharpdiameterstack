public static class DiameterMessageCode
{
    public const int CAPABILITY_EXCHANGE = 257;

    public const int CREDIT_CONTROL = 272;

    public const int DEVICE_WATCHDOG = 280;

    public static string GetMessageName(int CommandCode, bool IsRequest)
    {
        string MessageName = "";

        switch (CommandCode)
        {
            case 257:
                if (IsRequest)
                    MessageName = "Capability-Exchange-Request";
                else
                    MessageName = "Capability-Exchange-Answer";
                break;
            case 272:
                if (IsRequest)
                    MessageName = "Credit-Control-Request";
                else
                    MessageName = "Credit-Control-Answer";
                break;
            case 280:
                if (IsRequest)
                    MessageName = "Device-WatchDog-Request";
                else
                    MessageName = "Device-WatchDog-Answer";
                break;
            case 274:
                if (IsRequest)
                    MessageName = "Abort-Session-Request";
                else
                    MessageName = "Abort-Session-Answer";
                break;
        }

        return MessageName;
    }

}
public static class DiameterAvpCode
{
    public const int Active_Day=20367;
    public const int Auth_Application_Id = 258;
    public const int Acct_Application_Id = 259;
    public const int Account_Type = 20372;
    public const int Account_Query_Method = 20346;

    public const int Card_Type = 20365;
    public const int Card_Batch = 20364;
    public const int Card_Number = 20342;
    public const int Charge_Number = 20326;
    public const int Charge_Money = 20344;
    public const int CC_Request_Type = 416;
    public const int CC_Request_Number = 415;
    public const int CC_Service_Specific_Units = 417;
    public const int Calling_Party_Address = 20336;
    public const int Called_Party_Address = 20337;
    public const int Calling_Vlr_Number = 20302;
    public const int Call_Reference_Number = 20321;
    public const int Charge_ConfirmFlag = 20347;
    public const int CC_Money = 413;
    public const int Currency_Code = 425;
    public const int Calling_CellID_Or_SAI=20303;


    public const int Destination_Host = 293;
    public const int Destination_Realm = 283;

    public const int ETopUpSessionId = 20740;
    public const int Event_Timestamp = 55;
    public const int Exponent = 429;
    public const int TimeZone = 20324;
    public const int AccessMethod = 20340;

    public const int Firmware_Revision = 267;
    public const int Host_IP_Address = 257;

    public const int Inband_Security_Id = 299;
    public const int In_Information = 20300;
    public const int Adj_Information = 21500;
    public const int Bal_Information = 21100;
    public const int Fee_Change_Information = 22116;
    public const int MSC_Address = 20322;
    public const int Money_Value = 20328;
    public const int Account_Balance_Change = 20351;
    public const int Account_Date_Change = 20352;
    public const int Service_Reason = 22122;
    public const int Agent_Name = 22316;
    public const int Notify_Flag = 22173;

    public const int Origin_Host = 264;
    public const int Origin_Realm = 296;
    public const int Origin_State_Id = 278;
    public const int Product_Name = 269;

    public const int SerialNo = 20391;
    public const int Supported_Vendor_Id = 265;
    public const int Service_Context_Id = 461;
    public const int Subscription_Id = 443;
    public const int Subscription_Id_Type = 450;
    public const int Subscription_Id_Data = 444;
    public const int Service_Identifier = 439;
    public const int Service_Information = 873;
    public const int Session_Id = 263;
    public const int SSP_Time = 20386;

    public const int Total_Cost_Flag = 20375;

    public const int Recharge_Information = 20341;
    public const int Recharge_Method = 20343;
    public const int Route_Record = 282;
    public const int Requested_Action = 436;
    public const int Real_Called_Number = 20327;
    public const int Result_Code = 268;
    public const int Request_Service_Unit = 437;


    public const int Unit_Value = 445;
    public const int Used_Service_Unit = 446;
    public const int Value_Digits = 447;
    public const int User_Name = 1;
    public const int Vendor_Id = 266;
    public const int Vendor_Specific_Application_Id = 260;

    public const int RTBP_Information = 20600;
    public const int Service_Id = 20602;
    public const int CategoryID = 20639;
    public const int ContentID = 20640;
    public const int TransctionID = 20643;
    public const int need_cnfm = 20606;

}