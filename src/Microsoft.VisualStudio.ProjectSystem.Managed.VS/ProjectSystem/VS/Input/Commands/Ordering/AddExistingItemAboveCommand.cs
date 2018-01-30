﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.Input;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    [ProjectCommand(ManagedProjectSystemPackage.ManagedProjectSystemOrderCommandSet, ManagedProjectSystemPackage.AddExistingItemAboveCmdId)]
    [AppliesTo(ProjectCapability.SortByDisplayOrder)]
    internal class AddExistingItemAboveCommand : AbstractAddSiblingCommand
    {
        [ImportingConstructor]
        public AddExistingItemAboveCommand(IPhysicalProjectTree projectTree, ConfiguredProject configuredProject, IProjectTreeVsOperations operations) : base(projectTree, configuredProject, operations)
        {
        }

        protected override Task AddNode(IProjectTreeVsOperations operations, IProjectTree targetParent)
        {
            return operations.ShowAddExistingFilesDialogAsync(targetParent);
        }

        protected override async Task OnAddedNode(ConfiguredProject configuredProject, IProjectTree addedNode, IProjectTree target)
        {
            await OrderingHelper.TryMoveAboveAsync(configuredProject, addedNode, target).ConfigureAwait(true);
        }
    }
}
