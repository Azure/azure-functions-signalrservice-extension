﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using SignalRServiceExtension.Tests.Utils;
using Xunit;

namespace SignalRServiceExtension.Tests.Trigger
{
    public class SignalRTriggerResolverTests
    {
        public static IEnumerable<object[]> SignatureTestData()
        {
            var connectionId = "0f9c97a2f0bf4706afe87a14e0797b11";
            var accessKeys = new string[]
            {
                "7aab239577fd4f24bc919802fb629f5f",
                "a5f2815d0d0c4b00bd27e832432f91ab"
            };
            var signatures = new string[]
            {
                "sha256=7767effcb3946f3e1de039df4b986ef02c110b1469d02c0a06f41b3b727ab561",
                "sha256=d4aefb65547a00a9881fa8ac8bd03d0faf77af9da5205d45c6e57cbda4377760"
            };

            var req = TestHelpers.CreateHttpRequestMessage(String.Empty, String.Empty, String.Empty, connectionId,
                signatures: signatures);
            yield return new object[] { req, accessKeys[0], true };
            yield return new object[] { req, accessKeys[1], true };
            yield return new object[] { req, Guid.NewGuid().ToString(), false };
            yield return new object[] { req, null, false };
            yield return new object[] { req, string.Empty, false };

            req = TestHelpers.CreateHttpRequestMessage(String.Empty, String.Empty, String.Empty, connectionId);
            yield return new object[] { req, accessKeys[0], false };

            req = TestHelpers.CreateHttpRequestMessage(String.Empty, String.Empty, String.Empty, connectionId, signatures: new string[0]);
            yield return new object[] { req, accessKeys[0], false };
        }


        [Theory]
        [MemberData(nameof(SignatureTestData))]
        public void SignatureTest(HttpRequestMessage request, string accessKey, bool validate)
        {
            var resolver = new SignalRRequestResolver();
            Assert.Equal(validate, resolver.ValidateSignature(request, accessKey));
        }
    }
}
