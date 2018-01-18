// ListWindow.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2005 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Gui.Content;
using System.Linq;
using Gdk;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Editor.Extension;

namespace MonoDevelop.Ide.CodeCompletion
{
	interface IListDataProvider
	{
		int ItemCount { get; }
		string GetText (int n);
		int [] GetHighlightedTextIndices (int n);
		string GetMarkup (int n);
		bool HasMarkup (int n);
		string GetCompletionText (int n);
		CompletionData GetCompletionData (int n);
		string GetDescription (int n, bool isSelected);
		string GetRightSideDescription (int n, bool isSelected);
		Xwt.Drawing.Image GetIcon (int n);
		int CompareTo (int n, int m);
	}
}

