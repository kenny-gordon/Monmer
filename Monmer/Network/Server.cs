using Monmer.IO;
using Monmer.Network.Payloads;
using System.Net;
using System.Net.Sockets;

namespace Monmer.Network
{
    public static class Server
    {
        private static TcpListener _listener;

        internal static readonly Dictionary<int, ClientObject> RemoteClients = new Dictionary<int, ClientObject>();

        /// <summary>
        /// Indicates the number of connected nodes.
        /// </summary>
        public static int ConnectedCount => RemoteClients.Count;

        public static void Start(int port)
        {
            // Create a TCP/IP socket.  
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();

            // Start listening for connections.  
            _listener.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), null);

            Console.WriteLine("Server Started on {0}", _listener.LocalEndpoint);
        }

        private static void OnClientConnect(IAsyncResult asyncResult)
        {
            TcpClient remoteClient = _listener.EndAcceptTcpClient(asyncResult);
            
            Console.WriteLine("Incoming connection from {0} received.", remoteClient.Client.RemoteEndPoint);

            _listener.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), null);
            RegisterClient(remoteClient);
        }

        private static void RegisterClient(TcpClient remoteClient)
        {
            ClientObject client = new ClientObject();
            client.ConnectionID = ((IPEndPoint)remoteClient.Client.RemoteEndPoint).Port;
            client.Socket = remoteClient;
            client.Initialize();
            RemoteClients.Add(client.ConnectionID, client);
        }

        internal static void DeregisterClient(int connectionID)
        {
            RemoteClients.Remove(connectionID);
        }

        public static void SendToClient(int connectionID, Message message)
        {
            byte[] data = message.ToArray();
            RemoteClients[connectionID].Stream.BeginWrite(data, 0, data.Length, null, null);
        }

        public static void SendToClients(Message message)
        {
            foreach (var remoteClient in RemoteClients.Keys)
            {
                SendToClient(remoteClient, message);
            }
        }

        internal static void HandleMessage(int connectionID, Message message)
        {
            switch (message.Command)
            {
                case MessageCommand.Handshake:
                    OnHandshake(connectionID, message);
                    break;
            }
        }

        private static void OnHandshake(int connectionID, Message message)
        {
            var data = (HandshakePayload)message.Payload;

            // Send Handshake Acknowlegment to Client
            SendToClient(connectionID, Message.Create(MessageCommand.HandshakeAck));
        }

        internal class ClientObject
        {
            public int ConnectionID;
            public TcpClient Socket;
            public NetworkStream Stream;
            public Message Message;

            private byte[] _receiveBuffer;

            public void Initialize()
            {
                Socket.SendBufferSize = 4096;
                Socket.ReceiveBufferSize = 4096;
                Stream = Socket.GetStream();

                _receiveBuffer = new byte[4096];

                Stream.BeginRead(_receiveBuffer, 0, Socket.ReceiveBufferSize, OnReceive, null);
            }

            private void OnReceive(IAsyncResult asyncResult)
            {
                try
                {
                    int length = Stream.EndRead(asyncResult);

                    if (length <= 0)
                    {
                        CloseConnection();
                        return;
                    }

                    byte[] buffer = new byte[length];
                    Array.Copy(_receiveBuffer, buffer, length);

                    /// TODO: If Handshake is received by client send acknowledgement or disconect else handle incoming data
                    HandleMessage(ConnectionID, buffer.AsSerializable<Message>()); //New

                    Stream.BeginRead(_receiveBuffer, 0, Socket.ReceiveBufferSize, OnReceive, null);
                }
                catch (Exception)
                {
                    CloseConnection();
                    return;
                }
            }

            private void CloseConnection()
            {
                Console.WriteLine("Connection from '{0}' has been terminated.", Socket.Client.RemoteEndPoint);

                Socket.Close();
            }
        }
    }
}
