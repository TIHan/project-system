// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    internal abstract class AbstractAddItemCommand : AbstractSingleNodeProjectCommand
    {
        private readonly IPhysicalProjectTree _projectTree;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly SVsServiceProvider _serviceProvider;

        public AbstractAddItemCommand(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, SVsServiceProvider serviceProvider)
        {
            Requires.NotNull(projectTree, nameof(IPhysicalProjectTree));
            Requires.NotNull(projectVsServices, nameof(IUnconfiguredProjectVsServices));
            Requires.NotNull(serviceProvider, nameof(SVsServiceProvider));

            _projectTree = projectTree;
            _projectVsServices = projectVsServices;
            _serviceProvider = serviceProvider;
        }

        protected abstract string GetBrowseLocations(IPhysicalProjectTree projectTree, IProjectTree node);

        protected abstract System.Threading.Tasks.Task OnAddedItemsAsync(ConfiguredProject configuredProject, IProjectTree selectedNode, ReadOnlyCollection<IProjectTree> addedNode);

        protected abstract bool CanAdd(IPhysicalProjectTree projectTree, IProjectTree node);

        protected abstract Task<(int dialogResult, (IProjectTree selectedNode, ReadOnlyCollection<IProjectTree> addedNodes)?)>
            ShowAddItemDialogAsync(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, SVsServiceProvider serviceProvider, IProjectTree node, string browseLocations);

        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string commandText, CommandStatus progressiveStatus)
        {
            if (CanAdd(_projectTree, node))
            {
                return GetCommandStatusResult.Handled(commandText, CommandStatus.Enabled);
            }
            else
            {
                return GetCommandStatusResult.Unhandled;
            }
        }

        protected override async Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            if (!CanAdd(_projectTree, node))
            {
                return false;
            }

            var browseLocations = GetBrowseLocations(_projectTree, node);
            var (dialogResult, nodeResults) = await ShowAddItemDialogAsync(_projectTree, _projectVsServices, _serviceProvider, node, browseLocations).ConfigureAwait(true);

            if (dialogResult == VSConstants.S_OK)
            {
                Assumes.True(nodeResults.HasValue);

                await OnAddedItemsAsync(_projectVsServices.ActiveConfiguredProject, nodeResults.Value.selectedNode, nodeResults.Value.addedNodes).ConfigureAwait(false);
            }

            // Return true here regardless of whether or not the user clicked OK or they clicked Cancel. This ensures that some other
            // handler isn't called after we run.
            return dialogResult == VSConstants.S_OK || dialogResult == VSConstants.OLE_E_PROMPTSAVECANCELLED;
        }
    }
}
