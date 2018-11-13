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
        /// Writes the specified <see cref="SignalRExtensionMessage"/> to EventData.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="output">The output writer.</param>
        EventData WriteMessage();
    }
}
