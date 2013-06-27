using System;
using System.Collections.Generic;
using System.Net;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Microsoft.Silverlight.PolicyServers
{
    internal enum MulticastPolicyPacketType
    {
        Announcement = 0,
        Authorization = 1
    }

    // The MulticastPolicyPacket class encapsulates the functionality of serializing and deserializing messages
    // from the Silverlight multicast policy system.  On the wire, they look like:
    //
    // struct MulticastPolicyPacket
    // {
    //     byte[3] header = “SL\0”;
    //     byte version = 1;
    //     byte messageType = {Announcement: 0, Authorization: 1};
    //     byte[4] messageId;
    //     byte[2] port;
    //
    //     byte[2] applicationOriginUriLength;
    //     byte groupAddressLength;
    //
    //     byte[originUriLength] applicationOriginUri;
    //     byte[groupAddressLength] groupAddress;
    // };
    //
    // all multi-byte number fields are stored with least-significant-byte first.  applicationOriginUri is UTF-8
    // encoded.
    internal class MulticastPolicyPacket
    {
        private const int ConstantLength = 14;
        private static readonly byte[] HeaderBytes = new byte[] { (byte)'S', (byte)'L', 0 };

        private MulticastPolicyPacketType type;
        private byte[] messageId;
        private string applicationOrigin;
        private byte[] applicationOriginBytes;
        private IPAddress groupAddress;
        private byte[] groupAddressBytes;
        private int port;

        public MulticastPolicyPacket()
        {
            type = MulticastPolicyPacketType.Announcement;
            messageId = new byte[4];
        }

        public MulticastPolicyPacketType Type
        {
            get { return type; }
            set { type = value; }
        }

        public byte[] MessageId
        {
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used by Unit Tests (via FriendAccessAllowed)")]
            get { return messageId; }
        }

        public string ApplicationOrigin
        {
            get { return applicationOrigin; }

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used by Unit Tests (via FriendAccessAllowed)")]
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                byte[] bytes = Encoding.UTF8.GetBytes(value);
                if (bytes.Length > UInt16.MaxValue)
                {
                    throw new ArgumentException("ApplicationOrigin cannot exceed 65535 characters");
                }

                applicationOrigin = value;
                applicationOriginBytes = bytes;
            }
        }

        public IPAddress GroupAddress
        {
            get { return groupAddress; }

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used by Unit Tests (via FriendAccessAllowed)")]
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                groupAddress = value;
                groupAddressBytes = value.GetAddressBytes();
            }
        }

        public int Port
        {
            get { return port; }

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used by Unit Tests (via FriendAccessAllowed)")]
            set
            {
                if (value < 0 || value > UInt16.MaxValue)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                port = value;
            }
        }


        public int SerializeTo(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (count < 0 || count > (buffer.Length - offset))
            {
                throw new ArgumentOutOfRangeException("count");
            }

            if (groupAddressBytes == null)
            {
                throw new InvalidOperationException("GroupAddress must be set before calling SerializeTo");
            }
            if (applicationOriginBytes == null)
            {
                throw new InvalidOperationException("ApplicationOrigin must be set before calling SerializeTo");
            }

            int groupAddressLength = groupAddressBytes.Length;
            int applicationOriginLength = applicationOriginBytes.Length;

            int packetLength = ConstantLength + groupAddressLength + applicationOriginLength;
            if (packetLength > count)
            {
                throw new ArgumentException("Buffer is too small!");
            }

            buffer[offset] = HeaderBytes[0];
            buffer[offset + 1] = HeaderBytes[1];
            buffer[offset + 2] = HeaderBytes[2];

            buffer[offset + 3] = 1;  // version
            buffer[offset + 4] = (byte)type;

            buffer[offset + 5] = messageId[0];
            buffer[offset + 6] = messageId[1];
            buffer[offset + 7] = messageId[2];
            buffer[offset + 8] = messageId[3];

            buffer[offset + 9] = (byte)(port & 0xff);
            buffer[offset + 10] = (byte)((port >> 8) & 0xff);

            buffer[offset + 11] = (byte)(applicationOriginLength & 0xff);
            buffer[offset + 12] = (byte)((applicationOriginLength >> 8) & 0xff);

            buffer[offset + 13] = (byte)groupAddressLength;

            Buffer.BlockCopy(applicationOriginBytes, offset, buffer, 14, applicationOriginLength);
            Buffer.BlockCopy(groupAddressBytes, offset, buffer, 14 + applicationOriginLength, groupAddressLength);

            return packetLength;
        }

        // tries to parse the specified chunk of bytes as a multicast policy packet.  If it can,
        // it returns a managed representation.  Otherwise, it returns null.
        public static MulticastPolicyPacket Parse(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (count < 0 || count > (buffer.Length - offset))
            {
                throw new ArgumentOutOfRangeException("count");
            }

            if (count < ConstantLength)
            {
                // malformed packet - not long enough even if both variable-length fields are empty
                return null;
            }

            if (buffer[offset] != HeaderBytes[0] ||
                buffer[offset + 1] != HeaderBytes[1] ||
                buffer[offset + 2] != HeaderBytes[2])
            {
                // malformed packet - doesn't have the header
                return null;
            }

            if (buffer[offset + 3] != 1)
            {
                // we only understand version 1 of the protocol - ignore anything using a newer version.
                return null;
            }

            MulticastPolicyPacket rval = new MulticastPolicyPacket();

            rval.type = (MulticastPolicyPacketType)buffer[offset + 4];

            rval.messageId[0] = buffer[offset + 5];
            rval.messageId[1] = buffer[offset + 6];
            rval.messageId[2] = buffer[offset + 7];
            rval.messageId[3] = buffer[offset + 8];

            rval.port = ((int)buffer[offset + 9]) | (((int)buffer[offset + 10]) << 8);

            int applicationOriginLength = (buffer[offset + 11]) | (buffer[offset + 12] << 8);
            int groupAddressLength = buffer[offset + 13];

            offset += 14;

            if ((offset + applicationOriginLength) > count)
            {
                // malformed packet - application origin length past the end of the buffer
                return null;
            }

            rval.applicationOriginBytes = new byte[applicationOriginLength];
            Buffer.BlockCopy(buffer, offset, rval.applicationOriginBytes, 0, applicationOriginLength);

            try
            {
                UTF8Encoding encoding = new UTF8Encoding(false, true);
                rval.applicationOrigin = encoding.GetString(rval.applicationOriginBytes, 0, applicationOriginLength);
            }
            catch (DecoderFallbackException)
            {
                // malformed packet - invalid UTF8 in application origin uri
                return null;
            }

            offset += applicationOriginLength;

            if ((offset + groupAddressLength) > count)
            {
                // malformed packet - group address length past the end of the buffer
                return null;
            }

            if (groupAddressLength != 4 && groupAddressLength != 16)
            {
                // malformed packet - group address isn't ipv4 or ipv6
                return null;
            }

            rval.groupAddressBytes = new byte[groupAddressLength];
            Buffer.BlockCopy(buffer, offset, rval.groupAddressBytes, 0, groupAddressLength);
            rval.groupAddress = new IPAddress(rval.groupAddressBytes);

            return rval;
        }
    }
}
