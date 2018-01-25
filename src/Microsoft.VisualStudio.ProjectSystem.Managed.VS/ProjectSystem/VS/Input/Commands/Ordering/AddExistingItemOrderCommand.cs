// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Input;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    [ProjectCommand(CommandGroup.VisualStudioStandard97, (long)VSConstants.VSStd97CmdID.AddExistingItem)]
    [AppliesTo(ProjectCapability.SortByDisplayOrder)]
    [Order(2000)]
    internal class AddExistingItemOrderCommand : AbstractAddChildItemCommand
    {
        [ImportingConstructor]
        public AddExistingItemOrderCommand(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, SVsServiceProvider serviceProvider) : base(projectTree, projectVsServices, serviceProvider)
        {
        }

        protected override Task<(int dialogResult, (IProjectTree selectedNode, ReadOnlyCollection<IProjectTree> addedNodes)?)>
            ShowAddItemDialogAsync(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, SVsServiceProvider serviceProvider, IProjectTree node, string browseLocations)
        {
            return AddItemHelper.ShowAddExistingItemDialogAsync(projectTree, projectVsServices, serviceProvider, node, browseLocations);
        }
    }
}
