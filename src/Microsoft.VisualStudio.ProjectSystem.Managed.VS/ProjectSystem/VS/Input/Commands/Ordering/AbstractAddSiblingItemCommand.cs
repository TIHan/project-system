// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    internal abstract class AbstractAddSiblingItemCommand : AbstractAddItemCommand
    {
        public AbstractAddSiblingItemCommand(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, SVsServiceProvider serviceProvider) : base(projectTree, projectVsServices, serviceProvider)
        {
        }

        protected override string GetBrowseLocations(IPhysicalProjectTree projectTree, IProjectTree node)
        {
            return projectTree.TreeProvider.GetAddNewItemDirectory(node.Parent);
        }

        protected override bool CanAdd(IPhysicalProjectTree projectTree, IProjectTree node)
        {
            return projectTree.NodeCanHaveAdditions(node.Parent) && OrderingHelper.IsValidDisplayOrderForProjectTree(node);
        }
    }
}
