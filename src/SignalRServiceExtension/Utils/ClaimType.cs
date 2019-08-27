using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal static class ClaimType
    {
        public const string AzureSignalRSysPrefix = "asrs.s.";
        public const string AuthenticationType = AzureSignalRSysPrefix + "aut";
        public const string NameType = AzureSignalRSysPrefix + "nt";
        public const string RoleType = AzureSignalRSysPrefix + "rt";
        public const string UserId = AzureSignalRSysPrefix + "uid";
        public const string ServerName = AzureSignalRSysPrefix + "sn";
        public const string ServerStickyMode = AzureSignalRSysPrefix + "ssticky";
        public const string Id = AzureSignalRSysPrefix + "id";
        public const string AppName = AzureSignalRSysPrefix + "apn";
        public const string Version = AzureSignalRSysPrefix + "vn";

        public const string AzureSignalRUserPrefix = "asrs.u.";
    }
}
