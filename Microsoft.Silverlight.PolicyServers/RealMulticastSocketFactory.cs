using System;
using System.Net.Sockets;

namespace Microsoft.Silverlight.PolicyServers
{
    // This IMulticastSocketFactory creates IMulticastSockets that really talk to the network.
    internal class RealMulticastSocketFactory : IMulticastSocketFactory
    {
        public RealMulticastSocketFactory() { }

        public IMulticastSocket Create(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            return new RealMulticastSocket(addressFamily, socketType, protocolType);
        }
    }
}
