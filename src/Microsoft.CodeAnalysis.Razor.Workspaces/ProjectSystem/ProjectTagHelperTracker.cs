// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class ProjectTagHelperTracker
    {
        private const ProjectDifference Mask =
            ProjectDifference.WorkspaceProjectAdded |
            ProjectDifference.WorkspaceProjectChanged |
            ProjectDifference.WorkspaceProjectRemoved;

        private readonly object _lock = new object();
        private readonly ProjectState _state;

        private Task<TagHelperResolutionResult> _task;

        public ProjectTagHelperTracker(ProjectState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            _state = state;
        }

        public bool IsResultAvailable => _task?.IsCompleted == true;

        public ProjectTagHelperTracker ForkFor(ProjectState state, ProjectDifference difference)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if ((difference & Mask) != 0)
            {
                return null;
            }

            return new ProjectTagHelperTracker(state);
        }

        public Task<TagHelperResolutionResult> GetTagHelperInitializationTask(ProjectSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (_task == null)
            {
                lock (_task)
                {
                    if (_task == null)
                    {
                        var resolver = _state.Services.GetLanguageServices(RazorLanguage.Name).GetRequiredService<TagHelperResolver>();
                        _task = resolver.GetTagHelpersAsync(snapshot);
                    }
                }
            }

            return _task;
        }
    }
}
