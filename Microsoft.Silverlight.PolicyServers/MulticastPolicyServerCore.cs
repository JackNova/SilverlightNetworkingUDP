using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;


namespace Microsoft.Silverlight.PolicyServers
{
    // The main implementation of the multicast policy protocol.  An instance handles either IPv4
    // or IPv6; the public MulticastPolicyServer class instantiates one for each address family.
    internal class MulticastPolicyServerCore
    {
        private const int MulticastPolicyPort = 9430;

        private IMulticastSocketFactory factory;

        private IMulticastSocket socket;
        private byte[] buffer;
        private List<IPAddress> joinedGroups;

        private readonly AddressFamily addressFamily;
        private readonly IPEndPoint localEndPoint;
        private readonly SocketOptionLevel socketOptionLevel;
        private readonly MulticastPolicyConfiguration configuration;

        public MulticastPolicyServerCore(AddressFamily addressFamily, MulticastPolicyConfiguration configuration)
        {
            Debug.Assert(configuration != null, "Configuration should not be null");

            this.addressFamily = addressFamily;
            this.configuration = configuration;

            if (addressFamily == AddressFamily.InterNetwork)
            {
                this.localEndPoint = new IPEndPoint(IPAddress.Any, MulticastPolicyPort);
                this.socketOptionLevel = SocketOptionLevel.IP;
            }
            else
            {
                this.localEndPoint = new IPEndPoint(IPAddress.IPv6Any, MulticastPolicyPort);
                this.socketOptionLevel = SocketOptionLevel.IPv6;
            }

            SetMulticastSocketFactory(new RealMulticastSocketFactory());
        }

        public void SetMulticastSocketFactory(IMulticastSocketFactory factory)
        {
            Debug.Assert(factory != null, "Factory should not be null");
            this.factory = factory;
        }

        public void Start()
        {
            Trace.TraceInformation("MulticastPolicyServerCore<{0}>: Starting", addressFamily);

            foreach (ICollection<MulticastResource> respondTo in configuration.SingleSourceConfiguration.Values)
            {
                foreach (MulticastResource allowedResource in respondTo)
                {
                    if (allowedResource.GroupAddress.AddressFamily == addressFamily)
                    {
                        EnsureSocket();
                    }
                }
            }

            foreach (ICollection<MulticastResource> respondTo in configuration.AnySourceConfiguration.Values)
            {
                foreach (MulticastResource allowedResource in respondTo)
                {
                    if (allowedResource.GroupAddress.AddressFamily == addressFamily)
                    {
                        JoinGroup(allowedResource.GroupAddress);
                    }
                }
            }

            // if we actually created a socket above, start receiving messages from it
            if (socket != null)
            {
                EndPoint remoteEP = localEndPoint;
                IAsyncResult result;

                try
                {
                    result = socket.BeginReceiveMessageFrom(buffer, 0, buffer.Length, SocketFlags.None,
                    ref remoteEP, ReceiveCompletionCallback, null);
                }
                catch (SocketException ex)
                {
                    Trace.TraceWarning("MulticastPolicyServerCore<{0}>: Failure while receiving: {1}",
                        addressFamily, ex);
                    return;
                }

                if (result.CompletedSynchronously)
                {
                    ProcessReceiveCompletion(result);
                }
            }
        }

        public void Stop()
        {
            Trace.TraceInformation("MulticastPolicyServerCore<{0}>: Stopping", addressFamily);

            if (socket != null)
            {
                socket.Close();
            }
        }

        private void EnsureSocket()
        {
            if (socket == null)
            {
                socket = factory.Create(addressFamily, SocketType.Dgram, ProtocolType.Udp);
                socket.SetSocketOption(socketOptionLevel, SocketOptionName.PacketInformation, true);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                socket.Bind(localEndPoint);

                buffer = new byte[UInt16.MaxValue];
                joinedGroups = new List<IPAddress>();
            }
        }

        private void JoinGroup(IPAddress groupAddress)
        {
            EnsureSocket();

            if (!joinedGroups.Contains(groupAddress))
            {
                Trace.TraceInformation("MulticastPolicyServerCore<{0}>: Joining multicast group {1}",
                    addressFamily, groupAddress);

                object option;
                if (addressFamily == AddressFamily.InterNetwork)
                {
                    option = new MulticastOption(groupAddress);
                }
                else
                {
                    option = new IPv6MulticastOption(groupAddress);
                }

                try
                {
                    socket.SetSocketOption(socketOptionLevel, SocketOptionName.AddMembership, option);
                    joinedGroups.Add(groupAddress);
                }
                catch (SocketException ex)
                {
                    Trace.TraceWarning("MulticastPolicyServerCore<{0}>: Failure joining group {1} : {2}", 
                        addressFamily, groupAddress, ex);
                }
            }
        }

        private void ReceiveCompletionCallback(IAsyncResult result)
        {
            if (!result.CompletedSynchronously)
            {
                ProcessReceiveCompletion(result);
            }
        }

        private void ProcessReceiveCompletion(IAsyncResult result)
        {
            bool async = false;
            while (!async)
            {
                int bytesTransferred;
                SocketFlags flags = SocketFlags.None;
                EndPoint remoteEP = localEndPoint;
                IPPacketInformation packetInfo;

                try
                {
                    bytesTransferred = socket.EndReceiveMessageFrom(result, ref flags, ref remoteEP, out packetInfo);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode != SocketError.OperationAborted)
                    {
                        Trace.TraceWarning("MulticastPolicyServerCore<{0}>: Failure while receiving: {1}",
                            addressFamily, ex);
                    }
                    return;
                }

                MulticastPolicyPacket packet = MulticastPolicyPacket.Parse(buffer, 0, bytesTransferred);
                async = ProcessPacket(packet, remoteEP, packetInfo);

                if (!async)
                {
                    try
                    {
                        result = socket.BeginReceiveMessageFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remoteEP, ReceiveCompletionCallback, null);
                    }
                    catch (SocketException ex)
                    {
                        Trace.TraceWarning("PolicyServerCore<{0}>: failure while posting a receive: {1}",
                            addressFamily, ex);
                        return;
                    }

                    async = !(result.CompletedSynchronously);
                }
            }
        }

        private void SendCompletionCallback(IAsyncResult result)
        {
            if (!result.CompletedSynchronously)
            {
                ProcessSendCompletion(result, true);
            }
        }

        private void ProcessSendCompletion(IAsyncResult result, bool postReceive)
        {
            try
            {
                socket.EndSendTo(result);
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode != SocketError.OperationAborted)
                {
                    Trace.TraceWarning("MulticastPolicyServerCore<{0}>: failure while sending: {1}",
                        addressFamily, ex);
                }
                return;
            }

            if (postReceive)
            {
                bool async;
                EndPoint remoteEP = localEndPoint;

                try
                {
                    result = socket.BeginReceiveMessageFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remoteEP, ReceiveCompletionCallback, null);
                }
                catch (SocketException ex)
                {
                    Trace.TraceWarning("MulticastPolicyServerCore<{0}>: failure while posting a receive: {1}",
                        addressFamily, ex);
                    return;
                }

                async = !(result.CompletedSynchronously);

                if (!async)
                {
                    ProcessReceiveCompletion(result);
                }
            }
        }

        private bool ProcessPacket(MulticastPolicyPacket packet, EndPoint source, IPPacketInformation packetInfo)
        {
            if (packet == null)
            {
                return false;
            }

            if (packet.Type != MulticastPolicyPacketType.Announcement)
            {
                return false;
            }

            bool async;

            if (IsMulticastAddress(packetInfo.Address))
            {
                async = ProcessAnySourceAnnouncement(packet, packetInfo);
            }
            else
            {
                async = ProcessSingleSourceAnnouncement(packet, source);
            }

            return async;
        }

        private bool ProcessAnySourceAnnouncement(MulticastPolicyPacket packet, IPPacketInformation packetInfo)
        {
            Trace.TraceInformation("MulticastPolicyServerCore<" + addressFamily + ">: " +
                "Processing packet with GroupAddress=" + packet.GroupAddress + ", Port=" + packet.Port +
                ", ApplicationOrigin=" + packet.ApplicationOrigin);

            if (!IPAddress.Equals(packet.GroupAddress, packetInfo.Address))
            {
                Trace.TraceWarning(
                    "MulticastPolicyServerCore<{0}>: Rejecting because GroupAddress does not match packet destination",
                    addressFamily);
                return false;
            }

            bool async = false;

            if (ShouldRespondToPacket(packet, configuration.InternalAnySourceConfiguration))
            {
                packet.Type = MulticastPolicyPacketType.Authorization;

                int packetLength = packet.SerializeTo(buffer, 0, buffer.Length);
                
                IAsyncResult result;
                try
                {
                    result = socket.BeginSendTo(buffer, 0, packetLength, SocketFlags.None,
                        new IPEndPoint(packet.GroupAddress, MulticastPolicyPort), SendCompletionCallback, null);
                }
                catch (SocketException ex)
                {
                    Trace.TraceWarning("MulticastPolicyServerCore<{0}>: Error while sending authorization packet: {1}",
                        addressFamily, ex);
                    return false;
                }

                async = !(result.CompletedSynchronously);

                if (!async)
                {
                    ProcessSendCompletion(result, false);
                }
            }

            return async;
        }

        private bool ProcessSingleSourceAnnouncement(MulticastPolicyPacket packet, EndPoint source)
        {
            bool async = false;

            Trace.TraceInformation("MulticastPolicyServerCore<" + addressFamily + ">: " +
                "Processing packet with GroupAddress=" + packet.GroupAddress + ", Port=" + packet.Port +
                ", ApplicationOrigin=" + packet.ApplicationOrigin);

            if (ShouldRespondToPacket(packet, configuration.InternalSingleSourceConfiguration))
            {
                packet.Type = MulticastPolicyPacketType.Authorization;

                int packetLength = packet.SerializeTo(buffer, 0, buffer.Length);

                IAsyncResult result;

                try
                {
                    result = socket.BeginSendTo(buffer, 0, packetLength, SocketFlags.None, source, SendCompletionCallback, null);
                }
                catch (SocketException ex)
                {
                    Trace.TraceWarning("MulticastPolicyServerCore<{0}>: Error while sending authorization packet: {1}",
                        addressFamily, ex);
                    return false;
                }

                async = !(result.CompletedSynchronously);

                if (!async)
                {
                    ProcessSendCompletion(result, false);
                }
            }

            return async;
        }

        private bool ShouldRespondToPacket(MulticastPolicyPacket packet, PrefixKeyedDictionary<MulticastResource> config)
        {
            ICollection<MulticastResource> allowedResources = null;
            if (!config.TryGetValueByPrefixMatch(packet.ApplicationOrigin, out allowedResources))
            {
                Trace.TraceInformation(
                    "MulticastPolicyServerCore<{0}>: Rejecting because the app is blocked entirely",
                    addressFamily);
                return false;
            }

            foreach (MulticastResource resource in allowedResources)
            {
                if ((resource.GroupAddress == IPAddress.Any ||
                     IPAddress.Equals(resource.GroupAddress, packet.GroupAddress)) &&
                    (resource.HighPort >= packet.Port && resource.LowPort <= packet.Port))
                {
                    Trace.TraceInformation(
                        "MulticastPolicyServerCore<{0}>: Access is allowed, sending authorization packet",
                        addressFamily);
                    return true;
                }
            }

            Trace.TraceInformation(
                "MulticastPolicyServerCore<{0}>: Rejecting because the app is requesting access to an unapproved group/port",
                addressFamily);
            return false;
        }

        private static bool IsMulticastAddress(IPAddress address)
        {
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                byte[] addressBytes = address.GetAddressBytes();
                return ((addressBytes[0] & 0xE0) == 0xE0);
            }
            else
            {
                return address.IsIPv6Multicast;
            }
        }
    }
}
