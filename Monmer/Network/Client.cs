using Monmer.IO;
using Monmer.Network.Payloads;
using System.Net.Sockets;

namespace Monmer.Network
{
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
                    SendToServer(Message.Create(MessageCommand.Handshake, HandshakePayload.Create()));
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
                HandleMessage(buffer.AsSerializable<Message>()); //New

                _stream.BeginRead(_receiveBuffer, 0, 4096 * 2, OnReceive, null);
            }
            catch (Exception)
            {
                return;
            }
        }

        public static void SendToServer(Message message)
        {
            byte[] data = message.ToArray();
            _stream.BeginWrite(data, 0, data.Length, null, null);
        }

        private static void HandleMessage(Message message)
        {
            switch (message.Command)
            {
                case MessageCommand.HandshakeAck:
                    OnHandshakeAck();
                    break;
            }
        }

        private static void OnHandshakeAck()
        {
            SendToServer(Message.Create(MessageCommand.Handshake, HandshakePayload.Create()));

            Console.WriteLine("Server at {0} has Acknowleged Handshake", _socket.Client.RemoteEndPoint);
        }
    }
}
