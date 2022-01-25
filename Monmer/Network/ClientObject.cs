using Monmer.IO;
using Monmer.Network;
using Monmer.Network.Payloads;
using System.Net;
using System.Net.Sockets;

namespace Monmer.Network
{
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
                /// ServerHandler.HandleData(ConnectionID, newBytes); //Original
                /// 
                /// MessageHandler.Message(ConnectionID, buffer.AsSerializable<Message>()); //New
                /// 

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
            _listener.BeginAcceptSocket(new AsyncCallback(OnClientConnect), null);

            Console.WriteLine("Server Started on {0}", _listener.LocalEndpoint);
        }

        private static void OnClientConnect(IAsyncResult asyncResult)
        {
            TcpClient remoteClient = _listener.EndAcceptTcpClient(asyncResult);

            Console.WriteLine("Incoming connection from '{0}' received.", remoteClient.Client.RemoteEndPoint);

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
    }

    internal static class MessageHandler
    {
        public static void Message(int connectionID, Message message)
        {
            switch (message.Command)
            {
                case MessageCommand.Handshake:
                    OnHandShake((HandshakePayload)message.Payload);
                    break;
                case MessageCommand.HandshakeAck:
                    OnHandShakeAck();
                    break;
            }
        }

        private static void OnHandShake(HandshakePayload handshakePayload)
        {
            throw new NotImplementedException();
        }

        private static void OnHandShakeAck()
        {
            throw new NotImplementedException();
        }

    }

    public static class Client
    {
        private static TcpClient _socket;
        private static NetworkStream _stream;
        private static byte[] _receiveBuffer;

        public static void Connect(string serverAddr, int serverPort)
        {
            _socket = new TcpClient();
            _socket.ReceiveBufferSize = 4096;
            _socket.SendBufferSize = 4096;
            _receiveBuffer = new byte[4096 * 2];

            _socket.BeginConnect(serverAddr, serverPort, new AsyncCallback(OnServerConnect), _socket);
        }

        private static void OnServerConnect(IAsyncResult asyncResult)
        {
            try
            {
                _socket.EndConnect(asyncResult);

                if (_socket.Connected)
                {
                    _socket.NoDelay = true;
                    _stream = _socket.GetStream();
                    _stream.BeginRead(_receiveBuffer, 0, 4096 * 2, OnReceive, null);

                    /// TODO: Send Handshake to the server.
                    /// PacketSender.ClientOnSend(); //Original
                    /// 
                    /// MessageHandler.Message(ConnectionID, buffer.AsSerializable<Message>()); //New
                    /// 

                }
                else
                {
                    return;
                }

            }
            catch (Exception)
            {

                throw;
            }


        }

        private static void OnReceive(IAsyncResult asyncResult)
        {
            try
            {
                int length = _stream.EndRead(asyncResult);

                if (length <= 0)
                {
                    return;
                }

                byte[] buffer = new byte[length];
                Array.Copy(_receiveBuffer, buffer, length);

                /// TODO: Do something with incoming data
                /// ClientHandler.HandleData(newBytes); //Original
                /// 
                /// MessageHandler.Message(ConnectionID, buffer.AsSerializable<Message>()); //New
                /// 

                _stream.BeginRead(_receiveBuffer, 0, 4096 * 2, OnReceive, null);
            }
            catch (Exception)
            {
                return;
            }
        }
    }
}
