// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Input;
using System.Linq;  
using System;
using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    [ProjectCommand(CommandGroup.VisualStudioStandard97, (long)VSConstants.VSStd97CmdID.Paste)]
    [AppliesTo(ProjectCapability.SortByDisplayOrder)]
    [Order(int.MaxValue)]
    internal class PasteOrderCommand : AbstractSingleNodeProjectCommand
    {
        private readonly IPhysicalProjectTree _projectTree;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly SVsServiceProvider _serviceProvider;
        private readonly IPasteHandler _pasteHandler;

        [ImportingConstructor]
        public PasteOrderCommand(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, SVsServiceProvider serviceProvider, IPasteHandler pasteHandler)
        {
            Requires.NotNull(projectTree, nameof(IPhysicalProjectTree));
            Requires.NotNull(projectVsServices, nameof(IUnconfiguredProjectVsServices));
            Requires.NotNull(serviceProvider, nameof(SVsServiceProvider));
            Requires.NotNull(pasteHandler, nameof(IPasteHandler));

            _projectTree = projectTree;
            _projectVsServices = projectVsServices;
            _serviceProvider = serviceProvider;
            _pasteHandler = pasteHandler;

            PasteProcessors = new OrderPrecedenceImportCollection<IPasteDataObjectProcessor>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst);
        }

        [ImportMany]
        private OrderPrecedenceImportCollection<IPasteDataObjectProcessor> PasteProcessors { get; set; }

        [DllImport("ole32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int OleGetClipboard(out OLE.Interop.IDataObject dataObject);

        /// <summary>
        /// Handle the Paste operation to a targetNode. Do not call directly
        /// outside of PasteFromClipboard.
        /// </summary>
        private async System.Threading.Tasks.Task PasteFromClipboardAsync(IProjectTree targetNode)
        {
            Requires.NotNull(targetNode, nameof(targetNode));

            // Get the clipboardhelper service and use it after processing dataobject
            var clipboardHelper = _serviceProvider.GetService(typeof(SVsUIHierWinClipboardHelper)) as IVsUIHierWinClipboardHelper;
            Assumes.Present(clipboardHelper);

            // Get dataobject from clipboard
            ErrorHandler.ThrowOnFailure(OleGetClipboard(out var dataObject));
            if (dataObject == null)
            {
                Marshal.ThrowExceptionForHR(VSConstants.E_UNEXPECTED);
            }

            var pasteProcessor = PasteProcessors.First().Value;

            var items = await pasteProcessor.ProcessDataObjectAsync(dataObject, targetNode, _projectTree.TreeProvider, DropEffects.Copy).ConfigureAwait(true);

            var configuredProject = _projectVsServices.ActiveConfiguredProject;
            var projectLockService = configuredProject.UnconfiguredProject.ProjectService.Services.ProjectLockService;

            // Do a write lock.
            using (var writeLock = await projectLockService.WriteLockAsync())
            {
                // Grab the project.
                var project = await writeLock.GetProjectAsync(configuredProject).ConfigureAwait(true);

                var results = await _pasteHandler.PasteItemsAsync(items, DropEffects.Copy).ConfigureAwait(true);
            }
        }

        private bool CanAdd(IPhysicalProjectTree projectTree, IProjectTree node)
        {
            return projectTree.NodeCanHaveAdditions(node);
        }

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
            await PasteFromClipboardAsync(node).ConfigureAwait(true);
            return true;
        }
    }
}
