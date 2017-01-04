using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DiameterStack
{

    /// <summary>
    /// @Author Inam Khosa 
    /// @Date: October, 2013
    /// </summary>


    public class DiameterType
    {


        /// <summary>
        /// This is seconds shift (70 years in seconds) applied to date, 
        /// since NTP date starts since 1900, not 1970.
        /// </summary>
        private const long SECOND_SHIFT = 2208988800L;

        private const int INT_INET4 = 1;
        private const int INT_INET6 = 2;

        private const int INT32_SIZE = 4;
        private const int INT64_SIZE = 8;
        private const int FLOAT32_SIZE = 4;
        private const int FLOAT64_SIZE = 8;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public static int bytesToInt(sbyte[] rawData)
        {

            return 0;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public static long bytesToLong(sbyte[] rawData)
        {
            return 0;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public static float bytesToFloat(sbyte[] rawData)
        {
            return 0;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public static double bytesToDouble(sbyte[] rawData)
        {
            return 0;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public static string bytesToOctetString(sbyte[] rawData)
        {
            return "";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public static string bytesToUtf8String(sbyte[] rawData)
        {
            return "";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ntpTime"></param>
        /// <returns></returns>

        public static UInt32 BytesToSeconds(byte[] ntpTime)
        {
            decimal intpart = 0; //, fractpart = 0;

            for (var i = 0; i <= 3; i++)
                intpart = 256 * intpart + ntpTime[i];
            
            //for (var i = 4; i <= 7; i++)
            //    fractpart = 256 * fractpart + ntpTime[i];

            UInt32 milliseconds = (uint)intpart * 1000;// +((fractpart * 1000) / 0x100000000L);


            return milliseconds;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="date"></param>
        
        public static byte[] ConvertToNtp(decimal milliseconds)
        {
            decimal intpart = 0, fractpart = 0;
            var ntpData = new byte[4];

            intpart = milliseconds / 1000;
            fractpart = ((milliseconds % 1000) * 0x100000000L) / 1000m;

       
            var temp = intpart;
            for (var i = 3; i >= 0; i--)
            {
                ntpData[i] = (byte)(temp % 256);
                temp = temp / 256;
            }

            //temp = fractpart;
            //for (var i = 7; i >= 4; i--)
            //{
            //    ntpData[i] = (byte)(temp % 256);
            //    temp = temp / 256;
            //}
            return ntpData;
        }

        public static IPAddress bytesToAddress(sbyte[] rawData)
        {
            byte[] buff = new byte[rawData.Length];
            System.Buffer.BlockCopy(rawData, 0, buff, 0, rawData.Length);
            IPAddress ipToReturn = new IPAddress(buff);
            return ipToReturn;
        }

        public static sbyte[] int32ToBytes(int value)
        {
            sbyte[] bytes = new sbyte[INT32_SIZE];

            return bytes;
        }

        public static sbyte[] intU32ToBytes(long value)
        {
            // FIXME: this needs to reworked!
            sbyte[] bytes = new sbyte[INT32_SIZE];

            return bytes;
        }

        public static sbyte[] int64ToBytes(long value)
        {
            sbyte[] sbytes = new sbyte[INT64_SIZE];



            return sbytes;
        }

        public static sbyte[] float32ToBytes(float value)
        {
            sbyte[] bytes = new sbyte[FLOAT32_SIZE];

            return bytes;
        }

        public static sbyte[] float64ToBytes(double value)
        {
            sbyte[] bytes = new sbyte[FLOAT64_SIZE];

            return bytes;
        }

        public static sbyte[] octetStringToBytes(string value)
        {

            sbyte[] bytes = { 0 };

            return bytes;

        }

        public static sbyte[] utf8StringToBytes(string value)
        {
            sbyte[] bytes = { 0 };

            return bytes;
        }

        public static sbyte[] addressToBytes(IPAddress address)
        {


            sbyte[] data = new sbyte[2];


            return data;
        }




        public static sbyte[] objectToBytes(object data)
        {
            return null;
        }

        public static List<Avp> decodeAvpSet(sbyte[] buffer)
        {
            return decodeAvpSet(buffer, 0);
        }

        /// 
        /// <param name="buffer"> </param>
        /// <param name="shift"> - shift in buffer, for instance for whole message it will have non zero value
        /// @return </param>
        /// <exception cref="IOException"> </exception>
        /// <exception cref="AvpDataException"> </exception>
        public static List<Avp> decodeAvpSet(sbyte[] buffer, int shift)
        {
            return null;

        }

        public static sbyte[] encodeAvpSet(List<Avp> avps)
        {
            sbyte[] bytes = new sbyte[0];
            return bytes;
        }

        public static sbyte[] encodeAvp(Avp avp)
        {
            sbyte[] bytes = new sbyte[0];
            return bytes;
        }
    }
}
