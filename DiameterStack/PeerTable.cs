using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiameterStack
{

    /// <summary>
    /// @Author Inam Khosa 
    /// @Date: October, 2013
    /// </summary>

    public class PeerTable
    {

        public static List<Peer> peerList = new List<Peer>();


        public PeerTable()
        {

        }
        /// <summary>
        /// Loads Peers from Config File
        /// </summary>
        /// <returns></returns>
        public List<Peer> LoadPeers()
        {
            List<Peer> peers = new List<Peer>();

            try
            {
                string PeerCfg = string.Empty;

                if (ConfigurationManager.AppSettings["PeerConfFile"] != null && ConfigurationManager.AppSettings["PeerConfFile"] != string.Empty)
                    PeerCfg = ConfigurationManager.AppSettings["PeerConfFile"];
                else
                    PeerCfg = "Peers.xml";

                DataSet ds = new DataSet();

                ds.ReadXml(PeerCfg);

                if (ds.Tables.Count > 0)
                    foreach (DataRow row in ds.Tables["Peer"].Rows)
                    {
                        Peer peer = new Peer();

                        peer.Hostidentity = row["HostIdentity"].ToString();

                        URI uri = new URI(peer.Hostidentity);

                        peer.PeerIPAddress = uri.FQDN;

                        peer.PeerPort = uri.Port;

                        peer.StatusT = Convert.ToInt32(row["StatusT"]);

                        peer.IsDynamic = Convert.ToBoolean((row["IsDynamic"]));

                        peer.TLSEnabled = Convert.ToBoolean((row["IsTLSEnabled"]));

                        peers.Add(peer);
                    }

                return peers;
            }
            catch (Exception exp)
            {
                throw exp;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="PeerURI"></param>
        /// <returns></returns>
        public static Peer FindPeer(Peer peer)
        {
            Peer requestedPeer = peerList.Find(p => p.Hostidentity == peer.Hostidentity);

            return requestedPeer;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static List<Peer> GetPeerTable()
        {
            return peerList;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="peer"></param>
        public static void AddPeer(Peer peer)
        {

            peerList.Add(peer);

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="peer"></param>
        /// <returns></returns>
        public static bool RemovePeer(Peer peer)
        {

            bool returVal = peerList.Remove(peer);

            return returVal;

        }


    }
}
