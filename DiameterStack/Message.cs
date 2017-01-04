using DiameterStack.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Concurrent;
using System.Xml.Linq;

namespace DiameterStack
{

    /// <summary>
    /// @Author Inam Khosa 
    /// @Date: October, 2013
    /// </summary>

    public class Message
    {
        public int ProtocolVersion = 1;

        public int MessageLength;

        //public byte Flags;
        public long ExecTime = 0;

        public bool IsRequest = true;

        public int CommandCode;

        public int ApplicationID;


        public int HopbyHopIdentifier;

        public int EndtoEndIdentifier;

        //Non-Thread Safe
        public List<Avp> avps;

        //Avps XML
        public string AvpsXML = "";
        private string mSessionID;

        Dictionary<int, AttributeInfo> dict;

        const int Max_MSG_LENGTH = 4000;
        //For Debugging Purpose only 

        bool IsDebugEnabled = true;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="CommandCode"></param>
        /// <param name="ApplicationID"></param>
        /// <param name="CommandFlags"></param>
        public Message(int commadCode, int ApplicationId, bool rbit, bool pbit, bool ebit, bool tbit, List<Avp> avpList)
        {
            this.CommandCode = commadCode;

            this.ApplicationID = ApplicationId;
            SetRequestBit(rbit);
            this.avps = avpList;

            if (dict == null)
            {
                AvpDictionary obj = new AvpDictionary();
                dict = obj.LoadDictionary();
            }

            if (avps == null)
                avps = new List<Avp>();

            if (ConfigurationManager.AppSettings["EnableMessageDebug"] != "" && ConfigurationManager.AppSettings["EnableMessageDebug"] == "true")
                IsDebugEnabled = true;
            else
                IsDebugEnabled = false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stackContext"></param>
        /// <param name="commadCode"></param>
        /// <param name="ApplicationId"></param>
        /// <param name="rbit"></param>
        /// <param name="pbit"></param>
        /// <param name="ebit"></param>
        /// <param name="tbit"></param>
        public Message(StackContext stackContext, int commadCode, int ApplicationId, bool rbit, bool pbit, bool ebit, bool tbit)
        {
            this.CommandCode = commadCode;

            this.ApplicationID = ApplicationId;

            SetRequestBit(rbit);

            if (dict == null)
            {
                AvpDictionary obj = new AvpDictionary();
                dict = obj.LoadDictionary();
            }



            if (ConfigurationManager.AppSettings["EnableMessageDebug"] != "" && ConfigurationManager.AppSettings["EnableMessageDebug"] == "true")
                IsDebugEnabled = true;
            else
                IsDebugEnabled = false;

            if (avps == null)
                avps = new List<Avp>();

        }

        /// <summary>
        /// 
        /// </summary>
        public Message()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public Message(StackContext stackContext)
        {
            dict = stackContext.dictionary;
            IsDebugEnabled = stackContext.EnableDebug;
            avps = new List<Avp>();

        }
        public string SessionID
        {
            get {
                return mSessionID;
            }
            set {mSessionID = value;}
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public byte[] GetBytes()
        {


            try
            {
                int offset = 1;

                byte[] MessageBytes = new byte[Max_MSG_LENGTH];

                //Version 
                MessageBytes[0] = 1; //(byte)IPAddress.HostToNetworkOrder(1);

                //Message Length Place Holder left for copying message length in these three bytes latter.
                offset += 3;

                //Set Request Bit on , this is MSB Bit (7)
                byte flags = (byte)(1 << 7);

                if (!IsRequest)
                    flags = (byte)(0 << 7);

                Buffer.BlockCopy(BitConverter.GetBytes(flags), 0, MessageBytes, offset, 1);
                offset += 1;

                //Command Code
                int cmdCode = IPAddress.HostToNetworkOrder(CommandCode);
                Buffer.BlockCopy(BitConverter.GetBytes(cmdCode), 1, MessageBytes, offset, 3);
                offset += 3;

                //Application ID 
                int ApplicationId = IPAddress.HostToNetworkOrder(ApplicationID);
                Buffer.BlockCopy(BitConverter.GetBytes(ApplicationId), 0, MessageBytes, offset, 4);
                offset += 4;

                //End to End Identifier
                int E2EIdentifier = IPAddress.HostToNetworkOrder(EndtoEndIdentifier);
                Buffer.BlockCopy(BitConverter.GetBytes(E2EIdentifier), 0, MessageBytes, offset, 4);
                offset += 4;

                //Hop by Hop Identifier
                int HopByHopId = IPAddress.HostToNetworkOrder(HopbyHopIdentifier);
                Buffer.BlockCopy(BitConverter.GetBytes(HopByHopId), 0, MessageBytes, offset, 4);
                offset += 4;

                foreach (Avp avp in avps)
                {
                    byte[] avpBytes = avp.GetAvpBytes(avp, dict);
                    Buffer.BlockCopy(avpBytes, 0, MessageBytes, offset, avpBytes.Length);
                    offset += avpBytes.Length;
                }

                MessageLength = offset;

                //Copy Length , Dicard MSB
                int length = IPAddress.HostToNetworkOrder(MessageLength);

                Buffer.BlockCopy(BitConverter.GetBytes(length), 1, MessageBytes, 1, 3);

                byte[] bytesToReturn = new byte[MessageLength];

                Buffer.BlockCopy(MessageBytes, 0, bytesToReturn, 0, bytesToReturn.Length);

                //Test Call to our own Method for Processing the Message Succesfully if it Does then Other Applications will do. 
                //ParseMessage(bytesToReturn);

                return bytesToReturn;

            }
            catch (Exception exp)
            {
                throw exp;
            }



        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="recievedBuffer"></param>
        public Message(StackContext stackContext, byte[] recievedBuffer)
        {
            dict = stackContext.dictionary;

            IsDebugEnabled = stackContext.EnableDebug;
            IsRequest = false;

            avps = new List<Avp>();

            try
            {
                if (IsDebugEnabled)
                    Common.StackLog.Write2TraceLog("Message::ParseMessage", "Parsing Diameter Message");

                if (recievedBuffer.Length > 0)
                {


                    //Parse Buffer to Construct Message
                    MemoryStream memStream = new MemoryStream(recievedBuffer);
                    //Reader
                    BinaryReader reader = new BinaryReader(memStream);

                    //Protocol Version
                    ProtocolVersion = reader.ReadByte();
                    //check ProtocolVersion
                    if (ProtocolVersion != 1)
                        throw new Exception("Invalid Protocol Version");

                    //Mesage Length
                    byte[] tempMsgLeng = reader.ReadBytes(3);


                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(tempMsgLeng);

                    MessageLength = converToInt(tempMsgLeng);
                    if (MessageLength > recievedBuffer.Length)
                    {
                        StackLog.Write2ErrorLog("Message::ParseMessage", "Message Length (" + MessageLength.ToString() + ") is greater than Msg Received Len: " + recievedBuffer.Length.ToString() + ", Msg: " + recievedBuffer.ToString());
                        throw new Exception("Invalid Message Length");
                    }

                    // check message body
                    //if (MessageLength <= 20)
                    //{
                    //    StackLog.Write2ErrorLog("Message::ParseMessage", "Message Recived Length ("+MessageLength.ToString()+") is invalid, Msg: " + recievedBuffer.ToString());
                    //    return;
                    //}

                    //Command Flags
                    BitArray CommandFlags = new BitArray(reader.ReadByte());
                    //Command Code
                    byte[] tempCmdCode = reader.ReadBytes(3);

                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(tempCmdCode);

                    CommandCode = converToInt(tempCmdCode);

                    //ApplicationID
                    ApplicationID = IPAddress.NetworkToHostOrder(reader.ReadInt32());

                    //HopeByHop ID
                    HopbyHopIdentifier = IPAddress.NetworkToHostOrder(reader.ReadInt32());

                    //End to End ID
                    EndtoEndIdentifier = IPAddress.NetworkToHostOrder(reader.ReadInt32());

                    //Get All AVP Bytes
                    int HeaderLength = 20;
                    if (MessageLength < HeaderLength)
                    {
                        //HeaderLength = 0;
                        StackLog.Write2ErrorLog("Message::ParseMessage", "Command Code: " + CommandCode.ToString() + ", Message Recived Length (" + MessageLength.ToString() + "), Binary Reader Contains: " + System.Text.Encoding.Default.GetString(reader.ReadBytes(MessageLength)));
                    }
                    byte[] avpBytes = reader.ReadBytes(MessageLength - HeaderLength);

                    int Padding = 0;

                    avps = ProcessAvps(avpBytes, ref Padding, recievedBuffer);

                    Avp vSessionAvp = avps.Find(a => a.AvpName == "Session-Id");
                    if(vSessionAvp != null)
                        mSessionID = vSessionAvp.AvpValue.ToString();


                    //AvpsXML = ToXML(this);

                    return;
                }
                else
                {
                    if (IsDebugEnabled)
                    {
                        StackLog.Write2TraceLog("ParseMsg", "Bad Format Message Recived");

                    }
                    StackLog.Write2ErrorLog("Message::ParseMessage", "Bad Format Message Recived");
                    return;
                }

            }
            catch (Exception exp)
            {
                Common.StackLog.Write2ErrorLog("Message::ProcessMessage", "Error:" + exp.Message + " Stack:" + exp.StackTrace);
                throw exp;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="recievedBuffer"></param>
        public Message ProcessMessage(StackContext stackContext, byte[] recievedBuffer)
        {
            dict = stackContext.dictionary;

            IsDebugEnabled = stackContext.EnableDebug;

            avps = new List<Avp>();

            try
            {
                if (IsDebugEnabled)
                    Common.StackLog.Write2TraceLog("Message::ParseMessage", "Parsing Diameter Message");

                if (recievedBuffer.Length > 0)
                {


                    //Parse Buffer to Construct Message
                    MemoryStream memStream = new MemoryStream(recievedBuffer);
                    //Reader
                    BinaryReader reader = new BinaryReader(memStream);

                    //Protocol Version
                    ProtocolVersion = reader.ReadByte();
                    //Mesage Length
                    byte[] tempMsgLeng = reader.ReadBytes(3);


                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(tempMsgLeng);

                    MessageLength = converToInt(tempMsgLeng);
                    if (MessageLength > recievedBuffer.Length)
                    {
                        StackLog.Write2ErrorLog("Message::ProcessMessage", "Message Length (" + MessageLength.ToString() + ") is greater than Msg Received Len: " + recievedBuffer.Length.ToString() + ", Msg: " + System.Text.Encoding.Default.GetString(recievedBuffer));
                        return null;
                    }
                    //Command Flags

                    BitArray CommandFlags = new BitArray(reader.ReadByte());
                    //Command Code
                    byte[] tempCmdCode = reader.ReadBytes(3);

                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(tempCmdCode);

                    CommandCode = converToInt(tempCmdCode);

                    //ApplicationID
                    ApplicationID = IPAddress.NetworkToHostOrder(reader.ReadInt32());

                    //HopeByHop ID
                    HopbyHopIdentifier = IPAddress.NetworkToHostOrder(reader.ReadInt32());

                    //End to End ID
                    EndtoEndIdentifier = IPAddress.NetworkToHostOrder(reader.ReadInt32());

                    //Get All AVP Bytes
                    byte[] avpBytes = reader.ReadBytes(MessageLength - 20);

                    int Padding = 0;

                    avps = ProcessAvps(avpBytes, ref Padding, recievedBuffer);

                    Message recievedDiamMessage = new Message(CommandCode, ApplicationID, false, false, false, false, avps);

                    if (recievedDiamMessage.CommandCode == DiameterMessageCode.CREDIT_CONTROL)
                    {
                        Avp vSessionAvp = avps.Find(a => a.AvpName == "Session-Id");
                        recievedDiamMessage.SessionID = vSessionAvp.AvpValue.ToString();
                    }

                    recievedDiamMessage.MessageLength = MessageLength;

                    return recievedDiamMessage;
                }
                else
                {
                    if (IsDebugEnabled)
                    {
                        StackLog.Write2TraceLog("ParseMsg", "Bad Format Message Recived");

                    }
                    StackLog.Write2ErrorLog("Message::ParseMessage", "Bad Format Message Recived");
                    return null;
                }

            }
            catch (Exception exp)
            {
                Common.StackLog.Write2ErrorLog("Message::ProcessMessage", "Error:" + exp.Message + " Stack:" + exp.StackTrace);
                throw exp;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="avpBytes"></param>
        private List<Avp> ProcessAvps(byte[] avpBytes, ref int Padding, byte[] messageBuffer)
        {
            List<Avp> avpList = new List<Avp>();


            try
            {
                MemoryStream memStream = new MemoryStream(avpBytes);

                BinaryReader avpReader = new BinaryReader(memStream);

                int readCount = 0;

                int AvpHeaderLength = 8;



                while (readCount < avpBytes.Length)
                {
                    try
                    {
                        int AvpLength = 0;

                        //Read Avp Code 4 Bytes
                        int AvpCode = IPAddress.NetworkToHostOrder(avpReader.ReadInt32());
                        readCount += 4;

                        //Read AVP Flags 1 Bytes
                        //BitArray AvpFlags = new BitArray(avpReader.ReadBytes(1));
                        byte[] AvpFlagsArr = avpReader.ReadBytes(1);
                        byte AvpFlags = AvpFlagsArr[0];
                        readCount += 1;

                        //Read AvpLength 3 Bytes
                        byte[] tempAvpLeng = avpReader.ReadBytes(3);
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(tempAvpLeng);
                        readCount += 3;

                        AvpLength = converToInt(tempAvpLeng);

                        //Read Vendor ID
                        int VendorID = 0;
                        if ((AvpFlags & (1 << 7)) != 0)
                        {
                            VendorID = IPAddress.NetworkToHostOrder(avpReader.ReadInt32());
                            readCount += 4;
                            AvpHeaderLength += 4;
                        }

                        //AVP Data Length
                        int dataLength = AvpLength - AvpHeaderLength;
                        readCount += dataLength;

                        if (IsDebugEnabled)
                            Common.StackLog.Write2TraceLog("Message::ProcessAvp", "Dictionary Lookup For Avp = " + AvpCode);

                        if (!dict.ContainsKey(AvpCode))
                        {
                            string MessageBufferHex = BitConverter.ToString(messageBuffer).Replace("-", "");
                            StackLog.Write2ErrorLog("ProcessAvps", MessageBufferHex);
                            throw new Exception("AvpCode " + AvpCode.ToString() + " not found in dictionary");
                        }

                        AttributeInfo avpInfo = dict[AvpCode];

                        switch (avpInfo.DataType)
                        {

                            case "Address":
                                {


                                    short addressFamily = avpReader.ReadInt16();

                                    addressFamily = IPAddress.NetworkToHostOrder(addressFamily);

                                    byte[] buff = avpReader.ReadBytes(dataLength - 2);

                                    sbyte[] data = new sbyte[4];

                                    System.Buffer.BlockCopy(buff, 0, data, 0, 4);

                                    IPAddress ipAddr = DiameterType.bytesToAddress(data);

                                    Avp avp = new Avp(AvpCode, (object)ipAddr) { AvpName = avpInfo.AttributeName, AvpLength = AvpLength, VendorId = VendorID };

                                    avpList.Add(avp);
                                    //avpList.Add(new Avp(AVPCode, (object)ipAddr));

                                    int remainder = dataLength % 4;

                                    Padding = 4 - remainder;

                                    if (remainder == 0)
                                        Padding = 0;
                                    else
                                        Padding = 4 - remainder;

                                    avpReader.ReadBytes(Padding);

                                    //Count the Padding 
                                    readCount += Padding;

                                    if (IsDebugEnabled)
                                        Common.StackLog.Write2TraceLog("Message::ProcessAvp", avpInfo.AttributeName + "=" + ipAddr);

                                    break;
                                }

                            case "OctetString":
                                {
                                    //AVP Data
                                    byte[] data = avpReader.ReadBytes(dataLength);
                                    String OctetString = Encoding.Default.GetString(data);
                                    Avp avp = new Avp(AvpCode, (object)OctetString) { AvpName = avpInfo.AttributeName, AvpLength = AvpLength, VendorId = VendorID };
                                    avpList.Add(avp);
                                    //avpList.Add(new Avp(AVPCode, (object)OctetString));
                                    int remainder = dataLength % 4;

                                    Padding = 4 - remainder;

                                    if (remainder == 0)
                                        Padding = 0;
                                    else
                                        Padding = 4 - remainder;

                                    //Read Padding
                                    if (Padding != 0)
                                        avpReader.ReadBytes(Padding);

                                    //Count the Padding 
                                    readCount += Padding;

                                    if (IsDebugEnabled)
                                        Common.StackLog.Write2TraceLog("Message::ProcessAvp", avpInfo.AttributeName + "=" + OctetString);
                                    break;
                                }

                            case "DiamIdent":
                                {

                                    //AVP Data
                                    byte[] data = avpReader.ReadBytes(dataLength);
                                    String DiamIdent = Encoding.UTF8.GetString(data);

                                    Avp avp = new Avp(AvpCode, (object)DiamIdent) { AvpName = avpInfo.AttributeName, AvpLength = AvpLength, VendorId = VendorID };
                                    avpList.Add(avp);
                                    //avpList.Add(new Avp(AVPCode, (object)DiamIdent));

                                    //Padding
                                    int remainder = dataLength % 4;

                                    Padding = 4 - remainder;
                                    if (remainder == 0)
                                        Padding = 0;
                                    else
                                        Padding = 4 - remainder;
                                    //Read Padding
                                    if (Padding != 0)
                                        avpReader.ReadBytes(Padding);

                                    //Count the Padding 
                                    readCount += Padding;

                                    if (IsDebugEnabled)
                                        Common.StackLog.Write2TraceLog("Message::ProcessAvp", avpInfo.AttributeName + "=" + DiamIdent);


                                    break;
                                }

                            case "Unsigned32":
                                {
                                    //AVP Data
                                    byte[] data = avpReader.ReadBytes(dataLength);
                                    //bytesCount += dataLength;
                                    int Unsigned32 = BitConverter.ToInt32(data, 0);

                                    int value = IPAddress.NetworkToHostOrder(Unsigned32);

                                    if (value < 0)
                                    {
                                        uint x = (uint)-(uint)value;
                                        value = Convert.ToInt32(x);
                                    }

                                    Avp avp = new Avp(AvpCode, (object)value) { AvpName = avpInfo.AttributeName, AvpLength = AvpLength, VendorId = VendorID };

                                    avpList.Add(avp);
                                    //avpList.Add(new Avp(AVPCode, (object)value));
                                    if (IsDebugEnabled)
                                        Common.StackLog.Write2TraceLog("Message::ProcessAvp", avpInfo.AttributeName + "=" + value);
                                    break;
                                }

                            case "Unsigned64":
                                {
                                    //AVP Data
                                    byte[] data = avpReader.ReadBytes(dataLength);
                                    //bytesCount += dataLength;
                                    Int64 U64 = BitConverter.ToInt64(data, 0);

                                    Int64 value = IPAddress.NetworkToHostOrder(U64);

                                    Avp avp = new Avp(AvpCode, (object)value) { AvpName = avpInfo.AttributeName, AvpLength = AvpLength, VendorId = VendorID };
                                    avpList.Add(avp);

                                    //avpList.Add(new Avp(AVPCode, (object)value));
                                    if (IsDebugEnabled)
                                        Common.StackLog.Write2TraceLog("Message::ProcessAvp", avpInfo.AttributeName + "=" + value);
                                    break;

                                }
                            case "Integer32":
                                {
                                    //AVP Data
                                    byte[] data = avpReader.ReadBytes(dataLength);
                                    //bytesCount += dataLength;
                                    int SignedVal = BitConverter.ToInt32(data, 0);

                                    int value = IPAddress.NetworkToHostOrder(SignedVal);

                                    //if (value < 0)
                                    //{
                                    //    uint x = (uint)-(uint)value;
                                    //    value = Convert.ToInt32(x);
                                    //}

                                    Avp avp = new Avp(AvpCode, (object)value) { AvpName = avpInfo.AttributeName, AvpLength = AvpLength, VendorId = VendorID };

                                    avpList.Add(avp);
                                    //avpList.Add(new Avp(AVPCode, (object)value));
                                    if (IsDebugEnabled)
                                        Common.StackLog.Write2TraceLog("Message::ProcessAvp", avpInfo.AttributeName + "=" + value);
                                    break;
                                }
                            case "Integer8":
                                {
                                    //AVP Data
                                    byte[] data = avpReader.ReadBytes(dataLength);
                                    //bytesCount += dataLength;
                                    long EightByteInt = BitConverter.ToInt64(data, 0);

                                    Int64 value = IPAddress.NetworkToHostOrder(EightByteInt);

                                    Avp avp = new Avp(AvpCode, (object)value) { AvpName = avpInfo.AttributeName, AvpLength = AvpLength, VendorId = VendorID };
                                    avpList.Add(avp);

                                    //avpList.Add(new Avp(AVPCode, (object)value));
                                    if (IsDebugEnabled)
                                        Common.StackLog.Write2TraceLog("Message::ProcessAvp", avpInfo.AttributeName + "=" + value);
                                    break;

                                }
                            case "Integer64":
                                {
                                    //AVP Data
                                    byte[] data = avpReader.ReadBytes(dataLength);
                                    //bytesCount += dataLength;
                                    Int64 Sixty4BitInt = BitConverter.ToInt64(data, 0);

                                    Int64 value = IPAddress.NetworkToHostOrder(Sixty4BitInt);

                                    Avp avp = new Avp(AvpCode, (object)value) { AvpName = avpInfo.AttributeName, AvpLength = AvpLength, VendorId = VendorID };
                                    avpList.Add(avp);

                                    //avpList.Add(new Avp(AVPCode, (object)value));
                                    if (IsDebugEnabled)
                                        Common.StackLog.Write2TraceLog("Message::ProcessAvp", avpInfo.AttributeName + "=" + value);
                                    break;

                                }

                            case "UTF8String":
                                {
                                    //AVP Data
                                    byte[] data = avpReader.ReadBytes(dataLength);
                                    String UTF8String = Encoding.UTF8.GetString(data);

                                    Avp avp = new Avp(AvpCode, (object)UTF8String) { AvpName = avpInfo.AttributeName, AvpLength = AvpLength, VendorId = VendorID };
                                    avpList.Add(avp);
                                    //avpList.Add(new Avp(AVPCode, (object)UTF8String));
                                    //Padding
                                    int remainder = dataLength % 4;
                                    Padding = 4 - remainder;
                                    if (remainder == 0)
                                        Padding = 0;
                                    else
                                        Padding = 4 - remainder;

                                    avpReader.ReadBytes(Padding);
                                    //Count the Padding 
                                    readCount += Padding;


                                    if (IsDebugEnabled)
                                        Common.StackLog.Write2TraceLog("Message::ProcessAvp", avpInfo.AttributeName + "=" + UTF8String);

                                    break;
                                }

                            case "Enumerated":
                                {
                                    //AVP Data
                                    byte[] data = avpReader.ReadBytes(dataLength);
                                    Int32 Enumerated = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, 0));

                                    Avp avp = new Avp(AvpCode, (object)Enumerated) { AvpName = avpInfo.AttributeName, AvpLength = AvpLength, VendorId = VendorID };
                                    avpList.Add(avp);
                                    //avpList.Add(new Avp(AVPCode, (object)Enumerated));
                                    if (IsDebugEnabled)
                                        Common.StackLog.Write2TraceLog("Message::ProcessAvp", avpInfo.AttributeName + "=" + Enumerated);
                                    break;
                                }


                            case "Time":
                                {
                                    //AVP Data
                                    byte[] data = avpReader.ReadBytes(dataLength);

                                    //Get  NTP Timestamp from NTP Bytes ( 4 High Order Bytes from NTP Timestamp)
                                    DateTime BaseDate = new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                                    UInt32 seconds = DiameterType.BytesToSeconds(data);
                                    DateTime date = BaseDate.AddSeconds(seconds);//.AddMilliseconds(milliseconds);

                                    //Add Avp to Incoming Avps List
                                    Avp avp = new Avp(AvpCode, (object)date) { AvpName = avpInfo.AttributeName, AvpLength = AvpLength, VendorId = VendorID };
                                    avpList.Add(avp);

                                    //avpList.Add(new Avp(AVPCode, (object)date));

                                    int remainder = dataLength % 4;

                                    Padding = 4 - remainder;

                                    if (remainder == 0)
                                        Padding = 0;
                                    else
                                        Padding = 4 - remainder;

                                    //Read Padding
                                    if (Padding != 0)
                                        avpReader.ReadBytes(Padding);

                                    //Count the Padding 
                                    readCount += Padding;
                                    if (IsDebugEnabled)
                                        Common.StackLog.Write2TraceLog("Message::ProcessAvp", avpInfo.AttributeName + "=" + date);
                                    break;

                                }


                            case "Grouped":
                                {
                                    //AVP Data
                                    byte[] data = avpReader.ReadBytes(dataLength);

                                    Avp groupedAvp = new Avp();

                                    groupedAvp.AvpCode = AvpCode;
                                    groupedAvp.AvpName = avpInfo.AttributeName;
                                    groupedAvp.VendorId = VendorID;
                                    groupedAvp.isGrouped = true;
                                    //Inam 22-10-2013
                                    //groupedAvp.groupedAvps = ProcessGroupedAvpEx(ref groupedAvp, data);

                                    ProcessGroupedAvpEx(ref groupedAvp, data, messageBuffer);
                                    avpList.Add(groupedAvp);
                                    break;
                                }


                        }


                    }
                    catch (Exception exp)
                    {

                        Common.StackLog.Write2ErrorLog("Message::ProcessAvps", "Error:" + exp.Message + " Stack:" + exp.StackTrace);
                        throw exp;
                    }
                }

            }
            catch (Exception exp)
            {

                Common.StackLog.Write2ErrorLog("Message::ProcessAvps", "Error:" + exp.Message + " Stack:" + exp.StackTrace);
                throw exp;

            }

            return avpList;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="avpBytes"></param>
        /// <returns></returns>
        private void ProcessGroupedAvpEx(ref Avp gAvp, byte[] groupedBytes, byte[] messageBuffer)
        {


            int bytesCount = 0;

            int AvpHeaderLength = 8;


            while (bytesCount < groupedBytes.Length)
            {
                byte[] buffAvpCode = new byte[4];

                Buffer.BlockCopy(groupedBytes, bytesCount, buffAvpCode, 0, 4);

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(buffAvpCode);

                int Code = BitConverter.ToInt32(buffAvpCode, 0);

                //int AvpCode = IPAddress.NetworkToHostOrder(avpReader.ReadInt32());
                if (IsDebugEnabled)
                    Common.StackLog.Write2TraceLog("Message::ProcessGroupedAvpEx", "Lookup For Avp = " + Code);
                if (!dict.ContainsKey(Code))
                    throw new Exception("AvpCode " + Code.ToString() + " not found in dictionary");

                AttributeInfo avpInfo = dict[Code];

                if (avpInfo.DataType == "Grouped")
                {
                    byte[] buffAvpLen = new byte[3];

                    Buffer.BlockCopy(groupedBytes, bytesCount + 5, buffAvpLen, 0, 3);

                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(buffAvpLen);


                    int len = converToInt(buffAvpLen);

                    byte[] bytes = new byte[len];

                    Buffer.BlockCopy(groupedBytes, bytesCount, bytes, 0, len);
                    //Increment BytesCount
                    bytesCount += len;

                    MemoryStream memStream = new MemoryStream(bytes);

                    BinaryReader avpReader = new BinaryReader(memStream);

                    int AvpLength = 0;

                    //Read AVP Code 4 Bytes
                    int AvpCode = IPAddress.NetworkToHostOrder(avpReader.ReadInt32());


                    //Read AVP Flags 1 Bytes
                    //BitArray AvpFlags = new BitArray(avpReader.ReadBytes(1));
                    byte[] AvpFlagsArr = avpReader.ReadBytes(1);
                    byte AvpFlags = AvpFlagsArr[0];


                    //Read AvpLength 3 Bytes
                    byte[] tempAvpLeng = avpReader.ReadBytes(3);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(tempAvpLeng);


                    AvpLength = converToInt(tempAvpLeng);

                    //Read Vendor ID
                    int VendorID = 0;
                    if ((AvpFlags & (1 << 7)) != 0)
                    {
                        VendorID = IPAddress.NetworkToHostOrder(avpReader.ReadInt32());
                        AvpHeaderLength += 4;

                    }

                    //AVP Data Length
                    int dataLength = AvpLength - AvpHeaderLength;

                    byte[] data = avpReader.ReadBytes(dataLength);
                    Avp nestedAvp = new Avp();
                    nestedAvp.AvpCode = AvpCode;
                    nestedAvp.AvpName = avpInfo.AttributeName;
                    nestedAvp.VendorId = VendorID;
                    nestedAvp.isGrouped = true;


                    if (IsDebugEnabled)
                        Common.StackLog.Write2TraceLog("Message::ProcessGroupedAvpEx", "Adding Avp = " + nestedAvp.AvpName + " To grouped Avp " + gAvp.AvpName);
                    ProcessGroupedAvpEx(ref nestedAvp, data, messageBuffer);
                    gAvp.groupedAvps.Add(nestedAvp);

                }
                else
                {
                    byte[] buffAvpLen = new byte[3];

                    Buffer.BlockCopy(groupedBytes, bytesCount + 5, buffAvpLen, 0, 3);
                    //bytesCount += 3;
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(buffAvpLen);
                    int Padding = 0;

                    int AvpLength = converToInt(buffAvpLen);

                    byte[] bytes = new byte[AvpLength];

                    Buffer.BlockCopy(groupedBytes, bytesCount, bytes, 0, AvpLength);


                    Avp innerAvp = ProcessAvps(bytes, ref Padding, messageBuffer)[0];

                    if (IsDebugEnabled)
                        Common.StackLog.Write2TraceLog("Message::ProcessGroupedAvpEx", "Adding Avp '" + innerAvp.AvpName + "' To grouped Avp " + gAvp.AvpName);

                    gAvp.groupedAvps.Add(innerAvp);
                    //Inam - 22-10-2013
                    //avpList.Add(ProcessAvps(bytes,ref Padding)[0]);

                    bytesCount += bytes.Length + Padding;
                }
            }


        }


        /// <summary>
        /// dump message to a console
        /// </summary>
        /*    public void PrintMessage()
            {
                Common.StackLog.WriteLine("Message::PrintMessage", "Message CommandCode = " + this.CommandCode);

                foreach (Avp avp in avps)
                {

                    if (avp.isGrouped)
                    {
                        Console.WriteLine("<" + dict[avp.AvpCode].AttributeName + " >");
                        foreach (Avp avpg in avp.groupedAvps)
                        {

                            if (avpg.isGrouped)
                            {
                                Console.WriteLine(" <" + dict[avpg.AvpCode].AttributeName + " >");
                                foreach (Avp avpg2 in avpg.groupedAvps)
                                {

                                    if (avpg2.isGrouped)
                                    {
                                        Console.WriteLine("  <" + dict[avpg2.AvpCode].AttributeName + " >");
                                        foreach (Avp avpg3 in avpg2.groupedAvps)
                                        {
                                            if (avpg3.isGrouped)
                                            {
                                                Console.WriteLine("   <" + dict[avpg3.AvpCode].AttributeName + " >");
                                                foreach (Avp avpg4 in avpg3.groupedAvps)
                                                {
                                                    if (avpg4.isGrouped)
                                                    {
                                                        Console.WriteLine("    <" + dict[avpg4.AvpCode].AttributeName + " >");
                                                        foreach (Avp avpg5 in avpg4.groupedAvps)
                                                        {
                                                            Console.WriteLine("      <" + dict[avpg5.AvpCode].AttributeName + " = " + avpg5.AvpValue + " >");
                                                        }
                                                        Console.WriteLine("    </" + dict[avpg4.AvpCode].AttributeName + " >");
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine("    <" + dict[avpg4.AvpCode].AttributeName + " = " + avpg4.AvpValue + " />");
                                                    }
                                                }
                                                Console.WriteLine("   </" + dict[avpg3.AvpCode].AttributeName + " >");
                                            }
                                            else
                                            {
                                                Console.WriteLine("   <" + dict[avpg3.AvpCode].AttributeName + " = " + avpg3.AvpValue + " />");
                                            }

                                        }
                                        Console.WriteLine("  </" + dict[avpg2.AvpCode].AttributeName + " >");
                                    }
                                    else
                                    {
                                        Console.WriteLine("  <" + dict[avpg2.AvpCode].AttributeName + " = " + avpg2.AvpValue + " />");
                                    }
                                
                                }
                                Console.WriteLine(" </" + dict[avpg.AvpCode].AttributeName + " >");
                            }

                            else
                            {
                                Console.WriteLine(" <" + dict[avpg.AvpCode].AttributeName + "="+ avpg.AvpValue.ToString()+ " >");
                            }

                        
                        }
                    

                    }
                    else
                    {
                        Console.WriteLine("<"+dict[avp.AvpCode].AttributeName + " = " + avp.AvpValue +" />");
                    }
                }

            }*/


        public void PrintMessage()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("\n\n\nDiameter Messagge :: <" + DiameterMessageCode.GetMessageName(this.CommandCode, IsRequest) + ">SessionId:(" + mSessionID + ")ExecTime:(" + ExecTime + ")\r\n");
            foreach (Avp avp in avps)
            {

                if (avp.isGrouped)
                {
                    sb.Append("<" + dict[avp.AvpCode].AttributeName + " >\r\n");
                    foreach (Avp avpg in avp.groupedAvps)
                    {

                        if (avpg.isGrouped)
                        {
                            sb.Append(" <" + dict[avpg.AvpCode].AttributeName + " >\r\n");
                            foreach (Avp avpg2 in avpg.groupedAvps)
                            {

                                if (avpg2.isGrouped)
                                {
                                    sb.Append("  <" + dict[avpg2.AvpCode].AttributeName + " >\r\n");
                                    foreach (Avp avpg3 in avpg2.groupedAvps)
                                    {
                                        if (avpg3.isGrouped)
                                        {
                                            sb.Append("   <" + dict[avpg3.AvpCode].AttributeName + " >\r\n");
                                            foreach (Avp avpg4 in avpg3.groupedAvps)
                                            {
                                                if (avpg4.isGrouped)
                                                {
                                                    sb.Append("    <" + dict[avpg4.AvpCode].AttributeName + " >\r\n");
                                                    foreach (Avp avpg5 in avpg4.groupedAvps)
                                                    {
                                                        sb.Append("      <" + dict[avpg5.AvpCode].AttributeName + " = " + avpg5.AvpValue + " >\r\n");
                                                    }
                                                    sb.Append("    </" + dict[avpg4.AvpCode].AttributeName + " >\r\n");
                                                }
                                                else
                                                {
                                                    sb.Append("    <" + dict[avpg4.AvpCode].AttributeName + " = " + avpg4.AvpValue + " />\r\n");
                                                }
                                            }
                                            sb.Append("   </" + dict[avpg3.AvpCode].AttributeName + " >\r\n");
                                        }
                                        else
                                        {
                                            sb.Append("   <" + dict[avpg3.AvpCode].AttributeName + " = " + avpg3.AvpValue + " />\r\n");
                                        }

                                    }
                                    sb.Append("  </" + dict[avpg2.AvpCode].AttributeName + " >\r\n");
                                }
                                else
                                {
                                    sb.Append("  <" + dict[avpg2.AvpCode].AttributeName + " = " + avpg2.AvpValue + " />\r\n");
                                }

                            }
                            sb.Append(" </" + dict[avpg.AvpCode].AttributeName + " >\r\n");
                        }

                        else
                        {
                            sb.Append(" <" + dict[avpg.AvpCode].AttributeName + "=" + avpg.AvpValue.ToString() + " >\r\n");
                        }


                    }

                    sb.Append(" </" + dict[avp.AvpCode].AttributeName + " >\r\n");

                }
                else
                {
                    sb.Append("<" + dict[avp.AvpCode].AttributeName + " = " + avp.AvpValue + " />\r\n");
                }
            }

            //StackLog.Write2MessageLog(sb.ToString());
            Console.WriteLine(sb.ToString());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string ToStringOld()
        {
            StringBuilder vToString = new StringBuilder();
            vToString.Append("\r\nDiameter Message :<" + DiameterMessageCode.GetMessageName(this.CommandCode, this.IsRequest) + ">");

            foreach (Avp avp in avps)
            {

                if (avp.isGrouped)
                {
                    vToString.Append("<" + dict[avp.AvpCode].AttributeName + " >\r\n");
                    foreach (Avp avpg in avp.groupedAvps)
                    {

                        if (avpg.isGrouped)
                        {
                            vToString.Append(" <" + dict[avpg.AvpCode].AttributeName + " >\r\n");
                            foreach (Avp avpg2 in avpg.groupedAvps)
                            {

                                if (avpg2.isGrouped)
                                {
                                    vToString.Append("  <" + dict[avpg2.AvpCode].AttributeName + " >\r\n");
                                    foreach (Avp avpg3 in avpg2.groupedAvps)
                                    {
                                        if (avpg3.isGrouped)
                                        {
                                            vToString.Append("   <" + dict[avpg3.AvpCode].AttributeName + " >\r\n");
                                            foreach (Avp avpg4 in avpg3.groupedAvps)
                                            {
                                                if (avpg4.isGrouped)
                                                {
                                                    vToString.Append("    <" + dict[avpg4.AvpCode].AttributeName + " >\r\n");
                                                    foreach (Avp avpg5 in avpg4.groupedAvps)
                                                    {
                                                        vToString.Append("      <" + dict[avpg5.AvpCode].AttributeName + " = " + avpg5.AvpValue + " >\r\n");
                                                    }
                                                    vToString.Append("    </" + dict[avpg4.AvpCode].AttributeName + " >\r\n");
                                                }
                                                else
                                                {
                                                    vToString.Append("    <" + dict[avpg4.AvpCode].AttributeName + " = " + avpg4.AvpValue + " />\r\n");
                                                }
                                            }
                                            vToString.Append("   </" + dict[avpg3.AvpCode].AttributeName + " >");
                                        }
                                        else
                                        {
                                            vToString.Append("   <" + dict[avpg3.AvpCode].AttributeName + " = " + avpg3.AvpValue + " />\r\n");
                                        }

                                    }
                                    vToString.Append("  </" + dict[avpg2.AvpCode].AttributeName + " >");
                                }
                                else
                                {
                                    vToString.Append("  <" + dict[avpg2.AvpCode].AttributeName + " = " + avpg2.AvpValue + " />\r\n");
                                }

                            }
                            vToString.Append(" </" + dict[avpg.AvpCode].AttributeName + " >\r\n");
                        }

                        else
                        {
                            vToString.Append(" <" + dict[avpg.AvpCode].AttributeName + "=" + avpg.AvpValue.ToString() + " >\r\n");
                        }


                    }

                    vToString.Append(" </" + dict[avp.AvpCode].AttributeName + " >\r\n");
                }
                else
                {
                    vToString.Append("<" + dict[avp.AvpCode].AttributeName + " = " + avp.AvpValue + " />\r\n");
                }
            }
            return vToString.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            

            StringBuilder vToString = new StringBuilder();

            vToString.Append("\r\n\nDiameter Message :<" + DiameterMessageCode.GetMessageName(this.CommandCode, this.IsRequest) + ">{ Session ID = " + mSessionID + "}\r\n");

            foreach (var avp in this.avps)
            {

                if (avp.isGrouped)
                {
                    vToString.Append("<" + dict[avp.AvpCode].AttributeName + " >\r\n");
                    foreach (Avp avpg in avp.groupedAvps)
                    {

                        if (avpg.isGrouped)
                        {
                            vToString.Append(" <" + dict[avpg.AvpCode].AttributeName + " >\r\n");
                            foreach (Avp avpg2 in avpg.groupedAvps)
                            {

                                if (avpg2.isGrouped)
                                {
                                    vToString.Append("  <" + dict[avpg2.AvpCode].AttributeName + " >\r\n");
                                    foreach (Avp avpg3 in avpg2.groupedAvps)
                                    {
                                        if (avpg3.isGrouped)
                                        {
                                            vToString.Append("   <" + dict[avpg3.AvpCode].AttributeName + " >\r\n");
                                            foreach (Avp avpg4 in avpg3.groupedAvps)
                                            {
                                                if (avpg4.isGrouped)
                                                {
                                                    vToString.Append("    <" + dict[avpg4.AvpCode].AttributeName + " >\r\n");
                                                    foreach (Avp avpg5 in avpg4.groupedAvps)
                                                    {
                                                        vToString.Append("      <" + dict[avpg5.AvpCode].AttributeName + " = " + avpg5.AvpValue + " >\r\n");
                                                    }
                                                    vToString.Append("    </" + dict[avpg4.AvpCode].AttributeName + " >\r\n");
                                                }
                                                else
                                                {
                                                    vToString.Append("    <" + dict[avpg4.AvpCode].AttributeName + " = " + avpg4.AvpValue + " />\r\n");
                                                }
                                            }
                                            vToString.Append("   </" + dict[avpg3.AvpCode].AttributeName + " >");
                                        }
                                        else
                                        {
                                            vToString.Append("   <" + dict[avpg3.AvpCode].AttributeName + " = " + avpg3.AvpValue + " />\r\n");
                                        }

                                    }
                                    vToString.Append("  </" + dict[avpg2.AvpCode].AttributeName + " >");
                                }
                                else
                                {
                                    vToString.Append("  <" + dict[avpg2.AvpCode].AttributeName + " = " + avpg2.AvpValue + " />\r\n");
                                }

                            }
                            vToString.Append(" </" + dict[avpg.AvpCode].AttributeName + " >\r\n");
                        }

                        else
                        {
                            vToString.Append(" <" + dict[avpg.AvpCode].AttributeName + "=" + avpg.AvpValue.ToString() + " >\r\n");
                        }


                    }

                    vToString.Append(" </" + dict[avp.AvpCode].AttributeName + " >\r\n");
                }
                else
                {
                    vToString.Append("<" + dict[avp.AvpCode].AttributeName + " = " + avp.AvpValue + " />\r\n");
                }
            }
            return vToString.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        int converToInt(byte[] arr)
        {
            return arr[0] + (arr[1] << 8) + (arr[2] << 16);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        List<Avp> GetAvps()
        {

            return avps;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="AVPCode"></param>
        /// <returns></returns>
        public Avp GetAvp(int AvpCode)
        {
            return FindAvpByCode(AvpCode);
        }
        /// <summary>
        /// Adds an AVP to List
        /// </summary>
        /// <param name="AVP"></param>
        public void AddAvp(Avp avp)
        {
            this.avps.Add(avp);
        }
        /// <summary>
        /// Removes an AVP from the List
        /// </summary>
        /// <param name="AVP"></param>
        /// <returns></returns>
        public bool RemoveAvp(Avp AVP)
        {
            if (this.avps.Remove(AVP))
                return true;
            else
                return false;
        }
        /// <summary>
        /// Returns Avp when searched against Code
        /// </summary>
        /// <param name="AvpCode"></param>
        /// <returns></returns>
        public Avp FindAvpByCode(int AvpCode)
        {
            //Find AVP Here

            Avp avpToRet = avps.Find(a => a.AvpCode == AvpCode);

            return avpToRet;
        }
        /// <summary>
        /// Returns Avp by Name;
        /// </summary>
        /// <param name="AvpName"></param>
        /// <returns></returns>
        public Avp FindAvpByName(string AvpName)
        {
            //Find AVP Here

            Avp avpToRet = avps.Find(a => a.AvpName == AvpName);

            return avpToRet;
        }
        /// <summary>
        /// Returns Nested Avp
        /// </summary>
        /// <param name="avpPath"></param>
        /// <returns></returns>
        public Avp FindGroupedAvp(string avpPath)
        {

            string[] names = avpPath.Split('/');

            Avp mainAvp = FindAvpByName(names[0]);

            int index = 1; //Leave Main Avp and Iterate the Neted Avps

            while (index < names.Length)
            {
                mainAvp = mainAvp.groupedAvps.Find(a => a.AvpName.ToLower() == names[index].ToLower());
                index++;
            }

            return mainAvp;
        }

        /**
         * @return version of message (version filed in header)
         */
        int GetVersion()
        {

            return ProtocolVersion;

        }

        /**
         * Set 1 or 0 to R bit field of header
         * @param value true == 1 or false = 0
         */
        void SetRequestBit(bool value)
        {
            IsRequest = value;

        }

        /**
         * @return value of P bit from header of message
         */
        Boolean IsProxiable()
        {
            return true;
        }

        /**
         * Set 1 or 0 to P bit field of header
         * @param value true == 1 or false = 0
         */
        void SetProxiable(Boolean value)
        {
            this.ErrorBit = value;
        }

        /**
         * @return value of E bit from header of message
         */
        Boolean IsError()
        {
            return true;
        }

        /**
         * Set 1 or 0 to E bit field of header
         * @param value true == 1 or false = 0
         */
        void SetError(Boolean value)
        {

        }

        /**
         * @return value of T bit from header of message
         */
        Boolean IsReTransmitted()
        {
            return true;

        }

        /**
         * Set 1 or 0 to T bit field of header
         * @param value true == 1 or false = 0
         */
        void SetReTransmitted(Boolean value)
        {

        }

        /**
         * @return command code from header of message
         */
        int GetCommandCode()
        {

            return 0;

        }

        /**
         * Return message Session Id avp Value (null if avp not set) 
         * @return session id avp of message
         */
        public string GetSessionId()
        {
            return mSessionID;
        }
        public void SetSessionId(string sessionId)
        {
            mSessionID = sessionId;

        }


        /**
         * Return ApplicationId value from message header
         * @return ApplicationId value from message header
         */
        long GetApplicationId()
        {

            return ApplicationID;

        }


        /**
         * The Hop-by-Hop Identifier is an unsigned 32-bit integer field (in
         * network byte order) and aids in matching requests and replies. The
         * sender MUST ensure that the Hop-by-Hop identifier in a request is
         * unique on a given connection at any given time, and MAY attempt to
         * ensure that the number is unique across reboots. 
         * @return hop by hop identifier from header of message
         */
        public int GetHopByHopIdentifier()
        {
            Random rand = new Random((int)DateTime.Now.Ticks);
            HopbyHopIdentifier = rand.Next(65535);
            return HopbyHopIdentifier;
        }

        /**
         * The End-to-End Identifier is an unsigned 32-bit integer field (in
         * network byte order) and is used to detect duplicate messages. Upon
         * reboot implementations MAY set the high order 12 bits to contain
         * the low order 12 bits of current time, and the low order 20 bits
         * to a random value. Senders of request messages MUST insert a
         * unique identifier on each message.
         * @return end to end identifier from header of message
         */
        public int GetEndToEndIdentifier()
        {
            Random rand = new Random((int)DateTime.Now.Ticks);

            EndtoEndIdentifier = rand.Next(65535);
            return EndtoEndIdentifier;
        }

        /**
         * @return Set of message Avps
         */


        public bool ErrorBit { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="avps"></param>
        /// <returns></returns>
        public string ToXML(Message msg)
        {
            string xmlToRet = "<"+DiameterMessageCode.GetMessageName(msg.CommandCode, msg.IsRequest)+">\r\n";

            try
            {
                foreach (Avp avp in msg.avps)
                {
                    if (avp.isGrouped)
                        xmlToRet += GetGroupedAvpXml(avp);
                    else
                    {
                        XElement element = new XElement("AVP",
                                                    new XAttribute("AvpCode", avp.AvpCode),
                                                    new XAttribute("AvpName", (avp.AvpName == null) ? "" : avp.AvpName),
                                                    new XAttribute("AvpValue", (avp.AvpValue == null) ? "" : avp.AvpValue));

                        xmlToRet += element.ToString() + "\r\n";
                    }
                }

                xmlToRet += "</" + DiameterMessageCode.GetMessageName(msg.CommandCode, msg.IsRequest) + ">\r\n";
                //xmlToRet = XElement.Parse(xmlToRet).ToString();
                //string avpValue = findAvpValueByName("Result-Code", xmlToRet).ToString();
                return xmlToRet;

                //var avpsXML = new XElement(DiameterMessageCode.GetMessageName(msg.CommandCode,msg.IsRequest),
                //            from avp in msg.avps
                //            select new XElement("AVP",
                //                           new XAttribute("AvpCode", avp.AvpCode),
                //                           new XAttribute("AvpName", (avp.AvpName ==null)? "" : avp.AvpName),
                //                           new XAttribute("AvpValue", (avp.AvpValue == null) ? "" : avp.AvpValue),
                //                           new XAttribute("isGrouped", avp.isGrouped)
                //                       ));

                //return avpsXML.ToString();

            }
            catch (Exception ex)
            {
                Common.StackLog.Write2ErrorLog("ConvertLis2XML", ex.ToString());
                return "";
            }


        }

        private string GetGroupedAvpXml(Avp avpGroup)
        {
            string xmlToRet = "";

            xmlToRet += "<AVP AvpCode=\"" + avpGroup.AvpCode + "\" AvpName=\"" + avpGroup.AvpName + "\">\r\n";
            foreach (Avp avp in avpGroup.groupedAvps)
            {
                if (avp.isGrouped)
                    xmlToRet += GetGroupedAvpXml(avp);
                else
                {
                    XElement element = new XElement("AVP",
                                                new XAttribute("AvpCode", avp.AvpCode),
                                                new XAttribute("AvpName", (avp.AvpName == null) ? "" : avp.AvpName),
                                                new XAttribute("AvpValue", (avp.AvpValue == null) ? "" : avp.AvpValue));

                    xmlToRet += element.ToString() + "\r\n";
                }
            }
            xmlToRet += "</AVP>\r\n";
            
            return xmlToRet;
        }

        private string findAvpValueByName(string avpName, string AvpsXML)
        {
            XDocument doc = XDocument.Parse(AvpsXML);
            foreach (XElement element in doc.Descendants())
            {
                if (!element.HasElements)
                {
                    if (element.Attribute("AvpName").Value.ToLower().Equals(avpName.ToLower()))
                        return element.Attribute("AvpValue").Value;
                }
            }
            
            return "";
        }
        
        private XElement findAvp(string avpName, string AvpsXML)
        {
            XDocument doc = XDocument.Parse(AvpsXML);
            foreach (XElement element in doc.Descendants())
            {
                if (!element.HasElements)
                {
                    if (element.Attribute("AvpName").Value.ToLower().Equals(avpName.ToLower()))
                        return element;
                }
            }
            return null;
        }

        private XElement findAvp(int avpCode, string AvpsXML)
        {
            XDocument doc = XDocument.Parse(AvpsXML);
            foreach (XElement element in doc.Descendants())
            {
                if (!element.HasElements)
                {
                    if (element.Attribute("AvpName").Value.ToLower().Equals(avpCode.ToString()))
                        return element;
                }
            }
            return null;
        }

        private Avp findAvpByPath(string avpPath, string XmlMsg)
        {
            XDocument doc = XDocument.Parse(XmlMsg);

            return null;
            
        }
    }



    /// <summary>
    /// 
    /// </summary>


}
