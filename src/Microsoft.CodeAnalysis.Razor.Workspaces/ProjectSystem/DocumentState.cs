// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class DocumentState
    {
        public DocumentState(HostDocument hostDocument)
        {
            if (hostDocument == null)
            {
                throw new ArgumentNullException(nameof(hostDocument));
            }

            HostDocument = hostDocument;
        }

        public HostDocument HostDocument { get; }
    }
}
