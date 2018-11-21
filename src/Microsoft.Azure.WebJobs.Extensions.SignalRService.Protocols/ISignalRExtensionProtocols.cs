using System;
using Microsoft.Azure.EventHubs;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService.Protocols
{
    public interface ISignalRExtensionProtocols
    {
        /// <summary>
        /// Gets the version of the protocol.
        /// </summary>
        int Version { get; }

        /// <summary>
        /// Creates a new <see cref="SignalRExtensionMessage"/> from the <see cref="EventData"/>.
        /// </summary>
        /// <param name="input">The message received from Event Hub.</param>
        /// <param name="message">When this method returns <c>true</c>, contains the parsed message.</param>
        /// <returns>A value that is <c>true</c> if the <see cref="SignalRExtensionMessage"/> was successfully parsed; otherwise, <c>false</c>.</returns>
        bool TryParseMessage(EventData input, out SignalRExtensionMessage message);

        /// <summary>
        /// Build <see cref="EventData"/> from some meta data.
        /// </summary>
        /// <param name="messageType">The message type in <see cref="SignalRExtensionProtocolConstants"/></param>
        /// <param name="hub">The target hub name</param>
        /// <param name="connectionId">The client connection id</param>
        /// <param name="body">The message body</param>
        /// <returns>A <see cref="EventData"/> to sent to Event Hub</returns>
        EventData BuildMessage(int messageType, string hub, string connectionId, byte[] body);
    }
}
