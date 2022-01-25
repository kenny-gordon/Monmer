using Monmer.IO;

namespace Monmer.Network.Payloads
{
    /// <summary>
    /// Sent when a connection is established.
    /// </summary>
    public class HandshakePayload : ISerializable
    {
        /// <summary>
        /// The protocol version of the node.
        /// </summary>
        public uint Version;

        /// <summary>
        /// The time when connected to the node.
        /// </summary>
        public uint Timestamp;

        public int Size =>
            sizeof(uint) +              // Version
            sizeof(uint);               // Timestamp

        public static HandshakePayload Create()
        {
            return new HandshakePayload
            {
                Version = 0,
                Timestamp = DateTime.Now.ToTimestamp(),
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Version = reader.ReadUInt32();
            Timestamp = reader.ReadUInt32();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(Timestamp);
        }
    }
}
