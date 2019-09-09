using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Metadata : IDisposable
    {
        public TcpClient tcpClient { get; set; }
        public NetworkStream networkStream { get; set; }
        public CancellationTokenSource tokenSource { get; set; }
        public CancellationToken token { get; set; }
        public object sendLock { get; set; }

        public Metadata(TcpClient client)
        {
            tcpClient = client;
            networkStream = tcpClient.GetStream();
            tokenSource = new CancellationTokenSource();
            token = tokenSource.Token;
            sendLock = new object();
        }

        public void Dispose()
        {
            if (networkStream != null)
            {
                networkStream.Close();
                networkStream.Dispose();
            }

            tokenSource.Cancel();

            if (tcpClient != null)
            {
                tcpClient.Close();
                tcpClient.Dispose();
            }
        }
    }
}
