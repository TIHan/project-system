// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Input;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    [ProjectCommand(CommandGroup.VisualStudioStandard97, (long)VSConstants.VSStd97CmdID.Paste)]
    [AppliesTo(ProjectCapability.SortByDisplayOrder)]
    [Order(int.MaxValue)]
    internal class PasteCommand : AbstractAddChildCommand
    {
        [ImportingConstructor]
        public PasteCommand(IPhysicalProjectTree projectTree, ConfiguredProject configuredProject, IProjectTreeVsOperations operations) : base(projectTree, configuredProject, operations)
        {
        }

        protected override Task AddNode(IProjectTreeVsOperations operations, IProjectTree target)
        {
            return operations.PasteFromClipboardAsync(target);
        }

        protected override async Task OnAddedNode(ConfiguredProject configuredProject, IProjectTree addedNode, IProjectTree targetChild)
        {
            await OrderingHelper.TryMoveBelowAsync(configuredProject, addedNode, targetChild).ConfigureAwait(true);
        }
    }
}
