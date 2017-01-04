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

    public interface Realm
    {

        /**
         * Return name of this realm
         * 
         * @return name
         */
         String GetName();

        /**
         * Return applicationId associated with this realm
         * 
         * @return applicationId
         */
         ApplicationId GetApplicationId();

        /**
         * Return realm local action for this realm
         * 
         * @return realm local action
         */
         LocalAction GetLocalAction();

        /**
         * Return true if this realm is dynamic updated
         * 
         * @return true if this realm is dynamic updated
         */
         bool isDynamic();

        /**
         * Return expiration time for this realm in milisec
         * 
         * @return expiration time
         */
         long GetExpirationTime();

        /**
         * Returns true if realm is local. Local means that it is defined as local(not action) realm for this peer.
         * @return
         */
         bool isLocal();

    }

    public enum LocalAction
    {

        LOCAL, RELAY, PROXY, REDIRECT

    }


}
