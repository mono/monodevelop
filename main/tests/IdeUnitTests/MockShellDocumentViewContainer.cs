//
// MockShellDocumentViewContainer.cs
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
using System.Threading.Tasks;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Ide.Gui.Shell;
using Xwt.Drawing;

namespace IdeUnitTests
{
	public class MockShellDocumentViewContainer : MockShellDocumentView, IShellDocumentViewContainer
	{
		public override string Tag => "Container";

		public List<MockShellDocumentView> Views = new List<MockShellDocumentView> ();

		IShellDocumentViewItem IShellDocumentViewContainer.ActiveView { get; set; }

		public event EventHandler ActiveViewChanged;

		void IShellDocumentViewContainer.InsertView (int position, IShellDocumentViewItem view)
		{
			Views.Insert (position, (MockShellDocumentView) view);
		}

		void IShellDocumentViewContainer.RemoveAllViews ()
		{
			Views.Clear ();
		}

		void IShellDocumentViewContainer.RemoveView (int tabPos)
		{
			Views.RemoveAt (tabPos);
		}

		void IShellDocumentViewContainer.ReorderView (int currentIndex, int newIndex)
		{
			var v = Views [currentIndex];
			Views.RemoveAt (currentIndex);
			if (currentIndex < newIndex)
				currentIndex--;
			Views.Insert (newIndex, v);
		}

		void IShellDocumentViewContainer.ReplaceView (int position, IShellDocumentViewItem view)
		{
			Views [position] = (MockShellDocumentView)view;
		}

		void IShellDocumentViewContainer.SetCurrentMode (DocumentViewContainerMode currentMode)
		{
		}

		void IShellDocumentViewContainer.SetSupportedModes (DocumentViewContainerMode supportedModes)
		{
		}

		public override async Task Show ()
		{
			foreach (var v in Views)
				await v.Show ();
		}
	}
}
