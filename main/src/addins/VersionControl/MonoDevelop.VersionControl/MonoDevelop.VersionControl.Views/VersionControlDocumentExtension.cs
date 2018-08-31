//
// VersionControlDocumentExtension.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using System.Threading.Tasks;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using Mono.Addins;

namespace MonoDevelop.VersionControl.Views
{
	[ExportDocumentExtension (FileExtensions = "*")]
	class VersionControlDocumentExtension : DocumentExtension
	{
		protected override Task OnLoaded (FileOpenInformation fileOpenInformation)
		{
			UpdateViews ();
			return base.OnLoaded (fileOpenInformation);
		}

		protected override void OnSaved (FileSaveInformation fileSaveInformation)
		{
			UpdateViews ();
			base.OnSaved (fileSaveInformation);
		}

		void UpdateViews ()
		{
			if (!Document.IsFile || Window.FindView<ILogView> () >= 0)
				return;

			try {
				WorkspaceObject project = Document.Project;
				if (project == null) {
					// Fix for broken .csproj and .sln files not being seen as having a project.
					foreach (var projItem in Ide.IdeApp.Workspace.GetAllItems<UnknownSolutionItem> ()) {
						if (projItem.FileName == Document.FileName) {
							project = projItem;
						}
					}

					if (project == null)
						return;
				}

				var repo = VersionControlService.GetRepository (project);
				if (repo == null)
					return;

				var versionInfo = repo.GetVersionInfo (Document.FileName, VersionInfoQueryFlags.IgnoreCache);
				if (!versionInfo.IsVersioned)
					return;

				var item = new VersionControlItem (repo, project, Document.FileName, false, null);
				var vcInfo = new VersionControlDocumentInfo (Document.PrimaryView, item, item.Repository);
				TryAttachView (vcInfo, DiffCommand.DiffViewHandlers);
				TryAttachView (vcInfo, BlameCommand.BlameViewHandlers);
				TryAttachView (vcInfo, LogCommand.LogViewHandlers);
				TryAttachView (vcInfo, MergeCommand.MergeViewHandlers);
			} catch (Exception ex) {
				// If a user is hitting this, it will show a dialog box every time they
				// switch to a document or open a document, so suppress the crash dialog
				// This bug *should* be fixed already, but it's hard to tell.
				LoggingService.LogInternalError (ex);
			}
		}

		void TryAttachView (VersionControlDocumentInfo info, string type)
		{
			var handler = AddinManager.GetExtensionObjects<IVersionControlViewHandler> (type)
				.FirstOrDefault (h => h.CanHandle (info.Item, info.Document));
			if (handler != null)
				Window.AttachViewContent (handler.CreateView (info));
		}
	}
}
