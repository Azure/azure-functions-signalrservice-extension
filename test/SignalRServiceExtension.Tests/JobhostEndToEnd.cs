// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Host.Indexers;
using Microsoft.IdentityModel.Tokens;
using SignalRServiceExtension.Tests.Utils;
using SignalRServiceExtension.Tests.Utils.Loggings;
using Xunit;
using Xunit.Abstractions;

namespace SignalRServiceExtension.Tests
{
    public class JobhostEndToEnd
    {
        private const string AttributeConnectionStringName = "AttributeConnectionStringName";
        private const string DefaultUserId = "UserId";
        private const string DefaultHubName = "TestHub";
        private const string DefaultEndpoint = "http://abc.com";
        private const string DefaultAccessKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        private const string DefaultAttributeAccessKey = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";
        private const string DefaultConnectionStringFormat = "Endpoint={0};AccessKey={1};Version=1.0;";
        private static readonly string DefaultConnectionString = string.Format(DefaultConnectionStringFormat, DefaultEndpoint, DefaultAccessKey);
        private static readonly string DefaultAttributeConnectionString = string.Format(DefaultConnectionStringFormat, DefaultEndpoint, DefaultAttributeAccessKey);
        private static IServiceManager _functionOutServiceManager;
        private static string _functionOutConnectionString;
        private readonly ITestOutputHelper _output;

        public static Dictionary<string, string> ConnStrInsideOfAttrConfigDict = new Dictionary<string, string>
        {
            [AttributeConnectionStringName] = DefaultAttributeConnectionString,
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

        public static IEnumerable<object[]> SignalRAttributeTestData => new List<object[]>
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
            new object[] { typeof(SignalRFunctionsWithoutConnectionString), null, string.Format(ErrorMessages.EmptyConnectionStringErrorMessageFormat, $"{nameof(SignalRAttribute)}.{nameof(SignalRAttribute.ConnectionStringSetting)}")},
        };

        public static IEnumerable<object[]> SignalRConnectionInfoAttributeTestData => new List<object[]>
        {
            /* connection string only defined inside of attribute */
            new object[] { typeof(SignalRConnectionInfoFunctionsWithConnectionString), ConnStrInsideOfAttrConfigDict, null, DefaultAttributeConnectionString},
            
            /* connection string only defined outside of attribute */
            new object[] { typeof(SignalRConnectionInfoFunctionsWithoutConnectionString), ConnStrOutsideOfAttrConfigDict, null, DefaultConnectionString},
            
            /* connection string defined both inside and outside of attribute, and both of the connection strings are the same */
            new object[] { typeof(SignalRConnectionInfoFunctionsWithConnectionString), SameConnStrInsideAndOutsideOfAttrConfigDict, null, DefaultConnectionString},
            
            /* connection string defined both inside and outside of attribute, and both of the connection strings are different */
            new object[] { typeof(SignalRConnectionInfoFunctionsWithConnectionString), DiffConnStrInsideAndOutsideOfAttrConfigDict, ErrorMessages.DifferentConnectionStringsErrorMessage, null},
            
            /* connection string is not defined anywhere */
            new object[] { typeof(SignalRConnectionInfoFunctionsWithoutConnectionString), null, string.Format(ErrorMessages.EmptyConnectionStringErrorMessageFormat, $"{nameof(SignalRConnectionInfoAttribute)}.{nameof(SignalRConnectionInfoAttribute.ConnectionStringSetting)}"), null},
        };

        public JobhostEndToEnd(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [MemberData(nameof(SignalRAttributeTestData))]
        public async Task ValidSignalRAttributeConnectionStringSettingFacts(Type classType, Dictionary<string, string> configDict, string expectedErrorMessage)
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

        [Theory]
        [MemberData(nameof(SignalRConnectionInfoAttributeTestData))]
        public async Task ValidSignalRConnectionInfoAttributeConnectionStringSettingFacts(Type classType, Dictionary<string, string> configDict, string expectedErrorMessage, string expectedConnectionString)
        {
            var host = TestHelpers.NewHost(classType, configuration: configDict, loggerProvider: new XunitLoggerProvider(_output));
            if (expectedErrorMessage == null)
            {
                await host.GetJobHost().CallAsync($"{classType.Name}.Func");
                Assert.Equal(expectedConnectionString, _functionOutConnectionString);
            }
            else
            {
                var indexException = await Assert.ThrowsAsync<FunctionIndexingException>(() => host.StartAsync());
                Assert.Equal(expectedErrorMessage, indexException.InnerException.Message);
            }
        }

        private static void UpdateFunctionOutConnectionString(SignalRConnectionInfo connectionInfo)
        {
            var handler = new JwtSecurityTokenHandler();
            var accessKeys = new List<string> { DefaultAccessKey, DefaultAttributeAccessKey };
            var validationParameters = new TokenValidationParameters();
            validationParameters.ValidateIssuer = false;
            validationParameters.ValidateAudience = false;
            validationParameters.IssuerSigningKeyResolver = (token, securityToken, kid, validationParas) => from key in accessKeys
                                                                                                            select new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            handler.ValidateToken(connectionInfo.AccessToken, validationParameters, out var validatedToken);
            var validatedAccessKey = Encoding.UTF8.GetString((validatedToken.SigningKey as SymmetricSecurityKey)?.Key);
            _functionOutConnectionString = string.Format(DefaultConnectionStringFormat, DefaultEndpoint, validatedAccessKey);
        }

        public class SignalRFunctionsWithConnectionString
        {
            public void Func([SignalR(HubName = DefaultHubName, ConnectionStringSetting = AttributeConnectionStringName)] IAsyncCollector<SignalRMessage> signalRMessages)
            {
                _functionOutServiceManager = StaticServiceHubContextStore.ServiceHubContextStore.ServiceManager;
            }
        }

        public class SignalRConnectionInfoFunctionsWithConnectionString
        {
            public void Func([SignalRConnectionInfo(UserId = DefaultUserId, HubName = DefaultHubName, ConnectionStringSetting = AttributeConnectionStringName)] SignalRConnectionInfo connectionInfo)
            {
                UpdateFunctionOutConnectionString(connectionInfo);
            }
        }

        public class SignalRConnectionInfoFunctionsWithoutConnectionString
        {
            public void Func([SignalRConnectionInfo(UserId = DefaultUserId, HubName = DefaultHubName)] SignalRConnectionInfo connectionInfo)
            {
                UpdateFunctionOutConnectionString(connectionInfo);
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
