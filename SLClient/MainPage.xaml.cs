using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SLClient
{
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();

            this.communication = new Communication();
            communication.PointReceived += OnPointReceived;
        }

        void OnPointReceived(object sender, PointEventArgs e)
        {
            MoveCircle(e.Point);
        }

        void MoveCircle(Point point)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                circle.Margin = new Thickness(
                    point.X - (circle.Width / 2),
                    point.Y - (circle.Height) / 2,
                    mainGrid.ActualWidth - (point.X + circle.Width),
                    mainGrid.ActualHeight - ((point.Y + circle.Height)));
            }
                    ));
        }

        void OnMouseMove(object sender, MouseEventArgs args)
        {
            Point point = args.GetPosition(mainGrid);
            communication.SendPoint(point);
        }

        Communication communication;
    }
}
