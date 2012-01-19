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
			if (document == null || !document.IsFile || document.Project == null || document.Window.FindView<IDiffView> () >= 0)
				return;
			
			var repo = VersionControlService.GetRepository (document.Project);
			if (repo == null || !repo.GetVersionInfo (document.FileName).IsVersioned)
				return;
			
			var item = new VersionControlItem (repo, document.Project, document.FileName, false, null);
			TryAttachView <IDiffView> (document, item, DiffCommand.DiffViewHandlers);
			TryAttachView <IBlameView> (document, item, BlameCommand.BlameViewHandlers);
			TryAttachView <ILogView> (document, item, LogCommand.LogViewHandlers);
			TryAttachView <IMergeView> (document, item, MergeCommand.MergeViewHandlers);
		}
		
		void TryAttachView <T>(Document document, VersionControlItem item, string type)
			where T : IAttachableViewContent
		{
			var handler = AddinManager.GetExtensionObjects<IVersionControlViewHandler<T>> (type).FirstOrDefault (h => h.CanHandle (item));
			if (handler != null) {
				document.Window.AttachViewContent (handler.CreateView (item, document.PrimaryView));
			}
		}
	}
}

