using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.Silverlight.PolicyServers
{
    // This class programmatically represents a multicast resource to which a Silverlight application
    // may be granted access: a multicast group address which the application is allowed to join,
    // and a range of ports on which the application may receive traffic from the group.
    public class MulticastResource
    {
        private IPAddress groupAddress;
        private int lowPort;
        private int highPort;

        public MulticastResource(IPAddress groupAddress, int lowPort, int highPort)
        {
            if (groupAddress == null)
            {
                throw new ArgumentNullException("groupAddress");
            }

            if (lowPort < 0 || lowPort > UInt16.MaxValue)
            {
                throw new ArgumentOutOfRangeException("lowPort");
            }

            if (highPort < 0 || highPort > UInt16.MaxValue)
            {
                throw new ArgumentOutOfRangeException("highPort");
            }

            this.groupAddress = groupAddress;
            this.lowPort = lowPort;
            this.highPort = highPort;
        }

        public MulticastResource(IPAddress groupAddress, int port) 
            : this(groupAddress, port, port) 
        {
        }

        public IPAddress GroupAddress
        {
            get { return groupAddress; }
        }

        public int LowPort
        {
            get { return lowPort; }
        }

        public int HighPort
        {
            get { return highPort; }
        }

        public override bool Equals(object obj)
        {
            MulticastResource mr = obj as MulticastResource;
            if (mr == null)
            {
                return false;
            }

            return (IPAddress.Equals(mr.groupAddress, groupAddress) &&
                    (mr.lowPort == lowPort) &&
                    (mr.highPort == highPort));
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            if (groupAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                builder.Append("[");
                builder.Append(groupAddress);
                builder.Append("]:");
            }
            else
            {
                builder.Append(groupAddress.ToString());
                builder.Append(":");
            }

            if (lowPort == highPort)
            {
                builder.Append(lowPort);
            }
            else
            {
                builder.Append(lowPort);
                builder.Append("-");
                builder.Append(highPort);
            }

            return builder.ToString();
        }

        public override int GetHashCode()
        {
            return ((lowPort & (highPort << 16)) ^ groupAddress.GetHashCode());
        }
    }
}
