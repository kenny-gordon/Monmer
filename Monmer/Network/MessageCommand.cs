using Monmer.IO;
using Monmer.Network.Payloads;

namespace Monmer.Network
{
    public enum MessageCommand : byte
    {
        /// <summary>
        /// Sent when a connection is established.
        /// </summary>
        [ReflectionCache(typeof(HandshakePayload))]
        Handshake = 0x00,

        /// <summary>
        /// Sent to respond to <see cref="Handshake"/> messages.
        /// </summary>
        HandshakeAck = 0x01,
    }
}