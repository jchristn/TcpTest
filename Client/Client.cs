using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static bool runForever = true;
        static TcpClient client = null;
        static CancellationTokenSource tokenSource = new CancellationTokenSource();
        static CancellationToken token = tokenSource.Token;
        static readonly object sendLock = new object();

        static void Main(string[] args)
        {
            client = new TcpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            IAsyncResult ar = client.BeginConnect("127.0.0.1", 8000, null, null);
            WaitHandle wh = ar.AsyncWaitHandle;
             
            if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5), false))
            {
                client.Close();
                Console.WriteLine("Failed to connect to server");
                return;
            }

            client.EndConnect(ar);
            wh.Close();

            Task.Run(async () => await DataReceiver(token), token);

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
                        DisposeClient();
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
            Console.WriteLine("  dispose        Dispose the client");
            Console.WriteLine("  send           Send data to the server");
            Console.WriteLine("");
        }

        static void DisposeClient()
        {
            tokenSource.Cancel();
            tokenSource.Dispose();

            NetworkStream stream = client.GetStream();
            if (stream != null)
            {
                stream.Close();
                stream.Dispose();
            }

            client.Close();
            client.Dispose();
        }

        static void SendData()
        {
            Console.Write("Data: ");
            string data = Console.ReadLine();
            if (String.IsNullOrEmpty(data)) return;

            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            lock (sendLock)
            {
                client.GetStream().Write(dataBytes, 0, dataBytes.Length);
            }
        }

        static async Task DataReceiver(CancellationToken token)
        {
            try
            { 
                while (true)
                {
                    if (token.IsCancellationRequested
                        || client == null 
                        || !client.Connected 
                        || !IsClientConnected(client))
                    {
                        Console.WriteLine("Server disconnection detected from DataReceiver");
                        break;
                    }
                     
                    byte[] data = await DataReadAsync(token);
                    if (data == null)
                    {
                        await Task.Delay(30);
                        continue;
                    }

                    Console.WriteLine("Data from server: " + Encoding.UTF8.GetString(data));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    Environment.NewLine +
                    "DataReceiver Exception: " +
                    Environment.NewLine +
                    e.ToString() +
                    Environment.NewLine);
            }

            Console.WriteLine("Data receiver terminating");

            DisposeClient();
        }

        static async Task<byte[]> DataReadAsync(CancellationToken token)
        { 
            if (client == null 
                || !client.Connected)
            { 
                throw new OperationCanceledException();
            }
             
            byte[] buffer = new byte[1024];
            int read = 0;
             
            NetworkStream networkStream = client.GetStream();
            if (!networkStream.CanRead && !networkStream.DataAvailable)
            {
                throw new IOException();
            }

            using (MemoryStream ms = new MemoryStream())
            {
                while (true)
                {
                    read = await networkStream.ReadAsync(buffer, 0, buffer.Length);

                    if (read > 0)
                    {
                        ms.Write(buffer, 0, read);
                        return ms.ToArray();
                    }
                    else
                    {
                        throw new SocketException();
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
