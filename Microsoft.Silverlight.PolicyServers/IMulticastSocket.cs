using System;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.Silverlight.PolicyServers
{
    // This abstracts away the Sockets methods we use so we can fake out the Socket
    // class for testing purposes.
    internal interface IMulticastSocket : IDisposable
    {
        void SetSocketOption(SocketOptionLevel level, SocketOptionName name, bool value);
        void SetSocketOption(SocketOptionLevel level, SocketOptionName name, object value);

        void Bind(EndPoint endPoint);

        IAsyncResult BeginReceiveMessageFrom(byte[] buffer, int offset, int count, SocketFlags flags, ref EndPoint remoteEP, AsyncCallback callback, object state);
        IAsyncResult BeginSendTo(byte[] buffer, int offset, int count, SocketFlags flags, EndPoint remoteEP, AsyncCallback callback, object state);

        int EndReceiveMessageFrom(IAsyncResult result, ref SocketFlags socketFlags, ref EndPoint endPoint, out IPPacketInformation ipPacketInformation);
        int EndSendTo(IAsyncResult result);

        void Close();
    }
}
