using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiameterStack
{
    /// <summary>
    /// @Author Inam Khosa 
    /// @Date: October, 2013
    /// </summary>
    /// 
    public class ApplicationId
    {

        private static long serialVersionUID = 1L;

        /**
         * Undefined value of id for application identifier
         */
        public static long UNDEFINED_VALUE = 0x0;

        /**
         * Standards-track application IDs are by Designated Expert with Specification Required [IANA]
         */
        public class Standard
        {

            public  static long DIAMETER_COMMON_MESSAGE = 0x0;
            public  static long NASREQ = 0x1;
            public  static long MOBILE_IP = 0x2;
            public  static long DIAMETER_BASE_ACCOUNTING = 0x3;
            public  static long RELAY = 0xffffffff;

        }

        /**
         * IANA [IANA] has assigned the range 0x00000001 to 0x00ffffff for
         * standards-track applications; and 0x01000000 - 0xfffffffe for vendor
         * specific applications, on a first-come, first-served basis.  The
         * following values are allocated.
         */
        public class Ranges
        {

            static long STANDARDS_TRACK_APPLICATIONS_MIN = 0x00000001;
            static long STANDARDS_TRACK_APPLICATIONS_MAX = 0x00ffffff;

            static long VENDOR_SPECIFIC_APPLICATIONS_MIN = 0x01000000;
            static long VENDOR_SPECIFIC_APPLICATIONS_MAX = 0xfffffffe;
        }


        private long venId = UNDEFINED_VALUE;
        private long authId = UNDEFINED_VALUE;
        private long acctId = UNDEFINED_VALUE;


        /**
         * Create instance of ApplicationId use Authentication-App-Id
         * @param authAppId authentication application id
         * @return instance of class
         */
        public static ApplicationId createByAuthAppId(long authAppId)
        {
            return new ApplicationId(UNDEFINED_VALUE, authAppId, UNDEFINED_VALUE);
        }

        /**
         * Create instance of ApplicationId use Accounting-Applicaion-Id
         * @param acchAppId accounting applicaion Id
         * @return instance of class
         */
        public static ApplicationId createByAccAppId(long acchAppId)
        {
            return new ApplicationId(UNDEFINED_VALUE, UNDEFINED_VALUE, acchAppId);
        }

        /**
         * Create instance of ApplicationId use Authentication-App-Id and Vendor-Id
         * @param vendorId  vendor specific id
         * @param authAppId authentication application id
         * @return instance of class
         */
        public static ApplicationId createByAuthAppId(long vendorId, long authAppId)
        {
            return new ApplicationId(vendorId, authAppId, UNDEFINED_VALUE);
        }

        /**
         * Create instance of ApplicationId use Accounting-Applicaion-Id and Vendor-Id
         * @param vendorId vendor specific id
         * @param acchAppId accounting applicaion Id
         * @return instance of class
         */
        public static ApplicationId createByAccAppId(long vendorId, long acchAppId)
        {
            return new ApplicationId(vendorId, UNDEFINED_VALUE, acchAppId);
        }

        /**
         * Create instance
         * @param vendorId vendor specific id
         * @param authAppId authentication application id
         * @param acctAppId accounting applicaion Id
         */
        private ApplicationId(long vendorId, long authAppId, long acctAppId)
        {
            this.authId = authAppId;
            this.acctId = acctAppId;
            this.venId = vendorId;
        }

        /**
         * @return Vendor-Isd
         */
        public long getVendorId()
        {
            return venId;
        }

        /**
         * @return Authentication-Application-Id
         */
        public long getAuthAppId()
        {
            return authId;
        }

        /**
         * @return Accounting-Application-Id
         */

        public long getAcctAppId()
        {
            return acctId;
        }

        /**
         * @param obj check object
         * @return true if check object equals current instance (all appId is equals)
         */

        public bool equals(Object obj)
        
        {
           
            if ( obj.GetType() == typeof(ApplicationId))
            
            {
                  ApplicationId appId = (ApplicationId) obj;
                  return authId  == appId.authId &&
                      acctId == appId.acctId &&
                      venId  == appId.venId;
                } else {
                  return false;
            }
        }

        /**
         * @return hash code of object
         */
        public int hashCode() 
        {
            int result;
            result = (int) (venId ^ (venId >> 32));
            result = 31 * result + (int) (authId ^ (authId >> 32));
            result = 31 * result + (int) (acctId ^ (acctId >> 32));
            return result;
        }

        /**
         * @return String representation of object
         */
        public String toString()
        {
            return new StringBuilder("AppId [").Append("Vendor-Id:").Append(venId).
                Append("; Auth-Application-Id:").Append(authId).
                Append("; Acct-Application-Id:").Append(acctId).
                Append("]").
                ToString();
        }
    }
}
