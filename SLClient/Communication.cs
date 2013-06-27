using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Net.Sockets;

namespace SLClient
{
    public class PointEventArgs : EventArgs
    {
        public Point Point { get; set; }
    }

    public class Communication
    {
        public event EventHandler<PointEventArgs> PointReceived;

        public Communication()
        {
            this.buffer = new byte[16];

            this.udpClient = new UdpAnySourceMulticastClient(
                IPAddress.Parse("224.0.0.0"), 1025);

            this.udpClient.BeginJoinGroup(OnJoinCompleted, null);

        }

        void OnJoinCompleted(IAsyncResult result)
        {
            this.udpClient.EndJoinGroup(result);
            BeginRead();
        }

        private void BeginRead()
        {
            this.udpClient.BeginReceiveFromGroup(this.buffer, this.bytesReceived, this.buffer.Length -
                this.bytesReceived, OnReceiveCompleted, null);
        }
        void OnReceiveCompleted(IAsyncResult result)
        {
            IPEndPoint endPoint;

            int lastReceived = this.udpClient.EndReceiveFromGroup(result, out endPoint);

            this.bytesReceived += lastReceived;

            if (this.bytesReceived == this.buffer.Length)
            {
                Point point = PointExt.FromBuffer(this.buffer);
                if (this.PointReceived!=null)
                {
                    this.PointReceived(this,new PointEventArgs(){ Point = point});
                }
                this.bytesReceived = 0;
            }
            BeginRead();
        }

        public void SendPoint(Point p)
        {
            byte[] bits = PointExt.ToBuffer(p);
            this.udpClient.BeginSendToGroup(bits, 0, bits.Length, result =>
                {
                    this.udpClient.EndSendToGroup(result);
                }, null);
        }

        UdpAnySourceMulticastClient udpClient;
        byte[] buffer;
        int bytesReceived;
    }
}
