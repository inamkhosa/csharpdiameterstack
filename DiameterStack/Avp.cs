using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

/// <summary>
/// @Author Inam Khosa 
/// @Date: October, 2013
/// </summary>

namespace DiameterStack
{
    /// <summary>
    /// Request Type ENUMRATION
    /// </summary>
    public enum CC_Request_Type
    {
        INITIAL_REQUEST = 1,
        UPDATE_REQUEST = 2,
        TERMINATION_REQUEST = 3,
        EVENT_REQUEST = 4
    };
    /// <summary>
    /// 
    /// </summary>
    public enum Subscription_Id_Type
    {
        END_USER_E164 = 0,
        END_USER_IMSI = 1,
        END_USER_SIP_URI = 2,
        END_USER_NAI = 3

    }
    /// <summary>
    /// 
    /// </summary>
    public enum Requested_Action
    {
        DIRECT_DEBITING = 0,
        REFUND_ACCOUNT = 1,
        CHECK_BALANCE = 2,
        PRICE_ENQUIRY = 3
    }
    /// <summary>
    /// This Class Represents a Diameter Avp
    /// </summary>
    public class Avp
    {

        public int AvpLength;

        public byte AvpFlags;

        public int AvpCode;

        public string AvpName; 

        public object AvpValue;

        public int VendorId;

        public bool isGrouped;

        public bool isMandatory;

        public bool isEncrypted;

        public bool isVendorSpecific;

        public List<Avp> groupedAvps;

        // private int Max_AVP_LENGTH = 512;
        //public sbyte[] rawData = new sbyte[Max_AVP_LENGTH];

        /// <summary>
        /// 
        /// </summary>
        public Avp()
        {
            groupedAvps = new List<Avp>();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="AvpCode"></param>
        public Avp(int AvpCode)
        {
            this.AvpCode = AvpCode;
            this.AvpName = StackContext.GlobalAvpDictionary[AvpCode].AttributeName;
            groupedAvps = new List<Avp>();

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Code"></param>
        /// <param name="Value"></param>
        /// <param name="VendorId"></param>

        public Avp(int Code, object Value)
        {
            this.AvpCode = Code;
            this.AvpValue = Value;
            this.AvpName = StackContext.GlobalAvpDictionary[Code].AttributeName;
            groupedAvps = new List<Avp>();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="avp"></param>
        /// <returns></returns>

        public byte[] GetAvpBytes(Avp avp, Dictionary<int, AttributeInfo> dict)
        {
            int offset = 0;

            //Avp Value Bytes
            int Padding = 0;
            byte[] retValueBytes = GetAvpValueBytes(avp, ref Padding, dict);

            //If Grouped Avp then Above Method will return all Grouped AvpBytes, Take Care of Paddings Here or in Grouped Avp itself.
            if (avp.isGrouped)
                return retValueBytes;
            // 
            if (avp.isVendorSpecific)
            {
                avp.AvpLength = retValueBytes.Length + 12;
               
            }
            else
            {
                avp.AvpLength = retValueBytes.Length + 8;
               
            }
           

            //Initialize Return Array
            byte[] AvpBytesToRet = new byte[avp.AvpLength];

            //Add Header Bytes 

            //Avp Code
            int AvpCode = IPAddress.HostToNetworkOrder(avp.AvpCode);
            Buffer.BlockCopy(BitConverter.GetBytes(AvpCode), 0, AvpBytesToRet, offset, 4);
            offset += 4;

            //AvpFlags {V M P r r r r r}
            byte flags = (byte)(0 << 0);
            flags = (byte)(0 << 1);
            flags = (byte)(0 << 2);
            flags = (byte)(0 << 3);
            flags = (byte)(0 << 5);

            if (avp.isVendorSpecific)
                flags = (byte)(1 << 7);
            if (isMandatory)
                flags = (byte)(1 << 6);

            Buffer.BlockCopy(BitConverter.GetBytes(flags), 0, AvpBytesToRet, offset, 1);
            offset += 1;

            //AvpLength, Discard 0th Byte and Always excluding the Paddings from length 
            int Len = avp.AvpLength - Padding;
            int lenOfAvpToCopy = IPAddress.HostToNetworkOrder(Len);
            Buffer.BlockCopy(BitConverter.GetBytes(lenOfAvpToCopy), 1, AvpBytesToRet, offset, 3);
            offset += 3;

            //Vendor ID
            if (avp.isVendorSpecific)
            {
                UInt32 vId = (UInt32)IPAddress.HostToNetworkOrder(avp.VendorId);
                Buffer.BlockCopy(BitConverter.GetBytes(vId), 0, AvpBytesToRet, offset, 4);
                offset += 4;
            }

            //Add Value Byes
            Buffer.BlockCopy(retValueBytes, 0, AvpBytesToRet, offset, retValueBytes.Length);
            offset += retValueBytes.Length - Padding;


            return AvpBytesToRet;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] GetAvpValueBytes(Avp avp, ref int Padding, Dictionary<int, AttributeInfo> dict)
        {
            int AvpCode = avp.AvpCode;

            object Value = avp.AvpValue;

            if (!dict.ContainsKey(AvpCode))
            {
                Common.StackLog.Write2ErrorLog("GetValueBytes", "Lookup For Avp [" + AvpCode + "] ");
                Exception exp = new Exception("Avp Code:" + AvpCode.ToString() + " not found in dictionary");
                Common.StackLog.Write2ErrorLog("GetValueBytes", "Lookup For Avp [" + AvpCode + "] ");
                throw exp;
            }
            
            AttributeInfo avpInfo = dict[AvpCode];

           // Console.WriteLine("Success"); 

            switch (avpInfo.DataType)
            {

                case "Address":
                    {

                        short addressFamily = 1;
                        addressFamily = IPAddress.HostToNetworkOrder(addressFamily);
                        byte[] addressFamilyBytes = BitConverter.GetBytes(addressFamily);
                        byte[] addressBytes = IPAddress.Parse(Value.ToString()).GetAddressBytes();

                        //Calculate Padding
                        int remainder = (addressFamilyBytes.Length + addressBytes.Length) % 4;
                        
                        if (remainder == 0)
                            Padding = 0;
                        else
                            Padding = 4 - remainder;

                        byte[] retVal = new byte[addressFamilyBytes.Length + addressBytes.Length + Padding];

                        //Copy Value Bytes
                        System.Buffer.BlockCopy(addressFamilyBytes, 0, retVal, 0, addressFamilyBytes.Length);

                        System.Buffer.BlockCopy(addressBytes, 0, retVal, 2, addressBytes.Length);

                        //Add Padding Bytes
                        if (Padding > 0)
                        {
                            int destOffSet = retVal.Length - Padding;
                            System.Buffer.BlockCopy(Enumerable.Repeat((byte)0x0, Padding).ToArray(), 0, retVal, destOffSet, Padding);
                        }

                        return retVal;

                    }

                case "OctetString":
                    {
                        System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();

                        byte[] OctetStringBytes = encoding.GetBytes(Value.ToString());
   
                        //Calculate Padding
                        int remainder = (OctetStringBytes.Length) % 4;
                        
                        if (remainder == 0)

                            Padding = 0;
                        else
                            Padding = 4 - remainder;

                        byte[] retVal = new byte[OctetStringBytes.Length + Padding];

                         //Copy Value Bytes
                        System.Buffer.BlockCopy(OctetStringBytes, 0, retVal, 0, OctetStringBytes.Length);

                       
                       
                        //Add Padding Bytes
                        if (Padding > 0)
                        {
                            int destOffSet = retVal.Length - Padding;
                            System.Buffer.BlockCopy(Enumerable.Repeat((byte)0x0, Padding).ToArray(), 0, retVal, destOffSet, Padding);
                        }

                       
                       
                        return retVal;

                    }

                case "DiamIdent":
                    {
                        byte[] identityBytes = System.Text.Encoding.UTF8.GetBytes(Value.ToString());

                        //Calculate Padding
                        int remainder = identityBytes.Length % 4;
                        if (remainder == 0)
                            Padding = 0;
                        else
                            Padding = 4 - remainder;

                        byte[] retVal = new byte[identityBytes.Length + Padding];


                        //Copy Value Bytes
                        System.Buffer.BlockCopy(identityBytes, 0, retVal, 0, identityBytes.Length);

                        //Add Padding Bytes
                        if (Padding > 0)
                        {
                            int destOffSet = retVal.Length - Padding;
                            System.Buffer.BlockCopy(Enumerable.Repeat((byte)0x0, Padding).ToArray(), 0, retVal, destOffSet, Padding);
                        }

                        return retVal;

                    }

                
                case "Unsigned32":
                    {
                        byte[] UINTBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)Value));

                        //Calculate Padding
                        int remainder = UINTBytes.Length % 4;

                        if (remainder == 0)
                            Padding = 0;
                        else
                            Padding = 4 - remainder;

                        byte[] retVal = new byte[UINTBytes.Length + Padding];


                        //Copy Value Bytes
                        System.Buffer.BlockCopy(UINTBytes, 0, retVal, 0, UINTBytes.Length);

                        //Add Padding Bytes
                        if (Padding > 0)
                        {
                            int destOffSet = retVal.Length - Padding;
                            System.Buffer.BlockCopy(Enumerable.Repeat((byte)0x0, Padding).ToArray(), 0, retVal, destOffSet, Padding);
                        }

                        return retVal;

                    }
                case "Integer32":
                    {
                        byte[] UINTBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)Value));

                        //Calculate Padding
                        int remainder = UINTBytes.Length % 4;

                        if (remainder == 0)
                            Padding = 0;
                        else
                            Padding = 4 - remainder;

                        byte[] retVal = new byte[UINTBytes.Length + Padding];


                        //Copy Value Bytes
                        System.Buffer.BlockCopy(UINTBytes, 0, retVal, 0, UINTBytes.Length);

                        //Add Padding Bytes
                        if (Padding > 0)
                        {
                            int destOffSet = retVal.Length - Padding;
                            System.Buffer.BlockCopy(Enumerable.Repeat((byte)0x0, Padding).ToArray(), 0, retVal, destOffSet, Padding);
                        }

                        return retVal;

                    }
                case "Integer8":
                    {
                         long avpVal = Convert.ToInt64(Value);

                        byte[] U64Bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(avpVal));

                        //Calculate Padding
                        int remainder = U64Bytes.Length % 4;

                        if (remainder == 0)
                            Padding = 0;
                        else
                            Padding = 4 - remainder;

                        byte[] retVal = new byte[U64Bytes.Length + Padding];


                        //Copy Value Bytes
                        System.Buffer.BlockCopy(U64Bytes, 0, retVal, 0, U64Bytes.Length);

                        //Add Padding Bytes
                        if (Padding > 0)
                        {
                            int destOffSet = retVal.Length - Padding;
                            System.Buffer.BlockCopy(Enumerable.Repeat((byte)0x0, Padding).ToArray(), 0, retVal, destOffSet, Padding);
                        }

                        return retVal;
                    }
                case "Unsigned64":
                    {
                        long avpVal = Convert.ToInt64(Value);

                        byte[] U64Bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(avpVal));

                        //Calculate Padding
                        int remainder = U64Bytes.Length % 4;

                        if (remainder == 0)
                            Padding = 0;
                        else
                            Padding = 4 - remainder;

                        byte[] retVal = new byte[U64Bytes.Length + Padding];


                        //Copy Value Bytes
                        System.Buffer.BlockCopy(U64Bytes, 0, retVal, 0, U64Bytes.Length);

                        //Add Padding Bytes
                        if (Padding > 0)
                        {
                            int destOffSet = retVal.Length - Padding;
                            System.Buffer.BlockCopy(Enumerable.Repeat((byte)0x0, Padding).ToArray(), 0, retVal, destOffSet, Padding);
                        }

                        return retVal;
                       
                    }
                case "Integer64":
                    {
                        long avpVal = Convert.ToInt64(Value);

                        byte[] U64Bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(avpVal));

                        //Calculate Padding
                        int remainder = U64Bytes.Length % 4;

                        if (remainder == 0)
                            Padding = 0;
                        else
                            Padding = 4 - remainder;

                        byte[] retVal = new byte[U64Bytes.Length + Padding];


                        //Copy Value Bytes
                        System.Buffer.BlockCopy(U64Bytes, 0, retVal, 0, U64Bytes.Length);

                        //Add Padding Bytes
                        if (Padding > 0)
                        {
                            int destOffSet = retVal.Length - Padding;
                            System.Buffer.BlockCopy(Enumerable.Repeat((byte)0x0, Padding).ToArray(), 0, retVal, destOffSet, Padding);
                        }

                        return retVal;

                    }
                case "UTF8String":
                    {
                        byte[] UTF8Bytes = Encoding.UTF8.GetBytes(Value.ToString());
                        //Calculate Padding
                        int remainder = (UTF8Bytes.Length) % 4;

                        if (remainder == 0)
                            Padding = 0;
                        else
                            Padding = 4 - remainder;

                        byte[] retVal = new byte[UTF8Bytes.Length + Padding];

                        //Copy Value Bytes
                        System.Buffer.BlockCopy(UTF8Bytes, 0, retVal, 0, UTF8Bytes.Length);

                        //Add Padding Bytes
                        if (Padding > 0)
                        {
                            int destOffSet = retVal.Length - Padding;
                            System.Buffer.BlockCopy(Enumerable.Repeat((byte)0x0, Padding).ToArray(), 0, retVal, destOffSet, Padding);
                        }

                        return retVal;
                    }

                case "Enumerated":
                    {
                        byte[] enumratedBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)Value));

                        //Calculate Padding
                        int remainder = enumratedBytes.Length % 4;

                        if (remainder == 0)
                            Padding = 0;
                        else
                            Padding = 4 - remainder;

                        byte[] retVal = new byte[enumratedBytes.Length + Padding];


                        //Copy Value Bytes
                        System.Buffer.BlockCopy(enumratedBytes, 0, retVal, 0, enumratedBytes.Length);

                        //Add Padding Bytes
                        if (Padding > 0)
                        {
                            int destOffSet = retVal.Length - Padding;
                            System.Buffer.BlockCopy(Enumerable.Repeat((byte)0x0, Padding).ToArray(), 0, retVal, destOffSet, Padding);
                        }

                        return retVal;

                    }

                case "Time":
                    {
                      
                        ulong seconds = Convert.ToUInt64(Value);

                        //byte[] ntpTimeStamp = DiameterType.NtpTimestamp2Bytes(seconds) ;
                        byte[] ntpTimeStamp = DiameterType.ConvertToNtp(seconds); 

                        //Calculate Padding
                        int remainder = (ntpTimeStamp.Length) % 4;

                        if (remainder == 0)
                            Padding = 0;
                        else
                            Padding = 4 - remainder;

                        byte[] retVal = new byte[ntpTimeStamp.Length + Padding];

                        //Copy Value Bytes
                        System.Buffer.BlockCopy(ntpTimeStamp, 0, retVal, 0, ntpTimeStamp.Length);

                       

                        //Add Padding Bytes
                        if (Padding > 0)
                        {
                            int destOffSet = retVal.Length - Padding;
                            System.Buffer.BlockCopy(Enumerable.Repeat((byte)0x0, Padding).ToArray(), 0, retVal, destOffSet, Padding);
                        }

                        return retVal;
                    }

                case "Grouped":
                    {
                        // Process Group AVP here
                        byte[] retVal = GetGroupedAvpBytes(avp, dict);

                        return retVal;
                    }
                default:
                    {
                        return null;
                    }

            }

            /// Diameter Types
            //•	OctetString
            //•	Integer32
            //•	Integer64
            //•	Unsigned32
            //•	Unsigned64
            //•	Float32
            //•	Float64
            //•	Grouped
            //•	Address
            //•	Time
            //•	UTF8String
            //•	DiameterIdentity
            //•	DiameterURI
            //•	Enumerated
            //•	IPFilterRule
            //•	QoSFilterRule

        }
        /// <summary>
        /// Returns Grouped Avp Bytes Including Nested Grouped Avps
        /// </summary>
        /// <param name="AvpCode"></param>
        /// <returns></returns>
        private byte[] GetGroupedAvpBytes(Avp gAvp, Dictionary<int, AttributeInfo> dict)
        {
            //Dest Array Offset
            int destOffset = 0;

            int AvpCode = gAvp.AvpCode;
            
            //We may change the Length to Dynamic Length of Avp, If we can figure out to calculate it before processing
            byte[] groupedAvpBytes = new byte[1024];

            //Copy Grouped Avp Code to GroupedBytes
            int gAvpCode = IPAddress.HostToNetworkOrder(AvpCode);

            Buffer.BlockCopy(BitConverter.GetBytes(gAvpCode), 0, groupedAvpBytes, 0, 4);

            destOffset += 4;

            //Set Flags for the the Avp AvpFlags {V M P r r r r r}
            byte gAvpFlags = (byte)(0 << 0);
            
            gAvpFlags = (byte)(0 << 1);
            
            gAvpFlags = (byte)(0 << 2);
           
            gAvpFlags = (byte)(0 << 3);
           
            gAvpFlags = (byte)(0 << 5);
           
            if (gAvp.isVendorSpecific)
                gAvpFlags = (byte)(1 << 7);
            if (isMandatory)
                gAvpFlags = (byte)(1 << 6);

            Buffer.BlockCopy(BitConverter.GetBytes(gAvpFlags), 0, groupedAvpBytes, destOffset, 1);
            
            destOffset += 1;

            //Leaving 3 Bytes Place Holder for Copying Actual Length of Grouped Avp at the end of method because we can know this length at the end of copying all Avps
            destOffset += 3;

            if (gAvp.isVendorSpecific)
            {
                UInt32 vId = (UInt32)IPAddress.HostToNetworkOrder(gAvp.VendorId);
                Buffer.BlockCopy(BitConverter.GetBytes(vId), 0, groupedAvpBytes, destOffset, 4);
                destOffset += 4;
            }

            //Now Copy All Avps inside Grouped Avp
            foreach (Avp avp in gAvp.groupedAvps)
            {
                byte[] totalAvpBytes = GetAvpBytes(avp, dict);
               
                Buffer.BlockCopy(totalAvpBytes, 0, groupedAvpBytes, destOffset, totalAvpBytes.Length);

                destOffset += totalAvpBytes.Length;
            }

            //Set the Length of Grouped Avp
            UInt32 LenOfGroupedAvp = (UInt32)IPAddress.HostToNetworkOrder(destOffset);
            
            Buffer.BlockCopy(BitConverter.GetBytes(LenOfGroupedAvp), 1, groupedAvpBytes, 5, 3);

            byte[] retVal = new byte[destOffset];

            Buffer.BlockCopy(groupedAvpBytes, 0, retVal, 0, destOffset);

             return retVal;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="avp"></param>
        /// <param name="dict"></param>
        /// <returns></returns>
        private byte[] ProcessNestedGroupedAvp(Avp avp, Dictionary<int, AttributeInfo> dict)
        {
            //Process the Further nested Avps `````````````here. Untested Code

            //Copy Nested Avp Code to GroupedBytes
            int AvpCode = avp.AvpCode;
            //Change the Length to Dynamic Length of Avp
            byte[] nestedAvpBytes = new byte[1024];

            int destOffset = 0;

            //Copy Grouped Avp Code to GroupedBytes
            int gAvpCode = IPAddress.HostToNetworkOrder(AvpCode);

            Buffer.BlockCopy(BitConverter.GetBytes(gAvpCode), 0, nestedAvpBytes, 0, 4);

            destOffset += 4;

            //AvpFlags {V M P r r r r r}
            byte nestedAvpFlags = (byte)(0 << 0);
            nestedAvpFlags = (byte)(0 << 1);
            nestedAvpFlags = (byte)(0 << 2);
            nestedAvpFlags = (byte)(0 << 3);
            nestedAvpFlags = (byte)(0 << 5);
            if (avp.isVendorSpecific)
                nestedAvpFlags = (byte)(0 << 7);
            if (isMandatory)
                nestedAvpFlags = (byte)(1 << 6);

            Buffer.BlockCopy(BitConverter.GetBytes(nestedAvpFlags), 0, nestedAvpBytes, destOffset, 1);
            destOffset += 1;

            //Leaving 3 Bytes Place Holder for Copying Actual Length of Grouped Avp at the end of method because we can know this length at the end of copying all Avps
            destOffset += 3;

            //Avp Value Bytes
            int Padding = 0;

            byte[] retValueBytes = GetAvpValueBytes(avp, ref Padding, dict);

            //Grouped Avp Length 
            int lenOfNestedAvp = IPAddress.HostToNetworkOrder(destOffset);

            Buffer.BlockCopy(BitConverter.GetBytes(lenOfNestedAvp), 1, nestedAvpBytes, 5, 3);

            byte[] retVal = new byte[destOffset];

            Buffer.BlockCopy(nestedAvpBytes, 0, retVal, 0, destOffset);

            return retVal;

        }

    }
}