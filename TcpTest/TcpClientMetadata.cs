using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TcpTest
{
    public class TcpClientMetadata
    {
        public TcpClient TcpClient { get; set; }
        public CancellationTokenSource TokenSource { get; set; }
        public CancellationToken Token { get; set; }

        public TcpClientMetadata(TcpClient client)
        {
            TcpClient = client;
            TokenSource = new CancellationTokenSource();
            Token = TokenSource.Token;
        }
    }
}
