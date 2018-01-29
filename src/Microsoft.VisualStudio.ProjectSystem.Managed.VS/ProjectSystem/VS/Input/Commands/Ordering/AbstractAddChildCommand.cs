// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Input;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    internal abstract class AbstractAddChildCommand : AbstractSingleNodeProjectCommand
    {
        private readonly IPhysicalProjectTree _projectTree;
        private readonly ConfiguredProject _configuredProject;

        public AbstractAddChildCommand(IPhysicalProjectTree projectTree, ConfiguredProject configuredProject)
        {
            Requires.NotNull(projectTree, nameof(IPhysicalProjectTree));
            Requires.NotNull(configuredProject, nameof(configuredProject));

            _projectTree = projectTree;
            _configuredProject = configuredProject;
        }

        protected abstract Task AddNode(IProjectTreeService2 treeService, IProjectTree target);

        protected abstract Task OnAddedNode(ConfiguredProject configuredProject, IProjectTree addedNode, IProjectTree targetChild);

        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string commandText, CommandStatus progressiveStatus)
        {
            var treeService = _projectTree.TreeService as IProjectTreeService2;
            Assumes.NotNull(treeService);

            var result = CommandStatusResult.Unhandled;
            if (treeService.CanAddItemOrFolder(node))
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
            var treeService = _projectTree.TreeService as IProjectTreeService2;
            Assumes.NotNull(treeService);

            await _projectTree.TreeService.PublishLatestTreeAsync(waitForFileSystemUpdates: true).ConfigureAwait(true);
            await AddNode(treeService, node).ConfigureAwait(true);
            await _projectTree.TreeService.PublishLatestTreeAsync(waitForFileSystemUpdates: true).ConfigureAwait(true);

            var targetChild = OrderingHelper.GetLastChild(node);

            if (targetChild != null)
            {
                var updatedNode = _projectTree.CurrentTree.Find(node.Identity);
                var updatedTargetChild = _projectTree.CurrentTree.Find(targetChild.Identity);
                var addedNodes = updatedNode.Children.Where(x => !node.TryFind(x.Identity, out var subtree)).ToList();

                foreach (var addedNode in addedNodes)
                {
                    await OnAddedNode(_configuredProject, addedNode, updatedTargetChild).ConfigureAwait(true);
                }
            }

            return true;
        }
    }
}
