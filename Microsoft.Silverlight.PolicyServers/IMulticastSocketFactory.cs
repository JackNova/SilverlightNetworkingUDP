using System;
using System.Net.Sockets;

namespace Microsoft.Silverlight.PolicyServers
{
    // This abstracts away the process of creating a Socket, so we can fake out the Socket
    // class for testing purposes.
    internal interface IMulticastSocketFactory
    {
        IMulticastSocket Create(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType);
    }
}
