//
// DocumentViewContainer.cs
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

using MonoDevelop.Ide.Gui.Documents;
using System;

namespace MonoDevelop.Ide.Gui.Shell
{
	internal interface IShellDocumentViewContainer : IShellDocumentViewItem
	{
		void InsertView (int position, IShellDocumentViewItem view);
		void ReplaceView (int position, IShellDocumentViewItem view);
		void RemoveView (int tabPos);
		void ReorderView (int currentIndex, int newIndex);
		void SetCurrentMode (DocumentViewContainerMode currentMode);
		void SetSupportedModes (DocumentViewContainerMode supportedModes);
		void RemoveAllViews ();
		double [] GetRelativeSplitSizes ();
		void SetRelativeSplitSizes (double [] sizes);
		IShellDocumentViewItem ActiveView { get; set; }
		event EventHandler ActiveViewChanged;
	}
}
