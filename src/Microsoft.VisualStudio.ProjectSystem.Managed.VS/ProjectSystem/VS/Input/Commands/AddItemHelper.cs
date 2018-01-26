using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    internal static class AddItemHelper
    {
        /// <summary>
        /// Show the item dialog window to add new items.
        /// The items will be added as a sibling of the selected node unless the selected node is the project root.
        /// </summary>
        /// <returns>the dialog result, the newly updated selected node and the added nodes</returns>
        public static Task<(int dialogResult, (IProjectTree selectedNode, ReadOnlyCollection<IProjectTree> addedNodes)?)> ShowAddNewSiblingItemDialogAsync(
            IPhysicalProjectTree projectTree,
            IUnconfiguredProjectVsServices projectVsServices,
            SVsServiceProvider serviceProvider,
            IProjectTree selectedNode,
            string strBrowseLocations)
        {
            return ShowAddItemDialogAsync(projectTree, projectVsServices, serviceProvider, selectedNode, strBrowseLocations, AddItemAction.NewItem, addAsSibling: true);
        }

        /// <summary>
        /// Show the item dialog window to add existing items.
        /// The items will be added as a sibling of the selected node unless the selected node is the project root.
        /// </summary>
        /// <returns>the dialog result, the newly updated selected node and the added nodes</returns>
        public static Task<(int dialogResult, (IProjectTree selectedNode, ReadOnlyCollection<IProjectTree> addedNodes)?)> ShowAddExistingSiblingItemDialogAsync(
            IPhysicalProjectTree projectTree,
            IUnconfiguredProjectVsServices projectVsServices,
            SVsServiceProvider serviceProvider,
            IProjectTree selectedNode,
            string strBrowseLocations)
        {
            return ShowAddItemDialogAsync(projectTree, projectVsServices, serviceProvider, selectedNode, strBrowseLocations, AddItemAction.ExistingItem, addAsSibling: true);
        }

        /// <summary>
        /// Show the item dialog window to add new items.
        /// </summary>
        /// <returns>the dialog result, the newly updated selected node and the added nodes</returns>
        public static Task<(int dialogResult, (IProjectTree selectedNode, ReadOnlyCollection<IProjectTree> addedNodes)?)> ShowAddNewItemDialogAsync(
            IPhysicalProjectTree projectTree,
            IUnconfiguredProjectVsServices projectVsServices,
            SVsServiceProvider serviceProvider,
            IProjectTree selectedNode,
            string strBrowseLocations)
        {
            return ShowAddItemDialogAsync(projectTree, projectVsServices, serviceProvider, selectedNode, strBrowseLocations, AddItemAction.NewItem, addAsSibling: false);
        }

        /// <summary>
        /// Show the item dialog window to add existing items.
        /// </summary>
        /// <returns>the dialog result, the newly updated selected node and the added nodes</returns>
        public static Task<(int dialogResult, (IProjectTree selectedNode, ReadOnlyCollection<IProjectTree> addedNodes)?)> ShowAddExistingItemDialogAsync(
            IPhysicalProjectTree projectTree,
            IUnconfiguredProjectVsServices projectVsServices,
            SVsServiceProvider serviceProvider,
            IProjectTree selectedNode,
            string strBrowseLocations)
        {
            return ShowAddItemDialogAsync(projectTree, projectVsServices, serviceProvider, selectedNode, strBrowseLocations, AddItemAction.ExistingItem, addAsSibling: false);
        }

        /// <summary>
        /// Show the item dialog window to add new/existing items.
        /// The items will be added as a sibling of the selected node unless the selected node is the project root.
        /// </summary>
        /// <returns>the dialog result, the newly updated selected node and the added nodes</returns>
        private static async Task<(int dialogResult, (IProjectTree selectedNode, ReadOnlyCollection<IProjectTree> addedNodes)?)> ShowAddItemDialogAsync(
            IPhysicalProjectTree projectTree, 
            IUnconfiguredProjectVsServices projectVsServices, 
            SVsServiceProvider serviceProvider, 
            IProjectTree selectedNode, 
            string strBrowseLocations,
            AddItemAction addItemAction,
            bool addAsSibling)
        {
            Requires.NotNull(projectTree, nameof(projectTree));
            Requires.NotNull(projectVsServices, nameof(projectVsServices));
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(selectedNode, nameof(selectedNode));
            Requires.NotNull(strBrowseLocations, nameof(strBrowseLocations));

            await projectVsServices.ThreadingService.SwitchToUIThread();
           
            var node = selectedNode;
            // If the selected node is not the project root, let's add the new item as a sibling of the selected node by
            //     using its parent.
            if (!node.Flags.HasFlag(ProjectTreeFlags.Common.ProjectRoot) && addAsSibling)
            {
                node = selectedNode.Parent;
            }

            var (dialogResult, documents) = ShowAddItemDialog(serviceProvider, node, projectVsServices.VsProject, strBrowseLocations, addItemAction);

            if (dialogResult == VSConstants.S_OK)
            {
                Assumes.NotNullOrEmpty(documents);

                // Wait for reload.
                await projectTree.TreeService.PublishLatestTreeAsync(waitForFileSystemUpdates: true).ConfigureAwait(true);

                var addedNodes = 
                    documents
                    .Select(x => projectTree.CurrentTree.Find(node.Identity).FindImmediateChildByPath(x))
                    .Where(x => x != null).ToList().AsReadOnly();
                // This node is the one that was right clicked in order to add a new item.
                // Get the updated version.
                var updatedSelectedNode = projectTree.CurrentTree.Find(selectedNode.Identity);

                Assumes.NotNullOrEmpty(addedNodes);
                Assumes.NotNull(updatedSelectedNode);

                return (dialogResult, (updatedSelectedNode, addedNodes));
            }

            return (dialogResult, null);
        }

        private enum AddItemAction { NewItem=0, ExistingItem=1 }

        private static (int dialogResult, string[] documents) ShowAddItemDialog(SVsServiceProvider serviceProvider, IProjectTree folderNode, IVsProject vsProject, string strBrowseLocations, AddItemAction addItemAction)
        {
            var addItemDialog = serviceProvider.GetService<IVsAddProjectItemDlg, SVsAddProjectItemDlg>();
            Assumes.Present(addItemDialog);

            // Subscribe for events.
            var trackService = serviceProvider.GetService<IVsTrackProjectDocuments2, SVsTrackProjectDocuments>();
            var callback = new Callback();
            trackService.AdviseTrackProjectDocumentsEvents(callback, out var subscriptionId);

            var result = ShowAddItemDialog(addItemDialog, folderNode, vsProject, strBrowseLocations, addItemAction);

            // Unsubscribe when we have finished adding an item.
            trackService.UnadviseTrackProjectDocumentsEvents(subscriptionId);

            return (result, callback.Documents);
        }

        private static int ShowAddItemDialog(IVsAddProjectItemDlg addItemDialog, IProjectTree folderNode, IVsProject vsProject, string strBrowseLocations, AddItemAction addItemAction)
        {
            var uiFlags = __VSADDITEMFLAGS.VSADDITEM_AddNewItems | __VSADDITEMFLAGS.VSADDITEM_SuggestTemplateName | __VSADDITEMFLAGS.VSADDITEM_AllowHiddenTreeView;
            if (addItemAction == AddItemAction.ExistingItem)
            {
                uiFlags = __VSADDITEMFLAGS.VSADDITEM_AddExistingItems | __VSADDITEMFLAGS.VSADDITEM_AllowMultiSelect | __VSADDITEMFLAGS.VSADDITEM_AllowStickyFilter | __VSADDITEMFLAGS.VSADDITEM_ProjectHandlesLinks;
            }

            var strFilter = string.Empty;
            Guid addItemTemplateGuid = Guid.Empty;  // Let the dialog ask the hierarchy itself

            return addItemDialog.AddProjectItemDlg(folderNode.GetHierarchyId(), ref addItemTemplateGuid, vsProject, (uint)uiFlags,
                null, null, ref strBrowseLocations, ref strFilter, out int iDontShowAgain);
        }

        private class Callback : IVsTrackProjectDocumentsEvents2
        {
            public string[] Documents { get; set; }

            public int OnQueryAddFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYADDFILEFLAGS[] rgFlags, VSQUERYADDFILERESULTS[] pSummaryResult, VSQUERYADDFILERESULTS[] rgResults)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterAddFilesEx(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSADDFILEFLAGS[] rgFlags)
            {
                Documents = rgpszMkDocuments;
                return VSConstants.S_OK;
            }

            public int OnAfterAddDirectoriesEx(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSADDDIRECTORYFLAGS[] rgFlags)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterRemoveFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSREMOVEFILEFLAGS[] rgFlags)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterRemoveDirectories(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSREMOVEDIRECTORYFLAGS[] rgFlags)
            {
                return VSConstants.S_OK;
            }

            public int OnQueryRenameFiles(IVsProject pProject, int cFiles, string[] rgszMkOldNames, string[] rgszMkNewNames, VSQUERYRENAMEFILEFLAGS[] rgFlags, VSQUERYRENAMEFILERESULTS[] pSummaryResult, VSQUERYRENAMEFILERESULTS[] rgResults)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterRenameFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEFILEFLAGS[] rgFlags)
            {
                return VSConstants.S_OK;
            }

            public int OnQueryRenameDirectories(IVsProject pProject, int cDirs, string[] rgszMkOldNames, string[] rgszMkNewNames, VSQUERYRENAMEDIRECTORYFLAGS[] rgFlags, VSQUERYRENAMEDIRECTORYRESULTS[] pSummaryResult, VSQUERYRENAMEDIRECTORYRESULTS[] rgResults)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterRenameDirectories(int cProjects, int cDirs, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEDIRECTORYFLAGS[] rgFlags)
            {
                return VSConstants.S_OK;
            }

            public int OnQueryAddDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSQUERYADDDIRECTORYFLAGS[] rgFlags, VSQUERYADDDIRECTORYRESULTS[] pSummaryResult, VSQUERYADDDIRECTORYRESULTS[] rgResults)
            {
                return VSConstants.S_OK;
            }

            public int OnQueryRemoveFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYREMOVEFILEFLAGS[] rgFlags, VSQUERYREMOVEFILERESULTS[] pSummaryResult, VSQUERYREMOVEFILERESULTS[] rgResults)
            {
                return VSConstants.S_OK;
            }

            public int OnQueryRemoveDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSQUERYREMOVEDIRECTORYFLAGS[] rgFlags, VSQUERYREMOVEDIRECTORYRESULTS[] pSummaryResult, VSQUERYREMOVEDIRECTORYRESULTS[] rgResults)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterSccStatusChanged(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, uint[] rgdwSccStatus)
            {
                return VSConstants.S_OK;
            }
        }
    }
}
