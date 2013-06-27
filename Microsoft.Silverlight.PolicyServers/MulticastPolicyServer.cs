using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;

namespace Microsoft.Silverlight.PolicyServers
{
    // The main public interface for the multicast policy server.
    public class MulticastPolicyServer : IDisposable
    {
        private MulticastPolicyConfiguration configuration;

        private bool started;
        private bool disposed;

        private MulticastPolicyServerCore v4Server;
        private MulticastPolicyServerCore v6Server;

        public MulticastPolicyServer(MulticastPolicyConfiguration configuration)
        {
            this.configuration = configuration.MakeReadOnlyCopy();
        }

        public bool Started
        {
            get
            {
                return started;
            }
        }

        public MulticastPolicyConfiguration Configuration
        {
            get { return this.configuration; }
        }

        public void Start()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (started)
            {
                throw new InvalidOperationException("Already started");
            }

            Trace.TraceInformation("MulticastPolicyServer: Starting");

            started = true;

            try
            {
                v6Server = new MulticastPolicyServerCore(AddressFamily.InterNetworkV6, configuration);
                v4Server = new MulticastPolicyServerCore(AddressFamily.InterNetwork, configuration);

                v6Server.Start();
                v4Server.Start();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                Stop();
                throw;
            }
        }

        public void Stop()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (!started)
            {
                throw new InvalidOperationException("Not started yet");
            }

            Trace.TraceInformation("MulticastPolicyServer: Stopping");

            started = false;

            if (v4Server != null)
            {
                v4Server.Stop();
            }

            if (v6Server != null)
            {
                v6Server.Stop();
            }
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
                if (!disposed)
                {
                    if (started)
                    {
                        Stop();
                    }

                    disposed = true;
                }
            }
        }
    }
}
