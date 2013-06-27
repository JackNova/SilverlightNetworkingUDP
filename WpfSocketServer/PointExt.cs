using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace WpfSocketServer
{
    public class PointExt
    {
        internal static byte[] ToBuffer(Point point)
        {
            byte[] bufferX = BitConverter.GetBytes(point.X);
            byte[] bufferY = BitConverter.GetBytes(point.Y);
            byte[] buffer = new byte[bufferX.Length + bufferY.Length];

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

        internal static Point FromBuffer(byte[] bits, int overflow)
        {
            if (overflow>0)
                Console.WriteLine("What's going on?");

            Point point = new Point();
            point.X = BitConverter.ToDouble(bits, 0);
            point.Y = BitConverter.ToDouble(bits, 8);
            return point;
        }
    }
}
