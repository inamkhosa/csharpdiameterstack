using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using DiameterStack.Common;
namespace DiameterStack
{

    /// <summary>
    /// @Author Inam Khosa 
    /// @Date: December, 2013
    /// </summary>

    public static class ResponseWaiter
    {

        private static ConcurrentDictionary<string,Thread> vRequestSessions = new ConcurrentDictionary<string,Thread>();
        private static ConcurrentDictionary<string, Message> vResponseSessions = new ConcurrentDictionary<string, Message>();
        public static bool AddRequest(string pReqKey, Thread pTransRef)
        {
            Common.StackLog.Write2TraceLog("ResponseWaiter.AddRequest() ", " Adding Session (ID: " + pReqKey + ") Thread: " + pTransRef.Name + " (" + pTransRef.ThreadState.ToString() + ") ..\r\n");
            return vRequestSessions.TryAdd(pReqKey, pTransRef);
        }
        public static bool AddResponse(Message pRespMsg)
        {
            try
            {
                Thread vReqThread = null;
                for(int t=0;t<=3;t++)
                {
                    if (vResponseSessions.TryAdd(pRespMsg.SessionID, pRespMsg))
                    {
                            //Message StoreMsg = (Message)vResponseSessions[pRespMsg.SessionID];
                        //StackLog.Write2ErrorLog("ResponseWaiter.AddReponse", StoreMsg.ToString());
                        //if (StoreMsg.avps.Count == 0)
                        //    StackLog.Write2ErrorLog("ResponseWaiter.AddResponse", "Response Msg has no AVP's for Session:" + StoreMsg.SessionID);
                        for (int tq = 0; tq <= 3; tq++)
                        {
                            if (vRequestSessions.TryRemove(pRespMsg.SessionID, out vReqThread))
                            {
                                //interrupting sessionid:
                                Common.StackLog.Write2TraceLog("ResponseWaiter.AddResponse() ", " Interrupting Session (ID: " + pRespMsg.SessionID + ") Thread: " + vReqThread.Name + " (" + vReqThread.ThreadState.ToString() + ")..\r\n");
                                vReqThread.Interrupt();
                                break;
                            }
                        }
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                StackLog.Write2ErrorLog("ResponseWaiter.AddResponse", "Exception :" + ex.Message);
                return false;
            }
        }
        public static bool ContainsRequest(string pReqKey)
        {
            return vRequestSessions.ContainsKey(pReqKey);
        }
        public static bool ContainsResponse(string pReqKey)
        {
            return vResponseSessions.ContainsKey(pReqKey);
        }
        public static Message GetResponse(string pReqKey)
        {
            try
            {
                Message vRespMsg = null;
                bool vRespRemoved = false;
                for (int t = 0; t <= 3; t++)
                {
                    if (vResponseSessions.ContainsKey(pReqKey))
                    {
                        //for (int t = 0; t <= 3; t++)
                        //{
                        if (vResponseSessions.TryRemove(pReqKey, out vRespMsg))
                        {
                            vRespRemoved = true;
                            break;
                        }
                        //}
                    }
                    else
                        StackLog.Write2ErrorLog("ResponseWaiter.GetResponse", "Response Key not found for SessionID:"+pReqKey);

                }
                if(!vRespRemoved)
                    StackLog.Write2ErrorLog("ResponseWaiter.GetResponse", "Unable to remove Response for SessionID:"+pReqKey);
                return vRespMsg;
            }
            catch (Exception ex)
            {
                StackLog.Write2ErrorLog("ResponseWaiter.GetResponse", "Exception :" + ex.Message);
                return null;
            }
        }
    }
}
