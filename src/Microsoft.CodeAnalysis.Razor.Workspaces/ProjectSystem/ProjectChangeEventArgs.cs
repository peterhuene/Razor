// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class ProjectChangeEventArgs : EventArgs
    {
        public ProjectChangeEventArgs(string filePath, ProjectChangeKind kind)
        {
            FilePath = filePath;
            Kind = kind;
        }

        public string FilePath { get; }

        public ProjectChangeKind Kind { get; }
    }
}
