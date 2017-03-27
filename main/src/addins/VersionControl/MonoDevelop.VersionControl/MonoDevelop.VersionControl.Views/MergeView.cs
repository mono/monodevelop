//
// MergeView.cs
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
using MonoDevelop.Components;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.Views
{
	public interface IMergeView
	{
	}
	
	class MergeView : BaseView, IMergeView
	{
		VersionControlDocumentInfo info;
		MergeWidget widget;

		public override Control Control { 
			get {
				if (widget == null) {
					widget = new MergeWidget ();
					widget.Load (info);
				}
				
				return widget;
			}
		}

		public MergeView (VersionControlDocumentInfo info) : base (GettextCatalog.GetString ("Merge"), GettextCatalog.GetString ("Shows the merge view for the current file"))
		{
			this.info = info;
		}

		protected override void OnSelected ()
		{
			widget.UpdateLocalText ();
			widget.info.Start ();

			var buffer = info.Document.GetContent<MonoDevelop.Ide.Editor.TextEditor> ();
			if (buffer != null) {
				var loc = buffer.CaretLocation;
				int line = loc.Line < 1 ? 1 : loc.Line;
				int column = loc.Column < 1 ? 1 : loc.Column;
				widget.MainEditor.SetCaretTo (line, column);
			}
		}
	}
}

