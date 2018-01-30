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
        private readonly IProjectTreeVsOperations _operations;

        public AbstractAddChildCommand(IPhysicalProjectTree projectTree, ConfiguredProject configuredProject, IProjectTreeVsOperations operations)
        {
            Requires.NotNull(projectTree, nameof(IPhysicalProjectTree));
            Requires.NotNull(configuredProject, nameof(configuredProject));
            Requires.NotNull(operations, nameof(operations));

            _projectTree = projectTree;
            _configuredProject = configuredProject;
            _operations = operations;
        }

        protected abstract Task AddNode(IProjectTreeVsOperations operations, IProjectTree target);

        protected abstract Task OnAddedNode(ConfiguredProject configuredProject, IProjectTree addedNode, IProjectTree targetChild);

        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string commandText, CommandStatus progressiveStatus)
        {
            var result = CommandStatusResult.Unhandled;
            if (_operations.CanAddItem(node))
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
            await _projectTree.TreeService.PublishLatestTreeAsync(waitForFileSystemUpdates: true).ConfigureAwait(true);
            await AddNode(_operations, node).ConfigureAwait(true);
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
