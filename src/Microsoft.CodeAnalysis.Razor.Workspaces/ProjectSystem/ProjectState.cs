// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    // Internal tracker for DefaultProjectSnapshot
    internal class ProjectState
    {
        private readonly object _lock;

        private ProjectEngineTracker _projectEngine;
        private ProjectTagHelperTracker _tagHelpers;

        public ProjectState(
            HostWorkspaceServices services,
            HostProject hostProject,
            Project workspaceProject,
            IReadOnlyDictionary<string, DocumentState> documents,
            VersionStamp? version = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (hostProject == null)
            {
                throw new ArgumentNullException(nameof(hostProject));
            }

            Services = services;
            HostProject = hostProject;
            WorkspaceProject = workspaceProject;
            Version = version ?? VersionStamp.Create();
        }

        public IReadOnlyDictionary<string, DocumentState> Documents { get; }

        public HostProject HostProject { get; }

        public HostWorkspaceServices Services { get; }

        public Project WorkspaceProject { get; }

        public VersionStamp Version { get; }

        // Computed State
        public ProjectEngineTracker ProjectEngine
        {
            get
            {
                if (_projectEngine == null)
                {
                    lock (_lock)
                    {
                        if (_projectEngine == null)
                        {
                            _projectEngine = new ProjectEngineTracker(this);
                        }
                    }
                }

                return _projectEngine;
            }
        }

        public ProjectTagHelperTracker TagHelpers
        {
            get
            {
                if (_tagHelpers == null)
                {
                    lock (_lock)
                    {
                        if (_tagHelpers == null)
                        {
                            _tagHelpers = new ProjectTagHelperTracker(this);
                        }
                    }
                }

                return _tagHelpers;
            }
        }

        public ProjectState AddHostDocument(HostDocument hostDocument)
        {
            if (hostDocument == null)
            {
                throw new ArgumentNullException(nameof(hostDocument));
            }

            throw new NotImplementedException();
        }

        public ProjectState RemoveHostDocument(HostDocument hostDocument)
        {
            if (hostDocument == null)
            {
                throw new ArgumentNullException(nameof(hostDocument));
            }

            throw new NotImplementedException();
        }

        public ProjectState WithHostProject(HostProject hostProject)
        {
            if (hostProject == null)
            {
                throw new ArgumentNullException(nameof(hostProject));
            }

            var state = new ProjectState(Services, hostProject, WorkspaceProject, Documents, Version.GetNewerVersion());

            var difference = state.ComputeDifferenceFrom(this);
            if (difference == ProjectDifference.None)
            {
                return this;
            }

            state._projectEngine = _projectEngine?.ForkFor(state, difference);
            state._tagHelpers = _tagHelpers?.ForkFor(state, difference);

            return state;
        }

        public ProjectState WithWorkspaceProject(Project workspaceProject)
        {
            var state = new ProjectState(Services, HostProject, workspaceProject, Documents, Version.GetNewerVersion());

            var difference = state.ComputeDifferenceFrom(this);
            state._projectEngine = _projectEngine?.ForkFor(state, difference);
            state._tagHelpers = _tagHelpers?.ForkFor(state, difference);

            return state;
        }

        public ProjectDifference ComputeDifferenceFrom(ProjectState older)
        {
            if (older == null)
            {
                throw new ArgumentNullException(nameof(older));
            }

            var difference = ProjectDifference.None;
            if (!older.HostProject.Configuration.Equals(HostProject.Configuration))
            {
                difference |= ProjectDifference.ConfigurationChanged;
            }

            if (older.WorkspaceProject == null && WorkspaceProject != null)
            {
                difference |= ProjectDifference.WorkspaceProjectAdded;
            }
            else if (older.WorkspaceProject != null && WorkspaceProject == null)
            {
                difference |= ProjectDifference.WorkspaceProjectRemoved;
            }
            else if (older.WorkspaceProject?.Version != WorkspaceProject?.Version)
            {
                // For now this is very naive. We will want to consider changing
                // our logic here to be more robust.
                difference |= ProjectDifference.WorkspaceProjectChanged;
            }

            if (object.ReferenceEquals(older.Documents, Documents))
            {
                // Fast path: if the documents haven't changed then the collection will be
                // copied.
            }
            else if (older.HostProject.Documents.Count != HostProject.Documents.Count)
            {
                difference |= ProjectDifference.DocumentsChanged;
            }
            else
            {
                for (var i = 0; i < older.HostProject.Documents.Count; i++)
                {
                    if (!object.Equals(older.HostProject.Documents[i], HostProject.Documents[i]))
                    {
                        difference |= ProjectDifference.DocumentsChanged;
                        break;
                    }
                }
            }

            return difference;
        }
    }
}
