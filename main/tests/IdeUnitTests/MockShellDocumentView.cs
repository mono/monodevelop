//
// MockShellDocumentView.cs
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
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Ide.Gui.Shell;
using Xwt.Drawing;
using System.Text;
using System.Threading.Tasks;

namespace IdeUnitTests
{
	public class MockShellDocumentView: IShellDocumentViewItem
	{
		bool disposed;
		object delegatedCommandTarget;
		string title;
		string accessibilityDescription;

		DocumentView IShellDocumentViewItem.Item { get; set; }

		public virtual Task Show ()
		{
			return Task.CompletedTask;
		}

		void IShellDocumentViewItem.Dispose ()
		{
			disposed = true;
		}

		void IShellDocumentViewItem.SetDelegatedCommandTarget (object target)
		{
			delegatedCommandTarget = target;
		}

		void IShellDocumentViewItem.SetTitle (string label, Image icon, string accessibilityDescription)
		{
			title = label;
			this.accessibilityDescription = accessibilityDescription;
		}

		public virtual string Tag => "";

		public override string ToString ()
		{
			var sb = new StringBuilder ();
			Render (sb, 0);
			return sb.ToString ();
		}

		protected virtual void Render (StringBuilder sb, int indent)
		{
			sb.Append (new string (' ', indent));
			sb.Append ("[").Append (Tag).AppendLine ("]");
			RenderProperty (sb, indent + 2, "Title", title);
			RenderProperty (sb, indent + 2, "AccessibilityDescription", accessibilityDescription);
		}

		protected void RenderProperty (StringBuilder sb, int indent, string name, string value)
		{
			if (!string.IsNullOrEmpty (value)) {
				sb.Append (new string (' ', indent));
				sb.Append (name).Append (" = ").AppendLine (value);
			}
		}

		public void GrabViewFocus ()
		{
		}

		public event EventHandler GotFocus;
		public event EventHandler LostFocus;
	}
}
