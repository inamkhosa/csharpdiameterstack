using DiameterStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiameterStack.Common;
/// <summary>
/// @Author Inam Khosa 
/// @Date: October, 2013
/// </summary>
/// 
namespace Samples
{
    /// <summary>
    /// @Author Inam Khosa 
    /// @Date: December, 2013
    /// </summary>
    public class DiameterClient

    {
        URI PeerIdentity = new URI("aaa://10.13.121.196:6583;transport=tcp;protocol=diameter");
        //URI PeerIdentity = new URI("aaa://127.0.0.1:3868;transport=tcp;protocol=diameter");
        /// <summary>
        /// GET SUBSCRIBER INFO FROM OCS
        /// </summary>
        public StackContext stackContext;

        public ServiceResult fnGetSubscriberInfo(String pMSISDN)
        {
            String vSSPTime = DateTime.Now.ToString("yyyyMMddHHmmss");

            ServiceResult res = new ServiceResult();

            try
            {
                string sessionId = Utility.CreateSessionId(); // +pMSISDN;
                Avp resavp;
                String vMgtState;
                Message ccrEvent = QueryBalance_CCR_Event(stackContext, pMSISDN, pMSISDN, 1, pMSISDN, "00", 5, 1, 1, vSSPTime, 0, sessionId);
                Message ccaResponse = null;
                int ccaResult = 0;
                try
                {
                    ccaResponse = DiameterAAAStack.SendMessage(ccrEvent, PeerIdentity);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                try
                {
                    ccaResult = (int)ccaResponse.FindAvpByName("Result-Code").AvpValue;
                }
                catch (Exception ex)
                {
                    throw new Exception("No Result-Code AVP found");
                }

                if (ccaResult == 2001)
                {
                    //Get Auth-UserState AVP
                    vMgtState = ccaResponse.FindGroupedAvp("Service-Information/Management-Status").AvpValue.ToString();
                    res.AddParam("MGTSTATE", vMgtState);

                    Avp AvpBalInfo = ccaResponse.FindGroupedAvp("Service-Information/Balance-Information");
                    resavp = AvpBalInfo.groupedAvps.Find(a => a.AvpName == "Subscriber-State");
                    res.AddParam("SUBSTATE", resavp.AvpValue.ToString());
                    byte vSubState = Convert.ToByte(resavp.AvpValue.ToString());

                    bool isLost = Convert.ToBoolean(Convert.ToInt16(vMgtState.Substring(1, 1)));
                    bool isPool = Convert.ToBoolean(Convert.ToInt16(vMgtState.Substring(0, 1)));
                    bool isSuspended = Convert.ToBoolean(Convert.ToInt16(vMgtState.Substring(2, 1)));
                    bool isBlackListed = Convert.ToBoolean(Convert.ToInt16(vMgtState.Substring(5, 1)));

                    if (isSuspended)
                        //Return Pool Subscriber State
                        res.AddParam("ACSTATE", "3");
                    else if (isPool)
                        //Return Pool Subscriber State
                        res.AddParam("ACSTATE", "5");
                    else if (isBlackListed)
                    {
                        res.AddParam("FRAUDLOCK", "1");
                        res.AddParam("ACSTATE", "4");
                    }
                    else if (isLost)
                        //Return Disable Subscriber State
                        res.AddParam("ACSTATE", "4");
                    else if (vSubState == 0)
                        //Return Idle Subscriber State
                        res.AddParam("ACSTATE", "1");
                    else if (vSubState == 1)
                        //Return Active Subscriber State
                        res.AddParam("ACSTATE", "2");

                    else //Unkown State
                        //Return Disabled Subscriber State
                        res.AddParam("ACSTATE", "4");

                    //Freeze State
                    //bool M2 = Convert.ToBoolean(Convert.ToInt16(vAuthUserState.Substring(3, 1)));
                    //Operator Barred State
                    //bool M3 = Convert.ToBoolean(Convert.ToInt16(vAuthUserState.Substring(4, 1)));
                    //Operator Disabled State
                    //bool M4 = Convert.ToBoolean(Convert.ToInt16(vAuthUserState.Substring(5, 1)));
                    //Blacklisted
                    //if (M1 || M2 || M3 || M4)
                    //return a Disabled State for all of the above locks
                    //    res.AddParam("ACSTATE", "4");

                    //Get Subscriber PeriodsM

                    resavp = AvpBalInfo.groupedAvps.Find(a => a.AvpName == "ActivePeriod");
                    res.AddParam("ACTIVESTOP", resavp.AvpValue.ToString());
                    resavp = AvpBalInfo.groupedAvps.Find(a => a.AvpName == "graceperiod");
                    res.AddParam("SUSPENDSTOP", resavp.AvpValue.ToString());
                    resavp = AvpBalInfo.groupedAvps.Find(a => a.AvpName == "disableperiod");
                    res.AddParam("DISABLESTOP", resavp.AvpValue.ToString());
                    resavp = AvpBalInfo.groupedAvps.Find(a => a.AvpName == "Language-SMS");
                    res.AddParam("LANGTYPE", resavp.AvpValue.ToString());
                    resavp = AvpBalInfo.groupedAvps.Find(a => a.AvpName == "Balance");
                    res.AddParam("BALANCE", resavp.AvpValue.ToString());

                    //Get Subscriber Info Data by Account
                    for (int i = 0; i < AvpBalInfo.groupedAvps.Count; i++)
                    {
                        Avp AvpAccInfo = AvpBalInfo.groupedAvps[i];
                        if (AvpAccInfo.AvpName.Equals("Account-Change-Info"))
                        {
                            Avp AvpAccountId = AvpAccInfo.groupedAvps.Find(a => a.AvpName == "AccountId");
                            Avp AvpAccountType = AvpAccInfo.groupedAvps.Find(a => a.AvpName == "Account-Type");
                            Avp AvpAccountBal = AvpAccInfo.groupedAvps.Find(a => a.AvpName == "Current-Account-Balance");
                            //if (Convert.ToInt32(AvpAccountType.AvpValue) == 2000)
                            //{
                            //    res.AddParam("BALANCE", AvpAccountBal.AvpValue.ToString());
                            //}
                            //For PRMMONEY Account Info
                            if (Convert.ToInt32(AvpAccountType.AvpValue) == 2001)
                            {
                                res.AddParam("PRMMONEY", AvpAccountBal.AvpValue.ToString());
                            }
                            //For PRMVOLUME "Data" Account Info
                            if (Convert.ToInt32(AvpAccountType.AvpValue) == 4203)
                            {
                                res.AddParam("PRMVOLUME", AvpAccountBal.AvpValue.ToString());
                            }
                            //For PRMMINUTE promotional minutes Account Info
                            if (Convert.ToInt32(AvpAccountType.AvpValue) == 4202)
                            {
                                res.AddParam("PRMMINUTE", AvpAccountBal.AvpValue.ToString());
                            }
                            //For PRMSM promotional SMS Account Info
                            if (Convert.ToInt32(AvpAccountType.AvpValue) == 4204)
                            {
                                res.AddParam("PRMSM", AvpAccountBal.AvpValue.ToString());
                            }

                        }
                    }
                    if (!res.ContainsParam("BALANCE"))
                        res.AddParam("BALANCE", "0");
                    if (!res.ContainsParam("PRMMONEY"))
                        res.AddParam("PRMMONEY", "0");
                    if (!res.ContainsParam("PRMVOLUME"))
                        res.AddParam("PRMVOLUME", "0");
                    if (!res.ContainsParam("PRMMINUTE"))
                        res.AddParam("PRMMINUTE", "0");
                    if (!res.ContainsParam("PRMSM"))
                        res.AddParam("PRMSM", "0");

                }
                else
                {
                    res.mRespCode = -1;
                    res.mRespMsg = "Failed To get Balance Info(CCA_T) Error# " + ccaResult.ToString();
                }
            }
            catch (Exception e)
            {
                res.mRespCode = -1;
                res.mRespMsg = "Failed To Get Subscriber Info Error# " + e.Message;
                //clsGlobalInfo._fnLogError("clsDiameterStackInterface.fnGetSubscriberInfo", "Failed To get Subscriber Info Error# " + e.Message + " Stack:" + e.StackTrace, false);
            }
            return res;
        }
        public ServiceResult fnGetSubscriberInfo2(String pMSISDN)
        {
            String vSSPTime = DateTime.Now.ToString("yyyyMMddHHmmss");

            ServiceResult res = new ServiceResult();

            try
            {
                string sessionId = Utility.CreateSessionId();// +pMSISDN;
                Avp resavp;
                String vAuthUserState;
                Message ccrIEvent = QueryBalance_CCR_Initial(stackContext, pMSISDN, pMSISDN, 1, pMSISDN, "00", 5, 1, 1, vSSPTime, 0, sessionId);
                Message ccaIResponse = null;
                try
                {
                    ccaIResponse = DiameterAAAStack.SendMessage(ccrIEvent, PeerIdentity);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                if ((int)ccaIResponse.FindAvpByName("Result-Code").AvpValue == 2001)
                {
                    //Get Auth-UserState AVP

                    vAuthUserState = ccaIResponse.FindGroupedAvp("Service-Information/IN-Information/Auth-Information/Auth-userstate").AvpValue.ToString();
                    //AuthUserState is in format CCMMMMM
                    //C1 PPS CBS AccountLifeCycle= (0=Idle, 1=Active, 2=Suspend, 3=Disable, 4=Pool)
                    byte C1 = Convert.ToByte(vAuthUserState.Substring(0, 1));
                    //C2 Pospaid AccountLifeCyle
                    if (C1 == 0)
                        //Return Idle Subscriber State
                        res.AddParam("ACSTATE", "1");
                    else if (C1 == 1)
                        //Return Active Subscriber State
                        res.AddParam("ACSTATE", "2");
                    else if (C1 == 2)
                        //Return Suspended Subscriber State
                        res.AddParam("ACSTATE", "3");
                    else if (C1 == 3)
                        //Return Disabled Subscriber State
                        res.AddParam("ACSTATE", "4");
                    else if (C1 == 4)
                        //Return Pool Subscriber State
                        res.AddParam("ACSTATE", "5");
                    //Loss Claim State
                    bool M1 = Convert.ToBoolean(Convert.ToInt16(vAuthUserState.Substring(2, 1)));
                    //Freeze State
                    bool M2 = Convert.ToBoolean(Convert.ToInt16(vAuthUserState.Substring(3, 1)));
                    //Operator Barred State
                    bool M3 = Convert.ToBoolean(Convert.ToInt16(vAuthUserState.Substring(4, 1)));
                    //Operator Disabled State
                    bool M4 = Convert.ToBoolean(Convert.ToInt16(vAuthUserState.Substring(5, 1)));
                    //Blacklisted
                    res.AddParam("FRAUDLOCK", vAuthUserState.Substring(6, 1));
                    if (M1 || M2 || M3 || M4)
                        //return a Disabled State for all of the above locks
                        res.AddParam("ACSTATE", "4");

                    Message ccrTEvent = QueryBalance_CCR_Term(stackContext, pMSISDN, pMSISDN, 1, ccrIEvent.GetSessionId());
                    Message ccaTResponse = DiameterAAAStack.SendMessage(ccrTEvent, PeerIdentity);
                    if (ccaTResponse != null && (int)ccaTResponse.FindAvpByName("Result-Code").AvpValue == 2001)
                    {
                        //Get Subscriber PeriodsM
                        Avp AvpINInfo = ccaTResponse.FindGroupedAvp("Service-Information/IN-Information");
                        resavp = AvpINInfo.groupedAvps.Find(a => a.AvpName == "ActivePeriod");
                        res.AddParam("ACTIVESTOP", resavp.AvpValue.ToString());
                        resavp = AvpINInfo.groupedAvps.Find(a => a.AvpName == "graceperiod");
                        res.AddParam("SUSPENDSTOP", resavp.AvpValue.ToString());
                        resavp = AvpINInfo.groupedAvps.Find(a => a.AvpName == "disableperiod");
                        res.AddParam("DISABLESTOP", resavp.AvpValue.ToString());
                        res.AddParam("LANGTYPE", "1");

                        //Get Subscriber Info Data by Account
                        for (int i = 0; i < AvpINInfo.groupedAvps.Count; i++)
                        {
                            Avp AvpAccInfo = AvpINInfo.groupedAvps[i];
                            if (AvpAccInfo.AvpName.Equals("AccountInfo"))
                            {
                                Avp AvpAccountId = AvpAccInfo.groupedAvps.Find(a => a.AvpName == "AccountId");
                                Avp AvpAccountType = AvpAccInfo.groupedAvps.Find(a => a.AvpName == "Account-Type");
                                Avp AvpValueDigits = AvpAccInfo.groupedAvps.Find(a => a.AvpName == "CC-Money").groupedAvps[0].groupedAvps[0];
                                Avp AvpExponent = AvpAccInfo.groupedAvps.Find(a => a.AvpName == "CC-Money").groupedAvps[0].groupedAvps[1];
                                //For Main Account Info
                                if (Convert.ToInt32(AvpAccountType.AvpValue) == 2000)
                                {
                                    res.AddParam("BALANCE", AvpValueDigits.AvpValue.ToString());
                                    res.AddParam("BALANCE_EXP", AvpExponent.AvpValue.ToString());
                                }
                                //For PRMMONEY Account Info
                                if (Convert.ToInt32(AvpAccountType.AvpValue) == 2001)
                                {
                                    res.AddParam("PRMMONEY", AvpValueDigits.AvpValue.ToString());
                                    res.AddParam("PRMMONEY_EXP", AvpExponent.AvpValue.ToString());
                                }
                                //For PRMVOLUME "Data" Account Info
                                if (Convert.ToInt32(AvpAccountType.AvpValue) == 4203)
                                {
                                    res.AddParam("PRMVOLUME", AvpValueDigits.AvpValue.ToString());
                                    res.AddParam("PRMVOLUME_EXP", AvpExponent.AvpValue.ToString());
                                }
                                //For PRMMINUTE promotional minutes Account Info
                                if (Convert.ToInt32(AvpAccountType.AvpValue) == 4202)
                                {
                                    res.AddParam("PRMMINUTE", AvpValueDigits.AvpValue.ToString());
                                    res.AddParam("PRMMINUTE_EXP", AvpExponent.AvpValue.ToString());
                                }
                                //For PRMSM promotional SMS Account Info
                                if (Convert.ToInt32(AvpAccountType.AvpValue) == 4204)
                                {
                                    res.AddParam("PRMSM", AvpValueDigits.AvpValue.ToString());
                                    res.AddParam("PRMSM_EXP", AvpExponent.AvpValue.ToString());
                                }

                            }
                        }
                        if (!res.ContainsParam("BALANCE"))
                            res.AddParam("BALANCE", "0");
                        if (!res.ContainsParam("PRMMONEY"))
                            res.AddParam("PRMMONEY", "0");
                        if (!res.ContainsParam("PRMVOLUME"))
                            res.AddParam("PRMVOLUME", "0");
                        if (!res.ContainsParam("PRMMINUTE"))
                            res.AddParam("PRMMINUTE", "0");
                        if (!res.ContainsParam("PRMSM"))
                            res.AddParam("PRMSM", "0");

                    }
                    else
                    {
                        res.mRespCode = -1;
                        res.mRespMsg = "Failed To get Balance Info(CCA_T) Error# " + ccaTResponse.FindAvpByName("Result-Code").AvpValue.ToString();

                    }
                }
                else
                {
                    //Terminate Session
                    Message ccrTEvent = QueryBalance_CCR_Term(stackContext, pMSISDN, pMSISDN, 1, ccrIEvent.GetSessionId());
                    Message ccaTResponse = DiameterAAAStack.SendMessage(ccrTEvent, PeerIdentity);

                    res.mRespCode = -1;
                    res.mRespMsg = "Failed To get Balance Info (CCA_I) Error# " + ccaIResponse.FindAvpByName("Result-Code").AvpValue.ToString();

                }

            }
            catch (Exception e)
            {
                res.mRespCode = -1;
                res.mRespMsg = "Failed To Get Subscriber Info Error# " + e.StackTrace;
                //clsGlobalInfo._fnLogError("clsDiameterStackInterface.fnGetSubscriberInfo", "Failed To get Subscriber Info Error# " + e.Message + " Stack:" + e.StackTrace, false);
            }
            return res;
        }

        /// <summary>
        ///  
        /// </summary>
        /// <param name="context"></param>
        /// <param name="MSISDN"></param>
        /// <param name="AccountType"></param>
        /// <param name="ReChargeAmount"></param>
        /// <param name="ValidatityPeriod"></param>
        ///
        public ServiceResult fnRechargeSubscriber(string _pTranRef, string _pMsisdn, int _pFaceValue, int _pValidityPeriod)
        {
            ServiceResult res = new ServiceResult();
            Avp resavp;
            try
            {
                //processing
                String vSSPTime = DateTime.Now.ToString("yyyyMMddHHmmss");

                string sessionId = Utility.CreateSessionId();

                Message ccrEReq = Recharge_CCR_Event(stackContext, _pTranRef, 0, _pTranRef, DateTime.Now.ToString("yyyyMMdd"), _pTranRef, vSSPTime, 2000, 0, _pMsisdn, _pFaceValue, _pValidityPeriod, sessionId);

                Message ccaEResponse = DiameterAAAStack.SendMessage(ccrEReq, PeerIdentity);
                //if CCA_I is success
                if ((int)ccaEResponse.FindAvpByName("Result-Code").AvpValue == 2001)
                {
                    //Get Subscriber Periods
                    Avp AvpAdjInfo = ccaEResponse.FindGroupedAvp("Service-Information/Adjustment-Information");
                    resavp = AvpAdjInfo.groupedAvps.Find(a => a.AvpName.ToLower() == "activeperiod");
                    res.AddParam("ACTIVESTOP", resavp.AvpValue.ToString());
                    resavp = AvpAdjInfo.groupedAvps.Find(a => a.AvpName.ToLower() == "graceperiod");
                    res.AddParam("SUSPENDSTOP", resavp.AvpValue.ToString());
                    resavp = AvpAdjInfo.groupedAvps.Find(a => a.AvpName.ToLower() == "disableperiod");
                    res.AddParam("DISABLESTOP", resavp.AvpValue.ToString());
                    resavp = AvpAdjInfo.groupedAvps.Find(a => a.AvpName.ToLower() == "new-balance");
                    res.AddParam("BALANCE", resavp.AvpValue.ToString());
                }
                else
                {
                    res.mRespCode = -1;
                    res.mRespMsg = "Failed To Recharge(CCA_E) Error# " + ccaEResponse.FindAvpByName("Result-Code").AvpValue.ToString();

                }

            }
            catch (Exception e)
            {
                res.mRespCode = -1;
                res.mRespMsg = "Failed To Recharge Error# " + e.Message;
            }
            return res;
        }
        public ServiceResult fnPromoteSubscriber(string _pTranRef, int _pPrmType, string _pMsisdn, int _pFlag, int _pPrmVal)
        {
            ServiceResult res = new ServiceResult();
            Avp resavp;
            try
            {
                //processing
                String vSSPTime = DateTime.Now.ToString("yyyyMMddHHmmss");

                string sessionId = Utility.CreateSessionId();
                int vAccountType = 0;
                //Load Attributes
                if (_pPrmType == 1)
                    vAccountType = 2001;
                else if (_pPrmType == 2)
                    vAccountType = 4204;
                else if (_pPrmType == 3)
                    vAccountType = 4202;
                else if (_pPrmType == 4)
                    vAccountType = 4203;

                Message ccrEReq = Recharge_CCR_Event(stackContext, _pTranRef, 0, _pTranRef, DateTime.Now.ToString("yyyyMMdd"), _pTranRef, vSSPTime, vAccountType, 0, _pMsisdn, _pPrmVal, _pFlag, sessionId);

                Message ccaEResponse = DiameterAAAStack.SendMessage(ccrEReq, PeerIdentity);
                //if CCA_I is success
                if ((int)ccaEResponse.FindAvpByName("Result-Code").AvpValue == 2001)
                {
                        //Get Subscriber Periods
                        Avp AvpAdjInfo = ccaEResponse.FindGroupedAvp("Service-Information/Adjustment-Information");
                        resavp = AvpAdjInfo.groupedAvps.Find(a => a.AvpName.ToLower() == "activeperiod");
                        res.AddParam("ACTIVESTOP", resavp.AvpValue.ToString());
                        resavp = AvpAdjInfo.groupedAvps.Find(a => a.AvpName.ToLower() == "graceperiod");
                        res.AddParam("SUSPENDSTOP", resavp.AvpValue.ToString());
                        resavp = AvpAdjInfo.groupedAvps.Find(a => a.AvpName.ToLower() == "disableperiod");
                        res.AddParam("DISABLESTOP", resavp.AvpValue.ToString());
                        resavp = AvpAdjInfo.groupedAvps.Find(a => a.AvpName.ToLower() == "new-balance");
                        res.AddParam("BALANCE", resavp.AvpValue.ToString());
                }
                else
                {
                    res.mRespCode = -1;
                    res.mRespMsg = "Failed To Recharge(CCA_E) Error# " + ccaEResponse.FindAvpByName("Result-Code").AvpValue.ToString();

                }

            }
            catch (Exception e)
            {
                res.mRespCode = -1;
                res.mRespMsg = "Failed To Recharge Error# " + e.Message;
            }
            return res;
        }
        public ServiceResult fnRechargeSubscriberOld(string _pTranRef, string _pMsisdn, int _pFaceValue, int _pValidityPeriod)
        {
            ServiceResult res = new ServiceResult();
            Avp resavp;
            try
            {
                //processing
                String vSSPTime = DateTime.Now.ToString("yyyyMMddHHmmss");

                string sessionId = Utility.CreateSessionId();

                Message ccrIReq = Recharge_CCR_Initial(stackContext, _pTranRef, 0, _pTranRef, DateTime.Now.ToString("yyyyMMdd"), _pTranRef, vSSPTime, 2000, 0, _pMsisdn, _pFaceValue, _pValidityPeriod, sessionId);
                ccrIReq.PrintMessage();
                Message ccaIResponse = DiameterAAAStack.SendMessage(ccrIReq, PeerIdentity);
                //if CCA_I is success
                if ((int)ccaIResponse.FindAvpByName("Result-Code").AvpValue == 2001)
                {

                    Message ccrTReq = Recharge_CCR_Terminate(stackContext, _pMsisdn, _pMsisdn, sessionId);
                    Message ccaTResponse = DiameterAAAStack.SendMessage(ccrTReq, PeerIdentity);
                    if ((int)ccaTResponse.FindAvpByName("Result-Code").AvpValue == 2001)
                    {
                        //Get Subscriber Periods
                        Avp AvpINInfo = ccaTResponse.FindGroupedAvp("Service-Information/IN-Information");
                        resavp = AvpINInfo.groupedAvps.Find(a => a.AvpName == "ActivePeriod");
                        res.AddParam("ACTIVESTOP", resavp.AvpValue.ToString());
                        Avp AvpValueDigits = AvpINInfo.groupedAvps.Find(a => a.AvpName == "CC-Money").groupedAvps[0].groupedAvps[0];
                        //load attributes
                        res.AddParam("BALANCE", AvpValueDigits.AvpValue.ToString());
                    }
                    else
                    {
                        res.mRespCode = -1;
                        res.mRespMsg = "Failed To Recharge(CCA_T) Error# " + ccaTResponse.FindAvpByName("Result-Code").AvpValue.ToString();

                    }
                }
                else
                {
                    res.mRespCode = -1;
                    res.mRespMsg = "Failed To Recharge(CCA_I) Error# " + ccaIResponse.FindAvpByName("Result-Code").AvpValue.ToString();

                }

            }
            catch (Exception e)
            {
                res.mRespCode = -1;
                res.mRespMsg = "Failed To Recharge Error# " + e.Message;
            }
            return res;
        }
        public ServiceResult fnPromoteSubscriberOld(string _pTranRef, int _pPrmType, string _pMsisdn, int _pFlag, int _pPrmVal)
        {
            ServiceResult res = new ServiceResult();
            Avp resavp;
            try
            {
                //processing
                String vSSPTime = DateTime.Now.ToString("yyyyMMddHHmmss");

                string sessionId = Utility.CreateSessionId();
                int vAccountType = 0;
                int vCardType=0;
                //Load Attributes
                if (_pPrmType == 1)
                    vAccountType = 2001;
                else if (_pPrmType == 2)
                    vAccountType = 4204;
                else if (_pPrmType == 3)
                    vAccountType = 4202;
                else if (_pPrmType == 4)
                {
                    //vAccountType = 4203;
                    vAccountType = 2000;
                    vCardType = 4;
                }
                Message ccrIReq = Recharge_CCR_Initial(stackContext, _pTranRef, vCardType, _pTranRef, DateTime.Now.ToString("yyyyMMdd"), _pTranRef, vSSPTime, vAccountType, 0, _pMsisdn, _pPrmVal, 0, sessionId);

                Message ccaIResponse = DiameterAAAStack.SendMessage(ccrIReq, PeerIdentity);
                //if CCA_I is success
                if ((int)ccaIResponse.FindAvpByName("Result-Code").AvpValue == 2001)
                {

                    Message ccrTReq = Recharge_CCR_Terminate(stackContext, _pMsisdn, _pMsisdn, sessionId);
                    Message ccaTResponse = DiameterAAAStack.SendMessage(ccrTReq, PeerIdentity);
                    if ((int)ccaTResponse.FindAvpByName("Result-Code").AvpValue == 2001)
                    {
                        //Get Subscriber Periods
                        Avp AvpINInfo = ccaTResponse.FindGroupedAvp("Service-Information/IN-Information");
                        resavp = AvpINInfo.groupedAvps.Find(a => a.AvpName == "ActivePeriod");
                        res.AddParam("ACTIVESTOP", resavp.AvpValue.ToString());
                        Avp AvpValueDigits = AvpINInfo.groupedAvps.Find(a => a.AvpName == "CC-Money").groupedAvps[0].groupedAvps[0];
                        //Load Attributes
                        if (_pPrmType == 1)
                            res.AddParam("PRMMONEY", AvpValueDigits.AvpValue.ToString());
                        else if (_pPrmType == 2)
                            res.AddParam("PRMSM", AvpValueDigits.AvpValue.ToString());
                        else if (_pPrmType == 3)
                            res.AddParam("PRMMINUTE", AvpValueDigits.AvpValue.ToString());
                        else if (_pPrmType == 4)
                            res.AddParam("PRMVOLUME", AvpValueDigits.AvpValue.ToString());
                    }
                    else
                    {
                        res.mRespCode = -1;
                        res.mRespMsg = "Failed To Recharge(CCA_T) Error# " + ccaTResponse.FindAvpByName("Result-Code").AvpValue.ToString();

                    }
                }
                else
                {
                    res.mRespCode = -1;
                    res.mRespMsg = "Failed To Recharge(CCA_I) Error# " + ccaIResponse.FindAvpByName("Result-Code").AvpValue.ToString();

                }



            }
            catch (Exception e)
            {
                res.mRespCode = -1;
                res.mRespMsg = "Failed To Execute Request# " + e.Message;
            }
            return res;
        }
        public ServiceResult fnRcgSubWithPrm(string _pTranRef, string _pMsisdn, int _pAtRcgValue, int _pValidityPeriod, int _pPrmMoney, int _pPrmSms, int _pPrmMinutes, int _pPrmGprsVolume)
        {
            ServiceResult res = new ServiceResult();
            Avp resavp;
            try
            {
                //processing
                String vSSPTime = DateTime.Now.ToString("yyyyMMddHHmmss");

                string sessionId = Utility.CreateSessionId();

                Message ccrIReq = Recharge_CCR_Initial(stackContext, _pTranRef, 0, _pTranRef, DateTime.Now.ToString("yyyyMMdd"), _pTranRef, vSSPTime, 2000, 0, _pMsisdn, _pAtRcgValue, _pValidityPeriod, sessionId);

                Message ccaIResponse = DiameterAAAStack.SendMessage(ccrIReq, PeerIdentity);
                //if CCA_I is success
                if ((int)ccaIResponse.FindAvpByName("Result-Code").AvpValue == 2001)
                {

                    Message ccrTReq = Recharge_CCR_Terminate(stackContext, _pMsisdn, _pMsisdn, sessionId);
                    Message ccaTResponse = DiameterAAAStack.SendMessage(ccrTReq, PeerIdentity);
                    if ((int)ccaTResponse.FindAvpByName("Result-Code").AvpValue == 2001)
                    {
                        //Get Subscriber Periods
                        Avp AvpINInfo = ccaTResponse.FindGroupedAvp("Service-Information/IN-Information");
                        resavp = AvpINInfo.groupedAvps.Find(a => a.AvpName == "ActivePeriod");
                        res.AddParam("ACTIVESTOP", resavp.AvpValue.ToString());
                        Avp AvpValueDigits = AvpINInfo.groupedAvps.Find(a => a.AvpName == "CC-Money").groupedAvps[0].groupedAvps[0];
                        //load attributes
                        res.AddParam("BALANCE", AvpValueDigits.AvpValue.ToString());
                        if (_pValidityPeriod != 0)
                        {
                        }
                        //issue promotion cmd
                        if (_pPrmMoney != 0)
                        {
                            ServiceResult resPrm = fnPromoteSubscriber(_pTranRef, 1, _pMsisdn, 0, _pPrmMoney);
                            if (Convert.ToInt32(resPrm.mRespCode) != 0)
                                throw new Exception(" fnPromoteSubscriber:PRMTYPE=PRMMONEY " + resPrm.mRespMsg);
                            //get new PROM money bal
                            res.AddParam("PRMMONEY", resPrm.GetParamValue("PRMMONEY"));

                        }
                        else if (_pPrmSms != 0)
                        {
                            ServiceResult resPrm = fnPromoteSubscriber(_pTranRef, 2, _pMsisdn, 0, _pPrmSms);
                            if (Convert.ToInt32(resPrm.mRespCode) != 0)
                                throw new Exception(" fnPromoteSubscriber:PRMTYPE=PRMSM " + resPrm.mRespMsg);
                            //get new PRMSM bal
                            res.AddParam("PRMSM", resPrm.GetParamValue("PRMSM"));
                        }
                        else if (_pPrmMinutes != 0)
                        {
                            ServiceResult resPrm = fnPromoteSubscriber(_pTranRef, 3, _pMsisdn, 0, _pPrmMinutes);
                            if (Convert.ToInt32(resPrm.mRespCode) != 0)
                                throw new Exception(" fnPromoteSubscriber:PRMTYPE=PRMMINUTE " + resPrm.mRespMsg);
                            //get new PRMMINUTE bal
                            res.AddParam("PRMMINUTE", resPrm.GetParamValue("PRMMINUTE"));
                        }
                        else if (_pPrmGprsVolume != 0)
                        {
                            ServiceResult resPrm = fnPromoteSubscriber(_pTranRef, 4, _pMsisdn, 0, _pPrmGprsVolume);
                            if (Convert.ToInt32(resPrm.mRespCode) != 0)
                                throw new Exception(" fnPromoteSubscriber:PRMTYPE=PRMVOLUME " + resPrm.mRespMsg);
                            //get new PRMVOLUME bal
                            res.AddParam("PRMVOLUME", resPrm.GetParamValue("PRMVOLUME"));
                        }
                    }
                    else
                    {
                        res.mRespCode = -1;
                        res.mRespMsg = "Failed To Recharge(CCA_T) Error# " + ccaTResponse.FindAvpByName("Result-Code").AvpValue.ToString();

                    }
                }
                else
                {
                    res.mRespCode = -1;
                    res.mRespMsg = "Failed To Recharge(CCA_I) Error# " + ccaIResponse.FindAvpByName("Result-Code").AvpValue.ToString();

                }


            }
            catch (Exception e)
            {
                res.mRespCode = -1;
                res.mRespMsg = "Failed To Execute Request# " + e.Message;
            }
            return res;
        }
        public ServiceResult fnActivateSubscriber(string pMobileNo, bool pFlag)
        {
            ServiceResult res = new ServiceResult();
            try
            {

                //res.AddParam("",xxx);
            }
            catch (Exception e)
            {
                res.mRespCode = -1;
                res.mRespMsg = "Failed To Execute Request# " + e.Message;
            }
            return res;
        }
        public ServiceResult fnActivateSubscriberGprs(string _pTranRef, string _pMsisdn, int _pType)
        {
            ServiceResult res = new ServiceResult();
            try
            {

                //res.AddParam("",xxx);

            }
            catch (Exception e)
            {
                res.mRespCode = -1;
                res.mRespMsg = "Failed To Execute Request# " + e.Message;
            }
            return res;
        }

        private Message QueryBalance_CCR_Initial(StackContext stackContext, string userName, string MSIDN, ulong requestedServiceUnits, string callingPartyAddress, string calledPartyAddress, int AccountQueryMethod, int QueryMode, int ChargeConfirmFlag, string SSPTime, int TotalCostFlag, string sessionId)
        {
            Request req = new Request();

            Message ccrReq = req.CreditControlRequest(stackContext, stackContext.OrigionHost, stackContext.OrigionRealm, "www.huawei.com", 4, CC_Request_Type.INITIAL_REQUEST, 0, sessionId);

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Destination_Host, "cbp104"));
            ccrReq.avps.Add(new Avp(DiameterAvpCode.Service_Context_Id, "manager@huawei.com"));
            ccrReq.avps.Add(new Avp(DiameterAvpCode.User_Name, userName));

            long seconds = Convert.ToInt64((DateTime.UtcNow - new DateTime(1900, 1, 1, 0, 0, 0)).TotalSeconds);
            //ccrReq.avps.Add(new Avp(DiameterAvpCode.Event_Timestamp, 14236589681638796952));
            ccrReq.avps.Add(new Avp(DiameterAvpCode.Event_Timestamp, seconds));

            //Create Subscription-Id Grouped Avp
            Avp avpSubscriptionId = new Avp(DiameterAvpCode.Subscription_Id) { isGrouped = true };
            Avp avpSubscriptionIdType = new Avp(DiameterAvpCode.Subscription_Id_Type, Subscription_Id_Type.END_USER_E164);
            Avp avpSubscriptionIdData = new Avp(DiameterAvpCode.Subscription_Id_Data, MSIDN);
            avpSubscriptionId.groupedAvps.Add(avpSubscriptionIdType); //Add Avp to Grouped Avp
            avpSubscriptionId.groupedAvps.Add(avpSubscriptionIdData); //Add Avp to Grouped Avp
            ccrReq.avps.Add(avpSubscriptionId);
            //End Grouped Avp

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Service_Identifier, 1));

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Route_Record, "scp1"));

            // Requested Service Unit Avp 
            Avp avpRequestedServiceUnits = new Avp(DiameterAvpCode.Request_Service_Unit) { isGrouped = true };
            Avp avpCCServiceSpecificUnits = new Avp(DiameterAvpCode.CC_Service_Specific_Units, requestedServiceUnits);
            avpRequestedServiceUnits.groupedAvps.Add(avpCCServiceSpecificUnits);
            ccrReq.avps.Add(avpRequestedServiceUnits);

            //Create Subscription-Id Grouped Avp
            Avp ServiceInformation = new Avp(DiameterAvpCode.Service_Information) { isGrouped = true, isVendorSpecific = true, VendorId = DiameterVendors._3GPP };

            //In-Information 
            Avp avpInInformation = new Avp(DiameterAvpCode.In_Information) { isGrouped = true, isVendorSpecific = true, VendorId = DiameterVendors.Huawei };

            Avp avpCallingParty = new Avp(DiameterAvpCode.Calling_Party_Address, callingPartyAddress) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp avpCalledParty = new Avp(DiameterAvpCode.Called_Party_Address, calledPartyAddress) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp avpAccountQueryMethod = new Avp(DiameterAvpCode.Account_Query_Method, AccountQueryMethod) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp avpQueryMode = new Avp(DiameterAvpCode.AccessMethod, 1) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp avpChargeConfirmFlag = new Avp(DiameterAvpCode.Charge_ConfirmFlag, ChargeConfirmFlag) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp avpSSPTime = new Avp(DiameterAvpCode.SSP_Time, SSPTime) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp avpTotalCostFlag = new Avp(DiameterAvpCode.Total_Cost_Flag, TotalCostFlag) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };

            avpInInformation.groupedAvps.Add(avpCallingParty);
            avpInInformation.groupedAvps.Add(avpCalledParty);
            avpInInformation.groupedAvps.Add(avpAccountQueryMethod);

            avpInInformation.groupedAvps.Add(avpQueryMode);
            avpInInformation.groupedAvps.Add(avpChargeConfirmFlag);
            avpInInformation.groupedAvps.Add(avpSSPTime);
            avpInInformation.groupedAvps.Add(avpTotalCostFlag);

            //Add In Information Inside Service Information
            ServiceInformation.groupedAvps.Add(avpInInformation);

            //Add Service Information to CCR
            ccrReq.avps.Add(ServiceInformation);
            return ccrReq;
        }
        private Message QueryBalance_CCR_Event(StackContext stackContext, string userName, string MSIDN, ulong requestedServiceUnits, string callingPartyAddress, string calledPartyAddress, int AccountQueryMethod, int QueryMode, int ChargeConfirmFlag, string SSPTime, int TotalCostFlag, string sessionId)
        {
            Request req = new Request();

            Message ccrReq = req.CreditControlRequest(stackContext, stackContext.OrigionHost, stackContext.OrigionRealm, "www.huawei.com", 4, CC_Request_Type.EVENT_REQUEST, 0, sessionId);

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Destination_Host, "cbp104"));
            ccrReq.avps.Add(new Avp(DiameterAvpCode.Service_Context_Id, "QueryBalance@huawei.com"));
            ccrReq.avps.Add(new Avp(DiameterAvpCode.User_Name, userName));

            long seconds = Convert.ToInt64((DateTime.UtcNow - new DateTime(1900, 1, 1, 0, 0, 0)).TotalSeconds);
            //ccrReq.avps.Add(new Avp(DiameterAvpCode.Event_Timestamp, 14236589681638796952));
            ccrReq.avps.Add(new Avp(DiameterAvpCode.Requested_Action, 2));
            ccrReq.avps.Add(new Avp(DiameterAvpCode.Event_Timestamp, seconds));

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Service_Identifier, 1));

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Route_Record, "c00105987"));
            //Create Subscription-Id Grouped Avp
            Avp avpSubscriptionId = new Avp(DiameterAvpCode.Subscription_Id) { isGrouped = true };
            Avp avpSubscriptionIdType = new Avp(DiameterAvpCode.Subscription_Id_Type, Subscription_Id_Type.END_USER_E164);
            Avp avpSubscriptionIdData = new Avp(DiameterAvpCode.Subscription_Id_Data, MSIDN);
            avpSubscriptionId.groupedAvps.Add(avpSubscriptionIdType); //Add Avp to Grouped Avp
            avpSubscriptionId.groupedAvps.Add(avpSubscriptionIdData); //Add Avp to Grouped Avp
            ccrReq.avps.Add(avpSubscriptionId);
            //End Grouped Avp

      
            // Requested Service Unit Avp 
            //Avp avpRequestedServiceUnits = new Avp(DiameterAvpCode.Request_Service_Unit) { isGrouped = true };
            //Avp avpCCServiceSpecificUnits = new Avp(DiameterAvpCode.CC_Service_Specific_Units, requestedServiceUnits);
            //avpRequestedServiceUnits.groupedAvps.Add(avpCCServiceSpecificUnits);
            //ccrReq.avps.Add(avpRequestedServiceUnits);

            //Create Subscription-Id Grouped Avp
            Avp ServiceInformation = new Avp(DiameterAvpCode.Service_Information) { isGrouped = true, isVendorSpecific = true, VendorId = DiameterVendors._3GPP };

            //In-Information 
            Avp avpBalInformation = new Avp(DiameterAvpCode.Bal_Information) { isGrouped = true, isVendorSpecific = true, VendorId = DiameterVendors.Huawei };

            Avp avpCallingParty = new Avp(DiameterAvpCode.Calling_Party_Address, callingPartyAddress) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp avpCalledParty = new Avp(DiameterAvpCode.Called_Party_Address, calledPartyAddress) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp avpCallingVlr = new Avp(DiameterAvpCode.Calling_Vlr_Number, "923450000614") { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp avpCallingCellID = new Avp(DiameterAvpCode.Calling_CellID_Or_SAI, "") { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp avpMSCAddr = new Avp(DiameterAvpCode.MSC_Address, "") { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp avpAccessMethod = new Avp(DiameterAvpCode.AccessMethod, 1) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp avpAccountQueryMethod = new Avp(DiameterAvpCode.Account_Query_Method, 1) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp avpSSPTime = new Avp(DiameterAvpCode.SSP_Time, SSPTime) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };

            avpBalInformation.groupedAvps.Add(avpCallingParty);
            avpBalInformation.groupedAvps.Add(avpCalledParty);
            avpBalInformation.groupedAvps.Add(avpCallingVlr);
            avpBalInformation.groupedAvps.Add(avpCallingCellID);
            avpBalInformation.groupedAvps.Add(avpMSCAddr);
            avpBalInformation.groupedAvps.Add(avpAccessMethod);
            avpBalInformation.groupedAvps.Add(avpAccountQueryMethod);
            avpBalInformation.groupedAvps.Add(avpSSPTime);

            //Add In Information Inside Service Information
            ServiceInformation.groupedAvps.Add(avpBalInformation);

            //Add Service Information to CCR
            ccrReq.avps.Add(ServiceInformation);
            return ccrReq;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stackContext"></param>
        /// <param name="userName"></param>
        /// <param name="MSIDN"></param>
        /// <param name="requestedServiceUnits"></param>
        /// <param name="callingPartyAddress"></param>
        /// <param name="calledPartyAddress"></param>
        /// <param name="AccountQueryMethod"></param>
        /// <param name="QueryMode"></param>
        /// <param name="ChargeConfirmFlag"></param>
        /// <param name="SSPTime"></param>
        /// <param name="TotalCostFlag"></param>
        /// <returns></returns>
        private Message QueryBalance_CCR_Term(StackContext stackContext, string userName, string MSIDN, ulong requestedServiceUnits, string sessionId)
        {
            Request req = new Request();

            Message ccrReq = req.CreditControlRequest(stackContext, stackContext.OrigionHost, stackContext.OrigionRealm, "www.huawei.com", 4, CC_Request_Type.TERMINATION_REQUEST, 1, sessionId);

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Destination_Host, "cbp104"));
            ccrReq.avps.Add(new Avp(DiameterAvpCode.Service_Context_Id, "manager@huawei.com"));
            ccrReq.avps.Add(new Avp(DiameterAvpCode.User_Name, userName));

            long seconds = Convert.ToInt64((DateTime.UtcNow - new DateTime(1900, 1, 1, 0, 0, 0)).TotalSeconds);
            //ccrReq.avps.Add(new Avp(DiameterAvpCode.Event_Timestamp, 14236589681638796952));
            ccrReq.avps.Add(new Avp(DiameterAvpCode.Event_Timestamp, seconds));

            //Create Subscription-Id Grouped Avp
            Avp avpSubscriptionId = new Avp(DiameterAvpCode.Subscription_Id) { isGrouped = true };
            Avp avpSubscriptionIdType = new Avp(DiameterAvpCode.Subscription_Id_Type, Subscription_Id_Type.END_USER_E164);
            Avp avpSubscriptionIdData = new Avp(DiameterAvpCode.Subscription_Id_Data, MSIDN);
            avpSubscriptionId.groupedAvps.Add(avpSubscriptionIdType); //Add Avp to Grouped Avp
            avpSubscriptionId.groupedAvps.Add(avpSubscriptionIdData); //Add Avp to Grouped Avp
            ccrReq.avps.Add(avpSubscriptionId);
            //End Grouped Avp

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Service_Identifier, 1));

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Route_Record, "scp1"));

            // Requested Service Unit Avp 
            Avp UsedServiceUnits = new Avp(DiameterAvpCode.Used_Service_Unit) { isGrouped = true };
            Avp CCServiceSpecificUnits = new Avp(DiameterAvpCode.CC_Service_Specific_Units, requestedServiceUnits);
            UsedServiceUnits.groupedAvps.Add(CCServiceSpecificUnits);
            ccrReq.avps.Add(UsedServiceUnits);

            return ccrReq;
        }

        /// </summary>
        /// <param name="userName"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="callingPartyAddress"></param>
        /// <param name="calledPartyAddress"></param>
        /// <param name="realCalledNumber"></param>
        /// <param name="CallingVlrNumber"></param>
        /// <param name="CallReference"></param>
        /// <param name="MSCAddress"></param>
        /// <returns></returns>

        private Message Recharge_CCR_Initial(StackContext pStackContext, string pTopupSessionId, int pCardType, string pCardNum, string pCardBatch, string pSerialNum, string pSsptime, int pAccountType, int pAccountQueryMethod, string pMSISDN, int pChargeAmount, int pActiveDays, string pSessionId)
        {
            Request req = new Request();

            Message ccrReq = req.CreditControlRequest(pStackContext, pStackContext.OrigionHost, pStackContext.OrigionRealm, "www.huawei.com", 4, CC_Request_Type.INITIAL_REQUEST, 0, pSessionId);

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Destination_Host, "cbp104"));

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Service_Context_Id, "manager@huawei.com"));

            ulong currentTime = Utility.GetCurrentNTPTime();

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Event_Timestamp, currentTime));

            //Create Subscription-Id Grouped Avp
            Avp groupedAvpSubscriptionId = new Avp(DiameterAvpCode.Subscription_Id) { isGrouped = true };

            Avp avpSubscriptionIdType = new Avp(DiameterAvpCode.Subscription_Id_Type, Subscription_Id_Type.END_USER_E164);

            Avp avpSubscriptionIdData = new Avp(DiameterAvpCode.Subscription_Id_Data, pMSISDN);

            groupedAvpSubscriptionId.groupedAvps.Add(avpSubscriptionIdType); //Add Avp to Grouped Avp

            groupedAvpSubscriptionId.groupedAvps.Add(avpSubscriptionIdData); //Add Avp to Grouped Avp

            ccrReq.avps.Add(groupedAvpSubscriptionId);
            //End Grouped Avp

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Service_Identifier, 3));

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Route_Record, "scp1"));

            //Create Service-Information Grouped Avp
            Avp ServiceInformation = new Avp(DiameterAvpCode.Service_Information) { isGrouped = true, isVendorSpecific = true, VendorId = DiameterVendors._3GPP };

            //Add Grouped Avp In-Information 
            Avp InInformation = new Avp(DiameterAvpCode.In_Information) { isGrouped = true, isVendorSpecific = true, VendorId = DiameterVendors.Huawei };

            Avp AccountType = new Avp(DiameterAvpCode.Account_Type, pAccountType) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp MscAddr = new Avp(DiameterAvpCode.MSC_Address, "") { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp AccountQueryMethod = new Avp(DiameterAvpCode.Account_Query_Method, pAccountQueryMethod) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp TotalCostFlag = new Avp(DiameterAvpCode.Total_Cost_Flag, 1) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp SSPTime = new Avp(DiameterAvpCode.SSP_Time, pSsptime) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp CallingCellIDOrSAI = new Avp(DiameterAvpCode.Calling_CellID_Or_SAI, "") { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };

            InInformation.groupedAvps.Add(MscAddr);
            InInformation.groupedAvps.Add(AccountQueryMethod);
            InInformation.groupedAvps.Add(AccountType);
            InInformation.groupedAvps.Add(TotalCostFlag);
            InInformation.groupedAvps.Add(SSPTime);
            InInformation.groupedAvps.Add(CallingCellIDOrSAI);

            //Grouped Avp Recharge Infomration
            Avp RechargeInformation = new Avp(DiameterAvpCode.Recharge_Information) { isGrouped = true, isVendorSpecific = true, VendorId = DiameterVendors.Huawei };

            Avp EtopUpSessionId = new Avp(DiameterAvpCode.ETopUpSessionId, pTopupSessionId) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp CardNumber = new Avp(DiameterAvpCode.Card_Number, pCardNum) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp Card_Batch = new Avp(DiameterAvpCode.Card_Batch, pCardBatch) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp SerialNo = new Avp(DiameterAvpCode.SerialNo, pSerialNum) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            //Grouped Avp Charge-Money
            Avp ChargeMoney = new Avp(DiameterAvpCode.Charge_Money) { isGrouped = true, isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp MoneyValue = new Avp(DiameterAvpCode.Money_Value, pChargeAmount) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp ActiveDay = new Avp(DiameterAvpCode.Active_Day, pActiveDays) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };

            ChargeMoney.groupedAvps.Add(ActiveDay);
            ChargeMoney.groupedAvps.Add(MoneyValue);


            if (!pCardType.Equals("0"))
            {
                Avp CardType = new Avp(DiameterAvpCode.Card_Type, pCardType) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
                RechargeInformation.groupedAvps.Add(CardType);
            }
            RechargeInformation.groupedAvps.Add(CardNumber);
            RechargeInformation.groupedAvps.Add(Card_Batch);
            RechargeInformation.groupedAvps.Add(SerialNo);
            RechargeInformation.groupedAvps.Add(EtopUpSessionId);

            RechargeInformation.groupedAvps.Add(ChargeMoney);

            InInformation.groupedAvps.Add(RechargeInformation);
            //

            //Add In-Information to Service Infomration
            ServiceInformation.groupedAvps.Add(InInformation);

            ccrReq.avps.Add(ServiceInformation);

            return ccrReq;

        }
        private Message Recharge_CCR_Event(StackContext pStackContext, string pTopupSessionId, int pCardType, string pCardNum, string pCardBatch, string pSerialNum, string pSsptime, int pAccountType, int pAccountQueryMethod, string pMSISDN, int pChargeAmount, int pActiveDays, string pSessionId)
        {
            Request req = new Request();

            Message ccrReq = req.CreditControlRequest(pStackContext, pStackContext.OrigionHost, pStackContext.OrigionRealm, "www.huawei.com", 4, CC_Request_Type.EVENT_REQUEST, 0, pSessionId);

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Destination_Host, "cbp104"));

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Service_Context_Id, "Adjustment@huawei.com"));
            ccrReq.avps.Add(new Avp(DiameterAvpCode.Requested_Action, 0));


            ulong currentTime = Utility.GetCurrentNTPTime();

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Event_Timestamp, currentTime));

            //Create Subscription-Id Grouped Avp
            Avp groupedAvpSubscriptionId = new Avp(DiameterAvpCode.Subscription_Id) { isGrouped = true };

            Avp avpSubscriptionIdType = new Avp(DiameterAvpCode.Subscription_Id_Type, Subscription_Id_Type.END_USER_E164);

            Avp avpSubscriptionIdData = new Avp(DiameterAvpCode.Subscription_Id_Data, pMSISDN);

            groupedAvpSubscriptionId.groupedAvps.Add(avpSubscriptionIdType); //Add Avp to Grouped Avp

            groupedAvpSubscriptionId.groupedAvps.Add(avpSubscriptionIdData); //Add Avp to Grouped Avp

            ccrReq.avps.Add(groupedAvpSubscriptionId);
            //End Grouped Avp

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Service_Identifier, 3));

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Route_Record, "www.c00105987.com"));

            //Create Service-Information Grouped Avp
            Avp ServiceInformation = new Avp(DiameterAvpCode.Service_Information) { isGrouped = true, isVendorSpecific = true, VendorId = DiameterVendors._3GPP };

            //Add Grouped Avp Adjustment-Information 
            Avp AdjInformation = new Avp(DiameterAvpCode.Adj_Information) { isGrouped = true, isVendorSpecific = true, VendorId = DiameterVendors.Huawei };

            Avp AvpCallingPartyAddress = new Avp(DiameterAvpCode.Calling_Party_Address, "252"+pMSISDN) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp AvpCalledPartyAddress = new Avp(DiameterAvpCode.Called_Party_Address, "252"+pMSISDN) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp AvpCallingCellIDOrSAI = new Avp(DiameterAvpCode.Calling_CellID_Or_SAI, "46000185301425") { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp AvpCallReferenceNumber = new Avp(DiameterAvpCode.Call_Reference_Number, pTopupSessionId) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp AvpTimeZone = new Avp(DiameterAvpCode.TimeZone, pAccountQueryMethod) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp AvpAccessMethod = new Avp(DiameterAvpCode.AccessMethod, 1) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp AvpEtopUpSessionId = new Avp(DiameterAvpCode.ETopUpSessionId, pTopupSessionId) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp AvpSSPTime = new Avp(DiameterAvpCode.SSP_Time, pSsptime) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp AvpAgentName = new Avp(DiameterAvpCode.Agent_Name, "SafariEVC") { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp AvpNotifyFlag = new Avp(DiameterAvpCode.Notify_Flag, 0) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };


            AdjInformation.groupedAvps.Add(AvpCallingPartyAddress);
            AdjInformation.groupedAvps.Add(AvpCalledPartyAddress);
            AdjInformation.groupedAvps.Add(AvpCallingCellIDOrSAI);
            AdjInformation.groupedAvps.Add(AvpCallReferenceNumber);
            AdjInformation.groupedAvps.Add(AvpTimeZone);
            AdjInformation.groupedAvps.Add(AvpAccessMethod);


            Avp FeeChangeInformation = new Avp(DiameterAvpCode.Fee_Change_Information) { isGrouped = true, isVendorSpecific = true, VendorId = DiameterVendors.Huawei };

            Avp AvpAccountType = new Avp(DiameterAvpCode.Account_Type, pAccountType) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp AvpAccountBalanceChange = new Avp(DiameterAvpCode.Account_Balance_Change, pChargeAmount) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp AvpAccountDateChange = new Avp(DiameterAvpCode.Account_Date_Change,pActiveDays) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            Avp AvpServiceReason = new Avp(DiameterAvpCode.Service_Reason, "Adjustment") { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            FeeChangeInformation.groupedAvps.Add(AvpAccountType);
            FeeChangeInformation.groupedAvps.Add(AvpAccountBalanceChange);
            FeeChangeInformation.groupedAvps.Add(AvpAccountDateChange);
            FeeChangeInformation.groupedAvps.Add(AvpServiceReason);

            AdjInformation.groupedAvps.Add(FeeChangeInformation);
            AdjInformation.groupedAvps.Add(AvpEtopUpSessionId);
            AdjInformation.groupedAvps.Add(AvpSSPTime);
            AdjInformation.groupedAvps.Add(AvpAgentName);
            AdjInformation.groupedAvps.Add(AvpNotifyFlag);

            //Add In-Information to Service Infomration
            ServiceInformation.groupedAvps.Add(AdjInformation);

            ccrReq.avps.Add(ServiceInformation);

            return ccrReq;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stackContext"></param>
        /// <param name="userName"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="MSCAddress"></param>
        /// <returns></returns>
        private Message Recharge_CCR_Terminate(StackContext stackContext, string userName, string subscriptionId, string pSessionId)
        {
            Message ccrReq = new Request().CreditControlRequest(stackContext, stackContext.OrigionHost, stackContext.OrigionRealm, "www.huawei.com", 4, CC_Request_Type.TERMINATION_REQUEST, 1, pSessionId);

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Destination_Host, "cbp104"));

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Service_Context_Id, "manager@huawei.com"));

            ulong seconds = Utility.GetCurrentNTPTime();
            ccrReq.avps.Add(new Avp(DiameterAvpCode.Event_Timestamp, seconds));

            //Create Subscription-Id Grouped Avp
            Avp groupedAvpSubscriptionId = new Avp(DiameterAvpCode.Subscription_Id) { isGrouped = true };

            Avp avpSubscriptionIdType = new Avp(DiameterAvpCode.Subscription_Id_Type, Subscription_Id_Type.END_USER_E164);

            Avp avpSubscriptionIdData = new Avp(DiameterAvpCode.Subscription_Id_Data, subscriptionId);

            groupedAvpSubscriptionId.groupedAvps.Add(avpSubscriptionIdType); //Add Avp to Grouped Avp

            groupedAvpSubscriptionId.groupedAvps.Add(avpSubscriptionIdData); //Add Avp to Grouped Avp

            ccrReq.avps.Add(groupedAvpSubscriptionId);
            //End Grouped Avp

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Service_Identifier, 3));


            //Create Service-Information Grouped Avp
            Avp ServiceInformation = new Avp(DiameterAvpCode.Service_Information) { isGrouped = true, isVendorSpecific = true, VendorId = DiameterVendors._3GPP };

            //Add Grouped Avp In-Information 
            Avp InInformation = new Avp(DiameterAvpCode.In_Information) { isGrouped = true, isVendorSpecific = true, VendorId = DiameterVendors.Huawei };

            Avp ChargeConfirmFlag = new Avp(DiameterAvpCode.Charge_ConfirmFlag, 0) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            InInformation.groupedAvps.Add(ChargeConfirmFlag);

            //Add In-Information to Service Infomration
            ServiceInformation.groupedAvps.Add(InInformation);


            ccrReq.avps.Add(ServiceInformation);


            return ccrReq;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stackContext"></param>
        /// <param name="userName"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="callingPartyAddress"></param>
        /// <param name="calledPartyAddress"></param>
        /// <param name="realCalledNumber"></param>
        /// <param name="CallingVlrNumber"></param>
        /// <param name="CallReference"></param>
        /// <param name="MSCAddress"></param>
        /// <returns></returns>
        /// 
        public Message Direct_Debit(StackContext stackContext, string userName, string subscriptionId, string callingPartyAddress, string calledPartyAddress, string realCalledNumber, string CallingVlrNumber, string CallReference, string MSCAddress)
        {
            //TODO: To be fully tested
            return null;
            /*
            Request req = new Request();

            string SessionId = req.GetSessionId();

            Message ccrReq = req.CreditControlRequest(stackContext,stackContext.OrigionHost, stackContext.OrigionRealm, "www.huawei.com", 4, CC_Request_Type.EVENT_REQUEST, 0,SessionId);


            ccrReq.avps.Add(new Avp(DiameterAvpCode.Destination_Host, "cbp104"));

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Service_Context_Id, "VAS@huawei.com"));

            long seconds = Convert.ToInt64((DateTime.UtcNow - new DateTime(1900, 1, 1, 0, 0, 0)).TotalSeconds);
            //ccrReq.avps.Add(new Avp(DiameterAvpCode.Event_Timestamp, 14236589681638796952));
            ccrReq.avps.Add(new Avp(DiameterAvpCode.Event_Timestamp, seconds));

            //Create Subscription-Id Grouped Avp
            Avp groupedAvpSubscriptionId = new Avp(DiameterAvpCode.Subscription_Id) { isGrouped = true };
            Avp avpSubscriptionIdType = new Avp(DiameterAvpCode.Subscription_Id_Type, Subscription_Id_Type.END_USER_E164);
            Avp avpSubscriptionIdData = new Avp(DiameterAvpCode.Subscription_Id_Data, subscriptionId);
            groupedAvpSubscriptionId.groupedAvps.Add(avpSubscriptionIdType); //Add Avp to Grouped Avp
            groupedAvpSubscriptionId.groupedAvps.Add(avpSubscriptionIdData); //Add Avp to Grouped Avp
            ccrReq.avps.Add(groupedAvpSubscriptionId);
            //End Grouped Avp

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Service_Identifier, 0));
            ccrReq.avps.Add(new Avp(DiameterAvpCode.Requested_Action, Requested_Action.DIRECT_DEBITING));
   

            // Requested Service Unit Avp 
            Avp avpRequestedServiceUnits = new Avp(DiameterAvpCode.Request_Service_Unit) { isGrouped = true };
            Avp avpCCMoney = new Avp(DiameterAvpCode.CC_Money) { isGrouped = true, isVendorSpecific = true };
            Avp avpUnitValue = new Avp(DiameterAvpCode.Unit_Value) { isGrouped = true };
            avpUnitValue.groupedAvps.Add(new Avp(DiameterAvpCode.Value_Digits, 1000));
            avpUnitValue.groupedAvps.Add(new Avp(DiameterAvpCode.Exponent, 0));
            avpCCMoney.groupedAvps.Add(avpUnitValue);
            avpCCMoney.groupedAvps.Add(new Avp(DiameterAvpCode.Currency_Code, 109));
            avpRequestedServiceUnits.groupedAvps.Add(avpCCMoney);
            Avp avpCCServiceSpecificUnits = new Avp(DiameterAvpCode.CC_Service_Specific_Units, 10000);
            avpRequestedServiceUnits.groupedAvps.Add(avpCCServiceSpecificUnits);
            ccrReq.avps.Add(avpRequestedServiceUnits);

           
            //Add RTBP

            //Create Service-Information Grouped Avp
            Avp groupedAvpServiceInformation = new Avp(DiameterAvpCode.Service_Information) { isGrouped = true, isVendorSpecific = true, VendorId = DiameterVendors._3GPP };
            //Add RTBP-Information Avp
            Avp avpRTBPInformation = new Avp(DiameterAvpCode.RTBP_Information) { isGrouped = true, isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            avpRTBPInformation.groupedAvps.Add(new Avp(DiameterAvpCode.Service_Id, 1) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei });
            avpRTBPInformation.groupedAvps.Add(new Avp(DiameterAvpCode.CategoryID, 101) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei });
            avpRTBPInformation.groupedAvps.Add(new Avp(DiameterAvpCode.ContentID, 64023) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei });
            avpRTBPInformation.groupedAvps.Add(new Avp(DiameterAvpCode.TransctionID, 1) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei });
            avpRTBPInformation.groupedAvps.Add(new Avp(DiameterAvpCode.Charge_ConfirmFlag, 0) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei });
            avpRTBPInformation.groupedAvps.Add(new Avp(DiameterAvpCode.need_cnfm, 1) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei });
            //Add RTBP to Service Information 
            groupedAvpServiceInformation.groupedAvps.Add(avpRTBPInformation);

            ccrReq.avps.Add(groupedAvpServiceInformation);

            return ccrReq;
            */
        }

        public Message Refund_Account(StackContext stackContext, string userName, string subscriberId, string callingPartyAddress, string calledPartyAddress, string realCalledNumber, string CallingVlrNumber, string CallReference, string MSCAddress)
        {
            //TODO: To be fully tested
            return null;
            /*
            Request req = new Request();

            string SessionId = req.GetSessionId();

            Message ccrReq = req.CreditControlRequest(stackContext, stackContext.OrigionHost, stackContext.OrigionRealm, "www.huawei.com", 4, CC_Request_Type.EVENT_REQUEST, 0, SessionId);

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Destination_Host, "cbp104"));

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Service_Context_Id, "VAS@huawei.com"));

            long seconds = Convert.ToInt64((DateTime.UtcNow - new DateTime(1900, 1, 1, 0, 0, 0)).TotalSeconds);
            ccrReq.avps.Add(new Avp(DiameterAvpCode.Event_Timestamp, seconds));

            //Create Subscription-Id Grouped Avp
            Avp SubscriptionId = new Avp(DiameterAvpCode.Subscription_Id) { isGrouped = true };

            Avp SubscriptionIdType = new Avp(DiameterAvpCode.Subscription_Id_Type, Subscription_Id_Type.END_USER_E164);

            Avp SubscriptionIdData = new Avp(DiameterAvpCode.Subscription_Id_Data, subscriberId);

            SubscriptionId.groupedAvps.Add(SubscriptionIdType); //Add Avp to Grouped Avp

            SubscriptionId.groupedAvps.Add(SubscriptionIdData); //Add Avp to Grouped Avp

            ccrReq.avps.Add(SubscriptionId);
            //End Grouped Avp

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Service_Identifier, 0));

            ccrReq.avps.Add(new Avp(DiameterAvpCode.Requested_Action, Requested_Action.REFUND_ACCOUNT));

            // Requested Service Unit Grouped Avp 
            Avp RequestedServiceUnits = new Avp(DiameterAvpCode.Request_Service_Unit) { isGrouped = true };
            
            Avp UnitValue = new Avp(DiameterAvpCode.Unit_Value) { isGrouped = true };
            UnitValue.groupedAvps.Add(new Avp(DiameterAvpCode.Value_Digits, 10000));
            UnitValue.groupedAvps.Add(new Avp(DiameterAvpCode.Exponent, 0));
            
            Avp CCMoney = new Avp(DiameterAvpCode.CC_Money) { isGrouped = true, isVendorSpecific = true };
            CCMoney.groupedAvps.Add(UnitValue);
            CCMoney.groupedAvps.Add(new Avp(DiameterAvpCode.Currency_Code, 109));
            
            RequestedServiceUnits.groupedAvps.Add(CCMoney);
            
            Avp CCServiceSpecificUnits = new Avp(DiameterAvpCode.CC_Service_Specific_Units, 10000);
            RequestedServiceUnits.groupedAvps.Add(CCServiceSpecificUnits);
            
            ccrReq.avps.Add(RequestedServiceUnits);

            //Create Service-Information Grouped Avp
            Avp ServiceInformation = new Avp(DiameterAvpCode.Service_Information) { isGrouped = true, isVendorSpecific = true, VendorId = DiameterVendors._3GPP };
            //Add RTBP-Information Avp
            Avp RTBPInformation = new Avp(DiameterAvpCode.RTBP_Information) { isGrouped = true, isVendorSpecific = true, VendorId = DiameterVendors.Huawei };
            RTBPInformation.groupedAvps.Add(new Avp(DiameterAvpCode.Service_Id, 1) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei });
            RTBPInformation.groupedAvps.Add(new Avp(DiameterAvpCode.CategoryID, 101) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei });
            RTBPInformation.groupedAvps.Add(new Avp(DiameterAvpCode.ContentID, 1) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei });
            RTBPInformation.groupedAvps.Add(new Avp(DiameterAvpCode.TransctionID, "1") { isVendorSpecific = true, VendorId = DiameterVendors.Huawei });
            RTBPInformation.groupedAvps.Add(new Avp(DiameterAvpCode.Charge_ConfirmFlag, 0) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei });
            RTBPInformation.groupedAvps.Add(new Avp(DiameterAvpCode.need_cnfm, 1) { isVendorSpecific = true, VendorId = DiameterVendors.Huawei });
            
            //Add RTBP to Service Information 
            ServiceInformation.groupedAvps.Add(RTBPInformation);

            ccrReq.avps.Add(ServiceInformation);
            
            return ccrReq;
            */
        }
    }

    public class ServiceParam
    {
        public string pName;
        public string pValue;

        public ServiceParam()
        {
            pName = "";
            pValue = "";
        }
        public ServiceParam(string pParamName, string pParamValue)
        {
            pName = pParamName;
            pValue = pParamValue;
        }
     }
    public class ServiceResult
    {
        public List<ServiceParam> mData;
        public int mRespCode;
        public string mRespMsg;

        public ServiceResult()
        {
            this.mRespCode = 0;
            this.mRespMsg = "";
            this.mData = new List<ServiceParam>();
        }

        public ServiceResult(int pResCode, string pResDesc, List<ServiceParam> pData)
        {
            this.mRespCode = pResCode;
            this.mRespMsg = pResDesc;
            this.mData = pData;
        }

        public void AddParam(ServiceParam pObj)
        {
            mData.Add(pObj);
        }
        public void AddParam(string pParamName, string pParamValue)
        {
            mData.Add(new ServiceParam(pParamName, pParamValue));
        }
        public bool ContainsParam(string pParamName)
        {
            ServiceParam p = mData.Find(n => n.pName.Equals(pParamName));
            return p != null;
        }
        public string GetParamValue(string pParamName)
        {
            string res = "";
            pParamName = pParamName.Trim().ToUpper();
            ServiceParam p = mData.Find(n => n.pName.Equals(pParamName));

            res = (p == null) ? "" : p.pValue;

            return res;

            /*foreach (clsServiceParam p in mData )
            {
                if (p.pName.Equals(pParamName))
                {
                    res = p.pValue;
                    break;
                }
            }
            return res;*/
        }

    }
    }
