// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Input;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    internal abstract class AbstractAddSiblingCommand : AbstractSingleNodeProjectCommand
    {
        private readonly IPhysicalProjectTree _projectTree;
        private readonly ConfiguredProject _configuredProject;

        public AbstractAddSiblingCommand(IPhysicalProjectTree projectTree, ConfiguredProject configuredProject)
        {
            Requires.NotNull(projectTree, nameof(IPhysicalProjectTree));
            Requires.NotNull(configuredProject, nameof(configuredProject));

            _projectTree = projectTree;
            _configuredProject = configuredProject;
        }

        protected abstract Task AddNode(IProjectTreeServiceVsOperations treeService, IProjectTree targetParent);

        protected abstract Task OnAddedNode(ConfiguredProject configuredProject, IProjectTree addedNode, IProjectTree target);

        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string commandText, CommandStatus progressiveStatus)
        {
            var treeService = _projectTree.TreeService as IProjectTreeServiceVsOperations;
            Assumes.NotNull(treeService);

            var result = CommandStatusResult.Unhandled;
            if (node.Parent != null && OrderingHelper.HasValidDisplayOrder(node) && treeService.CanAddItem(node.Parent))
            {
                progressiveStatus |= CommandStatus.Supported | CommandStatus.Enabled;
                result = new CommandStatusResult(true, commandText, progressiveStatus);
            }
            else
            {
                result = new CommandStatusResult(false, commandText, progressiveStatus);
            }

            return result.AsTask();
        }

        protected override async Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            var treeService = _projectTree.TreeService as IProjectTreeServiceVsOperations;
            Assumes.NotNull(treeService);

            if (node.Parent != null)
            {
                await _projectTree.TreeService.PublishLatestTreeAsync(waitForFileSystemUpdates: true).ConfigureAwait(true);
                await AddNode(treeService, node.Parent).ConfigureAwait(true);
                await _projectTree.TreeService.PublishLatestTreeAsync(waitForFileSystemUpdates: true).ConfigureAwait(true);

                var updatedParent = _projectTree.CurrentTree.Find(node.Parent.Identity);
                var updatedNode = _projectTree.CurrentTree.Find(node.Identity);
                var addedNodes = updatedParent.Children.Where(x => !node.Parent.TryFind(x.Identity, out var subtree)).ToList();

                foreach (var addedNode in addedNodes)
                {
                    await OnAddedNode(_configuredProject, addedNode, updatedNode).ConfigureAwait(true);
                }
            }

            return true;
        }
    }
}
