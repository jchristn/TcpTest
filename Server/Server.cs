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
    class Program
    {
        static bool runForever = true;
        static CancellationTokenSource tokenSource = new CancellationTokenSource();
        static CancellationToken token = tokenSource.Token;
        static TcpListener listener = new TcpListener(IPAddress.Loopback, 8000);
        static Dictionary<string, Metadata> clients = new Dictionary<string, Metadata>();

        static void Main(string[] args)
        {
            listener.Start();
            Task.Run(() => AcceptConnections(token), token);

            while (runForever)
            {
                Console.Write("Command [? for help]: ");
                string userInput = Console.ReadLine();
                if (String.IsNullOrEmpty(userInput)) continue;

                switch (userInput)
                {
                    case "?":
                        Menu();
                        break;
                    case "q":
                        runForever = false;
                        break;
                    case "c":
                    case "cls":
                        Console.Clear();
                        break;
                    case "dispose":
                        DisposeServer();
                        break;
                    case "list":
                        ListClients();
                        break;
                    case "send":
                        SendData();
                        break;
                }
            }
        }

        static void Menu()
        {
            Console.WriteLine("");
            Console.WriteLine("  q              Quit this program");
            Console.WriteLine("  cls            Clear the screen");
            Console.WriteLine("  dispose        Dispose the server");
            Console.WriteLine("  list           List clients");
            Console.WriteLine("  send           Send data to a client");
            Console.WriteLine("");
        }

        static void DisposeServer()
        {
            try
            {
                if (clients != null && clients.Count > 0)
                {
                    foreach (KeyValuePair<string, Metadata> curr in clients)
                    {
                        Console.WriteLine("Disconnecting " + curr.Key);
                        curr.Value.Dispose();
                    }
                }

                tokenSource.Cancel();
                tokenSource.Dispose();

                if (listener != null && listener.Server != null)
                {
                    listener.Server.Close();
                    listener.Server.Dispose();
                }

                if (listener != null) listener.Stop();
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    Environment.NewLine +
                    "Dispose Exception:" +
                    Environment.NewLine +
                    e.ToString() +
                    Environment.NewLine);
            }
        }

        static void ListClients()
        { 
            Console.WriteLine("");
            Console.WriteLine("Clients: " + clients.Count);
             
            foreach (KeyValuePair<string, Metadata> curr in clients)
                Console.WriteLine("  " + curr.Key);

            Console.WriteLine("");
        }

        static void SendData()
        {
            ListClients();
            Console.Write("Client: ");
            string key = Console.ReadLine();
            if (String.IsNullOrEmpty(key)) return;
            Metadata md = clients[key];

            Console.Write("Data: ");
            string data = Console.ReadLine();
            if (String.IsNullOrEmpty(data)) return;
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            lock (md.sendLock)
            {
                md.networkStream.Write(dataBytes, 0, dataBytes.Length);
                md.networkStream.Flush();
            }
        }

        static async Task AcceptConnections(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                Metadata md = new Metadata(client);
                clients.Add(client.Client.RemoteEndPoint.ToString(), md);
                await Task.Run(() => DataReceiver(md), md.token);
            }
        }

        static async Task DataReceiver(Metadata md)
        {
            string header = "[" + md.tcpClient.Client.RemoteEndPoint.ToString() + "]";
            Console.WriteLine(header + " data receiver started");

            try
            {
                while (true)
                {
                    if (!IsClientConnected(md.tcpClient))
                    {
                        Console.WriteLine(header + " client no longer connected");
                        break;
                    }

                    if (token.IsCancellationRequested)
                    {
                        Console.WriteLine(header + " cancellation requested");
                        break;
                    }

                    byte[] data = await DataReadAsync(md.tcpClient, token);
                    if (data == null || data.Length < 1)
                    {
                        await Task.Delay(30);
                        continue;
                    }

                    Console.WriteLine(header + ": " + Encoding.UTF8.GetString(data));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    Environment.NewLine + 
                    header + 
                    " DataReceiver Exception: " + 
                    Environment.NewLine + 
                    e.ToString() + 
                    Environment.NewLine);
            }

            Console.WriteLine(header + " data receiver terminating");

            clients.Remove(md.tcpClient.Client.RemoteEndPoint.ToString());
            md.Dispose();
        }

        static async Task<byte[]> DataReadAsync(TcpClient client, CancellationToken token)
        {
            if (token.IsCancellationRequested) throw new OperationCanceledException();

            NetworkStream stream = client.GetStream();
            if (!stream.CanRead) return null;
            if (!stream.DataAvailable) return null;

            byte[] buffer = new byte[1024];
            int read = 0;

            using (MemoryStream ms = new MemoryStream())
            {
                while (true)
                {
                    read = await stream.ReadAsync(buffer, 0, buffer.Length);

                    if (read > 0)
                    {
                        ms.Write(buffer, 0, read);
                        return ms.ToArray();
                    }
                }
            }
        }

        static bool IsClientConnected(TcpClient client)
        {
            if (client.Connected)
            {
                if ((client.Client.Poll(0, SelectMode.SelectWrite)) && (!client.Client.Poll(0, SelectMode.SelectError)))
                {
                    byte[] buffer = new byte[1];
                    if (client.Client.Receive(buffer, SocketFlags.Peek) == 0)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
