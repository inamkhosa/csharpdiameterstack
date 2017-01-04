using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Xml;
using System.Configuration;
using System.Threading;
using System.Net.Sockets;
using System.Net;
namespace DiameterStack
{

    /// <summary>
    /// @Author Inam Khosa 
    /// @Date: October, 2013
    /// </summary>

    public class StackContext : EventArgs
    {
        public List<Peer> peers;

        public Dictionary<int, AttributeInfo> dictionary;

        public static Dictionary<int, AttributeInfo> GlobalAvpDictionary; 

        public string OrigionHost;

        public int CCATimeout=30; // seconds

        public string OrigionRealm;

        public bool EnableDebug;
        public bool EnableMessageLogging;

        public string ListenerIP;

        public int ListenerPort;

    }
    /// <summary>
    /// 
    /// </summary>
    public class DiameterAAAStack
    {
        public static StackContext stackContext = new StackContext();

        //private delegate void PeerStateEventHandler(object sender, EventArgs e);

        private delegate void PeerStateEventHandler(object sender, object e);

        private static Dictionary<PEER_STATE_EVENT, PeerStateEventHandler> EventHandlersTable;

        private static TcpListener TransportListner;

        private static Transport transport;
        public static bool isStopping = false;
        private static bool isReconnecting = false;
        //For Connection State monitoring
        private static List<int> portInUse = new List<int>();

        private static TcpListener connectionStateListener;

        private static Socket connectionStateSocket;
      
        public DiameterAAAStack()
        {
          
        }
        /// <summary>
        ///
        /// </summary>
        public static StackContext Initialize()
        {
            EventHandlersTable = new Dictionary<PEER_STATE_EVENT, PeerStateEventHandler>();

            //Load Dictionary
            AvpDictionary objDictionary = new AvpDictionary();

            Dictionary<int, AttributeInfo> dict = objDictionary.LoadDictionary();

            stackContext.dictionary = dict;

            StackContext.GlobalAvpDictionary = dict;
            //Set Transport Object 

            if(ConfigurationManager.AppSettings["CCATimeout"] == null)
                stackContext.CCATimeout = 30;
            else
            {
                if (!Int32.TryParse(ConfigurationManager.AppSettings["CCATimeout"].ToString(), out stackContext.CCATimeout))
                    stackContext.CCATimeout = 30;
            }

            transport = new Transport(stackContext);
            //Load Peers
            PeerTable pTable = new PeerTable();

            stackContext.peers = pTable.LoadPeers();

            if (ConfigurationManager.AppSettings["OrigionHost"] != null && ConfigurationManager.AppSettings["OrigionHost"] != "")

                stackContext.OrigionHost = ConfigurationManager.AppSettings["OrigionHost"];
            else
                stackContext.OrigionHost = "127.0.0.1";

            if (ConfigurationManager.AppSettings["OrigionRealm"] != null && ConfigurationManager.AppSettings["OrigionRealm"] != "")
                stackContext.OrigionRealm = ConfigurationManager.AppSettings["OrigionRealm"];
            else
                stackContext.OrigionRealm = "www.localhost.com";

            if (ConfigurationManager.AppSettings["ListenerIP"] != null && ConfigurationManager.AppSettings["ListenerIP"] != "")
                stackContext.ListenerIP = ConfigurationManager.AppSettings["ListenerIP"];
            else
                stackContext.ListenerIP = "127.0.0.1";

            if (ConfigurationManager.AppSettings["ListenerPort"] != null && ConfigurationManager.AppSettings["ListenerPort"] != "")
                stackContext.ListenerPort = Convert.ToInt32(ConfigurationManager.AppSettings["ListenerPort"]);
            else
                stackContext.ListenerPort = 3868;

            stackContext.EnableDebug = Convert.ToBoolean(ConfigurationManager.AppSettings["EnableDebug"]);
            stackContext.EnableMessageLogging = Convert.ToBoolean(ConfigurationManager.AppSettings["EnableMessageLogging"]);
            //Init Events Table
            InitializeEventsTable();

            //Start Listeners
            StartListenerThread(stackContext);

            //Start Peer State Machine
            StartPSM(stackContext);


            return stackContext;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool Shutdown()
        {
            try
            {
                if (stackContext != null)
                {
                    isStopping = true;

                    EventHandlersTable.Clear();

                    stackContext.dictionary.Clear();

                    stackContext.peers.Clear();

                    if (TransportListner != null)
                    {
                        TransportListner.Stop();
                    }

                    stackContext = null;
                    
                    return true;
                }
                else
                {
                    return false;
                }

            }
            catch (Exception exp)
            {
                Common.StackLog.Write2ErrorLog("Shutdown", "Error:"+exp.Message+" Stack:"+exp.StackTrace);
                return false;
            }

        }


        /// <summary>
        /// 
        /// </summary>
        private static void InitializeEventsTable()
        {

            Common.StackLog.Write2TraceLog("DiameterStack::InitializeEventsTable", " Adding Events and Handlers to the Event Table");

            EventHandlersTable.Add(PEER_STATE_EVENT.Start, Snd_CER);

            EventHandlersTable.Add(PEER_STATE_EVENT.Rcv_CEA, Process_CEA);

            EventHandlersTable.Add(PEER_STATE_EVENT.Rcv_DWR, Process_DWR);

            EventHandlersTable.Add(PEER_STATE_EVENT.Rcv_Message, Process_Message);

            EventHandlersTable.Add(PEER_STATE_EVENT.Peer_Disc, Reconnect_Peer);

        }
        /// <summary>
        /// Send CER Event Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Snd_CER(object sender, object e)
        {
            Common.StackLog.Write2TraceLog("DiameterAAAStack::Snd_CER", "Entering Snd_CER ...");
            Message CER = new Request().CapabilityExchangeRequest(stackContext);

            Common.StackLog.Write2TraceLog("\tDiameterAAAStack::Snd_CER", "Calling SendMessage from Snd_CER ...");
            Message CEA = SendMessage(CER, new URI(stackContext.peers[0].Hostidentity));
            Common.StackLog.Write2TraceLog("DiameterAAAStack::Snd_CER", "SendMessage Returned to Snd_CER ...");
            
            if(CEA !=null)
            RaisePeerStateChangeEvent(PEER_STATE_EVENT.Rcv_CEA, CEA);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stackContext"></param>
        private static void Process_CEA(object sender, object e)
        {
            Common.StackLog.Write2TraceLog("Process_CEA", "Entering Process_CEA");
            Message cea = (e as Message);

            Avp ResultCode = cea.avps.Find(a => a.AvpCode == 268);

            if ((int)ResultCode.AvpValue == 2001)
            {

                Common.StackLog.Write2TraceLog("Process_CEA", "Recieved CEA with Succcess Code");
                //Update Peer Entry in PeerTable
                UpdateConnectionState(stackContext.peers[0], PeerState.OPEN);
               
            }
            else
            {
                Common.StackLog.Write2TraceLog("Process_CEA", "Error in Connecting to the Remote Peer");
                cea.PrintMessage();
            }
            Common.StackLog.Write2TraceLog("Process_CEA", "Exiting Process_CEA");
        }

        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stackContext"></param>
        private static void Process_DWR(object sender, object e)
        {
            RecievedMessageInfo info = e as RecievedMessageInfo;
            
            Message DWR= info.data as Message;

            Message DWA = new Answer(stackContext).CreateDWA(DWR);

            SendMessage(DWA, info.PeerIdentity);

        }
        /// <summary>
        /// This method Process Incoming Messages
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Process_Message(object sender, object e)
        {
            try
            {
                RecievedMessageInfo info = e as RecievedMessageInfo;

                byte[] recievedBuffer = (info.data as byte[]);

                Message response = new Message(stackContext, recievedBuffer);

                if (response.CommandCode == 280)
                {
                    info.data = response;
                    RaisePeerStateChangeEvent(PEER_STATE_EVENT.Rcv_DWR, info);
                }

                response.PrintMessage();
            }
            catch (Exception exp)
            {
                Common.StackLog.Write2ErrorLog("DiameterAAAStack::Process_Message", "Error:" + exp.Message + " Stack:" + exp.StackTrace);
            }
        }
        /// <summary>
        /// This method Process Incoming Messages
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Reconnect_Peer(object sender, object e)
        {
            try
            {
                if (isReconnecting)
                    return;
                Common.StackLog.Write2TraceLog("DiameterAAAStack::Reconnect", "Entering ...");
                isReconnecting = true;
                //Update Peer Entry in PeerTable
                Common.StackLog.Write2TraceLog("\tDiameterAAAStack::Reconnect", "Shuttingdown Client ...");
                stackContext.peers[0].PeerConnection.Client.Shutdown(SocketShutdown.Both);
                UpdateConnectionState(stackContext.peers[0], PeerState.CLOSED);
                Common.StackLog.Write2TraceLog("\tDiameterAAAStack::Reconnect", "Connection closed ...");

                while (!isStopping && stackContext.peers[0].PeerState != PeerState.OPEN)
                {
                    Common.StackLog.Write2TraceLog("\tDiameterAAAStack::Reconnect", "Calling StartPSM ...");
                    StartPSM(stackContext);
                    Common.StackLog.Write2TraceLog("\tDiameterAAAStack::Reconnect", "StartPSM Ended...");
                    //Thread.Sleep(5000);
                }
                isReconnecting = false;
                Common.StackLog.Write2TraceLog("DiameterAAAStack::Reconnect", "Exiting ...");
            }
            catch (Exception exp)
            {
                Common.StackLog.Write2ErrorLog("DiameterAAAStack::Process_Message", "Error:" + exp.Message + " Stack:" + exp.StackTrace);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stackContext"></param>
        /// <param name="message"></param>
        /// <param name="PeerIdentity"></param>
        /// <returns></returns>
        public static Message SendMessage(Message request, URI PeerIdentity)
        {

            Peer destPeer = stackContext.peers.Find(p => p.PeerIPAddress == PeerIdentity.FQDN);

            if (destPeer.PeerState != PeerState.OPEN && request.CommandCode != DiameterMessageCode.CAPABILITY_EXCHANGE)
            {
                Common.StackLog.Write2TraceLog("DiameterAAAStack::SendMessage", "Diameter Peer is not in Connected State");
                return null;
            }

            Message response = transport.SendMessage(request, destPeer);

            return response;
        }

        public static void Reconnect(StackContext pStackContext)
        {
            try
            {
                if (isReconnecting)
                    return;
                Common.StackLog.Write2TraceLog("DiameterAAAStack::Reconnect", "Entering ...");
                isReconnecting = true;
                //Update Peer Entry in PeerTable
                Common.StackLog.Write2TraceLog("\tDiameterAAAStack::Reconnect", "Shuttingdown Client ...");

                //if(pStackContext.peers[0].PeerConnection.Client.
                try
                {
                    pStackContext.peers[0].PeerConnection.Client.Shutdown(SocketShutdown.Both);
                }
                catch (Exception ex)
                {
                    Common.StackLog.Write2ErrorLog("DiameterAAAStack::Reconnect", "Socket was already shutdown, error: "+ex.Message);
                }
                UpdateConnectionState(pStackContext.peers[0], PeerState.CLOSED);
                Common.StackLog.Write2TraceLog("\tDiameterAAAStack::Reconnect", "Connection closed ...");

                while (!isStopping && pStackContext.peers[0].PeerState != PeerState.OPEN)
                {
                    Common.StackLog.Write2TraceLog("\tDiameterAAAStack::Reconnect", "Calling StartPSM ...");
                    StartPSM(pStackContext);
                    Common.StackLog.Write2TraceLog("\tDiameterAAAStack::Reconnect", "StartPSM Ended...");
                    //Thread.Sleep(5000);
                }
                isReconnecting = false;
                Common.StackLog.Write2TraceLog("DiameterAAAStack::Reconnect", "Exiting ...");
            }
            catch (Exception ex)
            {
                Common.StackLog.Write2ErrorLog("DiameterAAAStack::Reconnect", "Error:" + ex.Message + " Stack:" + ex.StackTrace);
            }
        }

        //Starts a Listner Thread
        private static void StartListenerThread(StackContext stackContext)
        {
            try
            {
                Thread listenThread = new Thread(StartListener);

                listenThread.Start(stackContext);
            }
            catch (Exception exp)
            {
                Common.StackLog.Write2ErrorLog("DiameterAAAStack::StartListenerThread", "Error:" + exp.Message + " Stack:" + exp.StackTrace);
            }
        }

        /// <summary>
        /// Start Listner
        /// </summary>
        /// 
        private static void StartListener(object data)
        {
            try
            {
                StackContext stackContext = data as StackContext;

                List<ListenAddress> listenAddress = new List<ListenAddress>();

                listenAddress.Add(new ListenAddress() { IPAddress = stackContext.ListenerIP, Port = stackContext.ListenerPort });

                transport.StartListners(listenAddress, ref TransportListner);
            }
            catch (Exception exp)
            {
                Common.StackLog.Write2ErrorLog("DiameterAAAStack::StartListener", "Error:" + exp.Message + " Stack:" + exp.StackTrace);
            }
        }

        /// <summary>
        /// Initializes the Peer State Machine 
        /// </summary>
        /// <param name="stackContext"></param>
        /// <returns></returns>
        private static bool StartPSM(StackContext stackContext)
        {
            Common.StackLog.Write2TraceLog("DiameterAAAStack::StartPSM", "Entering StartPSM ...");
            try
            {
                RaisePeerStateChangeEvent(PEER_STATE_EVENT.Start, stackContext);
                Common.StackLog.Write2TraceLog("DiameterAAAStack::StartPSM", "Exiting StartPSM ...");
                return true;
            }
            catch (Exception exp)
            {
                Common.StackLog.Write2ErrorLog("DiameterAAAStack::StartPSM", "Error:" + exp.Message + " Stack:" + exp.StackTrace);
                return false;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public static void RaisePeerStateChangeEvent(PEER_STATE_EVENT EVENT, object Data)
        {
            PeerStateEventHandler EventCallBack = EventHandlersTable[EVENT];
            object sender = new DiameterAAAStack();
            object e = Data;
            //Fire Event
            EventCallBack(sender, e);

        }
        /// <summary>
        /// Updates Peer Connnection
        /// </summary>
        /// <param name="PeerIP"></param>
        /// <param name="tcpConnection"></param>
        public static void UpdateConnectionState(Peer peer, PeerState peerState)
        {
            try
            {
                stackContext.peers.Find(p => p.PeerIPAddress == peer.PeerIPAddress).PeerConnection = peer.PeerConnection;

                stackContext.peers.Find(p => p.PeerIPAddress == peer.PeerIPAddress).PeerState = peerState;
            }
            catch (Exception exp)
            {
                Common.StackLog.Write2ErrorLog("DiameterAAAStack::UpdateConnectionState", "Error:" + exp.Message + " Stack:" + exp.StackTrace);
            }

        }

    }
}
