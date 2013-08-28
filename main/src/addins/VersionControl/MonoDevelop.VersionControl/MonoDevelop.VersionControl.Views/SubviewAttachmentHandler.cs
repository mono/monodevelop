// 
// SubviewAttachmentHandler.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Linq;
using MonoDevelop.Components.Commands;
using Mono.Addins;
using MonoDevelop.VersionControl;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.LogReporting;
using MonoDevelop.Projects;

namespace MonoDevelop.VersionControl.Views
{
	class SubviewAttachmentHandler : CommandHandler
	{
		protected override void Run ()
		{
			Ide.IdeApp.Workbench.ActiveDocumentChanged += HandleDocumentChanged;
		}

		void HandleDocumentChanged (object sender, EventArgs e)
		{
			var document = Ide.IdeApp.Workbench.ActiveDocument;
			try {
				if (document == null || !document.IsFile || document.Window.FindView<IDiffView> () >= 0)
					return;

				IWorkspaceObject project = document.Project;
				if (project == null) {
					// Fix for broken .csproj and .sln files not being seen as having a project.
					foreach (var projItem in Ide.IdeApp.Workspace.GetAllSolutionItems<UnknownSolutionItem> ()) {
						if (projItem.FileName == document.FileName) {
							project = projItem;
						}
					}

					if (project == null)
						return;
				}
				
				var repo = VersionControlService.GetRepository (project);
				if (repo == null)
					return;

				var versionInfo = repo.GetVersionInfo (document.FileName, VersionInfoQueryFlags.IgnoreCache);
				if (!versionInfo.IsVersioned)
					return;

				var item = new VersionControlItem (repo, project, document.FileName, false, null);
				var vcInfo = new VersionControlDocumentInfo (document.PrimaryView, item, item.Repository);
				TryAttachView <IDiffView> (document, vcInfo, DiffCommand.DiffViewHandlers);
				TryAttachView <IBlameView> (document, vcInfo, BlameCommand.BlameViewHandlers);
				TryAttachView <ILogView> (document, vcInfo, LogCommand.LogViewHandlers);
				TryAttachView <IMergeView> (document, vcInfo, MergeCommand.MergeViewHandlers);
			} catch (Exception ex) {
				// If a user is hitting this, it will show a dialog box every time they
				// switch to a document or open a document, so suppress the crash dialog
				// This bug *should* be fixed already, but it's hard to tell.
				LogReportingService.ReportUnhandledException (ex, false, true);
			}
		}
		
		static void TryAttachView <T>(Document document, VersionControlDocumentInfo info, string type)
			where T : IAttachableViewContent
		{
			var handler = AddinManager.GetExtensionObjects<IVersionControlViewHandler<T>> (type)
				.FirstOrDefault (h => h.CanHandle (info.Item, info.Document));
			if (handler != null)
				document.Window.AttachViewContent (handler.CreateView (info));
		}
	}
}

