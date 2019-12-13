using System.Buffers;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal abstract class MessageParser
    {
        public static readonly MessageParser Json = new JsonMessageParser();
        public static readonly MessageParser MessagePack = new MessagePackMessageParser();

        public static MessageParser GetParser(string protocol)
        {
            return protocol == Constants.JsonContentType ? Json : MessagePack;
        }

        public abstract bool TryParseMessage(ref ReadOnlySequence<byte> buffer, out ISignalRServerlessMessage message);
    }
}
