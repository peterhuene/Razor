// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class ProjectEngineTracker
    {
        private const ProjectDifference Mask = ProjectDifference.ConfigurationChanged;

        private readonly object _lock = new object();

        private readonly ProjectState _state;
        private RazorProjectEngine _projectEngine;

        public ProjectEngineTracker(ProjectState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            _state = state;
        }

        public ProjectEngineTracker ForkFor(ProjectState state, ProjectDifference difference)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if ((difference & Mask) != 0)
            {
                return null;
            }

            return new ProjectEngineTracker(state) { _projectEngine = _projectEngine, };
        }

        public RazorProjectEngine GetProjectEngine(ProjectSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (_projectEngine == null)
            {
                lock (_lock)
                {
                    if (_projectEngine == null)
                    {
                        var factory = _state.Services.GetRequiredService<ProjectSnapshotProjectEngineFactory>();
                        _projectEngine =factory.Create(snapshot);
                    }
                }
            }

            return _projectEngine;
        }
    }
}
