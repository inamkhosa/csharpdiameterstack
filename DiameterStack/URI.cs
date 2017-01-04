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
    using System;
    using System.Text;

    [Serializable]
    public sealed class URI : IComparable
    {

        private const long serialVersionUID = 1L;

        private const string FIELD_PROTOCOL = "protocol=";
        
        private const string FIELD_TRANSPORT = "transport=";
        
        private const string SCHEME_SEPARATOR = "://";
        
        private const string PARAMS_SEPARATOR = ";";

        private const string DEFAULT_SCHEME = "aaa";
        
        private const int DEFAULT_PORT = 3868;

        private string scheme;
        
        private string host;
        
        private int port = -1;
        
        private string path = "";

        /// <summary>
        /// Constructor with string parameter </summary>
        /// <param name="uri"> String representation of URI </param>
        /// <exception cref="URISyntaxException"> which signals that URI has syntax error </exception>
        /// <exception cref="UnknownServiceException"> which signals that URI has incorrect scheme </exception>
        public URI(string uri)
        {
            parse(uri);
            
            if (FQDN == null || FQDN.Trim().Length == 0)
            {
                throw new Exception("Host not found");
            }
            if (!Scheme.Equals("aaa") && !Scheme.Equals("aaas"))
            {
                throw new Exception((new StringBuilder()).Append("Unsupported service: ").Append(Scheme).ToString());
            }
        }

        /// <returns> scheme for URI </returns>
        public string Scheme
        {
            get
            {
                return scheme;
            }
        }

        /// <returns> host name of URI </returns>
        public string FQDN
        {
            get
            {
                return host;
            }
        }

        /// <summary>
        /// Returns the port number of this URI, or -1 if this is not set. </summary>
        /// <returns> the port number of this URI </returns>
        public int Port
        {
            get
            {
                return port == -1 ? DEFAULT_PORT : port;
            }
        }

        /// <returns>  true if this URI is secure </returns>
        public bool Secure
        {
            get
            {
                return Scheme.EndsWith("s");
            }
        }

        /// <returns> path of this URI </returns>
        public string Path
        {
            get
            {
                return path;
            }
        }

        /// <returns> protocol parameter of this URI </returns>
        public string ProtocolParam
        {
            get
            {
                string[] args = Split(Path, PARAMS_SEPARATOR, true);
                foreach (string arg in args)
                {
                    if (arg.StartsWith(FIELD_PROTOCOL))
                    {
                        return arg.Substring(FIELD_PROTOCOL.Length);
                    }
                }

                return null;
            }
        }

        private string[] Split(string Path, string PARAMS_SEPARATOR, bool p)
        {
            throw new NotImplementedException();
        }

        /// <returns> transport parameter of this URI </returns>
        public string TransportParam
        {
            get
            {
                string[] args = Split(Path, PARAMS_SEPARATOR, true);
                foreach (string arg in args)
                {
                    if (arg.StartsWith(FIELD_TRANSPORT))
                    {
                        return arg.Substring(FIELD_TRANSPORT.Length);
                    }
                }

                return null;
            }
        }


        /// <returns> String representation of this URI in RFC 3588 format </returns>
        public override string ToString()
        {
            StringBuilder rc = (new StringBuilder(scheme)).Append(SCHEME_SEPARATOR).Append(host);
            if (port != -1)
            {
                rc.Append(":").Append(port);
            }
            if (path != null && path.Length > 0)
            {
                rc.Append(PARAMS_SEPARATOR).Append(path);
            }

            return rc.ToString();
        }
        /// <summary>
        /// parse
        /// </summary>
        /// <param name="uri"></param>
        private void parse(string uri)
        {
            try
            {
                int schemeStartIndex = uri.IndexOf(SCHEME_SEPARATOR);
                int schemeEndIndex = 0;
                if (schemeStartIndex == -1)
                {
                    scheme = DEFAULT_SCHEME;
                }
                else
                {
                    scheme = uri.Substring(0, schemeStartIndex);
                    schemeEndIndex = schemeStartIndex + 3;
                    schemeStartIndex = uri.IndexOf(';', schemeEndIndex);
                }

                if (schemeStartIndex == -1)
                {
                    host = uri.Substring(schemeEndIndex);
                }
                else
                {
                    host = uri.Substring(schemeEndIndex, schemeStartIndex - schemeEndIndex);
                }
                int sepIndex = host.IndexOf(':');
                if (sepIndex != -1)
                {
                    port = Convert.ToInt32(host.Substring(sepIndex + 1));
                    host = host.Substring(0, sepIndex);
                }
                if (schemeStartIndex != -1)
                {
                    path = uri.Substring(schemeStartIndex + 1);
                }
            }
            catch (Exception exp)
            {
                throw new Exception( "URI has incorrect format " + exp.ToString());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj == null || this.GetType() != obj.GetType())
            {
                return false;
            }

            URI that = (URI)obj;

            return Port == that.Port && !(host != null ? !host.Equals(that.host) : that.host != null) && !(path != null ? !path.Equals(that.path) : that.path != null) && !(scheme != null ? !scheme.Equals(that.scheme) : that.scheme != null);

        }

        public override int GetHashCode()
        {
            int result;
            result = (scheme != null ? scheme.GetHashCode() : 0);
            result = 31 * result + (host != null ? host.GetHashCode() : 0);
            result = 31 * result + Port;
            result = 31 * result + (path != null ? path.GetHashCode() : 0);
            return result;
        }

       
        public int CompareTo(object obj)
        {
            if (obj is URI)
            {
                return this.ToString().CompareTo(obj.ToString());
            }
            else
            {
                return -1;
            }
        }
    }
}
