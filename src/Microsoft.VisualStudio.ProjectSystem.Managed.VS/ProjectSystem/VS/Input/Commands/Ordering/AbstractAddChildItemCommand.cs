// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    internal abstract class AbstractAddChildItemCommand : AbstractAddItemCommand
    {
        public AbstractAddChildItemCommand(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, SVsServiceProvider serviceProvider) : base(projectTree, projectVsServices, serviceProvider)
        {
        }

        protected override string GetBrowseLocations(IPhysicalProjectTree projectTree, IProjectTree node)
        {
            return projectTree.TreeProvider.GetAddNewItemDirectory(node);
        }

        protected override System.Threading.Tasks.Task OnAddedItemsAsync(ConfiguredProject configuredProject, IProjectTree selectedNode, ReadOnlyCollection<IProjectTree> addedNodes)
        {
            var lastChild = OrderingHelper.GetLastChild(selectedNode);

            if (lastChild != null)
            {
                return System.Threading.Tasks.Task.Run(() => addedNodes.All(x => OrderingHelper.TryMoveBelowAsync(configuredProject, x, lastChild).Result));
            }

            return System.Threading.Tasks.Task.CompletedTask;
        }

        protected override bool CanAdd(IPhysicalProjectTree projectTree, IProjectTree node)
        {
            return projectTree.NodeCanHaveAdditions(node) &&
                (OrderingHelper.IsValidDisplayOrderForProjectTree(node) || node.Flags.HasFlag(ProjectTreeFlags.Common.ProjectRoot));
        }
    }
}
