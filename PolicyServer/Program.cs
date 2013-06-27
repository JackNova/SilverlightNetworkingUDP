using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.Silverlight.PolicyServers;

namespace PolicyServer
{
    public class Program
    {
        static void Main(string[] args)
        {
            string configFileName = "policyfile.xml";

            if (args.Length>0)
            {
                configFileName = args[0];
            }

            XmlDocument xmlConfigFile = new XmlDocument();
            xmlConfigFile.Load(configFileName);

            MulticastPolicyServer policyServer = new MulticastPolicyServer(
                new MulticastPolicyConfiguration(xmlConfigFile));

            policyServer.Start();

            Console.WriteLine("UDP Policy Server Waiting for Requests");

            Console.ReadLine();

            policyServer.Stop();
        }
    }
}
