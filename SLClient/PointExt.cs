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

namespace SLClient
{
    public class PointExt
    {
        internal static byte[] ToBuffer(Point point)
        {
            byte[] bufferX = BitConverter.GetBytes(point.X);
            byte[] bufferY = BitConverter.GetBytes(point.Y);
            byte[] buffer = new byte[bufferX.Length+bufferY.Length];
            
            System.Buffer.BlockCopy(bufferX, 0, buffer, 0, bufferX.Length);
            System.Buffer.BlockCopy(bufferY, 0, buffer, bufferX.Length, bufferY.Length);
            return buffer;
        }

        internal static Point FromBuffer(byte[] p)
        {
            Point point = new Point();
            point.X = BitConverter.ToDouble(p, 0);
            point.Y = BitConverter.ToDouble(p, 8);
            return point;
        }
    }
}
