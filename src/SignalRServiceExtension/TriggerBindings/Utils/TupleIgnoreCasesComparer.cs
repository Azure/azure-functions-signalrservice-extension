// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class TupleIgnoreCasesComparer: IEqualityComparer<(string, string, string)>
    {
        public static readonly TupleIgnoreCasesComparer Instance = new TupleIgnoreCasesComparer();

        public bool Equals((string, string, string) x, (string, string, string) y)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(x.Item1, y.Item1) &&
                   StringComparer.OrdinalIgnoreCase.Equals(x.Item2, y.Item2) &&
                   StringComparer.OrdinalIgnoreCase.Equals(x.Item3, y.Item3);
        }

        public int GetHashCode((string, string, string) obj)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item1) ^
                   StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item2) ^
                   StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item3);
        }
    }
}
