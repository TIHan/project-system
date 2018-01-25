// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    [ProjectCommand(ManagedProjectSystemPackage.ManagedProjectSystemOrderCommandSet, ManagedProjectSystemPackage.AddExistingItemAboveCmdId)]
    [AppliesTo(ProjectCapability.SortByDisplayOrder)]
    internal class AddExistingItemAboveCommand : AbstractAddExistingSiblingItemCommand
    {
        [ImportingConstructor]
        public AddExistingItemAboveCommand(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, SVsServiceProvider serviceProvider) : base(projectTree, projectVsServices, serviceProvider)
        {
        }

        protected override System.Threading.Tasks.Task OnAddedItemsAsync(ConfiguredProject configuredProject, IProjectTree selectedNode, ReadOnlyCollection<IProjectTree> addedNodes)
        {
            return System.Threading.Tasks.Task.Run(() => addedNodes.All(x => OrderingHelper.TryMoveAboveAsync(configuredProject, x, selectedNode).Result));
        }
    }
}
