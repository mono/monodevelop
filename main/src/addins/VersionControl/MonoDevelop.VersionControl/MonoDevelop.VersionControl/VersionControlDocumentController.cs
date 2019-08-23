//
// VersionControlDocumentController.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Projects;
using MonoDevelop.VersionControl.Views;
using System.Threading;
using MonoDevelop.Ide;

namespace MonoDevelop.VersionControl
{
	[ExportDocumentControllerExtension (MimeType = "*")]
	public class VersionControlDocumentController: DocumentControllerExtension
	{
		WorkspaceObject project;
		Repository repo;

		readonly WeakReference<DocumentView> diffViewRef = new WeakReference<DocumentView> (null);
		readonly WeakReference<DocumentView> blameViewRef = new WeakReference<DocumentView> (null);
		readonly WeakReference<DocumentView> logViewRef = new WeakReference<DocumentView> (null);
		readonly WeakReference<DocumentView> mergeViewRef = new WeakReference<DocumentView> (null);

		DocumentView mainView;
		VersionControlDocumentInfo vcInfo;

		public override async Task<bool> SupportsController (DocumentController controller)
		{
			if (!(controller is FileDocumentController fileController) || !IdeApp.IsInitialized)
				return false;

			project = controller.Owner;
			if (project == null) {
				// Fix for broken .csproj and .sln files not being seen as having a project.
				foreach (var projItem in Ide.IdeApp.Workspace.GetAllItems<UnknownSolutionItem> ()) {
					if (projItem.FileName == fileController.FilePath)
						project = projItem;
				}

				if (project == null)
					return false;
			}

			repo = VersionControlService.GetRepository (project);
			if (repo == null)
				return false;

			return true;
		}

		protected internal override async Task<DocumentView> OnInitializeView ()
		{
			mainView = await base.OnInitializeView ();
			var controller = (FileDocumentController)Controller;
			var item = new VersionControlItem (repo, project, controller.FilePath, false, null);
			vcInfo = new VersionControlDocumentInfo (this, Controller, item, item.Repository) {
				Document = Controller.Document
			};
			await UpdateSubviewsAsync ();
			VersionControlService.FileStatusChanged += VersionControlService_FileStatusChanged;
			return mainView;
		}

		public override void Dispose ()
		{
			VersionControlService.FileStatusChanged -= VersionControlService_FileStatusChanged;
			base.Dispose ();
		}

		void VersionControlService_FileStatusChanged (object sender, FileUpdateEventArgs args)
		{

			foreach (var file in args) {
				if (!vcInfo.Document.FilePath.IsNullOrEmpty && vcInfo.Document.FilePath.Equals (file.FilePath)) {
					Runtime.RunInMainThread (async () => {
						await UpdateSubviewsAsync ();
					});
				}
			}
		}

		bool showSubviews;
		async Task UpdateSubviewsAsync ()
		{
			var hasVersionInfo = repo.TryGetVersionInfo (this.vcInfo.Document.FilePath, out var info);
			if (hasVersionInfo)
				vcInfo.Item.VersionInfo = info;

			if (!hasVersionInfo || !info.IsVersioned) {
				if (!showSubviews)
					return;
				showSubviews = false;
				if (diffViewRef.TryGetTarget (out var diffView) && diffView != null) {
					mainView.AttachedViews.Remove (diffView);
					diffView.Dispose ();
					diffViewRef.SetTarget (null);
				}

				if (blameViewRef.TryGetTarget (out var blameView) && blameView != null) {
					mainView.AttachedViews.Remove (blameView);
					blameView.Dispose ();
					blameViewRef.SetTarget (null);
				}

				if (logViewRef.TryGetTarget (out var logView) && logView != null) {
					mainView.AttachedViews.Remove (logView);
					logView.Dispose ();
					logViewRef.SetTarget (null);
				}

				if (mergeViewRef.TryGetTarget (out var mergeView) && mergeView != null) {
					mainView.AttachedViews.Remove (mergeView);
					mergeView.Dispose ();
					mergeViewRef.SetTarget (null);
				}
			} else {
				if (showSubviews)
					return;
				showSubviews = true;
				diffViewRef.SetTarget (await TryAttachView (mainView, vcInfo, DiffCommand.DiffViewHandlers, GettextCatalog.GetString ("Changes"), GettextCatalog.GetString ("Shows the differences in the code between the current code and the version in the repository")));
				blameViewRef.SetTarget (await TryAttachView (mainView, vcInfo, BlameCommand.BlameViewHandlers, GettextCatalog.GetString ("Authors"), GettextCatalog.GetString ("Shows the authors of the current file")));
				logViewRef.SetTarget (await TryAttachView (mainView, vcInfo, LogCommand.LogViewHandlers, GettextCatalog.GetString ("Log"), GettextCatalog.GetString ("Shows the source control log for the current file")));
				mergeViewRef.SetTarget (await TryAttachView (mainView, vcInfo, MergeCommand.MergeViewHandlers, GettextCatalog.GetString ("Merge"), GettextCatalog.GetString ("Shows the merge view for the current file")));
			}
		}

		async Task<DocumentView> TryAttachView (DocumentView mainView, VersionControlDocumentInfo info, string type, string title, string description)
		{
			var handler = AddinManager.GetExtensionObjects<IVersionControlViewHandler> (type)
				.FirstOrDefault (h => h.CanHandle (info.Item, info.VersionControlExtension.Controller));
			if (handler != null) {
				var controller = handler.CreateView (info);
				if (controller == null)
					return null;
				await controller.Initialize (null, null);
				var item = await controller.GetDocumentView ();
				item.Title = title;
				item.AccessibilityDescription = description;
				mainView.AttachedViews.Add (item);
				return item;
			}
			return null;
		}

		internal void ShowDiffView (Revision originalRevision = null, Revision diffRevision = null, int line = -1)
		{
			if (diffViewRef.TryGetTarget (out var diffView)) {
				if (originalRevision != null && diffRevision != null && diffView.SourceController is DiffView content) {
					content.ComparisonWidget.info.RunAfterUpdate (delegate {
						content.ComparisonWidget.SetRevision (content.ComparisonWidget.DiffEditor, diffRevision);
						content.ComparisonWidget.SetRevision (content.ComparisonWidget.OriginalEditor, originalRevision);
						if (line != -1) {
							content.ComparisonWidget.DiffEditor.Caret.Location = new Ide.Editor.DocumentLocation (line, 1);
							content.ComparisonWidget.DiffEditor.CenterToCaret ();
						}
					});
				}
				diffView.SetActive ();
			}
		}

		internal void ShowBlameView ()
		{
			if (blameViewRef.TryGetTarget(out var blameView))
				blameView.SetActive ();
		}

		internal void ShowLogView (Revision revision = null)
		{
			if (logViewRef.TryGetTarget (out var logView)) {
				if (revision != null && logView.SourceController is LogView content)
					content.LogWidget.SelectedRevision = revision;

				logView.SetActive ();
			}
		}

		internal void ShowMergeView ()
		{
			if (mergeViewRef.TryGetTarget (out var mergeView))
				mergeView.SetActive ();
		}
	}
}
