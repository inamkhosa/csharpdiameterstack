using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DiameterStack
{
    public enum PeerState
    {
        CLOSED = 0,
        OPEN = 1,
        CLOSING = 2,
        TIMEDOUT = 3

    }



    /// <summary>
    /// @Author Inam Khosa 
    /// @Date: October, 2013
    /// </summary>

    public enum PEER_STATE_EVENT
    {
        Start = 1,           // The Diameter application has signaled that a connection should be initiated with the peer.
        R_Conn_CER = 2,      // An acknowledgement is received stating that the  transport connection has been established, and the associated CER has arrived.
        Rcv_Conn_Ack = 3,    //A positive acknowledgement is received confirming that the transport connection is established.
        Rcv_Conn_Nack = 4,   //A negative acknowledgement was received stating that the transport connection was not established.
        Timeout = 5,         //An application_defined timer has expired while waiting  for some event.
        Rcv_CER = 6,         //A CER message from the peer was received.
        Rcv_CEA = 7,         //A CEA message from the peer was received.
        Rcv_Non_CEA = 8,     //A message other than CEA from the peer was received.
        Peer_Disc = 9,       //A disconnection indication from the peer was received.
        Rcv_DPR = 10,        //A DPR message from the peer was received.
        Rcv_DPA = 11,        //A DPA message from the peer was received.
        Win_Election = 12,   //An election was held, and the local node was the  winner.
        Send_Message = 13,   //A message is to be sent.
        Rcv_Message = 14,    //A message other than CER, CEA, DPR, DPA, DWR or DWA was received.
        Stop = 15,         //The Diameter application has signaled that a connection should be terminated (e.g., on system shutdown).
        Rcv_DWR = 16
    };

    public class Peer
    {
        public string Hostidentity { get; set; }
        
        public int StatusT { get; set; }
        
        public bool IsDynamic { get; set; }
       
        public DateTime ExpirationTime { get; set; }

        public bool TLSEnabled { get; set; }
                
        public string PeerIPAddress { get; set; }

        public int PeerPort { get; set; }

        public TcpClient PeerConnection { get; set; }

        public PeerState PeerState{ get; set; }

        public PeerState NextState{ get; set; }

        public PEER_STATE_EVENT RecievedEvent{ get; set; }

        //public delegate void Action(object sender, EventArgs e);
  
    }
}
