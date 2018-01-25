// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    internal abstract class AbstractAddExistingSiblingItemCommand : AbstractAddSiblingItemCommand
    {
        public AbstractAddExistingSiblingItemCommand(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, SVsServiceProvider serviceProvider) : base(projectTree, projectVsServices, serviceProvider)
        {
        }

        protected override Task<(int dialogResult, (IProjectTree selectedNode, ReadOnlyCollection<IProjectTree> addedNodes)?)>
            ShowAddItemDialogAsync(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, SVsServiceProvider serviceProvider, IProjectTree node, string browseLocations)
        {
            return AddItemHelper.ShowAddExistingItemAsSiblingDialogAsync(projectTree, projectVsServices, serviceProvider, node, browseLocations);
        }
    }
}
