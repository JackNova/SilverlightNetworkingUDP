using System;
using System.Xml;
using System.Xml.XPath;
using System.Globalization;
using System.Collections.Generic;
using System.Net;

namespace Microsoft.Silverlight.PolicyServers
{
    // Handles programmatic configuration of a policy server, and loading a configuration from an XML
    // data source at runtime.
    public class MulticastPolicyConfiguration
    {
        private const string Namespace = "http://schemas.microsoft.com/silverlight/policyservers/multicastpolicyserver";

        private PrefixKeyedDictionary<MulticastResource> singleSourceConfiguration;
        private PrefixKeyedDictionary<MulticastResource> anySourceConfiguration;

        public MulticastPolicyConfiguration()
        {
            this.singleSourceConfiguration = new PrefixKeyedDictionary<MulticastResource>();
            this.anySourceConfiguration = new PrefixKeyedDictionary<MulticastResource>();
        }

        public MulticastPolicyConfiguration(IXPathNavigable xmlConfiguration)
            : this()
        {
            XPathNavigator navigator = xmlConfiguration.CreateNavigator();

            if (!navigator.MoveToChild("multicast-policy-responder", Namespace))
            {
                throw new FormatException("Configuration does not have a multicast-policy-responder root element");
            }

            LoadSingleSourceResponder(navigator.Clone());
            LoadAnySourceResponder(navigator.Clone());
        }

        private MulticastPolicyConfiguration(PrefixKeyedDictionary<MulticastResource> singleSourceConfiguration,
            PrefixKeyedDictionary<MulticastResource> anySourceConfiguration)
        {
            this.singleSourceConfiguration = singleSourceConfiguration;
            this.anySourceConfiguration = anySourceConfiguration;
        }

        public IValueSetDictionary<string, MulticastResource> SingleSourceConfiguration
        {
            get { return singleSourceConfiguration; }
        }

        public IValueSetDictionary<string, MulticastResource> AnySourceConfiguration
        {
            get { return anySourceConfiguration; }
        }

        internal PrefixKeyedDictionary<MulticastResource> InternalSingleSourceConfiguration
        {
            get { return singleSourceConfiguration; }
        }

        internal PrefixKeyedDictionary<MulticastResource> InternalAnySourceConfiguration
        {
            get { return anySourceConfiguration; }
        }

        internal MulticastPolicyConfiguration MakeReadOnlyCopy()
        {
            if (singleSourceConfiguration.IsReadOnly && anySourceConfiguration.IsReadOnly)
            {
                return this;
            }

            return new MulticastPolicyConfiguration(singleSourceConfiguration.MakeReadOnlyCopy(),
                anySourceConfiguration.MakeReadOnlyCopy());
        }

        private void LoadSingleSourceResponder(XPathNavigator navigator)
        {
            if (navigator.MoveToChild("ssm-responder", Namespace))
            {
                LoadResponder(navigator.Clone(), singleSourceConfiguration);

                if (navigator.MoveToNext("ssm-responder", Namespace))
                {
                    throw new FormatException("Configuration contains two ssm-responder nodes");
                }
            }
        }

        private void LoadAnySourceResponder(XPathNavigator navigator)
        {
            if (navigator.MoveToChild("asm-responder", Namespace))
            {
                LoadResponder(navigator.Clone(), anySourceConfiguration);

                if (navigator.MoveToNext("asm-responder", Namespace))
                {
                    throw new FormatException("Configuration contains two asm-responder nodes");
                }
            }
        }

        private static void LoadResponder(XPathNavigator navigator, 
            PrefixKeyedDictionary<MulticastResource> configuration)
        {
            XPathNodeIterator iter = navigator.SelectChildren("respond-to", Namespace);

            while (iter.MoveNext())
            {
                XPathNavigator attr = iter.Current.Clone();
                if (!attr.MoveToAttribute("application", String.Empty))
                {
                    throw new FormatException(
                            "Configuration contains a respond-to node without an application attribute");
                }

                LoadAllowedResources(iter.Current, attr.Value, configuration);
            }
        }

        private static void LoadAllowedResources(XPathNavigator navigator, string application,
            PrefixKeyedDictionary<MulticastResource> resources)
        {
            XPathNodeIterator iter = navigator.SelectChildren("allowed-resource", Namespace);

            while (iter.MoveNext())
            {
                XPathNavigator portAttr = iter.Current.Clone();
                if (!portAttr.MoveToAttribute("port", String.Empty))
                {
                    throw new FormatException(
                            "Configuration contains an allowed-resource node without a port attribute");
                }

                XPathNavigator groupAttr = iter.Current.Clone();
                if (!groupAttr.MoveToAttribute("group", String.Empty))
                {
                    throw new FormatException(
                            "Configuration contains an allowed-resource node without a group attribute");
                }

                int lowPort;
                int highPort;
                LoadPortRange(portAttr.Value, out lowPort, out highPort);

                IPAddress groupAddress;
                if (String.Equals(groupAttr.Value, "*", StringComparison.Ordinal))
                {
                    groupAddress = IPAddress.Any;
                }
                else
                {
                    groupAddress = IPAddress.Parse(groupAttr.Value);
                }

                resources.Add(application, new MulticastResource(groupAddress, lowPort, highPort));
            }
        }

        private static void LoadPortRange(string portString, out int lowPort, out int highPort)
        {
            if (String.Equals(portString, "*", StringComparison.Ordinal))
            {
                lowPort = 0;
                highPort = 0xFFFF;
                return;
            }

            string[] ints = portString.Split('-');

            if (ints.Length == 1)
            {
                lowPort = Int32.Parse(ints[0], CultureInfo.InvariantCulture);
                highPort = lowPort;
            }
            else if (ints.Length == 2)
            {
                lowPort = Int32.Parse(ints[0], CultureInfo.InvariantCulture);
                highPort = Int32.Parse(ints[1], CultureInfo.InvariantCulture);
            }
            else
            {
                throw new FormatException("Configuration contains invalidly formatted port attribute");
            }
        }
    }
}
