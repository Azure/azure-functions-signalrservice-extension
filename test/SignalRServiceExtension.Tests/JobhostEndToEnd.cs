// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Host.Indexers;
using SignalRServiceExtension.Tests.Utils;
using SignalRServiceExtension.Tests.Utils.Loggings;
using Xunit;
using Xunit.Abstractions;

namespace SignalRServiceExtension.Tests
{
    public class JobhostEndToEnd
    {
        private const string AttributeConnectionStringName = "AttributeConnectionStringName";
        private const string DefaultConnectionString = "Endpoint=http://abc.com;AccessKey=ABC;Version=1.0;";
        private const string DefaultAttributeConnectionString = "Endpoint=http://xyz.com;AccessKey=XYZ;Version=1.0;";
        private const string DefaultHubName = "TestHub";
        private static IServiceManager _functionOutServiceManager;
        private readonly ITestOutputHelper _output;

        public static Dictionary<string, string> ConnStrInsideOfAttrConfigDict = new Dictionary<string, string>
        {
            [AttributeConnectionStringName] = DefaultConnectionString,
        };

        public static Dictionary<string, string> ConnStrOutsideOfAttrConfigDict = new Dictionary<string, string>
        {
            [Constants.AzureSignalRConnectionStringName] = DefaultConnectionString,
        };

        public static Dictionary<string, string> SameConnStrInsideAndOutsideOfAttrConfigDict = new Dictionary<string, string>
        {
            [AttributeConnectionStringName] = DefaultConnectionString,
            [Constants.AzureSignalRConnectionStringName] = DefaultConnectionString
        };

        public static Dictionary<string, string> DiffConnStrInsideAndOutsideOfAttrConfigDict = new Dictionary<string, string>
        {
            [AttributeConnectionStringName] = DefaultAttributeConnectionString,
            [Constants.AzureSignalRConnectionStringName] = DefaultConnectionString
        };

        public static IEnumerable<object[]> TestData => new List<object[]>
        {
            /* connection string only defined inside of attribute */
            new object[] { typeof(SignalRFunctionsWithConnectionString), ConnStrInsideOfAttrConfigDict, null},
            
            /* connection string only defined outside of attribute */
            new object[] { typeof(SignalRFunctionsWithoutConnectionString), ConnStrOutsideOfAttrConfigDict, null},
            
            /* connection string defined both inside and outside of attribute, and both of the connection strings are the same */
            new object[] { typeof(SignalRFunctionsWithConnectionString), SameConnStrInsideAndOutsideOfAttrConfigDict, null},
            
            /* connection string defined both inside and outside of attribute, and both of the connection strings are different */
            new object[] { typeof(SignalRFunctionsWithConnectionString), DiffConnStrInsideAndOutsideOfAttrConfigDict, ErrorMessages.DifferentConnectionStringsErrorMessage},
            
            /* connection string is not defined anywhere */
            new object[] { typeof(SignalRFunctionsWithoutConnectionString), null, string.Format(ErrorMessages.EmptyConnectionStringErrorMessageFormat, $"{nameof(SignalRAttribute)}.{nameof(SignalRConnectionInfoAttribute.ConnectionStringSetting)}")},
        };

        public JobhostEndToEnd(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public async Task ValidConnectionStringSettingFacts(Type classType, Dictionary<string, string> configDict, string expectedErrorMessage)
        {
            var host = TestHelpers.NewHost(classType, configuration: configDict, loggerProvider: new XunitLoggerProvider(_output));
            if (expectedErrorMessage == null)
            {
                await host.GetJobHost().CallAsync($"{classType.Name}.Func");
                Assert.NotNull(_functionOutServiceManager);
            }
            else
            {
                var indexException = await Assert.ThrowsAsync<FunctionIndexingException>(() => host.StartAsync());
                Assert.Equal(expectedErrorMessage, indexException.InnerException.Message);
            }
        }

        public class SignalRFunctionsWithConnectionString
        {
            public void Func([SignalR(HubName = DefaultHubName, ConnectionStringSetting = AttributeConnectionStringName)] IAsyncCollector<SignalRMessage> signalRMessages)
            {
                _functionOutServiceManager = StaticServiceHubContextStore.ServiceHubContextStore.ServiceManager;
            }
        }

        public class SignalRFunctionsWithoutConnectionString
        {
            public void Func([SignalR(HubName = DefaultHubName)] IAsyncCollector<SignalRMessage> signalRMessages)
            {
                _functionOutServiceManager = StaticServiceHubContextStore.ServiceHubContextStore.ServiceManager;
            }
        }
    }
}
