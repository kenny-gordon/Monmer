using Monmer.IO;

namespace Monmer.Network
{
    public class Message : ISerializable
    {
        /// <summary>
        /// Indicates the maximum size of <see cref="Payload"/>.
        /// </summary>
        public const int PayloadMaxSize = 0x800000; // 8MB

        /// <summary>
        /// The command of the message.
        /// </summary>
        public MessageCommand Command;

        /// <summary>
        /// The payload of the message.
        /// </summary>
        public ISerializable Payload;

        private byte[] _payload;

        public int Size => sizeof(MessageCommand) + _payload.GetVariableSize();

        /// <summary>
        /// Creates a new instance of the <see cref="Message"/> class.
        /// </summary>
        /// <param name="command">The command of the message.</param>
        /// <param name="payload">The payload of the message. For the messages that don't require a payload, it should be <see langword="null"/>.</param>
        /// <returns></returns>
        public static Message Create(MessageCommand command, ISerializable payload = null)
        {
            Message message = new Message()
            {
                Command = command,
                Payload = payload,
                _payload = payload?.ToArray() ?? Array.Empty<byte>()
            };

            return message;
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Command = (MessageCommand)reader.ReadByte();
            Payload = ReflectionCache<MessageCommand>.CreateSerializable(Command, _payload = reader.ReadVariableBytes(PayloadMaxSize));

        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Command);
            writer.WriteVariableBytes(_payload);
        }
    }
}