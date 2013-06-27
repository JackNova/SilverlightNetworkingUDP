using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Net;

namespace WpfSocketServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += JoinMulticastGroup;
        }

        void JoinMulticastGroup(object sender, EventArgs e)
        {
            udpClient = new UdpClient();

            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress,
                1);

            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 1025));

            udpClient.JoinMulticastGroup(IPAddress.Parse("224.0.0.0"));

            BeginReceiveData();
        }

        void BeginReceiveData()
        {
            udpClient.BeginReceive(OnReceiveCompleted, null);
        }

        void OnReceiveCompleted(IAsyncResult result)
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] bits = udpClient.EndReceive(result, ref remoteEndPoint);

            if (bits.Length>=16)
            {
                Point point = PointExt.FromBuffer(bits, bits.Length - 16);
                MoveCircle(point);
            }
            BeginReceiveData();
        }

        void MoveCircle(Point point)
        {
            Dispatcher.BeginInvoke(new Action(() =>
                {
                    circle.Margin = new Thickness(
                        point.X - (circle.Width/2),
                        point.Y - (circle.Height)/2,
                        mainGrid.ActualWidth - (point.X + circle.Width),
                        mainGrid.ActualHeight - (point.Y + circle.Height));
                }));
        }


        void OnMouseMove(object sender, MouseEventArgs args)
        {
            Point point = args.GetPosition(mainGrid);

            byte[] bits = PointExt.ToBuffer(point);

            udpClient.BeginSend(bits, bits.Length,
                new IPEndPoint(IPAddress.Parse("224.0.0.0"), 1025),
                result =>
                {
                    udpClient.EndSend(result);
                }, null);
        }

        UdpClient udpClient;
    }
}
