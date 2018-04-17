// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    // All of the public state of this is immutable - we create a new instance and notify subscribers
    // when it changes. 
    //
    // However we use the private state to track things like dirty/clean.
    //
    // See the private constructors... When we update the snapshot we either are processing a Workspace
    // change (Project) or updating the computed state (ProjectSnapshotUpdateContext). We don't do both
    // at once. 
    internal class DefaultProjectSnapshot : ProjectSnapshot
    {
        private readonly Lazy<RazorProjectEngine> _projectEngine;
        private ProjectSnapshotComputedState _computedState;

        public DefaultProjectSnapshot(ProjectState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            State = state;
        }

        public ProjectState State { get; }

        public override RazorConfiguration Configuration => HostProject.Configuration;

        public override IReadOnlyList<DocumentSnapshot> Documents => State.HostProject.Documents;

        public override string FilePath => State.HostProject.FilePath;

        public HostProject HostProject => State.HostProject;

        public override bool IsInitialized => WorkspaceProject != null;

        public override VersionStamp Version => State.Version;

        public override Project WorkspaceProject => State.WorkspaceProject;

        public override RazorProjectEngine GetProjectEngine()
        {
            return State.ProjectEngine.GetProjectEngine(this);
        }

        public override async Task<IReadOnlyList<TagHelperDescriptor>> GetTagHelpersAsync()
        {
            var result = await State.TagHelpers.GetTagHelperInitializationTask(this);
            return result.Descriptors;
        }

        public override bool TryGetTagHelpers(out IReadOnlyList<TagHelperDescriptor> results)
        {
            if (State.TagHelpers.IsResultAvailable)
            {
                results = State.TagHelpers.GetTagHelperInitializationTask(this).Result.Descriptors;
                return true;
            }

            results = null;
            return false;
        }
    }
}