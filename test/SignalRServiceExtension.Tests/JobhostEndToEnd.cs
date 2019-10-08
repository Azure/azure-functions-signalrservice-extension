﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
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
using Xunit.Sdk;

namespace SignalRServiceExtension.Tests
{
    public class JobhostEndToEnd
    {
        private const string AttrConnStrConfigKey = "AttributeConnectionStringName";
        private const string DefaultUserId = "UserId";
        private const string DefaultHubName = "TestHub";
        private const string DefaultEndpoint = "http://abc.com";
        private const string DefaultAccessKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        private const string DefaultAttributeAccessKey = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";
        private const string DefaultConnectionStringFormat = "Endpoint={0};AccessKey={1};Version=1.0;";
        private static readonly string DefaultConnectionString = string.Format(DefaultConnectionStringFormat, DefaultEndpoint, DefaultAccessKey);
        private static readonly string DefaultAttributeConnectionString = string.Format(DefaultConnectionStringFormat, DefaultEndpoint, DefaultAttributeAccessKey);
        private static Dictionary<string, string> _curConfigDict;
        private readonly ITestOutputHelper _output;

        public static Dictionary<string, string> ConnStrInsideOfAttrConfigDict = new Dictionary<string, string>
        {
            [AttrConnStrConfigKey] = DefaultAttributeConnectionString,
        };

        public static Dictionary<string, string> ConnStrOutsideOfAttrConfigDict = new Dictionary<string, string>
        {
            [Constants.AzureSignalRConnectionStringName] = DefaultConnectionString,
        };

        public static Dictionary<string, string> DiffConfigKeySameConnStrConfigDict = new Dictionary<string, string>
        {
            [AttrConnStrConfigKey] = DefaultConnectionString,
            [Constants.AzureSignalRConnectionStringName] = DefaultConnectionString
        };

        public static Dictionary<string, string> DiffConfigKeyDiffConnStrConfigDict = new Dictionary<string, string>
        {
            [AttrConnStrConfigKey] = DefaultAttributeConnectionString,
            [Constants.AzureSignalRConnectionStringName] = DefaultConnectionString
        };

        public static Dictionary<string, string>[] TestConfigDicts = {
            ConnStrInsideOfAttrConfigDict,
            ConnStrOutsideOfAttrConfigDict,
            DiffConfigKeySameConnStrConfigDict,
            DiffConfigKeyDiffConnStrConfigDict,
            null,
            DiffConfigKeyDiffConnStrConfigDict,
        };

        public static Type[] TestClassTypesForSignalRAttribute =
        {
            typeof(SignalRFunctionsWithConnectionString),
            typeof(SignalRFunctionsWithoutConnectionString),
            typeof(SignalRFunctionsWithConnectionString),
            typeof(SignalRFunctionsWithConnectionString),
            typeof(SignalRFunctionsWithoutConnectionString),
            typeof(SignalRFunctionsWithMultipleConnectionStrings),
        };

        public static Type[] TestClassTypesForSignalRConnectionInfoAttribute =
        {
            typeof(SignalRConnectionInfoFunctionsWithConnectionString),
            typeof(SignalRConnectionInfoFunctionsWithoutConnectionString),
            typeof(SignalRConnectionInfoFunctionsWithConnectionString),
            typeof(SignalRConnectionInfoFunctionsWithConnectionString),
            typeof(SignalRConnectionInfoFunctionsWithoutConnectionString),
            typeof(SignalRConnectionInfoFunctionsWithMultipleConnectionStrings),
        };

        public static IEnumerable<object[]> SignalRAttributeTestData => GenerateTestData(TestClassTypesForSignalRAttribute, TestConfigDicts, GenerateTestExpectedErrorMessages($"{nameof(SignalRAttribute)}.{nameof(SignalRAttribute.ConnectionStringSetting)}"));

        public static IEnumerable<object[]> SignalRConnectionInfoAttributeTestData => GenerateTestData(TestClassTypesForSignalRConnectionInfoAttribute, TestConfigDicts, GenerateTestExpectedErrorMessages($"{nameof(SignalRConnectionInfoAttribute)}.{nameof(SignalRConnectionInfoAttribute.ConnectionStringSetting)}"));

        public JobhostEndToEnd(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [MemberData(nameof(SignalRAttributeTestData))]
        [MemberData(nameof(SignalRConnectionInfoAttributeTestData))]
        public async Task ConnectionStringSettingFacts(Type classType, Dictionary<string, string> configDict, string expectedErrorMessage)
        {
            if (configDict != null)
            {
                configDict[Constants.ServiceTransportTypeName] = nameof(ServiceTransportType.Transient);
            }
            _curConfigDict = configDict;
            var host = TestHelpers.NewHost(classType, configuration: configDict, loggerProvider: new XunitLoggerProvider(_output));
            if (expectedErrorMessage == null)
            {
                await Task.WhenAll(from method in classType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
                                   select host.GetJobHost().CallAsync($"{classType.Name}.{method.Name}"));
            }
            else
            {
                var indexException = await Assert.ThrowsAsync<FunctionIndexingException>(() => host.StartAsync());
                Assert.Equal(expectedErrorMessage, indexException.InnerException.Message);
            }
        }

        public static string[] GenerateTestExpectedErrorMessages(string attributePropertyName) => new string[]
        {
            null,
            null,
            null,
            null,
            string.Format(ErrorMessages.EmptyConnectionStringErrorMessageFormat, attributePropertyName),
            null,
        };

        public static IEnumerable<object[]> GenerateTestData(Type[] classType, Dictionary<string, string>[] configDicts, string[] expectedErrorMessages)
        {
            if (classType.Length != expectedErrorMessages.Length || classType.Length != configDicts.Length)
            {
                throw  new ArgumentException($"Length of {nameof(classType)}, {nameof(configDicts)} and {nameof(expectedErrorMessages)} are not the same.");
            }
            for (var i = 0; i < expectedErrorMessages.Length; i++)
            {
                yield return new object[] { classType[i], configDicts[i], expectedErrorMessages[i] };
            }
        }

        private static void UpdateFunctionOutConnectionString(SignalRConnectionInfo connectionInfo, string expectedConfigurationKey)
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
            var actualConnectionString = string.Format(DefaultConnectionStringFormat, DefaultEndpoint, validatedAccessKey);
            Assert.Equal(_curConfigDict[expectedConfigurationKey], actualConnectionString);
        }

        private static async Task SimulateSendingMessage(IAsyncCollector<SignalRMessage> signalRMessages)
        {
            try
            {
                await signalRMessages.AddAsync(
                    new SignalRMessage
                    {
                        UserId = DefaultUserId,
                        GroupName = "",
                        Target = "newMessage",
                        Arguments = new[] { "message" }
                    });
            }
            catch
            {
                // ignore all the exception, since we only want to test whether the service manager for specific is added in the service manager store
            }
        }

        #region SignalRAttributeTests
        public class SignalRFunctionsWithConnectionString
        {
            public async Task Func([SignalR(HubName = DefaultHubName, ConnectionStringSetting = AttrConnStrConfigKey)] IAsyncCollector<SignalRMessage> signalRMessages)
            {
                await SimulateSendingMessage(signalRMessages);
                Assert.NotNull(((ServiceManagerStore)StaticServiceHubContextStore.ServiceManagerStore).GetByConfigurationKey(AttrConnStrConfigKey));
            }
        }

        public class SignalRFunctionsWithoutConnectionString
        {
            public async Task Func([SignalR(HubName = DefaultHubName)] IAsyncCollector<SignalRMessage> signalRMessages)
            {
                await SimulateSendingMessage(signalRMessages);
                Assert.NotNull(((ServiceManagerStore)StaticServiceHubContextStore.ServiceManagerStore).GetByConfigurationKey(Constants.AzureSignalRConnectionStringName));
            }
        }

        public class SignalRFunctionsWithMultipleConnectionStrings
        {
            public async Task Func1([SignalR(HubName = DefaultHubName, ConnectionStringSetting = Constants.AzureSignalRConnectionStringName)] IAsyncCollector<SignalRMessage> signalRMessages)
            {
                await SimulateSendingMessage(signalRMessages);
                Assert.NotNull(((ServiceManagerStore)StaticServiceHubContextStore.ServiceManagerStore).GetByConfigurationKey(Constants.AzureSignalRConnectionStringName));
            }

            public async Task Func2([SignalR(HubName = DefaultHubName, ConnectionStringSetting = AttrConnStrConfigKey)] IAsyncCollector<SignalRMessage> signalRMessages)
            {
                await SimulateSendingMessage(signalRMessages);
                Assert.NotNull(((ServiceManagerStore)StaticServiceHubContextStore.ServiceManagerStore).GetByConfigurationKey(AttrConnStrConfigKey));
            }
        }
        #endregion

        #region SignalRConnectionInfoAttributeTests
        public class SignalRConnectionInfoFunctionsWithConnectionString
        {
            public void Func([SignalRConnectionInfo(UserId = DefaultUserId, HubName = DefaultHubName, ConnectionStringSetting = AttrConnStrConfigKey)] SignalRConnectionInfo connectionInfo)
            {
                UpdateFunctionOutConnectionString(connectionInfo, AttrConnStrConfigKey);
            }
        }

        public class SignalRConnectionInfoFunctionsWithoutConnectionString
        {
            public void Func([SignalRConnectionInfo(UserId = DefaultUserId, HubName = DefaultHubName)] SignalRConnectionInfo connectionInfo)
            {
                UpdateFunctionOutConnectionString(connectionInfo, Constants.AzureSignalRConnectionStringName);
            }
        }

        public class SignalRConnectionInfoFunctionsWithMultipleConnectionStrings
        {
            public void Func1([SignalRConnectionInfo(UserId = DefaultUserId, HubName = DefaultHubName, ConnectionStringSetting = Constants.AzureSignalRConnectionStringName)] SignalRConnectionInfo connectionInfo)
            {
                UpdateFunctionOutConnectionString(connectionInfo, Constants.AzureSignalRConnectionStringName);
            }

            public void Func2([SignalRConnectionInfo(UserId = DefaultUserId, HubName = DefaultHubName, ConnectionStringSetting = AttrConnStrConfigKey)] SignalRConnectionInfo connectionInfo)
            {
                UpdateFunctionOutConnectionString(connectionInfo, AttrConnStrConfigKey);
            }
        }
        #endregion
    }
}
