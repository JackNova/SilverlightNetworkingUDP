using System;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.Silverlight.PolicyServers
{
    // This IMulticastSocket calls through to the corresponding methods on the actual Socket class.
    internal class RealMulticastSocket : IMulticastSocket
    {
        private Socket socket;

        public RealMulticastSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            socket = new Socket(addressFamily, socketType, protocolType);
        }

        public void SetSocketOption(SocketOptionLevel level, SocketOptionName name, bool value)
        {
            socket.SetSocketOption(level, name, value);
        }

        public void SetSocketOption(SocketOptionLevel level, SocketOptionName name, object value)
        {
            socket.SetSocketOption(level, name, value);
        }

        public void Bind(EndPoint endPoint)
        {
            socket.Bind(endPoint);
        }

        public IAsyncResult BeginReceiveMessageFrom(byte[] buffer, int offset, int size, SocketFlags flags, ref EndPoint remoteEP, AsyncCallback callback, object state)
        {
            return socket.BeginReceiveMessageFrom(buffer, offset, size, flags, ref remoteEP, callback, state);
        }

        public IAsyncResult BeginSendTo(byte[] buffer, int offset, int size, SocketFlags flags, EndPoint remoteEP, AsyncCallback callback, object state)
        {
            return socket.BeginSendTo(buffer, offset, size, flags, remoteEP, callback, state);
        }

        public int EndReceiveMessageFrom(IAsyncResult result, ref SocketFlags socketFlags, ref EndPoint endPoint, out IPPacketInformation ipPacketInformation)
        {
            return socket.EndReceiveMessageFrom(result, ref socketFlags, ref endPoint, out ipPacketInformation);
        }

        public int EndSendTo(IAsyncResult result)
        {
            return socket.EndSendTo(result);
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                socket.Close();
            }
        }
    }
}
