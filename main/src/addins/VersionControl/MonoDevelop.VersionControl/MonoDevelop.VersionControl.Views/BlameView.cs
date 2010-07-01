// 
// BlameView.cs
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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using MonoDevelop.Core;
namespace MonoDevelop.VersionControl.Views
{
	internal class BlameView : BaseView, IAttachableViewContent 
	{
		BlameWidget widget;
		VersionControlDocumentInfo info;
		
		public override Gtk.Widget Control { 
			get {
				return widget;
			}
		}
		
		public static void Show (VersionControlItemList items)
		{
			foreach (VersionControlItem item in items) {
				var document = IdeApp.Workbench.OpenDocument (item.Path);
				ComparisonView.AttachViewContents (document, item);
				document.Window.SwitchView (3);
			}
		}
		
		public static bool CanShow (Repository repo, FilePath file)
		{
			if (null != repo && repo.CanGetAnnotations (file)) 
				return true;
			return false;
		}
		
		
		public BlameView (VersionControlDocumentInfo info) : base ("Blame")
		{
			this.info = info;
			widget = new BlameWidget (info);
		}
		
		#region IAttachableViewContent implementation
		public void Selected ()
		{
			widget.Editor.Document.IgnoreFoldings = true;
			widget.Editor.Caret.Location = info.Document.TextEditorData.Caret.Location;
			widget.Editor.CenterToCaret ();
		}

		public void Deselected ()
		{
			info.Document.TextEditorData.Caret.Location = widget.Editor.Caret.Location;
			info.Document.TextEditorData.Parent.CenterToCaret ();
			widget.Editor.Document.IgnoreFoldings = false;
		}

		public void BeforeSave ()
		{
		}

		public void BaseContentChanged ()
		{
		}
		#endregion
	}
}

