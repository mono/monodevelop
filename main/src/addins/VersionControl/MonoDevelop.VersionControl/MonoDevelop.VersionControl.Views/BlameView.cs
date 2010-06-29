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
namespace MonoDevelop.VersionControl.Views
{
	internal class BlameView : BaseView, IAttachableViewContent 
	{
		BlameWidget widget;
		
		public override Gtk.Widget Control { 
			get {
				return widget;
			}
		}
		
		public static void Show (VersionControlItemList items)
		{
			foreach (VersionControlItem item in items) {
				var document = IdeApp.Workbench.OpenDocument (item.Path);
				document.ActiveView.WorkbenchWindow.AttachViewContent (new ComparisonView (document, item));
				document.ActiveView.WorkbenchWindow.AttachViewContent (new BlameView (document, item));
				document.ActiveView.WorkbenchWindow.AttachViewContent (new LogView (item.Repository, item.Path));
				document.Window.SwitchView (2);
			}
		}
		
		public BlameView (Document doc, VersionControlItem item) : base ("Blame")
		{
			widget = new BlameWidget (doc, item);
		}
		
		#region IAttachableViewContent implementation
		public void Selected ()
		{
			widget.Editor.Document.IgnoreFoldings = true;
		}

		public void Deselected ()
		{
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

