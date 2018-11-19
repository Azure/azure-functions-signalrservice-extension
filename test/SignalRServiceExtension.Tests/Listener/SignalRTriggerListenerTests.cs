using Microsoft.Azure.EventHubs.Processor;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using BindingFlags = System.Reflection.BindingFlags;

namespace SignalRServiceExtension.Tests
{
    public class SignalRTriggerListenerTests
    {
        [Fact]
        public async Task ProcessEventsTest()
        {
            var partitionContext = GetPartitionContext();
            var checkpoint = new Mock<SignalRTriggerSharedListener.ICheckpointer>();
            checkpoint.Setup(checkPointer => checkPointer.CheckpointAsync(partitionContext)).Returns(Task.CompletedTask);
            var dispatcherMock = new Mock<ISignalRTriggerListenerDispatcher>(MockBehavior.Strict);
            dispatcherMock.Setup(dispatcher => dispatcher.DispatchListener(It.IsAny<EventData>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var eventProcessor = new SignalRTriggerSharedListener.EventProcessor(new SignalROptions(), NullLogger.Instance, dispatcherMock.Object, checkpoint.Object);

            for (int i = 0; i < 100; i++)
            {
                List<EventData> events = new List<EventData>() {new EventData(new byte[0]), new EventData(new byte[0]) , new EventData(new byte[0]) };
                await eventProcessor.ProcessEventsAsync(partitionContext, events);
            }

            checkpoint.Verify(p => p.CheckpointAsync(partitionContext), Times.Exactly(100));
        }

        [Fact]
        public async Task ProcessEventsTest_Exception()
        {
            var partitionContext = GetPartitionContext();
            var checkpoint = new Mock<SignalRTriggerSharedListener.ICheckpointer>();
            checkpoint.Setup(checkPointer => checkPointer.CheckpointAsync(partitionContext)).Returns(Task.CompletedTask);
            var dispatcherMock = new Mock<ISignalRTriggerListenerDispatcher>(MockBehavior.Strict);
            dispatcherMock
                .Setup(dispatcher =>
                    dispatcher.DispatchListener(It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception());
            var eventProcessor = new SignalRTriggerSharedListener.EventProcessor(new SignalROptions(), NullLogger.Instance, dispatcherMock.Object, checkpoint.Object);

            List<EventData> events = new List<EventData>() { new EventData(new byte[0]), new EventData(new byte[0]), new EventData(new byte[0]) };
            try
            {
                await eventProcessor.ProcessEventsAsync(partitionContext, events);
            }
            catch
            {
            }

            // Checkpoint must be called even through there're exceptions in dispatching
            checkpoint.Verify(p => p.CheckpointAsync(partitionContext), Times.Exactly(1));
        }

        internal static PartitionContext GetPartitionContext()
        {
            return (PartitionContext) Activator.CreateInstance(typeof(PartitionContext),BindingFlags.Instance|BindingFlags.NonPublic, null, new object[] {null, null, null, null, null}, null);
        }
    }
}
