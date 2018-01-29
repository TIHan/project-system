// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.Input;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    [ProjectCommand(ManagedProjectSystemPackage.ManagedProjectSystemOrderCommandSet, ManagedProjectSystemPackage.AddNewItemBelowCmdId)]
    [AppliesTo(ProjectCapability.SortByDisplayOrder)]
    internal class AddNewItemBelowCommand : AbstractAddSiblingCommand
    {
        [ImportingConstructor]
        public AddNewItemBelowCommand(IPhysicalProjectTree projectTree, ConfiguredProject configuredProject) : base(projectTree, configuredProject)
        {
        }

        protected override Task AddNode(IProjectTreeService2 treeService, IProjectTree targetParent)
        {
            return treeService.AddNewItemAsync(targetParent);
        }

        protected override async Task OnAddedNode(ConfiguredProject configuredProject, IProjectTree addedNode, IProjectTree target)
        {
            await OrderingHelper.TryMoveBelowAsync(configuredProject, addedNode, target).ConfigureAwait(true);
        }
    }
}
