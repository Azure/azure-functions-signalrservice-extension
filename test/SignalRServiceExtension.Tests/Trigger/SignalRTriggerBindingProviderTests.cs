using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Configuration;
using SignalRServiceExtension.Tests.Utils;
using Xunit;

namespace SignalRServiceExtension.Tests
{
    public class SignalRTriggerBindingProviderTests
    {
        [Fact]
        public void ResolveAttributeParameterTest()
        {
            var bindingProvider = CreateBindingProvider();
            var attribute = new SignalRTriggerAttribute();
            var parameter = typeof(TestServerlessHub).GetMethod(nameof(TestServerlessHub.TestFunction), BindingFlags.Instance | BindingFlags.NonPublic).GetParameters()[0];
            var resolvedAttribute = bindingProvider.GetParameterResolvedAttribute(attribute, parameter);
            Assert.Equal(nameof(TestServerlessHub), resolvedAttribute.HubName);
            Assert.Equal(Category.Messages, resolvedAttribute.Category);
            Assert.Equal(nameof(TestServerlessHub.TestFunction), resolvedAttribute.Event);
            Assert.Equal(new string[] {"arg0", "arg1"}, resolvedAttribute.ParameterNames);
        }

        [Fact]
        public void ResolveConnectionAttributeParameterTest()
        {
            var bindingProvider = CreateBindingProvider();
            var attribute = new SignalRTriggerAttribute();
            var parameter = typeof(TestConnectedServerlessHub).GetMethod(nameof(TestConnectedServerlessHub.Connected), BindingFlags.Instance | BindingFlags.NonPublic).GetParameters()[0];
            var resolvedAttribute = bindingProvider.GetParameterResolvedAttribute(attribute, parameter);
            Assert.Equal(nameof(TestConnectedServerlessHub), resolvedAttribute.HubName);
            Assert.Equal(Category.Connections, resolvedAttribute.Category);
            Assert.Equal(nameof(TestConnectedServerlessHub.Connected), resolvedAttribute.Event);
            Assert.Equal(new string[] { "arg0", "arg1" }, resolvedAttribute.ParameterNames);

            parameter = typeof(TestConnectedServerlessHub).GetMethod(nameof(TestConnectedServerlessHub.Disconnected), BindingFlags.Instance | BindingFlags.NonPublic).GetParameters()[0];
            resolvedAttribute = bindingProvider.GetParameterResolvedAttribute(attribute, parameter);
            Assert.Equal(nameof(TestConnectedServerlessHub), resolvedAttribute.HubName);
            Assert.Equal(Category.Connections, resolvedAttribute.Category);
            Assert.Equal(nameof(TestConnectedServerlessHub.Disconnected), resolvedAttribute.Event);
            Assert.Equal(new string[] { "arg0", "arg1" }, resolvedAttribute.ParameterNames);
        }

        [Fact]
        public void ResolveAttributeParameterConflictTest()
        {
            var bindingProvider = CreateBindingProvider();
            var attribute = new SignalRTriggerAttribute(){ParameterNames = new string[] {"arg0"}};
            var parameter = typeof(TestServerlessHub).GetMethod(nameof(TestServerlessHub.TestFunction), BindingFlags.Instance | BindingFlags.NonPublic).GetParameters()[0];
            Assert.ThrowsAny<Exception>(() => bindingProvider.GetParameterResolvedAttribute(attribute, parameter));
        }

        private SignalRTriggerBindingProvider CreateBindingProvider()
        {
            var dispatcher = new TestTriggerDispatcher();
            return new SignalRTriggerBindingProvider(dispatcher, new DefaultNameResolver(new ConfigurationSection(new ConfigurationRoot(new List<IConfigurationProvider>()), String.Empty)), new SignalROptions());
        }

        public class TestServerlessHub : ServerlessHub
        {
            internal void TestFunction(InvocationContext context,
                [SignalRParameter]string arg0,
                [SignalRParameter]int arg1)
            {
            }
        }

        public class TestConnectedServerlessHub : ServerlessHub
        {
            internal void Connected(InvocationContext context,
                [SignalRParameter]string arg0,
                [SignalRParameter]int arg1)
            {
            }

            internal void Disconnected(InvocationContext context,
                [SignalRParameter]string arg0,
                [SignalRParameter]int arg1)
            {
            }
        }
    }
}
