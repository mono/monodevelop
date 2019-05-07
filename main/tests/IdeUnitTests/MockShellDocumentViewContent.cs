//
// MockShellDocumentViewContent.cs
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Ide.Gui.Shell;
using Xwt.Drawing;

namespace IdeUnitTests
{
	public class MockShellDocumentViewContent : MockShellDocumentView, IShellDocumentViewContent
	{
		Control control;
		Func<CancellationToken, Task<Control>> contentLoader;
		IPathedDocument pathedDocument;
		MockShellDocumentToolbar toolbar;

		public override string Tag => "Content";

		public override async Task Show ()
		{
			await base.Show ();
			control = await contentLoader (CancellationToken.None);
			ContentInserted?.Invoke (this, EventArgs.Empty);
		}

		IShellDocumentToolbar IShellDocumentViewContent.GetToolbar (DocumentToolbarKind kind)
		{
			if (toolbar == null)
				toolbar = new MockShellDocumentToolbar ();
			return toolbar;
		}

		void IShellDocumentViewContent.HidePathBar ()
		{
			pathedDocument = null;
		}

		void IShellDocumentViewContent.ReloadContent ()
		{
			control = contentLoader (CancellationToken.None).Result;
		}

		void IShellDocumentViewContent.SetContentLoader (Func<CancellationToken, Task<Control>> contentLoader)
		{
			this.contentLoader = contentLoader;
		}

		void IShellDocumentViewContent.ShowPathBar (IPathedDocument pathedDocument)
		{
			this.pathedDocument = pathedDocument;
		}

		protected override void Render (StringBuilder sb, int indent)
		{
			base.Render (sb, indent);
		}

		public event EventHandler ContentInserted;
	}
}
