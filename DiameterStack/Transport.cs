using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using DiameterStack.Common;
using System.Runtime.Serialization.Formatters.Binary;

namespace DiameterStack
{
    /// <summary>
    /// @Author Inam Khosa 
    /// @Date: October, 2013
    /// </summary>
    public class Transport
    {
        const int MAX_BUFF_SIZE = 20000;
        const int MAX_TCP_BUFF_SIZE = 1048576;

        StackContext stackContext;

        private byte[] responseBuffer = new byte[MAX_BUFF_SIZE];
        private MemoryStream storage = new MemoryStream(MAX_BUFF_SIZE);

        private int mPos = 0;

        private int mLen = 0;

        private TcpClient tcpClient;

        MessageLogger msgLogger = MessageLogger.Instance;
        /// <summary>
        /// Default Constructor
        /// </summary>
        public Transport(StackContext stackContext)
        {
            this.stackContext = stackContext;
        }


        public bool Connect()
        {
            try
            {
                StateObject state = new StateObject();

                if (stackContext.peers[0].PeerConnection == null || !(stackContext.peers[0].PeerConnection.Connected))
                {
                    StackLog.Write2TraceLog("\r\n\nTransport.SendMessage()", "Establishing the Connection");

                    Common.StackLog.Write2TraceLog("Transport::SendMessage", "Connecting remote Peer[ " + stackContext.peers[0].PeerIPAddress + ":" + stackContext.peers[0].PeerPort + "]");

                    stackContext.peers[0].PeerConnection = new TcpClient(stackContext.peers[0].PeerIPAddress, stackContext.peers[0].PeerPort);

                    stackContext.peers[0].PeerConnection.ReceiveBufferSize = MAX_TCP_BUFF_SIZE;

                    Common.StackLog.Write2TraceLog("Transport::SendMessage() ", " Connection Established..");

                    //Update Peer Connection State
                    DiameterAAAStack.UpdateConnectionState(stackContext.peers[0], PeerState.OPEN);

                    Common.StackLog.Write2TraceLog("Transport::SendMessage() ", " Connection State Changed to OPEN..");

                    //Start Data Processing Thread
                    //pProcessDataThread = new Thread(processData);
                    //pProcessDataThread.Start();
                    //Start Receive CallBack

                    tcpClient = stackContext.peers[0].PeerConnection;

                    tcpClient.Client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                    state.workSocket = tcpClient.Client;
                   
                }
            }
            catch (Exception e)
            {
                Common.StackLog.Write2ErrorLog("Connect", "Error Connecting:" + e.Message + " Stack:" + e.StackTrace);
                return false;

            }
            return true;
        }
        
         /// <summary>
        /// Send Message to the Diameter Peer
        /// </summary>
        /// <param name="MessageBytes"></param>
        /// <param name="TransportType"></param>
        /// <param name="remotePeer"></param>
        public Message SendMessage(Message diameterRequest, Peer destPeer)
        {
            try
            {
                byte[] mgsBytesToSend = diameterRequest.GetBytes();

                //Connect if not connected
                if (stackContext.peers[0].PeerConnection == null || !stackContext.peers[0].PeerConnection.Connected)
                {
                    if (diameterRequest.CommandCode != DiameterMessageCode.CAPABILITY_EXCHANGE)
                        Common.StackLog.Write2ErrorLog("Transport::SendMessage", "Connection Broken, calling Connect()");

                    //Connect Diameter Socket
                    Connect();
                }

                
                Thread pWaiterThread;
        
                //Just Copy to localThreadWaiter Variable.
                pWaiterThread = Thread.CurrentThread;

                //CCRWaiter vRespWaiter = null;
                //if Message is a CCR message, we need to wait for a response, so add Session to CCRSession
                if (diameterRequest.CommandCode == DiameterMessageCode.CREDIT_CONTROL)
                    ResponseWaiter.AddRequest(diameterRequest.SessionID, pWaiterThread);
                //CER Never Contains a Session ID
                else if (diameterRequest.CommandCode == DiameterMessageCode.CAPABILITY_EXCHANGE)
                {
                    ResponseWaiter.AddRequest("CEX", pWaiterThread);
                    diameterRequest.SessionID = "CEX";
                }
                else
                {
                    StackLog.Write2TraceLog("Transport.SendMessage() ", "UnHandled Message Type:" + diameterRequest.CommandCode);
                    return null;
                }
                //Record Time
                TimeSpan STime = new TimeSpan(DateTime.Now.Ticks);
                ///////////////////////////////Send Message to the Remote Peer ////////////////////////////////

                //StackLog.Write2TraceLog("\r\nSendMessage", diameterRequest.ToString());
                msgLogger.Write2MessageLog(diameterRequest.ToString());

                Common.StackLog.Write2TraceLog("Transport::SendMessage() ", " Sending Message "+DiameterMessageCode.GetMessageName(diameterRequest.CommandCode, true) + " (" + diameterRequest.SessionID+") ..");

                destPeer.PeerConnection.GetStream().Write(mgsBytesToSend, 0, mgsBytesToSend.Length);

                //Wait for Response;
                Common.StackLog.Write2TraceLog("Transport::SendMessage() ", " Message Sent, Waiting For Diameter Message Response of Session: " + diameterRequest.SessionID);
                bool isInterrupted = false;
                try
                {
                    //Sleep for 3 Seconds Maximum until interrupted.
                    for (int i = 0; i <= stackContext.CCATimeout * 10; i++)
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (ThreadInterruptedException ex)
                {
                    isInterrupted = true;
                    //StackLog.Write2ErrorLog("SendMessage", "Thread Intrupted for for Response for the Session: " + diameterRequest.SessionID);
                }

                if(isInterrupted)
                    Common.StackLog.Write2TraceLog("Transport.SendMessage() ", " Waiter Thread Response Interrupted for Session: " + diameterRequest.SessionID);
                else
                    Common.StackLog.Write2TraceLog("Transport.SendMessage() ", " Waiter Thread Response Timedout for Session: " + diameterRequest.SessionID);
                
                //After Thread intrruption get the response here. 
                //CEX Dont have any session ID so validate here. 
                Message diameterResponse;
                if(ResponseWaiter.ContainsResponse(diameterRequest.SessionID))
                    diameterResponse = ResponseWaiter.GetResponse(diameterRequest.SessionID);
                else
                {
                    Common.StackLog.Write2ErrorLog("Transport::SendMessage", "Response message not available for " + DiameterMessageCode.GetMessageName(diameterRequest.CommandCode, true) + " " + diameterRequest.SessionID);
                    return null;
                }
                //Check the Recieved Response
                if (diameterResponse != null)
                {
                    Common.StackLog.Write2TraceLog("Transport.SendMessage() ", "Response Message " + DiameterMessageCode.GetMessageName(diameterResponse.CommandCode, false) + " of Request Session: " + diameterRequest.SessionID + " with Response of:" + diameterResponse.SessionID.ToString() + " Received..\r\n");
                    /*if (diameterResponse.avps.Count == 0)
                    {
                        StackLog.Write2ErrorLog("SendMessage", "Response Msg has no AVP's for Session:" + diameterResponse.SessionID);
                        StackLog.Write2ErrorLog("SendMessage", diameterResponse.ToString());
                    }*/
                    ////Log Recieved Message
                    //StackLog.Write2MessageLog(diameterResponse.ToString());
                    msgLogger.Write2MessageLog(diameterResponse.ToString());
                    TimeSpan ETime = new TimeSpan(DateTime.Now.Ticks);
                    diameterResponse.ExecTime = ETime.Subtract(STime).Milliseconds;
                    //diameterResponse.PrintMessage();
                }
                else
                {
                    Common.StackLog.Write2ErrorLog("Transport::SendMessage", "Unable to Receive Response message for " + DiameterMessageCode.GetMessageName(diameterRequest.CommandCode, true) + " " + diameterRequest.SessionID);
                    //Common.StackLog.Write2ErrorLog("Transport::SendMessage", diameterRequest.ToString());
                }

                return diameterResponse;
            }
            catch (Exception exp)
            {
                StackLog.Write2ErrorLog("DiameterTransport:SendMessage()", "Error:" + exp.Message + " Stack:" + exp.StackTrace.ToString());
                //throw exp;
                return null;
            }

        }

        private void ReceiveCallbackSeq(IAsyncResult result)
        {
            int bytesRead;

            StateObject state = (StateObject)result.AsyncState;

            Socket client = state.workSocket;

            //NetworkStream networkStream = tcpClient.GetStream();
            //responseBuffer = result.AsyncState as byte[];

            try
            {

                // Retrieve the state object and the client socket 

                try
                {
                    // read = networkStream.EndRead(result);
                    bytesRead = client.EndReceive(result);
                }
                catch
                {
                    //An error has occured when reading
                    return;
                }

                if (bytesRead == 0)
                {
                    //The connection has been closed.
                    return;
                }

                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallbackSeq), state);
                /// Begin processing Diameter messages
                /// 
                byte[] responselocBuffer = new byte[bytesRead];

                Array.Copy(state.buffer, 0, responselocBuffer, 0, bytesRead);

                Message msg = new Message(stackContext, responselocBuffer);

                //msg = msg.ProcessMessage(stackContext, responselocBuffer);

                //Perform Additional Messag Processing

                if (msg.CommandCode == DiameterMessageCode.DEVICE_WATCHDOG)
                {
                    Message DWA = new Answer(stackContext).CreateDWA(msg);
                    byte[] toSend = DWA.GetBytes();
                    //networkStream.Write(toSend, 0, toSend.Length);
                    client.Send(toSend);
                }
                else
                {
                    //Return message response
                    if (msg.CommandCode == DiameterMessageCode.CAPABILITY_EXCHANGE)
                    {
                        if (ResponseWaiter.ContainsRequest("CEX"))
                        {
                            msg.SessionID = "CEX";
                            if (!ResponseWaiter.AddResponse(msg))
                                Common.StackLog.Write2ErrorLog("ReceiveCallBack", "Unable to Add Response for CEA Session:" + msg.SessionID);
                        }
                        else
                        {
                            Common.StackLog.Write2ErrorLog("ReceiveCallBack", "Unexpected CEA Received \r\n" + msg.ToString());
                        }
                    }
                    else if (msg.CommandCode == DiameterMessageCode.CREDIT_CONTROL)
                    {
                        if (ResponseWaiter.ContainsRequest(msg.SessionID))
                        {
                            if (!ResponseWaiter.AddResponse(msg))
                                Common.StackLog.Write2ErrorLog("ReceiveCallBack", "Unable to Add Response for CCA Session:" + msg.SessionID);
                        }
                        else
                        {
                            Common.StackLog.Write2ErrorLog("ReceiveCallBack", "Unexpected CCA Received for Session " + msg.SessionID + "\r\n" + msg.ToString());
                        }
                    }
                    else
                    {
                        Common.StackLog.Write2ErrorLog("ReceiveCallBack", "Unexpected Messages Received:" + msg.ToString());
                    }
                }

                //Then start reading from the network again after Parsing recieved Message.
                //networkStream.BeginRead(responseBuffer, 0, responseBuffer.Length, ReceiveCallback, buffer);

            }
            catch (Exception e)
            {
                //Then start reading from the network again.
                //networkStream.BeginRead(buffer, 0, buffer.Length, ReceiveCallback, buffer);
                //state.workSocket.Connect(state.workSocket.RemoteEndPoint);
                Common.StackLog.Write2ErrorLog("ReceiveCallBack", "Error:" + e.Message + " Stack:" + e.StackTrace);

                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallbackSeq), state);
            }
        }
        private void Reconnect(Object obj)
        {
            StackContext stackContext = (StackContext)obj;
            DiameterAAAStack.Reconnect(stackContext);
        }

        private void ReceiveCallbackNew(IAsyncResult result)
        {
            int bytesRead;

            StateObject state = (StateObject)result.AsyncState;

            Socket client = state.workSocket;


            try
            {

                // Retrieve the state object and the client socket 

                try
                {
                    bytesRead = client.EndReceive(result);
                }
                catch (Exception e)
                {
                    //An error has occured when reading
                    Common.StackLog.Write2ErrorLog("ReceiveCallBack", "Error:" + e.Message + " Stack:" + e.StackTrace);
                    Thread t = new Thread(new ParameterizedThreadStart(Reconnect));
                    t.Start(stackContext);
                    return;
                }

                if (bytesRead == 0)
                {
                    //The connection has been closed.
                    //Reconnect
                    //DiameterAAAStack.Reconnect(stackContext);
                    Common.StackLog.Write2ErrorLog("ReceiveCallBack", "Zero Bytes Read");
                    
                    Thread t = new Thread(new ParameterizedThreadStart(Reconnect));
                    t.Start(stackContext);
                    return;
                }
                byte[] rcvMsg = new byte[bytesRead];
                Array.Copy(state.buffer, 0, rcvMsg, 0, bytesRead);
                appendData(rcvMsg);
                //Then start reading from the network again after Parsing recieved Message.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallbackNew), state);

            }
            catch (Exception e)
            {
                //Then start reading from the network again.
                //state.workSocket.Connect(state.workSocket.RemoteEndPoint);
                Common.StackLog.Write2ErrorLog("ReceiveCallBack", "Error:" + e.Message + " Stack:" + e.StackTrace);

                //Then start reading from the network again after Parsing recieved Message.
                if (client.Connected)
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallbackNew), state);
                else
                {
                    Thread t = new Thread(new ParameterizedThreadStart(Reconnect));
                    t.Start(stackContext);

                }
            }
        }
        private void appendData(byte[] data)
        {
            if (storage.Position + data.Length >= storage.Capacity)
            {
                int newcap = (int)storage.Length + data.Length * 2;
                MemoryStream tmp = new MemoryStream(newcap);
                byte[] tmpData = new byte[storage.Position];
                //ByteBuffer.flip equiv start
                storage.SetLength(storage.Position);
                storage.Seek(0, SeekOrigin.Begin);
                //ByteBuffer.flip equiv end
                tmpData = storage.GetBuffer();
                tmp.Write(tmpData, 0, tmpData.Length);
                storage = tmp;
                Common.StackLog.Write2ErrorLog("appendData", "Increase storage size. Current size is:"+storage.Length);
            }
            try
            {
                storage.Write(data, 0, data.Length);
            }
            catch (IOException e)
            {
                Common.StackLog.Write2ErrorLog("appendData", e.Message);
            }
        }
        private void processDataNew()
        {
            bool messageReceived;
            Common.StackLog.Write2TraceLog("Transport.processData","Starting processData Thread");

            while (tcpClient.Connected)
            {
                do
                {
                    messageReceived = seekMessage();
                } while (messageReceived);
            }
            Common.StackLog.Write2TraceLog("Transport.processData", "processData Thread Stopped");
        }
        private bool seekMessage()
        {
            //make sure there's actual data written on the buffer
            if (storage.Position == 0)
                return false;
            //ByteBuffer.flip equiv start
            //storage.SetLength(storage.Position);
            //storage.Seek(0, SeekOrigin.Begin);
            //ByteBuffer.flip equiv end 
            try
            {
                byte[] tmpbuf = new byte[3];
                int tmpVersion = storage.ReadByte();
                if (tmpVersion != 1)
                    return false;
                if (storage.Read(tmpbuf, 0, 3) != 3)
                    throw new IOException("not enough bytes to read");
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(tmpbuf);
                int tmpMsgLen = converToInt(tmpbuf);
                //verify if whole message is in the storage
                if (storage.Length < tmpMsgLen)
                {
                    //we don't have all data, lets fetch for more, restore buffer
                    storage.Seek(-4, SeekOrigin.Current);
                    return false;
                }
                storage.Seek(-4, SeekOrigin.Current);
                //read the complete message
                byte[] data = new byte[tmpMsgLen];
                storage.Read(data, 0, tmpMsgLen);
                //compact buffer
                byte[] tmpRemainder = new byte[storage.Length - storage.Position];
                storage.Read(tmpRemainder, 0, tmpRemainder.Length);
                storage.Write(tmpRemainder, 0, tmpRemainder.Length);
                storage.SetLength(tmpRemainder.Length);
                //end compact buffer code
                try
                {
                    //make a message out of data and process it
                    Message newMsg = new Message(stackContext, data);
                    ProcessreceivedMsg(newMsg);
                }
                catch (Exception e)
                {
                    Common.StackLog.Write2ErrorLog("seekMessage", "Error:" + e.Message + "Stack:" + e.StackTrace);

                    //garbage data was received, clear buffer
                    storage.Dispose();
                    storage = new MemoryStream(MAX_BUFF_SIZE);
                }

            }
            catch (Exception e)
            {
                Common.StackLog.Write2ErrorLog("seekMessage", "Error:" + e.Message + "Stack:" + e.StackTrace);
                return false;
            }

            return true;


        }
        private void ProcessreceivedMsg(Message msg)
        {
            if (msg.CommandCode == DiameterMessageCode.DEVICE_WATCHDOG)
            {
                Message DWA = new Answer(stackContext).CreateDWA(msg);
                byte[] toSend = DWA.GetBytes();
                //networkStream.Write(toSend, 0, toSend.Length);
                tcpClient.Client.Send(toSend);
            }
            else
            {
                //Return message response
                if (msg.CommandCode == DiameterMessageCode.CAPABILITY_EXCHANGE)
                {
                    if (ResponseWaiter.ContainsRequest("CEX"))
                    {
                        msg.SessionID = "CEX";
                        if (!ResponseWaiter.AddResponse(msg))
                            Common.StackLog.Write2ErrorLog("ProcessreceivedMsg", "Unable to Add Response for CEA Session:" + msg.SessionID);
                    }
                    else
                    {
                        Common.StackLog.Write2ErrorLog("ProcessreceivedMsg", "Unexpected CEA Received \r\n" + msg.ToString());
                    }
                }
                else if (msg.CommandCode == DiameterMessageCode.CREDIT_CONTROL)
                {
                    if (ResponseWaiter.ContainsRequest(msg.SessionID))
                    {
                        if (!ResponseWaiter.AddResponse(msg))
                            Common.StackLog.Write2ErrorLog("ProcessreceivedMsg", "Unable to Add Response for CCA Session:" + msg.SessionID);
                    }
                    else
                    {
                        Common.StackLog.Write2ErrorLog("ProcessreceivedMsg", "Unexpected CCA Received for Session " + msg.SessionID + "\r\n" + msg.ToString());
                    }
                }
                else
                {
                    Common.StackLog.Write2ErrorLog("ProcessreceivedMsg", "Unexpected Messages Received:" + msg.ToString() + msg.ToString());
                }
            }
 
        }
        private void processData()
        {
            //Common.StackLog.Write2TraceLog("Transport.processData", "Starting processData Thread");
            Message msg=null;

            //while (!DiameterAAAStack.isStopping)
            //{
                while (true)
                {
                    try
                    {
                        int msgBufLen = mLen - mPos;
                        if (msgBufLen <= 0)
                        {
                            //StackLog.Write2TraceLog("ReceiveCallBack", "buffer processed");
                            try
                            {
                                Thread.Sleep(10);
                            }
                            catch (ThreadInterruptedException)
                            {
                            }
                            break;
                        }

                        //StackLog.Write2TraceLog("ReceiveCallBack", "NEW:msgBufLen=" + msgBufLen);

                        byte[] msgBuffer = new byte[msgBufLen];

                        Array.Copy(responseBuffer, mPos, msgBuffer, 0, msgBufLen);

                        try
                        {
                            msg = new Message(stackContext, msgBuffer);
                        }
                        catch (Exception ex)
                        {
                            //Common.StackLog.Write2ErrorLog("Transport.processData", "Error: " + ex.Message + ", Stack: " + ex.StackTrace);
                            if (ex.Message.Equals("Invalid Protocol Version"))
                            {
                                StackLog.Write2ErrorLog("ReceiveCallBack", "Invalid Protocol Version mPos=" + mPos + ", mLen=" + mLen + ", mBufLen=" + msgBufLen);
                                mPos = mPos + 1;
                                continue;
                            }
                            if (ex.Message.Equals("Invalid Message Length"))
                                break;
                            else
                                throw ex;
                        }

                        //Set unprocessed message offset
                        mPos = mPos + msg.MessageLength;
                        //ProcessReceivedMsg
                        ProcessreceivedMsg(msg);
                    }
                    catch (Exception ex)
                    {
                        Common.StackLog.Write2ErrorLog("Transport.processData", "Error: " + ex.Message + ", Stack: " + ex.StackTrace);
                        break;
                    }
                }
            //}
            //Common.StackLog.Write2TraceLog("Transport.processData", "processData Thread Stopped");
        }
        private void ReceiveCallback(IAsyncResult result)
        {
            int bytesRead;
            StateObject state = (StateObject)result.AsyncState;
            Socket client = state.workSocket;
            try
            {
                // Retrieve the state object and the client socket 
                try
                {
                    bytesRead = client.EndReceive(result);
                }
                catch (Exception e)
                {
                    //An error has occured when reading
                    Common.StackLog.Write2ErrorLog("ReceiveCallBack", "Error:" + e.Message + " Stack:" + e.StackTrace);
                    Thread t = new Thread(new ParameterizedThreadStart(Reconnect));
                    t.Start(stackContext);
                    return;
                }
                if (bytesRead == 0)
                {
                    //The connection has been closed.
                    //Reconnect
                    Common.StackLog.Write2ErrorLog("ReceiveCallBack", "Zero Bytes Read");
                    Thread t = new Thread(new ParameterizedThreadStart(Reconnect));
                    t.Start(stackContext);
                    return;
                }

                /// Begin processing Diameter messages , If new data is too large for buffer, reset the buffer and store new data
                //StackLog.Write2TraceLog("ReceiveCallBack", "INIT:Bytesread=" + bytesRead + "\r\nINIT:mLen=" + mLen + "\r\nINIT:mPos=" + mPos);
                if (mLen + bytesRead > MAX_BUFF_SIZE)
                {

                    //StackLog.Write2ErrorLog("ReceiveCallBack", "RESET:mLen=" + mLen + "\r\nRESET:Bytesread=" + bytesRead);

                    if (mLen - mPos > 0)
                    {
                        byte[] tmpBuffer = new byte[mLen - mPos];
                        Array.Copy(responseBuffer, mPos, tmpBuffer, 0, mLen - mPos);
                        responseBuffer = new byte[MAX_BUFF_SIZE];
                        Array.Copy(tmpBuffer, 0, responseBuffer, 0, tmpBuffer.Length);
                        mLen = tmpBuffer.Length;
                        mPos = 0;
                    }
                    else
                    {
                        responseBuffer = new byte[MAX_BUFF_SIZE];
                        mPos = 0;
                        mLen = 0;
                    }
                }

                //Append new data into existing Message Buffer
                try
                {
                    //StackLog.Write2ErrorLog("ReceiveCallBack", "Copying Array: mLen: " + mLen + ", Bytesread: " + bytesRead + ", mPos: " + mPos + ", SrcArray len: " + state.buffer.Length + ", DstArray len: " + responseBuffer.Length);
                    Array.Copy(state.buffer, 0, responseBuffer, mLen, bytesRead);
                    mLen = mLen + bytesRead;
                    //StackLog.Write2ErrorLog("ReceiveCallBack", "Array Copied: mLen: " + mLen + ", Bytesread: " + bytesRead + ", mPos: " + mPos + ", SrcArray len: " + state.buffer.Length + ", DstArray len: " + responseBuffer.Length);
                }
                catch (Exception ex)
                {
                    StackLog.Write2ErrorLog("ReceiveCallBack", " Length Error: " + ex.Message + " \nmLen: " + mLen + ", Bytesread: " + bytesRead + ", mPos: " + mPos + ", SrcArray len: " + state.buffer.Length + ", DstArray len: " + responseBuffer.Length);
                    //return;
                    throw ex;
                }

                processData();

                if (client.Connected)
                {
                    //Then start reading from the network again after Parsing recieved Message.
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    StackLog.Write2ErrorLog("ReceiveCallBack", " Socket disconnected, Reconnect Again!");
                    Thread t = new Thread(new ParameterizedThreadStart(Reconnect));
                    t.Start(stackContext);
                }
            }
            catch (Exception e)
            {
                //Then start reading from the network again.
                //state.workSocket.Connect(state.workSocket.RemoteEndPoint);
                Common.StackLog.Write2ErrorLog("ReceiveCallBack", " ReceiveCallback Error:" + e.Message + " Stack:" + e.StackTrace);

                if (client.Connected)
                {
                    //Then start reading from the network again after Parsing recieved Message.
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    StackLog.Write2ErrorLog("ReceiveCallBack", " Exception Socket disconnected, Reconnect Again!");
                    Thread t = new Thread(new ParameterizedThreadStart(Reconnect));
                    t.Start(stackContext);
                }
            }
         }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="listenAddresses"></param>
        public void StartListners(List<ListenAddress> listenAddresses, ref TcpListener listner)
        {
            byte[] responseBuffer = new byte[4000];

            try
            {

                SocketAddress address = new SocketAddress(System.Net.Sockets.AddressFamily.InterNetwork);

                foreach (ListenAddress socketAddr in listenAddresses)
                {
                    Common.StackLog.Write2TraceLog("Transport::SendMessage", "Starting Listening on " + socketAddr.IPAddress.ToString() + " with Port: " + socketAddr.Port.ToString());

                    listner = new TcpListener(IPAddress.Parse(socketAddr.IPAddress), socketAddr.Port);

                    listner.Start();

                    Common.StackLog.Write2TraceLog("Transport::SendMessage", "Started Listening on " + listner.LocalEndpoint.ToString());

                    StackLog.Write2TraceLog("StartListners", "Started Listening on " + socketAddr.IPAddress + ":" + socketAddr.Port.ToString());
                }

                while (true)
                {
                    //try
                    //{
                        if (listner.Pending())
                        {
                            Common.StackLog.Write2TraceLog("Transport::StartListener", "Listening for Peer Connections ");

                            Socket socket = listner.AcceptSocket();

                            Common.StackLog.Write2TraceLog("Transport::StartListener", "Recieved Message From [" + socket + " ]");

                            //Get the Message Length
                            Array.Clear(responseBuffer, 0, responseBuffer.Length);

                            int rcvdcount = socket.Receive(responseBuffer);

                            byte[] RcvdBytes = new byte[rcvdcount];

                            Buffer.BlockCopy(responseBuffer, 0, RcvdBytes, 0, rcvdcount);
                            //
                            IPEndPoint remotePeer = socket.RemoteEndPoint as IPEndPoint;

                            RecievedMessageInfo rcvdObject = new RecievedMessageInfo() { data = RcvdBytes, PeerIdentity = new URI("aaa://" + remotePeer.Address + ":" + remotePeer.Port + ";transport=tcp;protocol=diameter") };

                            DiameterAAAStack.RaisePeerStateChangeEvent(PEER_STATE_EVENT.Rcv_Message, RcvdBytes);

                            Array.Clear(responseBuffer, 0, responseBuffer.Length);
                        }
                    //}
                    //catch (Exception ex)
                    //{
                    //}
                }

            }
            catch (Exception exp)
            {
                //Write Log Here..
                Common.StackLog.Write2ErrorLog("Transport::StartListners()", "Error:" + exp.Message + " Stack:" + exp.StackTrace);
                //Shutdown and end connection

                //listner.Stop();

                //throw exp;
            }
            finally
            {
                listner.Stop();
            }


        }



        /// <summary>
        /// This is Utility Method to get the Length of Recieved Message
        /// </summary>
        /// <param name="msgLengthBytes"></param>
        /// <returns></returns>
        private int GetMessageLength(byte[] msgLengthBytes)
        {
            byte[] buffer = new byte[3];

            System.Buffer.BlockCopy(msgLengthBytes, 1, buffer, 0, 3);

            if (BitConverter.IsLittleEndian)
                buffer = buffer.Reverse().ToArray();

            int MessageLength = converToInt(buffer);

            // MessageLength = IPAddress.HostToNetworkOrder(MessageLength);
            return MessageLength;
        }
        /// <summary>
        /// Convert 3-Bytes to Integer
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        int converToInt(byte[] arr)
        {
            return arr[0] + (arr[1] << 8) + (arr[2] << 16);

        }

    }

    public class RecievedMessageInfo
    {
        public object data { set; get; }
        public URI PeerIdentity { get; set; }
    }
    class StateObject
    {
        public Socket workSocket = null;								// Client socket.
        public const int BufferSize = 102048;      // Max Size of receive buffer.
        public int Position = 0;										// Size of receive buffer.
        public byte[] buffer = new byte[BufferSize];					// receive buffer.
    }
}
